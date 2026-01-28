namespace NOIR.Application.Features.Products.Commands.UploadProductImage;

/// <summary>
/// Validator for UploadProductImageCommand.
/// </summary>
public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxAltTextLength = 500;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/avif",
        "image/heic",
        "image/heif"
    };

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File cannot be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size cannot exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.")
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage("Invalid image format. Allowed formats: JPEG, PNG, GIF, WebP, AVIF, HEIC, HEIF.");

        RuleFor(x => x.AltText)
            .MaximumLength(MaxAltTextLength)
            .WithMessage($"Alt text cannot exceed {MaxAltTextLength} characters.")
            .When(x => x.AltText is not null);
    }
}
