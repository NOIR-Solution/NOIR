using NOIR.Domain.Entities.Wishlist;
using NOIR.Domain.Events.Wishlist;

namespace NOIR.Domain.UnitTests.Entities.Wishlist;

/// <summary>
/// Unit tests verifying that the Wishlist aggregate root raises
/// the correct domain events for creation, item management, and sharing.
/// </summary>
public class WishlistDomainEventTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";

    private static Domain.Entities.Wishlist.Wishlist CreateTestWishlist(
        string userId = TestUserId,
        string name = "My Wishlist",
        bool isDefault = true,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Wishlist.Wishlist.Create(userId, name, isDefault, tenantId);
    }

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaiseWishlistCreatedEvent()
    {
        // Act
        var wishlist = CreateTestWishlist();

        // Assert
        wishlist.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectWishlistId()
    {
        // Act
        var wishlist = CreateTestWishlist();

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistCreatedEvent>().Single();
        domainEvent.WishlistId.Should().Be(wishlist.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectUserId()
    {
        // Act
        var wishlist = CreateTestWishlist(userId: "user-789");

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistCreatedEvent>().Single();
        domainEvent.UserId.Should().Be("user-789");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectName()
    {
        // Act
        var wishlist = CreateTestWishlist(name: "Holiday Gift Ideas");

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistCreatedEvent>().Single();
        domainEvent.Name.Should().Be("Holiday Gift Ideas");
    }

    #endregion

    #region AddItem Domain Event

    [Fact]
    public void AddItem_ShouldRaiseWishlistItemAddedEvent()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();
        var productId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId);

        // Assert
        wishlist.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistItemAddedEvent>();
    }

    [Fact]
    public void AddItem_ShouldRaiseEventWithCorrectProductId()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();
        var productId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId);

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistItemAddedEvent>().Single();
        domainEvent.WishlistId.Should().Be(wishlist.Id);
        domainEvent.ProductId.Should().Be(productId);
        domainEvent.ProductVariantId.Should().BeNull();
    }

    [Fact]
    public void AddItem_WithVariant_ShouldRaiseEventWithVariantId()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId, variantId);

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistItemAddedEvent>().Single();
        domainEvent.ProductId.Should().Be(productId);
        domainEvent.ProductVariantId.Should().Be(variantId);
    }

    [Fact]
    public void AddItem_DuplicateProduct_ShouldNotRaiseEvent()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        wishlist.AddItem(productId);
        wishlist.ClearDomainEvents();

        // Act
        wishlist.AddItem(productId); // Duplicate

        // Assert
        wishlist.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_MultipleProducts_ShouldRaiseEventForEach()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();

        // Act
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Assert
        wishlist.DomainEvents.OfType<WishlistItemAddedEvent>().Should().HaveCount(3);
    }

    #endregion

    #region RemoveItem Domain Event

    [Fact]
    public void RemoveItem_ExistingItem_ShouldRaiseWishlistItemRemovedEvent()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());
        wishlist.ClearDomainEvents();

        // Act
        wishlist.RemoveItem(item.Id);

        // Assert
        wishlist.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistItemRemovedEvent>();
    }

    [Fact]
    public void RemoveItem_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());
        wishlist.ClearDomainEvents();

        // Act
        wishlist.RemoveItem(item.Id);

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistItemRemovedEvent>().Single();
        domainEvent.WishlistId.Should().Be(wishlist.Id);
        domainEvent.WishlistItemId.Should().Be(item.Id);
    }

    [Fact]
    public void RemoveItem_NonExistingItem_ShouldNotRaiseEvent()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.ClearDomainEvents();

        // Act
        wishlist.RemoveItem(Guid.NewGuid()); // Non-existing ID

        // Assert
        wishlist.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region GenerateShareToken Domain Event

    [Fact]
    public void GenerateShareToken_ShouldRaiseWishlistSharedEvent()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();

        // Act
        wishlist.GenerateShareToken();

        // Assert
        wishlist.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistSharedEvent>();
    }

    [Fact]
    public void GenerateShareToken_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist(userId: "user-share-test");
        wishlist.ClearDomainEvents();

        // Act
        wishlist.GenerateShareToken();

        // Assert
        var domainEvent = wishlist.DomainEvents.OfType<WishlistSharedEvent>().Single();
        domainEvent.WishlistId.Should().Be(wishlist.Id);
        domainEvent.UserId.Should().Be("user-share-test");
    }

    [Fact]
    public void GenerateShareToken_CalledTwice_ShouldRaiseTwoEvents()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.ClearDomainEvents();

        // Act
        wishlist.GenerateShareToken();
        wishlist.GenerateShareToken();

        // Assert
        wishlist.DomainEvents.OfType<WishlistSharedEvent>().Should().HaveCount(2);
    }

    #endregion
}
