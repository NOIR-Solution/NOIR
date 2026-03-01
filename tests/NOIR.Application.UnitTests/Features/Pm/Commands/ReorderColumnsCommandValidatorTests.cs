using NOIR.Application.Features.Pm.Commands.ReorderColumns;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ReorderColumnsCommandValidatorTests
{
    private readonly ReorderColumnsCommandValidator _validator;

    public ReorderColumnsCommandValidatorTests()
    {
        _validator = new ReorderColumnsCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new ReorderColumnsCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new ReorderColumnsCommand(Guid.Empty, [Guid.NewGuid()]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyColumnIds_ShouldFail()
    {
        // Arrange
        var command = new ReorderColumnsCommand(Guid.NewGuid(), []);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColumnIds);
    }

    [Fact]
    public void Validate_SingleColumnId_ShouldPass()
    {
        // Arrange
        var command = new ReorderColumnsCommand(Guid.NewGuid(), [Guid.NewGuid()]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
