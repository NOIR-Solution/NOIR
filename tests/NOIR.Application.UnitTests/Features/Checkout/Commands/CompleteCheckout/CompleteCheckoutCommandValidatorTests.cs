using NOIR.Application.Features.Checkout.Commands.CompleteCheckout;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.CompleteCheckout;

/// <summary>
/// Unit tests for CompleteCheckoutCommandValidator.
/// </summary>
public class CompleteCheckoutCommandValidatorTests
{
    private readonly CompleteCheckoutCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CompleteCheckoutCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidCommand_WithCustomerNotes_ShouldPass()
    {
        var command = new CompleteCheckoutCommand(Guid.NewGuid(), CustomerNotes: "Please deliver after 5pm");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullCustomerNotes_ShouldPass()
    {
        var command = new CompleteCheckoutCommand(Guid.NewGuid(), CustomerNotes: null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerNotes);
    }

    // --- SessionId ---

    [Fact]
    public void Validate_WithEmptySessionId_ShouldFail()
    {
        var command = new CompleteCheckoutCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    // --- CustomerNotes (optional) ---

    [Fact]
    public void Validate_WithCustomerNotesExceeding2000Characters_ShouldFail()
    {
        var longNotes = new string('A', 2001);
        var command = new CompleteCheckoutCommand(Guid.NewGuid(), CustomerNotes: longNotes);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerNotes)
            .WithErrorMessage("Customer notes must not exceed 2000 characters.");
    }

    [Fact]
    public void Validate_WithCustomerNotesExactly2000Characters_ShouldPass()
    {
        var notes = new string('A', 2000);
        var command = new CompleteCheckoutCommand(Guid.NewGuid(), CustomerNotes: notes);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerNotes);
    }

    [Fact]
    public void Validate_WithEmptyCustomerNotes_ShouldPass()
    {
        var command = new CompleteCheckoutCommand(Guid.NewGuid(), CustomerNotes: "");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerNotes);
    }
}
