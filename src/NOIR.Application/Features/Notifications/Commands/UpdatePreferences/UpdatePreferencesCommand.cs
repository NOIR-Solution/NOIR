namespace NOIR.Application.Features.Notifications.Commands.UpdatePreferences;

using NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Command to update notification preferences for the current user.
/// </summary>
/// <param name="Preferences">List of preference updates.</param>
public sealed record UpdatePreferencesCommand(IEnumerable<UpdatePreferenceRequest> Preferences);
