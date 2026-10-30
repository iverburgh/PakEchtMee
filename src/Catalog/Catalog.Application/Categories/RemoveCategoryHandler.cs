using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class RemoveCategoryHandler(ICategoryRepository repository, TimeProvider timeProvider)
    : IRequestHandler<RemoveCategoryHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Category? category = await repository.FindAsync(request.Id, cancellationToken);

        if (CatalogAccessRules.Check(category, request.Id, request.Actor, CatalogAccess.Edit) is { } denied)
            return denied;

        category!.Delete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
