using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;
using Platform.Engine.Models;
using Platform.Engine.Models.Delta;
using Platform.Engine.Services;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;

namespace Platform.Simulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var sim = new ComplexClinicEvolutionSimulator();
        await sim.RunAsync();
    }
}

public class ComplexClinicEvolutionSimulator
{
    private readonly IServiceProvider _services;
    private Guid _projectId;

    public ComplexClinicEvolutionSimulator()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase("ComplexClinicSim"));
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IMetadataDiffService, MetadataDiffService>();
        services.AddScoped<ISqlSchemaEvolutionService, MockSqlService>();
        _services = services.BuildServiceProvider();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n🚀 STARTING COMPLEX E2E SIMULATION: Clinic System Evolution");
        Console.WriteLine("============================================================");

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var versioning = scope.ServiceProvider.GetRequiredService<IVersioningService>();
        var diffService = scope.ServiceProvider.GetRequiredService<IMetadataDiffService>();
        var sqlService = scope.ServiceProvider.GetRequiredService<ISqlSchemaEvolutionService>();
        var repo = scope.ServiceProvider.GetRequiredService<IArtifactRepository>();

        var tenant = new Tenant { Name = "Global Health Labs" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var project = new Project { Name = "MedCloud", TenantId = tenant.Id };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        var doctorId = Guid.NewGuid();
        var doctorMeta = new EntityMetadata
        {
            Id = doctorId,
            Name = "Doctor",
            Fields = new List<FieldMetadata>
            {
                new() { Id = Guid.NewGuid(), Name = "Name", Type = "string" },
                new() { Id = Guid.NewGuid(), Name = "ConsultationFee", Type = "decimal" }
            }
        };

        var patientId = Guid.NewGuid();
        var patientMeta = new EntityMetadata
        {
            Id = patientId,
            Name = "Patient",
            Fields = new List<FieldMetadata>
            {
                new() { Id = Guid.NewGuid(), Name = "FullName", Type = "string" }
            }
        };

        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "Doctor", Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(doctorMeta) });
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "Patient", Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(patientMeta) });

        Console.WriteLine("\n[Phase 1] Metadata v1.0.0 Created (Doctor, Patient)");

        var v1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.0.0", "Admin");
        var v1Plan = diffService.Compare(null, v1Snapshot);
        await sqlService.ApplyMigrationAsync(v1Plan, "InMemoryConn");
        v1Snapshot.IsPublished = true;
        await db.SaveChangesAsync();

        Console.WriteLine("\n[Phase 2] Published v1.0.0: Tables Created.");

        var doctorFieldFee = doctorMeta.Fields.First(f => f.Name == "ConsultationFee");
        doctorFieldFee.Name = "BookingFee"; 

        doctorMeta.Fields.Add(new FieldMetadata { Id = Guid.NewGuid(), Name = "LicenseNumber", Type = "string", IsRequired = true });
        
        doctorMeta.Relations.Add(new RelationMetadata 
        { 
            Id = Guid.NewGuid(), 
            TargetEntity = "Patient", 
            Type = RelationType.ManyToOne, 
            NavPropName = "DefaultClinic" 
        });

        var doctorArtifact = (await repo.GetByProjectIdAsync(_projectId)).First(a => a.Name == "Doctor");
        doctorArtifact.Content = JsonSerializer.Serialize(doctorMeta);
        await repo.UpdateAsync(doctorArtifact);

        var v1_1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.1.0", "LeadDev");
        var migrationPlan = diffService.Compare(v1Snapshot, v1_1Snapshot);

        Console.WriteLine("\n[Phase 3] Delta Detection (v1.0.0 -> v1.1.0):");
        foreach (var delta in migrationPlan.Deltas)
        {
            Console.WriteLine($" ➡️ [{delta.Type}] {delta.Action}: {delta.Name} " + 
                (delta.Action == DeltaAction.Renamed ? $"(Was: {delta.PreviousName})" : ""));
            
            foreach (var change in delta.Changes)
            {
                Console.WriteLine($"    - {change.Key}: {change.Value.OldValue ?? "NULL"} -> {change.Value.NewValue}");
                if (change.Value.IsBreaking) Console.WriteLine("    ⚠️  CRITICAL: Breaking change!");
            }
        }

        await sqlService.ApplyMigrationAsync(migrationPlan, "InMemoryConn");
        
        Console.WriteLine("\n[Phase 4] SQL Evolution Successful.");
        Console.WriteLine("✅ SIMULATION COMPLETE.");
    }
}

public class MockSqlService : ISqlSchemaEvolutionService
{
    public List<string> GenerateMigrationScripts(MigrationPlan plan) => new List<string>();
    public Task ApplyMigrationAsync(MigrationPlan plan, string connectionString)
    {
        Console.WriteLine("\n[SQL ENGINE] Executing generated DDL statements:");
        foreach (var d in plan.Deltas)
        {
            if (d.Action == DeltaAction.Renamed) 
                Console.WriteLine($"   ALTER TABLE \"{GetTableName(d, plan)}\" RENAME COLUMN \"{d.PreviousName}\" TO \"{d.Name}\";");
            if (d.Action == DeltaAction.Added && d.Type == MetadataType.Field)
                Console.WriteLine($"   ALTER TABLE \"{GetTableName(d, plan)}\" ADD COLUMN \"{d.Name}\" {MapToSqlType(d)};");
        }
        return Task.CompletedTask;
    }

    private string GetTableName(MigrationDelta delta, MigrationPlan plan) => "Doctor";
    private string MapToSqlType(MigrationDelta delta) => "TEXT";
}
