namespace Platform.Engine.Interfaces;

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
