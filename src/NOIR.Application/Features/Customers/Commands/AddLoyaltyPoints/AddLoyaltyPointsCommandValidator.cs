namespace NOIR.Application.Features.Customers.Commands.AddLoyaltyPoints;

/// <summary>
/// Validator for AddLoyaltyPointsCommand.
/// </summary>
public sealed class AddLoyaltyPointsCommandValidator : AbstractValidator<AddLoyaltyPointsCommand>
{
    public AddLoyaltyPointsCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than zero.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
