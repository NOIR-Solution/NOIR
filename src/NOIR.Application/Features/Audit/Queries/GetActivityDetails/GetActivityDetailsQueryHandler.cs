using NOIR.Application.Common.Interfaces;
using NOIR.Application.Features.Audit.DTOs;

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
        var details = await _auditLogQueryService.GetActivityDetailsAsync(query.Id, cancellationToken);

        if (details is null)
        {
            return Result.Failure<ActivityDetailsDto>(
                Error.NotFound(
                    $"Activity entry with ID {query.Id} was not found.",
                    ErrorCodes.Business.NotFound));
        }

        return Result.Success(details);
    }
}
