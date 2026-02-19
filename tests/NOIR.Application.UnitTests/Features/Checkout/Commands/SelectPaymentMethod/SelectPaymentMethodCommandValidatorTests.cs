using NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SelectPaymentMethod;

/// <summary>
/// Unit tests for SelectPaymentMethodCommandValidator.
/// </summary>
public class SelectPaymentMethodCommandValidatorTests
{
    private readonly SelectPaymentMethodCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_COD_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.COD);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_CreditCard_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.CreditCard);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_BankTransfer_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.BankTransfer);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_EWallet_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.EWallet);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_QRCode_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.QRCode);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_DebitCard_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.DebitCard);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_Installment_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.Installment);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_BuyNowPayLater_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), PaymentMethod.BuyNowPayLater);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- SessionId ---

    [Fact]
    public void Validate_WithEmptySessionId_ShouldFail()
    {
        var command = new SelectPaymentMethodCommand(Guid.Empty, PaymentMethod.COD);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    // --- PaymentMethod ---

    [Fact]
    public void Validate_WithInvalidPaymentMethod_ShouldFail()
    {
        var command = new SelectPaymentMethodCommand(Guid.NewGuid(), (PaymentMethod)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("Invalid payment method.");
    }

    [Fact]
    public void Validate_WithOptionalPaymentGatewayId_ShouldPass()
    {
        var command = new SelectPaymentMethodCommand(
            Guid.NewGuid(), PaymentMethod.CreditCard, PaymentGatewayId: Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
