namespace NOIR.Application.Features.Promotions.Commands.CreatePromotion;

/// <summary>
/// Validator for CreatePromotionCommand.
/// </summary>
public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Promotion name is required.")
            .MaximumLength(200).WithMessage("Promotion name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promotion code is required.")
            .MaximumLength(50).WithMessage("Promotion code cannot exceed 50 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Promotion code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.PromotionType)
            .IsInEnum().WithMessage("Invalid promotion type.");

        RuleFor(x => x.DiscountType)
            .IsInEnum().WithMessage("Invalid discount type.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than zero.");

        When(x => x.DiscountType == DiscountType.Percentage, () =>
        {
            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100).WithMessage("Percentage discount cannot exceed 100%.");
        });

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.ApplyLevel)
            .IsInEnum().WithMessage("Invalid apply level.");

        When(x => x.MaxDiscountAmount.HasValue, () =>
        {
            RuleFor(x => x.MaxDiscountAmount!.Value)
                .GreaterThan(0).WithMessage("Maximum discount amount must be greater than zero.");
        });

        When(x => x.MinOrderValue.HasValue, () =>
        {
            RuleFor(x => x.MinOrderValue!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum order value must be non-negative.");
        });

        When(x => x.MinItemQuantity.HasValue, () =>
        {
            RuleFor(x => x.MinItemQuantity!.Value)
                .GreaterThan(0).WithMessage("Minimum item quantity must be greater than zero.");
        });

        When(x => x.UsageLimitTotal.HasValue, () =>
        {
            RuleFor(x => x.UsageLimitTotal!.Value)
                .GreaterThan(0).WithMessage("Total usage limit must be greater than zero.");
        });

        When(x => x.UsageLimitPerUser.HasValue, () =>
        {
            RuleFor(x => x.UsageLimitPerUser!.Value)
                .GreaterThan(0).WithMessage("Per-user usage limit must be greater than zero.");
        });
    }
}
