using NOIR.Application.Features.Orders.Commands.ReturnOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ReturnOrder;

/// <summary>
/// Unit tests for ReturnOrderCommandValidator.
/// </summary>
public class ReturnOrderCommandValidatorTests
{
    private readonly ReturnOrderCommandValidator _validator;

    public ReturnOrderCommandValidatorTests()
    {
        _validator = new ReturnOrderCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ReturnOrderCommand(Guid.NewGuid(), "Defective product");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithNullReason_ShouldPass()
    {
        // Arrange
        var command = new ReturnOrderCommand(Guid.NewGuid(), null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new ReturnOrderCommand(Guid.Empty, "Reason");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    [Fact]
    public async Task Validate_WithReasonExceeding500Characters_ShouldFail()
    {
        // Arrange
        var longReason = new string('A', 501);
        var command = new ReturnOrderCommand(Guid.NewGuid(), longReason);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Return reason cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithReasonExactly500Characters_ShouldPass()
    {
        // Arrange
        var reason = new string('A', 500);
        var command = new ReturnOrderCommand(Guid.NewGuid(), reason);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyReason_ShouldPass()
    {
        // Arrange
        var command = new ReturnOrderCommand(Guid.NewGuid(), string.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
