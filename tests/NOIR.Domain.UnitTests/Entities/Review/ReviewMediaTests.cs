using NOIR.Domain.Entities.Review;

namespace NOIR.Domain.UnitTests.Entities.Review;

/// <summary>
/// Unit tests for the ReviewMedia entity.
/// ReviewMedia.Create is internal, so all items are created via ProductReview.AddMedia.
/// Tests property initialization, media types, display ordering, and tenant inheritance.
/// </summary>
public class ReviewMediaTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static ProductReview CreateTestReview(string? tenantId = TestTenantId)
    {
        return ProductReview.Create(
            Guid.NewGuid(), "user-123", 4,
            "Great Product", "Really good quality!",
            tenantId: tenantId);
    }

    #endregion

    #region Creation Tests (via ProductReview.AddMedia)

    [Fact]
    public void Create_WithImageType_ShouldSetAllProperties()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/image.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.Should().NotBeNull();
        media.Id.Should().NotBe(Guid.Empty);
        media.ReviewId.Should().Be(review.Id);
        media.MediaUrl.Should().Be("https://example.com/image.jpg");
        media.MediaType.Should().Be(ReviewMediaType.Image);
        media.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void Create_WithVideoType_ShouldSetCorrectType()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/video.mp4", ReviewMediaType.Video, 0);

        // Assert
        media.MediaType.Should().Be(ReviewMediaType.Video);
    }

    [Fact]
    public void Create_ShouldSetTenantIdFromReview()
    {
        // Arrange
        var review = CreateTestReview(tenantId: "custom-tenant");

        // Act
        var media = review.AddMedia("https://example.com/img.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.TenantId.Should().Be("custom-tenant");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Arrange
        var review = CreateTestReview(tenantId: null);

        // Act
        var media = review.AddMedia("https://example.com/img.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media1 = review.AddMedia("https://example.com/1.jpg", ReviewMediaType.Image, 0);
        var media2 = review.AddMedia("https://example.com/2.jpg", ReviewMediaType.Image, 1);

        // Assert
        media1.Id.Should().NotBe(media2.Id);
    }

    [Fact]
    public void Create_ShouldSetReviewIdFromParent()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/img.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.ReviewId.Should().Be(review.Id);
    }

    #endregion

    #region DisplayOrder Tests

    [Fact]
    public void Create_WithDisplayOrder_ShouldRespectOrder()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media0 = review.AddMedia("https://example.com/first.jpg", ReviewMediaType.Image, 0);
        var media1 = review.AddMedia("https://example.com/second.jpg", ReviewMediaType.Image, 1);
        var media2 = review.AddMedia("https://example.com/third.jpg", ReviewMediaType.Image, 2);

        // Assert
        media0.DisplayOrder.Should().Be(0);
        media1.DisplayOrder.Should().Be(1);
        media2.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void Create_WithNonSequentialDisplayOrder_ShouldPreserveExactOrder()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media1 = review.AddMedia("https://example.com/a.jpg", ReviewMediaType.Image, 10);
        var media2 = review.AddMedia("https://example.com/b.jpg", ReviewMediaType.Image, 5);
        var media3 = review.AddMedia("https://example.com/c.jpg", ReviewMediaType.Image, 20);

        // Assert
        media1.DisplayOrder.Should().Be(10);
        media2.DisplayOrder.Should().Be(5);
        media3.DisplayOrder.Should().Be(20);
    }

    #endregion

    #region Media Type Tests

    [Theory]
    [InlineData(ReviewMediaType.Image)]
    [InlineData(ReviewMediaType.Video)]
    public void Create_WithAllMediaTypes_ShouldSetCorrectType(ReviewMediaType type)
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/file", type, 0);

        // Assert
        media.MediaType.Should().Be(type);
    }

    #endregion

    #region Integration with Review

    [Fact]
    public void AddMedia_ShouldAddToReviewMediaCollection()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        review.AddMedia("https://example.com/1.jpg", ReviewMediaType.Image, 0);
        review.AddMedia("https://example.com/2.mp4", ReviewMediaType.Video, 1);
        review.AddMedia("https://example.com/3.jpg", ReviewMediaType.Image, 2);

        // Assert
        review.Media.Should().HaveCount(3);
    }

    [Fact]
    public void AddMedia_MultipleItems_ShouldAllReferenceParentReview()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media1 = review.AddMedia("https://example.com/1.jpg", ReviewMediaType.Image, 0);
        var media2 = review.AddMedia("https://example.com/2.jpg", ReviewMediaType.Image, 1);

        // Assert
        media1.ReviewId.Should().Be(review.Id);
        media2.ReviewId.Should().Be(review.Id);
    }

    [Fact]
    public void AddMedia_ShouldReturnCreatedMediaItem()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://example.com/img.jpg", ReviewMediaType.Image, 0);

        // Assert
        review.Media.Should().Contain(media);
    }

    #endregion

    #region URL Handling

    [Fact]
    public void Create_WithLongUrl_ShouldPreserveFullUrl()
    {
        // Arrange
        var review = CreateTestReview();
        var longUrl = "https://example.com/" + new string('a', 500) + ".jpg";

        // Act
        var media = review.AddMedia(longUrl, ReviewMediaType.Image, 0);

        // Assert
        media.MediaUrl.Should().Be(longUrl);
    }

    [Fact]
    public void Create_WithDifferentUrlSchemes_ShouldPreserveScheme()
    {
        // Arrange
        var review = CreateTestReview();

        // Act
        var media = review.AddMedia("https://cdn.example.com/secure/image.jpg", ReviewMediaType.Image, 0);

        // Assert
        media.MediaUrl.Should().StartWith("https://");
    }

    #endregion
}
