namespace NOIR.Application.Features.Shipping;

/// <summary>
/// Centralized metadata for shipping providers.
/// Contains provider names, tracking URL templates, and service type mappings.
/// </summary>
public static class ShippingProviderMetadata
{
    /// <summary>
    /// Gets the official provider name for a provider code.
    /// </summary>
    public static string GetProviderName(ShippingProviderCode code) => code switch
    {
        ShippingProviderCode.GHTK => "Giao Hàng Tiết Kiệm",
        ShippingProviderCode.GHN => "Giao Hàng Nhanh",
        ShippingProviderCode.JTExpress => "J&T Express Vietnam",
        ShippingProviderCode.ViettelPost => "Viettel Post",
        ShippingProviderCode.NinjaVan => "Ninja Van Vietnam",
        ShippingProviderCode.VNPost => "Vietnam Post",
        ShippingProviderCode.BestExpress => "Best Express Vietnam",
        ShippingProviderCode.Custom => "Custom Provider",
        _ => code.ToString()
    };

    /// <summary>
    /// Gets the default tracking URL template for a provider.
    /// Use {trackingNumber} placeholder in the URL.
    /// </summary>
    public static string? GetDefaultTrackingUrlTemplate(ShippingProviderCode code) => code switch
    {
        ShippingProviderCode.GHTK => "https://i.ghtk.vn/{trackingNumber}",
        ShippingProviderCode.GHN => "https://donhang.ghn.vn/?order_code={trackingNumber}",
        ShippingProviderCode.JTExpress => "https://jtexpress.vn/vi/tracking?billcode={trackingNumber}",
        ShippingProviderCode.ViettelPost => "https://viettelpost.vn/tra-cuu?code={trackingNumber}",
        ShippingProviderCode.NinjaVan => "https://www.ninjavan.co/vi-vn/tracking?id={trackingNumber}",
        ShippingProviderCode.VNPost => "https://www.vnpost.vn/vi-vn/dinh-vi/buu-pham?key={trackingNumber}",
        _ => null
    };

    /// <summary>
    /// Gets the localized service type name for a provider and service code.
    /// </summary>
    public static string GetServiceTypeName(ShippingProviderCode provider, string serviceTypeCode) =>
        (provider, serviceTypeCode.ToUpperInvariant()) switch
        {
            (ShippingProviderCode.GHTK, "STANDARD") => "Giao hàng tiêu chuẩn",
            (ShippingProviderCode.GHTK, "EXPRESS") => "Giao hàng nhanh",
            (ShippingProviderCode.GHN, "1") => "Chuyển phát nhanh",
            (ShippingProviderCode.GHN, "2") => "Chuyển phát tiêu chuẩn",
            (ShippingProviderCode.GHN, "3") => "Chuyển phát tiết kiệm",
            _ => serviceTypeCode
        };
}
