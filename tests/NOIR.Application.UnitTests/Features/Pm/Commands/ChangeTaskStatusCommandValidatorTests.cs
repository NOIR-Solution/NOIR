using NOIR.Application.Features.Pm.Commands.ChangeTaskStatus;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ChangeTaskStatusCommandValidatorTests
{
    private readonly ChangeTaskStatusCommandValidator _validator;

    public ChangeTaskStatusCommandValidatorTests()
    {
        _validator = new ChangeTaskStatusCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskStatusCommand(Guid.NewGuid(), ProjectTaskStatus.InProgress);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskStatusCommand(Guid.Empty, ProjectTaskStatus.InProgress);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_InvalidStatus_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskStatusCommand(Guid.NewGuid(), (ProjectTaskStatus)999);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_DoneStatus_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskStatusCommand(Guid.NewGuid(), ProjectTaskStatus.Done);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
