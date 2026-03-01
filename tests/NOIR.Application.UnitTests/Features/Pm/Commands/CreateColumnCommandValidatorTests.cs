using NOIR.Application.Features.Pm.Commands.CreateColumn;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateColumnCommandValidatorTests
{
    private readonly CreateColumnCommandValidator _validator;

    public CreateColumnCommandValidatorTests()
    {
        _validator = new CreateColumnCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "In Progress");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.Empty, "In Progress");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), new string('A', 101));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ColorExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", Color: new string('A', 21));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_NullColor_ShouldPass()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", Color: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_ZeroWipLimit_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", WipLimit: 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WipLimit);
    }

    [Fact]
    public void Validate_NegativeWipLimit_ShouldFail()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", WipLimit: -1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WipLimit);
    }

    [Fact]
    public void Validate_PositiveWipLimit_ShouldPass()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", WipLimit: 5);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullWipLimit_ShouldPass()
    {
        // Arrange
        var command = new CreateColumnCommand(Guid.NewGuid(), "Todo", WipLimit: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WipLimit);
    }
}
