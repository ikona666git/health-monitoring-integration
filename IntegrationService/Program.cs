using System.Text;
using System.Text;
using System.Text.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/integration-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("=== ЗАПУСК ИНТЕГРАЦИОННОГО СЕРВИСА ===");

var httpClient = new HttpClient();

try
{
    var measurement = new
    {
        user_id = "user123",
        metric_type = "heart_rate",
        value = 135,
        timestamp = DateTime.UtcNow.ToString("o")
    };

    Log.Information("Отправка измерения: {@Measurement}", measurement);
    var json = JsonSerializer.Serialize(measurement);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Шаг 1: Ingestion
    Log.Information("ШАГ 1: Ingestion...");
    var ingestionResponse = await httpClient.PostAsync("http://localhost:5001/measurements", content);
    var ingestionResult = await ingestionResponse.Content.ReadAsStringAsync();
    Log.Information("Ingestion ответ: {Result}", ingestionResult);

    // Шаг 2: Rules Engine
    Log.Information("ШАГ 2: Rules Engine...");
    var rulesResponse = await httpClient.PostAsync("http://localhost:5002/check", content);
    var rulesResult = await rulesResponse.Content.ReadAsStringAsync();
    Log.Information("Rules Engine ответ: {Result}", rulesResult);

    // Шаг 3: Alerting
    if (rulesResult.Contains("true"))
    {
        Log.Warning("ШАГ 3: Отправка уведомления...");
        await httpClient.PostAsync("http://localhost:3000/alert", content);
        Log.Information("Уведомление отправлено");
    }

    Log.Information("=== СЦЕНАРИЙ УСПЕШНО ЗАВЕРШЁН ===");
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка: {Message}", ex.Message);
}
finally
{
    Log.CloseAndFlush();
}

Console.WriteLine("Логи в папке logs/");
Console.WriteLine("Нажмите Enter...");
Console.ReadLine();