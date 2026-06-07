using System.Text;
using System.Text.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Запуск SAGA сценария...");

var httpClient = new HttpClient();

try
{
    var measurement = new
    {
        user_id = "user123",
        metric_type = "heart_rate",
        value = 135,
        timestamp = DateTime.UtcNow.ToString("o"),
        source = "saga_test"
    };

    // Шаг 1: Отправка в Ingestion (порт 5001)
    Log.Information("ШАГ 1: Отправка в Ingestion...");
    var ingestionJson = JsonSerializer.Serialize(measurement);
    var ingestionContent = new StringContent(ingestionJson, Encoding.UTF8, "application/json");
    var ingestionResponse = await httpClient.PostAsync("http://localhost:5001/measurements", ingestionContent);
    var ingestionResult = await ingestionResponse.Content.ReadAsStringAsync();
    Log.Information("Ответ Ingestion: {0}", ingestionResult);

    // Шаг 2: Проверка норм в Rules (порт 5002)
    Log.Information("ШАГ 2: Проверка норм в Rules Engine...");
    var rulesContent = new StringContent(ingestionJson, Encoding.UTF8, "application/json");
    var rulesResponse = await httpClient.PostAsync("http://localhost:5002/check", rulesContent);
    var rulesResult = await rulesResponse.Content.ReadAsStringAsync();
    Log.Information("Ответ Rules Engine: {0}", rulesResult);

    // Шаг 3: Уведомление в Alerting (порт 3000)
    if (rulesResult.Contains("out_of_range") || rulesResult.Contains("true"))
    {
        Log.Warning("ШАГ 3: Отправка уведомления...");
        var alertContent = new StringContent(rulesResult, Encoding.UTF8, "application/json");
        await httpClient.PostAsync("http://localhost:3000/alert", alertContent);
        Log.Information("Уведомление отправлено!");
    }

    Log.Information("=== СЦЕНАРИЙ УСПЕШНО ЗАВЕРШЁН ===");
}
catch (Exception ex)
{
    Log.Error(ex, "Ошибка при выполнении сценария");
}

Log.CloseAndFlush();
Console.WriteLine("Нажмите Enter для выхода...");
Console.ReadLine();