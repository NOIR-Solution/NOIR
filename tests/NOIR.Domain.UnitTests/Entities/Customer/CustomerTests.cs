using NOIR.Domain.Entities.Customer;

namespace NOIR.Domain.UnitTests.Entities.Customer;

/// <summary>
/// Unit tests for the Customer aggregate root entity.
/// Tests factory methods, profile updates, RFM metrics, loyalty points,
/// segment recalculation, tier updates, tags, notes, and activation.
/// </summary>
public class CustomerTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestEmail = "john@example.com";
    private const string TestFirstName = "John";
    private const string TestLastName = "Doe";
    private const string TestPhone = "+84912345678";

    private static NOIR.Domain.Entities.Customer.Customer CreateTestCustomer(
        string? userId = TestUserId,
        string email = TestEmail,
        string firstName = TestFirstName,
        string lastName = TestLastName,
        string? phone = TestPhone,
        string? tenantId = TestTenantId)
    {
        return NOIR.Domain.Entities.Customer.Customer.Create(userId, email, firstName, lastName, phone, tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldCreateValidCustomer()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBe(Guid.Empty);
        customer.UserId.Should().Be(TestUserId);
        customer.Email.Should().Be(TestEmail);
        customer.FirstName.Should().Be(TestFirstName);
        customer.LastName.Should().Be(TestLastName);
        customer.Phone.Should().Be(TestPhone);
        customer.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultSegmentToNew()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.New);
    }

    [Fact]
    public void Create_ShouldSetDefaultTierToStandard()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.Tier.Should().Be(CustomerTier.Standard);
    }

    [Fact]
    public void Create_ShouldSetIsActiveTrue()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldInitializeRfmMetricsToDefaults()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.LastOrderDate.Should().BeNull();
        customer.TotalOrders.Should().Be(0);
        customer.TotalSpent.Should().Be(0m);
        customer.AverageOrderValue.Should().Be(0m);
    }

    [Fact]
    public void Create_ShouldInitializeLoyaltyPointsToZero()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.LoyaltyPoints.Should().Be(0);
        customer.LifetimeLoyaltyPoints.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldInitializeTagsAndNotesAsNull()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.Tags.Should().BeNull();
        customer.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyAddressCollection()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.Addresses.Should().NotBeNull();
        customer.Addresses.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyGroupMemberships()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.GroupMemberships.Should().NotBeNull();
        customer.GroupMemberships.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNullUserId_ShouldAllowGuestCustomer()
    {
        // Act
        var customer = CreateTestCustomer(userId: null);

        // Assert
        customer.UserId.Should().BeNull();
        customer.Email.Should().Be(TestEmail);
    }

    [Fact]
    public void Create_WithNullPhone_ShouldAllowNullPhone()
    {
        // Act
        var customer = CreateTestCustomer(phone: null);

        // Assert
        customer.Phone.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNullTenant()
    {
        // Act
        var customer = CreateTestCustomer(tenantId: null);

        // Assert
        customer.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var customer1 = CreateTestCustomer();
        var customer2 = CreateTestCustomer();

        // Assert
        customer1.Id.Should().NotBe(customer2.Id);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public void UpdateProfile_WithNewValues_ShouldUpdateAllFields()
    {
        // Arrange
        var customer = CreateTestCustomer();
        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newEmail = "jane@example.com";
        var newPhone = "+84987654321";

        // Act
        customer.UpdateProfile(newFirstName, newLastName, newEmail, newPhone);

        // Assert
        customer.FirstName.Should().Be(newFirstName);
        customer.LastName.Should().Be(newLastName);
        customer.Email.Should().Be(newEmail);
        customer.Phone.Should().Be(newPhone);
    }

    [Fact]
    public void UpdateProfile_WithNullPhone_ShouldClearPhone()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.UpdateProfile("Jane", "Smith", "jane@example.com", null);

        // Assert
        customer.Phone.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(100);
        var originalPoints = customer.LoyaltyPoints;
        var originalSegment = customer.Segment;

        // Act
        customer.UpdateProfile("Jane", "Smith", "jane@example.com", null);

        // Assert
        customer.LoyaltyPoints.Should().Be(originalPoints);
        customer.Segment.Should().Be(originalSegment);
        customer.IsActive.Should().BeTrue();
    }

    #endregion

    #region UpdateRfmMetrics Tests

    [Fact]
    public void UpdateRfmMetrics_ShouldSetAllRfmFields()
    {
        // Arrange
        var customer = CreateTestCustomer();
        var lastOrderDate = DateTimeOffset.UtcNow.AddDays(-5);

        // Act
        customer.UpdateRfmMetrics(lastOrderDate, 10, 5_000_000m);

        // Assert
        customer.LastOrderDate.Should().Be(lastOrderDate);
        customer.TotalOrders.Should().Be(10);
        customer.TotalSpent.Should().Be(5_000_000m);
    }

    [Fact]
    public void UpdateRfmMetrics_ShouldCalculateAverageOrderValue()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 10, 5_000_000m);

        // Assert
        customer.AverageOrderValue.Should().Be(500_000m);
    }

    [Fact]
    public void UpdateRfmMetrics_WithZeroOrders_ShouldSetAverageOrderValueToZero()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 0, 0m);

        // Assert
        customer.AverageOrderValue.Should().Be(0m);
    }

    [Fact]
    public void UpdateRfmMetrics_WithSingleOrder_ShouldEqualTotalSpent()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 1, 250_000m);

        // Assert
        customer.AverageOrderValue.Should().Be(250_000m);
    }

    #endregion

    #region RecalculateSegment Tests

    [Fact]
    public void RecalculateSegment_VipCustomer_ShouldSetVipSegment()
    {
        // Arrange - VIP: 20+ orders AND 10,000,000+ VND
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 20, 10_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.VIP);
    }

    [Fact]
    public void RecalculateSegment_HighOrdersButLowSpend_ShouldNotBeVip()
    {
        // Arrange - 20+ orders but < 10,000,000 VND
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 25, 5_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().NotBe(CustomerSegment.VIP);
    }

    [Fact]
    public void RecalculateSegment_HighSpendButLowOrders_ShouldNotBeVip()
    {
        // Arrange - < 20 orders but 10,000,000+ VND
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 15, 15_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().NotBe(CustomerSegment.VIP);
    }

    [Fact]
    public void RecalculateSegment_ZeroOrders_ShouldBeNew()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 0, 0m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.New);
    }

    [Fact]
    public void RecalculateSegment_OneOrder_ShouldBeNew()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow, 1, 100_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.New);
    }

    // Note: The code path (TotalOrders > 1 AND LastOrderDate == null) is unreachable
    // from public API because UpdateRfmMetrics always sets both values together.
    // The null-LastOrderDate branch in RecalculateSegment is defensive dead code.
    // Zero-order customers are already covered by RecalculateSegment_ZeroOrders_ShouldBeNew.

    [Fact]
    public void RecalculateSegment_OrderedWithinLast30Days_ShouldBeActive()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-15), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Active);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustUnder30DaysAgo_ShouldBeActive()
    {
        // Arrange - within 30 days boundary (using 29 to avoid sub-millisecond timing drift)
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-29), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Active);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustOver30DaysAgo_ShouldBeAtRisk()
    {
        // Arrange - just past the 30-day boundary
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-31), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.AtRisk);
    }

    [Fact]
    public void RecalculateSegment_OrderedBetween31And90DaysAgo_ShouldBeAtRisk()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-60), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.AtRisk);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustUnder90DaysAgo_ShouldBeAtRisk()
    {
        // Arrange - within 90 days boundary (using 89 to avoid sub-millisecond timing drift)
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-89), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.AtRisk);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustOver90DaysAgo_ShouldBeDormant()
    {
        // Arrange - just past the 90-day boundary
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-91), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Dormant);
    }

    [Fact]
    public void RecalculateSegment_OrderedBetween91And180DaysAgo_ShouldBeDormant()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-120), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Dormant);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustUnder180DaysAgo_ShouldBeDormant()
    {
        // Arrange - within 180 days boundary (using 179 to avoid sub-millisecond timing drift)
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-179), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Dormant);
    }

    [Fact]
    public void RecalculateSegment_OrderedJustOver180DaysAgo_ShouldBeLost()
    {
        // Arrange - just past the 180-day boundary
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-181), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Lost);
    }

    [Fact]
    public void RecalculateSegment_OrderedMoreThan180DaysAgo_ShouldBeLost()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-200), 5, 1_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(CustomerSegment.Lost);
    }

    [Theory]
    [InlineData(20, 10_000_000, CustomerSegment.VIP)]
    [InlineData(25, 15_000_000, CustomerSegment.VIP)]
    [InlineData(0, 0, CustomerSegment.New)]
    [InlineData(1, 100_000, CustomerSegment.New)]
    public void RecalculateSegment_WithRecentOrder_ShouldClassifyCorrectly(
        int totalOrders, double totalSpent, CustomerSegment expectedSegment)
    {
        // Arrange - use a recent order date for non-VIP cases
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-5), totalOrders, (decimal)totalSpent);

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.Segment.Should().Be(expectedSegment);
    }

    [Fact]
    public void RecalculateSegment_VipTakesPriorityOverRecency()
    {
        // Arrange - VIP criteria met even if ordered long ago
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-200), 20, 10_000_000m);

        // Act
        customer.RecalculateSegment();

        // Assert - VIP check happens before recency check
        customer.Segment.Should().Be(CustomerSegment.VIP);
    }

    #endregion

    #region SetSegment Tests

    [Theory]
    [InlineData(CustomerSegment.New)]
    [InlineData(CustomerSegment.Active)]
    [InlineData(CustomerSegment.AtRisk)]
    [InlineData(CustomerSegment.Dormant)]
    [InlineData(CustomerSegment.Lost)]
    [InlineData(CustomerSegment.VIP)]
    public void SetSegment_ShouldOverrideCurrentSegment(CustomerSegment segment)
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.SetSegment(segment);

        // Assert
        customer.Segment.Should().Be(segment);
    }

    #endregion

    #region Loyalty Points - AddLoyaltyPoints Tests

    [Fact]
    public void AddLoyaltyPoints_WithPositivePoints_ShouldIncreaseBothCounters()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddLoyaltyPoints(500);

        // Assert
        customer.LoyaltyPoints.Should().Be(500);
        customer.LifetimeLoyaltyPoints.Should().Be(500);
    }

    [Fact]
    public void AddLoyaltyPoints_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddLoyaltyPoints(100);
        customer.AddLoyaltyPoints(200);
        customer.AddLoyaltyPoints(300);

        // Assert
        customer.LoyaltyPoints.Should().Be(600);
        customer.LifetimeLoyaltyPoints.Should().Be(600);
    }

    [Fact]
    public void AddLoyaltyPoints_WithZeroPoints_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        var act = () => customer.AddLoyaltyPoints(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Points must be positive.");
    }

    [Fact]
    public void AddLoyaltyPoints_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        var act = () => customer.AddLoyaltyPoints(-10);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Points must be positive.");
    }

    [Fact]
    public void AddLoyaltyPoints_ShouldAutoUpdateTier()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act - Add enough points for Silver (5000+)
        customer.AddLoyaltyPoints(5000);

        // Assert
        customer.Tier.Should().Be(CustomerTier.Silver);
    }

    #endregion

    #region Loyalty Points - RedeemLoyaltyPoints Tests

    [Fact]
    public void RedeemLoyaltyPoints_WithSufficientBalance_ShouldDecreaseLoyaltyPoints()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(500);

        // Act
        customer.RedeemLoyaltyPoints(200);

        // Assert
        customer.LoyaltyPoints.Should().Be(300);
    }

    [Fact]
    public void RedeemLoyaltyPoints_ShouldNotDecreaseLifetimePoints()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(500);

        // Act
        customer.RedeemLoyaltyPoints(200);

        // Assert
        customer.LifetimeLoyaltyPoints.Should().Be(500);
    }

    [Fact]
    public void RedeemLoyaltyPoints_ExactBalance_ShouldReduceToZero()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(300);

        // Act
        customer.RedeemLoyaltyPoints(300);

        // Assert
        customer.LoyaltyPoints.Should().Be(0);
    }

    [Fact]
    public void RedeemLoyaltyPoints_MoreThanAvailable_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(100);

        // Act
        var act = () => customer.RedeemLoyaltyPoints(200);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient loyalty points. Available: 100, Requested: 200");
    }

    [Fact]
    public void RedeemLoyaltyPoints_WithZeroPoints_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(100);

        // Act
        var act = () => customer.RedeemLoyaltyPoints(0);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Points must be positive.");
    }

    [Fact]
    public void RedeemLoyaltyPoints_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(100);

        // Act
        var act = () => customer.RedeemLoyaltyPoints(-5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Points must be positive.");
    }

    [Fact]
    public void RedeemLoyaltyPoints_FromZeroBalance_ShouldThrow()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        var act = () => customer.RedeemLoyaltyPoints(1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient loyalty points. Available: 0, Requested: 1");
    }

    [Fact]
    public void RedeemLoyaltyPoints_MultipleTimes_ShouldTrackBalanceCorrectly()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(1000);

        // Act
        customer.RedeemLoyaltyPoints(300);
        customer.RedeemLoyaltyPoints(200);
        customer.RedeemLoyaltyPoints(100);

        // Assert
        customer.LoyaltyPoints.Should().Be(400);
        customer.LifetimeLoyaltyPoints.Should().Be(1000);
    }

    #endregion

    #region UpdateTier Tests

    [Fact]
    public void UpdateTier_Below5000Points_ShouldBeStandard()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(4999);

        // Assert - AddLoyaltyPoints calls UpdateTier internally
        customer.Tier.Should().Be(CustomerTier.Standard);
    }

    [Fact]
    public void UpdateTier_At5000Points_ShouldBeSilver()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(5000);

        // Assert
        customer.Tier.Should().Be(CustomerTier.Silver);
    }

    [Fact]
    public void UpdateTier_At10000Points_ShouldBeGold()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(10000);

        // Assert
        customer.Tier.Should().Be(CustomerTier.Gold);
    }

    [Fact]
    public void UpdateTier_At20000Points_ShouldBePlatinum()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(20000);

        // Assert
        customer.Tier.Should().Be(CustomerTier.Platinum);
    }

    [Fact]
    public void UpdateTier_At50000Points_ShouldBeDiamond()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(50000);

        // Assert
        customer.Tier.Should().Be(CustomerTier.Diamond);
    }

    [Theory]
    [InlineData(0, CustomerTier.Standard)]
    [InlineData(4999, CustomerTier.Standard)]
    [InlineData(5000, CustomerTier.Silver)]
    [InlineData(9999, CustomerTier.Silver)]
    [InlineData(10000, CustomerTier.Gold)]
    [InlineData(19999, CustomerTier.Gold)]
    [InlineData(20000, CustomerTier.Platinum)]
    [InlineData(49999, CustomerTier.Platinum)]
    [InlineData(50000, CustomerTier.Diamond)]
    [InlineData(100000, CustomerTier.Diamond)]
    public void UpdateTier_AtVariousPointLevels_ShouldSetCorrectTier(int points, CustomerTier expectedTier)
    {
        // Arrange
        var customer = CreateTestCustomer();
        if (points > 0)
        {
            customer.AddLoyaltyPoints(points);
        }

        // Assert
        customer.Tier.Should().Be(expectedTier);
    }

    [Fact]
    public void UpdateTier_AfterRedemption_ShouldRetainTierBasedOnLifetimePoints()
    {
        // Arrange - Tier is based on LifetimeLoyaltyPoints, not current balance
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(10000); // Gold tier

        // Act
        customer.RedeemLoyaltyPoints(9000); // Balance drops to 1000

        // Assert - Still Gold because LifetimeLoyaltyPoints is 10000
        // Note: RedeemLoyaltyPoints does NOT call UpdateTier, so tier stays
        customer.Tier.Should().Be(CustomerTier.Gold);
        customer.LoyaltyPoints.Should().Be(1000);
        customer.LifetimeLoyaltyPoints.Should().Be(10000);
    }

    #endregion

    #region Tag Tests

    [Fact]
    public void AddTag_ToCustomerWithNoTags_ShouldSetTag()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddTag("premium");

        // Assert
        customer.Tags.Should().Be("premium");
    }

    [Fact]
    public void AddTag_MultipleTags_ShouldJoinWithComma()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddTag("premium");
        customer.AddTag("newsletter");

        // Assert
        customer.Tags.Should().Be("premium,newsletter");
    }

    [Fact]
    public void AddTag_DuplicateTag_ShouldNotAddAgain()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("premium");

        // Act
        customer.AddTag("premium");

        // Assert
        customer.Tags.Should().Be("premium");
    }

    [Fact]
    public void AddTag_DuplicateTagCaseInsensitive_ShouldNotAddAgain()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("Premium");

        // Act
        customer.AddTag("PREMIUM");

        // Assert
        customer.Tags.Should().Be("Premium");
    }

    [Fact]
    public void AddTag_WithWhitespace_ShouldTrimTag()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddTag("  premium  ");

        // Assert
        customer.Tags.Should().Be("premium");
    }

    [Fact]
    public void AddTag_WithEmptyString_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddTag("");

        // Assert
        customer.Tags.Should().BeNull();
    }

    [Fact]
    public void AddTag_WithWhitespaceOnlyString_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddTag("   ");

        // Assert
        customer.Tags.Should().BeNull();
    }

    [Fact]
    public void RemoveTag_ExistingTag_ShouldRemoveIt()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("premium");
        customer.AddTag("newsletter");

        // Act
        customer.RemoveTag("premium");

        // Assert
        customer.Tags.Should().Be("newsletter");
    }

    [Fact]
    public void RemoveTag_CaseInsensitive_ShouldRemoveIt()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("Premium");

        // Act
        customer.RemoveTag("PREMIUM");

        // Assert
        customer.Tags.Should().BeNull();
    }

    [Fact]
    public void RemoveTag_LastTag_ShouldSetTagsToNull()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("premium");

        // Act
        customer.RemoveTag("premium");

        // Assert
        customer.Tags.Should().BeNull();
    }

    [Fact]
    public void RemoveTag_NonExistentTag_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("premium");

        // Act
        customer.RemoveTag("vip");

        // Assert
        customer.Tags.Should().Be("premium");
    }

    [Fact]
    public void RemoveTag_WhenTagsAreNull_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.RemoveTag("premium");

        // Assert
        customer.Tags.Should().BeNull();
    }

    [Fact]
    public void RemoveTag_WithEmptyString_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddTag("premium");

        // Act
        customer.RemoveTag("");

        // Assert
        customer.Tags.Should().Be("premium");
    }

    #endregion

    #region Notes Tests

    [Fact]
    public void AddNote_ToCustomerWithNoNotes_ShouldSetNote()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddNote("First contact via phone");

        // Assert
        customer.Notes.Should().Be("First contact via phone");
    }

    [Fact]
    public void AddNote_ToCustomerWithExistingNotes_ShouldAppendWithSeparator()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddNote("First note");

        // Act
        customer.AddNote("Second note");

        // Assert
        customer.Notes.Should().Be("First note\n---\nSecond note");
    }

    [Fact]
    public void AddNote_MultipleNotes_ShouldAppendAllWithSeparators()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddNote("Note 1");
        customer.AddNote("Note 2");
        customer.AddNote("Note 3");

        // Assert
        customer.Notes.Should().Be("Note 1\n---\nNote 2\n---\nNote 3");
    }

    [Fact]
    public void AddNote_WithEmptyString_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddNote("");

        // Assert
        customer.Notes.Should().BeNull();
    }

    [Fact]
    public void AddNote_WithWhitespaceOnlyString_ShouldDoNothing()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.AddNote("   ");

        // Assert
        customer.Notes.Should().BeNull();
    }

    [Fact]
    public void AddNote_WithEmptyStringAfterExistingNote_ShouldNotAppend()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddNote("Existing note");

        // Act
        customer.AddNote("");

        // Assert
        customer.Notes.Should().Be("Existing note");
    }

    #endregion

    #region Activate / Deactivate Tests

    [Fact]
    public void Deactivate_ActiveCustomer_ShouldSetIsActiveFalse()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.Deactivate();

        // Assert
        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_InactiveCustomer_ShouldSetIsActiveTrue()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.Deactivate();

        // Act
        customer.Activate();

        // Assert
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.Deactivate();

        // Act
        customer.Deactivate();

        // Assert
        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act
        customer.Activate();

        // Assert
        customer.IsActive.Should().BeTrue();
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void FullLifecycle_CreateAddPointsRedeemRecalculate_ShouldMaintainConsistency()
    {
        // Arrange
        var customer = CreateTestCustomer();

        // Act - Simulate customer lifecycle
        customer.AddLoyaltyPoints(10000); // Gold tier
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 15, 8_000_000m);
        customer.RecalculateSegment(); // Active segment (ordered 10 days ago)
        customer.RedeemLoyaltyPoints(3000);
        customer.AddTag("loyal");
        customer.AddNote("Great customer, frequent buyer");

        // Assert
        customer.Tier.Should().Be(CustomerTier.Gold);
        customer.Segment.Should().Be(CustomerSegment.Active);
        customer.LoyaltyPoints.Should().Be(7000);
        customer.LifetimeLoyaltyPoints.Should().Be(10000);
        customer.Tags.Should().Be("loyal");
        customer.Notes.Should().Be("Great customer, frequent buyer");
        customer.TotalOrders.Should().Be(15);
        customer.TotalSpent.Should().Be(8_000_000m);
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LoyaltyPointsAndTier_ProgressionFromStandardToDiamond()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.Tier.Should().Be(CustomerTier.Standard);

        // Act & Assert - Progress through tiers
        customer.AddLoyaltyPoints(5000);
        customer.Tier.Should().Be(CustomerTier.Silver);

        customer.AddLoyaltyPoints(5000); // Total: 10000
        customer.Tier.Should().Be(CustomerTier.Gold);

        customer.AddLoyaltyPoints(10000); // Total: 20000
        customer.Tier.Should().Be(CustomerTier.Platinum);

        customer.AddLoyaltyPoints(30000); // Total: 50000
        customer.Tier.Should().Be(CustomerTier.Diamond);
    }

    #endregion
}
