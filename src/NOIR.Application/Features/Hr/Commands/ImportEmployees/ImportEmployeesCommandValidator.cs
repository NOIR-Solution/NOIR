namespace NOIR.Application.Features.Hr.Commands.ImportEmployees;

public class ImportEmployeesCommandValidator : AbstractValidator<ImportEmployeesCommand>
{
    public ImportEmployeesCommandValidator()
    {
        RuleFor(x => x.FileData)
            .NotEmpty()
            .WithMessage("File data is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.")
            .Must(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only CSV files are supported.");
    }
}
