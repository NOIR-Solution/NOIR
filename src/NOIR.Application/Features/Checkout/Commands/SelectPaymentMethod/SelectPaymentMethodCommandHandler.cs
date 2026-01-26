namespace NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;

/// <summary>
/// Wolverine handler for selecting payment method.
/// </summary>
public class SelectPaymentMethodCommandHandler
{
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SelectPaymentMethodCommandHandler(
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IUnitOfWork unitOfWork)
    {
        _checkoutRepository = checkoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        SelectPaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        // Get checkout session with tracking
        var spec = new CheckoutSessionByIdForUpdateSpec(command.SessionId);
        var session = await _checkoutRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (session is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Checkout session with ID '{command.SessionId}' not found.", "NOIR-CHECKOUT-011"));
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", "Checkout session has expired.", "NOIR-CHECKOUT-012"));
        }

        try
        {
            session.SelectPaymentMethod(command.PaymentMethod, command.PaymentGatewayId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(CheckoutMapper.ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", ex.Message, "NOIR-CHECKOUT-013"));
        }
    }
}
