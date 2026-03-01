namespace NOIR.Application.Features.Crm.Queries.GetLeadById;

public class GetLeadByIdQueryHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;

    public GetLeadByIdQueryHandler(IRepository<Lead, Guid> leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<Result<Features.Crm.DTOs.LeadDto>> Handle(
        GetLeadByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.LeadByIdReadOnlySpec(query.Id);
        var lead = await _leadRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Lead with ID '{query.Id}' not found.", "NOIR-CRM-022"));
        }

        return Result.Success(new Features.Crm.DTOs.LeadDto(
            lead.Id, lead.Title, lead.ContactId, lead.Contact?.FullName ?? "",
            lead.Contact?.Email, lead.CompanyId, lead.Company?.Name, lead.Value, lead.Currency,
            lead.OwnerId, lead.Owner != null ? $"{lead.Owner.FirstName} {lead.Owner.LastName}" : null,
            lead.PipelineId, lead.Pipeline?.Name ?? "", lead.StageId, lead.Stage?.Name ?? "",
            lead.Stage?.Color, lead.Status, lead.SortOrder, lead.ExpectedCloseDate,
            lead.WonAt, lead.LostAt, lead.LostReason, lead.Notes,
            lead.CreatedAt, lead.ModifiedAt));
    }
}
