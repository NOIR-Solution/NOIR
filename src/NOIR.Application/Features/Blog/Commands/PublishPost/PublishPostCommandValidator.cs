namespace NOIR.Application.Features.Blog.Commands.PublishPost;

/// <summary>
/// Validator for PublishPostCommand.
/// </summary>
public sealed class PublishPostCommandValidator : AbstractValidator<PublishPostCommand>
{
    public PublishPostCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Post ID is required.");
    }
}
