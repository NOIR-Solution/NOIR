namespace NOIR.Application.Features.Crm.Commands.DeleteCompany;

public class DeleteCompanyCommandHandler
{
    private readonly IRepository<CrmCompany, Guid> _companyRepository;
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteCompanyCommandHandler(
        IRepository<CrmCompany, Guid> companyRepository,
        IRepository<CrmContact, Guid> contactRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _companyRepository = companyRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.CompanyDto>> Handle(
        DeleteCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.CompanyByIdSpec(command.Id);
        var company = await _companyRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (company is null)
        {
            return Result.Failure<Features.Crm.DTOs.CompanyDto>(
                Error.NotFound($"Company with ID '{command.Id}' not found.", "NOIR-CRM-011"));
        }

        // Check for existing contacts
        var hasContactsSpec = new Specifications.CompanyHasContactsSpec(command.Id);
        var contactCount = await _contactRepository.CountAsync(hasContactsSpec, cancellationToken);
        if (contactCount > 0)
        {
            return Result.Failure<Features.Crm.DTOs.CompanyDto>(
                Error.Validation("Id", "Cannot delete company with existing contacts."));
        }

        var dto = MapToDto(company);
        _companyRepository.Remove(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmCompany",
            entityId: command.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }

    private static Features.Crm.DTOs.CompanyDto MapToDto(CrmCompany c) =>
        new(c.Id, c.Name, c.Domain, c.Industry, c.Address, c.Phone, c.Website,
            c.OwnerId, c.Owner != null ? $"{c.Owner.FirstName} {c.Owner.LastName}" : null,
            c.TaxId, c.EmployeeCount, c.Notes, c.Contacts.Count,
            c.CreatedAt, c.ModifiedAt);
}
