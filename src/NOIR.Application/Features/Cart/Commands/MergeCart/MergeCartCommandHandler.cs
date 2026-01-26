namespace NOIR.Application.Features.Cart.Commands.MergeCart;

/// <summary>
/// Handler for merging guest cart into user cart on login.
/// </summary>
public sealed class MergeCartCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MergeCartCommandHandler> _logger;

    public MergeCartCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<MergeCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartMergeResultDto>> Handle(MergeCartCommand command, CancellationToken ct)
    {
        // 1. Get the guest cart by session ID
        var guestCartSpec = new ActiveCartBySessionIdSpec(command.SessionId, forUpdate: true);
        var guestCart = await _cartRepository.FirstOrDefaultAsync(guestCartSpec, ct);

        // If no guest cart, nothing to merge
        if (guestCart is null || guestCart.IsEmpty)
        {
            // Get or create user cart to return current state
            var existingUserCartSpec = new ActiveCartByUserIdSpec(command.UserId, forUpdate: false);
            var existingUserCart = await _cartRepository.FirstOrDefaultAsync(existingUserCartSpec, ct);

            if (existingUserCart is null)
            {
                return Result.Success(new CartMergeResultDto
                {
                    TargetCartId = Guid.Empty,
                    MergedItemCount = 0,
                    TotalItemCount = 0,
                    NewSubtotal = 0
                });
            }

            return Result.Success(new CartMergeResultDto
            {
                TargetCartId = existingUserCart.Id,
                MergedItemCount = 0,
                TotalItemCount = existingUserCart.ItemCount,
                NewSubtotal = existingUserCart.Subtotal
            });
        }

        var guestItemCount = guestCart.ItemCount;

        // 2. Get or create user cart
        var userCartSpec = new ActiveCartByUserIdSpec(command.UserId, forUpdate: true);
        var userCart = await _cartRepository.FirstOrDefaultAsync(userCartSpec, ct);

        if (userCart is null)
        {
            // No user cart exists - associate guest cart with user
            guestCart.AssociateWithUser(command.UserId);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Associated guest cart {GuestCartId} with user {UserId}. Items: {ItemCount}",
                guestCart.Id, command.UserId, guestItemCount);

            return Result.Success(new CartMergeResultDto
            {
                TargetCartId = guestCart.Id,
                MergedItemCount = guestItemCount,
                TotalItemCount = guestCart.ItemCount,
                NewSubtotal = guestCart.Subtotal
            });
        }

        // 3. Merge guest cart into user cart
        userCart.MergeFrom(guestCart);

        // 4. Mark guest cart as merged (this also raises the domain event)
        guestCart.MarkAsMerged(userCart.Id, command.UserId, guestItemCount);

        // 5. Save changes
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Merged guest cart {GuestCartId} ({GuestItems} items) into user cart {UserCartId}. Total: {TotalItems}",
            guestCart.Id, guestItemCount, userCart.Id, userCart.ItemCount);

        return Result.Success(new CartMergeResultDto
        {
            TargetCartId = userCart.Id,
            MergedItemCount = guestItemCount,
            TotalItemCount = userCart.ItemCount,
            NewSubtotal = userCart.Subtotal
        });
    }
}
