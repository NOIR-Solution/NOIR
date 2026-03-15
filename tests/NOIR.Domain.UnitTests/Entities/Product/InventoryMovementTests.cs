using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product;

/// <summary>
/// Unit tests for the InventoryMovement entity.
/// Tests factory method, computed QuantityAfter, reference/notes truncation,
/// movement type validation, and various movement scenarios.
/// </summary>
public class InventoryMovementTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestVariantId = Guid.NewGuid();
    private static readonly Guid TestProductId = Guid.NewGuid();

    #region Helper Methods

    private static InventoryMovement CreateTestMovement(
        InventoryMovementType movementType = InventoryMovementType.StockIn,
        int quantityBefore = 100,
        int quantityMoved = 50,
        string? reference = null,
        string? notes = null,
        string? userId = null,
        string? correlationId = null,
        string? tenantId = TestTenantId)
    {
        return InventoryMovement.Create(
            TestVariantId, TestProductId,
            movementType, quantityBefore, quantityMoved,
            tenantId, reference, notes, userId, correlationId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidMovement()
    {
        // Act
        var movement = CreateTestMovement();

        // Assert
        movement.ShouldNotBeNull();
        movement.Id.ShouldNotBe(Guid.Empty);
        movement.ProductVariantId.ShouldBe(TestVariantId);
        movement.ProductId.ShouldBe(TestProductId);
        movement.MovementType.ShouldBe(InventoryMovementType.StockIn);
        movement.QuantityBefore.ShouldBe(100);
        movement.QuantityMoved.ShouldBe(50);
        movement.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldCalculateQuantityAfterCorrectly()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 100, quantityMoved: 50);

        // Assert
        movement.QuantityAfter.ShouldBe(150); // 100 + 50
    }

    [Fact]
    public void Create_WithNegativeMoved_ShouldCalculateQuantityAfterCorrectly()
    {
        // Act - negative quantityMoved represents outflow
        var movement = CreateTestMovement(
            movementType: InventoryMovementType.StockOut,
            quantityBefore: 100,
            quantityMoved: -30);

        // Assert
        movement.QuantityAfter.ShouldBe(70); // 100 + (-30)
    }

    [Fact]
    public void Create_WithAllOptionalParameters_ShouldSetAllProperties()
    {
        // Act
        var movement = CreateTestMovement(
            reference: "ORD-001",
            notes: "Stock received from supplier",
            userId: "admin-user",
            correlationId: "corr-12345");

        // Assert
        movement.Reference.ShouldBe("ORD-001");
        movement.Notes.ShouldBe("Stock received from supplier");
        movement.UserId.ShouldBe("admin-user");
        movement.CorrelationId.ShouldBe("corr-12345");
    }

    [Fact]
    public void Create_WithNullOptionalParameters_ShouldAllowNulls()
    {
        // Act
        var movement = CreateTestMovement(
            reference: null, notes: null, userId: null, correlationId: null);

        // Assert
        movement.Reference.ShouldBeNull();
        movement.Notes.ShouldBeNull();
        movement.UserId.ShouldBeNull();
        movement.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var movement = CreateTestMovement(tenantId: null);

        // Assert
        movement.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var movement1 = CreateTestMovement();
        var movement2 = CreateTestMovement();

        // Assert
        movement1.Id.ShouldNotBe(movement2.Id);
    }

    #endregion

    #region Movement Type Tests

    [Theory]
    [InlineData(InventoryMovementType.StockIn)]
    [InlineData(InventoryMovementType.StockOut)]
    [InlineData(InventoryMovementType.Adjustment)]
    [InlineData(InventoryMovementType.Return)]
    [InlineData(InventoryMovementType.Reservation)]
    [InlineData(InventoryMovementType.ReservationRelease)]
    [InlineData(InventoryMovementType.Damaged)]
    [InlineData(InventoryMovementType.Expired)]
    public void Create_WithAllMovementTypes_ShouldSetCorrectType(InventoryMovementType type)
    {
        // Act
        var movement = CreateTestMovement(movementType: type);

        // Assert
        movement.MovementType.ShouldBe(type);
    }

    [Fact]
    public void Create_WithUndefinedMovementType_ShouldThrow()
    {
        // Act
        var act = () => CreateTestMovement(movementType: (InventoryMovementType)999);

        // Assert
        Should.Throw<ArgumentException>(act)
            .Message.ShouldContain("Invalid inventory movement type");
    }

    #endregion

    #region QuantityAfter Calculation Tests

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(100, 50, 150)]
    [InlineData(100, -50, 50)]
    [InlineData(0, -10, -10)]
    [InlineData(50, 0, 50)]
    public void Create_QuantityAfter_ShouldEqualQuantityBeforePlusMoved(
        int quantityBefore, int quantityMoved, int expectedAfter)
    {
        // Act
        var movement = InventoryMovement.Create(
            TestVariantId, TestProductId,
            InventoryMovementType.Adjustment,
            quantityBefore, quantityMoved, TestTenantId);

        // Assert
        movement.QuantityAfter.ShouldBe(expectedAfter);
    }

    [Fact]
    public void Create_StockIn_ShouldIncreaseQuantity()
    {
        // Act
        var movement = CreateTestMovement(
            movementType: InventoryMovementType.StockIn,
            quantityBefore: 50,
            quantityMoved: 100);

        // Assert
        movement.QuantityAfter.ShouldBe(150);
        movement.QuantityAfter.ShouldBeGreaterThan(movement.QuantityBefore);
    }

    [Fact]
    public void Create_StockOut_ShouldDecreaseQuantity()
    {
        // Act
        var movement = CreateTestMovement(
            movementType: InventoryMovementType.StockOut,
            quantityBefore: 100,
            quantityMoved: -30);

        // Assert
        movement.QuantityAfter.ShouldBe(70);
        movement.QuantityAfter.ShouldBeLessThan(movement.QuantityBefore);
    }

    #endregion

    #region Reference Truncation Tests

    [Fact]
    public void Create_WithReferenceUnder100Chars_ShouldPreserveFullReference()
    {
        // Arrange
        var reference = "ORD-20260219-0001";

        // Act
        var movement = CreateTestMovement(reference: reference);

        // Assert
        movement.Reference.ShouldBe(reference);
    }

    [Fact]
    public void Create_WithReferenceExactly100Chars_ShouldPreserveFullReference()
    {
        // Arrange
        var reference = new string('X', 100);

        // Act
        var movement = CreateTestMovement(reference: reference);

        // Assert
        movement.Reference.ShouldBe(reference);
        movement.Reference!.Length.ShouldBe(100);
    }

    [Fact]
    public void Create_WithReferenceOver100Chars_ShouldTruncateTo100()
    {
        // Arrange
        var reference = new string('X', 150);

        // Act
        var movement = CreateTestMovement(reference: reference);

        // Assert
        movement.Reference!.Length.ShouldBe(100);
        movement.Reference.ShouldBe(new string('X', 100));
    }

    #endregion

    #region Notes Truncation Tests

    [Fact]
    public void Create_WithNotesUnder500Chars_ShouldPreserveFullNotes()
    {
        // Arrange
        var notes = "Stock received from supplier ABC.";

        // Act
        var movement = CreateTestMovement(notes: notes);

        // Assert
        movement.Notes.ShouldBe(notes);
    }

    [Fact]
    public void Create_WithNotesExactly500Chars_ShouldPreserveFullNotes()
    {
        // Arrange
        var notes = new string('N', 500);

        // Act
        var movement = CreateTestMovement(notes: notes);

        // Assert
        movement.Notes.ShouldBe(notes);
        movement.Notes!.Length.ShouldBe(500);
    }

    [Fact]
    public void Create_WithNotesOver500Chars_ShouldTruncateAndAddEllipsis()
    {
        // Arrange
        var notes = new string('N', 600);

        // Act
        var movement = CreateTestMovement(notes: notes);

        // Assert
        movement.Notes!.Length.ShouldBe(503); // 500 chars + "..."
        movement.Notes.ShouldEndWith("...");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithZeroQuantityBefore_ShouldSucceed()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 0, quantityMoved: 100);

        // Assert
        movement.QuantityBefore.ShouldBe(0);
        movement.QuantityAfter.ShouldBe(100);
    }

    [Fact]
    public void Create_WithZeroQuantityMoved_ShouldSucceed()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 100, quantityMoved: 0);

        // Assert
        movement.QuantityMoved.ShouldBe(0);
        movement.QuantityAfter.ShouldBe(100);
    }

    [Fact]
    public void Create_ResultingInNegativeStock_ShouldNotThrow()
    {
        // Act - business logic should prevent overselling, entity stores the fact
        var movement = CreateTestMovement(quantityBefore: 10, quantityMoved: -50);

        // Assert
        movement.QuantityAfter.ShouldBe(-40);
    }

    #endregion
}
