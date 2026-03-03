namespace NOIR.Application.Features.Crm.Commands.CreateLead;

public class CreateLeadCommandHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<CrmContact, Guid> _contactRepository;
    private readonly IRepository<Pipeline, Guid> _pipelineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateLeadCommandHandler(
        IRepository<Lead, Guid> leadRepository,
        IRepository<CrmContact, Guid> contactRepository,
        IRepository<Pipeline, Guid> pipelineRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _leadRepository = leadRepository;
        _contactRepository = contactRepository;
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.LeadDto>> Handle(
        CreateLeadCommand command,
        CancellationToken cancellationToken)
    {
        // Validate contact exists
        var contactSpec = new Specifications.ContactByIdSpec(command.ContactId);
        var contact = await _contactRepository.FirstOrDefaultAsync(contactSpec, cancellationToken);
        if (contact is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Contact with ID '{command.ContactId}' not found.", "NOIR-CRM-020"));
        }

        // Validate pipeline exists and get first stage
        var pipelineSpec = new Specifications.PipelineByIdReadOnlySpec(command.PipelineId);
        var pipeline = await _pipelineRepository.FirstOrDefaultAsync(pipelineSpec, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Pipeline with ID '{command.PipelineId}' not found.", "NOIR-CRM-021"));
        }

        var firstStage = pipeline.Stages.OrderBy(s => s.SortOrder).FirstOrDefault();
        if (firstStage is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.Validation("PipelineId", "Pipeline has no stages."));
        }

        // Calculate sort order (max + 1 in that stage)
        var stageLeadsSpec = new Specifications.ActiveLeadsByStageSpec(firstStage.Id);
        var stageLeadCount = await _leadRepository.CountAsync(stageLeadsSpec, cancellationToken);
        var sortOrder = (double)(stageLeadCount + 1);

        var lead = Lead.Create(
            command.Title,
            command.ContactId,
            command.PipelineId,
            firstStage.Id,
            _currentUser.TenantId,
            command.CompanyId,
            command.Value,
            command.Currency,
            command.OwnerId,
            sortOrder,
            command.ExpectedCloseDate,
            command.Notes);

        await _leadRepository.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmLead",
            entityId: lead.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(lead, contact, pipeline, firstStage));
    }

    private static Features.Crm.DTOs.LeadDto MapToDto(Lead l, CrmContact contact, Pipeline pipeline, PipelineStage stage) =>
        new(l.Id, l.Title, l.ContactId, contact.FullName,
            contact.Email, l.CompanyId, l.Company?.Name, l.Value, l.Currency,
            l.OwnerId, l.Owner != null ? $"{l.Owner.FirstName} {l.Owner.LastName}" : null,
            l.PipelineId, pipeline.Name, l.StageId, stage.Name,
            stage.Color, l.Status, l.SortOrder, l.ExpectedCloseDate,
            l.WonAt, l.LostAt, l.LostReason, l.Notes,
            l.CreatedAt, l.ModifiedAt);
}
