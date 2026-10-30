using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyDto, System.Exception>;

namespace Families.Application.Families;

public sealed class RenameFamilyHandler(IFamilyRepository repository)
    : IRequestHandler<RenameFamilyHandler.Command, Result>
{
    public sealed record Command(Guid Id, string? Name) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Family? family = await repository.FindAsync(request.Id, cancellationToken);

        if (family is null)
            return NotFoundException.For("Familie", request.Id);

        FamilyName name = FamilyName.Create(request.Name);

        if (await repository.ExistsWithNameAsync(name, request.Id, cancellationToken))
            return new DomainValidationException($"Er bestaat al een familie met de naam '{name}'.");

        family.Rename(name);
        await repository.SaveChangesAsync(cancellationToken);

        return FamilyDto.From(family);
    }
}
