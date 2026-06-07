using System.Text;
using System.Text.Json;
using AutoMapper;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IntegrationService.Services;

public interface IHealthIntegrationService
{
    Task<Measurement> ProcessMeasurementAsync(Measurement measurement);
    Task<IEnumerable<CheckResult>> GetHistoryAsync(string? userId = null);
    Task<object> UpdateNormsAsync(string userId, string metric, double minVal, double maxVal);
}

public class HealthIntegrationService : IHealthIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IMapper _mapper;
    private readonly ILogger<HealthIntegrationService> _logger;
    private readonly string _ingestionUrl;
    private readonly string _rulesUrl;
    private readonly string _alertingUrl;

    public HealthIntegrationService(
        HttpClient httpClient,
        IMapper mapper,
        ILogger<HealthIntegrationService> logger)
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _logger = logger;

        _ingestionUrl = Environment.GetEnvironmentVariable("INGESTION_URL") ?? "http://127.0.0.1:5001";
        _rulesUrl = Environment.GetEnvironmentVariable("RULES_URL") ?? "http://127.0.0.1:5002";
        _alertingUrl = Environment.GetEnvironmentVariable("ALERTING_URL") ?? "http://127.0.0.1:3000";
    }

    public async Task<Measurement> ProcessMeasurementAsync(Measurement measurement)
    {
        Log.Information("  砫    ࠡ ⪨: {UserId}, {MetricType}, {Value}",
            measurement.UserId, measurement.MetricType, measurement.Value);

        var ingestionResult = await CallIngestionAsync(measurement);
        Log.Information("Ingestion  ⢥⨫: {@IngestionResult}", ingestionResult);

        var checkResult = await CallRulesEngineAsync(measurement);
        Log.Information("Rules Engine  ⢥⨫: {@CheckResult}", checkResult);

        if (checkResult.IsOutOfRange)
        {
            var alertRequest = _mapper.Map<AlertRequest>(checkResult);
            alertRequest.UserId = measurement.UserId;
            alertRequest.MetricType = measurement.MetricType;
            alertRequest.Value = measurement.Value;

            await CallAlertingAsync(alertRequest);
            Log.Warning("  ࠢ     㢥             ⪫      : {DeviationPercent}%", checkResult.DeviationPercent);
        }

        Log.Information("  ࠡ ⪠      襭 . AlertTriggered: {AlertTriggered}", checkResult.AlertTriggered);
        return measurement;
    }

    private async Task<JsonElement> CallIngestionAsync(Measurement measurement)
    {
        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_ingestionUrl}/measurements", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(responseContent);
    }

    private async Task<CheckResult> CallRulesEngineAsync(Measurement measurement)
    {
        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_rulesUrl}/check", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CheckResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? throw new Exception("Failed to parse Rules Engine response");
    }

    private async Task CallAlertingAsync(AlertRequest alert)
    {
        var json = JsonSerializer.Serialize(alert);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_alertingUrl}/alert", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<CheckResult>> GetHistoryAsync(string? userId = null)
    {
        var url = $"{_rulesUrl}/history";
        if (!string.IsNullOrEmpty(userId))
            url += $"?user_id={userId}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<CheckResult>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<CheckResult>();
    }

    public async Task<object> UpdateNormsAsync(string userId, string metric, double minVal, double maxVal)
    {
        var url = $"{_rulesUrl}/norms/{userId}?metric={metric}&min_val={minVal}&max_val={maxVal}";
        var response = await _httpClient.PutAsync(url, null);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<object>(content) ?? new { };
    }
}