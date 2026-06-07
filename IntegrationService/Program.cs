using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/integration-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("=== ОПТИМИЗИРОВАННЫЙ ЗАПУСК ===");

var stopwatch = Stopwatch.StartNew();
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

    // ПАРАЛЛЕЛЬНЫЕ ВЫЗОВЫ (Ingestion + Rules Engine)
    Log.Information("ШАГ 1+2: Параллельный вызов Ingestion и Rules Engine...");
    var sw = Stopwatch.StartNew();
    
    var ingestionTask = httpClient.PostAsync("http://localhost:5001/measurements", content);
    var rulesTask = httpClient.PostAsync("http://localhost:5002/check", content);
    
    await Task.WhenAll(ingestionTask, rulesTask);
    sw.Stop();
    Log.Information("Ingestion + Rules выполнены за {Elapsed} мс", sw.ElapsedMilliseconds);

    var ingestionResult = await ingestionTask.Result.Content.ReadAsStringAsync();
    var rulesResult = await rulesTask.Result.Content.ReadAsStringAsync();
    
    Log.Information("Ingestion ответ: {Result}", ingestionResult);
    Log.Information("Rules Engine ответ: {Result}", rulesResult);

    // ШАГ 3: Уведомление
    if (rulesResult.Contains("true"))
    {
        sw.Restart();
        Log.Information("ШАГ 3: Отправка уведомления...");
        await httpClient.PostAsync("http://localhost:3000/alert", content);
        sw.Stop();
        Log.Information("Уведомление отправлено за {Elapsed} мс", sw.ElapsedMilliseconds);
    }

    stopwatch.Stop();
    Log.Information("=== ОБЩЕЕ ВРЕМЯ: {Elapsed} мс ===", stopwatch.ElapsedMilliseconds);
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