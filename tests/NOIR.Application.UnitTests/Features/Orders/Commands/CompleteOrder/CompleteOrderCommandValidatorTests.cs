using NOIR.Application.Features.Orders.Commands.CompleteOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.CompleteOrder;

/// <summary>
/// Unit tests for CompleteOrderCommandValidator.
/// </summary>
public class CompleteOrderCommandValidatorTests
{
    private readonly CompleteOrderCommandValidator _validator;

    public CompleteOrderCommandValidatorTests()
    {
        _validator = new CompleteOrderCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidOrderId_ShouldPass()
    {
        // Arrange
        var command = new CompleteOrderCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new CompleteOrderCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }
}
