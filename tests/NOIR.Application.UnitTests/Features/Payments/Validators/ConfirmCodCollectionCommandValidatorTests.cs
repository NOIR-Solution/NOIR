using NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for ConfirmCodCollectionCommandValidator.
/// Tests all validation rules for confirming COD collection.
/// </summary>
public class ConfirmCodCollectionCommandValidatorTests
{
    private readonly ConfirmCodCollectionCommandValidator _validator = new();

    private static ConfirmCodCollectionCommand CreateValidCommand() => new(
        PaymentTransactionId: Guid.NewGuid(),
        Notes: "Collected by delivery agent");

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

    [Fact]
    public async Task Validate_WhenNotesIsNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ConfirmCodCollectionCommand(Guid.NewGuid(), null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PaymentTransactionId Validation

    [Fact]
    public async Task Validate_WhenPaymentTransactionIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PaymentTransactionId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentTransactionId)
            .WithErrorMessage("Payment transaction ID is required.");
    }

    #endregion

    #region Notes Validation

    [Fact]
    public async Task Validate_WhenNotesExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenNotesIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    #endregion
}
