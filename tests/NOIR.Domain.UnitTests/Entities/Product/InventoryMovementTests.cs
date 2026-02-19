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
        movement.Should().NotBeNull();
        movement.Id.Should().NotBe(Guid.Empty);
        movement.ProductVariantId.Should().Be(TestVariantId);
        movement.ProductId.Should().Be(TestProductId);
        movement.MovementType.Should().Be(InventoryMovementType.StockIn);
        movement.QuantityBefore.Should().Be(100);
        movement.QuantityMoved.Should().Be(50);
        movement.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldCalculateQuantityAfterCorrectly()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 100, quantityMoved: 50);

        // Assert
        movement.QuantityAfter.Should().Be(150); // 100 + 50
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
        movement.QuantityAfter.Should().Be(70); // 100 + (-30)
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
        movement.Reference.Should().Be("ORD-001");
        movement.Notes.Should().Be("Stock received from supplier");
        movement.UserId.Should().Be("admin-user");
        movement.CorrelationId.Should().Be("corr-12345");
    }

    [Fact]
    public void Create_WithNullOptionalParameters_ShouldAllowNulls()
    {
        // Act
        var movement = CreateTestMovement(
            reference: null, notes: null, userId: null, correlationId: null);

        // Assert
        movement.Reference.Should().BeNull();
        movement.Notes.Should().BeNull();
        movement.UserId.Should().BeNull();
        movement.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var movement = CreateTestMovement(tenantId: null);

        // Assert
        movement.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var movement1 = CreateTestMovement();
        var movement2 = CreateTestMovement();

        // Assert
        movement1.Id.Should().NotBe(movement2.Id);
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
        movement.MovementType.Should().Be(type);
    }

    [Fact]
    public void Create_WithUndefinedMovementType_ShouldThrow()
    {
        // Act
        var act = () => CreateTestMovement(movementType: (InventoryMovementType)999);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid inventory movement type*");
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
        movement.QuantityAfter.Should().Be(expectedAfter);
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
        movement.QuantityAfter.Should().Be(150);
        movement.QuantityAfter.Should().BeGreaterThan(movement.QuantityBefore);
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
        movement.QuantityAfter.Should().Be(70);
        movement.QuantityAfter.Should().BeLessThan(movement.QuantityBefore);
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
        movement.Reference.Should().Be(reference);
    }

    [Fact]
    public void Create_WithReferenceExactly100Chars_ShouldPreserveFullReference()
    {
        // Arrange
        var reference = new string('X', 100);

        // Act
        var movement = CreateTestMovement(reference: reference);

        // Assert
        movement.Reference.Should().Be(reference);
        movement.Reference!.Length.Should().Be(100);
    }

    [Fact]
    public void Create_WithReferenceOver100Chars_ShouldTruncateTo100()
    {
        // Arrange
        var reference = new string('X', 150);

        // Act
        var movement = CreateTestMovement(reference: reference);

        // Assert
        movement.Reference!.Length.Should().Be(100);
        movement.Reference.Should().Be(new string('X', 100));
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
        movement.Notes.Should().Be(notes);
    }

    [Fact]
    public void Create_WithNotesExactly500Chars_ShouldPreserveFullNotes()
    {
        // Arrange
        var notes = new string('N', 500);

        // Act
        var movement = CreateTestMovement(notes: notes);

        // Assert
        movement.Notes.Should().Be(notes);
        movement.Notes!.Length.Should().Be(500);
    }

    [Fact]
    public void Create_WithNotesOver500Chars_ShouldTruncateAndAddEllipsis()
    {
        // Arrange
        var notes = new string('N', 600);

        // Act
        var movement = CreateTestMovement(notes: notes);

        // Assert
        movement.Notes!.Length.Should().Be(503); // 500 chars + "..."
        movement.Notes.Should().EndWith("...");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithZeroQuantityBefore_ShouldSucceed()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 0, quantityMoved: 100);

        // Assert
        movement.QuantityBefore.Should().Be(0);
        movement.QuantityAfter.Should().Be(100);
    }

    [Fact]
    public void Create_WithZeroQuantityMoved_ShouldSucceed()
    {
        // Act
        var movement = CreateTestMovement(quantityBefore: 100, quantityMoved: 0);

        // Assert
        movement.QuantityMoved.Should().Be(0);
        movement.QuantityAfter.Should().Be(100);
    }

    [Fact]
    public void Create_ResultingInNegativeStock_ShouldNotThrow()
    {
        // Act - business logic should prevent overselling, entity stores the fact
        var movement = CreateTestMovement(quantityBefore: 10, quantityMoved: -50);

        // Assert
        movement.QuantityAfter.Should().Be(-40);
    }

    #endregion
}
