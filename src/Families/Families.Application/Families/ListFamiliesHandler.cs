using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<System.Collections.Generic.IReadOnlyList<Families.Application.Contracts.FamilyDto>, System.Exception>;

namespace Families.Application.Families;

public sealed class ListFamiliesHandler(IFamilyRepository repository)
    : IRequestHandler<ListFamiliesHandler.Query, Result>
{
    public sealed record Query : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Family> families = await repository.ListAsync(cancellationToken);

        return families.Select(FamilyDto.From).ToList();
    }
}
