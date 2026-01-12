namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 200;
    private const int MaxUrlLength = 500;
    private const int MaxColorLength = 50;

    public UpdateTenantCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(localization["validation.tenantId.required"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localization["validation.tenantName.required"])
            .MinimumLength(MinNameLength).WithMessage(localization.Get("validation.tenantName.minLength", MinNameLength))
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.tenantName.maxLength", MaxNameLength));

        RuleFor(x => x.LogoUrl)
            .MaximumLength(MaxUrlLength).WithMessage(localization.Get("validation.url.maxLength", MaxUrlLength))
            .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage(localization["validation.url.invalid"]);

        RuleFor(x => x.PrimaryColor)
            .MaximumLength(MaxColorLength).WithMessage(localization.Get("validation.color.maxLength", MaxColorLength))
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage(localization["validation.color.invalidHex"]);

        RuleFor(x => x.AccentColor)
            .MaximumLength(MaxColorLength).WithMessage(localization.Get("validation.color.maxLength", MaxColorLength))
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrEmpty(x.AccentColor))
            .WithMessage(localization["validation.color.invalidHex"]);

        RuleFor(x => x.Theme)
            .Must(theme => string.IsNullOrEmpty(theme) || new[] { "Light", "Dark", "System" }.Contains(theme))
            .WithMessage(localization["validation.theme.invalid"]);
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
