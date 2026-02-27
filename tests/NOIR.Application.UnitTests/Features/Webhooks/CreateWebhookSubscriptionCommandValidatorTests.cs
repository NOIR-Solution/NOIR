using NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for CreateWebhookSubscriptionCommandValidator.
/// Tests all validation rules including SSRF protection via WebhookUrlValidator.
/// </summary>
public class CreateWebhookSubscriptionCommandValidatorTests
{
    private readonly CreateWebhookSubscriptionCommandValidator _validator = new();

    private static CreateWebhookSubscriptionCommand CreateValidCommand(
        string name = "Order Notifications",
        string url = "https://8.8.8.8/webhooks",
        string eventPatterns = "order.*",
        string? description = null,
        string? customHeaders = null,
        int maxRetries = 5,
        int timeoutSeconds = 30)
    {
        return new CreateWebhookSubscriptionCommand(name, url, eventPatterns, description, customHeaders, maxRetries, timeoutSeconds);
    }

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Webhook subscription name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Webhook subscription name cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 200));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region URL Validation

    [Fact]
    public async Task Validate_WhenUrlIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(url: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL is required.");
    }

    [Fact]
    public async Task Validate_WhenUrlIsHttp_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(url: "http://api.example.com/webhooks");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL must start with 'https://'.");
    }

    [Fact]
    public async Task Validate_WhenUrlExceeds2048Characters_ShouldHaveError()
    {
        // Arrange
        var longUrl = "https://api.example.com/" + new string('a', 2025);
        var command = CreateValidCommand(url: longUrl);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL cannot exceed 2048 characters.");
    }

    [Fact]
    public async Task Validate_WhenUrlIsValidHttps_ShouldNotHaveError()
    {
        // Arrange — use a public IP to avoid DNS resolution failures in test environment
        var command = CreateValidCommand(url: "https://8.8.8.8/webhooks");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
    }

    #endregion

    #region SSRF Protection

    [Fact]
    public async Task Validate_WhenUrlIsLocalhost_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(url: "https://localhost/webhooks");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL must not target private, internal, or loopback addresses.");
    }

    [Fact]
    public async Task Validate_WhenUrlIsPrivateIp_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(url: "https://10.0.0.1/webhooks");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL must not target private, internal, or loopback addresses.");
    }

    [Fact]
    public async Task Validate_WhenUrlIsCloudMetadata_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(url: "https://169.254.169.254/latest/meta-data");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Webhook URL must not target private, internal, or loopback addresses.");
    }

    #endregion

    #region EventPatterns Validation

    [Fact]
    public async Task Validate_WhenEventPatternsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(eventPatterns: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EventPatterns)
            .WithErrorMessage("At least one event pattern is required.");
    }

    [Fact]
    public async Task Validate_WhenEventPatternsExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(eventPatterns: new string('a', 2001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EventPatterns)
            .WithErrorMessage("Event patterns cannot exceed 2000 characters.");
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: new string('A', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region CustomHeaders Validation

    [Fact]
    public async Task Validate_WhenCustomHeadersExceeds4000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(customHeaders: new string('A', 4001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomHeaders)
            .WithErrorMessage("Custom headers cannot exceed 4000 characters.");
    }

    [Fact]
    public async Task Validate_WhenCustomHeadersIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(customHeaders: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomHeaders);
    }

    #endregion

    #region MaxRetries Validation

    [Fact]
    public async Task Validate_WhenMaxRetriesIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxRetries: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxRetries);
    }

    [Fact]
    public async Task Validate_WhenMaxRetriesIsTen_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxRetries: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxRetries);
    }

    [Fact]
    public async Task Validate_WhenMaxRetriesIsEleven_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxRetries: 11);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxRetries)
            .WithErrorMessage("Max retries must be between 0 and 10.");
    }

    [Fact]
    public async Task Validate_WhenMaxRetriesIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxRetries: -1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxRetries)
            .WithErrorMessage("Max retries must be between 0 and 10.");
    }

    #endregion

    #region TimeoutSeconds Validation

    [Fact]
    public async Task Validate_WhenTimeoutSecondsIsFive_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(timeoutSeconds: 5);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeoutSeconds);
    }

    [Fact]
    public async Task Validate_WhenTimeoutSecondsIsSixty_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(timeoutSeconds: 60);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeoutSeconds);
    }

    [Fact]
    public async Task Validate_WhenTimeoutSecondsIsFour_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(timeoutSeconds: 4);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeoutSeconds)
            .WithErrorMessage("Timeout must be between 5 and 60 seconds.");
    }

    [Fact]
    public async Task Validate_WhenTimeoutSecondsIsSixtyOne_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(timeoutSeconds: 61);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeoutSeconds)
            .WithErrorMessage("Timeout must be between 5 and 60 seconds.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange — use a public IP to avoid DNS resolution failures in test environment
        var command = CreateValidCommand(
            name: "Order Notifications",
            url: "https://8.8.8.8/webhooks",
            eventPatterns: "order.*,payment.*",
            description: "Order and payment notifications",
            maxRetries: 3,
            timeoutSeconds: 15);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange — use a public IP to avoid DNS resolution failures in test environment
        var command = new CreateWebhookSubscriptionCommand(
            Name: "Webhook",
            Url: "https://8.8.8.8/hook",
            EventPatterns: "order.*");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
