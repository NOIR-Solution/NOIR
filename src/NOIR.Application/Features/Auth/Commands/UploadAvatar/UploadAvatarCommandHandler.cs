namespace NOIR.Application.Features.Auth.Commands.UploadAvatar;

/// <summary>
/// Handler for uploading user avatar.
/// Processes the image (resizes, optimizes) and updates user's AvatarUrl.
/// </summary>
public class UploadAvatarCommandHandler : IScopedService
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILocalizationService _localization;
    private readonly ILogger<UploadAvatarCommandHandler> _logger;

    private const string AvatarFolder = "avatars";

    public UploadAvatarCommandHandler(
        IUserIdentityService userIdentityService,
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        ILocalizationService localization,
        ILogger<UploadAvatarCommandHandler> logger)
    {
        _userIdentityService = userIdentityService;
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
        _localization = localization;
        _logger = logger;
    }

    public async Task<Result<AvatarUploadResultDto>> Handle(
        UploadAvatarCommand command,
        CancellationToken cancellationToken)
    {
        // Check user is authenticated
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<AvatarUploadResultDto>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find user
        var user = await _userIdentityService.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AvatarUploadResultDto>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Validate it's a valid image
        if (!await _imageProcessor.IsValidImageAsync(command.FileStream, command.FileName))
        {
            return Result.Failure<AvatarUploadResultDto>(
                Error.Validation(
                    "avatar",
                    _localization["profile.avatar.invalidFormat"],
                    ErrorCodes.Validation.InvalidInput));
        }

        // Reset stream for processing
        if (command.FileStream.CanSeek)
        {
            command.FileStream.Position = 0;
        }

        // Delete old avatar files if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            await DeleteOldAvatarFiles(user.AvatarUrl, command.UserId, cancellationToken);
        }

        // Configure processing options for avatars
        var storageFolder = $"{AvatarFolder}/{command.UserId}";
        var options = new ImageProcessingOptions
        {
            // Avatars only need thumb (150px for lists) and medium (640px for profile)
            Variants = [ImageVariant.Thumb, ImageVariant.Medium],
            // Generate WebP + JPEG (no AVIF for faster processing)
            Formats = [OutputFormat.WebP, OutputFormat.Jpeg],
            // No placeholder needed for avatars
            GenerateThumbHash = false,
            ExtractDominantColor = false,
            PreserveOriginal = false,
            StorageFolder = storageFolder
        };

        // Process the image
        var result = await _imageProcessor.ProcessAsync(
            command.FileStream,
            command.FileName,
            options,
            cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Avatar processing failed for user {UserId}: {Error}",
                command.UserId, result.ErrorMessage);

            return Result.Failure<AvatarUploadResultDto>(
                Error.Failure(
                    ErrorCodes.Auth.UpdateFailed,
                    result.ErrorMessage ?? _localization["profile.avatar.processingFailed"]));
        }

        // Get the medium WebP variant as the primary avatar URL
        var avatarVariant = result.Variants
            .Where(v => v.Variant == ImageVariant.Medium && v.Format == OutputFormat.WebP)
            .FirstOrDefault()
            ?? result.Variants.FirstOrDefault();

        if (avatarVariant is null)
        {
            _logger.LogError("No avatar variant generated for user {UserId}", command.UserId);
            return Result.Failure<AvatarUploadResultDto>(
                Error.Failure(ErrorCodes.Auth.UpdateFailed, "Failed to generate avatar"));
        }

        var publicUrl = avatarVariant.Url ?? avatarVariant.Path;

        // Update user's AvatarUrl
        var updateResult = await _userIdentityService.UpdateUserAsync(
            command.UserId,
            new UpdateUserDto(AvatarUrl: publicUrl),
            cancellationToken);

        if (!updateResult.Succeeded)
        {
            // Rollback: delete the uploaded files
            foreach (var variant in result.Variants)
            {
                await _fileStorage.DeleteAsync(variant.Path, cancellationToken);
            }

            return Result.Failure<AvatarUploadResultDto>(
                Error.Failure(
                    ErrorCodes.Auth.UpdateFailed,
                    string.Join(", ", updateResult.Errors ?? [])));
        }

        _logger.LogInformation(
            "Avatar uploaded for user {UserId}: {Slug} ({VariantCount} variants in {Ms}ms)",
            command.UserId, result.Slug, result.Variants.Count, result.ProcessingTimeMs);

        return Result.Success(new AvatarUploadResultDto(
            publicUrl,
            _localization["profile.avatar.uploadSuccess"]));
    }

    /// <summary>
    /// Delete old avatar files (all variants).
    /// </summary>
    private async Task DeleteOldAvatarFiles(string oldAvatarUrl, string userId, CancellationToken ct)
    {
        try
        {
            // Convert public URL back to storage path using file storage service
            var basePath = _fileStorage.GetStoragePath(oldAvatarUrl) ?? oldAvatarUrl;

            // Extract the slug from the path to find all variants
            // Path format: avatars/{userId}/{slug}-{variant}.{format}
            var fileName = Path.GetFileName(basePath);
            var slugMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"^(.+?)-(thumb|small|medium|large|extralarge|original)\.");

            if (slugMatch.Success)
            {
                var slug = slugMatch.Groups[1].Value;
                var folder = $"{AvatarFolder}/{userId}";

                // Try to delete all possible variant files
                var variants = new[] { "thumb", "small", "medium", "large", "extralarge", "original" };
                var formats = new[] { "webp", "jpg", "avif", "png" };

                foreach (var variant in variants)
                {
                    foreach (var format in formats)
                    {
                        var path = $"{folder}/{slug}-{variant}.{format}";
                        if (await _fileStorage.ExistsAsync(path, ct))
                        {
                            await _fileStorage.DeleteAsync(path, ct);
                        }
                    }
                }
            }
            else
            {
                // Fallback: just delete the single file (old format)
                await _fileStorage.DeleteAsync(basePath, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete old avatar files for user {UserId}", userId);
            // Don't fail the upload if cleanup fails
        }
    }
}
