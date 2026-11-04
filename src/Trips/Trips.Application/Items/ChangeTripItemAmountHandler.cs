using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripItemDto, System.Exception>;

namespace Trips.Application.Items;

/// <summary>Changes or removes one trip item's quantity rule while it is still to do; the catalog item is left alone.</summary>
public sealed class ChangeTripItemAmountHandler(ITripRepository repository)
    : IRequestHandler<ChangeTripItemAmountHandler.Command, Result>
{
    public sealed record Command(Guid FamilyId, Guid TripId, Guid ItemId, int? Quantity, string? Unit, string? Basis) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, request.FamilyId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        TripItem item = trip.ChangeItemAmount(request.ItemId, Amount.Create(request.Quantity, request.Unit, QuantityBases.Parse(request.Basis)));
        await repository.SaveChangesAsync(cancellationToken);

        return TripItemDto.From(item);
    }
}
