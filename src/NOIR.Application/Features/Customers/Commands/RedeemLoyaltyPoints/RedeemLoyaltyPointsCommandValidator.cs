namespace NOIR.Application.Features.Customers.Commands.RedeemLoyaltyPoints;

/// <summary>
/// Validator for RedeemLoyaltyPointsCommand.
/// </summary>
public sealed class RedeemLoyaltyPointsCommandValidator : AbstractValidator<RedeemLoyaltyPointsCommand>
{
    public RedeemLoyaltyPointsCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than zero.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
