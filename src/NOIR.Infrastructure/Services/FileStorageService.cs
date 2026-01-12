namespace NOIR.Infrastructure.Services;

/// <summary>
/// FluentStorage implementation of file storage service.
/// Supports local disk, Azure Blob, AWS S3, etc. based on configuration.
/// </summary>
public class FileStorageService : IFileStorage, IScopedService
{
    private readonly IBlobStorage _storage;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IBlobStorage storage, ILogger<FileStorageService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string? folder = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.IsNullOrEmpty(folder) ? fileName : $"{folder}/{fileName}";

            // Ensure stream position is at the beginning
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            // Copy to MemoryStream for reliable writing (some storage providers need seekable streams)
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            await _storage.WriteAsync(path, memoryStream, false, cancellationToken);

            _logger.LogInformation("File uploaded: {Path}, Size: {Size} bytes", path, memoryStream.Length);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _storage.ExistsAsync(path, cancellationToken))
            {
                _logger.LogWarning("File not found: {Path}", path);
                return null;
            }

            return await _storage.OpenReadAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {Path}", path);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _storage.ExistsAsync(path, cancellationToken))
            {
                _logger.LogWarning("File not found for deletion: {Path}", path);
                return false;
            }

            await _storage.DeleteAsync(path, cancellationToken);
            _logger.LogInformation("File deleted: {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", path);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _storage.ExistsAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if file exists: {Path}", path);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListAsync(string? folder = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobs = await _storage.ListAsync(new ListOptions { FolderPath = folder }, cancellationToken);
            return blobs.Select(b => b.FullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files in folder: {Folder}", folder ?? "root");
            return [];
        }
    }

    public string? GetPublicUrl(string path)
    {
        // Return API endpoint URL for serving files
        // The FileEndpoints handles serving files from storage
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // Return relative URL that will be handled by FileEndpoints
        return $"/api/files/{path}";
    }
}
