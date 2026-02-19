using NOIR.Domain.Entities.Wishlist;

namespace NOIR.Domain.UnitTests.Entities.Wishlist;

/// <summary>
/// Unit tests for the Wishlist aggregate root entity.
/// Tests factory methods, item management, sharing/privacy,
/// name updates, and computed properties.
/// </summary>
public class WishlistTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";

    /// <summary>
    /// Helper to create a standard wishlist for testing.
    /// </summary>
    private static Domain.Entities.Wishlist.Wishlist CreateTestWishlist(
        string userId = TestUserId,
        string name = "My Wishlist",
        bool isDefault = true,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Wishlist.Wishlist.Create(userId, name, isDefault, tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidWishlist()
    {
        // Act
        var wishlist = CreateTestWishlist();

        // Assert
        wishlist.Should().NotBeNull();
        wishlist.Id.Should().NotBe(Guid.Empty);
        wishlist.UserId.Should().Be(TestUserId);
        wishlist.Name.Should().Be("My Wishlist");
        wishlist.IsDefault.Should().BeTrue();
        wishlist.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var wishlist = CreateTestWishlist();

        // Assert
        wishlist.IsPublic.Should().BeFalse();
        wishlist.ShareToken.Should().BeNull();
        wishlist.Items.Should().BeEmpty();
        wishlist.ItemCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithIsDefaultFalse_ShouldSetFlag()
    {
        // Act
        var wishlist = CreateTestWishlist(isDefault: false);

        // Assert
        wishlist.IsDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceUserId_ShouldThrow(string? userId)
    {
        // Act
        var act = () => CreateTestWishlist(userId: userId!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceName_ShouldThrow(string? name)
    {
        // Act
        var act = () => CreateTestWishlist(name: name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var wishlist = CreateTestWishlist(tenantId: null);

        // Assert
        wishlist.TenantId.Should().BeNull();
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        wishlist.UpdateName("Holiday Gift Ideas");

        // Assert
        wishlist.Name.Should().Be("Holiday Gift Ideas");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_WithNullOrWhiteSpaceName_ShouldThrow(string? name)
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var act = () => wishlist.UpdateName(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var originalUserId = wishlist.UserId;
        var originalIsDefault = wishlist.IsDefault;
        var originalIsPublic = wishlist.IsPublic;

        // Act
        wishlist.UpdateName("New Name");

        // Assert
        wishlist.UserId.Should().Be(originalUserId);
        wishlist.IsDefault.Should().Be(originalIsDefault);
        wishlist.IsPublic.Should().Be(originalIsPublic);
    }

    #endregion

    #region SetPublic Tests

    [Fact]
    public void SetPublic_ToTrue_ShouldMakeWishlistPublic()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        wishlist.SetPublic(true);

        // Assert
        wishlist.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void SetPublic_ToFalse_ShouldMakeWishlistPrivate()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.SetPublic(true);

        // Act
        wishlist.SetPublic(false);

        // Assert
        wishlist.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void SetPublic_ToFalse_ShouldClearShareToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.GenerateShareToken(); // Sets IsPublic = true and generates token
        wishlist.ShareToken.Should().NotBeNullOrEmpty();

        // Act
        wishlist.SetPublic(false);

        // Assert
        wishlist.IsPublic.Should().BeFalse();
        wishlist.ShareToken.Should().BeNull();
    }

    [Fact]
    public void SetPublic_ToTrue_ShouldNotGenerateShareToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        wishlist.SetPublic(true);

        // Assert - Setting public alone does not generate a share token
        wishlist.IsPublic.Should().BeTrue();
        wishlist.ShareToken.Should().BeNull();
    }

    #endregion

    #region GenerateShareToken Tests

    [Fact]
    public void GenerateShareToken_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var token = wishlist.GenerateShareToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        wishlist.ShareToken.Should().Be(token);
    }

    [Fact]
    public void GenerateShareToken_ShouldSetIsPublicToTrue()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.IsPublic.Should().BeFalse();

        // Act
        wishlist.GenerateShareToken();

        // Assert
        wishlist.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void GenerateShareToken_ShouldGenerateUrlSafeToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var token = wishlist.GenerateShareToken();

        // Assert - Token should not contain URL-unsafe characters
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void GenerateShareToken_CalledTwice_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var token1 = wishlist.GenerateShareToken();
        var token2 = wishlist.GenerateShareToken();

        // Assert - Tokens should be different (using random bytes)
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateShareToken_ShouldHaveReasonableLength()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var token = wishlist.GenerateShareToken();

        // Assert - 32 bytes encoded as base64 with replacements
        // Base64 of 32 bytes = 44 chars, minus padding = ~43 chars
        token.Length.Should().BeGreaterThan(20);
        token.Length.Should().BeLessThan(50);
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_WithProductOnly_ShouldAddItemSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();

        // Act
        var item = wishlist.AddItem(productId);

        // Assert
        item.Should().NotBeNull();
        wishlist.Items.Should().HaveCount(1);
        item.WishlistId.Should().Be(wishlist.Id);
        item.ProductId.Should().Be(productId);
        item.ProductVariantId.Should().BeNull();
        item.Note.Should().BeNull();
        item.Priority.Should().Be(WishlistItemPriority.None);
    }

    [Fact]
    public void AddItem_WithProductAndVariant_ShouldAddItemSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        var item = wishlist.AddItem(productId, variantId);

        // Assert
        item.ProductId.Should().Be(productId);
        item.ProductVariantId.Should().Be(variantId);
    }

    [Fact]
    public void AddItem_WithNote_ShouldSetNote()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();

        // Act
        var item = wishlist.AddItem(productId, note: "Birthday gift idea");

        // Assert
        item.Note.Should().Be("Birthday gift idea");
    }

    [Fact]
    public void AddItem_ShouldSetAddedAtTimestamp()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var beforeAdd = DateTimeOffset.UtcNow;

        // Act
        var item = wishlist.AddItem(productId);

        // Assert
        item.AddedAt.Should().BeOnOrAfter(beforeAdd);
    }

    [Fact]
    public void AddItem_ShouldSetDefaultPriorityToNone()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        var item = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item.Priority.Should().Be(WishlistItemPriority.None);
    }

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
        secondItem.Should().BeSameAs(firstItem);
        wishlist.Items.Should().HaveCount(1);
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
        secondItem.Should().BeSameAs(firstItem);
        wishlist.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_SameProductDifferentVariants_ShouldAddBoth()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var variant1 = Guid.NewGuid();
        var variant2 = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId, variant1);
        wishlist.AddItem(productId, variant2);

        // Assert
        wishlist.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_SameProductWithAndWithoutVariant_ShouldAddBoth()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        wishlist.AddItem(productId); // No variant
        wishlist.AddItem(productId, variantId); // With variant

        // Assert
        wishlist.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_MultipleProducts_ShouldAddAll()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Assert
        wishlist.Items.Should().HaveCount(3);
    }

    [Fact]
    public void AddItem_ShouldSetTenantId()
    {
        // Arrange
        var wishlist = CreateTestWishlist(tenantId: TestTenantId);

        // Act
        var item = wishlist.AddItem(Guid.NewGuid());

        // Assert
        item.TenantId.Should().Be(TestTenantId);
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_ExistingItem_ShouldRemoveSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.RemoveItem(item.Id);

        // Assert
        wishlist.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistingItem_ShouldDoNothing()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.RemoveItem(Guid.NewGuid()); // Random ID that doesn't exist

        // Assert
        wishlist.Items.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveItem_FromMultipleItems_ShouldOnlyRemoveSpecifiedItem()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item1 = wishlist.AddItem(Guid.NewGuid());
        var item2 = wishlist.AddItem(Guid.NewGuid());
        var item3 = wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.RemoveItem(item2.Id);

        // Assert
        wishlist.Items.Should().HaveCount(2);
        wishlist.Items.Should().Contain(item1);
        wishlist.Items.Should().NotContain(item2);
        wishlist.Items.Should().Contain(item3);
    }

    [Fact]
    public void RemoveItem_AllItems_ShouldResultInEmptyCollection()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item1 = wishlist.AddItem(Guid.NewGuid());
        var item2 = wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.RemoveItem(item1.Id);
        wishlist.RemoveItem(item2.Id);

        // Assert
        wishlist.Items.Should().BeEmpty();
        wishlist.ItemCount.Should().Be(0);
    }

    #endregion

    #region ItemCount Computed Property Tests

    [Fact]
    public void ItemCount_EmptyWishlist_ShouldBeZero()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Assert
        wishlist.ItemCount.Should().Be(0);
    }

    [Fact]
    public void ItemCount_AfterAddingItems_ShouldReflectCount()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Assert
        wishlist.ItemCount.Should().Be(3);
    }

    [Fact]
    public void ItemCount_AfterRemovingItem_ShouldDecrease()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.RemoveItem(item.Id);

        // Assert
        wishlist.ItemCount.Should().Be(1);
    }

    [Fact]
    public void ItemCount_AddingDuplicate_ShouldNotIncrease()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var productId = Guid.NewGuid();
        wishlist.AddItem(productId);

        // Act
        wishlist.AddItem(productId); // Duplicate

        // Assert
        wishlist.ItemCount.Should().Be(1);
    }

    #endregion

    #region WishlistItem Entity Tests

    [Fact]
    public void WishlistItem_UpdateNote_ShouldUpdateSuccessfully()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Original note");

        // Act
        item.UpdateNote("Updated note");

        // Assert
        item.Note.Should().Be("Updated note");
    }

    [Fact]
    public void WishlistItem_UpdateNote_WithNull_ShouldClearNote()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid(), note: "Some note");

        // Act
        item.UpdateNote(null);

        // Assert
        item.Note.Should().BeNull();
    }

    [Theory]
    [InlineData(WishlistItemPriority.None)]
    [InlineData(WishlistItemPriority.Low)]
    [InlineData(WishlistItemPriority.Medium)]
    [InlineData(WishlistItemPriority.High)]
    public void WishlistItem_UpdatePriority_ShouldSetCorrectPriority(WishlistItemPriority priority)
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        item.UpdatePriority(priority);

        // Assert
        item.Priority.Should().Be(priority);
    }

    [Fact]
    public void WishlistItem_UpdatePriority_MultipleTimes_ShouldUseLatestValue()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var item = wishlist.AddItem(Guid.NewGuid());

        // Act
        item.UpdatePriority(WishlistItemPriority.Low);
        item.UpdatePriority(WishlistItemPriority.High);
        item.UpdatePriority(WishlistItemPriority.Medium);

        // Assert
        item.Priority.Should().Be(WishlistItemPriority.Medium);
    }

    #endregion

    #region Sharing Workflow Tests

    [Fact]
    public void SharingWorkflow_GenerateTokenThenUnshare()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act - Generate share token (makes public)
        var token = wishlist.GenerateShareToken();

        // Assert
        wishlist.IsPublic.Should().BeTrue();
        wishlist.ShareToken.Should().Be(token);

        // Act - Unshare (makes private, clears token)
        wishlist.SetPublic(false);

        // Assert
        wishlist.IsPublic.Should().BeFalse();
        wishlist.ShareToken.Should().BeNull();
    }

    [Fact]
    public void SharingWorkflow_SetPublicThenGenerateToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();

        // Act - Set public without token
        wishlist.SetPublic(true);
        wishlist.IsPublic.Should().BeTrue();
        wishlist.ShareToken.Should().BeNull();

        // Act - Generate token
        var token = wishlist.GenerateShareToken();

        // Assert
        wishlist.IsPublic.Should().BeTrue();
        wishlist.ShareToken.Should().Be(token);
    }

    [Fact]
    public void SharingWorkflow_RegenerateToken_ShouldReplaceOldToken()
    {
        // Arrange
        var wishlist = CreateTestWishlist();
        var oldToken = wishlist.GenerateShareToken();

        // Act
        var newToken = wishlist.GenerateShareToken();

        // Assert
        newToken.Should().NotBe(oldToken);
        wishlist.ShareToken.Should().Be(newToken);
    }

    #endregion

    // Note: Enum membership is implicitly verified by UpdatePriority_AllValues tests.
    // Ordinal ordering is a stable domain invariant worth keeping.
    [Fact]
    public void WishlistItemPriority_ShouldHaveCorrectOrdinalValues()
    {
        // Assert - Verify ordering makes sense for sorting
        ((int)WishlistItemPriority.None).Should().BeLessThan((int)WishlistItemPriority.Low);
        ((int)WishlistItemPriority.Low).Should().BeLessThan((int)WishlistItemPriority.Medium);
        ((int)WishlistItemPriority.Medium).Should().BeLessThan((int)WishlistItemPriority.High);
    }
}
