using NOIR.Application.Features.Orders.Commands.CancelOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Unit tests for CancelOrderCommandValidator.
/// </summary>
public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), "Customer changed their mind");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullReason_ShouldPass()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyReason_ShouldPass()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- OrderId ---

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = new CancelOrderCommand(Guid.Empty, "Some reason");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    // --- Reason (optional with max length) ---

    [Fact]
    public void Validate_WithReasonExceeding500Characters_ShouldFail()
    {
        var longReason = new string('A', 501);
        var command = new CancelOrderCommand(Guid.NewGuid(), longReason);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Cancellation reason cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithReasonExactly500Characters_ShouldPass()
    {
        var reason = new string('A', 500);
        var command = new CancelOrderCommand(Guid.NewGuid(), reason);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
