using Viagem.Data.Models;

namespace Viagem.Common;

public static class EnumExtensions
{
    // ExpenseCategory
    public static string GetLabel(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Transport => "Transport",
        ExpenseCategory.Accommodation => "Accommodation",
        ExpenseCategory.Food => "Food & Drink",
        ExpenseCategory.Activities => "Activities",
        ExpenseCategory.Shopping => "Shopping",
        ExpenseCategory.Health => "Health",
        ExpenseCategory.Communication => "Communication",
        _ => "Other"
    };

    public static string GetIcon(this ExpenseCategory category) => category switch
    {
        ExpenseCategory.Transport => "🚌",
        ExpenseCategory.Accommodation => "🏨",
        ExpenseCategory.Food => "🍽️",
        ExpenseCategory.Activities => "🎭",
        ExpenseCategory.Shopping => "🛍️",
        ExpenseCategory.Health => "💊",
        ExpenseCategory.Communication => "📱",
        _ => "💰"
    };

    // LodgingType
    public static string GetLabel(this LodgingType type) => type switch
    {
        LodgingType.Hotel => "Hotel",
        LodgingType.Hostel => "Hostel",
        LodgingType.VacationRental => "Vacation Rental",
        LodgingType.CampSite => "Campsite",
        LodgingType.Home => "Home",
        _ => "Other"
    };

    public static string GetIcon(this LodgingType type) => type switch
    {
        LodgingType.Hotel => "🏨",
        LodgingType.Hostel => "🛏️",
        LodgingType.VacationRental => "🏡",
        LodgingType.CampSite => "⛺",
        LodgingType.Home => "🏠",
        _ => "🛖"
    };

    // TransportationType
    public static string GetLabel(this TransportationType type) => type switch
    {
        TransportationType.Flight => "Flight",
        TransportationType.Train => "Train",
        TransportationType.Bus => "Bus",
        TransportationType.Car => "Car",
        TransportationType.CarRental => "Car Rental",
        TransportationType.Ferry => "Ferry",
        TransportationType.Taxi => "Taxi / Ride",
        TransportationType.Walk => "Walking",
        TransportationType.Bike => "Bicycle",
        TransportationType.Parking => "Parking",
        _ => "Other"
    };

    public static string GetIcon(this TransportationType type) => type switch
    {
        TransportationType.Flight => "✈️",
        TransportationType.Train => "🚆",
        TransportationType.Bus => "🚌",
        TransportationType.Car => "🚗",
        TransportationType.CarRental => "🚙",
        TransportationType.Ferry => "⛴️",
        TransportationType.Taxi => "🚕",
        TransportationType.Walk => "🚶",
        TransportationType.Bike => "🚲",
        TransportationType.Parking => "🅿️",
        _ => "🚐"
    };
}
