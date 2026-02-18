namespace NOIR.Application.Features.Promotions.Commands.ActivatePromotion;

/// <summary>
/// Validator for ActivatePromotionCommand.
/// </summary>
public sealed class ActivatePromotionCommandValidator : AbstractValidator<ActivatePromotionCommand>
{
    public ActivatePromotionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promotion ID is required.");
    }
}
