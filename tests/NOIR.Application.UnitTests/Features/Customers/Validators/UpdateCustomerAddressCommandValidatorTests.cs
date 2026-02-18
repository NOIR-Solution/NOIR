namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for UpdateCustomerAddressCommandValidator.
/// Tests all validation rules for updating a customer address.
/// </summary>
public class UpdateCustomerAddressCommandValidatorTests
{
    private readonly UpdateCustomerAddressCommandValidator _validator = new();

    private static UpdateCustomerAddressCommand CreateValidCommand(
        Guid? customerId = null,
        Guid? addressId = null,
        AddressType addressType = AddressType.Shipping,
        string fullName = "John Doe",
        string phone = "0901234567",
        string addressLine1 = "123 Main St",
        string province = "Ho Chi Minh")
    {
        return new UpdateCustomerAddressCommand(
            customerId ?? Guid.NewGuid(),
            addressId ?? Guid.NewGuid(),
            addressType,
            fullName,
            phone,
            addressLine1,
            province);
    }

    #region CustomerId Validation

    [Fact]
    public async Task Validate_WhenCustomerIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(customerId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required.");
    }

    #endregion

    #region AddressId Validation

    [Fact]
    public async Task Validate_WhenAddressIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressId)
            .WithErrorMessage("Address ID is required.");
    }

    #endregion

    #region AddressType Validation

    [Fact]
    public async Task Validate_WhenAddressTypeIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressType: (AddressType)999);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressType)
            .WithErrorMessage("Invalid address type.");
    }

    #endregion

    #region FullName Validation

    [Fact]
    public async Task Validate_WhenFullNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fullName: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name is required.");
    }

    [Fact]
    public async Task Validate_WhenFullNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fullName: new string('A', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name cannot exceed 100 characters.");
    }

    #endregion

    #region Phone Validation

    [Fact]
    public async Task Validate_WhenPhoneIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(phone: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone number is required.");
    }

    [Fact]
    public async Task Validate_WhenPhoneExceeds20Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(phone: new string('0', 21));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone number cannot exceed 20 characters.");
    }

    #endregion

    #region AddressLine1 Validation

    [Fact]
    public async Task Validate_WhenAddressLine1IsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressLine1: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 is required.");
    }

    #endregion

    #region Province Validation

    [Fact]
    public async Task Validate_WhenProvinceIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(province: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Province)
            .WithErrorMessage("Province is required.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
