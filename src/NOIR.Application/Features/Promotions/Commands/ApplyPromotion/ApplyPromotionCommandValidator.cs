namespace NOIR.Application.Features.Promotions.Commands.ApplyPromotion;

/// <summary>
/// Validator for ApplyPromotionCommand.
/// </summary>
public sealed class ApplyPromotionCommandValidator : AbstractValidator<ApplyPromotionCommand>
{
    public ApplyPromotionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promotion code is required.")
            .MaximumLength(50).WithMessage("Promotion code cannot exceed 50 characters.");

        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.OrderTotal)
            .GreaterThanOrEqualTo(0).WithMessage("Order total must be non-negative.");
    }
}
