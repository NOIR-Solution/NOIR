namespace NOIR.Application.Features.Payments.Commands.TestGatewayConnection;

/// <summary>
/// Command to test connectivity to a payment gateway.
/// </summary>
public sealed record TestGatewayConnectionCommand(Guid GatewayId);
