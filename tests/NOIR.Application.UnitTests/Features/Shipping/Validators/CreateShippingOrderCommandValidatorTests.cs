namespace NOIR.Application.UnitTests.Features.Shipping.Validators;

/// <summary>
/// Unit tests for CreateShippingOrderCommandValidator.
/// Tests all validation rules for creating a shipping order.
/// </summary>
public class CreateShippingOrderCommandValidatorTests
{
    private readonly CreateShippingOrderCommandValidator _validator = new();

    private static ShippingAddressDto CreateValidAddress(string prefix = "Test") => new(
        FullName: $"{prefix} User",
        Phone: "0901234567",
        Email: "test@example.com",
        AddressLine1: "123 Main St",
        AddressLine2: null,
        Ward: "Ward 1",
        WardCode: "W001",
        District: "District 1",
        DistrictCode: "D001",
        Province: "HCM",
        ProvinceCode: "P001",
        PostalCode: "70000",
        CountryCode: "VN");

    private static ShippingContactDto CreateValidContact(string name = "Test User") => new(
        FullName: name,
        Phone: "0901234567",
        Email: "test@example.com");

    private static List<ShippingItemDto> CreateValidItems() => new()
    {
        new ShippingItemDto("Product A", 2, 500m, 100000m, "SKU-001")
    };

    private static CreateShippingOrderCommand CreateValidCommand() => new(
        OrderId: Guid.NewGuid(),
        ProviderCode: ShippingProviderCode.GHTK,
        ServiceTypeCode: "standard",
        PickupAddress: CreateValidAddress("Pickup"),
        DeliveryAddress: CreateValidAddress("Delivery"),
        Sender: CreateValidContact("Sender"),
        Recipient: CreateValidContact("Recipient"),
        Items: CreateValidItems(),
        TotalWeightGrams: 1000m,
        DeclaredValue: 200000m,
        CodAmount: 200000m);

    #region Valid Command

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

    [Fact]
    public async Task Validate_WhenCodAmountIsNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand() with { CodAmount = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region OrderId Validation

    [Fact]
    public async Task Validate_WhenOrderIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    #endregion

    #region ProviderCode Validation

    [Fact]
    public async Task Validate_WhenProviderCodeIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProviderCode = (ShippingProviderCode)999 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProviderCode)
            .WithErrorMessage("Invalid shipping provider code.");
    }

    #endregion

    #region ServiceTypeCode Validation

    [Fact]
    public async Task Validate_WhenServiceTypeCodeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ServiceTypeCode = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServiceTypeCode)
            .WithErrorMessage("Service type code is required.");
    }

    #endregion

    #region PickupAddress Validation

    [Fact]
    public async Task Validate_WhenPickupAddressIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PickupAddress = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PickupAddress)
            .WithErrorMessage("Pickup address is required.");
    }

    [Fact]
    public async Task Validate_WhenPickupAddressFullNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var address = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { PickupAddress = address };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PickupAddress.FullName)
            .WithErrorMessage("Pickup address full name is required.");
    }

    [Fact]
    public async Task Validate_WhenPickupAddressPhoneIsEmpty_ShouldHaveError()
    {
        // Arrange
        var address = CreateValidAddress() with { Phone = "" };
        var command = CreateValidCommand() with { PickupAddress = address };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PickupAddress.Phone)
            .WithErrorMessage("Pickup address phone is required.");
    }

    #endregion

    #region DeliveryAddress Validation

    [Fact]
    public async Task Validate_WhenDeliveryAddressIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DeliveryAddress = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeliveryAddress)
            .WithErrorMessage("Delivery address is required.");
    }

    [Fact]
    public async Task Validate_WhenDeliveryAddressFullNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var address = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { DeliveryAddress = address };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeliveryAddress.FullName)
            .WithErrorMessage("Delivery address full name is required.");
    }

    [Fact]
    public async Task Validate_WhenDeliveryAddressPhoneIsEmpty_ShouldHaveError()
    {
        // Arrange
        var address = CreateValidAddress() with { Phone = "" };
        var command = CreateValidCommand() with { DeliveryAddress = address };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeliveryAddress.Phone)
            .WithErrorMessage("Delivery address phone is required.");
    }

    #endregion

    #region Sender/Recipient Validation

    [Fact]
    public async Task Validate_WhenSenderIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Sender = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sender)
            .WithErrorMessage("Sender information is required.");
    }

    [Fact]
    public async Task Validate_WhenRecipientIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Recipient = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recipient)
            .WithErrorMessage("Recipient information is required.");
    }

    #endregion

    #region Items Validation

    [Fact]
    public async Task Validate_WhenItemsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Items = new List<ShippingItemDto>() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one item is required.");
    }

    #endregion

    #region TotalWeightGrams Validation

    [Fact]
    public async Task Validate_WhenTotalWeightGramsIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TotalWeightGrams = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TotalWeightGrams)
            .WithErrorMessage("Total weight must be greater than 0.");
    }

    [Fact]
    public async Task Validate_WhenTotalWeightGramsIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TotalWeightGrams = -100 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TotalWeightGrams)
            .WithErrorMessage("Total weight must be greater than 0.");
    }

    #endregion

    #region DeclaredValue Validation

    [Fact]
    public async Task Validate_WhenDeclaredValueIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DeclaredValue = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeclaredValue)
            .WithErrorMessage("Declared value must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenDeclaredValueIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DeclaredValue = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DeclaredValue);
    }

    #endregion

    #region CodAmount Validation

    [Fact]
    public async Task Validate_WhenCodAmountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CodAmount = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CodAmount)
            .WithErrorMessage("COD amount must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenCodAmountIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CodAmount = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CodAmount);
    }

    #endregion
}
