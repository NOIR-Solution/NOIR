namespace NOIR.Application.Features.Crm.Commands.UpdateContact;

public class UpdateContactCommandHandler
{
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateContactCommandHandler(
        IRepository<CrmContact, Guid> contactRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.ContactDto>> Handle(
        UpdateContactCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ContactByIdSpec(command.Id);
        var contact = await _contactRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (contact is null)
        {
            return Result.Failure<Features.Crm.DTOs.ContactDto>(
                Error.NotFound($"Contact with ID '{command.Id}' not found.", "NOIR-CRM-002"));
        }

        // Validate email uniqueness (exclude self)
        var emailSpec = new Specifications.ContactByEmailSpec(command.Email, command.Id);
        var existingByEmail = await _contactRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result.Failure<Features.Crm.DTOs.ContactDto>(
                Error.Conflict($"A contact with email '{command.Email}' already exists.", "NOIR-CRM-001"));
        }

        contact.Update(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Source,
            command.Phone,
            command.JobTitle,
            command.CompanyId,
            command.OwnerId,
            command.CustomerId,
            command.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmContact",
            entityId: contact.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(contact));
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
