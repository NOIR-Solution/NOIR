namespace NOIR.Application.Features.Inventory.Commands.CreateStockMovement;

/// <summary>
/// Validator for CreateStockMovementCommand.
/// </summary>
public sealed class CreateStockMovementCommandValidator : AbstractValidator<CreateStockMovementCommand>
{
    public CreateStockMovementCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Product Variant ID is required.");

        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Invalid movement type.");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Quantity cannot be zero.");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("Reference cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
