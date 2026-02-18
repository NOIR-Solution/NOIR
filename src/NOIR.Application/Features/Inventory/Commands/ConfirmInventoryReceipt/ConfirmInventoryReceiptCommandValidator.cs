namespace NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;

/// <summary>
/// Validator for ConfirmInventoryReceiptCommand.
/// </summary>
public sealed class ConfirmInventoryReceiptCommandValidator : AbstractValidator<ConfirmInventoryReceiptCommand>
{
    public ConfirmInventoryReceiptCommandValidator()
    {
        RuleFor(x => x.ReceiptId)
            .NotEmpty().WithMessage("Receipt ID is required.");
    }
}
