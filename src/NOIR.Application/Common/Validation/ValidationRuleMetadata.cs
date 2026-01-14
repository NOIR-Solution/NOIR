namespace NOIR.Application.Common.Validation;

/// <summary>
/// Metadata about a single validation rule applied to a field
/// </summary>
public sealed record ValidationRuleInfo(
    string RuleType,
    Dictionary<string, object>? Parameters = null,
    string? MessageKey = null
);

/// <summary>
/// Metadata about all validation rules applied to a single field
/// </summary>
public sealed record ValidationFieldMetadata(
    string FieldName,
    string FieldType,
    bool IsRequired,
    IReadOnlyList<ValidationRuleInfo> Rules
);

/// <summary>
/// Complete validation metadata for a command/request type
/// </summary>
public sealed record ValidatorMetadata(
    string CommandName,
    string CommandFullName,
    IReadOnlyList<ValidationFieldMetadata> Fields
);
