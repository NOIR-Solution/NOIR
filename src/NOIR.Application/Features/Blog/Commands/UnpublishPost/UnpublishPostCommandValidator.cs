namespace NOIR.Application.Features.Blog.Commands.UnpublishPost;

/// <summary>
/// Validator for UnpublishPostCommand.
/// </summary>
public sealed class UnpublishPostCommandValidator : AbstractValidator<UnpublishPostCommand>
{
    public UnpublishPostCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Post ID is required.");
    }
}
