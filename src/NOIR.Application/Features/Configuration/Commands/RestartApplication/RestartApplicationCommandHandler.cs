namespace NOIR.Application.Features.Configuration.Commands.RestartApplication;

/// <summary>
/// Handler for restarting the application.
/// Checks environment and initiates graceful shutdown.
/// </summary>
public class RestartApplicationCommandHandler
{
    private readonly IApplicationRestartService _restartService;
    private readonly ILogger<RestartApplicationCommandHandler> _logger;

    public RestartApplicationCommandHandler(
        IApplicationRestartService restartService,
        ILogger<RestartApplicationCommandHandler> logger)
    {
        _restartService = restartService;
        _logger = logger;
    }

    public async Task<Result<RestartApplicationResult>> Handle(
        RestartApplicationCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Application restart requested by {UserId}: {Reason}",
            command.UserId, command.Reason);

        var result = await _restartService.InitiateRestartAsync(
            command.Reason,
            command.UserId!,
            cancellationToken);

        if (result.IsSuccess)
        {
            var env = _restartService.DetectEnvironment();
            var restartResult = new RestartApplicationResult(
                "Application restart initiated. Shutdown will begin in 2 seconds.",
                env.ToString(),
                DateTimeOffset.UtcNow);

            return Result<RestartApplicationResult>.Success(restartResult);
        }

        return Result.Failure<RestartApplicationResult>(result.Error);
    }
}
