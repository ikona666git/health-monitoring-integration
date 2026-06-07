using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IntegrationService.Models;

namespace IntegrationService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<UserNorm> UserNorms { get; set; }
    public DbSet<AlertHistory> AlertHistories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique();
        
        modelBuilder.Entity<UserNorm>()
            .HasIndex(un => new { un.UserId, un.MetricType })
            .IsUnique();
        
        modelBuilder.Entity<Measurement>()
            .HasIndex(m => new { m.UserId, m.MetricType });
        
        modelBuilder.Entity<AlertHistory>()
            .HasIndex(a => new { a.UserId, a.SentAt });
    }
}