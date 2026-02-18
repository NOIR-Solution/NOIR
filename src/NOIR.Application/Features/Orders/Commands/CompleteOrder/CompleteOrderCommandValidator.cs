namespace NOIR.Application.Features.Orders.Commands.CompleteOrder;

/// <summary>
/// Validator for CompleteOrderCommand.
/// </summary>
public sealed class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
{
    public CompleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");
    }
}
