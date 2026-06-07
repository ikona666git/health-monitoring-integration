using System.ComponentModel.DataAnnotations;

namespace IntegrationService.Models;

public class AlertHistory
{
    [Key]
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double Value { get; set; }
    public double? MinNormal { get; set; }
    public double? MaxNormal { get; set; }
    public double? DeviationPercent { get; set; }
    public string? Message { get; set; }
    public string? Channel { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}