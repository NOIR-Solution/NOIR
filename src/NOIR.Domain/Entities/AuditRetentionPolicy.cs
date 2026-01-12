namespace NOIR.Domain.Entities;

/// <summary>
/// Configurable retention policy for audit logs.
/// Supports tenant-specific policies and compliance presets (GDPR, SOX, HIPAA, PCI-DSS).
/// </summary>
public class AuditRetentionPolicy : AggregateRoot<Guid>, ITenantEntity
{
    /// <summary>
    /// Tenant ID for multi-tenant filtering.
    /// Null means this is a system-wide default policy.
    /// </summary>
    public string? TenantId { get; protected set; }

    /// <summary>
    /// Policy name for display (e.g., "Default Policy", "GDPR Compliant").
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Optional description of the policy.
    /// </summary>
    public string? Description { get; set; }

    #region Tiered Retention (Days)

    /// <summary>
    /// Days to keep in hot storage (fast access, not archived).
    /// Default: 30 days
    /// </summary>
    public int HotStorageDays { get; set; } = 30;

    /// <summary>
    /// Days to keep in warm storage (archived but accessible).
    /// Default: 90 days
    /// </summary>
    public int WarmStorageDays { get; set; } = 90;

    /// <summary>
    /// Days to keep in cold storage (compressed, slow access).
    /// Default: 365 days (1 year)
    /// </summary>
    public int ColdStorageDays { get; set; } = 365;

    /// <summary>
    /// Days after which records are permanently deleted.
    /// Default: 2555 days (~7 years for SOX compliance)
    /// </summary>
    public int DeleteAfterDays { get; set; } = 2555;

    #endregion

    #region Scope

    /// <summary>
    /// Entity types this policy applies to (null = all entity types).
    /// Stored as JSON array (e.g., ["Customer", "Order"]).
    /// </summary>
    public string? EntityTypesJson { get; set; }

    /// <summary>
    /// Compliance preset identifier (GDPR, SOX, HIPAA, PCI-DSS, Custom).
    /// Used to apply predefined retention periods.
    /// </summary>
    public string? CompliancePreset { get; set; }

    #endregion

    #region Options

    /// <summary>
    /// Whether to export records to file storage before archiving.
    /// </summary>
    public bool ExportBeforeArchive { get; set; } = true;

    /// <summary>
    /// Whether to export records to file storage before permanent deletion.
    /// </summary>
    public bool ExportBeforeDelete { get; set; } = true;

    /// <summary>
    /// Whether this policy is active and should be applied.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for policy evaluation (higher = evaluated first).
    /// Used when multiple policies could apply.
    /// </summary>
    public int Priority { get; set; }

    #endregion

    /// <summary>
    /// Creates a new audit retention policy.
    /// </summary>
    public static AuditRetentionPolicy Create(
        string name,
        string? tenantId = null,
        string? description = null,
        string? compliancePreset = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var policy = new AuditRetentionPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            TenantId = tenantId,
            Description = description,
            CompliancePreset = compliancePreset
        };

        // Apply compliance preset defaults if specified
        if (!string.IsNullOrEmpty(compliancePreset))
        {
            policy.ApplyCompliancePreset(compliancePreset);
        }

        return policy;
    }

    /// <summary>
    /// Applies predefined retention periods based on compliance framework.
    /// </summary>
    public void ApplyCompliancePreset(string preset)
    {
        CompliancePreset = preset;

        switch (preset.ToUpperInvariant())
        {
            case "GDPR":
                // GDPR: Data minimization, shorter retention
                HotStorageDays = 30;
                WarmStorageDays = 90;
                ColdStorageDays = 365;
                DeleteAfterDays = 365; // 1 year
                Description = "GDPR-compliant: 1-year maximum retention with data minimization";
                break;

            case "SOX":
                // SOX: 7-year retention for financial records
                HotStorageDays = 90;
                WarmStorageDays = 365;
                ColdStorageDays = 2555;
                DeleteAfterDays = 2920; // ~8 years
                Description = "SOX-compliant: 7-8 year retention for financial audit trails";
                break;

            case "HIPAA":
                // HIPAA: 6-year retention for PHI
                HotStorageDays = 90;
                WarmStorageDays = 365;
                ColdStorageDays = 2190;
                DeleteAfterDays = 2555; // ~7 years
                Description = "HIPAA-compliant: 6-7 year retention for protected health information";
                break;

            case "PCI-DSS":
            case "PCI":
                // PCI-DSS: 1 year with 3 months readily available
                HotStorageDays = 90;
                WarmStorageDays = 365;
                ColdStorageDays = 365;
                DeleteAfterDays = 450; // ~15 months
                Description = "PCI-DSS compliant: 1 year retention with 3 months readily available";
                break;

            case "CUSTOM":
            default:
                // Custom: Keep existing values
                Description ??= "Custom retention policy";
                break;
        }
    }

    /// <summary>
    /// Updates the retention periods.
    /// </summary>
    public void UpdateRetentionPeriods(
        int hotStorageDays,
        int warmStorageDays,
        int coldStorageDays,
        int deleteAfterDays)
    {
        if (hotStorageDays < 0) throw new ArgumentException("Hot storage days cannot be negative", nameof(hotStorageDays));
        if (warmStorageDays < hotStorageDays) throw new ArgumentException("Warm storage days must be >= hot storage days", nameof(warmStorageDays));
        if (coldStorageDays < warmStorageDays) throw new ArgumentException("Cold storage days must be >= warm storage days", nameof(coldStorageDays));
        if (deleteAfterDays < coldStorageDays) throw new ArgumentException("Delete after days must be >= cold storage days", nameof(deleteAfterDays));

        HotStorageDays = hotStorageDays;
        WarmStorageDays = warmStorageDays;
        ColdStorageDays = coldStorageDays;
        DeleteAfterDays = deleteAfterDays;

        // Clear compliance preset if periods are modified directly
        CompliancePreset = "CUSTOM";
    }
}
