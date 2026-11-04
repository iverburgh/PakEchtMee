using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyCompositionDto, System.Exception>;

namespace Families.Application.Composition;

/// <summary>Lets a member change their own family's size. The caller passes the member's family, so no admin check applies.</summary>
public sealed class ChangeFamilyCompositionHandler(IFamilyRepository repository)
    : IRequestHandler<ChangeFamilyCompositionHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, int Adults, int Children) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Family? family = await repository.FindAsync(request.FamilyId, cancellationToken);

        if (family is null)
            return NotFoundException.For("Familie", request.FamilyId);

        family.ChangeComposition(FamilyComposition.Create(request.Adults, request.Children));
        await repository.SaveChangesAsync(cancellationToken);

        return FamilyCompositionDto.From(family.Composition);
    }
}
