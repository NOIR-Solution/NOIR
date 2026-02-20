using NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;

namespace NOIR.Application.UnitTests.Features.Payments.Commands.RefreshPaymentStatus;

public class RefreshPaymentStatusCommandValidatorTests
{
    private readonly RefreshPaymentStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new RefreshPaymentStatusCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyPaymentTransactionId_ShouldFail()
    {
        var command = new RefreshPaymentStatusCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentTransactionId)
            .WithErrorMessage("Payment transaction ID is required.");
    }
}
