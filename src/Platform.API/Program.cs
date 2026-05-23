using Microsoft.EntityFrameworkCore;
using Platform.Core.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;
using Platform.Engine.Generators;
using Elsa.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowStudio",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// DB Context
builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseSqlite("Data Source=platform.db"));

// Repositories
builder.Services.AddScoped<IArtifactRepository, ArtifactRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

// Engine Services
builder.Services.AddScoped<EntityGenerator>();
builder.Services.AddScoped<DbContextGenerator>();
builder.Services.AddScoped<RepositoryGenerator>();
builder.Services.AddScoped<ControllerGenerator>();
builder.Services.AddScoped<ProjectGenerator>();
builder.Services.AddScoped<ConnectorGenerator>();
builder.Services.AddScoped<SecurityGenerator>();
builder.Services.AddScoped<FrontendGenerator>();
builder.Services.AddScoped<AngularComponentGenerator>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IVersioningService, Platform.Engine.Services.VersioningService>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IMetadataDiffService, Platform.Engine.Services.MetadataDiffService>();
builder.Services.AddScoped<Platform.Engine.Interfaces.ISqlSchemaEvolutionService, Platform.Engine.Services.SqlSchemaEvolutionService>();
builder.Services.AddScoped<Platform.Engine.Services.MetadataLoader>();
builder.Services.AddScoped<Platform.Engine.Services.RelationNormalizationService>();
builder.Services.AddScoped<Platform.Engine.Interfaces.ICompatibilityProvider, Platform.Engine.Services.CompatibilityProvider>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IMetadataNormalizationService, Platform.Engine.Services.MetadataNormalizationService>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IConnectivityHub, Platform.Engine.Services.ConnectivityHub>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IDataProvider, Platform.Engine.Services.DataExecution.ConnectorDataProvider>();
builder.Services.AddScoped<Platform.Engine.Interfaces.IJobTrackingService, Platform.Engine.Services.JobTrackingService>();
builder.Services.AddScoped<Platform.Engine.Services.DataExecution.DataExecutionEngine>();

// AI Services — OpenAI-compatible provider infrastructure
builder.Services.AddHttpClient("AiProvider", (sp, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("AiSettings:TimeoutSeconds", 120));
});
builder.Services.AddScoped<Platform.Core.Interfaces.ITenantAiProviderRepository,
    Platform.Infrastructure.Data.Repositories.TenantAiProviderRepository>();
builder.Services.AddScoped<Platform.Engine.Services.Ai.TenantAiProviderResolver>();
builder.Services.AddScoped<Platform.Engine.Services.Ai.SchemaContextExtractor>();
builder.Services.AddScoped<Platform.Engine.Services.Ai.AiSkillLibrary>();
// Legacy Gemini service (kept for backwards compatibility)
builder.Services.AddHttpClient<Platform.API.Services.GeminiService>();

// Elsa Workflows 3.0 Integration
builder.Services.AddElsa(elsa =>
{
    var connectionString = "Data Source=platform.db";
    
    // Configure Management with EF Core & SQLite
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(connectionString)));
    
    // Configure Runtime with EF Core & SQLite
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(connectionString)));
    
    // Enable API
    elsa.UseWorkflowsApi();
    
    // Enable HTTP activities
    elsa.UseHttp(http => http.ConfigureHttpOptions = options => 
    {
        options.BasePath = "/workflows";
        options.BaseUrl = new Uri("http://localhost:5018");
    });
    
    // Enable JavaScript and Liquid expressions
    elsa.UseJavaScript();
    elsa.UseLiquid();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowStudio");

app.UseAuthorization();

// Elsa Middleware
app.UseWorkflowsApi();
app.UseWorkflows();

app.MapControllers();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"DB Init Error: {ex.Message}");
}

app.Run();
