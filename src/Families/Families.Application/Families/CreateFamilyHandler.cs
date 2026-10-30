using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyDto, System.Exception>;

namespace Families.Application.Families;

public sealed class CreateFamilyHandler(IFamilyRepository repository)
    : IRequestHandler<CreateFamilyHandler.Command, Result>
{
    public sealed record Command(string? Name) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        FamilyName name = FamilyName.Create(request.Name);

        if (await repository.ExistsWithNameAsync(name, null, cancellationToken))
            return new DomainValidationException($"Er bestaat al een familie met de naam '{name}'.");

        Family family = Family.Create(name);
        repository.Add(family);
        await repository.SaveChangesAsync(cancellationToken);

        return FamilyDto.From(family);
    }
}
