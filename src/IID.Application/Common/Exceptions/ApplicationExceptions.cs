namespace IID.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity or resource is not found.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"{name} '{key}' was not found.")
    {
    }
}

/// <summary>
/// Thrown when an operation conflicts with the current state of a resource.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when validation rules fail for an incoming request or command.
/// </summary>
public sealed class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        };
    }
}
