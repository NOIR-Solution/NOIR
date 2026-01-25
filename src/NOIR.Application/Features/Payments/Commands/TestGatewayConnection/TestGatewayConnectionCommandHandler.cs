namespace NOIR.Application.Features.Payments.Commands.TestGatewayConnection;

/// <summary>
/// Handler for TestGatewayConnectionCommand.
/// Tests connectivity to a payment gateway using stored credentials.
/// </summary>
public class TestGatewayConnectionCommandHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly ILogger<TestGatewayConnectionCommandHandler> _logger;

    public TestGatewayConnectionCommandHandler(
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentGatewayFactory gatewayFactory,
        ILogger<TestGatewayConnectionCommandHandler> logger)
    {
        _gatewayRepository = gatewayRepository;
        _gatewayFactory = gatewayFactory;
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

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Get the provider with credentials initialized
            var provider = await _gatewayFactory.GetProviderWithCredentialsAsync(
                gateway.Provider, cancellationToken);

            if (provider == null)
            {
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

            return healthStatus switch
            {
                GatewayHealthStatus.Healthy => Result.Success(new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection successful",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds)),

                GatewayHealthStatus.Degraded => Result.Success(new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection successful but gateway reports degraded performance",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds)),

                GatewayHealthStatus.Unhealthy => Result.Success(new TestConnectionResultDto(
                    Success: false,
                    Message: "Gateway is unhealthy - check credentials",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                    ErrorCode: "GATEWAY_UNHEALTHY")),

                _ => Result.Success(new TestConnectionResultDto(
                    Success: true,
                    Message: "Connection test completed (status unknown)",
                    ResponseTimeMs: stopwatch.ElapsedMilliseconds))
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Gateway {GatewayId} ({Provider}) connection test failed with HTTP error",
                command.GatewayId,
                gateway.Provider);

            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: $"Connection failed: {ex.Message}",
                ErrorCode: "CONNECTION_FAILED"));
        }
        catch (TaskCanceledException)
        {
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

            return Result.Success(new TestConnectionResultDto(
                Success: false,
                Message: $"Test failed: {ex.Message}",
                ErrorCode: "UNEXPECTED_ERROR"));
        }
    }
}
