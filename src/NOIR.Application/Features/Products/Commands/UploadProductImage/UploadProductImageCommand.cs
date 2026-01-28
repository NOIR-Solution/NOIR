namespace NOIR.Application.Features.Products.Commands.UploadProductImage;

/// <summary>
/// Command to upload and process an image for a product.
/// Unlike AddProductImageCommand which takes a URL, this handles file upload,
/// image processing (resize, optimize), and storage.
/// </summary>
public sealed record UploadProductImageCommand(
    Guid ProductId,
    string FileName,
    Stream FileStream,
    string ContentType,
    long FileSize,
    string? AltText,
    bool IsPrimary) : IAuditableCommand<ProductImageUploadResultDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => AltText ?? FileName;
    public string? GetActionDescription() => $"Uploaded image '{FileName}' to product";
}
