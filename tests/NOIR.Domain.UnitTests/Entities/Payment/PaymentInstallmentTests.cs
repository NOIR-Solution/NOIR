using NOIR.Domain.Entities.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the PaymentInstallment entity.
/// Tests factory methods, state transitions (scheduled, pending, paid, failed, cancelled),
/// retry logic, and property validation.
/// </summary>
public class PaymentInstallmentTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestCurrency = "VND";
    private const decimal TestAmount = 250_000m;
    private static readonly Guid TestTransactionId = Guid.NewGuid();

    /// <summary>
    /// Helper to create a default valid PaymentInstallment for tests.
    /// </summary>
    private static PaymentInstallment CreateTestInstallment(
        Guid? paymentTransactionId = null,
        int installmentNumber = 1,
        int totalInstallments = 3,
        decimal amount = TestAmount,
        string currency = TestCurrency,
        DateTimeOffset? dueDate = null,
        string? tenantId = TestTenantId)
    {
        return PaymentInstallment.Create(
            paymentTransactionId ?? TestTransactionId,
            installmentNumber,
            totalInstallments,
            amount,
            currency,
            dueDate ?? DateTimeOffset.UtcNow.AddDays(30),
            tenantId);
    }

    #region Create Factory

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidInstallment()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var dueDate = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var installment = PaymentInstallment.Create(
            transactionId, 1, 3, 500_000m, "VND", dueDate, TestTenantId);

        // Assert
        installment.Should().NotBeNull();
        installment.Id.Should().NotBe(Guid.Empty);
        installment.PaymentTransactionId.Should().Be(transactionId);
        installment.InstallmentNumber.Should().Be(1);
        installment.TotalInstallments.Should().Be(3);
        installment.Amount.Should().Be(500_000m);
        installment.Currency.Should().Be("VND");
        installment.DueDate.Should().Be(dueDate);
        installment.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToScheduled()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Scheduled);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.PaidAt.Should().BeNull();
        installment.GatewayReference.Should().BeNull();
        installment.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeRetryCountToZero()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var installment = CreateTestInstallment(tenantId: null);

        // Assert
        installment.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_WithDifferentCurrency_ShouldSetCurrency()
    {
        // Act
        var installment = CreateTestInstallment(currency: "USD");

        // Assert
        installment.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_MultipleInstallments_ShouldHaveUniqueIds()
    {
        // Act
        var installment1 = CreateTestInstallment(installmentNumber: 1);
        var installment2 = CreateTestInstallment(installmentNumber: 2);

        // Assert
        installment1.Id.Should().NotBe(installment2.Id);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 6)]
    [InlineData(12, 12)]
    public void Create_WithVariousInstallmentNumbers_ShouldSetCorrectly(int number, int total)
    {
        // Act
        var installment = CreateTestInstallment(installmentNumber: number, totalInstallments: total);

        // Assert
        installment.InstallmentNumber.Should().Be(number);
        installment.TotalInstallments.Should().Be(total);
    }

    #endregion

    #region MarkAsPending

    [Fact]
    public void MarkAsPending_FromScheduled_ShouldTransitionToPending()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act
        installment.MarkAsPending();

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Pending);
    }

    [Fact]
    public void MarkAsPending_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var installment = CreateTestInstallment();
        var originalAmount = installment.Amount;
        var originalDueDate = installment.DueDate;

        // Act
        installment.MarkAsPending();

        // Assert
        installment.Amount.Should().Be(originalAmount);
        installment.DueDate.Should().Be(originalDueDate);
        installment.PaidAt.Should().BeNull();
        installment.GatewayReference.Should().BeNull();
    }

    #endregion

    #region MarkAsPaid

    [Fact]
    public void MarkAsPaid_ShouldTransitionToPaid()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();

        // Act
        installment.MarkAsPaid("GW-REF-12345");

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Paid);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetPaidAtTimestamp()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();
        var beforePaid = DateTimeOffset.UtcNow;

        // Act
        installment.MarkAsPaid("GW-REF-12345");

        // Assert
        installment.PaidAt.Should().NotBeNull();
        installment.PaidAt.Should().BeOnOrAfter(beforePaid);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetGatewayReference()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();

        // Act
        installment.MarkAsPaid("GW-REF-ABC");

        // Assert
        installment.GatewayReference.Should().Be("GW-REF-ABC");
    }

    #endregion

    #region MarkAsFailed

    [Fact]
    public void MarkAsFailed_ShouldTransitionToFailed()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();

        // Act
        installment.MarkAsFailed("Insufficient funds");

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailureReason()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();

        // Act
        installment.MarkAsFailed("Card declined");

        // Assert
        installment.FailureReason.Should().Be("Card declined");
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementRetryCount()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();
        installment.RetryCount.Should().Be(0);

        // Act
        installment.MarkAsFailed("First failure");

        // Assert
        installment.RetryCount.Should().Be(1);
    }

    [Fact]
    public void MarkAsFailed_CalledMultipleTimes_ShouldIncrementRetryCountEachTime()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act
        installment.MarkAsFailed("Failure 1");
        installment.MarkAsFailed("Failure 2");
        installment.MarkAsFailed("Failure 3");

        // Assert
        installment.RetryCount.Should().Be(3);
        installment.FailureReason.Should().Be("Failure 3");
    }

    [Fact]
    public void MarkAsFailed_ShouldOverwritePreviousFailureReason()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("First reason");

        // Act
        installment.MarkAsFailed("Second reason");

        // Assert
        installment.FailureReason.Should().Be("Second reason");
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_ShouldTransitionToCancelled()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act
        installment.Cancel();

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromPending_ShouldTransitionToCancelled()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();

        // Act
        installment.Cancel();

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Cancelled);
    }

    #endregion

    #region ResetForRetry

    [Fact]
    public void ResetForRetry_FromFailed_ShouldTransitionToPending()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("Temporary error");

        // Act
        installment.ResetForRetry();

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Pending);
    }

    [Fact]
    public void ResetForRetry_ShouldClearFailureReason()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("Some error");
        installment.FailureReason.Should().NotBeNull();

        // Act
        installment.ResetForRetry();

        // Assert
        installment.FailureReason.Should().BeNull();
    }

    [Fact]
    public void ResetForRetry_ShouldPreserveRetryCount()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("Error 1");
        installment.MarkAsFailed("Error 2");
        installment.RetryCount.Should().Be(2);

        // Act
        installment.ResetForRetry();

        // Assert
        installment.RetryCount.Should().Be(2);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_ScheduledToPaid_ShouldTransitionCorrectly()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act & Assert
        installment.Status.Should().Be(InstallmentStatus.Scheduled);

        installment.MarkAsPending();
        installment.Status.Should().Be(InstallmentStatus.Pending);

        installment.MarkAsPaid("GW-SUCCESS-001");
        installment.Status.Should().Be(InstallmentStatus.Paid);
        installment.PaidAt.Should().NotBeNull();
        installment.GatewayReference.Should().Be("GW-SUCCESS-001");
    }

    [Fact]
    public void FullLifecycle_FailAndRetry_ShouldTrackAttempts()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act - first attempt fails
        installment.MarkAsPending();
        installment.MarkAsFailed("Network timeout");
        installment.RetryCount.Should().Be(1);

        // Retry
        installment.ResetForRetry();
        installment.Status.Should().Be(InstallmentStatus.Pending);
        installment.FailureReason.Should().BeNull();

        // Second attempt fails
        installment.MarkAsFailed("Insufficient funds");
        installment.RetryCount.Should().Be(2);

        // Retry again
        installment.ResetForRetry();

        // Third attempt succeeds
        installment.MarkAsPaid("GW-SUCCESS-003");
        installment.Status.Should().Be(InstallmentStatus.Paid);
        installment.RetryCount.Should().Be(2);
    }

    [Fact]
    public void FullLifecycle_ScheduledToCancelled_ShouldTransitionCorrectly()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act & Assert
        installment.Status.Should().Be(InstallmentStatus.Scheduled);

        installment.Cancel();
        installment.Status.Should().Be(InstallmentStatus.Cancelled);
    }

    #endregion
}
