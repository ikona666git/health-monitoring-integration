using IntegrationService.Mapping;
using IntegrationService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/integration-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(dispose: true);
    });
    services.AddAutoMapper(typeof(IntegrationProfile));
    services.AddHttpClient<IHealthIntegrationService, HealthIntegrationService>();
    services.AddScoped<IHealthIntegrationService, HealthIntegrationService>();

    var provider = services.BuildServiceProvider();
    var integrator = provider.GetRequiredService<IHealthIntegrationService>();

    Log.Information("=== Тестирование интеграционного сервиса ===");

    var testMeasurement = new IntegrationService.Models.Measurement
    {
        UserId = "user123",
        MetricType = "heart_rate",
        Value = 135,
        Timestamp = DateTime.UtcNow.ToString("o"),
        Source = "integration_test"
    };

    var result = await integrator.ProcessMeasurementAsync(testMeasurement);

    Log.Information("Результат: {@Result}", result);

    var history = await integrator.GetHistoryAsync("user123");
    Log.Information("История проверок: {@History}", history);
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка при выполнении интеграционного сервиса");
}
finally
{
    Log.CloseAndFlush();
}