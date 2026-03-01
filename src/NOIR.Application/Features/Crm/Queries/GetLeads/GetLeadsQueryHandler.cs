namespace NOIR.Application.Features.Crm.Queries.GetLeads;

public class GetLeadsQueryHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;

    public GetLeadsQueryHandler(IRepository<Lead, Guid> leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<Result<PagedResult<Features.Crm.DTOs.LeadDto>>> Handle(
        GetLeadsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new Specifications.LeadsFilterSpec(
            query.PipelineId, query.StageId, query.OwnerId, query.Status,
            skip, query.PageSize);

        var leads = await _leadRepository.ListAsync(spec, cancellationToken);

        var countSpec = new Specifications.LeadsCountSpec(
            query.PipelineId, query.StageId, query.OwnerId, query.Status);
        var totalCount = await _leadRepository.CountAsync(countSpec, cancellationToken);

        var items = leads.Select(MapToDto).ToList();

        return Result.Success(PagedResult<Features.Crm.DTOs.LeadDto>.Create(
            items, totalCount, query.Page - 1, query.PageSize));
    }

    private static Features.Crm.DTOs.LeadDto MapToDto(Lead l) =>
        new(l.Id, l.Title, l.ContactId, l.Contact?.FullName ?? "",
            l.Contact?.Email, l.CompanyId, l.Company?.Name, l.Value, l.Currency,
            l.OwnerId, l.Owner != null ? $"{l.Owner.FirstName} {l.Owner.LastName}" : null,
            l.PipelineId, l.Pipeline?.Name ?? "", l.StageId, l.Stage?.Name ?? "",
            l.Stage?.Color, l.Status, l.SortOrder, l.ExpectedCloseDate,
            l.WonAt, l.LostAt, l.LostReason, l.Notes,
            l.CreatedAt, l.ModifiedAt);
}
