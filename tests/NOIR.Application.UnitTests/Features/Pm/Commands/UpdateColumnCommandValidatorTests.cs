using NOIR.Application.Features.Pm.Commands.UpdateColumn;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateColumnCommandValidatorTests
{
    private readonly UpdateColumnCommandValidator _validator;

    public UpdateColumnCommandValidatorTests()
    {
        _validator = new UpdateColumnCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "In Review");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.Empty, Guid.NewGuid(), "In Review");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyColumnId_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.Empty, "In Review");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColumnId);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), new string('A', 101));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ColorExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "Done", Color: new string('A', 21));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_ZeroWipLimit_ShouldFail()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "Done", WipLimit: 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WipLimit);
    }

    [Fact]
    public void Validate_PositiveWipLimit_ShouldPass()
    {
        // Arrange
        var command = new UpdateColumnCommand(Guid.NewGuid(), Guid.NewGuid(), "Done", WipLimit: 3);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
