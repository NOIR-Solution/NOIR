using NOIR.Application.Features.Pm.Commands.RemoveLabelFromTask;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class RemoveLabelFromTaskCommandValidatorTests
{
    private readonly RemoveLabelFromTaskCommandValidator _validator;

    public RemoveLabelFromTaskCommandValidatorTests()
    {
        _validator = new RemoveLabelFromTaskCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new RemoveLabelFromTaskCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new RemoveLabelFromTaskCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Validate_EmptyLabelId_ShouldFail()
    {
        // Arrange
        var command = new RemoveLabelFromTaskCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }
}
