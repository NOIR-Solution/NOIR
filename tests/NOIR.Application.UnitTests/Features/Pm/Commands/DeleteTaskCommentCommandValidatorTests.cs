using NOIR.Application.Features.Pm.Commands.DeleteTaskComment;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteTaskCommentCommandValidatorTests
{
    private readonly DeleteTaskCommentCommandValidator _validator;

    public DeleteTaskCommentCommandValidatorTests()
    {
        _validator = new DeleteTaskCommentCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new DeleteTaskCommentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskCommentCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaskId);
    }

    [Fact]
    public void Validate_EmptyCommentId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskCommentCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CommentId);
    }
}
