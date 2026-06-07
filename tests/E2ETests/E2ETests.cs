using System.Text;
using System.Text.Json;
using Xunit;

namespace E2ETests;

public class E2ETests
{
    private readonly HttpClient _httpClient;

    public E2ETests()
    {
        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task FullE2EScenario_UserMeasurement_AlertTriggered()
    {
        // 1. Проверка здоровья модулей
        var ingestionHealth = await _httpClient.GetAsync("http://localhost:5001/health");
        var rulesHealth = await _httpClient.GetAsync("http://localhost:5002/health");
        var alertingHealth = await _httpClient.GetAsync("http://localhost:3000/health");

        Assert.True(ingestionHealth.IsSuccessStatusCode);
        Assert.True(rulesHealth.IsSuccessStatusCode);
        Assert.True(alertingHealth.IsSuccessStatusCode);

        // 2. Отправка измерения
        var measurement = new
        {
            user_id = "e2e_user",
            metric_type = "heart_rate",
            value = 135,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ingestionResponse = await _httpClient.PostAsync("http://localhost:5001/measurements", content);
        Assert.True(ingestionResponse.IsSuccessStatusCode);

        // 3. Проверка норм
        var rulesResponse = await _httpClient.PostAsync("http://localhost:5002/check", content);
        var rulesResult = await rulesResponse.Content.ReadAsStringAsync();
        Assert.Contains("is_out_of_range", rulesResult);

        // 4. Проверка уведомления
        var alertResponse = await _httpClient.PostAsync("http://localhost:3000/alert", content);
        Assert.True(alertResponse.IsSuccessStatusCode);

        // 5. Проверка истории
        var historyResponse = await _httpClient.GetAsync("http://localhost:5002/history");
        var history = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("e2e_user", history);
    }

    [Fact]
    public async Task E2E_NormalValue_NoAlert()
    {
        var measurement = new
        {
            user_id = "e2e_user2",
            metric_type = "heart_rate",
            value = 75,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var rulesResponse = await _httpClient.PostAsync("http://localhost:5002/check", content);
        var rulesResult = await rulesResponse.Content.ReadAsStringAsync();

        Assert.Contains("is_out_of_range", rulesResult);
    }
}