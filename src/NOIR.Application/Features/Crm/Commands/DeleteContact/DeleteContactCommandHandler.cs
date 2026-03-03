namespace NOIR.Application.Features.Crm.Commands.DeleteContact;

public class DeleteContactCommandHandler
{
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteContactCommandHandler(
        IRepository<CrmContact, Guid> contactRepository,
        IRepository<Lead, Guid> leadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _contactRepository = contactRepository;
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.ContactDto>> Handle(
        DeleteContactCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ContactByIdSpec(command.Id);
        var contact = await _contactRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (contact is null)
        {
            return Result.Failure<Features.Crm.DTOs.ContactDto>(
                Error.NotFound($"Contact with ID '{command.Id}' not found.", "NOIR-CRM-002"));
        }

        // Check for active leads
        var activeLeadsSpec = new Specifications.ContactHasActiveLeadsSpec(command.Id);
        var activeLeadsCount = await _leadRepository.CountAsync(activeLeadsSpec, cancellationToken);
        if (activeLeadsCount > 0)
        {
            return Result.Failure<Features.Crm.DTOs.ContactDto>(
                Error.Validation("Id", "Cannot delete contact with active deals."));
        }

        var dto = MapToDto(contact);
        _contactRepository.Remove(contact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmContact",
            entityId: command.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }

    private static Features.Crm.DTOs.ContactDto MapToDto(CrmContact c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.JobTitle,
            c.CompanyId, c.Company?.Name, c.OwnerId,
            c.Owner != null ? $"{c.Owner.FirstName} {c.Owner.LastName}" : null,
            c.Source, c.CustomerId, c.Notes,
            c.Leads.Select(l => new Features.Crm.DTOs.LeadBriefDto(
                l.Id, l.Title, l.Value, l.Currency, l.Status, l.Stage?.Name)).ToList(),
            c.CreatedAt, c.ModifiedAt);
}
