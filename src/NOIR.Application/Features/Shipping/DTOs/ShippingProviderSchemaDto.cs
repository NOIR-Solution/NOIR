namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// DTO for a single shipping provider schema.
/// </summary>
public record ShippingProviderSchemaDto(
    string ProviderCode,
    string DisplayName,
    string Description,
    string? IconUrl,
    List<CredentialFieldDto> Fields,
    EnvironmentDefaultsDto Environments,
    bool SupportsCod,
    bool SupportsInsurance,
    string? DefaultTrackingUrlTemplate,
    string? DocumentationUrl = null);

/// <summary>
/// DTO for all shipping provider schemas.
/// </summary>
public record ShippingProviderSchemasDto(
    Dictionary<string, ShippingProviderSchemaDto> Schemas);
