using NOIR.Application.Features.Orders.Queries.GetOrderNotes;
using NOIR.Application.Features.Orders.DTOs;

namespace NOIR.Application.UnitTests.Features.Orders.Queries.GetOrderNotes;

/// <summary>
/// Unit tests for GetOrderNotesQueryHandler.
/// </summary>
public class GetOrderNotesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetOrderNotesQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetOrderNotesQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetOrderNotesQueryHandler(_dbContextMock.Object);
    }

    private static OrderNote CreateTestNote(
        Guid orderId,
        string content = "Test note",
        string userId = "user-123",
        string userName = "Test User")
    {
        return OrderNote.Create(orderId, content, userId, userName, TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingNotes_ShouldReturnNotesList()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var note1 = CreateTestNote(orderId, "First note", "user-1", "User One");
        var note2 = CreateTestNote(orderId, "Second note", "user-2", "User Two");

        var notes = new List<OrderNote> { note1, note2 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        var query = new GetOrderNotesQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoNotes_ShouldReturnEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var notes = new List<OrderNote>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        var query = new GetOrderNotesQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnNotesForSpecifiedOrder()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        var note1 = CreateTestNote(orderId1, "Note for order 1");
        var note2 = CreateTestNote(orderId2, "Note for order 2");
        var note3 = CreateTestNote(orderId1, "Another note for order 1");

        var notes = new List<OrderNote> { note1, note2, note3 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        var query = new GetOrderNotesQuery(orderId1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(n => n.OrderId.Should().Be(orderId1));
    }

    #endregion
}
