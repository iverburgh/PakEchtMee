using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Shared.Validation;
using Result = CSharpFunctionalExtensions.Result<Families.Application.Contracts.FamilyCompositionDto, System.Exception>;

namespace Families.Application.Composition;

/// <summary>Reads a family's composition; also used by the composition root to start a new trip from it.</summary>
public sealed class GetFamilyCompositionHandler(IFamilyRepository repository)
    : IRequestHandler<GetFamilyCompositionHandler.Query, Result>
{
    public sealed record Query(Guid FamilyId) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Family? family = await repository.FindAsync(request.FamilyId, cancellationToken);

        if (family is null)
            return NotFoundException.For("Familie", request.FamilyId);

        return FamilyCompositionDto.From(family.Composition);
    }
}
