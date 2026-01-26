namespace NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;

/// <summary>
/// Wolverine handler for selecting shipping method.
/// </summary>
public class SelectShippingMethodCommandHandler
{
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SelectShippingMethodCommandHandler(
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IUnitOfWork unitOfWork)
    {
        _checkoutRepository = checkoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        SelectShippingMethodCommand command,
        CancellationToken cancellationToken)
    {
        // Get checkout session with tracking
        var spec = new CheckoutSessionByIdForUpdateSpec(command.SessionId);
        var session = await _checkoutRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (session is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Checkout session with ID '{command.SessionId}' not found.", "NOIR-CHECKOUT-008"));
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", "Checkout session has expired.", "NOIR-CHECKOUT-009"));
        }

        try
        {
            session.SelectShippingMethod(
                command.ShippingMethod,
                command.ShippingCost,
                command.EstimatedDeliveryAt);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(CheckoutMapper.ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", ex.Message, "NOIR-CHECKOUT-010"));
        }
    }
}
