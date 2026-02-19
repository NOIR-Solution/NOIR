using NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SelectShippingMethod;

/// <summary>
/// Unit tests for SelectShippingMethodCommandValidator.
/// </summary>
public class SelectShippingMethodCommandValidatorTests
{
    private readonly SelectShippingMethodCommandValidator _validator = new();

    private static SelectShippingMethodCommand CreateValidCommand() =>
        new(SessionId: Guid.NewGuid(), ShippingMethod: "Express Delivery", ShippingCost: 30000m);

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithZeroShippingCost_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingCost = 0m };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- SessionId ---

    [Fact]
    public void Validate_WithEmptySessionId_ShouldFail()
    {
        var command = CreateValidCommand() with { SessionId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    // --- ShippingMethod ---

    [Fact]
    public void Validate_WithEmptyShippingMethod_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingMethod = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingMethod)
            .WithErrorMessage("Shipping method is required.");
    }

    [Fact]
    public void Validate_WithNullShippingMethod_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingMethod = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingMethod)
            .WithErrorMessage("Shipping method is required.");
    }

    [Fact]
    public void Validate_WithShippingMethodExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingMethod = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingMethod)
            .WithErrorMessage("Shipping method must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingMethodExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingMethod = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingMethod);
    }

    // --- ShippingCost ---

    [Fact]
    public void Validate_WithNegativeShippingCost_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingCost = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingCost)
            .WithErrorMessage("Shipping cost cannot be negative.");
    }

    [Fact]
    public void Validate_WithLargePositiveShippingCost_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingCost = 999999.99m };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingCost);
    }
}
