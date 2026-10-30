using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class CreateCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<CreateCategoryHandler.Command, Result>
{
    /// <summary><c>General</c> creates a general category (administrators only) instead of one for the actor's family.</summary>
    public sealed record Command(CatalogActor Actor, string? Name, bool General = false) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (CatalogAccessRules.CheckCanCreate(request.Actor, request.General) is { } denied)
            return denied;

        CategoryName name = CategoryName.Create(request.Name);
        Guid? familyId = request.General ? null : request.Actor.FamilyId;

        if (await repository.ExistsWithNameAsync(name, familyId, null, cancellationToken))
            return new DomainValidationException($"Er bestaat al een categorie met de naam '{name}'.");

        Category category = familyId is { } family ? Category.CreateForFamily(family, name) : Category.Create(name);
        repository.Add(category);
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
