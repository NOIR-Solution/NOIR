namespace NOIR.Application.UnitTests.Features.Payments.Validators;

using NOIR.Application.Features.Payments.Commands.TestGatewayConnection;

/// <summary>
/// Unit tests for TestGatewayConnectionCommandValidator.
/// </summary>
public class TestGatewayConnectionCommandValidatorTests
{
    private readonly TestGatewayConnectionCommandValidator _validator;

    public TestGatewayConnectionCommandValidatorTests()
    {
        _validator = new TestGatewayConnectionCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenGatewayIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new TestGatewayConnectionCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GatewayId)
            .WithErrorMessage("Gateway ID is required.");
    }

    [Fact]
    public async Task Validate_WhenGatewayIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new TestGatewayConnectionCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
