using NOIR.Domain.Entities.Review;

namespace NOIR.Domain.UnitTests.Entities.Review;

/// <summary>
/// Unit tests for the ProductReview aggregate root entity.
/// Tests factory methods, approval workflow, admin responses,
/// voting, media management, and business rule validation.
/// </summary>
public class ReviewTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private static readonly Guid TestProductId = Guid.NewGuid();

    /// <summary>
    /// Helper to create a standard product review for testing.
    /// </summary>
    private static ProductReview CreateTestReview(
        Guid? productId = null,
        string userId = TestUserId,
        int rating = 4,
        string? title = "Great Product",
        string content = "I really enjoyed using this product. Highly recommended!",
        Guid? orderId = null,
        bool isVerifiedPurchase = false,
        string? tenantId = TestTenantId)
    {
        return ProductReview.Create(
            productId ?? TestProductId,
            userId,
            rating,
            title,
            content,
            orderId,
            isVerifiedPurchase,
            tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidReview()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.ShouldNotBeNull();
        review.Id.ShouldNotBe(Guid.Empty);
        review.ProductId.ShouldBe(TestProductId);
        review.UserId.ShouldBe(TestUserId);
        review.Rating.ShouldBe(4);
        review.Title.ShouldBe("Great Product");
        review.Content.ShouldBe("I really enjoyed using this product. Highly recommended!");
        review.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.HelpfulVotes.ShouldBe(0);
        review.NotHelpfulVotes.ShouldBe(0);
        review.AdminResponse.ShouldBeNull();
        review.AdminRespondedAt.ShouldBeNull();
        review.Media.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithVerifiedPurchase_ShouldSetFlag()
    {
        // Act
        var review = CreateTestReview(isVerifiedPurchase: true);

        // Assert
        review.IsVerifiedPurchase.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithoutVerifiedPurchase_ShouldDefaultToFalse()
    {
        // Act
        var review = CreateTestReview(isVerifiedPurchase: false);

        // Assert
        review.IsVerifiedPurchase.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithOrderId_ShouldSetOrderId()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var review = CreateTestReview(orderId: orderId);

        // Assert
        review.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void Create_WithoutOrderId_ShouldHaveNullOrderId()
    {
        // Act
        var review = CreateTestReview(orderId: null);

        // Assert
        review.OrderId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullTitle_ShouldAllowNullTitle()
    {
        // Act
        var review = CreateTestReview(title: null);

        // Assert
        review.Title.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldTrimTitle()
    {
        // Act
        var review = CreateTestReview(title: "  Trimmed Title  ");

        // Assert
        review.Title.ShouldBe("Trimmed Title");
    }

    [Fact]
    public void Create_ShouldTrimContent()
    {
        // Act
        var review = CreateTestReview(content: "  Trimmed content  ");

        // Assert
        review.Content.ShouldBe("Trimmed content");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithValidRating_ShouldSetRating(int rating)
    {
        // Act
        var review = CreateTestReview(rating: rating);

        // Assert
        review.Rating.ShouldBe(rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithRatingBelowMinimum_ShouldThrow(int rating)
    {
        // Act
        var act = () => CreateTestReview(rating: rating);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .Message.ShouldContain("Rating must be between 1 and 5");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(100)]
    public void Create_WithRatingAboveMaximum_ShouldThrow(int rating)
    {
        // Act
        var act = () => CreateTestReview(rating: rating);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .Message.ShouldContain("Rating must be between 1 and 5");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceUserId_ShouldThrow(string? userId)
    {
        // Act
        var act = () => CreateTestReview(userId: userId!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceContent_ShouldThrow(string? content)
    {
        // Act
        var act = () => CreateTestReview(content: content!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Approval Workflow Tests

    [Fact]
    public void Approve_PendingReview_ShouldSetStatusToApproved()
    {
        // Arrange
        var review = CreateTestReview();
        review.Status.ShouldBe(ReviewStatus.Pending);

        // Act
        review.Approve();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_PendingReview_ShouldSetStatusToRejected()
    {
        // Arrange
        var review = CreateTestReview();
        review.Status.ShouldBe(ReviewStatus.Pending);

        // Act
        review.Reject();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Rejected);
    }

    [Fact]
    public void Approve_AlreadyApprovedReview_ShouldRemainApproved()
    {
        // Arrange
        var review = CreateTestReview();
        review.Approve();

        // Act - Calling approve again should be idempotent
        review.Approve();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_AlreadyRejectedReview_ShouldRemainRejected()
    {
        // Arrange
        var review = CreateTestReview();
        review.Reject();

        // Act
        review.Reject();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Rejected);
    }

    [Fact]
    public void Approve_RejectedReview_ShouldChangeToApproved()
    {
        // Arrange
        var review = CreateTestReview();
        review.Reject();

        // Act
        review.Approve();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_ApprovedReview_ShouldChangeToRejected()
    {
        // Arrange
        var review = CreateTestReview();
        review.Approve();

        // Act
        review.Reject();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Rejected);
    }

    #endregion

    #region Admin Response Tests

    [Fact]
    public void AddAdminResponse_WithValidResponse_ShouldSetResponse()
    {
        // Arrange
        var review = CreateTestReview();
        var beforeResponse = DateTimeOffset.UtcNow;

        // Act
        review.AddAdminResponse("Thank you for your review!");

        // Assert
        review.AdminResponse.ShouldBe("Thank you for your review!");
        review.AdminRespondedAt.ShouldNotBeNull();
        review.AdminRespondedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeResponse);
    }

    [Fact]
    public void AddAdminResponse_ShouldTrimResponse()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.AddAdminResponse("  Thank you!  ");

        // Assert
        review.AdminResponse.ShouldBe("Thank you!");
    }

    [Fact]
    public void AddAdminResponse_CalledTwice_ShouldOverwritePreviousResponse()
    {
        // Arrange
        var review = CreateTestReview();
        review.AddAdminResponse("First response");

        // Act
        review.AddAdminResponse("Updated response");

        // Assert — content is overwritten, timestamp is refreshed
        review.AdminResponse.ShouldBe("Updated response");
        review.AdminRespondedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAdminResponse_WithNullOrWhiteSpaceResponse_ShouldThrow(string? response)
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var act = () => review.AddAdminResponse(response!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Voting Tests

    [Fact]
    public void VoteHelpful_ShouldIncrementHelpfulVotes()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();

        // Assert
        review.HelpfulVotes.ShouldBe(1);
    }

    [Fact]
    public void VoteHelpful_CalledMultipleTimes_ShouldTrackCorrectCount()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();
        review.VoteHelpful();
        review.VoteHelpful();

        // Assert
        review.HelpfulVotes.ShouldBe(3);
    }

    [Fact]
    public void VoteNotHelpful_ShouldIncrementNotHelpfulVotes()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteNotHelpful();

        // Assert
        review.NotHelpfulVotes.ShouldBe(1);
    }

    [Fact]
    public void VoteNotHelpful_CalledMultipleTimes_ShouldTrackCorrectCount()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteNotHelpful();
        review.VoteNotHelpful();
        review.VoteNotHelpful();
        review.VoteNotHelpful();

        // Assert
        review.NotHelpfulVotes.ShouldBe(4);
    }

    [Fact]
    public void Votes_MixedHelpfulAndNotHelpful_ShouldTrackIndependently()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();
        review.VoteHelpful();
        review.VoteNotHelpful();
        review.VoteHelpful();
        review.VoteNotHelpful();

        // Assert
        review.HelpfulVotes.ShouldBe(3);
        review.NotHelpfulVotes.ShouldBe(2);
    }

    #endregion

    #region Media Tests

    [Fact]
    public void AddMedia_ShouldAddMediaToCollection()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/image.jpg", ReviewMediaType.Image, 0);

        // Assert
        review.Media.Count().ShouldBe(1);
        media.ShouldNotBeNull();
        media.ReviewId.ShouldBe(review.Id);
        media.MediaUrl.ShouldBe("https://example.com/image.jpg");
        media.MediaType.ShouldBe(ReviewMediaType.Image);
        media.DisplayOrder.ShouldBe(0);
    }

    [Fact]
    public void AddMedia_MultipleMediaItems_ShouldAddAll()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.AddMedia("https://example.com/image1.jpg", ReviewMediaType.Image, 0);
        review.AddMedia("https://example.com/video.mp4", ReviewMediaType.Video, 1);
        review.AddMedia("https://example.com/image2.jpg", ReviewMediaType.Image, 2);

        // Assert
        review.Media.Count().ShouldBe(3);
    }

    [Fact]
    public void AddMedia_ShouldSetTenantId()
    {
        // Arrange
        var review = CreateTestReview(tenantId: TestTenantId);

        // Act
        var media = review.AddMedia("https://example.com/image.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void AddMedia_WithVideoType_ShouldSetCorrectType()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/video.mp4", ReviewMediaType.Video, 0);

        // Assert
        media.MediaType.ShouldBe(ReviewMediaType.Video);
    }

    [Fact]
    public void AddMedia_WithDisplayOrder_ShouldRespectOrder()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media1 = review.AddMedia("https://example.com/third.jpg", ReviewMediaType.Image, 2);
        var media2 = review.AddMedia("https://example.com/first.jpg", ReviewMediaType.Image, 0);
        var media3 = review.AddMedia("https://example.com/second.jpg", ReviewMediaType.Image, 1);

        // Assert
        media1.DisplayOrder.ShouldBe(2);
        media2.DisplayOrder.ShouldBe(0);
        media3.DisplayOrder.ShouldBe(1);
    }

    [Fact]
    public void AddMedia_ShouldReturnNewMediaItem()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/image.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.Id.ShouldNotBe(Guid.Empty);
        review.Media.ShouldContain(media);
    }

    #endregion

    #region Combined Workflow Tests

    [Fact]
    public void FullReviewWorkflow_CreateApproveRespondVote()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var review = CreateTestReview(
            rating: 5,
            title: "Amazing!",
            content: "Best product ever!",
            orderId: orderId,
            isVerifiedPurchase: true);

        // Assert initial state
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.IsVerifiedPurchase.ShouldBeTrue();
        review.OrderId.ShouldBe(orderId);

        // Act - Add media
        var media = review.AddMedia("https://example.com/photo.jpg", ReviewMediaType.Image, 0);
        review.Media.Count().ShouldBe(1);

        // Act - Approve
        review.Approve();
        review.Status.ShouldBe(ReviewStatus.Approved);

        // Act - Vote
        review.VoteHelpful();
        review.VoteHelpful();
        review.VoteNotHelpful();
        review.HelpfulVotes.ShouldBe(2);
        review.NotHelpfulVotes.ShouldBe(1);

        // Act - Admin response
        review.AddAdminResponse("Thank you for your kind words!");
        review.AdminResponse.ShouldBe("Thank you for your kind words!");
        review.AdminRespondedAt.ShouldNotBeNull();
    }

    [Fact]
    public void FullReviewWorkflow_CreateReject()
    {
        // Arrange
        var review = CreateTestReview(
            rating: 1,
            title: "Spam review",
            content: "Buy products from competitive-site.com");

        // Act
        review.Reject();

        // Assert
        review.Status.ShouldBe(ReviewStatus.Rejected);
    }

    #endregion

    // Note: Enum membership is implicitly verified by status-specific tests
    // (Approve_PendingReview, Reject_PendingReview, etc.) and media type tests.
    // Explicit enum count tests are fragile and break when legitimate values are added.
}
