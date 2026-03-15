using NOIR.Application.Features.Cart.Commands.MergeCart;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for MergeCartCommandHandler.
/// </summary>
public class MergeCartCommandHandlerTests
{
    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<MergeCartCommandHandler>> _loggerMock;
    private readonly MergeCartCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public MergeCartCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<MergeCartCommandHandler>>();

        _handler = new MergeCartCommandHandler(
            _cartRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoGuestCart_ReturnsEmptyResult()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new MergeCartCommand(TestSessionId, TestUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.MergedItemCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_GuestCartNoUserCart_AssociatesGuestCartWithUser()
    {
        // Arrange
        var guestCart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, "VND", TestTenantId);
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        guestCart.AddItem(productId, variantId, "Product", "Variant", 100m, 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestCart);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new MergeCartCommand(TestSessionId, TestUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.MergedItemCount.ShouldBe(2);
        result.Value.TotalItemCount.ShouldBe(2);

        guestCart.UserId.ShouldBe(TestUserId);
        guestCart.SessionId.ShouldBeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BothCartsExist_MergesGuestIntoUser()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var variantId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();

        var guestCart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, "VND", TestTenantId);
        guestCart.AddItem(productId1, variantId1, "Product 1", "Variant 1", 100m, 1);

        var userCart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, "VND", TestTenantId);
        userCart.AddItem(productId2, variantId2, "Product 2", "Variant 2", 200m, 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestCart);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCart);

        var command = new MergeCartCommand(TestSessionId, TestUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.MergedItemCount.ShouldBe(1);
        result.Value.TotalItemCount.ShouldBe(3); // 2 from user + 1 from guest

        guestCart.Status.ShouldBe(CartStatus.Merged);
        userCart.ItemCount.ShouldBe(3);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task Handle_SameProductInBothCarts_QuantitiesAreCombined()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var guestCart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, "VND", TestTenantId);
        guestCart.AddItem(productId, variantId, "Product", "Variant", 100m, 2);

        var userCart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, "VND", TestTenantId);
        userCart.AddItem(productId, variantId, "Product", "Variant", 100m, 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guestCart);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCart);

        var command = new MergeCartCommand(TestSessionId, TestUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalItemCount.ShouldBe(5); // 3 + 2 combined
    }
}
