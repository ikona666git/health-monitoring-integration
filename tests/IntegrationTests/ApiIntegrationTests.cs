using System.Text;
using System.Text.Json;
using Xunit;

namespace IntegrationTests;

public class ApiIntegrationTests
{
    private readonly HttpClient _httpClient = new();

    [Fact]
    public async Task Ingestion_HealthCheck_ReturnsOk()
    {
        var response = await _httpClient.GetAsync($"{ServiceUrls.Ingestion}/health");
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("ok", content);
    }

    [Fact]
    public async Task RulesEngine_HealthCheck_ReturnsOk()
    {
        var response = await _httpClient.GetAsync($"{ServiceUrls.Rules}/health");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Alerting_HealthCheck_ReturnsOk()
    {
        var response = await _httpClient.GetAsync($"{ServiceUrls.Alerting}/health");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SendMeasurement_FullFlow_ReturnsAccepted()
    {
        var measurement = new
        {
            user_id = "test_user",
            metric_type = "heart_rate",
            value = 135,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ServiceUrls.Ingestion}/measurements", content);
        var result = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("accepted", result);
        Assert.Contains("rules_check", result);
        Assert.Contains("alert_triggered", result);
    }

    [Fact]
    public async Task CheckRules_OutOfRange_ReturnsDeviation()
    {
        var measurement = new
        {
            user_id = "test_user",
            metric_type = "heart_rate",
            value = 135
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ServiceUrls.Rules}/check", content);
        var result = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("is_out_of_range", result);
        Assert.Contains("deviation_percent", result);
    }
}
