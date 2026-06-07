namespace IntegrationService.Models;

public class AlertRequest
{
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double Value { get; set; }
    public double? MinNormal { get; set; }
    public double? MaxNormal { get; set; }
    public double? DeviationPercent { get; set; }
    public string? AlertChannel { get; set; } = "console";
}