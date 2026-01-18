namespace NOIR.Infrastructure.Storage;

/// <summary>
/// File storage configuration settings.
/// Supports Local, Azure, and S3 providers.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider: "Local", "Azure", or "S3".
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Base path for local storage.
    /// </summary>
    public string LocalPath { get; set; } = "uploads";

    /// <summary>
    /// Azure Blob Storage connection string.
    /// </summary>
    public string? AzureConnectionString { get; set; }

    /// <summary>
    /// Azure Blob container name.
    /// </summary>
    public string? AzureContainerName { get; set; }

    /// <summary>
    /// AWS S3 access key ID.
    /// </summary>
    public string? S3AccessKeyId { get; set; }

    /// <summary>
    /// AWS S3 secret access key.
    /// </summary>
    public string? S3SecretAccessKey { get; set; }

    /// <summary>
    /// AWS S3 bucket name.
    /// </summary>
    public string? S3BucketName { get; set; }

    /// <summary>
    /// AWS S3 region (e.g., "us-east-1", "eu-west-1").
    /// </summary>
    public string? S3Region { get; set; }

    /// <summary>
    /// URL prefix for serving media files (e.g., "/media", "/cdn").
    /// Used to generate public URLs for uploaded files.
    /// Default: "/media"
    /// </summary>
    public string MediaUrlPrefix { get; set; } = "/media";
}
