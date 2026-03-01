using NOIR.Application.Features.Pm.Commands.AddLabelToTask;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddLabelToTaskCommandValidatorTests
{
    private readonly AddLabelToTaskCommandValidator _validator;

    public AddLabelToTaskCommandValidatorTests()
    {
        _validator = new AddLabelToTaskCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new AddLabelToTaskCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new AddLabelToTaskCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Validate_EmptyLabelId_ShouldFail()
    {
        // Arrange
        var command = new AddLabelToTaskCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }
}
