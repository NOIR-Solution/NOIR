namespace NOIR.Application.Features.Crm.Commands.LoseLead;

public class LoseLeadCommandHandler
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoseLeadCommandHandler(
        IRepository<Lead, Guid> leadRepository,
        IUnitOfWork unitOfWork)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Features.Crm.DTOs.LeadDto>> Handle(
        LoseLeadCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.LeadByIdSpec(command.LeadId);
        var lead = await _leadRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (lead is null)
        {
            return Result.Failure<Features.Crm.DTOs.LeadDto>(
                Error.NotFound($"Lead with ID '{command.LeadId}' not found.", "NOIR-CRM-022"));
        }

        // Lose the lead (domain validates status is Active)
        lead.Lose(command.Reason);
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
