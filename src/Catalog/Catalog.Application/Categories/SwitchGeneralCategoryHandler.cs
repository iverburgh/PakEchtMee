using Catalog.Application.Contracts;
using Catalog.Domain;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

/// <summary>Switches a general category on or off for the actor's family. Existing trips keep what they already have.</summary>
public sealed class SwitchGeneralCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<SwitchGeneralCategoryHandler.Command, Result>
{
    public sealed record Command(CatalogActor Actor, Guid Id, bool On) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Actor.FamilyId is not { } familyId)
            return new ForbiddenException("Je bent geen lid van een familie, dus er valt niets aan of uit te zetten.");

        Category? category = await repository.FindAsync(request.Id, cancellationToken);

        if (CatalogAccessRules.Check(category, request.Id, request.Actor, CatalogAccess.Read) is { } denied)
            return denied;

        if (category!.IsDeleted)
            return NotFoundException.For("Categorie", request.Id);

        if (!category.IsGeneral)
            return new DomainValidationException($"Alleen algemene categorieën kun je aan- of uitzetten; '{category.Name}' is een eigen categorie.");

        await repository.SwitchAsync(familyId, category, request.On, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category, isSwitchedOn: request.On);
    }
}
