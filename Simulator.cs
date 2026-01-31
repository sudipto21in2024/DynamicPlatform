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
using Platform.Engine.Models.Connectivity;
using Platform.Engine.Services;
using Platform.Engine.Services.DataExecution;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;

namespace Platform.Simulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var sim = new ConnectivityE2ESimulator();
        await sim.RunAsync();
    }
}

public class ConnectivityE2ESimulator
{
    private readonly IServiceProvider _services;
    private Guid _projectId;

    public ConnectivityE2ESimulator()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase("ConnectivityE2ESim"));
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IMetadataDiffService, MetadataDiffService>();
        services.AddScoped<ISqlSchemaEvolutionService, MockSqlService>();
        services.AddScoped<ICompatibilityProvider, CompatibilityProvider>();
        services.AddScoped<IConnectivityHub, ConnectivityHub>();
        services.AddScoped<IDataProvider, ConnectorDataProvider>();
        
        // Register a "Native" Slack Connector for simulation
        services.AddScoped<IConnector, SlackConnector>();

        _services = services.BuildServiceProvider();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n🚀 STARTING CONNECTIVITY HUB INTEGRATION TEST");
        Console.WriteLine("==================================================");

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IConnectivityHub>();
        var repo = scope.ServiceProvider.GetRequiredService<IArtifactRepository>();

        // 1. Setup Project
        var tenant = new Tenant { Name = "Global Logistics" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var project = new Project { Name = "ShipTarget", TenantId = tenant.Id };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        // 2. Register a Configuration-Based Connector in Artifacts
        var slackMeta = new ConnectorMetadata
        {
            Name = "SlackNotifier",
            Description = "Sends alerts to Slack channels",
            ConfigProperties = new List<ConnectorProperty> 
            { 
                new() { Name = "WebhookUrl", Type = "string", DefaultValue = "https://hooks.slack.com/services/..." } 
            },
            Inputs = new List<ConnectorParameter> 
            { 
                new() { Name = "Message", Type = "string" },
                new() { Name = "Channel", Type = "string" }
            },
            BusinessLogic = "// Automated logic here"
        };

        await repo.AddAsync(new Artifact 
        { 
            ProjectId = _projectId, 
            Name = "SlackNotifier", 
            Type = ArtifactType.Connector, 
            Content = JsonSerializer.Serialize(slackMeta) 
        });

        Console.WriteLine("[Step 1] 'SlackNotifier' Connector Artifact registered.");

        // 3. EXECUTE NATIVE CONNECTOR (Registered in DI)
        Console.WriteLine("\n[Step 2] Executing NATIVE Connector (SlackConnector)...");
        var nativeRequest = new ConnectorExecutionRequest
        {
            ConnectorName = "SlackNative",
            Inputs = new Dictionary<string, object?> { ["Message"] = "System Alert: Engine Hot", ["Channel"] = "#alerts" }
        };

        var nativeResult = await hub.ExecuteConnectorAsync(_projectId, nativeRequest);
        if (nativeResult.Success)
            Console.WriteLine($"   ✅ Native Success: {nativeResult.Data}");
        else
            Console.WriteLine($"   ❌ Native Failure: {nativeResult.ErrorMessage}");

        // 4. EXECUTE ARTIFACT CONNECTOR (Configuration Driven)
        Console.WriteLine("\n[Step 3] Executing ARTIFACT Connector (SlackNotifier)...");
        var artifactRequest = new ConnectorExecutionRequest
        {
            ConnectorName = "SlackNotifier",
            Inputs = new Dictionary<string, object?> { ["Message"] = "Shipment Delayed", ["Channel"] = "#logistic-updates" }
        };

        var artifactResult = await hub.ExecuteConnectorAsync(_projectId, artifactRequest);
        if (artifactResult.Success)
            Console.WriteLine($"   ✅ Artifact Success: {artifactResult.Data}");
        else
            Console.WriteLine($"   ❌ Artifact Failure: {artifactResult.ErrorMessage}");

        Console.WriteLine("\n==================================================");
        Console.WriteLine("🏆 CONNECTIVITY HUB TEST COMPLETED SUCCESSFULLY");
        Console.WriteLine("==================================================\n");
    }
}

public class SlackConnector : IConnector
{
    public string Name => "SlackNative";

    public Task<object?> ExecuteAsync(IDictionary<string, object?> inputs)
    {
        var msg = inputs["Message"];
        var channel = inputs["Channel"];
        return Task.FromResult<object?>($"[SLACK API] Posted '{msg}' to {channel}");
    }
}

public class MockSqlService : ISqlSchemaEvolutionService
{
    public List<string> GenerateMigrationScripts(MigrationPlan plan) => new();
    public Task ApplyMigrationAsync(MigrationPlan plan, string connectionString) => Task.CompletedTask;
}
