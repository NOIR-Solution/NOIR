namespace NOIR.Application.Features.Blog.Commands.DeleteTag;

/// <summary>
/// Validator for DeleteTagCommand.
/// </summary>
public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tag ID is required.");
    }
}
