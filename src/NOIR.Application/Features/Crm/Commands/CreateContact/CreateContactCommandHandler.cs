namespace NOIR.Application.Features.Crm.Commands.CreateContact;

public class CreateContactCommandHandler
{
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateContactCommandHandler(
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
        CreateContactCommand command,
        CancellationToken cancellationToken)
    {
        // Validate email uniqueness
        var emailSpec = new Specifications.ContactByEmailSpec(command.Email);
        var existingByEmail = await _contactRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result.Failure<Features.Crm.DTOs.ContactDto>(
                Error.Conflict($"A contact with email '{command.Email}' already exists.", "NOIR-CRM-001"));
        }

        var contact = CrmContact.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Source,
            _currentUser.TenantId,
            command.Phone,
            command.JobTitle,
            command.CompanyId,
            command.OwnerId,
            notes: command.Notes);

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmContact",
            entityId: contact.Id,
            operation: EntityOperation.Created,
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
