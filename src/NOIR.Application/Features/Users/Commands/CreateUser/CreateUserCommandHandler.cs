namespace NOIR.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Wolverine handler for admin user creation.
/// Allows administrators to create new users with optional role assignments.
/// Users are automatically assigned to the current tenant context.
/// </summary>
public class CreateUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;
    private readonly IEmailService _emailService;
    private readonly IBaseUrlService _baseUrlService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization,
        IEmailService emailService,
        IBaseUrlService baseUrlService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
        _emailService = emailService;
        _baseUrlService = baseUrlService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Get current tenant context - users are created within the current tenant
        var tenantId = _currentUser.TenantId;

        // Check if email is already taken within this tenant
        var existingUser = await _userIdentityService.FindByEmailAsync(command.Email, tenantId, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure<UserDto>(
                Error.Conflict(_localization["users.emailAlreadyExists"], ErrorCodes.Auth.DuplicateEmail));
        }

        // Create user DTO with tenant assignment
        var createDto = new CreateUserDto(
            Email: command.Email,
            FirstName: command.FirstName,
            LastName: command.LastName,
            DisplayName: command.DisplayName,
            TenantId: tenantId);

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

        // Send welcome email (fire and forget - don't block user creation)
        if (command.SendWelcomeEmail)
        {
            _ = SendWelcomeEmailAsync(
                createdUser.Email,
                createdUser.DisplayName ?? createdUser.Email,
                command.Password,
                cancellationToken);
        }

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

    private async Task SendWelcomeEmailAsync(
        string email,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            var loginUrl = _baseUrlService.BuildUrl("/login");
            var model = new WelcomeEmailModel(
                UserName: userName,
                Email: email,
                TemporaryPassword: temporaryPassword,
                LoginUrl: loginUrl,
                ApplicationName: "NOIR");

            await _emailService.SendTemplateAsync(
                email,
                "Welcome to NOIR - Your Account Has Been Created",
                "WelcomeEmail",
                model,
                cancellationToken);

            _logger.LogInformation("Welcome email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            // Log but don't fail - email delivery shouldn't block user creation
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
        }
    }
}
