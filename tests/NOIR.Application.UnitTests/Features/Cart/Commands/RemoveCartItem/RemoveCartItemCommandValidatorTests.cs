using NOIR.Application.Features.Cart.Commands.RemoveCartItem;

namespace NOIR.Application.UnitTests.Features.Cart.Commands.RemoveCartItem;

/// <summary>
/// Unit tests for RemoveCartItemCommandValidator.
/// </summary>
public class RemoveCartItemCommandValidatorTests
{
    private readonly RemoveCartItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new RemoveCartItemCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCartId_ShouldFail()
    {
        var command = new RemoveCartItemCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CartId)
            .WithErrorMessage("Cart ID is required");
    }

    [Fact]
    public void Validate_WithEmptyItemId_ShouldFail()
    {
        var command = new RemoveCartItemCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage("Item ID is required");
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ShouldFail()
    {
        var command = new RemoveCartItemCommand(Guid.Empty, Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CartId);
        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }
}
