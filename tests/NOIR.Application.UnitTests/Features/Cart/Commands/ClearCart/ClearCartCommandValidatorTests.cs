using NOIR.Application.Features.Cart.Commands.ClearCart;

namespace NOIR.Application.UnitTests.Features.Cart.Commands.ClearCart;

/// <summary>
/// Unit tests for ClearCartCommandValidator.
/// </summary>
public class ClearCartCommandValidatorTests
{
    private readonly ClearCartCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCartId_ShouldPass()
    {
        var command = new ClearCartCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCartId_ShouldFail()
    {
        var command = new ClearCartCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CartId)
            .WithErrorMessage("Cart ID is required");
    }
}
