using Catalog.Domain;
using Catalog.Domain.Categories;
using Shared.Validation;

namespace Catalog.Application;

/// <summary>
/// Turns <see cref="Category.AccessFor"/> into the failure a handler returns: another family's category is reported
/// as not found (its existence is not leaked), a member writing to the general catalog as forbidden.
/// </summary>
internal static class CatalogAccessRules
{
    /// <returns>The failure to return, or <c>null</c> when the actor may proceed.</returns>
    public static Exception? Check(Category? category, Guid categoryId, CatalogActor actor, CatalogAccess required)
    {
        ArgumentNullException.ThrowIfNull(actor);

        CatalogAccess access = category?.AccessFor(actor) ?? CatalogAccess.None;

        return access switch
        {
            CatalogAccess.None => NotFoundException.For("Categorie", categoryId),
            _ when access < required => new ForbiddenException("Algemene categorieën en hun spullen kan alleen de beheerder wijzigen."),
            _ => null,
        };
    }

    /// <summary>Creating needs a scope the actor may write to: the general catalog for administrators, their own family otherwise.</summary>
    public static Exception? CheckCanCreate(CatalogActor actor, bool general)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return (general, actor) switch
        {
            (true, { IsAdministrator: true }) => null,
            (true, _) => new ForbiddenException("Alleen de beheerder kan algemene categorieën aanmaken."),
            (false, { FamilyId: not null }) => null,
            _ => new ForbiddenException("Je bent geen lid van een familie, dus je kunt geen eigen categorieën aanmaken."),
        };
    }
}
