
namespace NOIR.Application.Features.Audit.Queries.GetActivityDetails;

/// <summary>
/// Handler for getting activity details.
/// </summary>
public sealed class GetActivityDetailsQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public GetActivityDetailsQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<ActivityDetailsDto>> Handle(
        GetActivityDetailsQuery query,
        CancellationToken cancellationToken)
    {
        // Service handles both NotFound and Forbidden errors
        return await _auditLogQueryService.GetActivityDetailsAsync(query.Id, cancellationToken);
    }
}
