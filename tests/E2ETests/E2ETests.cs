using System.Text;
using System.Text.Json;
using Xunit;

namespace E2ETests;

public class E2ETests
{
    private readonly HttpClient _httpClient = new();

    [Fact]
    public async Task FullE2EScenario_UserMeasurement_AlertTriggered()
    {
        var ingestionHealth = await _httpClient.GetAsync($"{ServiceUrls.Ingestion}/health");
        var rulesHealth = await _httpClient.GetAsync($"{ServiceUrls.Rules}/health");
        var alertingHealth = await _httpClient.GetAsync($"{ServiceUrls.Alerting}/health");

        Assert.True(ingestionHealth.IsSuccessStatusCode);
        Assert.True(rulesHealth.IsSuccessStatusCode);
        Assert.True(alertingHealth.IsSuccessStatusCode);

        var measurement = new
        {
            user_id = "e2e_user",
            metric_type = "heart_rate",
            value = 135,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var ingestionResponse = await _httpClient.PostAsync($"{ServiceUrls.Ingestion}/measurements", content);
        var ingestionResult = await ingestionResponse.Content.ReadAsStringAsync();

        Assert.True(ingestionResponse.IsSuccessStatusCode);
        Assert.Contains("accepted", ingestionResult);
        Assert.Contains("rules_check", ingestionResult);
        Assert.Contains("alert_triggered", ingestionResult);
        Assert.Contains("alert_id", ingestionResult);

        var historyResponse = await _httpClient.GetAsync($"{ServiceUrls.Rules}/history");
        var history = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("e2e_user", history);

        var alertsResponse = await _httpClient.GetAsync($"{ServiceUrls.Alerting}/alerts");
        var alerts = await alertsResponse.Content.ReadAsStringAsync();
        Assert.Contains("e2e_user", alerts);
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

        var ingestionResponse = await _httpClient.PostAsync($"{ServiceUrls.Ingestion}/measurements", content);
        var result = await ingestionResponse.Content.ReadAsStringAsync();

        Assert.True(ingestionResponse.IsSuccessStatusCode);
        Assert.Contains("accepted", result);

        using var doc = JsonDocument.Parse(result);
        var rulesCheck = doc.RootElement.GetProperty("rules_check");
        Assert.False(rulesCheck.GetProperty("is_out_of_range").GetBoolean());
        Assert.False(rulesCheck.GetProperty("alert_triggered").GetBoolean());
    }
}
