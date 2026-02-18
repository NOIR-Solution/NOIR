namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for AddCustomerAddressCommandValidator.
/// Tests all validation rules for adding a customer address.
/// </summary>
public class AddCustomerAddressCommandValidatorTests
{
    private readonly AddCustomerAddressCommandValidator _validator = new();

    private static AddCustomerAddressCommand CreateValidCommand(
        Guid? customerId = null,
        AddressType addressType = AddressType.Shipping,
        string fullName = "John Doe",
        string phone = "0901234567",
        string addressLine1 = "123 Main St",
        string province = "Ho Chi Minh",
        string? addressLine2 = null,
        string? ward = null,
        string? district = null,
        string? postalCode = null,
        bool isDefault = false)
    {
        return new AddCustomerAddressCommand(
            customerId ?? Guid.NewGuid(),
            addressType,
            fullName,
            phone,
            addressLine1,
            province,
            addressLine2,
            ward,
            district,
            postalCode,
            isDefault);
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

    [Theory]
    [InlineData(AddressType.Shipping)]
    [InlineData(AddressType.Billing)]
    public async Task Validate_WhenAddressTypeIsValid_ShouldNotHaveError(AddressType addressType)
    {
        // Arrange
        var command = CreateValidCommand(addressType: addressType);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AddressType);
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

    [Fact]
    public async Task Validate_WhenAddressLine1Exceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressLine1: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 cannot exceed 200 characters.");
    }

    #endregion

    #region AddressLine2 Validation

    [Fact]
    public async Task Validate_WhenAddressLine2Exceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressLine2: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine2)
            .WithErrorMessage("Address line 2 cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenAddressLine2IsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(addressLine2: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine2);
    }

    #endregion

    #region Ward Validation

    [Fact]
    public async Task Validate_WhenWardExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(ward: new string('A', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Ward)
            .WithErrorMessage("Ward cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenWardIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(ward: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Ward);
    }

    #endregion

    #region District Validation

    [Fact]
    public async Task Validate_WhenDistrictExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(district: new string('A', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.District)
            .WithErrorMessage("District cannot exceed 100 characters.");
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

    [Fact]
    public async Task Validate_WhenProvinceExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(province: new string('A', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Province)
            .WithErrorMessage("Province cannot exceed 100 characters.");
    }

    #endregion

    #region PostalCode Validation

    [Fact]
    public async Task Validate_WhenPostalCodeExceeds20Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(postalCode: new string('0', 21));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code cannot exceed 20 characters.");
    }

    [Fact]
    public async Task Validate_WhenPostalCodeIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(postalCode: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand(
            addressLine2: "Apt 4B",
            ward: "Ward 1",
            district: "District 1",
            postalCode: "70000",
            isDefault: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
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
