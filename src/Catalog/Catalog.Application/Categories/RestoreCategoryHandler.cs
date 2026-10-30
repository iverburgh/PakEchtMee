using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class RestoreCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<RestoreCategoryHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Category? category = await repository.FindAsync(request.Id, cancellationToken);

        if (CatalogAccessRules.Check(category, request.Id, request.Actor, CatalogAccess.Edit) is { } denied)
            return denied;

        if (await repository.ExistsWithNameAsync(category!.Name, category.FamilyId, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een categorie met de naam '{category.Name}', dus deze kan niet onder die naam worden teruggezet.");

        category.Restore();
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
