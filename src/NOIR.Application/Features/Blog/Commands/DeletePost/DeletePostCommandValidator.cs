namespace NOIR.Application.Features.Blog.Commands.DeletePost;

/// <summary>
/// Validator for DeletePostCommand.
/// </summary>
public sealed class DeletePostCommandValidator : AbstractValidator<DeletePostCommand>
{
    public DeletePostCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Post ID is required.");
    }
}
