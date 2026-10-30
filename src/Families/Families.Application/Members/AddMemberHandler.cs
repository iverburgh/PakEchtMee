using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyDto, System.Exception>;

namespace Families.Application.Members;

/// <summary>Adds an account to a family. An account belongs to at most one family, so the current one is named on a clash.</summary>
public sealed class AddMemberHandler(IFamilyRepository repository)
    : IRequestHandler<AddMemberHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, string? Email) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        EmailAddress email = EmailAddress.Create(request.Email);
        Family? family = await repository.FindAsync(request.FamilyId, cancellationToken);

        if (family is null)
            return NotFoundException.For("Familie", request.FamilyId);

        Family? current = await repository.FindByMemberAsync(email, cancellationToken);

        if (current is not null)
            return new DomainValidationException($"{email} is al lid van {current.Name}; haal het daar eerst weg.");

        family.AddMember(email);
        await repository.SaveChangesAsync(cancellationToken);

        return FamilyDto.From(family);
    }
}
