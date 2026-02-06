namespace NOIR.Web.Endpoints;

/// <summary>
/// Development-only endpoints for E2E testing.
/// These endpoints are ONLY registered when the application runs in Development mode.
/// They provide test utilities that bypass normal security flows (e.g., email delivery).
/// </summary>
public static class DevEndpoints
{
    public static void MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev")
            .WithTags("Development")
            .ExcludeFromDescription(); // Hide from API docs

        // Test Password Reset - Creates an OTP session and returns the plaintext OTP
        // This allows E2E tests to complete the full password reset flow without email delivery
        group.MapPost("/auth/test-password-reset", async (
            DevPasswordResetRequest request,
            IOtpService otpService,
            ISecureTokenGenerator tokenGenerator,
            IRepository<PasswordResetOtp, Guid> otpRepository,
            IUnitOfWork unitOfWork,
            IUserIdentityService userIdentityService,
            IOptionsMonitor<PasswordResetSettings> settings,
            IMultiTenantContextAccessor tenantAccessor,
            IMultiTenantStore<Tenant> tenantStore,
            CancellationToken cancellationToken) =>
        {
            var normalizedEmail = request.Email.ToLowerInvariant().Trim();
            var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

            // If no tenant context (e.g., no X-Tenant header), resolve from request or default
            if (string.IsNullOrEmpty(tenantId))
            {
                var tenantIdentifier = request.TenantIdentifier ?? "default";
                var tenant = await tenantStore.GetByIdentifierAsync(tenantIdentifier);
                tenantId = tenant?.Id;
            }

            // Look up user (OTP still created even if user doesn't exist, for security consistency)
            var user = await userIdentityService.FindByEmailAsync(normalizedEmail, tenantId, cancellationToken);

            // Generate plaintext OTP and hash
            var otpCode = otpService.GenerateOtp();
            var otpHash = otpService.HashOtp(otpCode);
            var sessionToken = tokenGenerator.GenerateToken(32);

            // Create OTP record (same as PasswordResetService.RequestPasswordResetAsync)
            var otp = PasswordResetOtp.Create(
                normalizedEmail,
                otpHash,
                sessionToken,
                settings.CurrentValue.OtpExpiryMinutes,
                user?.Id,
                tenantId);

            await otpRepository.AddAsync(otp, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Return session token AND plaintext OTP (dev-only!)
            return Results.Ok(new DevPasswordResetResponse(
                sessionToken,
                otpService.MaskEmail(normalizedEmail),
                otp.ExpiresAt,
                settings.CurrentValue.OtpLength,
                otpCode));
        })
        .WithName("DevTestPasswordReset")
        .WithSummary("[DEV ONLY] Create password reset session with plaintext OTP");

        // Set Password - Directly sets a user's password (bypasses ALL validation)
        // Used for E2E test cleanup to restore original passwords after reset flow tests
        // SECURITY: Only registered when env.IsDevelopment() (see Program.cs line 473-476)
        // Uses PasswordHasher directly to bypass ASP.NET Core Identity password validators
        group.MapPost("/auth/set-password", async (
            DevSetPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email.ToLowerInvariant().Trim());
            if (user is null)
                return Results.NotFound("User not found");

            // Bypass password validation by directly hashing and setting
            user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.NewPassword);
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded
                ? Results.Ok(new { success = true })
                : Results.BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
        })
        .WithName("DevSetPassword")
        .WithSummary("[DEV ONLY] Directly set a user's password (bypasses validation)");
    }
}

// Request/Response DTOs for dev endpoints
public record DevPasswordResetRequest(string Email, string? TenantIdentifier = null);
public record DevSetPasswordRequest(string Email, string NewPassword, string? TenantIdentifier = null);
public record DevPasswordResetResponse(
    string SessionToken,
    string MaskedEmail,
    DateTimeOffset ExpiresAt,
    int OtpLength,
    string PlainOtp);
