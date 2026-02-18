namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for VoteReviewCommandHandler.
/// Tests review voting (helpful/not helpful) scenarios with mocked dependencies.
/// </summary>
public class VoteReviewCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly VoteReviewCommandHandler _handler;

    public VoteReviewCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new VoteReviewCommandHandler(
            _reviewRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ProductReview CreateTestReview(
        string userId = "user-123",
        int rating = 4,
        string content = "This is a great product worth buying.")
    {
        return ProductReview.Create(
            Guid.NewGuid(),
            userId,
            rating,
            "Test Review",
            content,
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenIsHelpfulTrue_ShouldIncrementHelpfulVotes()
    {
        // Arrange
        var existingReview = CreateTestReview();
        var initialHelpfulVotes = existingReview.HelpfulVotes;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new VoteReviewCommand(Guid.NewGuid(), IsHelpful: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingReview.HelpfulVotes.Should().Be(initialHelpfulVotes + 1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIsHelpfulFalse_ShouldIncrementNotHelpfulVotes()
    {
        // Arrange
        var existingReview = CreateTestReview();
        var initialNotHelpfulVotes = existingReview.NotHelpfulVotes;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new VoteReviewCommand(Guid.NewGuid(), IsHelpful: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingReview.NotHelpfulVotes.Should().Be(initialNotHelpfulVotes + 1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenReviewNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        var command = new VoteReviewCommand(reviewId, IsHelpful: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-REVIEW-002");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var existingReview = CreateTestReview();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new VoteReviewCommand(Guid.NewGuid(), IsHelpful: true);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ReviewByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
