using NOIR.Application.Features.Payments.Commands.CreatePayment;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for CreatePaymentCommandValidator.
/// Tests all validation rules for creating a payment.
/// </summary>
public class CreatePaymentCommandValidatorTests
{
    private readonly CreatePaymentCommandValidator _validator = new();

    private static CreatePaymentCommand CreateValidCommand() => new(
        OrderId: Guid.NewGuid(),
        Amount: 100.00m,
        Currency: "VND",
        PaymentMethod: PaymentMethod.EWallet,
        Provider: "MoMo",
        ReturnUrl: "https://example.com/return",
        IdempotencyKey: "key-123",
        Metadata: null);

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
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 0.01m,
            Currency: "USD",
            PaymentMethod: PaymentMethod.CreditCard,
            Provider: "Stripe",
            ReturnUrl: null,
            IdempotencyKey: null,
            Metadata: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region OrderId Validation

    [Fact]
    public async Task Validate_WhenOrderIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    #endregion

    #region Amount Validation

    [Fact]
    public async Task Validate_WhenAmountIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenAmountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = -10m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenAmountIsPositive_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = 0.01m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    #endregion

    #region Currency Validation

    [Fact]
    public async Task Validate_WhenCurrencyIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required.");
    }

    [Fact]
    public async Task Validate_WhenCurrencyExceeds3Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = "ABCD" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Us1")]
    [InlineData("12A")]
    [InlineData("AB")]
    [InlineData("A")]
    public async Task Validate_WhenCurrencyHasInvalidFormat_ShouldHaveError(string currency)
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = currency };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be a valid 3-letter ISO code.");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("VND")]
    [InlineData("EUR")]
    public async Task Validate_WhenCurrencyIsValid_ShouldNotHaveError(string currency)
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = currency };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    #endregion

    #region PaymentMethod Validation

    [Fact]
    public async Task Validate_WhenPaymentMethodIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PaymentMethod = (PaymentMethod)999 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("Invalid payment method.");
    }

    [Fact]
    public async Task Validate_WhenPaymentMethodIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PaymentMethod = PaymentMethod.BankTransfer };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PaymentMethod);
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

    #region ReturnUrl Validation

    [Fact]
    public async Task Validate_WhenReturnUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ReturnUrl = new string('a', 2001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReturnUrl)
            .WithErrorMessage("Return URL cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenReturnUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ReturnUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReturnUrl);
    }

    [Fact]
    public async Task Validate_WhenReturnUrlIs2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ReturnUrl = new string('a', 2000) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReturnUrl);
    }

    #endregion

    #region IdempotencyKey Validation

    [Fact]
    public async Task Validate_WhenIdempotencyKeyExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { IdempotencyKey = new string('k', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage("Idempotency key cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenIdempotencyKeyIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { IdempotencyKey = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public async Task Validate_WhenIdempotencyKeyIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { IdempotencyKey = new string('k', 100) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    #endregion
}
