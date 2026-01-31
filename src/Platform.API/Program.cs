using Microsoft.EntityFrameworkCore;
using Platform.Core.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;
using Platform.Engine.Generators;
using Elsa.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.PostgreSql;

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// AI Services
builder.Services.AddHttpClient<Platform.API.Services.GeminiService>();

// Elsa Workflows 3.0 Integration
builder.Services.AddElsa(elsa =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    
    // Configure Management with EF Core & PostgreSQL
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(connectionString)));
    
    // Configure Runtime with EF Core & PostgreSQL
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(connectionString)));
    
    // Enable API
    elsa.UseWorkflowsApi();
    
    // Enable HTTP activities (Incoming webhooks etc)
    elsa.UseHttp(http => http.ConfigureHttpOptions = options => options.BasePath = "/workflows");
    
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
        db.Database.EnsureCreated();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"DB Init Error: {ex.Message}");
}

app.Run();
