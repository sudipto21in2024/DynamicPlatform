namespace Platform.Engine.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Platform.Engine.Interfaces;
using Platform.Engine.Services;
using Platform.Engine.Services.DataExecution;
using Platform.Engine.Services.OutputGenerators;
using Platform.Engine.Workflows.Activities;
using Elsa;

/// <summary>
/// Extension methods for configuring Visual Data Designer services
/// </summary>
public static class DataDesignerServiceExtensions
{
    /// <summary>
    /// Adds Visual Data Designer services to the DI container
    /// </summary>
    public static IServiceCollection AddDataDesigner(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));
        services.Configure<NotificationOptions>(
            configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<DataExecutionOptions>(
            configuration.GetSection(DataExecutionOptions.SectionName));
        
        // Register data providers
        services.AddScoped<IDataProvider, EntityDataProvider>();
        services.AddScoped<IDataProvider, StaticDataProvider>();
        
        // Register query builder
        services.AddScoped<IQueryBuilder, DynamicQueryBuilder>();
        
        // Register output generators
        services.AddScoped<IOutputGenerator, CsvOutputGenerator>();
        services.AddScoped<IOutputGenerator, ExcelOutputGenerator>();
        services.AddScoped<IOutputGenerator, PdfOutputGenerator>();
        services.AddScoped<IOutputGenerator, JsonOutputGenerator>();
        
        // Register execution engine
        services.AddScoped<DataExecutionEngine>();
        
        // Register job tracking
        services.AddScoped<IJobTrackingService, JobTrackingService>();
        
        // Register blob storage
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        
        // Register notification service
        services.AddScoped<INotificationService, NotificationService>();
        
        // Register Elsa activities
        // Register Elsa activities
        services.AddTransient<ExecuteDataQueryActivity>();
        services.AddTransient<GenerateReportOutputActivity>();
        services.AddTransient<UploadToStorageActivity>();
        services.AddTransient<NotifyUserActivity>();
        
        return services;
    }
}

/// <summary>
/// Data execution configuration options
/// </summary>
public class DataExecutionOptions
{
    public const string SectionName = "DataExecution";
    
    /// <summary>
    /// Quick job timeout in seconds
    /// </summary>
    public int QuickJobTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum rows for quick jobs
    /// </summary>
    public int QuickJobMaxRows { get; set; } = 10000;
    
    /// <summary>
    /// Chunk size for long-running jobs
    /// </summary>
    public int ChunkSize { get; set; } = 1000;
    
    /// <summary>
    /// Default container name for reports
    /// </summary>
    public string DefaultContainerName { get; set; } = "reports";
}
