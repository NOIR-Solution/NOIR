namespace NOIR.Application.Features.Notifications.Queries.GetPreferences;

using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Domain.Enums;
using NOIR.Domain.Interfaces;

/// <summary>
/// Wolverine handler for getting user notification preferences.
/// Returns preferences for all categories, creating defaults if not found.
/// </summary>
public class GetPreferencesQueryHandler
{
    private readonly IRepository<NotificationPreference, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetPreferencesQueryHandler(
        IRepository<NotificationPreference, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<IEnumerable<NotificationPreferenceDto>>> Handle(
        GetPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<IEnumerable<NotificationPreferenceDto>>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        var spec = new UserPreferencesSpec(_currentUser.UserId);
        var preferences = await _repository.ListAsync(spec, cancellationToken);

        // If no preferences exist, create defaults
        if (!preferences.Any())
        {
            var defaults = NotificationPreference.CreateDefaults(
                _currentUser.UserId,
                _currentUser.TenantId);

            foreach (var pref in defaults)
            {
                await _repository.AddAsync(pref, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            preferences = await _repository.ListAsync(spec, cancellationToken);
        }

        var dtos = preferences.Select(p => new NotificationPreferenceDto(
            p.Id,
            p.Category,
            GetCategoryDisplayName(p.Category),
            p.InAppEnabled,
            p.EmailFrequency));

        return Result.Success(dtos);
    }

    private static string GetCategoryDisplayName(NotificationCategory category) => category switch
    {
        NotificationCategory.System => "System",
        NotificationCategory.UserAction => "User Actions",
        NotificationCategory.Workflow => "Workflow",
        NotificationCategory.Security => "Security",
        NotificationCategory.Integration => "Integration",
        _ => category.ToString()
    };
}
