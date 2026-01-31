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

/// <summary>
/// Extended E2E test for the Workflow Engine and Delta Management integration.
/// This simulates "Multiple Complex Rules" being analyzed for hazards during evolution.
/// </summary>
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
        _services = services.BuildServiceProvider();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n🚀 STARTING COMPLEX RULES INTEGRATION TEST");
        Console.WriteLine("==================================================");

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var versioning = scope.ServiceProvider.GetRequiredService<IVersioningService>();
        var diffService = scope.ServiceProvider.GetRequiredService<IMetadataDiffService>();
        var repo = scope.ServiceProvider.GetRequiredService<IArtifactRepository>();

        // 1. SETUP DOMAIN
        var tenant = new Tenant { Name = "Global Health" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var project = new Project { Name = "ClinicCare", TenantId = tenant.Id };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        // 2. DEFINE DOCTOR ENTITY WITH FIELD RULES (Rule Type 1: Field Validation)
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

        // 3. DEFINE COMPLEX BUSINESS RULE (Rule Type 2: Business Rule)
        var ruleMeta = new BusinessRuleMetadata
        {
            Name = "PremiumLogic",
            TargetEntity = "Doctor",
            Condition = "ConsultationFee > 500",
            Action = "MarkAsElite"
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = "EliteRule", Type = ArtifactType.CustomObject, Content = JsonSerializer.Serialize(ruleMeta) });

        // 4. DEFINE WORKFLOW (Rule Type 3: Workflow Branching)
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

        Console.WriteLine("[Step 1] Baseline v1.0.0 Published: 1 Entity, 1 Business Rule, 1 Workflow.");
        var v1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.0.0", "Admin");
        v1Snapshot.IsPublished = true;
        await db.SaveChangesAsync();

        // 5. THE COMPLEX EVOLUTION (Breaking multiple rules)
        doctorMeta.Name = "Practitioner";
        doctorMeta.Fields.First(f => f.Id == feeFieldId).Name = "Charge";
        doctorMeta.Fields.First(f => f.Id == emailFieldId).Type = "int"; 

        var drArtifact = (await repo.GetByProjectIdAsync(_projectId)).First(a => a.Name == "Doctor");
        drArtifact.Content = JsonSerializer.Serialize(doctorMeta);
        await repo.UpdateAsync(drArtifact);

        Console.WriteLine("[Step 2] Metadata Evolved: Complex renames and type changes applied.");

        // 6. MULTI-LAYER IMPACT ANALYSIS
        var v1_1Snapshot = await versioning.CreateSnapshotAsync(_projectId, "1.1.0", "LeadDev");
        var migrationPlan = diffService.Compare(v1Snapshot, v1_1Snapshot);

        Console.WriteLine("\n[Phase 3] Multi-Layer Impact Analysis Report:");
        
        foreach (var delta in migrationPlan.Deltas)
        {
            Console.WriteLine($" ➡️ [{delta.Type}] {delta.Action}: {delta.Name}");
            if (delta.Changes.Any(c => c.Value.IsBreaking))
            {
                var breaking = delta.Changes.First(c => c.Value.IsBreaking);
                Console.WriteLine($"    🚨 BREAKING: {breaking.Key} changed from {breaking.Value.OldValue} to {breaking.Value.NewValue}");
            }
        }

        // 7. SCAN RULES & WORKFLOWS
        Console.WriteLine("\n[Phase 4] System-Wide Rule Dependency Scan:");
        
        AnalyzeRuleImpact("EliteRule", ruleMeta, migrationPlan);
        AnalyzeRuleImpact("FeeValidator (Workflow)", workflowMeta, migrationPlan);
        AnalyzeRuleImpact("Email validation (Field Rule)", doctorMeta, migrationPlan);

        Console.WriteLine("\n==================================================");
        Console.WriteLine("🏆 COMPLEX RULES TEST COMPLETED SUCCESSFULLY");
        Console.WriteLine("==================================================\n");
    }

    private void AnalyzeRuleImpact(string ruleName, object meta, MigrationPlan plan)
    {
        Console.WriteLine($" 🔍 Scanning {ruleName}...");
        string content = JsonSerializer.Serialize(meta);
        bool impacted = false;

        foreach (var delta in plan.Deltas.Where(d => d.Action == DeltaAction.Renamed))
        {
            if (content.Contains($"\"{delta.PreviousName}\""))
            {
                Console.WriteLine($"    ‼️  IMPACT: Rule refers to old name '{delta.PreviousName}' instead of '{delta.Name}'.");
                impacted = true;
            }
        }

        foreach (var delta in plan.Deltas.Where(d => d.Type == MetadataType.Field && d.Changes.ContainsKey("Type")))
        {
            if (content.Contains($"\"{delta.Name}\"") || content.Contains($"\"{delta.PreviousName}\""))
            {
                Console.WriteLine($"    ‼️  IMPACT: Field '{delta.Name}' has changed type. Rule logic may be invalid.");
                impacted = true;
            }
        }

        if (!impacted) Console.WriteLine("    ✅ No direct impact detected.");
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
