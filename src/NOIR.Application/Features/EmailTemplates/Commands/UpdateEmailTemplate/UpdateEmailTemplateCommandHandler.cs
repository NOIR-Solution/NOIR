namespace NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Wolverine handler for updating an email template.
/// </summary>
public class UpdateEmailTemplateCommandHandler
{
    private readonly IRepository<EmailTemplate, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailTemplateCommandHandler(
        IRepository<EmailTemplate, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EmailTemplateDto>> Handle(
        UpdateEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        // Get template with tracking for modification
        var spec = new EmailTemplateByIdForUpdateSpec(command.Id);
        var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound("NOIR-EMAIL-001", $"Email template with ID '{command.Id}' not found."));
        }

        // Update the template (this increments the version)
        template.Update(
            command.Subject,
            command.HtmlBody,
            command.PlainTextBody,
            command.Description,
            template.AvailableVariables); // Keep existing variables

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return updated DTO
        var dto = new EmailTemplateDto(
            template.Id,
            template.Name,
            template.Subject,
            template.HtmlBody,
            template.PlainTextBody,
            template.Language,
            template.IsActive,
            template.Version,
            template.Description,
            ParseVariables(template.AvailableVariables),
            template.CreatedAt,
            template.ModifiedAt);

        return Result.Success(dto);
    }

    private static List<string> ParseVariables(string? availableVariables)
    {
        if (string.IsNullOrWhiteSpace(availableVariables))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(availableVariables) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
