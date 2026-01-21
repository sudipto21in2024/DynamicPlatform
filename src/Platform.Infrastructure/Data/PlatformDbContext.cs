using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;

namespace Platform.Infrastructure.Data;

public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Artifact> Artifacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project -> Tenant
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Tenant)
            .WithMany(t => t.Projects)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Artifact -> Project
        modelBuilder.Entity<Artifact>()
            .HasOne(a => a.Project)
            .WithMany(p => p.Artifacts)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Artifact>()
            .HasIndex(a => new { a.ProjectId, a.Name })
            .IsUnique();
    }
}
