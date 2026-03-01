namespace NOIR.Application.Features.Hr.Commands.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(100).WithMessage("Tag name cannot exceed 100 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid tag category.");

        RuleFor(x => x.Color)
            .MaximumLength(9).WithMessage("Color cannot exceed 9 characters.")
            .When(x => x.Color is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
