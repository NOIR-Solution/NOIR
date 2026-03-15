using NOIR.Domain.Entities.Order;

namespace NOIR.Domain.UnitTests.Entities.Order;

/// <summary>
/// Unit tests for the OrderNote entity.
/// Tests factory method, property initialization, and default values.
/// </summary>
public class OrderNoteTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestOrderId = Guid.NewGuid();

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var note = OrderNote.Create(orderId, "Urgent: contact customer", "user-123", "Admin User", TestTenantId);

        // Assert
        note.ShouldNotBeNull();
        note.Id.ShouldNotBe(Guid.Empty);
        note.OrderId.ShouldBe(orderId);
        note.Content.ShouldBe("Urgent: contact customer");
        note.CreatedByUserId.ShouldBe("user-123");
        note.CreatedByUserName.ShouldBe("Admin User");
        note.IsInternal.ShouldBeTrue();
        note.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldAlwaysBeInternal()
    {
        // Act
        var note = OrderNote.Create(TestOrderId, "Note content", "user-1", "User Name");

        // Assert
        note.IsInternal.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var note = OrderNote.Create(TestOrderId, "Note", "user-1", "User");

        // Assert
        note.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var note1 = OrderNote.Create(TestOrderId, "Note 1", "user-1", "User 1");
        var note2 = OrderNote.Create(TestOrderId, "Note 2", "user-2", "User 2");

        // Assert
        note1.Id.ShouldNotBe(note2.Id);
    }

    [Fact]
    public void Create_WithDifferentOrderIds_ShouldRespectOrderId()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        // Act
        var note1 = OrderNote.Create(orderId1, "Note 1", "user-1", "User 1");
        var note2 = OrderNote.Create(orderId2, "Note 2", "user-1", "User 1");

        // Assert
        note1.OrderId.ShouldBe(orderId1);
        note2.OrderId.ShouldBe(orderId2);
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldNotThrow()
    {
        // Act - entity does not validate empty content; that's the application layer's job
        var note = OrderNote.Create(TestOrderId, "", "user-1", "User");

        // Assert
        note.Content.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithLongContent_ShouldStoreFullContent()
    {
        // Arrange
        var longContent = new string('A', 5000);

        // Act
        var note = OrderNote.Create(TestOrderId, longContent, "user-1", "User");

        // Assert
        note.Content.ShouldBe(longContent);
        note.Content.Length.ShouldBe(5000);
    }

    [Fact]
    public void Create_ShouldSetCorrectUserId()
    {
        // Act
        var note = OrderNote.Create(TestOrderId, "Content", "admin-user-456", "Admin Name");

        // Assert
        note.CreatedByUserId.ShouldBe("admin-user-456");
    }

    [Fact]
    public void Create_ShouldSetCorrectUserName()
    {
        // Act
        var note = OrderNote.Create(TestOrderId, "Content", "user-1", "Nguyen Van A");

        // Assert
        note.CreatedByUserName.ShouldBe("Nguyen Van A");
    }

    #endregion
}
