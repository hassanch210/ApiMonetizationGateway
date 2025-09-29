using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Models;

namespace ApiMonetizationGateway.Shared.Data;

public class ApiMonetizationContext : DbContext
{
    public ApiMonetizationContext(DbContextOptions<ApiMonetizationContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tier> Tiers { get; set; }
    public DbSet<ApiUsage> ApiUsages { get; set; }
    public DbSet<MonthlyUsageSummary> MonthlyUsageSummaries { get; set; }
    public DbSet<UserTier> UserTiers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ApiKey).IsUnique();
            
            entity.HasOne(e => e.Tier)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Tier configuration
        modelBuilder.Entity<Tier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.Property(e => e.MonthlyPrice)
                .HasColumnType("decimal(18,2)");
        });

        // ApiUsage configuration
        modelBuilder.Entity<ApiUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RequestTimestamp });
            entity.HasIndex(e => e.RequestTimestamp);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.ApiUsages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonthlyUsageSummary configuration
        modelBuilder.Entity<MonthlyUsageSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Year, e.Month }).IsUnique();
            
            entity.Property(e => e.CalculatedCost)
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.TierPrice)
                .HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.MonthlyUsageSummaries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserTier configuration
        modelBuilder.Entity<UserTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.TierId, e.IsActive });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserTiers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Tier)
                .WithMany()
                .HasForeignKey(e => e.TierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed default tiers
        modelBuilder.Entity<Tier>().HasData(
            new Tier
            {
                Id = 1,
                Name = "Free",
                Description = "Free tier with basic limits",
                MonthlyQuota = 100,
                RateLimit = 2,
                MonthlyPrice = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tier
            {
                Id = 2,
                Name = "Pro",
                Description = "Professional tier with higher limits",
                MonthlyQuota = 100000,
                RateLimit = 10,
                MonthlyPrice = 50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}