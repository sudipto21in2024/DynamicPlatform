namespace Platform.Engine.Workflows.Activities;

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Platform.Engine.Interfaces;

/// <summary>
/// Elsa activity for uploading files to blob storage
/// </summary>
[Activity("Data Operations", "Upload to Storage", Description = "Uploads file to blob storage and returns download URL")]
public class UploadToStorageActivity : CodeActivity
{
    // Define IBlobStorageService interface here or use the one in Platform.Engine.Interfaces?
    // It was defined inside the file in Step 550.
    // I should check if I defined it in Interfaces. Wait, I did NOT!
    // I better verify if IBlobStorageService is in Interfaces.
    // I'll assume I need to move it out or define it here for now (to fix duplicate definition errors later).
    // Actually, Step 550 showed it INSIDE the file. I should move it to Interfaces first to be clean, 
    // or keep it here if I don't want to break other things right now. 
    // BUT! Since I'm refactoring, I should do it right.
    // I'll check if IBlobStorageService exists in Interfaces. if not I create it.
    
    [Input(Description = "File stream to upload")]
    public Input<Stream> FileStream { get; set; } = default!;
    
    [Input(Description = "File name")]
    public Input<string> FileName { get; set; } = default!;
    
    [Input(Description = "Job ID")]
    public Input<string> JobId { get; set; } = default!;
    
    [Input(Description = "Container name", DefaultValue = "reports")]
    public Input<string> ContainerName { get; set; } = new("reports");
    
    [Output]
    public Output<string> DownloadUrl { get; set; } = default!;
    
    [Output]
    public Output<string> BlobPath { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Resolve service
        // Since IBlobStorageService was defined locally, I can't resolve it via GetRequiredService unless the interface is shared.
        // Assuming I'll move the interface, I'll use the interface type.
        // For now, I'll use dynamic or assume it's registered.
        // Wait, I should really fix the interface location first.
        // I'll assume IBlobStorageService is in Platform.Engine.Interfaces (I will create it in next step).
        var storageService = context.GetRequiredService<IBlobStorageService>(); 
        
        var fileStream = FileStream.Get(context);
        var fileName = FileName.Get(context);
        var jobId = JobId.Get(context);
        var containerName = ContainerName.Get(context);
        
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new InvalidOperationException("File stream is null or empty");
        }
        
        // Reset stream position
        fileStream.Position = 0;
        
        // Create blob path with job ID
        var blobPath = $"{jobId}/{fileName}";
        
        // Upload to storage
        var downloadUrl = await storageService.UploadAsync(
            containerName,
            blobPath,
            fileStream,
            context.CancellationToken
        );
        
        // Dispose the stream after upload (optional, but good practice if we own it)
        await fileStream.DisposeAsync();
        
        DownloadUrl.Set(context, downloadUrl);
        BlobPath.Set(context, blobPath);
    }
}

// I will MOVE this interface to a separate file in next step to follow standards.
// For now, removing it from here to avoid conflicts if I create it separately.
