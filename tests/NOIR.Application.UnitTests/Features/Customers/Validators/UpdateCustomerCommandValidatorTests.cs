namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for UpdateCustomerCommandValidator.
/// Tests all validation rules for updating a customer.
/// </summary>
public class UpdateCustomerCommandValidatorTests
{
    private readonly UpdateCustomerCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.Empty,
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Customer ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Email Validation

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "invalid-email",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public async Task Validate_WhenEmailExceeds256Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 300) + "@test.com"; // 309 chars, well over 256 limit
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: longEmail,
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region FirstName Validation

    [Fact]
    public async Task Validate_WhenFirstNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "",
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required.");
    }

    [Fact]
    public async Task Validate_WhenFirstNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: new string('A', 101),
            LastName: "Doe");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name cannot exceed 100 characters.");
    }

    #endregion

    #region LastName Validation

    [Fact]
    public async Task Validate_WhenLastNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required.");
    }

    [Fact]
    public async Task Validate_WhenLastNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: new string('A', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name cannot exceed 100 characters.");
    }

    #endregion

    #region Phone Validation

    [Fact]
    public async Task Validate_WhenPhoneExceeds20Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe",
            Phone: new string('0', 21));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone number cannot exceed 20 characters.");
    }

    [Fact]
    public async Task Validate_WhenPhoneIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe",
            Phone: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    #endregion

    #region Tags Validation

    [Fact]
    public async Task Validate_WhenTagsExceed1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe",
            Tags: new string('a', 1001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tags)
            .WithErrorMessage("Tags cannot exceed 1000 characters.");
    }

    #endregion

    #region Notes Validation

    [Fact]
    public async Task Validate_WhenNotesExceed2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe",
            Notes: new string('a', 2001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 2000 characters.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateCustomerCommand(
            Id: Guid.NewGuid(),
            Email: "john@example.com",
            FirstName: "John",
            LastName: "Doe",
            Phone: "0901234567",
            Tags: "vip",
            Notes: "Updated");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
