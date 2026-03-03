namespace NOIR.Application.Features.Crm.Commands.CreateCompany;

public class CreateCompanyCommandHandler
{
    private readonly IRepository<CrmCompany, Guid> _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateCompanyCommandHandler(
        IRepository<CrmCompany, Guid> companyRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.CompanyDto>> Handle(
        CreateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        // Validate name uniqueness
        var nameSpec = new Specifications.CompanyByNameSpec(command.Name);
        var existingByName = await _companyRepository.FirstOrDefaultAsync(nameSpec, cancellationToken);
        if (existingByName is not null)
        {
            return Result.Failure<Features.Crm.DTOs.CompanyDto>(
                Error.Conflict($"A company with name '{command.Name}' already exists.", "NOIR-CRM-010"));
        }

        var company = CrmCompany.Create(
            command.Name,
            _currentUser.TenantId,
            command.Domain,
            command.Industry,
            command.Address,
            command.Phone,
            command.Website,
            command.OwnerId,
            command.TaxId,
            command.EmployeeCount,
            command.Notes);

        await _companyRepository.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmCompany",
            entityId: company.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(company));
    }

    private static Features.Crm.DTOs.CompanyDto MapToDto(CrmCompany c) =>
        new(c.Id, c.Name, c.Domain, c.Industry, c.Address, c.Phone, c.Website,
            c.OwnerId, c.Owner != null ? $"{c.Owner.FirstName} {c.Owner.LastName}" : null,
            c.TaxId, c.EmployeeCount, c.Notes, c.Contacts.Count,
            c.CreatedAt, c.ModifiedAt);
}
