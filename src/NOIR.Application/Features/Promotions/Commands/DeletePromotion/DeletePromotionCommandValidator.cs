namespace NOIR.Application.Features.Promotions.Commands.DeletePromotion;

/// <summary>
/// Validator for DeletePromotionCommand.
/// </summary>
public sealed class DeletePromotionCommandValidator : AbstractValidator<DeletePromotionCommand>
{
    public DeletePromotionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promotion ID is required.");
    }
}
