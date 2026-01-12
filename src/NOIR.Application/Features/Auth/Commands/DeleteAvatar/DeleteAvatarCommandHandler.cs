namespace NOIR.Application.Features.Auth.Commands.DeleteAvatar;

/// <summary>
/// Handler for deleting user avatar.
/// Deletes file from storage and clears user's AvatarUrl.
/// </summary>
public class DeleteAvatarCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IFileStorage _fileStorage;
    private readonly ILocalizationService _localization;

    public DeleteAvatarCommandHandler(
        IUserIdentityService userIdentityService,
        IFileStorage fileStorage,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _fileStorage = fileStorage;
        _localization = localization;
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

        // Delete from storage
        await _fileStorage.DeleteAsync(user.AvatarUrl, cancellationToken);

        // Clear user's AvatarUrl (set to empty string to indicate clearing)
        var updateResult = await _userIdentityService.UpdateUserAsync(
            command.UserId,
            new UpdateUserDto(AvatarUrl: string.Empty),
            cancellationToken);

        if (!updateResult.Succeeded)
        {
            return Result.Failure<AvatarDeleteResultDto>(
                Error.Failure(
                    string.Join(", ", updateResult.Errors),
                    ErrorCodes.Auth.UpdateFailed));
        }

        return Result.Success(new AvatarDeleteResultDto(
            true,
            _localization["profile.avatar.deleteSuccess"]));
    }
}
