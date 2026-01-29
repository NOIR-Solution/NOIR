namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for a select field option.
/// </summary>
public record FieldOptionDto(
    string Value,
    string Label,
    string? Description = null);

/// <summary>
/// DTO for a single credential field definition.
/// Supports types: text, password, url, number, select
/// </summary>
public record CredentialFieldDto(
    string Key,
    string Label,
    string Type,
    bool Required,
    string? Default = null,
    string? Placeholder = null,
    string? HelpText = null,
    List<FieldOptionDto>? Options = null);

/// <summary>
/// DTO for environment-specific default URLs.
/// </summary>
public record EnvironmentDefaultsDto(
    Dictionary<string, string> Sandbox,
    Dictionary<string, string> Production);

/// <summary>
/// DTO for a single gateway provider schema.
/// </summary>
public record GatewaySchemaDto(
    string Provider,
    string DisplayName,
    string Description,
    string? IconUrl,
    List<CredentialFieldDto> Fields,
    EnvironmentDefaultsDto Environments,
    bool SupportsCod,
    string? DocumentationUrl = null);

/// <summary>
/// DTO for all gateway schemas.
/// </summary>
public record GatewaySchemasDto(
    Dictionary<string, GatewaySchemaDto> Schemas);

/// <summary>
/// DTO for test connection result.
/// </summary>
public record TestConnectionResultDto(
    bool Success,
    string Message,
    long? ResponseTimeMs = null,
    string? ErrorCode = null);
