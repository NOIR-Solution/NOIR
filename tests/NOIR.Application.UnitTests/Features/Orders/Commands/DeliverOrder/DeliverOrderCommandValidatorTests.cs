using NOIR.Application.Features.Orders.Commands.DeliverOrder;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.DeliverOrder;

/// <summary>
/// Unit tests for DeliverOrderCommandValidator.
/// </summary>
public class DeliverOrderCommandValidatorTests
{
    private readonly DeliverOrderCommandValidator _validator;

    public DeliverOrderCommandValidatorTests()
    {
        _validator = new DeliverOrderCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidOrderId_ShouldPass()
    {
        // Arrange
        var command = new DeliverOrderCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new DeliverOrderCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }
}
