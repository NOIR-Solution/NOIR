using NOIR.Application.Features.Cart.Commands.AddToCart;

namespace NOIR.Application.UnitTests.Features.Cart.Commands.AddToCart;

/// <summary>
/// Unit tests for AddToCartCommandValidator.
/// </summary>
public class AddToCartCommandValidatorTests
{
    private readonly AddToCartCommandValidator _validator = new();

    private static AddToCartCommand CreateValidCommand() =>
        new(ProductId: Guid.NewGuid(), ProductVariantId: Guid.NewGuid(), Quantity: 1)
        {
            UserId = "user-123"
        };

    [Fact]
    public void Validate_WithValidCommand_WithUserId_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_WithSessionId_ShouldPass()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 5)
        {
            SessionId = "session-abc"
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyProductId_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required");
    }

    [Fact]
    public void Validate_WithEmptyProductVariantId_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductVariantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductVariantId)
            .WithErrorMessage("Product Variant ID is required");
    }

    [Fact]
    public void Validate_WithZeroQuantity_ShouldFail()
    {
        var command = CreateValidCommand() with { Quantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ShouldFail()
    {
        var command = CreateValidCommand() with { Quantity = -1 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be greater than 0");
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

    [Fact]
    public void Validate_WithNeitherUserIdNorSessionId_ShouldFail()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 1)
        {
            UserId = null,
            SessionId = null
        };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Either UserId or SessionId must be provided");
    }

    [Fact]
    public void Validate_WithEmptyUserIdAndEmptySessionId_ShouldFail()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 1)
        {
            UserId = "",
            SessionId = ""
        };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Either UserId or SessionId must be provided");
    }

    [Fact]
    public void Validate_WithBothUserIdAndSessionId_ShouldPass()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 2)
        {
            UserId = "user-123",
            SessionId = "session-abc"
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
