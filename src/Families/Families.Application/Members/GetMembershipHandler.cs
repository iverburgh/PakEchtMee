using CSharpFunctionalExtensions;
using Families.Application.Contracts;
using Families.Domain;
using MediatR;
using Result = CSharpFunctionalExtensions.Result<CSharpFunctionalExtensions.Maybe<Families.Application.Contracts.MembershipDto>, System.Exception>;

namespace Families.Application.Members;

/// <summary>Finds the family of an account. "No family" is a normal answer (an administrator, or a stranger), not a failure.</summary>
public sealed class GetMembershipHandler(IFamilyRepository repository)
    : IRequestHandler<GetMembershipHandler.Query, Result>
{
    public sealed record Query(string? Email) : IRequest<Result>;

    public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Family? family = await repository.FindByMemberAsync(EmailAddress.Create(request.Email), cancellationToken);

        return family is null
            ? Maybe<MembershipDto>.None
            : Maybe.From(new MembershipDto(family.Id, family.Name.Value));
    }
}
