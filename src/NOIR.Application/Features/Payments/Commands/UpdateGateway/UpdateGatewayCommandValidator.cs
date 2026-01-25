namespace NOIR.Application.Features.Payments.Commands.UpdateGateway;

/// <summary>
/// Validator for UpdateGatewayCommand.
/// </summary>
public sealed class UpdateGatewayCommandValidator : AbstractValidator<UpdateGatewayCommand>
{
    private const int MaxDisplayNameLength = 200;

    public UpdateGatewayCommandValidator()
    {
        RuleFor(x => x.GatewayId)
            .NotEmpty().WithMessage("Gateway ID is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength).WithMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Environment)
            .IsInEnum().WithMessage("Invalid gateway environment.")
            .When(x => x.Environment.HasValue);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.")
            .When(x => x.SortOrder.HasValue);
    }
}
