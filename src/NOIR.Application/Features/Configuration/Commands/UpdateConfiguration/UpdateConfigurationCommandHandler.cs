namespace NOIR.Application.Features.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Handler for updating configuration sections.
/// Integrates with audit logging to track configuration changes.
/// </summary>
public class UpdateConfigurationCommandHandler
{
    private readonly IConfigurationManagementService _service;
    private readonly ILogger<UpdateConfigurationCommandHandler> _logger;

    public UpdateConfigurationCommandHandler(
        IConfigurationManagementService service,
        ILogger<UpdateConfigurationCommandHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<Result<ConfigurationBackupDto>> Handle(
        UpdateConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating configuration section '{SectionName}' requested by {UserId}",
            command.SectionName, command.UserId);

        var result = await _service.UpdateSectionAsync(
            command.SectionName,
            command.NewValueJson,
            command.UserId!,
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Configuration section '{SectionName}' updated successfully. Backup: {BackupId}",
                command.SectionName, result.Value.Id);
        }

        return result;
    }
}
