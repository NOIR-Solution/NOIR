using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Specifications;

namespace NOIR.Application.Features.Audit.Commands.UpdateRetentionPolicy;

/// <summary>
/// Command to update an existing audit retention policy.
/// </summary>
public record UpdateRetentionPolicyCommand(
    Guid Id,
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
    int Priority) : IAuditableCommand
{
    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Update;
}

/// <summary>
/// Handler for UpdateRetentionPolicyCommand.
/// </summary>
public class UpdateRetentionPolicyCommandHandler
{
    private readonly IRepository<AuditRetentionPolicy, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRetentionPolicyCommandHandler(
        IRepository<AuditRetentionPolicy, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuditRetentionPolicyDto>> Handle(
        UpdateRetentionPolicyCommand cmd,
        CancellationToken ct)
    {
        var spec = new AuditRetentionPolicyByIdTrackingSpec(cmd.Id);
        var policy = await _repository.FirstOrDefaultAsync(spec, ct);

        if (policy is null)
        {
            return Result.Failure<AuditRetentionPolicyDto>(
                Error.NotFound("RetentionPolicy.NotFound", $"Retention policy with ID {cmd.Id} not found"));
        }

        // Update basic properties
        policy.Name = cmd.Name;
        policy.Description = cmd.Description;
        policy.ExportBeforeArchive = cmd.ExportBeforeArchive;
        policy.ExportBeforeDelete = cmd.ExportBeforeDelete;
        policy.IsActive = cmd.IsActive;
        policy.Priority = cmd.Priority;

        // Apply compliance preset or custom retention periods
        if (!string.IsNullOrEmpty(cmd.CompliancePreset) && cmd.CompliancePreset != "CUSTOM")
        {
            policy.ApplyCompliancePreset(cmd.CompliancePreset);
        }
        else
        {
            policy.UpdateRetentionPeriods(
                cmd.HotStorageDays,
                cmd.WarmStorageDays,
                cmd.ColdStorageDays,
                cmd.DeleteAfterDays);
        }

        // Update entity types
        policy.EntityTypesJson = cmd.EntityTypes?.Length > 0
            ? System.Text.Json.JsonSerializer.Serialize(cmd.EntityTypes)
            : null;

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
/// Validator for UpdateRetentionPolicyCommand.
/// </summary>
public class UpdateRetentionPolicyCommandValidator : AbstractValidator<UpdateRetentionPolicyCommand>
{
    public UpdateRetentionPolicyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Policy ID is required");

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
    }
}
