namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for file storage operations.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads a file and returns the storage path.
    /// </summary>
    Task<string> UploadAsync(string fileName, Stream content, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file by its path.
    /// </summary>
    Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by its path.
    /// </summary>
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists at the given path.
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a folder.
    /// </summary>
    Task<IEnumerable<string>> ListAsync(string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public URL for the file (if supported by the storage provider).
    /// </summary>
    string? GetPublicUrl(string path);

    /// <summary>
    /// Converts a public URL back to a storage path.
    /// Returns null if the URL doesn't match the expected prefix.
    /// </summary>
    string? GetStoragePath(string publicUrl);

    /// <summary>
    /// Gets the configured media URL prefix (e.g., "/media").
    /// </summary>
    string MediaUrlPrefix { get; }
}
