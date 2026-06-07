using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntegrationService.Models;

public class UserNorm
{
    [Key]
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string MetricType { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("User")]
    public int? UserDbId { get; set; }
    public User? User { get; set; }
}