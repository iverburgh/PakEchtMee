using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class RenameCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<RenameCategoryHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id, string? Name) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Category? category = await repository.FindAsync(request.Id, cancellationToken);

        if (CatalogAccessRules.Check(category, request.Id, request.Actor, CatalogAccess.Edit) is { } denied)
            return denied;

        CategoryName name = CategoryName.Create(request.Name);

        if (await repository.ExistsWithNameAsync(name, category!.FamilyId, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een categorie met de naam '{name}'.");

        category.Rename(name);
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
