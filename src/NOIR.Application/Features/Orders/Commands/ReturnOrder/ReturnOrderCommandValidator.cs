namespace NOIR.Application.Features.Orders.Commands.ReturnOrder;

/// <summary>
/// Validator for ReturnOrderCommand.
/// </summary>
public sealed class ReturnOrderCommandValidator : AbstractValidator<ReturnOrderCommand>
{
    public ReturnOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Return reason cannot exceed 500 characters.");
    }
}
