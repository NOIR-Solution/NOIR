using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.Application.Features.Audit.Commands.CreateRetentionPolicy;

/// <summary>
/// Command to create a new audit retention policy.
/// </summary>
public record CreateRetentionPolicyCommand(
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
    int Priority) : IAuditableCommand
{
    public object? GetTargetId() => null; // Create operation - no target ID
    public AuditOperationType OperationType => AuditOperationType.Create;
}

/// <summary>
/// Handler for CreateRetentionPolicyCommand.
/// </summary>
public class CreateRetentionPolicyCommandHandler
{
    private readonly IRepository<AuditRetentionPolicy, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateRetentionPolicyCommandHandler(
        IRepository<AuditRetentionPolicy, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditRetentionPolicyDto>> Handle(
        CreateRetentionPolicyCommand cmd,
        CancellationToken ct)
    {
        // Create the policy
        var policy = AuditRetentionPolicy.Create(
            cmd.Name,
            _currentUser.TenantId,
            cmd.Description,
            cmd.CompliancePreset);

        // Apply custom retention periods if not using a preset
        if (string.IsNullOrEmpty(cmd.CompliancePreset) || cmd.CompliancePreset == "CUSTOM")
        {
            policy.UpdateRetentionPeriods(
                cmd.HotStorageDays,
                cmd.WarmStorageDays,
                cmd.ColdStorageDays,
                cmd.DeleteAfterDays);
        }

        // Set additional properties
        policy.ExportBeforeArchive = cmd.ExportBeforeArchive;
        policy.ExportBeforeDelete = cmd.ExportBeforeDelete;
        policy.Priority = cmd.Priority;

        if (cmd.EntityTypes?.Length > 0)
        {
            policy.EntityTypesJson = System.Text.Json.JsonSerializer.Serialize(cmd.EntityTypes);
        }

        await _repository.AddAsync(policy, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AuditRetentionPolicyDto(
            policy.Id,
            policy.TenantId,
            policy.Name,
            policy.Description,
            policy.HotStorageDays,
            policy.WarmStorageDays,
            policy.ColdStorageDays,
            policy.DeleteAfterDays,
            cmd.EntityTypes,
            policy.CompliancePreset,
            policy.ExportBeforeArchive,
            policy.ExportBeforeDelete,
            policy.IsActive,
            policy.Priority,
            policy.CreatedAt,
            policy.CreatedBy,
            policy.ModifiedAt,
            policy.ModifiedBy));
    }
}

/// <summary>
/// Validator for CreateRetentionPolicyCommand.
/// </summary>
public class CreateRetentionPolicyCommandValidator : AbstractValidator<CreateRetentionPolicyCommand>
{
    public CreateRetentionPolicyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.HotStorageDays)
            .GreaterThanOrEqualTo(0).WithMessage("Hot storage days must be non-negative");

        RuleFor(x => x.WarmStorageDays)
            .GreaterThanOrEqualTo(x => x.HotStorageDays)
            .WithMessage("Warm storage days must be >= hot storage days");

        RuleFor(x => x.ColdStorageDays)
            .GreaterThanOrEqualTo(x => x.WarmStorageDays)
            .WithMessage("Cold storage days must be >= warm storage days");

        RuleFor(x => x.DeleteAfterDays)
            .GreaterThanOrEqualTo(x => x.ColdStorageDays)
            .WithMessage("Delete after days must be >= cold storage days");

        RuleFor(x => x.CompliancePreset)
            .Must(BeValidPreset).When(x => !string.IsNullOrEmpty(x.CompliancePreset))
            .WithMessage("Invalid compliance preset. Valid values: GDPR, SOX, HIPAA, PCI-DSS, CUSTOM");
    }

    private static bool BeValidPreset(string? preset)
    {
        if (string.IsNullOrEmpty(preset)) return true;
        return preset.ToUpperInvariant() switch
        {
            "GDPR" or "SOX" or "HIPAA" or "PCI-DSS" or "PCI" or "CUSTOM" => true,
            _ => false
        };
    }
}
