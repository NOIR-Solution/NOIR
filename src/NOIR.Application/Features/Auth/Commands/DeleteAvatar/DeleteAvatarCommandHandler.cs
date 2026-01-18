namespace NOIR.Application.Features.Auth.Commands.DeleteAvatar;

/// <summary>
/// Handler for deleting user avatar.
/// Deletes all variant files from storage and clears user's AvatarUrl.
/// </summary>
public class DeleteAvatarCommandHandler : IScopedService
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IFileStorage _fileStorage;
    private readonly ILocalizationService _localization;
    private readonly ILogger<DeleteAvatarCommandHandler> _logger;

    private const string AvatarFolder = "avatars";

    public DeleteAvatarCommandHandler(
        IUserIdentityService userIdentityService,
        IFileStorage fileStorage,
        ILocalizationService localization,
        ILogger<DeleteAvatarCommandHandler> logger)
    {
        _userIdentityService = userIdentityService;
        _fileStorage = fileStorage;
        _localization = localization;
        _logger = logger;
    }

    public async Task<Result<AvatarDeleteResultDto>> Handle(
        DeleteAvatarCommand command,
        CancellationToken cancellationToken)
    {
        // Check user is authenticated
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<AvatarDeleteResultDto>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find user
        var user = await _userIdentityService.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AvatarDeleteResultDto>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Check if user has an avatar
        if (string.IsNullOrEmpty(user.AvatarUrl))
        {
            return Result.Success(new AvatarDeleteResultDto(
                true,
                _localization["profile.avatar.noAvatar"]));
        }

        // Delete all avatar variant files
        await DeleteAvatarFiles(user.AvatarUrl, command.UserId, cancellationToken);

        // Clear user's AvatarUrl (set to empty string to indicate clearing)
        var updateResult = await _userIdentityService.UpdateUserAsync(
            command.UserId,
            new UpdateUserDto(AvatarUrl: string.Empty),
            cancellationToken);

        if (!updateResult.Succeeded)
        {
            return Result.Failure<AvatarDeleteResultDto>(
                Error.Failure(
                    ErrorCodes.Auth.UpdateFailed,
                    string.Join(", ", updateResult.Errors ?? [])));
        }

        return Result.Success(new AvatarDeleteResultDto(
            true,
            _localization["profile.avatar.deleteSuccess"]));
    }

    /// <summary>
    /// Delete avatar files (all variants).
    /// </summary>
    private async Task DeleteAvatarFiles(string avatarUrl, string userId, CancellationToken ct)
    {
        try
        {
            // Convert public URL back to storage path using file storage service
            var basePath = _fileStorage.GetStoragePath(avatarUrl) ?? avatarUrl;

            // Extract the slug from the path to find all variants
            // Path format: avatars/{userId}/{slug}-{variant}.{format}
            var fileName = Path.GetFileName(basePath);
            var slugMatch = System.Text.RegularExpressions.Regex.Match(
                fileName,
                @"^(.+?)-(thumb|small|medium|large|extralarge|original)\.");

            if (slugMatch.Success)
            {
                var slug = slugMatch.Groups[1].Value;
                var folder = $"{AvatarFolder}/{userId}";

                // Delete all possible variant files
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

                _logger.LogInformation("Deleted avatar files for user {UserId}, slug: {Slug}", userId, slug);
            }
            else
            {
                // Fallback: just delete the single file (old format)
                await _fileStorage.DeleteAsync(basePath, ct);
                _logger.LogInformation("Deleted single avatar file for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete avatar files for user {UserId}", userId);
            // Don't fail the delete operation if file cleanup fails
        }
    }
}
