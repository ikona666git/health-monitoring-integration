using System.ComponentModel.DataAnnotations;

namespace IntegrationService.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public required string UserId { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<UserNorm> UserNorms { get; set; } = new List<UserNorm>();
}