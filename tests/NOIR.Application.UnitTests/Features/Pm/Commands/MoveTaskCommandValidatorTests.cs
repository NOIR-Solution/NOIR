using NOIR.Application.Features.Pm.Commands.MoveTask;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class MoveTaskCommandValidatorTests
{
    private readonly MoveTaskCommandValidator _validator;

    public MoveTaskCommandValidatorTests()
    {
        _validator = new MoveTaskCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 1.0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new MoveTaskCommand(Guid.Empty, Guid.NewGuid(), 1.0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyColumnId_ShouldFail()
    {
        // Arrange
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.Empty, 1.0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColumnId);
    }

    [Fact]
    public void Validate_ZeroSortOrder_ShouldPass()
    {
        // Arrange
        var command = new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
