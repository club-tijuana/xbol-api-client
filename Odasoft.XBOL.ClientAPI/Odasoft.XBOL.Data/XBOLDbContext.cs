using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Configurations;
using Odasoft.XBOL.Data.Extensions;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data
{
    public class XBOLDbContext : DbContext
    {
        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<Bundle> Bundles => Set<Bundle>();
        public DbSet<BundleEventSchedule> BundleEventSchedules => Set<BundleEventSchedule>();
        public DbSet<BundleSection> BundleSections => Set<BundleSection>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<ClientLoginIdentifier> ClientLoginIdentifiers => Set<ClientLoginIdentifier>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<SeasonPass> SeasonPasses => Set<SeasonPass>();
        public DbSet<SeasonPassEventTicket> SeasonPassEventTickets => Set<SeasonPassEventTicket>();
        public DbSet<SeasonSection> SeasonSections => Set<SeasonSection>();
        public DbSet<SeasonSeat> SeasonSeats => Set<SeasonSeat>();
        public DbSet<BaseSeat> BaseSeats => Set<BaseSeat>();
        public DbSet<BaseRow> BaseRows => Set<BaseRow>();
        public DbSet<BaseSection> BaseSections => Set<BaseSection>();
        public DbSet<BaseZone> BaseZones => Set<BaseZone>();
        public DbSet<VenueMap> VenueMaps => Set<VenueMap>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Event> Events { get; set; }
        public DbSet<EventSeat> EventSeats { get; set; }
        public DbSet<EventSchedule> EventSchedules { get; set; }
        public DbSet<InventoryBatch> InventoryBatches { get; set; }
        public DbSet<Performer> Performers { get; set; }
        public DbSet<EventViews> EventViews { get; set; }
        public DbSet<ClientFavoriteEvent> ClientFavoriteEvents { get; set; }
        public DbSet<SequenceTracker> SequenceTrackers { get; set; }
        public DbSet<EventCategory> EventCategories => Set<EventCategory>();
        public DbSet<BlobAsset> BlobAssets => Set<BlobAsset>();
        public DbSet<Media> Media => Set<Media>();
        public DbSet<BlobAsset> BlobAssets => Set<BlobAsset>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();

        public XBOLDbContext() { }

        public XBOLDbContext(DbContextOptions<XBOLDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(@"Host=localhost;Port=5432;Database=XBOL;Username=postgres;Password=12345");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(BaseModel.Id))
                        .ValueGeneratedOnAdd();
                }
            }

            modelBuilder.Entity<SeasonPassEventTicket>()
                .Property(spet => spet.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.RemovePluralizingTableNameConvention();

            modelBuilder.ApplyConfiguration(new ClientLoginIdentifierConfiguration());
            modelBuilder.ApplyConfiguration(new TicketConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new BlobAssetConfiguration());
            modelBuilder.ApplyConfiguration(new MediaConfiguration());
            modelBuilder.ApplyConfiguration(new BundleConfiguration());
            modelBuilder.ApplyConfiguration(new BundleEventScheduleConfiguration());
        }
    }
}
