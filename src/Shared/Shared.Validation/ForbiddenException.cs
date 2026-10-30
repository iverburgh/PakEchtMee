namespace Shared.Validation;

/// <summary>
/// Signals that the caller is known but may not do this, such as a family member changing the general catalog;
/// mapped to a 403 response at the API boundary.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
        : base("Je hebt geen rechten voor deze actie.")
    {
    }

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
