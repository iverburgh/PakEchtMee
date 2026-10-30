using Catalog.Domain;

namespace Web.Access;

/// <summary>
/// Who is calling and on behalf of which family, decided once per request by <see cref="CurrentUserMiddleware"/>.
/// Endpoints pass these facts to the handlers, so the bounded contexts never see tokens or the Families context.
/// </summary>
internal sealed class CurrentUser
{
    public string? Email { get; private set; }

    public bool IsAdministrator { get; private set; }

    public Guid? FamilyId { get; private set; }

    public string? FamilyName { get; private set; }

    /// <summary>Only administrators and family members may use the app; everyone else gets a 403.</summary>
    public bool IsKnown => IsAdministrator || FamilyId is not null;

    /// <summary>The same facts in the catalog's own vocabulary.</summary>
    public CatalogActor CatalogActor => new(FamilyId, IsAdministrator);

    internal void Set(string email, bool isAdministrator, Guid? familyId, string? familyName)
    {
        Email = email;
        IsAdministrator = isAdministrator;
        FamilyId = familyId;
        FamilyName = familyName;
    }
}
