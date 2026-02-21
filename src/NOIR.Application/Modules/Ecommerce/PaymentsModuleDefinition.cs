namespace NOIR.Application.Modules.Ecommerce;

public sealed class PaymentsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Payments;
    public string DisplayNameKey => "modules.ecommerce.payments";
    public string DescriptionKey => "modules.ecommerce.payments.description";
    public string Icon => "CreditCard";
    public int SortOrder => 121;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Payments.VNPay", "modules.ecommerce.payments.vnpay", "modules.ecommerce.payments.vnpay.description"),
        new("Ecommerce.Payments.MoMo", "modules.ecommerce.payments.momo", "modules.ecommerce.payments.momo.description"),
        new("Ecommerce.Payments.ZaloPay", "modules.ecommerce.payments.zalopay", "modules.ecommerce.payments.zalopay.description"),
        new("Ecommerce.Payments.COD", "modules.ecommerce.payments.cod", "modules.ecommerce.payments.cod.description"),
    ];
}
