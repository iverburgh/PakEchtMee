namespace Shared.Validation;

/// <summary>Signals that a requested entity does not exist; mapped to a 404 response at the API boundary.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : base("Het gevraagde item is niet gevonden.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static NotFoundException For(string resource, Guid id) => new($"{resource} '{id}' is niet gevonden.");
}
