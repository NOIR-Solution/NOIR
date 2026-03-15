using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Queries.GetCart;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for GetCartQueryHandler.
/// </summary>
public class GetCartQueryHandlerTests
{
    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly GetCartQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public GetCartQueryHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _handler = new GetCartQueryHandler(_cartRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDto()
    {
        // Arrange
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, "VND", TestTenantId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.Status.ShouldBe(CartStatus.Active);
    }

    [Fact]
    public async Task Handle_NoCartExists_ReturnsEmptyCartDto()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var query = new GetCartQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(Guid.Empty);
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_GuestSession_ReturnsGuestCart()
    {
        // Arrange
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, "VND", TestTenantId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartQuery { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.SessionId.ShouldBe(TestSessionId);
        result.Value.IsGuest.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_UserPreferredOverSession_ReturnsUserCart()
    {
        // Arrange
        var userCart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, "VND", TestTenantId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCart);

        var query = new GetCartQuery { UserId = TestUserId, SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.IsGuest.ShouldBe(false);

        // Session lookup should not have been called since user cart was found
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
