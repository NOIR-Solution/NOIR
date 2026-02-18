namespace NOIR.Application.Features.Inventory.Commands.CancelInventoryReceipt;

/// <summary>
/// Validator for CancelInventoryReceiptCommand.
/// </summary>
public sealed class CancelInventoryReceiptCommandValidator : AbstractValidator<CancelInventoryReceiptCommand>
{
    public CancelInventoryReceiptCommandValidator()
    {
        RuleFor(x => x.ReceiptId)
            .NotEmpty().WithMessage("Receipt ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}
