using NOIR.Application.Features.Orders.Commands.ConfirmOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Unit tests for ConfirmOrderCommandValidator.
/// </summary>
public class ConfirmOrderCommandValidatorTests
{
    private readonly ConfirmOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidOrderId_ShouldPass()
    {
        var command = new ConfirmOrderCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = new ConfirmOrderCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }
}
