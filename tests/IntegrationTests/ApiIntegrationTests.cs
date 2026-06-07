using System.Text;
using System.Text.Json;
using Xunit;

namespace IntegrationTests;

public class ApiIntegrationTests
{
    private readonly HttpClient _httpClient;

    public ApiIntegrationTests()
    {
        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task Ingestion_HealthCheck_ReturnsOk()
    {
        // Arrange
        var url = "http://localhost:5001/health";

        // Act
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("ok", content);
    }

    [Fact]
    public async Task RulesEngine_HealthCheck_ReturnsOk()
    {
        var url = "http://localhost:5002/health";
        var response = await _httpClient.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Alerting_HealthCheck_ReturnsOk()
    {
        var url = "http://localhost:3000/health";
        var response = await _httpClient.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SendMeasurement_FullFlow_ReturnsAccepted()
    {
        // Arrange
        var measurement = new
        {
            user_id = "test_user",
            metric_type = "heart_rate",
            value = 135,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync("http://localhost:5001/measurements", content);
        var result = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("accepted", result);
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

        var response = await _httpClient.PostAsync("http://localhost:5002/check", content);
        var result = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("is_out_of_range", result);
        Assert.Contains("deviation_percent", result);
    }
}