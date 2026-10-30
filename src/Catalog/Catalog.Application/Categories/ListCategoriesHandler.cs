using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CategoryDto>, System.Exception>;

namespace Catalog.Application.Categories;

/// <summary>
/// The catalog as the actor's family sees it: its own categories plus the general ones it has switched on. This is
/// also what a trip may be generated from, so a switched-off or foreign category can never end up on a trip.
/// </summary>
public sealed class ListCategoriesHandler(ICategoryRepository repository)
    : IRequestHandler<ListCategoriesHandler.Query, Result>
{
    /// <summary><c>IncludeSwitchedOff</c> also lists switched-off general categories, for the screen that switches them back on.</summary>
    public sealed record Query(CatalogActor Actor, bool IncludeDeleted = false, bool IncludeSwitchedOff = false) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<Category> categories = await repository.ListVisibleAsync(request.Actor.FamilyId, request.IncludeDeleted, cancellationToken);
        IReadOnlySet<Guid> switchedOff = request.Actor.FamilyId is { } familyId
            ? await repository.ListSwitchedOffAsync(familyId, cancellationToken)
            : new HashSet<Guid>();

        return categories
            .Where(category => category.AccessFor(request.Actor) is not CatalogAccess.None)
            .Select(category => CategoryDto.From(category, isSwitchedOn: !switchedOff.Contains(category.Id)))
            .Where(category => category.IsSwitchedOn || request.IncludeSwitchedOff)
            .ToList();
    }
}
