using MediatR;
using Shared.Validation;
using Trips.Application.Contracts;
using Trips.Domain;
using Result = CSharpFunctionalExtensions.Result<Trips.Application.Contracts.TripDetailsDto, System.Exception>;

namespace Trips.Application.Trips;

/// <summary>
/// Adds categories and their items to a trip. The caller passes the catalog snapshots, so this context
/// never reads the catalog itself.
/// </summary>
public sealed class SelectTripCategoriesHandler(ITripRepository repository)
    : IRequestHandler<SelectTripCategoriesHandler.Command, Result>
{
    public sealed record Command(Guid TripId, IReadOnlyList<CategorySelection> Categories) : IRequest<Result>;

    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trip? trip = await repository.FindAsync(request.TripId, cancellationToken);

        if (trip is null)
            return NotFoundException.For("Uitje", request.TripId);

        if (request.Categories.Count is 0)
            return new DomainValidationException("Kies minstens één categorie.");

        trip.SelectCategories([.. request.Categories]);
        await repository.SaveChangesAsync(cancellationToken);

        return TripDetailsDto.From(trip);
    }
}
