namespace NOIR.Application.Features.Customers.Commands.UpdateCustomerSegment;

/// <summary>
/// Validator for UpdateCustomerSegmentCommand.
/// </summary>
public sealed class UpdateCustomerSegmentCommandValidator : AbstractValidator<UpdateCustomerSegmentCommand>
{
    public UpdateCustomerSegmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Segment)
            .IsInEnum().WithMessage("Invalid customer segment.");
    }
}
