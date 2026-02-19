using NOIR.Domain.Entities.Shipping;
using NOIR.Domain.Events.Shipping;

namespace NOIR.Domain.UnitTests.Entities.Shipping;

/// <summary>
/// Unit tests for the ShippingProvider aggregate root entity.
/// Tests factory methods, configuration, activation/deactivation,
/// health status, tracking URL generation, and property setters.
/// </summary>
public class ShippingProviderTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestDisplayName = "GHTK Express";
    private const string TestProviderName = "Giao Hang Tiet Kiem";
    private const ShippingProviderCode TestProviderCode = ShippingProviderCode.GHTK;
    private const GatewayEnvironment TestEnvironment = GatewayEnvironment.Sandbox;

    /// <summary>
    /// Helper to create a default valid shipping provider for tests.
    /// </summary>
    private static ShippingProvider CreateTestProvider(
        ShippingProviderCode providerCode = TestProviderCode,
        string displayName = TestDisplayName,
        string providerName = TestProviderName,
        GatewayEnvironment environment = TestEnvironment,
        string? tenantId = TestTenantId)
    {
        return ShippingProvider.Create(providerCode, displayName, providerName, environment, tenantId);
    }

    #region Create Factory Method

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidProvider()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.Id.Should().NotBe(Guid.Empty);
        provider.ProviderCode.Should().Be(TestProviderCode);
        provider.DisplayName.Should().Be(TestDisplayName);
        provider.ProviderName.Should().Be(TestProviderName);
        provider.Environment.Should().Be(TestEnvironment);
        provider.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldDefaultToInactive()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldDefaultHealthStatusToUnknown()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Unknown);
    }

    [Fact]
    public void Create_ShouldDefaultSupportsCodToTrue()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportsCod.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldDefaultSupportsInsuranceToFalse()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportsInsurance.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.EncryptedCredentials.Should().BeNull();
        provider.WebhookSecret.Should().BeNull();
        provider.WebhookUrl.Should().BeNull();
        provider.MinWeightGrams.Should().BeNull();
        provider.MaxWeightGrams.Should().BeNull();
        provider.MinCodAmount.Should().BeNull();
        provider.MaxCodAmount.Should().BeNull();
        provider.ApiBaseUrl.Should().BeNull();
        provider.TrackingUrlTemplate.Should().BeNull();
        provider.LastHealthCheck.Should().BeNull();
        provider.Metadata.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldDefaultSortOrderToZero()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldDefaultSupportedServicesToEmptyJsonArray()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportedServices.Should().Be("[]");
    }

    [Fact]
    public void Create_ShouldRaiseShippingProviderCreatedEvent()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ShippingProviderCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                ProviderId = provider.Id,
                ProviderCode = TestProviderCode
            });
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var provider = CreateTestProvider(tenantId: null);

        // Assert
        provider.TenantId.Should().BeNull();
    }

    [Theory]
    [InlineData(ShippingProviderCode.GHTK)]
    [InlineData(ShippingProviderCode.GHN)]
    [InlineData(ShippingProviderCode.JTExpress)]
    [InlineData(ShippingProviderCode.ViettelPost)]
    [InlineData(ShippingProviderCode.NinjaVan)]
    [InlineData(ShippingProviderCode.VNPost)]
    [InlineData(ShippingProviderCode.BestExpress)]
    [InlineData(ShippingProviderCode.Custom)]
    public void Create_WithDifferentProviderCodes_ShouldSetCorrectCode(ShippingProviderCode code)
    {
        // Act
        var provider = CreateTestProvider(providerCode: code);

        // Assert
        provider.ProviderCode.Should().Be(code);
    }

    [Theory]
    [InlineData(GatewayEnvironment.Sandbox)]
    [InlineData(GatewayEnvironment.Production)]
    public void Create_WithDifferentEnvironments_ShouldSetCorrectEnvironment(GatewayEnvironment env)
    {
        // Act
        var provider = CreateTestProvider(environment: env);

        // Assert
        provider.Environment.Should().Be(env);
    }

    [Fact]
    public void Create_MultipleProviders_ShouldGenerateUniqueIds()
    {
        // Act
        var provider1 = CreateTestProvider();
        var provider2 = CreateTestProvider();

        // Assert
        provider1.Id.Should().NotBe(provider2.Id);
    }

    #endregion

    #region Configure

    [Fact]
    public void Configure_ShouldSetCredentialsAndWebhookSecret()
    {
        // Arrange
        var provider = CreateTestProvider();
        var encryptedCreds = "AES256_ENCRYPTED_JSON_CREDS";
        var webhookSecret = "whsec_test_secret_123";

        // Act
        provider.Configure(encryptedCreds, webhookSecret);

        // Assert
        provider.EncryptedCredentials.Should().Be(encryptedCreds);
        provider.WebhookSecret.Should().Be(webhookSecret);
    }

    [Fact]
    public void Configure_WithNullWebhookSecret_ShouldAllowNull()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.Configure("encrypted_creds", null);

        // Assert
        provider.EncryptedCredentials.Should().Be("encrypted_creds");
        provider.WebhookSecret.Should().BeNull();
    }

    [Fact]
    public void Configure_ShouldOverwritePreviousCredentials()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.Configure("old_creds", "old_secret");

        // Act
        provider.Configure("new_creds", "new_secret");

        // Assert
        provider.EncryptedCredentials.Should().Be("new_creds");
        provider.WebhookSecret.Should().Be("new_secret");
    }

    #endregion

    #region Activation / Deactivation

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.IsActive.Should().BeFalse();

        // Act
        provider.Activate();

        // Assert
        provider.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.Activate();
        provider.IsActive.Should().BeTrue();

        // Act
        provider.Deactivate();

        // Assert
        provider.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.Activate();

        // Act
        provider.Activate();

        // Assert
        provider.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.IsActive.Should().BeFalse();

        // Act
        provider.Deactivate();

        // Assert
        provider.IsActive.Should().BeFalse();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void SetWebhookUrl_ShouldSetUrl()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetWebhookUrl("https://api.example.com/webhooks/shipping/ghtk");

        // Assert
        provider.WebhookUrl.Should().Be("https://api.example.com/webhooks/shipping/ghtk");
    }

    [Fact]
    public void SetApiBaseUrl_ShouldSetUrl()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetApiBaseUrl("https://services.giaohangtietkiem.vn");

        // Assert
        provider.ApiBaseUrl.Should().Be("https://services.giaohangtietkiem.vn");
    }

    [Fact]
    public void SetTrackingUrlTemplate_ShouldSetTemplate()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetTrackingUrlTemplate("https://track.ghtk.vn/{trackingNumber}");

        // Assert
        provider.TrackingUrlTemplate.Should().Be("https://track.ghtk.vn/{trackingNumber}");
    }

    [Fact]
    public void SetWeightLimits_ShouldSetMinAndMax()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetWeightLimits(100, 50_000);

        // Assert
        provider.MinWeightGrams.Should().Be(100);
        provider.MaxWeightGrams.Should().Be(50_000);
    }

    [Fact]
    public void SetWeightLimits_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetWeightLimits(100, 50_000);

        // Act
        provider.SetWeightLimits(null, null);

        // Assert
        provider.MinWeightGrams.Should().BeNull();
        provider.MaxWeightGrams.Should().BeNull();
    }

    [Fact]
    public void SetCodLimits_ShouldSetMinAndMax()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodLimits(10_000m, 20_000_000m);

        // Assert
        provider.MinCodAmount.Should().Be(10_000m);
        provider.MaxCodAmount.Should().Be(20_000_000m);
    }

    [Fact]
    public void SetCodLimits_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetCodLimits(10_000m, 20_000_000m);

        // Act
        provider.SetCodLimits(null, null);

        // Assert
        provider.MinCodAmount.Should().BeNull();
        provider.MaxCodAmount.Should().BeNull();
    }

    [Fact]
    public void SetSupportedServices_ShouldSetJson()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetSupportedServices("""["Standard","Express","Same Day"]""");

        // Assert
        provider.SupportedServices.Should().Be("""["Standard","Express","Same Day"]""");
    }

    [Fact]
    public void SetCodSupport_True_ShouldSetSupportsCodToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodSupport(true);

        // Assert
        provider.SupportsCod.Should().BeTrue();
    }

    [Fact]
    public void SetCodSupport_False_ShouldSetSupportsCodToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodSupport(false);

        // Assert
        provider.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public void SetInsuranceSupport_True_ShouldSetSupportsInsuranceToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetInsuranceSupport(true);

        // Assert
        provider.SupportsInsurance.Should().BeTrue();
    }

    [Fact]
    public void SetInsuranceSupport_False_ShouldSetSupportsInsuranceToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetInsuranceSupport(false);

        // Assert
        provider.SupportsInsurance.Should().BeFalse();
    }

    [Fact]
    public void SetSortOrder_ShouldSetSortOrder()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetSortOrder(5);

        // Assert
        provider.SortOrder.Should().Be(5);
    }

    [Fact]
    public void UpdateDisplayName_ShouldSetNewDisplayName()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateDisplayName("GHTK Premium");

        // Assert
        provider.DisplayName.Should().Be("GHTK Premium");
    }

    [Fact]
    public void UpdateEnvironment_ShouldSetNewEnvironment()
    {
        // Arrange
        var provider = CreateTestProvider(environment: GatewayEnvironment.Sandbox);

        // Act
        provider.UpdateEnvironment(GatewayEnvironment.Production);

        // Assert
        provider.Environment.Should().Be(GatewayEnvironment.Production);
    }

    [Fact]
    public void UpdateCredentials_ShouldSetNewEncryptedCredentials()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.Configure("old_creds", "secret");

        // Act
        provider.UpdateCredentials("new_encrypted_creds");

        // Assert
        provider.EncryptedCredentials.Should().Be("new_encrypted_creds");
    }

    [Fact]
    public void SetMetadata_ShouldSetMetadata()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetMetadata("""{"region":"south","priority":"high"}""");

        // Assert
        provider.Metadata.Should().Be("""{"region":"south","priority":"high"}""");
    }

    [Fact]
    public void SetMetadata_WithNull_ShouldClearMetadata()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetMetadata("""{"key":"value"}""");

        // Act
        provider.SetMetadata(null);

        // Assert
        provider.Metadata.Should().BeNull();
    }

    #endregion

    #region UpdateHealthStatus

    [Fact]
    public void UpdateHealthStatus_ToHealthy_ShouldSetStatusAndTimestamp()
    {
        // Arrange
        var provider = CreateTestProvider();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Healthy);

        // Assert
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Healthy);
        provider.LastHealthCheck.Should().NotBeNull();
        provider.LastHealthCheck.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void UpdateHealthStatus_ToDegraded_ShouldSetStatus()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Degraded);

        // Assert
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Degraded);
        provider.LastHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void UpdateHealthStatus_ToUnhealthy_ShouldSetStatus()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Unhealthy);

        // Assert
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Unhealthy);
        provider.LastHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void UpdateHealthStatus_ShouldOverwritePreviousCheckTimestamp()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Healthy);
        var firstCheck = provider.LastHealthCheck;

        // Small delay to ensure different timestamps
        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Degraded);

        // Assert
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Degraded);
        provider.LastHealthCheck.Should().BeOnOrAfter(firstCheck!.Value);
    }

    #endregion

    #region GetTrackingUrl

    [Fact]
    public void GetTrackingUrl_WithTemplate_ShouldReplaceTrackingNumber()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetTrackingUrlTemplate("https://track.ghtk.vn/{trackingNumber}");

        // Act
        var url = provider.GetTrackingUrl("TRK-12345");

        // Assert
        url.Should().Be("https://track.ghtk.vn/TRK-12345");
    }

    [Fact]
    public void GetTrackingUrl_WithoutTemplate_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.TrackingUrlTemplate.Should().BeNull();

        // Act
        var url = provider.GetTrackingUrl("TRK-12345");

        // Assert
        url.Should().BeNull();
    }

    [Fact]
    public void GetTrackingUrl_WithEmptyTemplate_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetTrackingUrlTemplate("");

        // Act
        var url = provider.GetTrackingUrl("TRK-12345");

        // Assert
        url.Should().BeNull();
    }

    [Fact]
    public void GetTrackingUrl_WithComplexTemplate_ShouldReplaceCorrectly()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.SetTrackingUrlTemplate("https://tracking.ghn.dev/package?code={trackingNumber}&lang=vi");

        // Act
        var url = provider.GetTrackingUrl("GHN-ABC-789");

        // Assert
        url.Should().Be("https://tracking.ghn.dev/package?code=GHN-ABC-789&lang=vi");
    }

    #endregion

    #region Combined Workflow

    [Fact]
    public void FullProviderSetup_ConfigureActivateAndSetLimits()
    {
        // Arrange
        var provider = CreateTestProvider(
            providerCode: ShippingProviderCode.GHN,
            displayName: "GHN Express",
            providerName: "Giao Hang Nhanh",
            environment: GatewayEnvironment.Production);

        // Act - configure
        provider.Configure("encrypted_api_key_json", "webhook_secret_ghn");
        provider.SetWebhookUrl("https://api.example.com/webhooks/ghn");
        provider.SetApiBaseUrl("https://online-gateway.ghn.vn");
        provider.SetTrackingUrlTemplate("https://tracking.ghn.dev/{trackingNumber}");
        provider.SetWeightLimits(100, 50_000);
        provider.SetCodLimits(0m, 10_000_000m);
        provider.SetSupportedServices("""["Standard","Express"]""");
        provider.SetCodSupport(true);
        provider.SetInsuranceSupport(true);
        provider.SetSortOrder(1);
        provider.SetMetadata("""{"apiVersion":"v2"}""");
        provider.Activate();
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Healthy);

        // Assert
        provider.IsActive.Should().BeTrue();
        provider.EncryptedCredentials.Should().Be("encrypted_api_key_json");
        provider.WebhookSecret.Should().Be("webhook_secret_ghn");
        provider.WebhookUrl.Should().Be("https://api.example.com/webhooks/ghn");
        provider.ApiBaseUrl.Should().Be("https://online-gateway.ghn.vn");
        provider.MinWeightGrams.Should().Be(100);
        provider.MaxWeightGrams.Should().Be(50_000);
        provider.MinCodAmount.Should().Be(0m);
        provider.MaxCodAmount.Should().Be(10_000_000m);
        provider.SupportsCod.Should().BeTrue();
        provider.SupportsInsurance.Should().BeTrue();
        provider.SortOrder.Should().Be(1);
        provider.HealthStatus.Should().Be(ShippingProviderHealthStatus.Healthy);
        provider.LastHealthCheck.Should().NotBeNull();
        provider.GetTrackingUrl("TRK-999").Should().Be("https://tracking.ghn.dev/TRK-999");
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.DomainEvents.Should().HaveCountGreaterThan(0);

        // Act
        provider.ClearDomainEvents();

        // Assert
        provider.DomainEvents.Should().BeEmpty();
    }

    #endregion
}
