namespace NOIR.Application.Features.Pm.Queries.SearchProjects;

public class SearchProjectsQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public SearchProjectsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<Features.Pm.DTOs.ProjectSearchDto>>> Handle(
        SearchProjectsQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.SearchText))
        {
            return Result.Success(new List<Features.Pm.DTOs.ProjectSearchDto>());
        }

        var searchText = query.SearchText.Trim().ToLowerInvariant();

        var projects = await _dbContext.Projects
            .Where(p => p.Name.ToLower().Contains(searchText) || p.Slug.ToLower().Contains(searchText) || p.ProjectCode.ToLower().Contains(searchText))
            .OrderBy(p => p.Name)
            .Take(query.Take)
            .TagWith("SearchProjects")
            .Select(p => new Features.Pm.DTOs.ProjectSearchDto(p.Id, p.Name, p.Slug, p.Status, p.Color, p.Icon, p.ProjectCode))
            .ToListAsync(cancellationToken);

        return Result.Success(projects);
    }
}
