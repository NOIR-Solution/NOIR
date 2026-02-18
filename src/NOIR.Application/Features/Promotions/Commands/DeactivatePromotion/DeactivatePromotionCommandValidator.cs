namespace NOIR.Application.Features.Promotions.Commands.DeactivatePromotion;

/// <summary>
/// Validator for DeactivatePromotionCommand.
/// </summary>
public sealed class DeactivatePromotionCommandValidator : AbstractValidator<DeactivatePromotionCommand>
{
    public DeactivatePromotionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promotion ID is required.");
    }
}
