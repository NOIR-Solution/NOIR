namespace NOIR.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to access a forbidden resource.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Access to this resource is forbidden.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
