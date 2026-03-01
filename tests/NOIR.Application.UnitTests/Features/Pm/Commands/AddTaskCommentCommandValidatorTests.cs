using NOIR.Application.Features.Pm.Commands.AddTaskComment;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddTaskCommentCommandValidatorTests
{
    private readonly AddTaskCommentCommandValidator _validator;

    public AddTaskCommentCommandValidatorTests()
    {
        _validator = new AddTaskCommentCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new AddTaskCommentCommand(Guid.NewGuid(), "This is a comment");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new AddTaskCommentCommand(Guid.Empty, "This is a comment");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Validate_EmptyContent_ShouldFail()
    {
        // Arrange
        var command = new AddTaskCommentCommand(Guid.NewGuid(), "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new AddTaskCommentCommand(Guid.NewGuid(), new string('A', 5001));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
}
