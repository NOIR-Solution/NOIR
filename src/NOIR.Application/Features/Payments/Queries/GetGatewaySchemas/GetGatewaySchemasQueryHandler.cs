namespace NOIR.Application.Features.Payments.Queries.GetGatewaySchemas;

/// <summary>
/// Handler for GetGatewaySchemasQuery.
/// Returns credential field definitions for all supported payment gateways.
/// </summary>
public class GetGatewaySchemasQueryHandler
{
    private static readonly GatewaySchemasDto _schemas = BuildSchemas();

    public Task<Result<GatewaySchemasDto>> Handle(
        GetGatewaySchemasQuery query,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(_schemas));
    }

    private static GatewaySchemasDto BuildSchemas()
    {
        var schemas = new Dictionary<string, GatewaySchemaDto>
        {
            ["vnpay"] = new GatewaySchemaDto(
                Provider: "vnpay",
                DisplayName: "VNPay",
                Description: "Vietnam Payment Gateway - ATM, Internet Banking, QR Code, Credit/Debit Card",
                IconUrl: "https://www.google.com/s2/favicons?domain=vnpay.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("TmnCode", "TMN Code", "text", Required: true,
                        Placeholder: "Your VNPay merchant code",
                        HelpText: "Provided by VNPay when you register as a merchant"),
                    new("HashSecret", "Hash Secret", "password", Required: true,
                        Placeholder: "Your VNPay hash secret",
                        HelpText: "Secret key for signature verification"),
                    new("Version", "API Version", "select", Required: false,
                        Default: "2.1.0",
                        Placeholder: "Select API version",
                        HelpText: "VNPay API version",
                        Options: new List<FieldOptionDto>
                        {
                            new("2.1.0", "Version 2.1.0", "Latest stable version (recommended)"),
                            new("2.0.1", "Version 2.0.1", "Legacy version"),
                            new("2.0.0", "Version 2.0.0", "Legacy version")
                        }),
                    new("PaymentUrl", "Payment URL", "url", Required: false,
                        Placeholder: "Custom payment URL (optional)",
                        HelpText: "Override default payment URL if needed"),
                    new("ApiUrl", "API URL", "url", Required: false,
                        Placeholder: "Custom API URL (optional)",
                        HelpText: "Override default API URL if needed")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["PaymentUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                        ["ApiUrl"] = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["PaymentUrl"] = "https://pay.vnpay.vn/vpcpay.html",
                        ["ApiUrl"] = "https://merchant.vnpay.vn/merchant_webapi/api/transaction"
                    }
                ),
                SupportsCod: false,
                DocumentationUrl: "https://sandbox.vnpayment.vn/apis/"),

            ["momo"] = new GatewaySchemaDto(
                Provider: "momo",
                DisplayName: "MoMo",
                Description: "Vietnam E-Wallet - QR Code and App payments",
                IconUrl: "https://www.google.com/s2/favicons?domain=momo.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("PartnerCode", "Partner Code", "text", Required: true,
                        Placeholder: "Your MoMo partner code",
                        HelpText: "Provided by MoMo when you register as a merchant"),
                    new("AccessKey", "Access Key", "text", Required: true,
                        Placeholder: "Your MoMo access key",
                        HelpText: "API access key from MoMo merchant portal"),
                    new("SecretKey", "Secret Key", "password", Required: true,
                        Placeholder: "Your MoMo secret key",
                        HelpText: "Secret key for signature generation"),
                    new("ApiEndpoint", "API Endpoint", "url", Required: false,
                        Placeholder: "Custom API endpoint (optional)",
                        HelpText: "Override default API endpoint if needed")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiEndpoint"] = "https://test-payment.momo.vn/v2/gateway/api"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiEndpoint"] = "https://payment.momo.vn/v2/gateway/api"
                    }
                ),
                SupportsCod: false,
                DocumentationUrl: "https://developers.momo.vn/"),

            ["zalopay"] = new GatewaySchemaDto(
                Provider: "zalopay",
                DisplayName: "ZaloPay",
                Description: "Vietnam E-Wallet by Zalo - QR Code and App payments",
                IconUrl: "https://www.google.com/s2/favicons?domain=zalopay.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("AppId", "App ID", "text", Required: true,
                        Placeholder: "Your ZaloPay App ID",
                        HelpText: "Application ID from ZaloPay merchant portal"),
                    new("Key1", "Key 1", "password", Required: true,
                        Placeholder: "Your ZaloPay Key 1",
                        HelpText: "Primary key for signature generation"),
                    new("Key2", "Key 2", "password", Required: true,
                        Placeholder: "Your ZaloPay Key 2",
                        HelpText: "Secondary key for callback verification"),
                    new("Endpoint", "API Endpoint", "url", Required: false,
                        Placeholder: "Custom API endpoint (optional)",
                        HelpText: "Override default API endpoint if needed")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["Endpoint"] = "https://sb-openapi.zalopay.vn/v2"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["Endpoint"] = "https://openapi.zalopay.vn/v2"
                    }
                ),
                SupportsCod: false,
                DocumentationUrl: "https://docs.zalopay.vn/"),

            ["sepay"] = new GatewaySchemaDto(
                Provider: "sepay",
                DisplayName: "SePay",
                Description: "VietQR Bank Transfer - Direct bank payment with QR code, instant settlement",
                IconUrl: "https://www.google.com/s2/favicons?domain=sepay.vn&sz=64",
                Fields: new List<CredentialFieldDto>
                {
                    new("ApiToken", "API Token", "password", Required: true,
                        Placeholder: "Your SePay API Token",
                        HelpText: "API Token from SePay dashboard (Company Settings > API Access)"),
                    new("BankAccountNumber", "Bank Account Number", "text", Required: true,
                        Placeholder: "Your bank account number",
                        HelpText: "The bank account to receive payments"),
                    new("BankCode", "Bank", "select", Required: true,
                        Placeholder: "Select your bank",
                        HelpText: "The bank where your account is registered",
                        Options: GetVietnameseBankOptions()),
                    new("WebhookApiKey", "Webhook API Key", "password", Required: false,
                        Placeholder: "Optional API key for webhook authentication",
                        HelpText: "If set, SePay will include this in webhook headers for verification")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://my.sepay.vn/userapi",
                        ["QrBaseUrl"] = "https://qr.sepay.vn/img"
                    },
                    Production: new Dictionary<string, string>
                    {
                        ["ApiBaseUrl"] = "https://my.sepay.vn/userapi",
                        ["QrBaseUrl"] = "https://qr.sepay.vn/img"
                    }
                ),
                SupportsCod: false,
                DocumentationUrl: "https://docs.sepay.vn/"),

            ["cod"] = new GatewaySchemaDto(
                Provider: "cod",
                DisplayName: "Cash on Delivery",
                Description: "Pay with cash when receiving the order",
                IconUrl: null, // No website favicon, frontend will use fallback icon
                Fields: new List<CredentialFieldDto>
                {
                    new("MaxCodAmount", "Maximum COD Amount", "number", Required: false,
                        Default: "50000000",
                        Placeholder: "50000000",
                        HelpText: "Maximum order value allowed for COD (in VND)"),
                    new("CodFee", "COD Fee", "number", Required: false,
                        Default: "0",
                        Placeholder: "0",
                        HelpText: "Additional fee for COD orders (in VND)")
                },
                Environments: new EnvironmentDefaultsDto(
                    Sandbox: new Dictionary<string, string>(),
                    Production: new Dictionary<string, string>()
                ),
                SupportsCod: true)
        };

        return new GatewaySchemasDto(schemas);
    }

    /// <summary>
    /// Returns the list of Vietnamese banks supported by SePay/VietQR.
    /// Data sourced from qr.sepay.vn/banks.json
    /// </summary>
    private static List<FieldOptionDto> GetVietnameseBankOptions()
    {
        return new List<FieldOptionDto>
        {
            new("MB", "MBBank", "MB Bank (Military Bank)"),
            new("VCB", "Vietcombank", "Joint Stock Commercial Bank for Foreign Trade of Vietnam"),
            new("TCB", "Techcombank", "Vietnam Technological and Commercial Joint Stock Bank"),
            new("ACB", "ACB", "Asia Commercial Joint Stock Bank"),
            new("VPB", "VPBank", "Vietnam Prosperity Joint Stock Commercial Bank"),
            new("BIDV", "BIDV", "Bank for Investment and Development of Vietnam"),
            new("ICB", "VietinBank", "Vietnam Joint Stock Commercial Bank for Industry and Trade"),
            new("VBA", "Agribank", "Vietnam Bank for Agriculture and Rural Development"),
            new("STB", "Sacombank", "Saigon Thuong Tin Commercial Joint Stock Bank"),
            new("TPB", "TPBank", "Tien Phong Commercial Joint Stock Bank"),
            new("HDB", "HDBank", "Ho Chi Minh City Development Joint Stock Commercial Bank"),
            new("VIB", "VIB", "Vietnam International Commercial Joint Stock Bank"),
            new("MSB", "MSB", "Vietnam Maritime Commercial Joint Stock Bank"),
            new("OCB", "OCB", "Orient Commercial Joint Stock Bank"),
            new("SEAB", "SeABank", "Southeast Asia Commercial Joint Stock Bank"),
            new("EIB", "Eximbank", "Vietnam Export Import Commercial Joint Stock Bank"),
            new("LPB", "LienVietPostBank", "Lien Viet Post Joint Stock Commercial Bank"),
            new("SHBVN", "ShinhanBank", "Shinhan Bank Vietnam"),
            new("ABB", "ABBANK", "An Binh Commercial Joint Stock Bank"),
            new("BAB", "BacABank", "Bac A Commercial Joint Stock Bank"),
            new("VCCB", "VietCapitalBank", "Viet Capital Commercial Joint Stock Bank"),
            new("PBVN", "PublicBank", "Public Bank Vietnam"),
            new("KLB", "KienLongBank", "Kien Long Commercial Joint Stock Bank"),
            new("NAB", "NamABank", "Nam A Commercial Joint Stock Bank")
        };
    }
}
