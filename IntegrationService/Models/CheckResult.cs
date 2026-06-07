namespace IntegrationService.Models;

public class CheckResult
{
    public int? MeasurementId { get; set; }
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double Value { get; set; }
    public double? MinNormal { get; set; }
    public double? MaxNormal { get; set; }
    public bool IsOutOfRange { get; set; }
    public double? DeviationPercent { get; set; }
    public bool AlertTriggered { get; set; }
}