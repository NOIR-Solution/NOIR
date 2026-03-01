namespace NOIR.Application.Features.Crm.Queries.GetContacts;

public class GetContactsQueryHandler
{
    private readonly IRepository<CrmContact, Guid> _contactRepository;

    public GetContactsQueryHandler(IRepository<CrmContact, Guid> contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Result<PagedResult<Features.Crm.DTOs.ContactListDto>>> Handle(
        GetContactsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new Specifications.ContactsFilterSpec(
            query.Search, query.CompanyId, query.OwnerId, query.Source,
            skip, query.PageSize);

        var contacts = await _contactRepository.ListAsync(spec, cancellationToken);

        var countSpec = new Specifications.ContactsCountSpec(
            query.Search, query.CompanyId, query.OwnerId, query.Source);
        var totalCount = await _contactRepository.CountAsync(countSpec, cancellationToken);

        var items = contacts.Select(c => new Features.Crm.DTOs.ContactListDto(
            c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.JobTitle,
            c.Company?.Name, c.Owner != null ? $"{c.Owner.FirstName} {c.Owner.LastName}" : null,
            c.Source, c.CustomerId.HasValue, c.CreatedAt)).ToList();

        return Result.Success(PagedResult<Features.Crm.DTOs.ContactListDto>.Create(
            items, totalCount, query.Page - 1, query.PageSize));
    }
}
