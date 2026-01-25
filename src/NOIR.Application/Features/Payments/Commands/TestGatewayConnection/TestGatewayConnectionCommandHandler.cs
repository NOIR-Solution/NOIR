namespace NOIR.Application.Features.Payments.Commands.TestGatewayConnection;

/// <summary>
/// Handler for TestGatewayConnectionCommand.
/// Tests connectivity to a payment gateway using stored credentials.
/// </summary>
public class TestGatewayConnectionCommandHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentOperationLogger _operationLogger;
    private readonly ILogger<TestGatewayConnectionCommandHandler> _logger;

    public TestGatewayConnectionCommandHandler(
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentGatewayFactory gatewayFactory,
        IPaymentOperationLogger operationLogger,
        ILogger<TestGatewayConnectionCommandHandler> logger)
    {
        _gatewayRepository = gatewayRepository;
        _gatewayFactory = gatewayFactory;
        _operationLogger = operationLogger;
        _logger = logger;
    }

    public async Task<Result<TestConnectionResultDto>> Handle(
        TestGatewayConnectionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentGatewayByIdSpec(command.GatewayId);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (gateway == null)
        {
            return Result.Failure<TestConnectionResultDto>(
                Error.NotFound("Payment gateway not found.", ErrorCodes.Payment.GatewayNotFound));
        }

        if (string.IsNullOrEmpty(gateway.EncryptedCredentials))
        {
            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: "Gateway has no credentials configured",
                ErrorCode: "NO_CREDENTIALS"));
        }

        // Start operation logging
        var operationLogId = await _operationLogger.StartOperationAsync(
            PaymentOperationType.TestConnection,
            gateway.Provider,
            cancellationToken: cancellationToken);

        await _operationLogger.SetRequestDataAsync(operationLogId, new { GatewayId = command.GatewayId, Provider = gateway.Provider }, cancellationToken);

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Get the provider with credentials initialized
            var provider = await _gatewayFactory.GetProviderWithCredentialsAsync(
                gateway.Provider, cancellationToken);

            if (provider == null)
            {
                await _operationLogger.CompleteFailedAsync(
                    operationLogId,
                    "PROVIDER_UNAVAILABLE",
                    $"Provider '{gateway.Provider}' is not available",
                    cancellationToken: cancellationToken);

                return Result.Success(new TestConnectionResultDto(
                    Success: false,
                    Message: $"Provider '{gateway.Provider}' is not available",
                    ErrorCode: "PROVIDER_UNAVAILABLE"));
            }

            // Call health check
            var healthStatus = await provider.HealthCheckAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Gateway {GatewayId} ({Provider}) health check: {Status} in {ElapsedMs}ms",
                command.GatewayId,
                gateway.Provider,
                healthStatus,
                stopwatch.ElapsedMilliseconds);

            var result = healthStatus switch
            {
                GatewayHealthStatus.Healthy => new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection successful",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds),

                GatewayHealthStatus.Degraded => new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection successful but gateway reports degraded performance",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds),

                GatewayHealthStatus.Unhealthy => new TestConnectionResultDto(
                    Success: false,
                    Message: "Gateway is unhealthy - check credentials",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                    ErrorCode: "GATEWAY_UNHEALTHY"),

                _ => new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection test completed (status unknown)",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds)
            };

            if (result.Success)
            {
                await _operationLogger.CompleteSuccessAsync(operationLogId, result, cancellationToken: cancellationToken);
            }
            else
            {
                await _operationLogger.CompleteFailedAsync(operationLogId, result.ErrorCode, result.Message, result, cancellationToken: cancellationToken);
            }

            return Result.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Gateway {GatewayId} ({Provider}) connection test failed with HTTP error",
                command.GatewayId,
                gateway.Provider);

            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                "CONNECTION_FAILED",
                ex.Message,
                exception: ex,
                cancellationToken: cancellationToken);

            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: $"Connection failed: {ex.Message}",
                ErrorCode: "CONNECTION_FAILED"));
        }
        catch (TaskCanceledException ex)
        {
            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                "TIMEOUT",
                "Connection timed out after 10 seconds",
                exception: ex,
                cancellationToken: cancellationToken);

            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: "Connection timed out after 10 seconds",
                ErrorCode: "TIMEOUT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Gateway {GatewayId} ({Provider}) connection test failed with unexpected error",
                command.GatewayId,
                gateway.Provider);

            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                "UNEXPECTED_ERROR",
                ex.Message,
                exception: ex,
                cancellationToken: cancellationToken);

            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: $"Test failed: {ex.Message}",
                ErrorCode: "UNEXPECTED_ERROR"));
        }
    }
}
