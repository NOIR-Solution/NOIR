using NOIR.Application.Features.Orders.Commands.ShipOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ShipOrder;

/// <summary>
/// Unit tests for ShipOrderCommandValidator.
/// </summary>
public class ShipOrderCommandValidatorTests
{
    private readonly ShipOrderCommandValidator _validator = new();

    private static ShipOrderCommand CreateValidCommand() =>
        new(OrderId: Guid.NewGuid(), TrackingNumber: "VN123456789", ShippingCarrier: "Vietnam Post");

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- OrderId ---

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    // --- TrackingNumber ---

    [Fact]
    public void Validate_WithEmptyTrackingNumber_ShouldFail()
    {
        var command = CreateValidCommand() with { TrackingNumber = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber)
            .WithErrorMessage("Tracking number is required.");
    }

    [Fact]
    public void Validate_WithNullTrackingNumber_ShouldFail()
    {
        var command = CreateValidCommand() with { TrackingNumber = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber)
            .WithErrorMessage("Tracking number is required.");
    }

    [Fact]
    public void Validate_WithTrackingNumberExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { TrackingNumber = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber)
            .WithErrorMessage("Tracking number cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithTrackingNumberExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { TrackingNumber = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TrackingNumber);
    }

    // --- ShippingCarrier ---

    [Fact]
    public void Validate_WithEmptyShippingCarrier_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingCarrier = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingCarrier)
            .WithErrorMessage("Shipping carrier is required.");
    }

    [Fact]
    public void Validate_WithNullShippingCarrier_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingCarrier = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingCarrier)
            .WithErrorMessage("Shipping carrier is required.");
    }

    [Fact]
    public void Validate_WithShippingCarrierExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingCarrier = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingCarrier)
            .WithErrorMessage("Shipping carrier cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingCarrierExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingCarrier = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingCarrier);
    }
}
