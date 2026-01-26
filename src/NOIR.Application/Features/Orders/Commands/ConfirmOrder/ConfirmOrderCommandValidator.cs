namespace NOIR.Application.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Validator for ConfirmOrderCommand.
/// </summary>
public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");
    }
}
