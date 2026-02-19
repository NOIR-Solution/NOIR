using NOIR.Application.Features.Orders.Commands.CreateOrder;
using NOIR.Application.Features.Orders.DTOs;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Unit tests for CreateOrderCommandValidator, AddressValidator, and OrderItemValidator.
/// </summary>
public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateAddressDto CreateValidAddress() =>
        new(
            FullName: "Nguyen Van A",
            Phone: "+84123456789",
            AddressLine1: "123 Le Loi Street",
            AddressLine2: "Floor 5",
            Ward: "Ben Nghe",
            District: "District 1",
            Province: "Ho Chi Minh City",
            Country: "Vietnam",
            PostalCode: "700000");

    private static CreateOrderItemDto CreateValidItem() =>
        new(
            ProductId: Guid.NewGuid(),
            ProductVariantId: Guid.NewGuid(),
            ProductName: "Laptop Pro 15",
            VariantName: "Silver 16GB",
            UnitPrice: 25000000m,
            Quantity: 1);

    private static CreateOrderCommand CreateValidCommand() =>
        new(
            CustomerEmail: "customer@example.com",
            CustomerName: "Nguyen Van A",
            CustomerPhone: "+84123456789",
            ShippingAddress: CreateValidAddress(),
            BillingAddress: null,
            ShippingMethod: "Express",
            ShippingAmount: 30000m,
            CouponCode: null,
            DiscountAmount: 0m,
            CustomerNotes: null,
            Items: [CreateValidItem()],
            Currency: "VND");

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMultipleValidItems_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Items = [CreateValidItem(), CreateValidItem(), CreateValidItem()]
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithBillingAddress_ShouldPass()
    {
        var command = CreateValidCommand() with { BillingAddress = CreateValidAddress() };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- CustomerEmail ---

    [Fact]
    public void Validate_WithEmptyCustomerEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email is required.");
    }

    [Fact]
    public void Validate_WithNullCustomerEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email is required.");
    }

    [Fact]
    public void Validate_WithInvalidCustomerEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = "not-an-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email must be a valid email address.");
    }

    [Fact]
    public void Validate_WithCustomerEmailExceeding256Characters_ShouldFail()
    {
        var longEmail = new string('a', 245) + "@example.com"; // 257 chars
        var command = CreateValidCommand() with { CustomerEmail = longEmail };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email cannot exceed 256 characters.");
    }

    // --- CustomerName (optional) ---

    [Fact]
    public void Validate_WithNullCustomerName_ShouldPass()
    {
        var command = CreateValidCommand() with { CustomerName = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerName);
    }

    [Fact]
    public void Validate_WithCustomerNameExceeding200Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerName = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerName)
            .WithErrorMessage("Customer name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithCustomerNameExactly200Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { CustomerName = new string('A', 200) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerName);
    }

    // --- CustomerPhone (optional) ---

    [Fact]
    public void Validate_WithNullCustomerPhone_ShouldPass()
    {
        var command = CreateValidCommand() with { CustomerPhone = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerPhone);
    }

    [Fact]
    public void Validate_WithCustomerPhoneExceeding20Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerPhone = new string('1', 21) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerPhone)
            .WithErrorMessage("Customer phone cannot exceed 20 characters.");
    }

    // --- ShippingAddress ---

    [Fact]
    public void Validate_WithNullShippingAddress_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingAddress = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress)
            .WithErrorMessage("Shipping address is required.");
    }

    // --- ShippingMethod (optional) ---

    [Fact]
    public void Validate_WithNullShippingMethod_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingMethod = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingMethod);
    }

    [Fact]
    public void Validate_WithShippingMethodExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingMethod = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingMethod)
            .WithErrorMessage("Shipping method cannot exceed 100 characters.");
    }

    // --- ShippingAmount ---

    [Fact]
    public void Validate_WithNegativeShippingAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingAmount = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAmount)
            .WithErrorMessage("Shipping amount must be non-negative.");
    }

    [Fact]
    public void Validate_WithZeroShippingAmount_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingAmount = 0m };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingAmount);
    }

    // --- DiscountAmount ---

    [Fact]
    public void Validate_WithNegativeDiscountAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { DiscountAmount = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DiscountAmount)
            .WithErrorMessage("Discount amount must be non-negative.");
    }

    [Fact]
    public void Validate_WithZeroDiscountAmount_ShouldPass()
    {
        var command = CreateValidCommand() with { DiscountAmount = 0m };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountAmount);
    }

    // --- CouponCode (optional) ---

    [Fact]
    public void Validate_WithNullCouponCode_ShouldPass()
    {
        var command = CreateValidCommand() with { CouponCode = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CouponCode);
    }

    [Fact]
    public void Validate_WithCouponCodeExceeding50Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { CouponCode = new string('A', 51) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CouponCode)
            .WithErrorMessage("Coupon code cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithCouponCodeExactly50Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { CouponCode = new string('A', 50) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CouponCode);
    }

    // --- CustomerNotes (optional) ---

    [Fact]
    public void Validate_WithNullCustomerNotes_ShouldPass()
    {
        var command = CreateValidCommand() with { CustomerNotes = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerNotes);
    }

    [Fact]
    public void Validate_WithCustomerNotesExceeding1000Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerNotes = new string('A', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerNotes)
            .WithErrorMessage("Customer notes cannot exceed 1000 characters.");
    }

    [Fact]
    public void Validate_WithCustomerNotesExactly1000Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { CustomerNotes = new string('A', 1000) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerNotes);
    }

    // --- Currency ---

    [Fact]
    public void Validate_WithEmptyCurrency_ShouldFail()
    {
        var command = CreateValidCommand() with { Currency = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required.");
    }

    [Fact]
    public void Validate_WithNullCurrency_ShouldFail()
    {
        var command = CreateValidCommand() with { Currency = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required.");
    }

    [Fact]
    public void Validate_WithCurrencyExceeding10Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Currency = new string('A', 11) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency cannot exceed 10 characters.");
    }

    // --- Items ---

    [Fact]
    public void Validate_WithEmptyItems_ShouldFail()
    {
        var command = CreateValidCommand() with { Items = [] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order must contain at least one item.");
    }

    [Fact]
    public void Validate_WithNullItems_ShouldFail()
    {
        var command = CreateValidCommand() with { Items = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithMoreThan100Items_ShouldFail()
    {
        var items = Enumerable.Range(0, 101).Select(_ => CreateValidItem()).ToList();
        var command = CreateValidCommand() with { Items = items };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order cannot contain more than 100 items.");
    }

    [Fact]
    public void Validate_WithExactly100Items_ShouldPass()
    {
        var items = Enumerable.Range(0, 100).Select(_ => CreateValidItem()).ToList();
        var command = CreateValidCommand() with { Items = items };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    // --- Item Validation (OrderItemValidator via RuleForEach) ---

    [Fact]
    public void Validate_WithItemEmptyProductId_ShouldFail()
    {
        var badItem = CreateValidItem() with { ProductId = Guid.Empty };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Product ID is required.");
    }

    [Fact]
    public void Validate_WithItemEmptyProductVariantId_ShouldFail()
    {
        var badItem = CreateValidItem() with { ProductVariantId = Guid.Empty };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Product variant ID is required.");
    }

    [Fact]
    public void Validate_WithItemEmptyProductName_ShouldFail()
    {
        var badItem = CreateValidItem() with { ProductName = "" };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Product name is required.");
    }

    [Fact]
    public void Validate_WithItemProductNameExceeding200Characters_ShouldFail()
    {
        var badItem = CreateValidItem() with { ProductName = new string('A', 201) };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Product name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithItemEmptyVariantName_ShouldFail()
    {
        var badItem = CreateValidItem() with { VariantName = "" };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Variant name is required.");
    }

    [Fact]
    public void Validate_WithItemVariantNameExceeding100Characters_ShouldFail()
    {
        var badItem = CreateValidItem() with { VariantName = new string('A', 101) };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Variant name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithItemNegativeUnitPrice_ShouldFail()
    {
        var badItem = CreateValidItem() with { UnitPrice = -1m };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Unit price must be non-negative.");
    }

    [Fact]
    public void Validate_WithItemZeroUnitPrice_ShouldPass()
    {
        var freeItem = CreateValidItem() with { UnitPrice = 0m };
        var command = CreateValidCommand() with { Items = [freeItem] };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithItemZeroQuantity_ShouldFail()
    {
        var badItem = CreateValidItem() with { Quantity = 0 };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Quantity must be greater than zero.");
    }

    [Fact]
    public void Validate_WithItemNegativeQuantity_ShouldFail()
    {
        var badItem = CreateValidItem() with { Quantity = -1 };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Quantity must be greater than zero.");
    }

    [Fact]
    public void Validate_WithItemQuantityExceeding1000_ShouldFail()
    {
        var badItem = CreateValidItem() with { Quantity = 1001 };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Quantity cannot exceed 1000.");
    }

    [Fact]
    public void Validate_WithItemQuantityExactly1000_ShouldPass()
    {
        var item = CreateValidItem() with { Quantity = 1000 };
        var command = CreateValidCommand() with { Items = [item] };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithItemSkuExceeding50Characters_ShouldFail()
    {
        var badItem = CreateValidItem() with { Sku = new string('A', 51) };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="SKU cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithItemImageUrlExceeding500Characters_ShouldFail()
    {
        var badItem = CreateValidItem() with { ImageUrl = new string('A', 501) };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Image URL cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithItemOptionsSnapshotExceeding500Characters_ShouldFail()
    {
        var badItem = CreateValidItem() with { OptionsSnapshot = new string('A', 501) };
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Options snapshot cannot exceed 500 characters.");
    }

    // --- Shipping Address Validation (AddressValidator via SetValidator) ---

    [Fact]
    public void Validate_WithShippingAddressEmptyFullName_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Full name is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressFullNameExceeding100Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { FullName = new string('A', 101) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Full name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyPhone_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Phone = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Phone number is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressPhoneExceeding20Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Phone = new string('1', 21) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Phone number cannot exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyAddressLine1_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { AddressLine1 = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Address line 1 is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressAddressLine1Exceeding200Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { AddressLine1 = new string('A', 201) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Address line 1 cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressLine2Exceeding200Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { AddressLine2 = new string('A', 201) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Address line 2 cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyWard_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Ward = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Ward is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressWardExceeding100Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Ward = new string('A', 101) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Ward cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyDistrict_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { District = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="District is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressDistrictExceeding100Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { District = new string('A', 101) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="District cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyProvince_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Province = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Province is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressProvinceExceeding100Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Province = new string('A', 101) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Province cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressEmptyCountry_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Country = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Country is required.");
    }

    [Fact]
    public void Validate_WithShippingAddressCountryExceeding100Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { Country = new string('A', 101) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Country cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithShippingAddressPostalCodeExceeding20Characters_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { PostalCode = new string('1', 21) };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Postal code cannot exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithNullShippingAddressPostalCode_ShouldPass()
    {
        var address = CreateValidAddress() with { PostalCode = null };
        var command = CreateValidCommand() with { ShippingAddress = address };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- Billing Address Validation (only validated when not null) ---

    [Fact]
    public void Validate_WithBillingAddressEmptyFullName_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { BillingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage =="Full name is required.");
    }

    [Fact]
    public void Validate_WithNullBillingAddress_ShouldPass()
    {
        var command = CreateValidCommand() with { BillingAddress = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.BillingAddress);
    }
}
