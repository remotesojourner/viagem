using Viagem.Data.Models;
using Viagem.Data.Repositories.Interfaces;
using Viagem.Services.Interfaces;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public class TransportationService(ITransportationRepository repo, IExpenseRepository expenseRepo) : ITransportationService
{
    public async Task<List<TransportationViewModel>> GetTripTransportationsAsync(int tripId)
    {
        var items = await repo.GetByTripAsync(tripId);
        return items.Select(ToViewModel).ToList();
    }

    public async Task<TransportationViewModel?> GetTransportationAsync(int id)
    {
        var item = await repo.GetByIdAsync(id);
        return item == null ? null : ToViewModel(item);
    }

    public async Task<TransportationViewModel> CreateAsync(CreateTransportationRequest request)
    {
        var entity = new Transportation
        {
            TripId = request.TripId,
            Type = request.Type,
            Origin = request.Origin,
            OriginCity = request.OriginCity,
            Destination = request.Destination,
            DestinationCity = request.DestinationCity,
            Provider = request.Provider,
            ConfirmationCode = request.ConfirmationCode,
            FlightNumber = request.FlightNumber,
            AssignedSeats = request.AssignedSeats,
            Notes = request.Notes,
            Link = request.Link,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            DepartureTimezone = request.DepartureTimezone,
            ArrivalTimezone = request.ArrivalTimezone,
            RentalCompany = request.RentalCompany,
            PickupLocation = request.PickupLocation,
            DropOffLocation = request.DropOffLocation,
            SpotNumber = request.SpotNumber,
            ParkingAddress = request.ParkingAddress,
            OriginPlaceId = request.OriginPlaceId,
            DestinationPlaceId = request.DestinationPlaceId,
            Travellers = request.TravellerProfileIds
                .Select(id => new TransportationTraveller { TravellerProfileId = id })
                .ToList()
        };

        var created = await repo.CreateAsync(entity);

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var expenseId = await expenseRepo.UpsertLinkedAsync(
                created.TripId, "Transportation", created.Id,
                $"{created.Type} – {created.Origin ?? ""} → {created.Destination ?? ""}",
                ExpenseCategory.Transport,
                request.CostAmount.Value, request.CostCurrency,
                created.DepartureTime, null);
            created.ExpenseId = expenseId;
            await repo.UpdateAsync(created);
        }

        var full = await repo.GetByIdAsync(created.Id);
        return ToViewModel(full!);
    }

    public async Task<TransportationViewModel?> UpdateAsync(UpdateTransportationRequest request)
    {
        var existing = await repo.GetByIdAsync(request.Id);
        if (existing == null) return null;

        var stub = new Transportation
        {
            Id = existing.Id,
            TripId = existing.TripId,
            Type = request.Type,
            Origin = request.Origin,
            OriginCity = request.OriginCity,
            Destination = request.Destination,
            DestinationCity = request.DestinationCity,
            Provider = request.Provider,
            ConfirmationCode = request.ConfirmationCode,
            FlightNumber = request.FlightNumber,
            AssignedSeats = request.AssignedSeats,
            Notes = request.Notes,
            Link = request.Link,
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
            DepartureTimezone = request.DepartureTimezone,
            ArrivalTimezone = request.ArrivalTimezone,
            RentalCompany = request.RentalCompany,
            PickupLocation = request.PickupLocation,
            DropOffLocation = request.DropOffLocation,
            SpotNumber = request.SpotNumber,
            ParkingAddress = request.ParkingAddress,
            OriginPlaceId = request.OriginPlaceId,
            DestinationPlaceId = request.DestinationPlaceId,
            ExpenseId = existing.ExpenseId,
            CreatedAt = existing.CreatedAt
        };

        await repo.UpdateAsync(stub);

        // Sync linked expense from request values
        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var expenseId = await expenseRepo.UpsertLinkedAsync(
                existing.TripId, "Transportation", existing.Id,
                $"{request.Type} – {request.Origin ?? ""} → {request.Destination ?? ""}",
                ExpenseCategory.Transport,
                request.CostAmount.Value, request.CostCurrency,
                request.DepartureTime, existing.ExpenseId);
            if (expenseId != existing.ExpenseId)
            {
                stub.ExpenseId = expenseId;
                await repo.UpdateAsync(stub);
            }
        }
        else if (existing.ExpenseId.HasValue)
        {
            await expenseRepo.DeleteLinkedAsync(existing.ExpenseId);
            stub.ExpenseId = null;
            await repo.UpdateAsync(stub);
        }

        var full = await repo.GetByIdAsync(request.Id);
        return full == null ? null : ToViewModel(full);
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await repo.GetByIdAsync(id);
        if (existing?.ExpenseId.HasValue == true)
            await expenseRepo.DeleteLinkedAsync(existing.ExpenseId);
        await repo.DeleteAsync(id);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static TravellerProfileSummaryViewModel ToTravellerSummary(TravellerProfile tp)
        => new(tp.Id, tp.LegalName, tp.Email);

    private static TransportationViewModel ToViewModel(Transportation t)
        => new(t.Id, t.TripId, t.Type,
            t.Origin, t.OriginCity, t.Destination, t.DestinationCity,
            t.Provider, t.ConfirmationCode, t.FlightNumber, t.AssignedSeats,
            t.Notes, t.Link,
            t.DepartureTime, t.ArrivalTime, t.DepartureTimezone, t.ArrivalTimezone,
            t.CostAmount, t.CostCurrency,
            t.RentalCompany, t.PickupLocation, t.DropOffLocation,
            t.SpotNumber, t.ParkingAddress,
            t.OriginPlaceId, t.OriginPlace?.Name,
            t.DestinationPlaceId, t.DestinationPlace?.Name,
            t.Travellers
                .Where(tt => tt.TravellerProfile != null)
                .Select(tt => ToTravellerSummary(tt.TravellerProfile!))
                .ToList());
}
