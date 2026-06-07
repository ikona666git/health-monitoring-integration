using IntegrationService.Data;
using IntegrationService.Data;
using IntegrationService.Mapping;
using IntegrationService.Services;
using Microsoft.EntityFrameworkCore;
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
    
    // Добавляем DbContext для SQL Server
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer("Server=localhost;Database=HealthMonitoringDb;Trusted_Connection=True;TrustServerCertificate=True;"));
    
    services.AddAutoMapper(typeof(IntegrationProfile));
    services.AddHttpClient<IHealthIntegrationService, HealthIntegrationService>();
    services.AddScoped<IHealthIntegrationService, HealthIntegrationService>();

    var provider = services.BuildServiceProvider();
    
    // Создаём базу данных, если её нет
    using (var scope = provider.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        Log.Information("База данных проверена/создана");
    }
    
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
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка при выполнении интеграционного сервиса");
}
finally
{
    Log.CloseAndFlush();
}