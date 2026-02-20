using NOIR.Application.Features.Orders.Commands.ManualCreateOrder;
using NOIR.Application.Features.Orders.DTOs;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ManualCreateOrder;

/// <summary>
/// Unit tests for ManualCreateOrderCommandValidator and ManualOrderItemValidator.
/// </summary>
public class ManualCreateOrderCommandValidatorTests
{
    private readonly ManualCreateOrderCommandValidator _validator = new();

    private static CreateAddressDto CreateValidAddress() =>
        new(
            FullName: "Nguyen Van A",
            Phone: "+84123456789",
            AddressLine1: "123 Le Loi Street",
            AddressLine2: null,
            Ward: "Ben Nghe",
            District: "District 1",
            Province: "Ho Chi Minh City",
            Country: "Vietnam",
            PostalCode: "700000");

    private static ManualOrderItemDto CreateValidItem() =>
        new(ProductVariantId: Guid.NewGuid(), Quantity: 1);

    private static ManualCreateOrderCommand CreateValidCommand() =>
        new(
            CustomerEmail: "customer@example.com",
            CustomerName: "Nguyen Van A",
            CustomerPhone: "+84123456789",
            CustomerId: null,
            Items: [CreateValidItem()],
            ShippingAddress: null,
            BillingAddress: null,
            ShippingMethod: null,
            CouponCode: null,
            CustomerNotes: null,
            InternalNotes: null,
            PaymentMethod: null,
            InitialPaymentStatus: null);

    // --- Valid Command ---

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            ShippingAddress = CreateValidAddress(),
            BillingAddress = CreateValidAddress(),
            ShippingMethod = "Express",
            CouponCode = "SAVE10",
            CustomerNotes = "Notes",
            InternalNotes = "Internal",
            ShippingAmount = 30m,
            DiscountAmount = 10m,
            TaxAmount = 5m
        };
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
        var longEmail = new string('a', 245) + "@example.com";
        var command = CreateValidCommand() with { CustomerEmail = longEmail };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email cannot exceed 256 characters.");
    }

    // --- CustomerName ---

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

    // --- CustomerPhone ---

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

    // --- Item Validation (ManualOrderItemValidator) ---

    [Fact]
    public void Validate_WithItemEmptyVariantId_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.Empty, 1);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Product variant ID is required.");
    }

    [Fact]
    public void Validate_WithItemZeroQuantity_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.NewGuid(), 0);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity must be greater than zero.");
    }

    [Fact]
    public void Validate_WithItemNegativeQuantity_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.NewGuid(), -1);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity must be greater than zero.");
    }

    [Fact]
    public void Validate_WithItemQuantityExceeding1000_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.NewGuid(), 1001);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Quantity cannot exceed 1000.");
    }

    [Fact]
    public void Validate_WithItemNegativeUnitPrice_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.NewGuid(), 1, UnitPrice: -1m);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Unit price must be non-negative.");
    }

    [Fact]
    public void Validate_WithItemNegativeDiscountAmount_ShouldFail()
    {
        var badItem = new ManualOrderItemDto(Guid.NewGuid(), 1, DiscountAmount: -5m);
        var command = CreateValidCommand() with { Items = [badItem] };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Item discount amount must be non-negative.");
    }

    // --- Amounts ---

    [Fact]
    public void Validate_WithNegativeShippingAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingAmount = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingAmount)
            .WithErrorMessage("Shipping amount must be non-negative.");
    }

    [Fact]
    public void Validate_WithNegativeDiscountAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { DiscountAmount = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DiscountAmount)
            .WithErrorMessage("Discount amount must be non-negative.");
    }

    [Fact]
    public void Validate_WithNegativeTaxAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { TaxAmount = -1m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TaxAmount)
            .WithErrorMessage("Tax amount must be non-negative.");
    }

    [Fact]
    public void Validate_WithZeroAmounts_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            ShippingAmount = 0m,
            DiscountAmount = 0m,
            TaxAmount = 0m
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- String Fields ---

    [Fact]
    public void Validate_WithShippingMethodExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { ShippingMethod = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ShippingMethod)
            .WithErrorMessage("Shipping method cannot exceed 100 characters.");
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
    public void Validate_WithCustomerNotesExceeding1000Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerNotes = new string('A', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerNotes)
            .WithErrorMessage("Customer notes cannot exceed 1000 characters.");
    }

    [Fact]
    public void Validate_WithInternalNotesExceeding2000Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { InternalNotes = new string('A', 2001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InternalNotes)
            .WithErrorMessage("Internal notes cannot exceed 2000 characters.");
    }

    [Fact]
    public void Validate_WithEmptyCurrency_ShouldFail()
    {
        var command = CreateValidCommand() with { Currency = "" };
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

    // --- Address Validation (only when provided) ---

    [Fact]
    public void Validate_WithShippingAddressEmptyFullName_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { ShippingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Full name is required.");
    }

    [Fact]
    public void Validate_WithNullShippingAddress_ShouldPass()
    {
        var command = CreateValidCommand() with { ShippingAddress = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingAddress);
    }

    [Fact]
    public void Validate_WithBillingAddressEmptyFullName_ShouldFail()
    {
        var badAddress = CreateValidAddress() with { FullName = "" };
        var command = CreateValidCommand() with { BillingAddress = badAddress };
        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Full name is required.");
    }

    [Fact]
    public void Validate_WithNullBillingAddress_ShouldPass()
    {
        var command = CreateValidCommand() with { BillingAddress = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.BillingAddress);
    }
}
