using Microsoft.Extensions.Logging;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Features.Users.DTOs;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for sending welcome emails to newly created users.
/// Implements IScopedService for automatic DI registration.
/// </summary>
public class WelcomeEmailService : IWelcomeEmailService, IScopedService
{
    private readonly IEmailService _emailService;
    private readonly IBaseUrlService _baseUrlService;
    private readonly ILogger<WelcomeEmailService> _logger;

    public WelcomeEmailService(
        IEmailService emailService,
        IBaseUrlService baseUrlService,
        ILogger<WelcomeEmailService> logger)
    {
        _emailService = emailService;
        _baseUrlService = baseUrlService;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        string email,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
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
