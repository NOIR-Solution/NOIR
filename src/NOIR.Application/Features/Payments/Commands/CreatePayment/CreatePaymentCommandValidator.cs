namespace NOIR.Application.Features.Payments.Commands.CreatePayment;

/// <summary>
/// Validator for CreatePaymentCommand.
/// </summary>
public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    private const int MaxCurrencyLength = 3;
    private const int MaxProviderLength = 50;
    private const int MaxUrlLength = 2000;
    private const int MaxIdempotencyKeyLength = 100;

    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(MaxCurrencyLength).WithMessage($"Currency cannot exceed {MaxCurrencyLength} characters.")
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be a valid 3-letter ISO code.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .MaximumLength(MaxProviderLength).WithMessage($"Provider cannot exceed {MaxProviderLength} characters.");

        RuleFor(x => x.ReturnUrl)
            .MaximumLength(MaxUrlLength).WithMessage($"Return URL cannot exceed {MaxUrlLength} characters.")
            .When(x => x.ReturnUrl is not null);

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(MaxIdempotencyKeyLength).WithMessage($"Idempotency key cannot exceed {MaxIdempotencyKeyLength} characters.")
            .When(x => x.IdempotencyKey is not null);
    }
}
