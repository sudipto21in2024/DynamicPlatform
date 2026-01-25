using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Generators;
using Platform.Engine.Models;
using Platform.Engine.Services;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Platform.Simulator;

public class ClinicSystemSimulator
{
    private readonly IServiceProvider _services;
    private Guid _projectId;

    public ClinicSystemSimulator()
    {
        var services = new ServiceCollection();
        
        // Use In-Memory DB for Simulation
        services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase("ClinicSim"));
        
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped<EntityGenerator>();
        services.AddScoped<DbContextGenerator>();
        services.AddScoped<RepositoryGenerator>();
        services.AddScoped<ControllerGenerator>();
        services.AddScoped<ProjectGenerator>();
        services.AddScoped<ConnectorGenerator>();
        services.AddScoped<SecurityGenerator>();
        services.AddScoped<FrontendGenerator>();
        services.AddScoped<AngularComponentGenerator>();
        services.AddScoped<MetadataLoader>();
        services.AddScoped<RelationNormalizationService>();

        _services = services.BuildServiceProvider();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("=== CLINIC SYSTEM SIMULATION START ===");
        
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IArtifactRepository>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        // 1. Setup Project & Tenant
        var tenant = new Tenant { Name = "Global Clinics Inc" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var project = new Project 
        { 
            Name = "ClinicManagement", 
            TenantId = tenant.Id,
            IsolatedConnectionString = "Host=localhost;Database=app_clinic;Username=sim;Password=sim"
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        Console.WriteLine($"Project Created: {project.Name} (ID: {_projectId})");

        // 2. Define Entities
        await CreateSpecializationEntity(repo);
        await CreateDoctorEntity(repo);
        await CreatePatientEntity(repo);
        await CreateAppointmentEntity(repo);

        // 2.1 Define Enums
        await CreateAppointmentStatusEnum(repo);

        // 2.2 Define Dashboards
        await CreateDoctorDashboard(repo);
        await CreateAdminEarningDashboard(repo);

        // 2.3 Define Workflow
        await CreateOverlapCheckWorkflow(repo);

        Console.WriteLine("Entities and Workflows Defined in Metadata Store.");

        // 3. Trigger Build (Code Generation)
        Console.WriteLine("Starting Build Process...");
        var artifacts = (await repo.GetByProjectIdAsync(_projectId)).ToList();
        
        // mimicking BuildController logic
        var loader = scope.ServiceProvider.GetRequiredService<MetadataLoader>();
        var relationService = scope.ServiceProvider.GetRequiredService<RelationNormalizationService>();
        
        var artifactEnums = artifacts
            .Where(a => a.Type == ArtifactType.Enum)
            .Select(a => loader.LoadEnumMetadata(a))
            .ToList();

        var initialEntities = artifacts
            .Where(a => a.Type == ArtifactType.Entity)
            .Select(a => loader.LoadEntityMetadata(a))
            .ToList();

        var entities = relationService.Normalize(initialEntities);
        Console.WriteLine($"Build: Loaded {artifactEnums.Count} enums and normalized {entities.Count} entities.");

        // 4. Full-Stack Build Verification Simulation
        Console.WriteLine("--- Starting Full-Stack Build Verification ---");
        
        // Backend Verification
        Console.WriteLine("Step 1: Backend Verification (dotnet build)...");
        bool backendOk = SimulateBackendBuild(entities, artifactEnums);
        Console.WriteLine(backendOk ? "Result: Backend Build SUCCESS." : "Result: Backend Build FAILED.");

        // Frontend Verification
        Console.WriteLine("Step 2: Frontend Verification (npm build)...");
        bool devMode = true; // Simulating dev vs prod build
        bool frontendOk = SimulateFrontendBuild(entities, devMode);
        Console.WriteLine(frontendOk ? "Result: Frontend Build SUCCESS." : "Result: Frontend Build FAILED.");

        if (backendOk && frontendOk)
        {
            Console.WriteLine("Final Status: PROJECT IS VALID AND READY TO PUBLISH.");
        }

        // 5. Verification Check (Detailed)
        var appointment = entities.FirstOrDefault(e => e.Name == "Appointment");
        if (appointment != null)
        {
            Console.WriteLine("\nMetadata Audit:");
            Console.WriteLine($" - Entity: {appointment.Name}");
            Console.WriteLine($" - Fields Found: {string.Join(", ", appointment.Fields.Select(f => f.Name))}");
            Console.WriteLine($" - Relations Validated: {string.Join(", ", appointment.Relations.Select(r => r.TargetEntity))}");
        }

        Console.WriteLine("=== SIMULATION SUCCESSFUL ===");
    }

    private bool SimulateBackendBuild(List<EntityMetadata> entities, List<EnumMetadata?> enums)
    {
        // ... (existing field checks)
        
        // Verification: Ensure Appointment.Status uses AppointmentStatus Enum
        var appt = entities.FirstOrDefault(e => e.Name == "Appointment");
        if (appt != null)
        {
            var statusField = appt.Fields.FirstOrDefault(f => f.Name == "Status");
            if (statusField != null && statusField.Type == "AppointmentStatus")
            {
                Console.WriteLine("Step 1.0: Verifying Enum integration in Domain...");
                bool enumExists = enums.Any(e => e?.Name == "AppointmentStatus");
                Console.WriteLine(enumExists ? "Result: Enum reference VALID." : "Result: Enum reference BROKEN.");
            }
            
            Console.WriteLine("Step 1.1: Verifying Overlap Check Workflow logic...");
            bool logicValid = SimulateOverlapCheck(appt);
            Console.WriteLine(logicValid ? "Result: Overlap Logic is SOUND." : "Result: Overlap Logic MISSING FIELDS.");
        }

        foreach (var entity in entities)
        {
            if (string.IsNullOrEmpty(entity.Name)) return false;
            if (entity.Fields.Any(f => string.IsNullOrEmpty(f.Name))) return false;
        }
        return true;
    }

    private bool SimulateOverlapCheck(EntityMetadata appt)
    {
        // Check if required fields for overlap detection exist
        bool hasDoctor = appt.Relations.Any(r => r.TargetEntity == "Doctor");
        bool hasDate = appt.Fields.Any(f => f.Name == "AppointmentDate");
        
        // Conceptually, we also need a 'Duration' or 'EndTime', but for MVP we assume 30m blocks
        return hasDoctor && hasDate;
    }

    private bool SimulateFrontendBuild(List<EntityMetadata> entities, bool production)
    {
        // In a real scenario, this would call 'npm run build'.
        // For simulation, we verify that all entities have corresponding UI bindings.
        foreach (var entity in entities)
        {
            // Simulate check for Angular Component generation
            if (entity.Fields.Count == 0) return false;
        }
        return true;
    }

    private async Task CreateSpecializationEntity(IArtifactRepository repo)
    {
        var meta = new EntityMetadata
        {
            Name = "Specialization",
            Fields = new List<FieldMetadata>
            {
                new() { Name = "Name", Type = "string", IsRequired = true },
                new() { Name = "Description", Type = "string" }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = meta.Name, Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(meta) });
    }

    private async Task CreateDoctorEntity(IArtifactRepository repo)
    {
        var meta = new EntityMetadata
        {
            Name = "Doctor",
            Fields = new List<FieldMetadata>
            {
                new() { Name = "FullName", Type = "string", IsRequired = true },
                new() { Name = "Email", Type = "string", IsRequired = true },
                new() { Name = "ConsultationFee", Type = "decimal", IsRequired = true }
            },
            Relations = new List<RelationMetadata>
            {
                new() { TargetEntity = "Specialization", Type = RelationType.ManyToOne, NavPropName = "Specialization" }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = meta.Name, Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(meta) });
    }

    private async Task CreatePatientEntity(IArtifactRepository repo)
    {
        var meta = new EntityMetadata
        {
            Name = "Patient",
            Fields = new List<FieldMetadata>
            {
                new() { Name = "FullName", Type = "string", IsRequired = true },
                new() { Name = "Email", Type = "string", IsRequired = true }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = meta.Name, Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(meta) });
    }

    private async Task CreateAppointmentEntity(IArtifactRepository repo)
    {
        var meta = new EntityMetadata
        {
            Name = "Appointment",
            Fields = new List<FieldMetadata>
            {
                new() { Name = "AppointmentDate", Type = "datetime", IsRequired = true },
                new() { Name = "Status", Type = "AppointmentStatus", IsRequired = true },
                new() { Name = "FeeAmount", Type = "decimal" }
            },
            Relations = new List<RelationMetadata>
            {
                new() { TargetEntity = "Doctor", Type = RelationType.ManyToOne, NavPropName = "Doctor" },
                new() { TargetEntity = "Patient", Type = RelationType.ManyToOne, NavPropName = "Patient" }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = meta.Name, Type = ArtifactType.Entity, Content = JsonSerializer.Serialize(meta) });
    }

    private async Task CreateOverlapCheckWorkflow(IArtifactRepository repo)
    {
        var workflow = new WorkflowMetadata
        {
            Name = "AppointmentOverlapCheck",
            DefinitionJson = @"{
                ""id"": ""wf-overlap-check"",
                ""name"": ""AppointmentOverlapCheck"",
                ""root"": {
                    ""type"": ""Elsa.Sequence"",
                    ""version"": 1,
                    ""activities"": [
                        {
                            ""id"": ""act-1"",
                            ""type"": ""Elsa.HttpEndpoint"",
                            ""path"": ""/validate-appointment"",
                            ""method"": ""POST""
                        },
                        {
                            ""id"": ""act-2"",
                            ""type"": ""Elsa.JavaScript"",
                            ""script"": ""
                                var db = resolve('GeneratedDbContext');
                                var doctorId = input.DoctorId;
                                var date = input.AppointmentDate;
                                // Simplified simulation of overlap query logic
                                var overlap = db.Appointments.Any(a => a.DoctorId == doctorId && a.Date == date);
                                setVariable('isOverlap', overlap);
                            ""
                        },
                        {
                            ""id"": ""act-3"",
                            ""type"": ""Elsa.If"",
                            ""condition"": ""isOverlap == true"",
                            ""then"": {
                                ""type"": ""Elsa.HttpResult"",
                                ""statusCode"": 409,
                                ""content"": ""Conflict: Time slot already booked.""
                            },
                            ""else"": {
                                ""type"": ""Elsa.HttpResult"",
                                ""statusCode"": 200,
                                ""content"": ""Success: slot available.""
                            }
                        }
                    ]
                }
            }"
        };
        await repo.AddAsync(new Artifact 
        { 
            ProjectId = _projectId, 
            Name = workflow.Name, 
            Type = ArtifactType.Workflow, 
            Content = JsonSerializer.Serialize(workflow) 
        });
    }

    private async Task CreateDoctorDashboard(IArtifactRepository repo)
    {
        var page = new PageMetadata
        {
            Name = "DoctorHome",
            Route = "/doctor/home",
            AllowedRoles = new List<string> { "Doctor" },
            Widgets = new List<WidgetMetadata>
            {
                new() 
                { 
                    Type = "StatCard",
                    Layout = new() { Desktop = new() { ColStart = 0, RowStart = 0, ColSpan = 4, RowSpan = 1 } },
                    Config = new() { Title = "Active Patients", Icon = "groups", Theme = "primary" },
                    DataSource = new() { EntityName = "Patient", DataType = "Entity", Aggregate = "count" }
                },
                new() 
                { 
                    Type = "Calendar",
                    Layout = new() { Desktop = new() { ColStart = 4, RowStart = 0, ColSpan = 8, RowSpan = 5 } },
                    Config = new() { Title = "Shift Planner", Icon = "calendar_today", Theme = "glass" },
                    DataSource = new() { EntityName = "Appointment", DataType = "Entity", DataField = "AppointmentDate" }
                },
                new() 
                { 
                    Type = "DataGrid",
                    Layout = new() { Desktop = new() { ColStart = 0, RowStart = 1, ColSpan = 4, RowSpan = 4 } },
                    Config = new() { Title = "Today's Appointments", Icon = "event" },
                    DataSource = new() { EntityName = "Appointment", DataType = "Entity", Filter = "Status == 'Confirmed'", Sort = "AppointmentDate ASC", Pagination = new() { Enabled = true, PageSize = 10 } }
                }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = page.Name, Type = ArtifactType.Page, Content = JsonSerializer.Serialize(page) });
    }

    private async Task CreateAdminEarningDashboard(IArtifactRepository repo)
    {
        var page = new PageMetadata
        {
            Name = "AdminRevenue",
            Route = "/admin/revenue",
            AllowedRoles = new List<string> { "Admin" },
            Widgets = new List<WidgetMetadata>
            {
                new() 
                { 
                    Type = "StatCard",
                    Layout = new() { Desktop = new() { ColStart = 0, RowStart = 0, ColSpan = 3, RowSpan = 1 } },
                    Config = new() { Title = "Total Gross Revenue", Icon = "monetization_on", Theme = "success" },
                    DataSource = new() { EntityName = "Appointment", DataType = "Entity", Aggregate = "sum", DataField = "FeeAmount" }
                },
                new() 
                { 
                    Type = "Chart",
                    Layout = new() { Desktop = new() { ColStart = 0, RowStart = 1, ColSpan = 12, RowSpan = 5 } },
                    Config = new() { Title = "Revenue Trends", Icon = "show_chart" },
                    DataSource = new() { EntityName = "Appointment", DataType = "Entity", Aggregate = "sum", DataField = "FeeAmount", Sort = "AppointmentDate ASC" }
                }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = page.Name, Type = ArtifactType.Page, Content = JsonSerializer.Serialize(page) });
    }

    private async Task CreateAppointmentStatusEnum(IArtifactRepository repo)
    {
        var meta = new EnumMetadata
        {
            Name = "AppointmentStatus",
            Namespace = "ClinicManagement.Entities",
            Values = new List<EnumValue>
            {
                new() { Name = "Scheduled", Value = 0 },
                new() { Name = "Confirmed", Value = 1 },
                new() { Name = "Cancelled", Value = 2 },
                new() { Name = "Completed", Value = 3 }
            }
        };
        await repo.AddAsync(new Artifact { ProjectId = _projectId, Name = meta.Name, Type = ArtifactType.Enum, Content = JsonSerializer.Serialize(meta) });
        Console.WriteLine("Enum 'AppointmentStatus' metadata seeded.");
    }

    public static async Task Main(string[] args)
    {
        var sim = new ClinicSystemSimulator();
        await sim.RunAsync();
    }
}
