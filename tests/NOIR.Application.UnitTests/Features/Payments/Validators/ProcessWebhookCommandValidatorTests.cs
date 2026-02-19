using NOIR.Application.Features.Payments.Commands.ProcessWebhook;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for ProcessWebhookCommandValidator.
/// Tests all validation rules for processing a payment webhook.
/// </summary>
public class ProcessWebhookCommandValidatorTests
{
    private readonly ProcessWebhookCommandValidator _validator = new();

    private static ProcessWebhookCommand CreateValidCommand() => new(
        Provider: "MoMo",
        RawPayload: "{\"transactionId\":\"123\",\"status\":\"completed\"}",
        Signature: "abc123",
        IpAddress: "127.0.0.1",
        Headers: null);

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Provider Validation

    [Fact]
    public async Task Validate_WhenProviderIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Provider = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Provider)
            .WithErrorMessage("Provider is required.");
    }

    [Fact]
    public async Task Validate_WhenProviderExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Provider = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Provider)
            .WithErrorMessage("Provider cannot exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_WhenProviderIs50Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Provider = new string('A', 50) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Provider);
    }

    #endregion

    #region RawPayload Validation

    [Fact]
    public async Task Validate_WhenRawPayloadIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { RawPayload = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RawPayload)
            .WithErrorMessage("Webhook payload is required.");
    }

    [Fact]
    public async Task Validate_WhenRawPayloadExceedsMaxSize_ShouldHaveError()
    {
        // Arrange - 1MB + 1
        var command = CreateValidCommand() with { RawPayload = new string('X', 1048577) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RawPayload)
            .WithErrorMessage("Webhook payload exceeds maximum size.");
    }

    [Fact]
    public async Task Validate_WhenRawPayloadIsAtMaxSize_ShouldNotHaveError()
    {
        // Arrange - exactly 1MB
        var command = CreateValidCommand() with { RawPayload = new string('X', 1048576) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RawPayload);
    }

    #endregion
}
