namespace Platform.Engine.Workflows.Activities;

using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;

/// <summary>
/// Elsa activity for uploading files to blob storage
/// </summary>
[Activity(
    Category = "Data Operations",
    DisplayName = "Upload to Storage",
    Description = "Uploads file to blob storage and returns download URL"
)]
public class UploadToStorageActivity : Activity
{
    private readonly IBlobStorageService _storageService;
    
    [ActivityInput(Hint = "File stream to upload")]
    public Stream FileStream { get; set; } = null!;
    
    [ActivityInput(Hint = "File name")]
    public string FileName { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Job ID")]
    public string JobId { get; set; } = string.Empty;
    
    [ActivityInput(Hint = "Container name", DefaultValue = "reports")]
    public string ContainerName { get; set; } = "reports";
    
    [ActivityOutput]
    public string DownloadUrl { get; set; } = string.Empty;
    
    [ActivityOutput]
    public string BlobPath { get; set; } = string.Empty;
    
    public UploadToStorageActivity(IBlobStorageService storageService)
    {
        _storageService = storageService;
    }
    
    protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(
        ActivityExecutionContext context)
    {
        if (FileStream == null || FileStream.Length == 0)
        {
            throw new InvalidOperationException("File stream is null or empty");
        }
        
        // Reset stream position
        FileStream.Position = 0;
        
        // Create blob path with job ID
        BlobPath = $"{JobId}/{FileName}";
        
        // Upload to storage
        DownloadUrl = await _storageService.UploadAsync(
            ContainerName,
            BlobPath,
            FileStream,
            context.CancellationToken
        );
        
        // Dispose the stream after upload
        await FileStream.DisposeAsync();
        
        return Done();
    }
}

/// <summary>
/// Interface for blob storage service
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(
        string containerName,
        string blobPath,
        Stream stream,
        CancellationToken cancellationToken = default);
    
    Task<Stream> DownloadAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default);
}
