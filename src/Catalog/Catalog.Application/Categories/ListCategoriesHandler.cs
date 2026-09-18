using Catalog.Application.Contracts;
using Catalog.Domain.Categories;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Catalog.Application.Contracts.CategoryDto>, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class ListCategoriesHandler(ICategoryRepository repository)
    : IRequestHandler<ListCategoriesHandler.Query, Result>
{
    public sealed record Query(bool IncludeDeleted = false) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<Category> categories = await repository.ListAsync(request.IncludeDeleted, cancellationToken);

        return categories.Select(CategoryDto.From).ToList();
    }
}
