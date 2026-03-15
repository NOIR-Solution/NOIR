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
        installment.ShouldNotBeNull();
        installment.Id.ShouldNotBe(Guid.Empty);
        installment.PaymentTransactionId.ShouldBe(transactionId);
        installment.InstallmentNumber.ShouldBe(1);
        installment.TotalInstallments.ShouldBe(3);
        installment.Amount.ShouldBe(500_000m);
        installment.Currency.ShouldBe("VND");
        installment.DueDate.ShouldBe(dueDate);
        installment.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToScheduled()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.Status.ShouldBe(InstallmentStatus.Scheduled);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.PaidAt.ShouldBeNull();
        installment.GatewayReference.ShouldBeNull();
        installment.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeRetryCountToZero()
    {
        // Act
        var installment = CreateTestInstallment();

        // Assert
        installment.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var installment = CreateTestInstallment(tenantId: null);

        // Assert
        installment.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithDifferentCurrency_ShouldSetCurrency()
    {
        // Act
        var installment = CreateTestInstallment(currency: "USD");

        // Assert
        installment.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_MultipleInstallments_ShouldHaveUniqueIds()
    {
        // Act
        var installment1 = CreateTestInstallment(installmentNumber: 1);
        var installment2 = CreateTestInstallment(installmentNumber: 2);

        // Assert
        installment1.Id.ShouldNotBe(installment2.Id);
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
        installment.InstallmentNumber.ShouldBe(number);
        installment.TotalInstallments.ShouldBe(total);
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
        installment.Status.ShouldBe(InstallmentStatus.Pending);
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
        installment.Amount.ShouldBe(originalAmount);
        installment.DueDate.ShouldBe(originalDueDate);
        installment.PaidAt.ShouldBeNull();
        installment.GatewayReference.ShouldBeNull();
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
        installment.Status.ShouldBe(InstallmentStatus.Paid);
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
        installment.PaidAt.ShouldNotBeNull();
        installment.PaidAt!.Value.ShouldBeGreaterThanOrEqualTo(beforePaid);
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
        installment.GatewayReference.ShouldBe("GW-REF-ABC");
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
        installment.Status.ShouldBe(InstallmentStatus.Failed);
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
        installment.FailureReason.ShouldBe("Card declined");
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementRetryCount()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsPending();
        installment.RetryCount.ShouldBe(0);

        // Act
        installment.MarkAsFailed("First failure");

        // Assert
        installment.RetryCount.ShouldBe(1);
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
        installment.RetryCount.ShouldBe(3);
        installment.FailureReason.ShouldBe("Failure 3");
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
        installment.FailureReason.ShouldBe("Second reason");
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
        installment.Status.ShouldBe(InstallmentStatus.Cancelled);
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
        installment.Status.ShouldBe(InstallmentStatus.Cancelled);
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
        installment.Status.ShouldBe(InstallmentStatus.Pending);
    }

    [Fact]
    public void ResetForRetry_ShouldClearFailureReason()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("Some error");
        installment.FailureReason.ShouldNotBeNull();

        // Act
        installment.ResetForRetry();

        // Assert
        installment.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void ResetForRetry_ShouldPreserveRetryCount()
    {
        // Arrange
        var installment = CreateTestInstallment();
        installment.MarkAsFailed("Error 1");
        installment.MarkAsFailed("Error 2");
        installment.RetryCount.ShouldBe(2);

        // Act
        installment.ResetForRetry();

        // Assert
        installment.RetryCount.ShouldBe(2);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_ScheduledToPaid_ShouldTransitionCorrectly()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act & Assert
        installment.Status.ShouldBe(InstallmentStatus.Scheduled);

        installment.MarkAsPending();
        installment.Status.ShouldBe(InstallmentStatus.Pending);

        installment.MarkAsPaid("GW-SUCCESS-001");
        installment.Status.ShouldBe(InstallmentStatus.Paid);
        installment.PaidAt.ShouldNotBeNull();
        installment.GatewayReference.ShouldBe("GW-SUCCESS-001");
    }

    [Fact]
    public void FullLifecycle_FailAndRetry_ShouldTrackAttempts()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act - first attempt fails
        installment.MarkAsPending();
        installment.MarkAsFailed("Network timeout");
        installment.RetryCount.ShouldBe(1);

        // Retry
        installment.ResetForRetry();
        installment.Status.ShouldBe(InstallmentStatus.Pending);
        installment.FailureReason.ShouldBeNull();

        // Second attempt fails
        installment.MarkAsFailed("Insufficient funds");
        installment.RetryCount.ShouldBe(2);

        // Retry again
        installment.ResetForRetry();

        // Third attempt succeeds
        installment.MarkAsPaid("GW-SUCCESS-003");
        installment.Status.ShouldBe(InstallmentStatus.Paid);
        installment.RetryCount.ShouldBe(2);
    }

    [Fact]
    public void FullLifecycle_ScheduledToCancelled_ShouldTransitionCorrectly()
    {
        // Arrange
        var installment = CreateTestInstallment();

        // Act & Assert
        installment.Status.ShouldBe(InstallmentStatus.Scheduled);

        installment.Cancel();
        installment.Status.ShouldBe(InstallmentStatus.Cancelled);
    }

    #endregion
}
