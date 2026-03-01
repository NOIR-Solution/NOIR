using NOIR.Application.Features.Pm.Commands.DeleteTask;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteTaskCommandValidatorTests
{
    private readonly DeleteTaskCommandValidator _validator;

    public DeleteTaskCommandValidatorTests()
    {
        _validator = new DeleteTaskCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new DeleteTaskCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
