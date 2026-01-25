using Microsoft.EntityFrameworkCore;
using Platform.Core.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Data.Repositories;
using Platform.Engine.Generators;

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

// Engine Services
builder.Services.AddScoped<EntityGenerator>();
builder.Services.AddScoped<DbContextGenerator>();
builder.Services.AddScoped<RepositoryGenerator>();
builder.Services.AddScoped<ControllerGenerator>();
builder.Services.AddScoped<ProjectGenerator>();
builder.Services.AddScoped<ConnectorGenerator>();
builder.Services.AddScoped<AngularComponentGenerator>();
builder.Services.AddScoped<Platform.Engine.Services.MetadataLoader>();
builder.Services.AddScoped<Platform.Engine.Services.RelationNormalizationService>();

// AI Services
builder.Services.AddHttpClient<Platform.API.Services.GeminiService>();

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
