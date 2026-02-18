namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for AddAdminResponseCommandHandler.
/// Tests adding admin responses to reviews with mocked dependencies.
/// </summary>
public class AddAdminResponseCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddAdminResponseCommandHandler _handler;

    public AddAdminResponseCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddAdminResponseCommandHandler(
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
    public async Task Handle_WhenReviewExists_ShouldAddResponseAndReturnDto()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var existingReview = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddAdminResponseCommand(reviewId, "Thank you for your feedback!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminResponse.Should().Be("Thank you for your feedback!");
        result.Value.AdminRespondedAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldTrimResponseWhitespace()
    {
        // Arrange
        var existingReview = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AddAdminResponseCommand(Guid.NewGuid(), "  Thank you!  ");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminResponse.Should().Be("Thank you!");
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

        var command = new AddAdminResponseCommand(reviewId, "Response");

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

        var command = new AddAdminResponseCommand(Guid.NewGuid(), "Response");

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
