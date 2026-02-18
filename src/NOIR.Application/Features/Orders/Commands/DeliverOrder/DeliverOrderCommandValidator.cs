namespace NOIR.Application.Features.Orders.Commands.DeliverOrder;

/// <summary>
/// Validator for DeliverOrderCommand.
/// </summary>
public sealed class DeliverOrderCommandValidator : AbstractValidator<DeliverOrderCommand>
{
    public DeliverOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");
    }
}
