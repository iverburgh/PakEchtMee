namespace Shared.Validation;

/// <summary>Signals that a domain rule or input constraint was violated; mapped to a 400 response at the API boundary.</summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException()
        : base("Er is een domeinregel geschonden.")
    {
    }

    public DomainValidationException(string message)
        : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
