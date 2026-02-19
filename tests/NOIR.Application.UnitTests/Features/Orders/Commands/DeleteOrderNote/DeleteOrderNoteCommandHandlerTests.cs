using NOIR.Application.Features.Orders.Commands.DeleteOrderNote;
using NOIR.Application.Features.Orders.DTOs;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.DeleteOrderNote;

/// <summary>
/// Unit tests for DeleteOrderNoteCommandHandler.
/// </summary>
public class DeleteOrderNoteCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteOrderNoteCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteOrderNoteCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteOrderNoteCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private static OrderNote CreateTestNote(Guid orderId, string content = "Test note")
    {
        return OrderNote.Create(orderId, content, "user-123", "Test User", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingNote_ShouldDeleteAndReturnDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var note = CreateTestNote(orderId, "Note to delete");
        var noteId = note.Id;

        var notes = new List<OrderNote> { note }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteOrderNoteCommand(orderId, noteId) { UserId = "user-123" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(noteId);
        result.Value.Content.Should().Be("Note to delete");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenNoteNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var notes = new List<OrderNote>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        var command = new DeleteOrderNoteCommand(Guid.NewGuid(), Guid.NewGuid()) { UserId = "user-123" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Order.NoteNotFound);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMismatchedOrderId_ShouldReturnNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var differentOrderId = Guid.NewGuid();
        var note = CreateTestNote(orderId);

        var notes = new List<OrderNote> { note }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.OrderNotes).Returns(notes.Object);

        var command = new DeleteOrderNoteCommand(differentOrderId, note.Id) { UserId = "user-123" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Order.NoteNotFound);
    }

    #endregion
}
