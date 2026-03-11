namespace NOIR.Application.Features.ApiKeys.Commands.UpdateApiKey;

/// <summary>
/// Validator for UpdateApiKeyCommand.
/// </summary>
public sealed class UpdateApiKeyCommandValidator : AbstractValidator<UpdateApiKeyCommand>
{
    public UpdateApiKeyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("API key ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(200).WithMessage("API key name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Permissions)
            .NotEmpty().WithMessage("At least one permission must be assigned.")
            .Must(p => p.Count <= 100).WithMessage("Cannot assign more than 100 permissions to a single key.");
    }
}
