namespace Web.Access;

/// <summary>
/// Configuration section <c>Access</c>. Administrators come from configuration rather than the database, so no data
/// change can ever lock them out.
/// </summary>
internal sealed class AccessOptions
{
    public const string SectionName = "Access";

    public IList<string> Administrators { get; } = [];

    public bool IsAdministrator(string email)
        => Administrators.Any(administrator => string.Equals(administrator.Trim(), email, StringComparison.OrdinalIgnoreCase));
}
