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
    public DbSet<ReportDefinition> ReportDefinitions { get; set; }
    public DbSet<JobInstance> JobInstances { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ProjectSnapshot> ProjectSnapshots { get; set; }

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

        // Snapshot -> Project
        modelBuilder.Entity<ProjectSnapshot>()
            .HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectSnapshot>()
            .HasIndex(s => new { s.ProjectId, s.Version })
            .IsUnique();
    }
}
