using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyDto, System.Exception>;

namespace Families.Application.Members;

public sealed class RemoveMemberHandler(IFamilyRepository repository)
    : IRequestHandler<RemoveMemberHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, string? Email) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        EmailAddress email = EmailAddress.Create(request.Email);
        Family? family = await repository.FindAsync(request.FamilyId, cancellationToken);

        if (family is null)
            return NotFoundException.For("Familie", request.FamilyId);

        family.RemoveMember(email);
        await repository.SaveChangesAsync(cancellationToken);

        return FamilyDto.From(family);
    }
}
