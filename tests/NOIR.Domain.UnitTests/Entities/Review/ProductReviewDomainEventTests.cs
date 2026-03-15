using NOIR.Domain.Entities.Review;
using NOIR.Domain.Events.Review;

namespace NOIR.Domain.UnitTests.Entities.Review;

/// <summary>
/// Unit tests verifying that the ProductReview aggregate root raises
/// the correct domain events for creation, approval, rejection, and admin response.
/// </summary>
public class ProductReviewDomainEventTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-456";
    private static readonly Guid TestProductId = Guid.NewGuid();

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

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaiseReviewCreatedEvent()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        review.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ReviewCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectReviewId()
    {
        // Act
        var review = CreateTestReview();

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewCreatedEvent>().Single();
        domainEvent.ReviewId.ShouldBe(review.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectProductId()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var review = CreateTestReview(productId: productId);

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewCreatedEvent>().Single();
        domainEvent.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectUserIdAndRating()
    {
        // Act
        var review = CreateTestReview(userId: "user-789", rating: 5);

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewCreatedEvent>().Single();
        domainEvent.UserId.ShouldBe("user-789");
        domainEvent.Rating.ShouldBe(5);
    }

    #endregion

    #region Approve Domain Event

    [Fact]
    public void Approve_ShouldRaiseReviewApprovedEvent()
    {
        // Arrange
        var review = CreateTestReview();
        review.ClearDomainEvents();

        // Act
        review.Approve();

        // Assert
        review.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ReviewApprovedEvent>();
    }

    [Fact]
    public void Approve_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var review = CreateTestReview(productId: productId);
        review.ClearDomainEvents();

        // Act
        review.Approve();

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewApprovedEvent>().Single();
        domainEvent.ReviewId.ShouldBe(review.Id);
        domainEvent.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void Approve_CalledTwice_ShouldRaiseTwoEvents()
    {
        // Arrange
        var review = CreateTestReview();
        review.ClearDomainEvents();

        // Act
        review.Approve();
        review.Approve();

        // Assert - Approve is not guarded, so each call raises an event
        review.DomainEvents.OfType<ReviewApprovedEvent>().Count().ShouldBe(2);
    }

    #endregion

    #region Reject Domain Event

    [Fact]
    public void Reject_ShouldRaiseReviewRejectedEvent()
    {
        // Arrange
        var review = CreateTestReview();
        review.ClearDomainEvents();

        // Act
        review.Reject();

        // Assert
        review.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ReviewRejectedEvent>();
    }

    [Fact]
    public void Reject_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var review = CreateTestReview(productId: productId);
        review.ClearDomainEvents();

        // Act
        review.Reject();

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewRejectedEvent>().Single();
        domainEvent.ReviewId.ShouldBe(review.Id);
        domainEvent.ProductId.ShouldBe(productId);
    }

    #endregion

    #region AddAdminResponse Domain Event

    [Fact]
    public void AddAdminResponse_ShouldRaiseReviewAdminRespondedEvent()
    {
        // Arrange
        var review = CreateTestReview();
        review.ClearDomainEvents();

        // Act
        review.AddAdminResponse("Thank you for your feedback!");

        // Assert
        review.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ReviewAdminRespondedEvent>();
    }

    [Fact]
    public void AddAdminResponse_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var review = CreateTestReview(productId: productId);
        review.ClearDomainEvents();

        // Act
        review.AddAdminResponse("We appreciate your review!");

        // Assert
        var domainEvent = review.DomainEvents.OfType<ReviewAdminRespondedEvent>().Single();
        domainEvent.ReviewId.ShouldBe(review.Id);
        domainEvent.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void AddAdminResponse_CalledTwice_ShouldRaiseTwoEvents()
    {
        // Arrange
        var review = CreateTestReview();
        review.ClearDomainEvents();

        // Act
        review.AddAdminResponse("First response");
        review.AddAdminResponse("Updated response");

        // Assert
        review.DomainEvents.OfType<ReviewAdminRespondedEvent>().Count().ShouldBe(2);
    }

    #endregion
}
