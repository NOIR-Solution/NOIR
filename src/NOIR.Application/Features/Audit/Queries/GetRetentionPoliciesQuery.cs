using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Specifications;

namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get retention policies.
/// </summary>
public record GetRetentionPoliciesQuery(
    string? TenantId = null,
    bool IncludeInactive = false);

/// <summary>
/// Query to get a single retention policy by ID.
/// </summary>
public record GetRetentionPolicyByIdQuery(Guid PolicyId);

/// <summary>
/// Handler for GetRetentionPoliciesQuery.
/// </summary>
public class GetRetentionPoliciesQueryHandler
{
    private readonly IRepository<AuditRetentionPolicy, Guid> _repository;

    public GetRetentionPoliciesQueryHandler(IRepository<AuditRetentionPolicy, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<AuditRetentionPolicyDto>>> Handle(
        GetRetentionPoliciesQuery query,
        CancellationToken ct)
    {
        var spec = new AuditRetentionPoliciesSpec(query.TenantId, query.IncludeInactive);
        var policies = await _repository.ListAsync(spec, ct);

        var dtos = policies.Select(MapToDto).ToList();
        return Result.Success<IReadOnlyList<AuditRetentionPolicyDto>>(dtos);
    }

    private static AuditRetentionPolicyDto MapToDto(AuditRetentionPolicy policy)
    {
        string[]? entityTypes = null;
        if (!string.IsNullOrEmpty(policy.EntityTypesJson))
        {
            try
            {
                entityTypes = System.Text.Json.JsonSerializer.Deserialize<string[]>(policy.EntityTypesJson);
            }
            catch { /* Ignore parse errors */ }
        }

        return new AuditRetentionPolicyDto(
            policy.Id,
            policy.TenantId,
            policy.Name,
            policy.Description,
            policy.HotStorageDays,
            policy.WarmStorageDays,
            policy.ColdStorageDays,
            policy.DeleteAfterDays,
            entityTypes,
            policy.CompliancePreset,
            policy.ExportBeforeArchive,
            policy.ExportBeforeDelete,
            policy.IsActive,
            policy.Priority,
            policy.CreatedAt,
            policy.CreatedBy,
            policy.ModifiedAt,
            policy.ModifiedBy);
    }
}

/// <summary>
/// Handler for GetRetentionPolicyByIdQuery.
/// </summary>
public class GetRetentionPolicyByIdQueryHandler
{
    private readonly IRepository<AuditRetentionPolicy, Guid> _repository;

    public GetRetentionPolicyByIdQueryHandler(IRepository<AuditRetentionPolicy, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<AuditRetentionPolicyDto>> Handle(
        GetRetentionPolicyByIdQuery query,
        CancellationToken ct)
    {
        var spec = new AuditRetentionPolicyByIdSpec(query.PolicyId);
        var policy = await _repository.FirstOrDefaultAsync(spec, ct);

        if (policy is null)
        {
            return Result.Failure<AuditRetentionPolicyDto>(
                Error.NotFound("RetentionPolicy.NotFound", $"Retention policy with ID {query.PolicyId} not found"));
        }

        string[]? entityTypes = null;
        if (!string.IsNullOrEmpty(policy.EntityTypesJson))
        {
            try
            {
                entityTypes = System.Text.Json.JsonSerializer.Deserialize<string[]>(policy.EntityTypesJson);
            }
            catch { /* Ignore parse errors */ }
        }

        return Result.Success(new AuditRetentionPolicyDto(
            policy.Id,
            policy.TenantId,
            policy.Name,
            policy.Description,
            policy.HotStorageDays,
            policy.WarmStorageDays,
            policy.ColdStorageDays,
            policy.DeleteAfterDays,
            entityTypes,
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
