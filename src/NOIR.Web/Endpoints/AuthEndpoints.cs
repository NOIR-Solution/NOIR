namespace NOIR.Web.Endpoints;

/// <summary>
/// Authentication API endpoints.
/// Supports both JWT (header) and cookie-based authentication.
/// Uses Result pattern with ToHttpResult() extension for consistent error handling.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .CacheOutput("NoCache"); // Never cache auth responses

        // Login/Register/Refresh use stricter "auth" rate limit (Sliding Window, 5 req/min)
        // to prevent brute force attacks
        group.MapPost("/register", async (
            RegisterCommand command,
            IMessageBus bus,
            bool useCookies = false) =>
        {
            // Apply useCookies from query parameter
            var commandWithCookies = command with { UseCookies = useCookies };
            var result = await bus.InvokeAsync<Result<AuthResponse>>(commandWithCookies);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("Register")
        .WithSummary("Register a new user account")
        .WithDescription("Creates a new user account. Use ?useCookies=true to set HttpOnly auth cookies for browser clients.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/login", async (
            LoginCommand command,
            IMessageBus bus,
            bool useCookies = false) =>
        {
            // Apply useCookies from query parameter
            var commandWithCookies = command with { UseCookies = useCookies };
            var result = await bus.InvokeAsync<Result<AuthResponse>>(commandWithCookies);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("Login")
        .WithSummary("Login with email and password")
        .WithDescription("Authenticates user and returns tokens. Use ?useCookies=true to set HttpOnly auth cookies for browser clients.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (
            IMessageBus bus,
            LogoutCommand? command) =>
        {
            // Use provided command or create default
            var logoutCommand = command ?? new LogoutCommand();
            var result = await bus.InvokeAsync<Result>(logoutCommand);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("Logout")
        .WithSummary("Logout and clear authentication")
        .WithDescription("Clears auth cookies and optionally revokes refresh token. Use revokeAllSessions=true to logout from all devices.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/refresh", async (RefreshTokenCommand command, IMessageBus bus, bool useCookies = false) =>
        {
            // Apply useCookies from query parameter
            var commandWithCookies = command with { UseCookies = useCookies };
            var result = await bus.InvokeAsync<Result<AuthResponse>>(commandWithCookies);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("RefreshToken")
        .WithSummary("Refresh access token using refresh token")
        .WithDescription("Refreshes access token. Use ?useCookies=true to update HttpOnly auth cookies for browser clients. Refresh token can be provided in request body or will be read from cookie.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // /me endpoint uses standard "fixed" rate limit (already authenticated)
        group.MapGet("/me", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<CurrentUserDto>>(new GetCurrentUserQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("fixed")
        .WithName("GetCurrentUser")
        .WithSummary("Get current authenticated user profile")
        .Produces<CurrentUserDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // Update profile endpoint with DTO-level audit tracking
        group.MapPut("/me", async (
            UpdateUserProfileCommand command,
            IMessageBus bus,
            ICurrentUser currentUser) =>
        {
            // Set UserId for audit before-state fetching
            var commandWithUserId = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<UserProfileDto>>(commandWithUserId);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("fixed")
        .WithName("UpdateUserProfile")
        .WithSummary("Update current user profile")
        .WithDescription("Updates user profile fields (first name, last name). Changes are tracked in audit logs with before/after diff.")
        .Produces<UserProfileDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // Forgot Password Flow
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IPasswordResetService passwordResetService,
            HttpContext httpContext,
            IMultiTenantContextAccessor tenantAccessor) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

            var result = await passwordResetService.RequestPasswordResetAsync(
                request.Email,
                tenantId,
                ipAddress);

            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("ForgotPassword")
        .WithSummary("Request password reset OTP")
        .WithDescription("Sends a 6-digit OTP to the email address for password reset verification. Returns session token for the reset flow.")
        .Produces<PasswordResetRequestResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/forgot-password/verify", async (
            VerifyOtpRequest request,
            IPasswordResetService passwordResetService) =>
        {
            var result = await passwordResetService.VerifyOtpAsync(
                request.SessionToken,
                request.Otp);

            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("VerifyPasswordResetOtp")
        .WithSummary("Verify password reset OTP")
        .WithDescription("Verifies the 6-digit OTP and returns a reset token for setting the new password.")
        .Produces<PasswordResetVerifyResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/forgot-password/resend", async (
            ResendOtpRequest request,
            IPasswordResetService passwordResetService) =>
        {
            var result = await passwordResetService.ResendOtpAsync(request.SessionToken);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("ResendPasswordResetOtp")
        .WithSummary("Resend password reset OTP")
        .WithDescription("Resends the OTP to the email. Subject to cooldown and max resend limits.")
        .Produces<PasswordResetResendResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/forgot-password/reset", async (
            ResetPasswordRequest request,
            IPasswordResetService passwordResetService) =>
        {
            var result = await passwordResetService.ResetPasswordAsync(
                request.ResetToken,
                request.NewPassword);

            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("ResetPassword")
        .WithSummary("Reset password with token")
        .WithDescription("Sets a new password using the reset token from OTP verification. All existing sessions are revoked for security.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Change Password (for authenticated users)
        group.MapPost("/change-password", async (
            ChangePasswordCommand command,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("fixed")
        .WithName("ChangePassword")
        .WithSummary("Change the current user's password")
        .WithDescription("Changes password after verifying current password. All sessions are revoked for security.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // Avatar Upload
        group.MapPost("/me/avatar", async (
            IFormFile file,
            [FromServices] ICurrentUser currentUser,
            [FromServices] UploadAvatarCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            await using var stream = file.OpenReadStream();
            var command = new UploadAvatarCommand(
                file.FileName,
                stream,
                file.ContentType,
                file.Length)
            {
                UserId = currentUser.UserId
            };

            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("fixed")
        .DisableAntiforgery()
        .WithName("UploadAvatar")
        .WithSummary("Upload user avatar image")
        .WithDescription("Uploads avatar image (JPG, PNG, GIF, WebP). Max 2MB. Replaces existing avatar if present.")
        .Produces<AvatarUploadResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // Avatar Delete
        group.MapDelete("/me/avatar", async (
            [FromServices] ICurrentUser currentUser,
            [FromServices] DeleteAvatarCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            var command = new DeleteAvatarCommand { UserId = currentUser.UserId };
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("fixed")
        .WithName("DeleteAvatar")
        .WithSummary("Delete user avatar image")
        .WithDescription("Deletes the user's custom avatar. Falls back to Gravatar or initials.")
        .Produces<AvatarDeleteResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // Email Change Flow
        group.MapPost("/me/email/request", async (
            RequestEmailChangeRequest request,
            ICurrentUser currentUser,
            IEmailChangeService emailChangeService,
            HttpContext httpContext,
            IMultiTenantContextAccessor tenantAccessor,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

            var result = await emailChangeService.RequestEmailChangeAsync(
                currentUser.UserId,
                request.NewEmail,
                tenantId,
                ipAddress,
                cancellationToken);

            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth")
        .WithName("RequestEmailChange")
        .WithSummary("Request email change with OTP verification")
        .WithDescription("Initiates email change by sending a 6-digit OTP to the new email address. Returns session token for verification.")
        .Produces<EmailChangeRequestResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/me/email/verify", async (
            VerifyEmailChangeRequest request,
            IEmailChangeService emailChangeService,
            CancellationToken cancellationToken) =>
        {
            var result = await emailChangeService.VerifyOtpAsync(
                request.SessionToken,
                request.Otp,
                cancellationToken);

            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth")
        .WithName("VerifyEmailChange")
        .WithSummary("Verify email change OTP")
        .WithDescription("Verifies the 6-digit OTP and completes the email change if valid.")
        .Produces<EmailChangeVerifyResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/me/email/resend", async (
            ResendEmailChangeRequest request,
            IEmailChangeService emailChangeService,
            CancellationToken cancellationToken) =>
        {
            var result = await emailChangeService.ResendOtpAsync(
                request.SessionToken,
                cancellationToken);

            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth")
        .WithName("ResendEmailChangeOtp")
        .WithSummary("Resend email change OTP")
        .WithDescription("Resends the OTP to the new email address. Subject to cooldown and max resend limits.")
        .Produces<EmailChangeResendResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests);
    }
}

// Request DTOs for forgot password endpoints
public record ForgotPasswordRequest(string Email);
public record VerifyOtpRequest(string SessionToken, string Otp);
public record ResendOtpRequest(string SessionToken);
public record ResetPasswordRequest(string ResetToken, string NewPassword);

// Request DTOs for email change endpoints
public record RequestEmailChangeRequest(string NewEmail);
public record VerifyEmailChangeRequest(string SessionToken, string Otp);
public record ResendEmailChangeRequest(string SessionToken);
