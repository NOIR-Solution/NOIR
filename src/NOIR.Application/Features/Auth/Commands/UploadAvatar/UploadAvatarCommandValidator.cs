namespace NOIR.Application.Features.Auth.Commands.UploadAvatar;

/// <summary>
/// Validator for UploadAvatarCommand.
/// Validates file size and content type.
/// </summary>
public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    public UploadAvatarCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);

        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage(localization["profile.avatar.maxSize"]);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct.ToLowerInvariant()))
            .WithMessage(localization["profile.avatar.invalidFormat"]);
    }
}
