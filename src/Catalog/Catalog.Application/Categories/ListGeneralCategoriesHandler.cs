using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CategoryDto>, System.Exception>;

namespace Catalog.Application.Categories;

/// <summary>The general catalog as the administrator maintains it, independent of any family's switches.</summary>
public sealed class ListGeneralCategoriesHandler(ICategoryRepository repository)
    : IRequestHandler<ListGeneralCategoriesHandler.Query, Result>
{
    public sealed record Query(CatalogActor Actor, bool IncludeDeleted = false) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Actor.IsAdministrator)
            return new ForbiddenException("Alleen de beheerder beheert de algemene catalogus.");

        IReadOnlyList<Category> categories = await repository.ListGeneralAsync(request.IncludeDeleted, cancellationToken);

        return categories.Select(category => CategoryDto.From(category)).ToList();
    }
}
