using NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SetCheckoutAddress;

/// <summary>
/// Unit tests for SetCheckoutAddressCommandValidator.
/// </summary>
public class SetCheckoutAddressCommandValidatorTests
{
    private readonly SetCheckoutAddressCommandValidator _validator = new();

    private static SetCheckoutAddressCommand CreateValidCommand() =>
        new(
            SessionId: Guid.NewGuid(),
            AddressType: "Shipping",
            FullName: "Nguyen Van A",
            Phone: "+84123456789",
            AddressLine1: "123 Le Loi Street",
            AddressLine2: null,
            Ward: "Ben Nghe",
            District: "District 1",
            Province: "Ho Chi Minh City",
            PostalCode: "700000",
            Country: "Vietnam");

    [Fact]
    public void Validate_WithValidCommand_Shipping_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_Billing_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressType = "Billing" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithCaseInsensitiveAddressType_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressType = "shipping" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressType);
    }

    [Fact]
    public void Validate_WithCaseInsensitiveBillingType_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressType = "BILLING" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressType);
    }

    // --- SessionId ---

    [Fact]
    public void Validate_WithEmptySessionId_ShouldFail()
    {
        var command = CreateValidCommand() with { SessionId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    // --- AddressType ---

    [Fact]
    public void Validate_WithEmptyAddressType_ShouldFail()
    {
        var command = CreateValidCommand() with { AddressType = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressType);
    }

    [Fact]
    public void Validate_WithInvalidAddressType_ShouldFail()
    {
        var command = CreateValidCommand() with { AddressType = "Home" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressType)
            .WithErrorMessage("Address type must be 'Shipping' or 'Billing'.");
    }

    // --- FullName ---

    [Fact]
    public void Validate_WithEmptyFullName_ShouldFail()
    {
        var command = CreateValidCommand() with { FullName = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name is required.");
    }

    [Fact]
    public void Validate_WithFullNameExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { FullName = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithFullNameExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { FullName = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    // --- Phone ---

    [Fact]
    public void Validate_WithEmptyPhone_ShouldFail()
    {
        var command = CreateValidCommand() with { Phone = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone number is required.");
    }

    [Fact]
    public void Validate_WithPhoneExceeding20Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Phone = new string('1', 21) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithPhoneExactly20Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { Phone = new string('1', 20) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    // --- AddressLine1 ---

    [Fact]
    public void Validate_WithEmptyAddressLine1_ShouldFail()
    {
        var command = CreateValidCommand() with { AddressLine1 = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 is required.");
    }

    [Fact]
    public void Validate_WithAddressLine1Exceeding200Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { AddressLine1 = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithAddressLine1Exactly200Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressLine1 = new string('A', 200) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine1);
    }

    // --- AddressLine2 (optional) ---

    [Fact]
    public void Validate_WithNullAddressLine2_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressLine2 = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine2);
    }

    [Fact]
    public void Validate_WithAddressLine2Exceeding200Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { AddressLine2 = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressLine2)
            .WithErrorMessage("Address line 2 must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithAddressLine2Exactly200Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { AddressLine2 = new string('A', 200) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine2);
    }

    // --- Ward (optional) ---

    [Fact]
    public void Validate_WithNullWard_ShouldPass()
    {
        var command = CreateValidCommand() with { Ward = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Ward);
    }

    [Fact]
    public void Validate_WithWardExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Ward = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Ward)
            .WithErrorMessage("Ward must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithWardExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { Ward = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Ward);
    }

    // --- District (optional) ---

    [Fact]
    public void Validate_WithNullDistrict_ShouldPass()
    {
        var command = CreateValidCommand() with { District = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.District);
    }

    [Fact]
    public void Validate_WithDistrictExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { District = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.District)
            .WithErrorMessage("District must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithDistrictExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { District = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.District);
    }

    // --- Province (optional) ---

    [Fact]
    public void Validate_WithNullProvince_ShouldPass()
    {
        var command = CreateValidCommand() with { Province = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Province);
    }

    [Fact]
    public void Validate_WithProvinceExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Province = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Province)
            .WithErrorMessage("Province must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithProvinceExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { Province = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Province);
    }

    // --- PostalCode (optional) ---

    [Fact]
    public void Validate_WithNullPostalCode_ShouldPass()
    {
        var command = CreateValidCommand() with { PostalCode = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    [Fact]
    public void Validate_WithPostalCodeExceeding20Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { PostalCode = new string('1', 21) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithPostalCodeExactly20Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { PostalCode = new string('1', 20) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    // --- Country ---

    [Fact]
    public void Validate_WithEmptyCountry_ShouldFail()
    {
        var command = CreateValidCommand() with { Country = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country is required.");
    }

    [Fact]
    public void Validate_WithCountryExceeding100Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Country = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithCountryExactly100Characters_ShouldPass()
    {
        var command = CreateValidCommand() with { Country = new string('A', 100) };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }
}
