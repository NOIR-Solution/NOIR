using NOIR.Domain.Entities.Review;

namespace NOIR.Domain.UnitTests.Entities.Review;

/// <summary>
/// Unit tests for the ProductReview aggregate root entity.
/// Tests factory method validation, property initialization, approval workflow,
/// admin responses, voting mechanics, and media management.
/// </summary>
public class ProductReviewTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-456";
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();

    /// <summary>
    /// Helper to create a standard product review for testing.
    /// </summary>
    private static ProductReview CreateTestReview(
        Guid? productId = null,
        string userId = TestUserId,
        int rating = 4,
        string? title = "Great Product",
        string content = "This product exceeded my expectations.",
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

    #region Create Factory - Property Initialization

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Act
        var review = ProductReview.Create(
            productId, TestUserId, 5, "Amazing", "Best product ever!",
            orderId, true, TestTenantId);

        // Assert
        review.Should().NotBeNull();
        review.Id.Should().NotBe(Guid.Empty);
        review.ProductId.Should().Be(productId);
        review.UserId.Should().Be(TestUserId);
        review.Rating.Should().Be(5);
        review.Title.Should().Be("Amazing");
        review.Content.Should().Be("Best product ever!");
        review.OrderId.Should().Be(orderId);
        review.IsVerifiedPurchase.Should().BeTrue();
        review.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.Status.Should().Be(ReviewStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeVotesToZero()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.HelpfulVotes.Should().Be(0);
        review.NotHelpfulVotes.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldInitializeAdminResponseAsNull()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.AdminResponse.Should().BeNull();
        review.AdminRespondedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyMediaCollection()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.Media.Should().NotBeNull();
        review.Media.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var review1 = CreateTestReview();
        var review2 = CreateTestReview();

        // Assert
        review1.Id.Should().NotBe(Guid.Empty);
        review2.Id.Should().NotBe(Guid.Empty);
        review1.Id.Should().NotBe(review2.Id);
    }

    [Fact]
    public void Create_WithNullTitle_ShouldAllowNullTitle()
    {
        // Act
        var review = CreateTestReview(title: null);

        // Assert
        review.Title.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullOrderId_ShouldHaveNullOrderId()
    {
        // Act
        var review = CreateTestReview(orderId: null);

        // Assert
        review.OrderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNullTenant()
    {
        // Act
        var review = CreateTestReview(tenantId: null);

        // Assert
        review.TenantId.Should().BeNull();
    }

    #endregion

    #region Create Factory - Trimming

    [Fact]
    public void Create_ShouldTrimTitle()
    {
        // Act
        var review = CreateTestReview(title: "  Spaced Title  ");

        // Assert
        review.Title.Should().Be("Spaced Title");
    }

    [Fact]
    public void Create_ShouldTrimContent()
    {
        // Act
        var review = CreateTestReview(content: "  Spaced content  ");

        // Assert
        review.Content.Should().Be("Spaced content");
    }

    #endregion

    #region Create Factory - Rating Validation

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithValidRating_ShouldSucceed(int rating)
    {
        // Act
        var review = CreateTestReview(rating: rating);

        // Assert
        review.Rating.Should().Be(rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Create_WithRatingBelowMinimum_ShouldThrowArgumentOutOfRange(int rating)
    {
        // Act
        var act = () => CreateTestReview(rating: rating);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Rating must be between 1 and 5*");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(100)]
    public void Create_WithRatingAboveMaximum_ShouldThrowArgumentOutOfRange(int rating)
    {
        // Act
        var act = () => CreateTestReview(rating: rating);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Rating must be between 1 and 5*");
    }

    #endregion

    #region Create Factory - Required Field Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ShouldThrowArgumentException(string? userId)
    {
        // Act
        var act = () => CreateTestReview(userId: userId!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidContent_ShouldThrowArgumentException(string? content)
    {
        // Act
        var act = () => CreateTestReview(content: content!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Approval Workflow

    [Fact]
    public void Approve_ShouldSetStatusToApproved()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.Approve();

        // Assert
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.Reject();

        // Assert
        review.Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public void Approve_CalledTwice_ShouldBeIdempotent()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.Approve();
        review.Approve();

        // Assert
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_CalledTwice_ShouldBeIdempotent()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.Reject();
        review.Reject();

        // Assert
        review.Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public void Approve_AfterReject_ShouldChangeToApproved()
    {
        // Arrange
        var review = CreateTestReview();
        review.Reject();

        // Act
        review.Approve();

        // Assert
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Fact]
    public void Reject_AfterApprove_ShouldChangeToRejected()
    {
        // Arrange
        var review = CreateTestReview();
        review.Approve();

        // Act
        review.Reject();

        // Assert
        review.Status.Should().Be(ReviewStatus.Rejected);
    }

    #endregion

    #region Admin Response

    [Fact]
    public void AddAdminResponse_ShouldSetResponseAndTimestamp()
    {
        // Arrange
        var review = CreateTestReview();
        var before = DateTimeOffset.UtcNow;

        // Act
        review.AddAdminResponse("Thank you for your feedback!");

        // Assert
        review.AdminResponse.Should().Be("Thank you for your feedback!");
        review.AdminRespondedAt.Should().NotBeNull();
        review.AdminRespondedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void AddAdminResponse_ShouldTrimWhitespace()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.AddAdminResponse("  Trimmed response  ");

        // Assert
        review.AdminResponse.Should().Be("Trimmed response");
    }

    [Fact]
    public void AddAdminResponse_CalledTwice_ShouldOverwritePrevious()
    {
        // Arrange
        var review = CreateTestReview();
        review.AddAdminResponse("First response");

        // Act
        review.AddAdminResponse("Updated response");

        // Assert
        review.AdminResponse.Should().Be("Updated response");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAdminResponse_WithInvalidResponse_ShouldThrowArgumentException(string? response)
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var act = () => review.AddAdminResponse(response!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Voting

    [Fact]
    public void VoteHelpful_ShouldIncrementHelpfulVotes()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();

        // Assert
        review.HelpfulVotes.Should().Be(1);
        review.NotHelpfulVotes.Should().Be(0);
    }

    [Fact]
    public void VoteNotHelpful_ShouldIncrementNotHelpfulVotes()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteNotHelpful();

        // Assert
        review.NotHelpfulVotes.Should().Be(1);
        review.HelpfulVotes.Should().Be(0);
    }

    [Fact]
    public void VoteHelpful_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();
        review.VoteHelpful();
        review.VoteHelpful();

        // Assert
        review.HelpfulVotes.Should().Be(3);
    }

    [Fact]
    public void VoteNotHelpful_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteNotHelpful();
        review.VoteNotHelpful();

        // Assert
        review.NotHelpfulVotes.Should().Be(2);
    }

    [Fact]
    public void MixedVotes_ShouldTrackIndependently()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.VoteHelpful();
        review.VoteNotHelpful();
        review.VoteHelpful();
        review.VoteNotHelpful();
        review.VoteHelpful();

        // Assert
        review.HelpfulVotes.Should().Be(3);
        review.NotHelpfulVotes.Should().Be(2);
    }

    #endregion

    #region Media Management

    [Fact]
    public void AddMedia_ShouldAddToCollection()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://cdn.example.com/photo.jpg", ReviewMediaType.Image, 0);

        // Assert
        review.Media.Should().HaveCount(1);
        review.Media.Should().Contain(media);
    }

    [Fact]
    public void AddMedia_ShouldSetMediaProperties()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://cdn.example.com/photo.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.ReviewId.Should().Be(review.Id);
        media.MediaUrl.Should().Be("https://cdn.example.com/photo.jpg");
        media.MediaType.Should().Be(ReviewMediaType.Image);
        media.DisplayOrder.Should().Be(0);
        media.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AddMedia_ShouldInheritTenantId()
    {
        // Arrange
        var review = CreateTestReview(tenantId: "specific-tenant");

        // Act
        var media = review.AddMedia("https://cdn.example.com/img.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.TenantId.Should().Be("specific-tenant");
    }

    [Fact]
    public void AddMedia_MultipleItems_ShouldTrackAll()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.AddMedia("https://cdn.example.com/img1.jpg", ReviewMediaType.Image, 0);
        review.AddMedia("https://cdn.example.com/vid.mp4", ReviewMediaType.Video, 1);
        review.AddMedia("https://cdn.example.com/img2.jpg", ReviewMediaType.Image, 2);

        // Assert
        review.Media.Should().HaveCount(3);
    }

    [Fact]
    public void AddMedia_WithVideoType_ShouldSetCorrectType()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://cdn.example.com/clip.mp4", ReviewMediaType.Video, 0);

        // Assert
        media.MediaType.Should().Be(ReviewMediaType.Video);
    }

    [Fact]
    public void AddMedia_ShouldReturnCreatedMediaItem()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://cdn.example.com/photo.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.Should().NotBeNull();
        media.Id.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Verified Purchase

    [Fact]
    public void Create_WithVerifiedPurchase_ShouldSetFlag()
    {
        // Act
        var review = CreateTestReview(isVerifiedPurchase: true, orderId: TestOrderId);

        // Assert
        review.IsVerifiedPurchase.Should().BeTrue();
        review.OrderId.Should().Be(TestOrderId);
    }

    [Fact]
    public void Create_WithoutVerifiedPurchase_ShouldDefaultToFalse()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.IsVerifiedPurchase.Should().BeFalse();
    }

    #endregion

    #region End-to-End Workflow

    [Fact]
    public void FullWorkflow_CreateApproveRespondVoteAddMedia()
    {
        // Arrange - Create a verified purchase review
        var review = CreateTestReview(
            rating: 5,
            title: "Outstanding quality",
            content: "Exceeded all expectations!",
            orderId: TestOrderId,
            isVerifiedPurchase: true);

        // Assert initial state
        review.Status.Should().Be(ReviewStatus.Pending);
        review.IsVerifiedPurchase.Should().BeTrue();

        // Act - Add media
        review.AddMedia("https://cdn.example.com/photo1.jpg", ReviewMediaType.Image, 0);
        review.AddMedia("https://cdn.example.com/video.mp4", ReviewMediaType.Video, 1);
        review.Media.Should().HaveCount(2);

        // Act - Approve
        review.Approve();
        review.Status.Should().Be(ReviewStatus.Approved);

        // Act - Vote
        review.VoteHelpful();
        review.VoteHelpful();
        review.VoteNotHelpful();
        review.HelpfulVotes.Should().Be(2);
        review.NotHelpfulVotes.Should().Be(1);

        // Act - Admin response
        review.AddAdminResponse("We appreciate your detailed review!");
        review.AdminResponse.Should().Be("We appreciate your detailed review!");
        review.AdminRespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullWorkflow_CreateAndReject()
    {
        // Arrange
        var review = CreateTestReview(
            rating: 1,
            title: "Inappropriate content",
            content: "This contains spam links.");

        // Act
        review.Reject();

        // Assert
        review.Status.Should().Be(ReviewStatus.Rejected);
        review.Rating.Should().Be(1);
    }

    #endregion
}
