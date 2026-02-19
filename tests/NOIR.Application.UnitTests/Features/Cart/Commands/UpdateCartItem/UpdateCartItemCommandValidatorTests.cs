using NOIR.Application.Features.Cart.Commands.UpdateCartItem;

namespace NOIR.Application.UnitTests.Features.Cart.Commands.UpdateCartItem;

/// <summary>
/// Unit tests for UpdateCartItemCommandValidator.
/// </summary>
public class UpdateCartItemCommandValidatorTests
{
    private readonly UpdateCartItemCommandValidator _validator = new();

    private static UpdateCartItemCommand CreateValidCommand() =>
        new(CartId: Guid.NewGuid(), ItemId: Guid.NewGuid(), Quantity: 2);

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCartId_ShouldFail()
    {
        var command = CreateValidCommand() with { CartId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CartId)
            .WithErrorMessage("Cart ID is required");
    }

    [Fact]
    public void Validate_WithEmptyItemId_ShouldFail()
    {
        var command = CreateValidCommand() with { ItemId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage("Item ID is required");
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ShouldFail()
    {
        var command = CreateValidCommand() with { Quantity = -1 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity cannot be negative");
    }

    [Fact]
    public void Validate_WithZeroQuantity_ShouldPass()
    {
        // Zero quantity is allowed (used to remove item)
        var command = CreateValidCommand() with { Quantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithQuantityExceeding100_ShouldFail()
    {
        var command = CreateValidCommand() with { Quantity = 101 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity cannot exceed 100");
    }

    [Fact]
    public void Validate_WithQuantityExactly100_ShouldPass()
    {
        var command = CreateValidCommand() with { Quantity = 100 };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WithQuantityExactly1_ShouldPass()
    {
        var command = CreateValidCommand() with { Quantity = 1 };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }
}
