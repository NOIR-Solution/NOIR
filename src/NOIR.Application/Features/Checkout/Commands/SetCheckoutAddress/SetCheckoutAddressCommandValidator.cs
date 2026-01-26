namespace NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;

/// <summary>
/// FluentValidation validator for SetCheckoutAddressCommand.
/// </summary>
public class SetCheckoutAddressCommandValidator : AbstractValidator<SetCheckoutAddressCommand>
{
    public SetCheckoutAddressCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.");

        RuleFor(x => x.AddressType)
            .NotEmpty()
            .WithMessage("Address type is required.")
            .Must(x => x.Equals("Shipping", StringComparison.OrdinalIgnoreCase) ||
                       x.Equals("Billing", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Address type must be 'Shipping' or 'Billing'.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(100)
            .WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .MaximumLength(20)
            .WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .WithMessage("Address line 1 is required.")
            .MaximumLength(200)
            .WithMessage("Address line 1 must not exceed 200 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200)
            .WithMessage("Address line 2 must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.Ward)
            .MaximumLength(100)
            .WithMessage("Ward must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Ward));

        RuleFor(x => x.District)
            .MaximumLength(100)
            .WithMessage("District must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.District));

        RuleFor(x => x.Province)
            .MaximumLength(100)
            .WithMessage("Province must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Province));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required.")
            .MaximumLength(100)
            .WithMessage("Country must not exceed 100 characters.");
    }
}
