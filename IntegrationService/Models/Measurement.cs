using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntegrationService.Models;

public class Measurement
{
    [Key]
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double Value { get; set; }
    public string? Timestamp { get; set; }
    public string? Source { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsOutOfRange { get; set; }
    public double? DeviationPercent { get; set; }
    public bool AlertSent { get; set; }
    
    [ForeignKey("User")]
    public int? UserDbId { get; set; }
    public User? User { get; set; }
}