using Viagem.Data.Models;
using Viagem.Services.ViewModels;

namespace Viagem.Services;

public partial class TripService
{
    // ── Transportations ────────────────────────────────────────────────────────
    
    public async Task<TransportationViewModel> AddTransportationAsync(string userId, CreateTransportationRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) throw new InvalidOperationException("Trip not found or access denied.");

        var entity = new Transportation
        {
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
            TravellerProfileIds = request.TravellerProfileIds.ToList()
        };

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = new Expense
            {
                Name = $"{entity.Type} – {entity.Origin ?? ""} → {entity.Destination ?? ""}",
                Category = ExpenseCategory.Transport,
                Amount = request.CostAmount,
                Currency = request.CostCurrency,
                OccurredOn = request.DepartureTime,
                SourceType = "Transportation",
                SourceId = entity.Id,
                CreatedById = userId
            };
            trip.Expenses.Add(exp);
            entity.ExpenseId = exp.Id;
        }

        trip.Transportations.Add(entity);
        await repo.UpdateAsync(trip);
        return ToTransportationViewModel(entity, trip);
    }

    public async Task<TransportationViewModel?> UpdateTransportationAsync(string userId, UpdateTransportationRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) return null;

        var entity = trip.Transportations.FirstOrDefault(t => t.Id == request.Id);
        if (entity == null) return null;

        entity.Type = request.Type;
        entity.Origin = request.Origin;
        entity.OriginCity = request.OriginCity;
        entity.Destination = request.Destination;
        entity.DestinationCity = request.DestinationCity;
        entity.Provider = request.Provider;
        entity.ConfirmationCode = request.ConfirmationCode;
        entity.FlightNumber = request.FlightNumber;
        entity.AssignedSeats = request.AssignedSeats;
        entity.Notes = request.Notes;
        entity.Link = request.Link;
        entity.DepartureTime = request.DepartureTime;
        entity.ArrivalTime = request.ArrivalTime;
        entity.DepartureTimezone = request.DepartureTimezone;
        entity.ArrivalTimezone = request.ArrivalTimezone;
        entity.RentalCompany = request.RentalCompany;
        entity.PickupLocation = request.PickupLocation;
        entity.DropOffLocation = request.DropOffLocation;
        entity.SpotNumber = request.SpotNumber;
        entity.ParkingAddress = request.ParkingAddress;
        entity.OriginPlaceId = request.OriginPlaceId;
        entity.DestinationPlaceId = request.DestinationPlaceId;
        entity.TravellerProfileIds = request.TravellerProfileIds.ToList();
        entity.UpdatedAt = DateTime.UtcNow;

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp == null)
            {
                exp = new Expense
                {
                    Name = $"{entity.Type} – {entity.Origin ?? ""} → {entity.Destination ?? ""}",
                    Category = ExpenseCategory.Transport,
                    SourceType = "Transportation",
                    SourceId = entity.Id,
                    CreatedById = userId
                };
                trip.Expenses.Add(exp);
                entity.ExpenseId = exp.Id;
            }
            exp.Amount = request.CostAmount;
            exp.Currency = request.CostCurrency;
            exp.OccurredOn = request.DepartureTime;
        }
        else if (entity.ExpenseId.HasValue)
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp != null) trip.Expenses.Remove(exp);
            entity.ExpenseId = null;
        }

        await repo.UpdateAsync(trip);
        return ToTransportationViewModel(entity, trip);
    }

    public async Task RemoveTransportationAsync(string userId, int tripId, Guid id)
    {
        var trip = await repo.GetByIdAsync(tripId, userId);
        if (trip == null) return;
        
        var entity = trip.Transportations.FirstOrDefault(t => t.Id == id);
        if (entity != null)
        {
            if (entity.ExpenseId.HasValue)
            {
                var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
                if (exp != null) trip.Expenses.Remove(exp);
            }
            trip.Transportations.Remove(entity);
            await repo.UpdateAsync(trip);
        }
    }

    private static TransportationViewModel ToTransportationViewModel(Transportation t, Trip trip)
    {
        var expense = t.ExpenseId.HasValue ? trip.Expenses.FirstOrDefault(e => e.Id == t.ExpenseId) : null;
        return new TransportationViewModel(
            t.Id, trip.Id, t.Type,
            t.Origin, t.OriginCity, t.Destination, t.DestinationCity,
            t.Provider, t.ConfirmationCode, t.FlightNumber, t.AssignedSeats,
            t.Notes, t.Link,
            t.DepartureTime, t.ArrivalTime, t.DepartureTimezone, t.ArrivalTimezone,
            expense?.Amount, expense?.Currency,
            t.RentalCompany, t.PickupLocation, t.DropOffLocation,
            t.SpotNumber, t.ParkingAddress,
            t.OriginPlaceId, null, // Place mappings handled elsewhere or fetched by UI if needed
            t.DestinationPlaceId, null,
            trip.Travellers.Where(tt => t.TravellerProfileIds.Contains(tt.TravellerProfileId) && tt.TravellerProfile != null)
                .Select(tt => new TravellerProfileSummaryViewModel(tt.TravellerProfile!.Id, tt.TravellerProfile.LegalName, tt.TravellerProfile.Email))
                .ToList());
    }

    // ── Lodgings ──────────────────────────────────────────────────────────────
    
    public async Task<LodgingViewModel> AddLodgingAsync(string userId, CreateLodgingRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) throw new InvalidOperationException("Trip not found");

        var entity = new Lodging
        {
            Type = request.Type,
            Name = request.Name,
            Address = request.Address,
            ConfirmationCode = request.ConfirmationCode,
            Notes = request.Notes,
            Link = request.Link,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            PlaceId = request.PlaceId,
            TravellerProfileIds = request.TravellerProfileIds.ToList()
        };

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = new Expense
            {
                Name = $"Lodging: {entity.Name}",
                Category = ExpenseCategory.Accommodation,
                Amount = request.CostAmount,
                Currency = request.CostCurrency,
                OccurredOn = request.StartDate,
                SourceType = "Lodging",
                SourceId = entity.Id,
                CreatedById = userId
            };
            trip.Expenses.Add(exp);
            entity.ExpenseId = exp.Id;
        }

        trip.Lodgings.Add(entity);
        await repo.UpdateAsync(trip);
        return ToLodgingViewModel(entity, trip);
    }

    public async Task<LodgingViewModel?> UpdateLodgingAsync(string userId, UpdateLodgingRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) return null;

        var entity = trip.Lodgings.FirstOrDefault(l => l.Id == request.Id);
        if (entity == null) return null;

        entity.Type = request.Type;
        entity.Name = request.Name;
        entity.Address = request.Address;
        entity.ConfirmationCode = request.ConfirmationCode;
        entity.Notes = request.Notes;
        entity.Link = request.Link;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Timezone = request.Timezone;
        entity.PlaceId = request.PlaceId;
        entity.TravellerProfileIds = request.TravellerProfileIds.ToList();
        entity.UpdatedAt = DateTime.UtcNow;

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp == null)
            {
                exp = new Expense
                {
                    Name = $"Lodging: {entity.Name}",
                    Category = ExpenseCategory.Accommodation,
                    SourceType = "Lodging",
                    SourceId = entity.Id,
                    CreatedById = userId
                };
                trip.Expenses.Add(exp);
                entity.ExpenseId = exp.Id;
            }
            exp.Amount = request.CostAmount;
            exp.Currency = request.CostCurrency;
            exp.OccurredOn = request.StartDate;
        }
        else if (entity.ExpenseId.HasValue)
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp != null) trip.Expenses.Remove(exp);
            entity.ExpenseId = null;
        }

        await repo.UpdateAsync(trip);
        return ToLodgingViewModel(entity, trip);
    }

    public async Task RemoveLodgingAsync(string userId, int tripId, Guid id)
    {
        var trip = await repo.GetByIdAsync(tripId, userId);
        if (trip == null) return;
        var entity = trip.Lodgings.FirstOrDefault(l => l.Id == id);
        if (entity != null)
        {
            if (entity.ExpenseId.HasValue)
            {
                var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
                if (exp != null) trip.Expenses.Remove(exp);
            }
            trip.Lodgings.Remove(entity);
            await repo.UpdateAsync(trip);
        }
    }

    private static LodgingViewModel ToLodgingViewModel(Lodging l, Trip trip)
    {
        var expense = l.ExpenseId.HasValue ? trip.Expenses.FirstOrDefault(e => e.Id == l.ExpenseId) : null;
        return new LodgingViewModel(
            l.Id, trip.Id, l.Type, l.Name, l.Address, l.ConfirmationCode, l.Notes, l.Link,
            l.StartDate, l.EndDate, l.Timezone, expense?.Amount, expense?.Currency,
            l.PlaceId, null,
            trip.Travellers.Where(tt => l.TravellerProfileIds.Contains(tt.TravellerProfileId) && tt.TravellerProfile != null)
                .Select(tt => new TravellerProfileSummaryViewModel(tt.TravellerProfile!.Id, tt.TravellerProfile.LegalName, tt.TravellerProfile.Email))
                .ToList());
    }

    // ── Activities ────────────────────────────────────────────────────────────

    public async Task<ActivityViewModel> AddActivityAsync(string userId, CreateActivityRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) throw new InvalidOperationException("Trip not found");

        var entity = new Activity
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Notes = request.Notes,
            Link = request.Link,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            PlaceId = request.PlaceId,
            TravellerProfileIds = request.TravellerProfileIds.ToList()
        };

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = new Expense
            {
                Name = $"Activity: {entity.Name}",
                Category = ExpenseCategory.Activities,
                Amount = request.CostAmount,
                Currency = request.CostCurrency,
                OccurredOn = request.StartDate,
                SourceType = "Activity",
                SourceId = entity.Id,
                CreatedById = userId
            };
            trip.Expenses.Add(exp);
            entity.ExpenseId = exp.Id;
        }

        trip.Activities.Add(entity);
        await repo.UpdateAsync(trip);
        return ToActivityViewModel(entity, trip);
    }

    public async Task<ActivityViewModel?> UpdateActivityAsync(string userId, UpdateActivityRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) return null;

        var entity = trip.Activities.FirstOrDefault(a => a.Id == request.Id);
        if (entity == null) return null;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.Notes = request.Notes;
        entity.Link = request.Link;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Timezone = request.Timezone;
        entity.PlaceId = request.PlaceId;
        entity.TravellerProfileIds = request.TravellerProfileIds.ToList();
        entity.UpdatedAt = DateTime.UtcNow;

        if (request.CostAmount is > 0 && !string.IsNullOrEmpty(request.CostCurrency))
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp == null)
            {
                exp = new Expense
                {
                    Name = $"Activity: {entity.Name}",
                    Category = ExpenseCategory.Activities,
                    SourceType = "Activity",
                    SourceId = entity.Id,
                    CreatedById = userId
                };
                trip.Expenses.Add(exp);
                entity.ExpenseId = exp.Id;
            }
            exp.Amount = request.CostAmount;
            exp.Currency = request.CostCurrency;
            exp.OccurredOn = request.StartDate;
        }
        else if (entity.ExpenseId.HasValue)
        {
            var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
            if (exp != null) trip.Expenses.Remove(exp);
            entity.ExpenseId = null;
        }

        await repo.UpdateAsync(trip);
        return ToActivityViewModel(entity, trip);
    }

    public async Task RemoveActivityAsync(string userId, int tripId, Guid id)
    {
        var trip = await repo.GetByIdAsync(tripId, userId);
        if (trip == null) return;
        var entity = trip.Activities.FirstOrDefault(a => a.Id == id);
        if (entity != null)
        {
            if (entity.ExpenseId.HasValue)
            {
                var exp = trip.Expenses.FirstOrDefault(e => e.Id == entity.ExpenseId);
                if (exp != null) trip.Expenses.Remove(exp);
            }
            trip.Activities.Remove(entity);
            await repo.UpdateAsync(trip);
        }
    }

    private static ActivityViewModel ToActivityViewModel(Activity a, Trip trip)
    {
        var expense = a.ExpenseId.HasValue ? trip.Expenses.FirstOrDefault(e => e.Id == a.ExpenseId) : null;
        return new ActivityViewModel(
            a.Id, trip.Id, a.Name, a.Description, a.Address, a.Notes, a.Link,
            a.StartDate, a.EndDate, a.Timezone, expense?.Amount, expense?.Currency,
            a.PlaceId, null,
            trip.Travellers.Where(tt => a.TravellerProfileIds.Contains(tt.TravellerProfileId) && tt.TravellerProfile != null)
                .Select(tt => new TravellerProfileSummaryViewModel(tt.TravellerProfile!.Id, tt.TravellerProfile.LegalName, tt.TravellerProfile.Email))
                .ToList());
    }

    // ── Expenses ──────────────────────────────────────────────────────────────

    public async Task<ExpenseViewModel> AddExpenseAsync(string userId, CreateExpenseRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) throw new InvalidOperationException("Trip not found");

        var entity = new Expense
        {
            Name = request.Name,
            Category = request.Category,
            Notes = request.Notes,
            Amount = request.Amount,
            Currency = request.Currency,
            OccurredOn = request.OccurredOn,
            CreatedById = request.CreatedById ?? userId,
            Splits = request.Splits.Select(s => new ExpenseSplit { TravellerProfileId = s.TravellerProfileId, Amount = s.Amount }).ToList()
        };

        trip.Expenses.Add(entity);
        await repo.UpdateAsync(trip);
        return ToExpenseViewModel(entity, trip);
    }

    public async Task<ExpenseViewModel?> UpdateExpenseAsync(string userId, UpdateExpenseRequest request)
    {
        var trip = await repo.GetByIdAsync(request.TripId, userId);
        if (trip == null) return null;

        var entity = trip.Expenses.FirstOrDefault(e => e.Id == request.Id);
        if (entity == null) return null;

        entity.Name = request.Name;
        entity.Category = request.Category;
        entity.Notes = request.Notes;
        entity.Amount = request.Amount;
        entity.Currency = request.Currency;
        entity.OccurredOn = request.OccurredOn;
        entity.UpdatedAt = DateTime.UtcNow;

        entity.Splits.Clear();
        foreach (var split in request.Splits)
        {
            entity.Splits.Add(new ExpenseSplit { TravellerProfileId = split.TravellerProfileId, Amount = split.Amount });
        }

        await repo.UpdateAsync(trip);
        return ToExpenseViewModel(entity, trip);
    }

    public async Task RemoveExpenseAsync(string userId, int tripId, Guid id)
    {
        var trip = await repo.GetByIdAsync(tripId, userId);
        if (trip == null) return;
        var entity = trip.Expenses.FirstOrDefault(e => e.Id == id);
        if (entity != null)
        {
            // If it's linked to a source, remove the Source's ExpenseId reference
            if (entity.SourceId.HasValue && entity.SourceType != null)
            {
                if (entity.SourceType == "Transportation")
                {
                    var source = trip.Transportations.FirstOrDefault(t => t.Id == entity.SourceId.Value);
                    if (source != null) source.ExpenseId = null;
                }
                else if (entity.SourceType == "Lodging")
                {
                    var source = trip.Lodgings.FirstOrDefault(l => l.Id == entity.SourceId.Value);
                    if (source != null) source.ExpenseId = null;
                }
                else if (entity.SourceType == "Activity")
                {
                    var source = trip.Activities.FirstOrDefault(a => a.Id == entity.SourceId.Value);
                    if (source != null) source.ExpenseId = null;
                }
            }

            trip.Expenses.Remove(entity);
            await repo.UpdateAsync(trip);
        }
    }

    private static ExpenseViewModel ToExpenseViewModel(Expense e, Trip trip)
    {
        return new ExpenseViewModel(
            e.Id, trip.Id, e.Name, e.Category, e.Notes, e.Amount, e.Currency, e.OccurredOn,
            null, // Name mapping for CreatedBy usually handled at UI
            e.Splits.Select(s => new ExpenseSplitViewModel(
                s.TravellerProfileId,
                trip.Travellers.FirstOrDefault(tt => tt.TravellerProfileId == s.TravellerProfileId)?.TravellerProfile?.LegalName ?? "Unknown",
                s.Amount)).ToList(),
            e.SourceId.HasValue, e.SourceType, e.SourceId);
    }
}
