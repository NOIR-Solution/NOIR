namespace NOIR.Application.Features.Checkout.Queries.GetCheckoutSession;

/// <summary>
/// Wolverine handler for getting checkout session by ID.
/// </summary>
public class GetCheckoutSessionQueryHandler
{
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;

    public GetCheckoutSessionQueryHandler(IRepository<CheckoutSession, Guid> checkoutRepository)
    {
        _checkoutRepository = checkoutRepository;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        GetCheckoutSessionQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new CheckoutSessionByIdSpec(query.SessionId);
        var session = await _checkoutRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (session is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Checkout session with ID '{query.SessionId}' not found.", "NOIR-CHECKOUT-021"));
        }

        return Result.Success(CheckoutMapper.ToDto(session));
    }
}
