namespace IntegrationService.Models;

public class Measurement
{
    public int? Id { get; set; }
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double Value { get; set; }
    public required string Timestamp { get; set; }
    public string Source { get; set; } = "sensor";
}