using NOIR.Domain.Entities.Payment;
using NOIR.Domain.Events.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the PaymentGateway aggregate root entity.
/// Tests factory methods, configuration, activation/deactivation,
/// health status updates, and property mutation methods.
/// </summary>
public class PaymentGatewayTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestProvider = "vnpay";
    private const string TestDisplayName = "VNPay Gateway";

    /// <summary>
    /// Helper to create a default valid PaymentGateway for tests.
    /// </summary>
    private static PaymentGateway CreateTestGateway(
        string provider = TestProvider,
        string displayName = TestDisplayName,
        GatewayEnvironment environment = GatewayEnvironment.Sandbox,
        string? tenantId = TestTenantId)
    {
        return PaymentGateway.Create(provider, displayName, environment, tenantId);
    }

    #region Create Factory

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidGateway()
    {
        // Act
        var gateway = PaymentGateway.Create(TestProvider, TestDisplayName, GatewayEnvironment.Sandbox, TestTenantId);

        // Assert
        gateway.Should().NotBeNull();
        gateway.Id.Should().NotBe(Guid.Empty);
        gateway.Provider.Should().Be(TestProvider);
        gateway.DisplayName.Should().Be(TestDisplayName);
        gateway.Environment.Should().Be(GatewayEnvironment.Sandbox);
        gateway.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetIsActiveToFalse()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetHealthStatusToUnknown()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Unknown);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.EncryptedCredentials.Should().BeNull();
        gateway.WebhookSecret.Should().BeNull();
        gateway.WebhookUrl.Should().BeNull();
        gateway.MinAmount.Should().BeNull();
        gateway.MaxAmount.Should().BeNull();
        gateway.LastHealthCheck.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeSupportedCurrenciesToEmptyJsonArray()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.SupportedCurrencies.Should().Be("[]");
    }

    [Fact]
    public void Create_ShouldDefaultSortOrderToZero()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldRaisePaymentGatewayCreatedEvent()
    {
        // Act
        var gateway = CreateTestGateway();

        // Assert
        gateway.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentGatewayCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                GatewayId = gateway.Id,
                Provider = TestProvider
            });
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var gateway = PaymentGateway.Create(TestProvider, TestDisplayName, GatewayEnvironment.Sandbox, tenantId: null);

        // Assert
        gateway.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_WithProductionEnvironment_ShouldSetEnvironment()
    {
        // Act
        var gateway = CreateTestGateway(environment: GatewayEnvironment.Production);

        // Assert
        gateway.Environment.Should().Be(GatewayEnvironment.Production);
    }

    [Fact]
    public void Create_MultipleGateways_ShouldHaveUniqueIds()
    {
        // Act
        var gateway1 = CreateTestGateway(provider: "vnpay");
        var gateway2 = CreateTestGateway(provider: "momo");

        // Assert
        gateway1.Id.Should().NotBe(gateway2.Id);
    }

    #endregion

    #region Configure

    [Fact]
    public void Configure_ShouldSetEncryptedCredentialsAndWebhookSecret()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.Configure("encrypted-json-data", "webhook-secret-123");

        // Assert
        gateway.EncryptedCredentials.Should().Be("encrypted-json-data");
        gateway.WebhookSecret.Should().Be("webhook-secret-123");
    }

    [Fact]
    public void Configure_WithNullWebhookSecret_ShouldAllowNull()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.Configure("encrypted-json-data", null);

        // Assert
        gateway.EncryptedCredentials.Should().Be("encrypted-json-data");
        gateway.WebhookSecret.Should().BeNull();
    }

    [Fact]
    public void Configure_CalledTwice_ShouldOverwritePreviousValues()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.Configure("first-creds", "first-secret");

        // Act
        gateway.Configure("second-creds", "second-secret");

        // Assert
        gateway.EncryptedCredentials.Should().Be("second-creds");
        gateway.WebhookSecret.Should().Be("second-secret");
    }

    #endregion

    #region SetWebhookUrl

    [Fact]
    public void SetWebhookUrl_ShouldSetUrl()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetWebhookUrl("https://api.example.com/webhooks/vnpay");

        // Assert
        gateway.WebhookUrl.Should().Be("https://api.example.com/webhooks/vnpay");
    }

    [Fact]
    public void SetWebhookUrl_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.SetWebhookUrl("https://old-url.com");

        // Act
        gateway.SetWebhookUrl("https://new-url.com");

        // Assert
        gateway.WebhookUrl.Should().Be("https://new-url.com");
    }

    #endregion

    #region SetAmountLimits

    [Fact]
    public void SetAmountLimits_ShouldSetMinAndMaxAmount()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetAmountLimits(10_000m, 50_000_000m);

        // Assert
        gateway.MinAmount.Should().Be(10_000m);
        gateway.MaxAmount.Should().Be(50_000_000m);
    }

    [Fact]
    public void SetAmountLimits_WithNullValues_ShouldAllowNull()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetAmountLimits(null, null);

        // Assert
        gateway.MinAmount.Should().BeNull();
        gateway.MaxAmount.Should().BeNull();
    }

    [Fact]
    public void SetAmountLimits_WithOnlyMinAmount_ShouldSetMinOnly()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetAmountLimits(5_000m, null);

        // Assert
        gateway.MinAmount.Should().Be(5_000m);
        gateway.MaxAmount.Should().BeNull();
    }

    [Fact]
    public void SetAmountLimits_CalledTwice_ShouldOverwritePreviousValues()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.SetAmountLimits(1_000m, 100_000m);

        // Act
        gateway.SetAmountLimits(5_000m, 200_000m);

        // Assert
        gateway.MinAmount.Should().Be(5_000m);
        gateway.MaxAmount.Should().Be(200_000m);
    }

    #endregion

    #region SetSupportedCurrencies

    [Fact]
    public void SetSupportedCurrencies_ShouldSetJsonString()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetSupportedCurrencies("[\"VND\",\"USD\"]");

        // Assert
        gateway.SupportedCurrencies.Should().Be("[\"VND\",\"USD\"]");
    }

    [Fact]
    public void SetSupportedCurrencies_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.SetSupportedCurrencies("[\"VND\"]");

        // Act
        gateway.SetSupportedCurrencies("[\"VND\",\"USD\",\"EUR\"]");

        // Assert
        gateway.SupportedCurrencies.Should().Be("[\"VND\",\"USD\",\"EUR\"]");
    }

    #endregion

    #region SetSortOrder

    [Fact]
    public void SetSortOrder_ShouldSetValue()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.SetSortOrder(5);

        // Assert
        gateway.SortOrder.Should().Be(5);
    }

    [Fact]
    public void SetSortOrder_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.SetSortOrder(1);

        // Act
        gateway.SetSortOrder(10);

        // Assert
        gateway.SortOrder.Should().Be(10);
    }

    #endregion

    #region UpdateDisplayName

    [Fact]
    public void UpdateDisplayName_ShouldSetNewName()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.UpdateDisplayName("Updated VNPay");

        // Assert
        gateway.DisplayName.Should().Be("Updated VNPay");
    }

    [Fact]
    public void UpdateDisplayName_ShouldOverwritePreviousValue()
    {
        // Arrange
        var gateway = CreateTestGateway(displayName: "Original Name");

        // Act
        gateway.UpdateDisplayName("New Name");

        // Assert
        gateway.DisplayName.Should().Be("New Name");
    }

    #endregion

    #region UpdateEnvironment

    [Fact]
    public void UpdateEnvironment_FromSandboxToProduction_ShouldUpdate()
    {
        // Arrange
        var gateway = CreateTestGateway(environment: GatewayEnvironment.Sandbox);

        // Act
        gateway.UpdateEnvironment(GatewayEnvironment.Production);

        // Assert
        gateway.Environment.Should().Be(GatewayEnvironment.Production);
    }

    [Fact]
    public void UpdateEnvironment_FromProductionToSandbox_ShouldUpdate()
    {
        // Arrange
        var gateway = CreateTestGateway(environment: GatewayEnvironment.Production);

        // Act
        gateway.UpdateEnvironment(GatewayEnvironment.Sandbox);

        // Assert
        gateway.Environment.Should().Be(GatewayEnvironment.Sandbox);
    }

    #endregion

    #region UpdateCredentials

    [Fact]
    public void UpdateCredentials_ShouldSetNewCredentials()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.Configure("old-creds", "secret");

        // Act
        gateway.UpdateCredentials("new-encrypted-creds");

        // Assert
        gateway.EncryptedCredentials.Should().Be("new-encrypted-creds");
    }

    [Fact]
    public void UpdateCredentials_ShouldNotAffectWebhookSecret()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.Configure("old-creds", "my-secret");

        // Act
        gateway.UpdateCredentials("new-creds");

        // Assert
        gateway.EncryptedCredentials.Should().Be("new-creds");
        gateway.WebhookSecret.Should().Be("my-secret");
    }

    #endregion

    #region Activate / Deactivate

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.IsActive.Should().BeFalse();

        // Act
        gateway.Activate();

        // Assert
        gateway.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.Activate();

        // Act
        gateway.Activate();

        // Assert
        gateway.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var gateway = CreateTestGateway();
        gateway.Activate();
        gateway.IsActive.Should().BeTrue();

        // Act
        gateway.Deactivate();

        // Assert
        gateway.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.Deactivate();

        // Assert
        gateway.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ActivateThenDeactivate_ShouldToggleCorrectly()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act & Assert
        gateway.Activate();
        gateway.IsActive.Should().BeTrue();

        gateway.Deactivate();
        gateway.IsActive.Should().BeFalse();

        gateway.Activate();
        gateway.IsActive.Should().BeTrue();
    }

    #endregion

    #region UpdateHealthStatus

    [Fact]
    public void UpdateHealthStatus_ToHealthy_ShouldSetStatusAndTimestamp()
    {
        // Arrange
        var gateway = CreateTestGateway();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        gateway.UpdateHealthStatus(GatewayHealthStatus.Healthy);

        // Assert
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Healthy);
        gateway.LastHealthCheck.Should().NotBeNull();
        gateway.LastHealthCheck.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void UpdateHealthStatus_ToDegraded_ShouldSetStatus()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.UpdateHealthStatus(GatewayHealthStatus.Degraded);

        // Assert
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Degraded);
    }

    [Fact]
    public void UpdateHealthStatus_ToUnhealthy_ShouldSetStatus()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.UpdateHealthStatus(GatewayHealthStatus.Unhealthy);

        // Assert
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Unhealthy);
    }

    [Fact]
    public void UpdateHealthStatus_CalledMultipleTimes_ShouldUpdateTimestampEachTime()
    {
        // Arrange
        var gateway = CreateTestGateway();

        // Act
        gateway.UpdateHealthStatus(GatewayHealthStatus.Healthy);
        var firstCheck = gateway.LastHealthCheck;

        gateway.UpdateHealthStatus(GatewayHealthStatus.Degraded);
        var secondCheck = gateway.LastHealthCheck;

        // Assert
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Degraded);
        secondCheck.Should().BeOnOrAfter(firstCheck!.Value);
    }

    #endregion

    #region Full Configuration Workflow

    [Fact]
    public void FullWorkflow_CreateConfigureActivate_ShouldSetAllFields()
    {
        // Arrange & Act
        var gateway = PaymentGateway.Create("momo", "MoMo Wallet", GatewayEnvironment.Production, TestTenantId);
        gateway.Configure("encrypted-api-key-json", "momo-webhook-secret");
        gateway.SetWebhookUrl("https://api.example.com/webhooks/momo");
        gateway.SetAmountLimits(10_000m, 20_000_000m);
        gateway.SetSupportedCurrencies("[\"VND\"]");
        gateway.SetSortOrder(2);
        gateway.Activate();
        gateway.UpdateHealthStatus(GatewayHealthStatus.Healthy);

        // Assert
        gateway.Provider.Should().Be("momo");
        gateway.DisplayName.Should().Be("MoMo Wallet");
        gateway.Environment.Should().Be(GatewayEnvironment.Production);
        gateway.EncryptedCredentials.Should().Be("encrypted-api-key-json");
        gateway.WebhookSecret.Should().Be("momo-webhook-secret");
        gateway.WebhookUrl.Should().Be("https://api.example.com/webhooks/momo");
        gateway.MinAmount.Should().Be(10_000m);
        gateway.MaxAmount.Should().Be(20_000_000m);
        gateway.SupportedCurrencies.Should().Be("[\"VND\"]");
        gateway.SortOrder.Should().Be(2);
        gateway.IsActive.Should().BeTrue();
        gateway.HealthStatus.Should().Be(GatewayHealthStatus.Healthy);
        gateway.LastHealthCheck.Should().NotBeNull();
    }

    #endregion
}
