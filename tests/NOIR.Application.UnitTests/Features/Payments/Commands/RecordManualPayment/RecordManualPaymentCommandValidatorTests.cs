using NOIR.Application.Features.Payments.Commands.RecordManualPayment;

namespace NOIR.Application.UnitTests.Features.Payments.Commands.RecordManualPayment;

public class RecordManualPaymentCommandValidatorTests
{
    private readonly RecordManualPaymentCommandValidator _validator = new();

    private static RecordManualPaymentCommand CreateValidCommand() =>
        new(
            OrderId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "VND",
            PaymentMethod: PaymentMethod.BankTransfer,
            ReferenceNumber: "REF-001",
            Notes: null,
            PaidAt: DateTimeOffset.UtcNow);

    // --- Valid Command ---

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalCommand_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            ReferenceNumber = null,
            Notes = null,
            PaidAt = null
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // --- OrderId ---

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    // --- Amount ---

    [Fact]
    public void Validate_WithZeroAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { Amount = 0m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_WithNegativeAmount_ShouldFail()
    {
        var command = CreateValidCommand() with { Amount = -10m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero.");
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
    public void Validate_WithCurrencyExceeding3Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Currency = "ABCD" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency code cannot exceed 3 characters.");
    }

    // --- PaymentMethod ---

    [Fact]
    public void Validate_WithInvalidPaymentMethod_ShouldFail()
    {
        var command = CreateValidCommand() with { PaymentMethod = (PaymentMethod)999 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("Invalid payment method.");
    }

    // --- ReferenceNumber ---

    [Fact]
    public void Validate_WithReferenceNumberExceeding200Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { ReferenceNumber = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReferenceNumber)
            .WithErrorMessage("Reference number cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithNullReferenceNumber_ShouldPass()
    {
        var command = CreateValidCommand() with { ReferenceNumber = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ReferenceNumber);
    }

    // --- Notes ---

    [Fact]
    public void Validate_WithNotesExceeding1000Characters_ShouldFail()
    {
        var command = CreateValidCommand() with { Notes = new string('A', 1001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }

    [Fact]
    public void Validate_WithNullNotes_ShouldPass()
    {
        var command = CreateValidCommand() with { Notes = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}
