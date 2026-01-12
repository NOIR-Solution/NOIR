namespace NOIR.Application.Features.Auth.Commands.UploadAvatar;

/// <summary>
/// Handler for uploading user avatar.
/// Uploads file to storage and updates user's AvatarUrl.
/// </summary>
public class UploadAvatarCommandHandler : IScopedService
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IFileStorage _fileStorage;
    private readonly ILocalizationService _localization;

    private const string AvatarFolder = "avatars";
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public UploadAvatarCommandHandler(
        IUserIdentityService userIdentityService,
        IFileStorage fileStorage,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _fileStorage = fileStorage;
        _localization = localization;
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

        // Validate file extension
        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure<AvatarUploadResultDto>(
                Error.Validation(
                    _localization["profile.avatar.invalidFormat"],
                    ErrorCodes.Validation.InvalidInput));
        }

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            // Convert public URL back to storage path (strip /api/files/ prefix)
            var oldStoragePath = user.AvatarUrl.StartsWith("/api/files/", StringComparison.OrdinalIgnoreCase)
                ? user.AvatarUrl["/api/files/".Length..]
                : user.AvatarUrl;
            await _fileStorage.DeleteAsync(oldStoragePath, cancellationToken);
        }

        // Generate unique filename: avatars/{userId}/{guid}{extension}
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var folder = $"{AvatarFolder}/{command.UserId}";

        // Upload file
        var storagePath = await _fileStorage.UploadAsync(
            uniqueFileName,
            command.FileStream,
            folder,
            cancellationToken);

        // Get public URL for storage
        var publicUrl = _fileStorage.GetPublicUrl(storagePath) ?? storagePath;

        // Update user's AvatarUrl with public URL
        var updateResult = await _userIdentityService.UpdateUserAsync(
            command.UserId,
            new UpdateUserDto(AvatarUrl: publicUrl),
            cancellationToken);

        if (!updateResult.Succeeded)
        {
            // Rollback: delete the uploaded file
            await _fileStorage.DeleteAsync(storagePath, cancellationToken);

            return Result.Failure<AvatarUploadResultDto>(
                Error.Failure(
                    string.Join(", ", updateResult.Errors),
                    ErrorCodes.Auth.UpdateFailed));
        }

        return Result.Success(new AvatarUploadResultDto(
            publicUrl,
            _localization["profile.avatar.uploadSuccess"]));
    }
}
