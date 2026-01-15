namespace NOIR.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Wolverine handler for admin user creation.
/// Allows administrators to create new users with optional role assignments.
/// </summary>
public class CreateUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILocalizationService _localization;

    public CreateUserCommandHandler(
        IUserIdentityService userIdentityService,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _localization = localization;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Check if email is already taken
        var existingUser = await _userIdentityService.FindByEmailAsync(command.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure<UserDto>(
                Error.Conflict(_localization["users.emailAlreadyExists"], ErrorCodes.Auth.DuplicateEmail));
        }

        // Create user DTO
        var createDto = new CreateUserDto(
            Email: command.Email,
            FirstName: command.FirstName,
            LastName: command.LastName,
            DisplayName: command.DisplayName);

        // Create user
        var createResult = await _userIdentityService.CreateUserAsync(createDto, command.Password, cancellationToken);
        if (!createResult.Succeeded)
        {
            return Result.Failure<UserDto>(
                Error.ValidationErrors(createResult.Errors!, ErrorCodes.Validation.General));
        }

        var userId = createResult.UserId!;

        // Assign roles if provided
        if (command.RoleNames is { Count: > 0 })
        {
            var roleResult = await _userIdentityService.AssignRolesAsync(
                userId,
                command.RoleNames,
                replaceExisting: true,
                cancellationToken);

            if (!roleResult.Succeeded)
            {
                // User created but role assignment failed - log warning but don't fail
                // The user can still be assigned roles later
            }
        }

        // Get created user
        var createdUser = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
        if (createdUser is null)
        {
            return Result.Failure<UserDto>(
                Error.Failure(ErrorCodes.System.UnknownError, "Failed to retrieve created user"));
        }

        // Get assigned roles
        var roles = await _userIdentityService.GetRolesAsync(userId, cancellationToken);

        var userDto = new UserDto(
            createdUser.Id,
            createdUser.Email,
            createdUser.Email, // UserName
            createdUser.DisplayName,
            createdUser.FirstName,
            createdUser.LastName,
            true, // EmailConfirmed - admin-created users are auto-confirmed
            false, // LockoutEnabled - new users are not locked
            null, // LockoutEnd
            roles);

        return Result.Success(userDto);
    }
}
