using NOIR.Domain.Entities.Wishlist;

namespace NOIR.Domain.UnitTests.Entities.Wishlist;

/// <summary>
/// Unit tests for the WishlistItem entity.
/// WishlistItem.Create is internal, so all items are created via Wishlist.AddItem.
/// Tests property initialization, note updates, priority updates, and edge cases.
/// </summary>
public class WishlistItemTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";

    #region Helper Methods

    private static Domain.Entities.Wishlist.Wishlist CreateTestWishlist(string? tenantId = TestTenantId)
    {
        return Domain.Entities.Wishlist.Wishlist.Create(TestUserId, "Test Wishlist", true, tenantId);
    }

    #endregion

    #region Creation Tests (via Wishlist.AddItem)

    [Fact]
    public void Create_WithProductOnly_ShouldSetBasicProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();

        // Act
        var item = wishlist.AddItem(productId);

        // Assert
        item.ShouldNotBeNull();
        item.Id.ShouldNotBe(Guid.Empty);
        item.WishlistId.ShouldBe(wishlist.Id);
        item.ProductId.ShouldBe(productId);
        item.ProductVariantId.ShouldBeNull();
        item.Note.ShouldBeNull();
        item.Priority.ShouldBe(WishlistItemPriority.None);
    }

    [Fact]
    public void Create_WithProductAndVariant_ShouldSetVariantId()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        var item = wishlist.AddItem(productId, variantId);

        // Assert
        item.ProductId.ShouldBe(productId);
        item.ProductVariantId.ShouldBe(variantId);
    }

    [Fact]
    public void Create_WithNote_ShouldSetNote()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Birthday gift idea");

        // Assert
        item.Note.ShouldBe("Birthday gift idea");
    }

    [Fact]
    public void Create_ShouldSetAddedAtTimestamp()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var beforeAdd = DateTimeOffset.UtcNow;

        // Act
        var item = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item.AddedAt.ShouldBeGreaterThanOrEqualTo(beforeAdd);
    }

    [Fact]
    public void Create_ShouldSetTenantIdFromWishlist()
    {
        // Arrange
        var wishlist = CreateTestWishlist(tenantId: "custom-tenant");

        // Act
        var item = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item.TenantId.ShouldBe("custom-tenant");
    }

    [Fact]
    public void Create_ShouldDefaultPriorityToNone()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var item = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item.Priority.ShouldBe(WishlistItemPriority.None);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var item1 = wishlist.AddItem(Guid.NewGuid());
        var item2 = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item1.Id.ShouldNotBe(item2.Id);
    }

    #endregion

    #region UpdateNote Tests

    [Fact]
    public void UpdateNote_WithValidNote_ShouldUpdateSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Original note");

        // Act
        item.UpdateNote("Updated note");

        // Assert
        item.Note.ShouldBe("Updated note");
    }

    [Fact]
    public void UpdateNote_WithNull_ShouldClearNote()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Some note");

        // Act
        item.UpdateNote(null);

        // Assert
        item.Note.ShouldBeNull();
    }

    [Fact]
    public void UpdateNote_WithEmptyString_ShouldSetEmptyString()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Some note");

        // Act
        item.UpdateNote("");

        // Assert
        item.Note.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateNote_MultipleTimes_ShouldUseLatestValue()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        item.UpdateNote("First");
        item.UpdateNote("Second");
        item.UpdateNote("Third");

        // Assert
        item.Note.ShouldBe("Third");
    }

    [Fact]
    public void UpdateNote_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var item = wishlist.AddItem(productId);
        item.UpdatePriority(WishlistItemPriority.High);
        var originalAddedAt = item.AddedAt;

        // Act
        item.UpdateNote("New note");

        // Assert
        item.ProductId.ShouldBe(productId);
        item.Priority.ShouldBe(WishlistItemPriority.High);
        item.AddedAt.ShouldBe(originalAddedAt);
    }

    #endregion

    #region UpdatePriority Tests

    [Theory]
    [InlineData(WishlistItemPriority.None)]
    [InlineData(WishlistItemPriority.Low)]
    [InlineData(WishlistItemPriority.Medium)]
    [InlineData(WishlistItemPriority.High)]
    public void UpdatePriority_WithAllValues_ShouldSetCorrectPriority(WishlistItemPriority priority)
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        item.UpdatePriority(priority);

        // Assert
        item.Priority.ShouldBe(priority);
    }

    [Fact]
    public void UpdatePriority_MultipleTimes_ShouldUseLatestValue()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        item.UpdatePriority(WishlistItemPriority.Low);
        item.UpdatePriority(WishlistItemPriority.High);
        item.UpdatePriority(WishlistItemPriority.Medium);

        // Assert
        item.Priority.ShouldBe(WishlistItemPriority.Medium);
    }

    [Fact]
    public void UpdatePriority_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Gift idea");
        var originalNote = item.Note;
        var originalAddedAt = item.AddedAt;

        // Act
        item.UpdatePriority(WishlistItemPriority.High);

        // Assert
        item.Note.ShouldBe(originalNote);
        item.AddedAt.ShouldBe(originalAddedAt);
    }

    #endregion

    #region Duplicate Detection Tests

    [Fact]
    public void AddItem_DuplicateProductWithoutVariant_ShouldReturnExistingItem()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var firstItem = wishlist.AddItem(productId);

        // Act
        var secondItem = wishlist.AddItem(productId);

        // Assert
        secondItem.ShouldBeSameAs(firstItem);
        wishlist.Items.Count().ShouldBe(1);
    }

    [Fact]
    public void AddItem_DuplicateProductWithSameVariant_ShouldReturnExistingItem()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var firstItem = wishlist.AddItem(productId, variantId);

        // Act
        var secondItem = wishlist.AddItem(productId, variantId);

        // Assert
        secondItem.ShouldBeSameAs(firstItem);
        wishlist.Items.Count().ShouldBe(1);
    }

    [Fact]
    public void AddItem_SameProductDifferentVariants_ShouldAddBoth()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId, Guid.NewGuid());
        wishlist.AddItem(productId, Guid.NewGuid());

        // Assert
        wishlist.Items.Count().ShouldBe(2);
    }

    [Fact]
    public void AddItem_SameProductWithAndWithoutVariant_ShouldAddBoth()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId);
        wishlist.AddItem(productId, Guid.NewGuid());

        // Assert
        wishlist.Items.Count().ShouldBe(2);
    }

    #endregion
}
