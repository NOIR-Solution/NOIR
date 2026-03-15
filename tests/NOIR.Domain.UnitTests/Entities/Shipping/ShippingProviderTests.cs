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
        provider.ShouldNotBeNull();
        provider.Id.ShouldNotBe(Guid.Empty);
        provider.ProviderCode.ShouldBe(TestProviderCode);
        provider.DisplayName.ShouldBe(TestDisplayName);
        provider.ProviderName.ShouldBe(TestProviderName);
        provider.Environment.ShouldBe(TestEnvironment);
        provider.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldDefaultToInactive()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldDefaultHealthStatusToUnknown()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Unknown);
    }

    [Fact]
    public void Create_ShouldDefaultSupportsCodToTrue()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportsCod.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldDefaultSupportsInsuranceToFalse()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportsInsurance.ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.EncryptedCredentials.ShouldBeNull();
        provider.WebhookSecret.ShouldBeNull();
        provider.WebhookUrl.ShouldBeNull();
        provider.MinWeightGrams.ShouldBeNull();
        provider.MaxWeightGrams.ShouldBeNull();
        provider.MinCodAmount.ShouldBeNull();
        provider.MaxCodAmount.ShouldBeNull();
        provider.ApiBaseUrl.ShouldBeNull();
        provider.TrackingUrlTemplate.ShouldBeNull();
        provider.LastHealthCheck.ShouldBeNull();
        provider.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldDefaultSortOrderToZero()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldDefaultSupportedServicesToEmptyJsonArray()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        provider.SupportedServices.ShouldBe("[]");
    }

    [Fact]
    public void Create_ShouldRaiseShippingProviderCreatedEvent()
    {
        // Act
        var provider = CreateTestProvider();

        // Assert
        var __evt = provider.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<ShippingProviderCreatedEvent>();

        __evt.ProviderId.ShouldBe(provider.Id);

        __evt.ProviderCode.ShouldBe(TestProviderCode);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var provider = CreateTestProvider(tenantId: null);

        // Assert
        provider.TenantId.ShouldBeNull();
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
        provider.ProviderCode.ShouldBe(code);
    }

    [Theory]
    [InlineData(GatewayEnvironment.Sandbox)]
    [InlineData(GatewayEnvironment.Production)]
    public void Create_WithDifferentEnvironments_ShouldSetCorrectEnvironment(GatewayEnvironment env)
    {
        // Act
        var provider = CreateTestProvider(environment: env);

        // Assert
        provider.Environment.ShouldBe(env);
    }

    [Fact]
    public void Create_MultipleProviders_ShouldGenerateUniqueIds()
    {
        // Act
        var provider1 = CreateTestProvider();
        var provider2 = CreateTestProvider();

        // Assert
        provider1.Id.ShouldNotBe(provider2.Id);
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
        provider.EncryptedCredentials.ShouldBe(encryptedCreds);
        provider.WebhookSecret.ShouldBe(webhookSecret);
    }

    [Fact]
    public void Configure_WithNullWebhookSecret_ShouldAllowNull()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.Configure("encrypted_creds", null);

        // Assert
        provider.EncryptedCredentials.ShouldBe("encrypted_creds");
        provider.WebhookSecret.ShouldBeNull();
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
        provider.EncryptedCredentials.ShouldBe("new_creds");
        provider.WebhookSecret.ShouldBe("new_secret");
    }

    #endregion

    #region Activation / Deactivation

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.IsActive.ShouldBeFalse();

        // Act
        provider.Activate();

        // Assert
        provider.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.Activate();
        provider.IsActive.ShouldBeTrue();

        // Act
        provider.Deactivate();

        // Assert
        provider.IsActive.ShouldBeFalse();
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
        provider.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.IsActive.ShouldBeFalse();

        // Act
        provider.Deactivate();

        // Assert
        provider.IsActive.ShouldBeFalse();
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
        provider.WebhookUrl.ShouldBe("https://api.example.com/webhooks/shipping/ghtk");
    }

    [Fact]
    public void SetApiBaseUrl_ShouldSetUrl()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetApiBaseUrl("https://services.giaohangtietkiem.vn");

        // Assert
        provider.ApiBaseUrl.ShouldBe("https://services.giaohangtietkiem.vn");
    }

    [Fact]
    public void SetTrackingUrlTemplate_ShouldSetTemplate()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetTrackingUrlTemplate("https://track.ghtk.vn/{trackingNumber}");

        // Assert
        provider.TrackingUrlTemplate.ShouldBe("https://track.ghtk.vn/{trackingNumber}");
    }

    [Fact]
    public void SetWeightLimits_ShouldSetMinAndMax()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetWeightLimits(100, 50_000);

        // Assert
        provider.MinWeightGrams.ShouldBe(100);
        provider.MaxWeightGrams.ShouldBe(50_000);
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
        provider.MinWeightGrams.ShouldBeNull();
        provider.MaxWeightGrams.ShouldBeNull();
    }

    [Fact]
    public void SetCodLimits_ShouldSetMinAndMax()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodLimits(10_000m, 20_000_000m);

        // Assert
        provider.MinCodAmount.ShouldBe(10_000m);
        provider.MaxCodAmount.ShouldBe(20_000_000m);
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
        provider.MinCodAmount.ShouldBeNull();
        provider.MaxCodAmount.ShouldBeNull();
    }

    [Fact]
    public void SetSupportedServices_ShouldSetJson()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetSupportedServices("""["Standard","Express","Same Day"]""");

        // Assert
        provider.SupportedServices.ShouldBe("""["Standard","Express","Same Day"]""");
    }

    [Fact]
    public void SetCodSupport_True_ShouldSetSupportsCodToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodSupport(true);

        // Assert
        provider.SupportsCod.ShouldBeTrue();
    }

    [Fact]
    public void SetCodSupport_False_ShouldSetSupportsCodToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetCodSupport(false);

        // Assert
        provider.SupportsCod.ShouldBeFalse();
    }

    [Fact]
    public void SetInsuranceSupport_True_ShouldSetSupportsInsuranceToTrue()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetInsuranceSupport(true);

        // Assert
        provider.SupportsInsurance.ShouldBeTrue();
    }

    [Fact]
    public void SetInsuranceSupport_False_ShouldSetSupportsInsuranceToFalse()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetInsuranceSupport(false);

        // Assert
        provider.SupportsInsurance.ShouldBeFalse();
    }

    [Fact]
    public void SetSortOrder_ShouldSetSortOrder()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetSortOrder(5);

        // Assert
        provider.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void UpdateDisplayName_ShouldSetNewDisplayName()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateDisplayName("GHTK Premium");

        // Assert
        provider.DisplayName.ShouldBe("GHTK Premium");
    }

    [Fact]
    public void UpdateEnvironment_ShouldSetNewEnvironment()
    {
        // Arrange
        var provider = CreateTestProvider(environment: GatewayEnvironment.Sandbox);

        // Act
        provider.UpdateEnvironment(GatewayEnvironment.Production);

        // Assert
        provider.Environment.ShouldBe(GatewayEnvironment.Production);
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
        provider.EncryptedCredentials.ShouldBe("new_encrypted_creds");
    }

    [Fact]
    public void SetMetadata_ShouldSetMetadata()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.SetMetadata("""{"region":"south","priority":"high"}""");

        // Assert
        provider.Metadata.ShouldBe("""{"region":"south","priority":"high"}""");
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
        provider.Metadata.ShouldBeNull();
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
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Healthy);
        provider.LastHealthCheck.ShouldNotBeNull();
        provider.LastHealthCheck!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Fact]
    public void UpdateHealthStatus_ToDegraded_ShouldSetStatus()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Degraded);

        // Assert
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Degraded);
        provider.LastHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateHealthStatus_ToUnhealthy_ShouldSetStatus()
    {
        // Arrange
        var provider = CreateTestProvider();

        // Act
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Unhealthy);

        // Assert
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Unhealthy);
        provider.LastHealthCheck.ShouldNotBeNull();
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
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Degraded);
        provider.LastHealthCheck!.Value.ShouldBeGreaterThanOrEqualTo(firstCheck!.Value);
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
        url.ShouldBe("https://track.ghtk.vn/TRK-12345");
    }

    [Fact]
    public void GetTrackingUrl_WithoutTemplate_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.TrackingUrlTemplate.ShouldBeNull();

        // Act
        var url = provider.GetTrackingUrl("TRK-12345");

        // Assert
        url.ShouldBeNull();
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
        url.ShouldBeNull();
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
        url.ShouldBe("https://tracking.ghn.dev/package?code=GHN-ABC-789&lang=vi");
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
        provider.IsActive.ShouldBeTrue();
        provider.EncryptedCredentials.ShouldBe("encrypted_api_key_json");
        provider.WebhookSecret.ShouldBe("webhook_secret_ghn");
        provider.WebhookUrl.ShouldBe("https://api.example.com/webhooks/ghn");
        provider.ApiBaseUrl.ShouldBe("https://online-gateway.ghn.vn");
        provider.MinWeightGrams.ShouldBe(100);
        provider.MaxWeightGrams.ShouldBe(50_000);
        provider.MinCodAmount.ShouldBe(0m);
        provider.MaxCodAmount.ShouldBe(10_000_000m);
        provider.SupportsCod.ShouldBeTrue();
        provider.SupportsInsurance.ShouldBeTrue();
        provider.SortOrder.ShouldBe(1);
        provider.HealthStatus.ShouldBe(ShippingProviderHealthStatus.Healthy);
        provider.LastHealthCheck.ShouldNotBeNull();
        provider.GetTrackingUrl("TRK-999").ShouldBe("https://tracking.ghn.dev/TRK-999");
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var provider = CreateTestProvider();
        provider.DomainEvents.Count().ShouldBeGreaterThan(0);

        // Act
        provider.ClearDomainEvents();

        // Assert
        provider.DomainEvents.ShouldBeEmpty();
    }

    #endregion
}
