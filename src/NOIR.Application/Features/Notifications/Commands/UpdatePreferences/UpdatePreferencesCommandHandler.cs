namespace NOIR.Application.Features.Notifications.Commands.UpdatePreferences;

using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Domain.Interfaces;

/// <summary>
/// Wolverine handler for updating notification preferences.
/// </summary>
public class UpdatePreferencesCommandHandler
{
    private readonly IRepository<NotificationPreference, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public UpdatePreferencesCommandHandler(
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
        UpdatePreferencesCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<IEnumerable<NotificationPreferenceDto>>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        var spec = new UserPreferencesSpec(_currentUser.UserId, asTracking: true);
        var existingPreferences = await _repository.ListAsync(spec, cancellationToken);
        var preferenceDict = existingPreferences.ToDictionary(p => p.Category);

        var updatedPreferences = new List<NotificationPreference>();

        foreach (var update in command.Preferences)
        {
            if (preferenceDict.TryGetValue(update.Category, out var existing))
            {
                existing.Update(update.InAppEnabled, update.EmailFrequency);
                updatedPreferences.Add(existing);
            }
            else
            {
                // Create new preference if it doesn't exist
                var newPref = NotificationPreference.Create(
                    _currentUser.UserId,
                    update.Category,
                    update.InAppEnabled,
                    update.EmailFrequency,
                    _currentUser.TenantId);
                await _repository.AddAsync(newPref, cancellationToken);
                updatedPreferences.Add(newPref);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = updatedPreferences.Select(p => new NotificationPreferenceDto(
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
