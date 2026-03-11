namespace NOIR.Application.Features.ApiKeys.Commands.CreateApiKey;

/// <summary>
/// Validator for CreateApiKeyCommand.
/// </summary>
public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(200).WithMessage("API key name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Permissions)
            .NotEmpty().WithMessage("At least one permission must be assigned.")
            .Must(p => p.Count <= 100).WithMessage("Cannot assign more than 100 permissions to a single key.");

        RuleFor(x => x.ExpiresAt)
            .Must(d => !d.HasValue || d.Value > DateTimeOffset.UtcNow)
            .WithMessage("Expiration date must be in the future.");
    }
}
