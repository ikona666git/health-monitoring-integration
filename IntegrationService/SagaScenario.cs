using System.Text;
using System.Text.Json;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IntegrationService;

public class SagaScenario
{
    private readonly HttpClient _httpClient;
    private readonly string _ingestionUrl = "http://localhost:5001";
    private readonly string _rulesUrl = "http://localhost:5002";
    private readonly string _alertingUrl = "http://localhost:3000";

    public SagaScenario(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task RunAsync()
    {
        Log.Information("=== SAGA СЦЕНАРИЙ СТАРТ ===");

        var measurement = new
        {
            user_id = "user123",
            metric_type = "heart_rate",
            value = 135,
            timestamp = DateTime.UtcNow.ToString("o"),
            source = "saga_test"
        };

        // Шаг 1: Отправка в Ingestion
        var ingestionJson = JsonSerializer.Serialize(measurement);
        var ingestionContent = new StringContent(ingestionJson, Encoding.UTF8, "application/json");
        var ingestionResponse = await _httpClient.PostAsync($"{_ingestionUrl}/measurements", ingestionContent);
        var ingestionResult = await ingestionResponse.Content.ReadAsStringAsync();
        Log.Information("ШАГ 1 Ingestion: {0}", ingestionResult);

        // Шаг 2: Проверка норм в Rules
        var rulesContent = new StringContent(ingestionJson, Encoding.UTF8, "application/json");
        var rulesResponse = await _httpClient.PostAsync($"{_rulesUrl}/check", rulesContent);
        var rulesResult = await rulesResponse.Content.ReadAsStringAsync();
        Log.Information("ШАГ 2 Rules Engine: {0}", rulesResult);

        // Шаг 3: Уведомление (если нужно)
        if (rulesResult.Contains("true"))
        {
            var alertContent = new StringContent(rulesResult, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_alertingUrl}/alert", alertContent);
            Log.Warning("ШАГ 3 Уведомление отправлено!");
        }

        Log.Information("=== SAGA СЦЕНАРИЙ ФИНИШ ===");
    }
}