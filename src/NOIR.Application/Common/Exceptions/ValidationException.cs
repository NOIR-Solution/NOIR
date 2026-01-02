namespace NOIR.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when one or more validation failures occur.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public ValidationException(string propertyName, string errorMessage) : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, [errorMessage] }
        };
    }
}
