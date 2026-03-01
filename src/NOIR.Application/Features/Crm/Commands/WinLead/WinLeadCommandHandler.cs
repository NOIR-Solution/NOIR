namespace NOIR.Application.Features.Crm.Commands.WinLead;

public class WinLeadCommandHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public WinLeadCommandHandler(
        IRepository<Lead, Guid> leadRepository,
        IRepository<CrmContact, Guid> contactRepository,
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _leadRepository = leadRepository;
        _contactRepository = contactRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Features.Crm.DTOs.LeadDto>> Handle(
        WinLeadCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.LeadByIdSpec(command.LeadId);
        var lead = await _leadRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Lead with ID '{command.LeadId}' not found.", "NOIR-CRM-022"));
        }

        // Win the lead (domain validates status is Active)
        lead.Win();

        // Auto-create customer from contact if contact has no customer yet
        var contactSpec = new Specifications.ContactByIdSpec(lead.ContactId);
        var contact = await _contactRepository.FirstOrDefaultAsync(contactSpec, cancellationToken);

        if (contact is not null && contact.CustomerId is null)
        {
            var customer = Domain.Entities.Customer.Customer.Create(
                null,
                contact.Email,
                contact.FirstName,
                contact.LastName,
                contact.Phone,
                _currentUser.TenantId);

            await _customerRepository.AddAsync(customer, cancellationToken);

            // Link customer to contact
            contact.Update(
                contact.FirstName,
                contact.LastName,
                contact.Email,
                contact.Source,
                contact.Phone,
                contact.JobTitle,
                contact.CompanyId,
                contact.OwnerId,
                customer.Id,
                contact.Notes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(lead));
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
