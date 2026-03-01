using NOIR.Application.Features.Pm.Commands.DeleteTaskLabel;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteTaskLabelCommandValidatorTests
{
    private readonly DeleteTaskLabelCommandValidator _validator;

    public DeleteTaskLabelCommandValidatorTests()
    {
        _validator = new DeleteTaskLabelCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new DeleteTaskLabelCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskLabelCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyLabelId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskLabelCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }
}
