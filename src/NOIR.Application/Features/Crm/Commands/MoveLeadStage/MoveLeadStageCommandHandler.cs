namespace NOIR.Application.Features.Crm.Commands.MoveLeadStage;

public class MoveLeadStageCommandHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public MoveLeadStageCommandHandler(
        IRepository<Lead, Guid> leadRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _leadRepository = leadRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.LeadDto>> Handle(
        MoveLeadStageCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.LeadByIdSpec(command.LeadId);
        var lead = await _leadRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Lead with ID '{command.LeadId}' not found.", "NOIR-CRM-022"));
        }

        // Reordering within the same stage — always allowed (active or system)
        if (lead.StageId == command.NewStageId)
        {
            lead.MoveToStage(command.NewStageId, command.NewSortOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await PublishUpdate(lead, cancellationToken);
            return Result.Success(MapToDto(lead));
        }

        // Cross-stage move: validate target stage
        var targetStage = await _dbContext.PipelineStages
            .TagWith("MoveLeadStageCommandHandler")
            .Where(s => s.Id == command.NewStageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetStage is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Stage with ID '{command.NewStageId}' not found.", "NOIR-CRM-031"));
        }

        if (targetStage.StageType != StageType.Active)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.Validation("StageId", "Use WinLead or LoseLead commands to move a lead to Won or Lost stage."));
        }

        if (lead.Status != LeadStatus.Active)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.Validation("LeadId", "Use ReopenLead command before moving a Won or Lost lead to an active stage."));
        }

        lead.MoveToStage(command.NewStageId, command.NewSortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishUpdate(lead, cancellationToken);

        return Result.Success(MapToDto(lead));
    }

    private async Task PublishUpdate(Lead lead, CancellationToken cancellationToken) =>
        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmLead",
            entityId: lead.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

    private static Features.Crm.DTOs.LeadDto MapToDto(Lead l) =>
        new(l.Id, l.Title, l.ContactId, l.Contact?.FullName ?? "",
            l.Contact?.Email, l.CompanyId, l.Company?.Name, l.Value, l.Currency,
            l.OwnerId, l.Owner != null ? $"{l.Owner.FirstName} {l.Owner.LastName}" : null,
            l.PipelineId, l.Pipeline?.Name ?? "", l.StageId, l.Stage?.Name ?? "",
            l.Stage?.Color, l.Status, l.SortOrder, l.ExpectedCloseDate,
            l.WonAt, l.LostAt, l.LostReason, l.Notes,
            l.CreatedAt, l.ModifiedAt);
}
