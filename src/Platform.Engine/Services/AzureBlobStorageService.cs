namespace Platform.Engine.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Platform.Engine.Workflows.Activities;

/// <summary>
/// Azure Blob Storage implementation
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    
    public AzureBlobStorageService(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
    }
    
    public async Task<string> UploadAsync(
        string containerName,
        string blobPath,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        
        // Create container if it doesn't exist
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken
        );
        
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        // Upload the file
        await blobClient.UploadAsync(
            stream,
            overwrite: true,
            cancellationToken: cancellationToken
        );
        
        // Generate download URL
        if (_options.UseSasTokens)
        {
            // Generate SAS token for temporary access
            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobPath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(_options.SasTokenExpirationDays)
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);
            
            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            return sasToken.ToString();
        }
        else
        {
            // Return blob URL (requires public access or authentication)
            return blobClient.Uri.ToString();
        }
    }
    
    public async Task<Stream> DownloadAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value.Content;
    }
    
    public async Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Blob storage configuration options
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    
    /// <summary>
    /// Azure Storage connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to use SAS tokens for download URLs
    /// </summary>
    public bool UseSasTokens { get; set; } = true;
    
    /// <summary>
    /// SAS token expiration in days
    /// </summary>
    public int SasTokenExpirationDays { get; set; } = 7;
}
