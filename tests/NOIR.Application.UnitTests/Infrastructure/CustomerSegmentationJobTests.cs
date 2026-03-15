using NOIR.Application.Modules;
using NOIR.Infrastructure.Customers;

namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CustomerSegmentationJob.
/// Tests focus on domain segment logic and job structure.
/// Integration-level tenant iteration is tested via integration tests.
/// </summary>
public class CustomerSegmentationJobTests
{
    #region Job Type Tests

    [Fact]
    public void Job_ShouldImplementIScopedService()
    {
        typeof(CustomerSegmentationJob).GetInterfaces().ShouldContain(typeof(IScopedService));
    }

    [Fact]
    public void Job_ShouldBePublicClass()
    {
        typeof(CustomerSegmentationJob).IsPublic.ShouldBe(true);
    }

    [Fact]
    public void ExecuteAsync_MethodSignature_ShouldExist()
    {
        var method = typeof(CustomerSegmentationJob)
            .GetMethod("ExecuteAsync", [typeof(CancellationToken)]);

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task));
    }

    [Fact]
    public void Job_ShouldUseCorrectModuleFeatureName()
    {
        // Verify the constant is well-defined (not null/empty)
        ModuleNames.Ecommerce.Customers.ShouldNotBeNullOrEmpty();
        ModuleNames.Ecommerce.Customers.ShouldBe("Ecommerce.Customers");
    }

    #endregion

    #region Segment Logic Tests (Pure Domain)

    private static Customer CreateCustomer(DateTimeOffset lastOrderDate, int totalOrders, decimal totalSpent)
    {
        var customer = Customer.Create(null, "test@example.com", "Test", "User", null, "tenant-1");
        customer.UpdateRfmMetrics(lastOrderDate, totalOrders, totalSpent);
        return customer;
    }

    [Fact]
    public void RecalculateSegment_WithNoOrders_ShouldBeNewSegment()
    {
        var customer = Customer.Create(null, "new@example.com", "New", "User", null, "tenant-1");

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.New);
    }

    [Fact]
    public void RecalculateSegment_WithOneOrder_ShouldBeNewSegment()
    {
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-5), totalOrders: 1, totalSpent: 100_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.New);
    }

    [Fact]
    public void RecalculateSegment_WithVipThresholds_ShouldBeVipSegment()
    {
        // VIP: >=20 orders AND >=10M VND
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-2), totalOrders: 20, totalSpent: 10_000_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.VIP);
    }

    [Fact]
    public void RecalculateSegment_WithRecentOrderAndMultipleOrders_ShouldBeActiveSegment()
    {
        // Active: last order <= 30 days, > 1 order, not VIP
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-15), totalOrders: 5, totalSpent: 500_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.Active);
    }

    [Fact]
    public void RecalculateSegment_WithOrderBetween30And90DaysAgo_ShouldBeAtRisk()
    {
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-60), totalOrders: 5, totalSpent: 500_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.AtRisk);
    }

    [Fact]
    public void RecalculateSegment_WithOrderBetween90And180DaysAgo_ShouldBeDormant()
    {
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-120), totalOrders: 5, totalSpent: 500_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.Dormant);
    }

    [Fact]
    public void RecalculateSegment_WithOrderMoreThan180DaysAgo_ShouldBeLost()
    {
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-200), totalOrders: 5, totalSpent: 500_000m);

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(CustomerSegment.Lost);
    }

    [Fact]
    public void RecalculateSegment_IsIdempotent_CalledTwice_ProducesSameResult()
    {
        var customer = CreateCustomer(DateTimeOffset.UtcNow.AddDays(-15), totalOrders: 5, totalSpent: 500_000m);

        customer.RecalculateSegment();
        var firstSegment = customer.Segment;

        customer.RecalculateSegment();

        customer.Segment.ShouldBe(firstSegment);
    }

    #endregion
}
