namespace NOIR.Application.Features.Shipping.Queries.GetShippingProviderSchemas;

/// <summary>
/// Handler for GetShippingProviderSchemasQuery.
/// Returns credential field definitions for all supported shipping providers.
/// </summary>
public class GetShippingProviderSchemasQueryHandler
{
    private static readonly ShippingProviderSchemasDto _schemas = BuildSchemas();

    public Task<Result<ShippingProviderSchemasDto>> Handle(
        GetShippingProviderSchemasQuery query,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(_schemas));
    }

    private static ShippingProviderSchemasDto BuildSchemas()
    {
        var schemas = new Dictionary<string, ShippingProviderSchemaDto>
        {
            ["ghtk"] = new ShippingProviderSchemaDto(
                ProviderCode: "ghtk",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.GHTK),
                Description: "Most popular shipping provider in Vietnam - fastest delivery (40 hrs avg), wide coverage",
                IconUrl: "https://www.google.com/s2/favicons?domain=ghtk.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("ApiToken", "API Token", "password", Required: true,
                        Placeholder: "Your GHTK API token",
                        HelpText: "API token from GHTK merchant dashboard"),
                    new("WebhookToken", "Webhook Token", "password", Required: false,
                        Placeholder: "Webhook verification token (optional)",
                        HelpText: "Token for verifying webhook callbacks from GHTK")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://services.ghtklab.com"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://services.giaohangtietkiem.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: true,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.GHTK),
                DocumentationUrl: "https://docs.giaohangtietkiem.vn/"),

            ["ghn"] = new ShippingProviderSchemaDto(
                ProviderCode: "ghn",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.GHN),
                Description: "Second largest shipping provider - excellent API, multiple service tiers",
                IconUrl: "https://www.google.com/s2/favicons?domain=ghn.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("ShopId", "Shop ID", "text", Required: true,
                        Placeholder: "Your GHN Shop ID",
                        HelpText: "Shop ID from GHN merchant portal"),
                    new("Token", "API Token", "password", Required: true,
                        Placeholder: "Your GHN API token",
                        HelpText: "API token from GHN developer console")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://dev-online-gateway.ghn.vn"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://online-gateway.ghn.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: true,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.GHN),
                DocumentationUrl: "https://api.ghn.vn/home/docs/detail"),

            ["jtexpress"] = new ShippingProviderSchemaDto(
                ProviderCode: "jtexpress",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.JTExpress),
                Description: "100% on-time rate, fresh product support, strong cross-border capabilities",
                IconUrl: "https://www.google.com/s2/favicons?domain=jtexpress.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("CustomerCode", "Customer Code", "text", Required: true,
                        Placeholder: "Your J&T customer code",
                        HelpText: "Customer code provided by J&T Express"),
                    new("ApiAccount", "API Account", "text", Required: true,
                        Placeholder: "Your J&T API account",
                        HelpText: "API account for authentication"),
                    new("Password", "Password", "password", Required: true,
                        Placeholder: "Your J&T API password",
                        HelpText: "Password for API authentication"),
                    new("PrivateKey", "Private Key", "password", Required: true,
                        Placeholder: "Your J&T private key",
                        HelpText: "Private key for request signing")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://jtapi-test.jtexpress.vn"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://jtapi.jtexpress.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: false,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.JTExpress),
                DocumentationUrl: "https://jtexpress.vn/vi/developer"),

            ["viettelpost"] = new ShippingProviderSchemaDto(
                ProviderCode: "viettelpost",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.ViettelPost),
                Description: "State-owned carrier with strong rural coverage and competitive pricing",
                IconUrl: "https://www.google.com/s2/favicons?domain=viettelpost.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("Username", "Username", "text", Required: true,
                        Placeholder: "Your Viettel Post username",
                        HelpText: "Login username for Viettel Post API"),
                    new("Password", "Password", "password", Required: true,
                        Placeholder: "Your Viettel Post password",
                        HelpText: "Login password for Viettel Post API"),
                    new("ApiKey", "API Key", "password", Required: false,
                        Placeholder: "API key (optional)",
                        HelpText: "Optional API key for additional authentication")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://partner-dev.viettelpost.vn"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://partner.viettelpost.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: true,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.ViettelPost),
                DocumentationUrl: "https://partner.viettelpost.vn/v2/docs"),

            ["ninjavan"] = new ShippingProviderSchemaDto(
                ProviderCode: "ninjavan",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.NinjaVan),
                Description: "Tech-focused carrier with excellent returns management and real-time tracking",
                IconUrl: "https://www.google.com/s2/favicons?domain=ninjavan.co&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("ClientId", "Client ID", "text", Required: true,
                        Placeholder: "Your Ninja Van client ID",
                        HelpText: "OAuth2 client ID from Ninja Van developer portal"),
                    new("ClientSecret", "Client Secret", "password", Required: true,
                        Placeholder: "Your Ninja Van client secret",
                        HelpText: "OAuth2 client secret for API authentication")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://api-sandbox.ninjavan.co"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://api.ninjavan.co"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: false,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.NinjaVan),
                DocumentationUrl: "https://api-docs.ninjavan.co/"),

            ["vnpost"] = new ShippingProviderSchemaDto(
                ProviderCode: "vnpost",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.VNPost),
                Description: "National postal service with widest coverage across all provinces",
                IconUrl: "https://www.google.com/s2/favicons?domain=vnpost.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("ContractCode", "Contract Code", "text", Required: true,
                        Placeholder: "Your VNPost contract code",
                        HelpText: "Contract code from VNPost business agreement"),
                    new("ApiKey", "API Key", "password", Required: true,
                        Placeholder: "Your VNPost API key",
                        HelpText: "API key from VNPost developer portal"),
                    new("ApiSecret", "API Secret", "password", Required: true,
                        Placeholder: "Your VNPost API secret",
                        HelpText: "API secret for request authentication")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://donhang-test.vnpost.vn"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://donhang.vnpost.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: false,
                DefaultTrackingUrlTemplate: ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(ShippingProviderCode.VNPost),
                DocumentationUrl: "https://www.vnpost.vn/vi-vn/ho-tro/api"),

            ["bestexpress"] = new ShippingProviderSchemaDto(
                ProviderCode: "bestexpress",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.BestExpress),
                Description: "Budget-friendly option with competitive pricing for standard deliveries",
                IconUrl: "https://www.google.com/s2/favicons?domain=best-inc.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("PartnerId", "Partner ID", "text", Required: true,
                        Placeholder: "Your Best Express partner ID",
                        HelpText: "Partner ID from Best Express merchant portal"),
                    new("PartnerKey", "Partner Key", "password", Required: true,
                        Placeholder: "Your Best Express partner key",
                        HelpText: "Partner key for API authentication")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://api-test.best-inc.vn"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://api.best-inc.vn"
                    }
                ),
                SupportsCod: true,
                SupportsInsurance: false,
                DefaultTrackingUrlTemplate: null,
                DocumentationUrl: "https://www.best-inc.vn/"),

            ["custom"] = new ShippingProviderSchemaDto(
                ProviderCode: "custom",
                DisplayName: ShippingProviderMetadata.GetProviderName(ShippingProviderCode.Custom),
                Description: "Configure a custom shipping provider with your own API integration",
                IconUrl: null,
                Fields: new List<CredentialFieldDto>
                {
                    new("ApiBaseUrl", "API Base URL", "url", Required: true,
                        Placeholder: "https://api.your-provider.com",
                        HelpText: "Base URL for the provider's API"),
                    new("ApiKey", "API Key", "password", Required: false,
                        Placeholder: "API key (if required)",
                        HelpText: "Optional API key for authentication"),
                    new("ApiSecret", "API Secret", "password", Required: false,
                        Placeholder: "API secret (if required)",
                        HelpText: "Optional API secret for request signing")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>(),
                    Production: new Dictionary<string, string>()
                ),
                SupportsCod: false,
                SupportsInsurance: false,
                DefaultTrackingUrlTemplate: null)
        };

        return new ShippingProviderSchemasDto(schemas);
    }
}
