namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;

/// <summary>
/// Wolverine handler for getting a single email template by ID.
/// </summary>
public class GetEmailTemplateQueryHandler
{
    private readonly IRepository<EmailTemplate, Guid> _repository;

    public GetEmailTemplateQueryHandler(IRepository<EmailTemplate, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<EmailTemplateDto>> Handle(
        GetEmailTemplateQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new EmailTemplateByIdSpec(query.Id);
        var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound("NOIR-EMAIL-001", $"Email template with ID '{query.Id}' not found."));
        }

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
