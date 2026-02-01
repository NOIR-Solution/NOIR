namespace NOIR.Application.Features.Payments.Commands.TestGatewayConnection;

/// <summary>
/// Validator for TestGatewayConnectionCommand.
/// </summary>
public sealed class TestGatewayConnectionCommandValidator : AbstractValidator<TestGatewayConnectionCommand>
{
    public TestGatewayConnectionCommandValidator()
    {
        RuleFor(x => x.GatewayId)
            .NotEmpty().WithMessage("Gateway ID is required.");
    }
}
