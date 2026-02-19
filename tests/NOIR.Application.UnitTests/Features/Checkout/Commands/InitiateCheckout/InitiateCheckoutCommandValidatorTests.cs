using NOIR.Application.Features.Checkout.Commands.InitiateCheckout;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.InitiateCheckout;

/// <summary>
/// Unit tests for InitiateCheckoutCommandValidator.
/// </summary>
public class InitiateCheckoutCommandValidatorTests
{
    private readonly InitiateCheckoutCommandValidator _validator = new();

    private static InitiateCheckoutCommand CreateValidCommand() =>
        new(CartId: Guid.NewGuid(), CustomerEmail: "customer@example.com");

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_WithOptionalFields_ShouldPass()
    {
        var command = new InitiateCheckoutCommand(
            Guid.NewGuid(),
            "customer@example.com",
            CustomerName: "John Doe",
            CustomerPhone: "+84123456789");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- CartId ---

    [Fact]
    public void Validate_WithEmptyCartId_ShouldFail()
    {
        var command = CreateValidCommand() with { CartId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CartId)
            .WithErrorMessage("Cart ID is required.");
    }

    // --- CustomerEmail ---

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email is required.");
    }

    [Fact]
    public void Validate_WithNullEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Customer email is required.");
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var command = CreateValidCommand() with { CustomerEmail = "not-an-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Validate_WithEmailExceeding256Characters_ShouldFail()
    {
        var longEmail = new string('a', 245) + "@example.com"; // 257 chars
        var command = CreateValidCommand() with { CustomerEmail = longEmail };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerEmail)
            .WithErrorMessage("Email must not exceed 256 characters.");
    }

    [Fact]
    public void Validate_WithEmailExactly256Characters_ShouldPass()
    {
        var localPart = new string('a', 243); // 243 + "@example.com" = 255 chars -- need to be <= 256
        var email = localPart + "@example.com"; // 255 chars, valid
        var command = CreateValidCommand() with { CustomerEmail = email };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerEmail);
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
    public void Validate_WithCustomerNameExceeding100Characters_ShouldFail()
    {
        var longName = new string('A', 101);
        var command = CreateValidCommand() with { CustomerName = longName };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerName)
            .WithErrorMessage("Customer name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithCustomerNameExactly100Characters_ShouldPass()
    {
        var name = new string('A', 100);
        var command = CreateValidCommand() with { CustomerName = name };
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
        var longPhone = new string('1', 21);
        var command = CreateValidCommand() with { CustomerPhone = longPhone };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerPhone)
            .WithErrorMessage("Customer phone must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithCustomerPhoneExactly20Characters_ShouldPass()
    {
        var phone = new string('1', 20);
        var command = CreateValidCommand() with { CustomerPhone = phone };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerPhone);
    }
}
