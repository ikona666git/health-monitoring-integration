using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/integration-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

var ingestionUrl = builder.Configuration["ServiceUrls:Ingestion"]
    ?? Environment.GetEnvironmentVariable("INGESTION_URL")
    ?? "http://localhost:5101";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Health Monitoring — Integration API",
        Version = "v1",
        Description = "Интеграционный слой (.NET): оркестрация сквозного сценария Датчики → Показатели → Нормы → Уведомления.",
    });
});

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

builder.Services.AddHttpClient("ingestion", client =>
{
    client.BaseAddress = new Uri(ingestionUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddPolicyHandler(policyWrap);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Integration API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseSerilogRequestLogging();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithTags("Health")
    .WithSummary("Health check интеграционного слоя");

app.MapPost("/api/measurements", async (MeasurementRequest request, IHttpClientFactory httpFactory) =>
{
    var httpClient = httpFactory.CreateClient("ingestion");
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync("/measurements", content);
    var body = await response.Content.ReadAsStringAsync();

    return response.IsSuccessStatusCode
        ? Results.Content(body, "application/json")
        : Results.Problem(detail: body, statusCode: (int)response.StatusCode);
})
.WithName("SendMeasurement")
.WithTags("Integration")
.WithSummary("Отправить измерение через интеграционный слой")
.WithDescription("Проксирует запрос в Ingestion и возвращает полный результат сквозного сценария.");

Log.Information("Integration API запущен. Swagger: /swagger");
app.Run();

public record MeasurementRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("metric_type")] string MetricType,
    [property: JsonPropertyName("value")] double Value,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null);
