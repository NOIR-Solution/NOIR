using NOIR.Application.Features.Orders.Commands.AddOrderNote;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.AddOrderNote;

/// <summary>
/// Unit tests for AddOrderNoteCommandHandler.
/// </summary>
public class AddOrderNoteCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly AddOrderNoteCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestUserName = "Test User";

    public AddOrderNoteCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();

        _handler = new AddOrderNoteCommandHandler(
            _orderRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _userIdentityServiceMock.Object);
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com")
    {
        return Order.Create(orderNumber, customerEmail, 100.00m, 110.00m, "VND", TestTenantId);
    }

    private void SetupUserIdentity(string userId, string displayName)
    {
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityDto(
                userId, "test@example.com", TestTenantId,
                "Test", "User", displayName, displayName,
                null, null, true, false, false,
                DateTimeOffset.UtcNow, null));
    }

    private void SetupOrderNotesDbSet()
    {
        var notes = new List<OrderNote>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddNoteAndReturnDto()
    {
        // Arrange
        var order = CreateTestOrder();
        var command = new AddOrderNoteCommand(order.Id, "This order needs special packaging")
        {
            UserId = TestUserId
        };

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        SetupUserIdentity(TestUserId, TestUserName);
        SetupOrderNotesDbSet();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("This order needs special packaging");
        result.Value.OrderId.Should().Be(order.Id);
        result.Value.CreatedByUserId.Should().Be(TestUserId);
        result.Value.CreatedByUserName.Should().Be(TestUserName);
        result.Value.IsInternal.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ShouldUseUnknownAsUserName()
    {
        // Arrange
        var order = CreateTestOrder();
        var command = new AddOrderNoteCommand(order.Id, "Test note")
        {
            UserId = "unknown-user"
        };

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("unknown-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        SetupOrderNotesDbSet();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedByUserName.Should().Be("Unknown");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new AddOrderNoteCommand(Guid.NewGuid(), "Test note")
        {
            UserId = TestUserId
        };

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Order.NotFound);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var order = CreateTestOrder();
        var command = new AddOrderNoteCommand(order.Id, "Test note")
        {
            UserId = TestUserId
        };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        SetupUserIdentity(TestUserId, TestUserName);
        SetupOrderNotesDbSet();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
