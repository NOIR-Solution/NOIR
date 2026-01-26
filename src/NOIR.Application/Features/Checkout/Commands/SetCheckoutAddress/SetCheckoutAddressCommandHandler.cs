namespace NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;

/// <summary>
/// Wolverine handler for setting checkout address.
/// </summary>
public class SetCheckoutAddressCommandHandler
{
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCheckoutAddressCommandHandler(
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IUnitOfWork unitOfWork)
    {
        _checkoutRepository = checkoutRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        SetCheckoutAddressCommand command,
        CancellationToken cancellationToken)
    {
        // Get checkout session with tracking
        var spec = new CheckoutSessionByIdForUpdateSpec(command.SessionId);
        var session = await _checkoutRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (session is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Checkout session with ID '{command.SessionId}' not found.", "NOIR-CHECKOUT-004"));
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", "Checkout session has expired.", "NOIR-CHECKOUT-005"));
        }

        // Create address value object
        var address = new Address
        {
            FullName = command.FullName,
            Phone = command.Phone,
            AddressLine1 = command.AddressLine1,
            AddressLine2 = command.AddressLine2,
            Ward = command.Ward ?? string.Empty,
            District = command.District ?? string.Empty,
            Province = command.Province ?? string.Empty,
            Country = command.Country,
            PostalCode = command.PostalCode,
            IsDefault = false
        };

        try
        {
            if (command.AddressType.Equals("Shipping", StringComparison.OrdinalIgnoreCase))
            {
                session.SetShippingAddress(address);
            }
            else if (command.AddressType.Equals("Billing", StringComparison.OrdinalIgnoreCase))
            {
                session.SetBillingAddress(address, command.BillingSameAsShipping);
            }
            else
            {
                return Result.Failure<CheckoutSessionDto>(
                    Error.Validation("AddressType", "Address type must be 'Shipping' or 'Billing'.", "NOIR-CHECKOUT-006"));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(CheckoutMapper.ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", ex.Message, "NOIR-CHECKOUT-007"));
        }
    }
}
