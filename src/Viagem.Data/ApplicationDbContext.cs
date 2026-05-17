using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;

namespace Viagem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripDestination> TripDestinations => Set<TripDestination>();
    public DbSet<TripTraveller> TripTravellers => Set<TripTraveller>();
    public DbSet<Transportation> Transportations => Set<Transportation>();
    public DbSet<TransportationTraveller> TransportationTravellers => Set<TransportationTraveller>();
    public DbSet<TransportationAttachment> TransportationAttachments => Set<TransportationAttachment>();
    public DbSet<Lodging> Lodgings => Set<Lodging>();
    public DbSet<LodgingTraveller> LodgingTravellers => Set<LodgingTraveller>();
    public DbSet<LodgingAttachment> LodgingAttachments => Set<LodgingAttachment>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityTraveller> ActivityTravellers => Set<ActivityTraveller>();
    public DbSet<ActivityAttachment> ActivityAttachments => Set<ActivityAttachment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<ExpenseAttachment> ExpenseAttachments => Set<ExpenseAttachment>();
    public DbSet<TravellerProfile> TravellerProfiles => Set<TravellerProfile>();
    public DbSet<TravellerProfileAlias> TravellerProfileAliases => Set<TravellerProfileAlias>();
    public DbSet<TravellerAdditionalField> TravellerAdditionalFields => Set<TravellerAdditionalField>();
    public DbSet<TravellerProfileManager> TravellerProfileManagers => Set<TravellerProfileManager>();
    public DbSet<TravellerAttachment> TravellerAttachments => Set<TravellerAttachment>();
    public DbSet<TripAttachment> TripAttachments => Set<TripAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Airline> Airlines => Set<Airline>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Trip>()
            .HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transportation>()
            .HasOne(t => t.Expense)
            .WithMany()
            .HasForeignKey(t => t.ExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Lodging>()
            .HasOne(l => l.Expense)
            .WithMany()
            .HasForeignKey(l => l.ExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Activity>()
            .HasOne(a => a.Expense)
            .WithMany()
            .HasForeignKey(a => a.ExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Expense>()
            .HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TravellerProfile>()
            .HasOne(tp => tp.Owner)
            .WithMany()
            .HasForeignKey(tp => tp.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TravellerProfile>()
            .HasOne(tp => tp.LinkedUser)
            .WithMany()
            .HasForeignKey(tp => tp.LinkedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TripAttachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Airport>()
            .HasIndex(a => a.IataCode);

        builder.Entity<Airline>()
            .HasIndex(a => a.Code);

        builder.Entity<ApplicationUser>()
            .Ignore(u => u.PhoneNumber)
            .Ignore(u => u.PhoneNumberConfirmed);
    }
}
