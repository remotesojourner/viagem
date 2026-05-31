using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Viagem.Data.Models;

namespace Viagem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripTraveller> TripTravellers => Set<TripTraveller>();
    public DbSet<TravellerProfile> TravellerProfiles => Set<TravellerProfile>();
    public DbSet<TravellerProfileAlias> TravellerProfileAliases => Set<TravellerProfileAlias>();
    public DbSet<TravellerAdditionalField> TravellerAdditionalFields => Set<TravellerAdditionalField>();
    public DbSet<TravellerProfileManager> TravellerProfileManagers => Set<TravellerProfileManager>();
    public DbSet<TravellerAttachment> TravellerAttachments => Set<TravellerAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<UserTravelStats> UserTravelStats => Set<UserTravelStats>();
    public DbSet<UserTravelDestination> UserTravelDestinations => Set<UserTravelDestination>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Trip>()
            .HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.Entity<Trip>().OwnsMany(t => t.Destinations, b => b.ToJson());
        builder.Entity<Trip>().OwnsMany(t => t.Transportations, b => b.ToJson());
        builder.Entity<Trip>().OwnsMany(t => t.Lodgings, b => b.ToJson());
        builder.Entity<Trip>().OwnsMany(t => t.Activities, b => b.ToJson());
        builder.Entity<Trip>().OwnsMany(t => t.Expenses, b =>
        {
            b.ToJson();
            b.OwnsMany(e => e.Splits);
        });
        builder.Entity<Trip>().OwnsMany(t => t.Attachments, b => b.ToJson());

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

        builder.Entity<UserTravelStats>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserTravelStats>()
            .HasIndex(s => new { s.UserId, s.Year })
            .IsUnique();

        builder.Entity<UserTravelDestination>()
            .HasOne(d => d.Place)
            .WithMany()
            .HasForeignKey(d => d.PlaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserTravelDestination>()
            .HasIndex(d => new { d.UserId, d.PlaceId });
    }
}
