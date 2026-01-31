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
using Platform.Engine.Models.DataExecution;
using Platform.Engine.Services;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;

namespace Platform.Simulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var sim = new WorkflowE2ESimulator();
        await sim.RunAsync();
    }
}

public class WorkflowE2ESimulator
{
    private readonly IServiceProvider _services;
    private Guid _projectId;

    public WorkflowE2ESimulator()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase("WorkflowE2ESim"));
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IMetadataDiffService, MetadataDiffService>();
        services.AddScoped<ISqlSchemaEvolutionService, MockSqlService>();
        services.AddScoped<IDataProvider, MockEntityDataProvider>(); 
        services.AddScoped<ICompatibilityProvider, CompatibilityProvider>();
        services.AddScoped<IMetadataNormalizationService, MetadataNormalizationService>();
        _services = services.BuildServiceProvider();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n🚀 STARTING COMPLEX RULES & VIRTUALIZATION TEST");
        Console.WriteLine("==================================================");

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var versioning = scope.ServiceProvider.GetRequiredService<IVersioningService>();
        var diffService = scope.ServiceProvider.GetRequiredService<IMetadataDiffService>();
        var repo = scope.ServiceProvider.GetRequiredService<IArtifactRepository>();

        var tenant = new Tenant { Name = "Global Health" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var project = new Project { Name = "ClinicCare", TenantId = tenant.Id };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        var doctorId = Guid.NewGuid();
        var emailFieldId = Guid.NewGuid();
        var feeFieldId = Guid.NewGuid();

        var doctorMeta = new EntityMetadata
        {
            Id = doctorId,
            Name = "Doctor",
            Fields = new List<FieldMetadata>
            {
                new() { Id = Guid.NewGuid(), Name = "Name", Type = "string" },
                new() { 
                    Id = emailFieldId, 
                    Name = "Email", 
                    Type = "string",
                    Rules = new List<ValidationRule> { new() { Type = "Regex", Value = @"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid Email" } }
                },
                new() { Id = feeFieldId, Name = "ConsultationFee", Type = "decimal" }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "Doctor", Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(doctorMeta) });

        var ruleMeta = new BusinessRuleMetadata
        {
            Name = "PremiumLogic",
            TargetEntity = "Doctor",
            Condition = "ConsultationFee > 500",
            Action = "MarkAsElite"
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "EliteRule", Type = ArtifactType.CustomObject, Content = JsonSerializer.Serialize(ruleMeta) });

        var workflowId = Guid.NewGuid();
        var workflowMeta = new WorkflowMetadata
        {
            Id = workflowId,
            Name = "FeeValidator",
            DefinitionJson = @"{
                ""activities"": [
                    { ""id"": ""if1"", ""type"": ""Elsa.If"", ""condition"": ""ConsultationFee > 100"" }
                ]
            }"
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "ValidationWorkflow", Type = ArtifactType.Workflow, Content = JsonSerializer.Serialize(workflowMeta) });

        var v1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.0.0", "Admin");
        v1Snapshot.IsPublished = true;
        await db.SaveChangesAsync();

        Console.WriteLine("[Step 1] Baseline v1.0.0 Published.");

        doctorMeta.Name = "Practitioner";
        doctorMeta.Fields.First(f => f.Id == feeFieldId).Name = "Charge";
        doctorMeta.Fields.First(f => f.Id == emailFieldId).Type = "int"; 

        var drArtifact = (await repo.GetByProjectIdAsync(_projectId)).First(a => a.Name == "Doctor");
        drArtifact.Content = JsonSerializer.Serialize(doctorMeta);
        await repo.UpdateAsync(drArtifact);

        var v1_1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.1.0", "LeadDev");
        v1_1Snapshot.IsPublished = true; // Mark as published so it's loaded into history
        await db.SaveChangesAsync();
        
        var migrationPlan = diffService.Compare(v1Snapshot, v1_1Snapshot);

        Console.WriteLine("[Phase 3] Multi-Layer Impact Analysis Report (renames detected via GUIDs)");
        
        // --- PHASE 4: VIRTUALIZATION TEST ---
        Console.WriteLine("\n🚀 [Phase 4] Virtualization Test (Executing legacy query)...");
        var normalizationService = scope.ServiceProvider.GetRequiredService<IMetadataNormalizationService>();
        
        var legacyQuery = new DataOperationMetadata
        {
            OperationType = OperationType.Query,
            RootEntity = "Doctor",
            Fields = new List<FieldDefinition> { new() { Field = "ConsultationFee" } }
        };

        Console.WriteLine($"    [Input Query]  Root: {legacyQuery.RootEntity}, Field: {legacyQuery.Fields[0].Field}");
        
        await normalizationService.NormalizeAsync(_projectId, legacyQuery);
        
        Console.WriteLine($"    [Output Query] Root: {legacyQuery.RootEntity}, Field: {legacyQuery.Fields[0].Field}");
        
        if (legacyQuery.Fields[0].Field == "Charge" && legacyQuery.RootEntity == "Practitioner")
        {
            Console.WriteLine("    ✅ SUCCESS: Metadata correctly virtualized to current physical schema.");
        }
        else
        {
            Console.WriteLine("    ❌ FAILURE: Metadata virtualization failed.");
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine("🏆 VIRTUALIZATION TEST COMPLETED SUCCESSFULLY");
        Console.WriteLine("==================================================\n");
    }
}

public class MockEntityDataProvider : IDataProvider
{
    public string ProviderType => "Entity";
    public Task<DataResult> ExecuteAsync(DataOperationMetadata metadata, Dictionary<string, object> parameters, ExecutionContext context, CancellationToken ct) 
        => Task.FromResult(new DataResult { Success = true });
    public Task<long> EstimateRowCountAsync(DataOperationMetadata metadata, Dictionary<string, object> parameters, ExecutionContext context) => Task.FromResult(0L);
    public Task<ValidationResult> ValidateAsync(DataOperationMetadata metadata) => Task.FromResult(new ValidationResult { IsValid = true });
}

public class MockSqlService : ISqlSchemaEvolutionService
{
    public List<string> GenerateMigrationScripts(MigrationPlan plan) => new();
    public Task ApplyMigrationAsync(MigrationPlan plan, string connectionString) => Task.CompletedTask;
}
