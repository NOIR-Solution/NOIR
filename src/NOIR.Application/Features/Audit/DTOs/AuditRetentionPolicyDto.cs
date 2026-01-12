namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// DTO for audit retention policy.
/// </summary>
public record AuditRetentionPolicyDto(
    Guid Id,
    string? TenantId,
    string Name,
    string? Description,
    int HotStorageDays,
    int WarmStorageDays,
    int ColdStorageDays,
    int DeleteAfterDays,
    string[]? EntityTypes,
    string? CompliancePreset,
    bool ExportBeforeArchive,
    bool ExportBeforeDelete,
    bool IsActive,
    int Priority,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAt,
    string? LastModifiedBy);

/// <summary>
/// DTO for creating a new retention policy.
/// </summary>
public record CreateRetentionPolicyDto(
    string Name,
    string? Description,
    int HotStorageDays,
    int WarmStorageDays,
    int ColdStorageDays,
    int DeleteAfterDays,
    string[]? EntityTypes,
    string? CompliancePreset,
    bool ExportBeforeArchive,
    bool ExportBeforeDelete,
    int Priority);

/// <summary>
/// DTO for updating an existing retention policy.
/// </summary>
public record UpdateRetentionPolicyDto(
    string Name,
    string? Description,
    int HotStorageDays,
    int WarmStorageDays,
    int ColdStorageDays,
    int DeleteAfterDays,
    string[]? EntityTypes,
    string? CompliancePreset,
    bool ExportBeforeArchive,
    bool ExportBeforeDelete,
    bool IsActive,
    int Priority);

/// <summary>
/// DTO for applying a compliance preset.
/// </summary>
public record ApplyCompliancePresetDto(
    string PresetName);

/// <summary>
/// Available compliance presets.
/// </summary>
public static class CompliancePresets
{
    public const string GDPR = "GDPR";
    public const string SOX = "SOX";
    public const string HIPAA = "HIPAA";
    public const string PCI = "PCI-DSS";
    public const string Custom = "CUSTOM";

    public static readonly IReadOnlyList<CompliancePresetInfo> All =
    [
        new(GDPR, "GDPR - Data Minimization", 30, 90, 365, 365),
        new(SOX, "SOX - 7-8 Year Retention", 90, 365, 2555, 2920),
        new(HIPAA, "HIPAA - 6-7 Year PHI Retention", 90, 365, 2190, 2555),
        new(PCI, "PCI-DSS - 1 Year with 3 Months Ready", 90, 365, 365, 450),
        new(Custom, "Custom - User Defined", 30, 90, 365, 2555)
    ];
}

/// <summary>
/// Information about a compliance preset.
/// </summary>
public record CompliancePresetInfo(
    string Name,
    string Description,
    int HotStorageDays,
    int WarmStorageDays,
    int ColdStorageDays,
    int DeleteAfterDays);

/// <summary>
/// DTO for compliance preset display in API responses.
/// </summary>
public record CompliancePresetDto(
    string Code,
    string Name,
    int HotStorageDays,
    int WarmStorageDays,
    int ColdStorageDays,
    int DeleteAfterDays,
    string Description);
