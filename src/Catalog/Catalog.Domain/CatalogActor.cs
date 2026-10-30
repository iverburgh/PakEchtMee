namespace Catalog.Domain;

/// <summary>
/// Who acts on the catalog: the family they belong to (if any) and whether they administer the general catalog.
/// The API decides this per request; the catalog only uses it to answer "may this actor see or change that".
/// </summary>
public sealed record CatalogActor(Guid? FamilyId, bool IsAdministrator)
{
    public static CatalogActor MemberOf(Guid familyId) => new(familyId, false);

    public static readonly CatalogActor Administrator = new(null, true);
}

/// <summary>What an actor may do with a category and the items in it.</summary>
public enum CatalogAccess
{
    /// <summary>The category is invisible: it belongs to another family.</summary>
    None = 0,

    /// <summary>The category may be browsed and used for trips, but not changed: a general category for a member.</summary>
    Read = 1,

    Edit = 2,
}
