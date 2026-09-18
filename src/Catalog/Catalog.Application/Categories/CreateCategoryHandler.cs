using Catalog.Application.Contracts;
using Catalog.Domain.Categories;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Catalog.Application.Contracts.CategoryDto, System.Exception>;

namespace Catalog.Application.Categories;

public sealed class CreateCategoryHandler(ICategoryRepository repository)
    : IRequestHandler<CreateCategoryHandler.Command, Result>
{
    public sealed record Command(string? Name) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        CategoryName name = CategoryName.Create(request.Name);

        if (await repository.ExistsWithNameAsync(name, null, cancellationToken))
            return new DomainValidationException($"Er bestaat al een categorie met de naam '{name}'.");

        Category category = Category.Create(name);
        repository.Add(category);
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
