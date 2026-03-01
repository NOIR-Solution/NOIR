using NOIR.Application.Features.Pm.Commands.UpdateProject;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator;

    public UpdateProjectCommandValidatorTests()
    {
        _validator = new UpdateProjectCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Updated Project");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.Empty, "Updated Project");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), new string('A', 201));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Description: new string('A', 2001));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_CurrencyExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Currency: "ABCD");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Validate_NegativeBudget_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Budget: -100m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Budget);
    }

    [Fact]
    public void Validate_ZeroBudget_ShouldPass()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Budget: 0m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Budget);
    }

    [Fact]
    public void Validate_ColorExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Color: new string('A', 21));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_IconExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Name", Icon: new string('A', 51));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Icon);
    }
}
