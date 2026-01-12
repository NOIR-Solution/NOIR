namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;

/// <summary>
/// Wolverine handler for getting list of email templates.
/// </summary>
public class GetEmailTemplatesQueryHandler
{
    private readonly IRepository<EmailTemplate, Guid> _repository;

    public GetEmailTemplatesQueryHandler(IRepository<EmailTemplate, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<EmailTemplateListDto>>> Handle(
        GetEmailTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new EmailTemplatesSpec(query.Language, query.Search);
        var templates = await _repository.ListAsync(spec, cancellationToken);

        var result = templates.Select(t => new EmailTemplateListDto(
            t.Id,
            t.Name,
            t.Subject,
            t.Language,
            t.IsActive,
            t.Version,
            t.Description,
            ParseVariables(t.AvailableVariables)
        )).ToList();

        return Result.Success(result);
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
