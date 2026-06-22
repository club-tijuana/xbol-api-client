using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Commons.Requests;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.Models;
using Xunit;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class EventCatalogServiceTests
{
    [Fact]
    public async Task GetItemsAsync_BuyableOnlyFiltersBundlesBeforePagination()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTimeOffset.UtcNow;

        database.Context.Bundles.Add(CreateBundle(10, BundleType.Basic, "Buyable Basic", now, now.AddDays(-1), now.AddDays(10)));
        database.Context.Bundles.Add(CreateBundle(20, BundleType.Basic, "Future Basic", now, now.AddDays(1), now.AddDays(10)));
        database.Context.Bundles.Add(CreateBundle(9, BundleType.SeasonPass, "Previous Season", now, now.AddMonths(-8), now.AddMonths(-2)));
        database.Context.Bundles.Add(CreateBundle(
            30,
            BundleType.SeasonPass,
            "Renewal Only Season",
            now,
            now.AddDays(-1),
            now.AddDays(10),
            renewalStartDate: now.AddDays(-1),
            renewalEndDate: now.AddDays(2),
            previousBundleId: 9));

        await database.Context.SaveChangesAsync();
        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetItemsAsync(new EventCatalogQueryParams
        {
            ItemType = EventCatalogItemType.Bundle,
            BuyableOnly = true,
            Page = 1,
            PageSize = 1,
            SortBy = "name",
            Descending = false
        });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Buyable Basic");
    }

    [Fact]
    public async Task GetBundleBannerAsync_ReturnsNullForRenewalOnlySeasonWithoutClientEligibility()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTimeOffset.UtcNow;
        database.Context.Bundles.Add(CreateBundle(9, BundleType.SeasonPass, "Previous Season", now, now.AddMonths(-8), now.AddMonths(-2)));
        database.Context.Bundles.Add(CreateBundle(
            10,
            BundleType.SeasonPass,
            "Renewal Only Season",
            now,
            now.AddDays(-1),
            now.AddDays(10),
            renewalStartDate: now.AddDays(-1),
            renewalEndDate: now.AddDays(2),
            previousBundleId: 9));
        await database.Context.SaveChangesAsync();

        var sut = CreateBundleService(database.Context);

        var result = await sut.GetBundleBannerAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBundleBannerAsync_ReturnsRenewalSeasonForEligibleClient()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now);
        var previousBundle = CreateBundle(9, BundleType.SeasonPass, "Previous Season", now, now.AddMonths(-8), now.AddMonths(-2));
        var renewalBundle = CreateBundle(
            10,
            BundleType.SeasonPass,
            "Renewal Only Season",
            now,
            now.AddDays(-1),
            now.AddDays(10),
            renewalStartDate: now.AddDays(-1),
            renewalEndDate: now.AddDays(2),
            preSaleDate: now.AddDays(3),
            previousBundleId: previousBundle.Id);

        database.Context.Clients.Add(client);
        database.Context.Bundles.AddRange(previousBundle, renewalBundle);
        database.Context.BundlePasses.Add(new BundlePass
        {
            Client = client,
            Bundle = previousBundle,
            TrackingCode = "A-1",
            PrivateToken = Guid.NewGuid().ToString("N"),
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 100,
            PurchasedAt = now.AddMonths(-6),
            CreatedAt = now.AddMonths(-6),
            UpdatedAt = now.AddMonths(-6),
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        });
        await database.Context.SaveChangesAsync();

        var sut = CreateBundleService(database.Context);

        var result = await sut.GetBundleBannerAsync(client.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(renewalBundle.Id);
    }

    private static BundleService CreateBundleService(XBOLDbContext context)
    {
        return new BundleService(
            new BundleRepository(context),
            new BundlePassRepository(context),
            new MediaRepository(context));
    }

    private static Bundle CreateBundle(
        long id,
        BundleType type,
        string name,
        DateTimeOffset now,
        DateTimeOffset? onSaleDate,
        DateTimeOffset? offSaleDate,
        DateTimeOffset? renewalStartDate = null,
        DateTimeOffset? renewalEndDate = null,
        DateTimeOffset? preSaleDate = null,
        long? previousBundleId = null)
    {
        var venueMap = CreateVenueMap(now);
        var baseSection = CreateBaseSection(venueMap, now);

        return new Bundle
        {
            Id = id,
            VenueMap = venueMap,
            Name = name,
            Status = EventStatus.Published,
            BundleType = type,
            BundlePricingType = BundlePricingType.Composite,
            ExternalKey = $"bundle-{id}",
            StartDate = now.AddDays(20),
            EndDate = now.AddDays(30),
            PublishedDate = now.AddDays(-2),
            OnSaleDate = onSaleDate,
            PreSaleDate = preSaleDate,
            OffSaleDate = offSaleDate,
            RenewalStartDate = renewalStartDate,
            RenewalEndDate = renewalEndDate,
            PreviousBundleId = previousBundleId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            BundleSections =
            [
                new BundleSection
                {
                    BaseSection = baseSection,
                    DisplayName = "Main",
                    TotalSeats = 1,
                    AvailableSeats = 1,
                    BundleSeats =
                    [
                        new BundleSeat
                        {
                            BaseSeat = new BaseSeat
                            {
                                BaseRow = new BaseRow
                                {
                                    BaseSection = baseSection,
                                    RowLabel = "A"
                                },
                                SeatNumber = "1",
                                SeatType = SeatType.Standard
                            },
                            ExternalSeatObjectKey = "A-1",
                            ForSale = true
                        }
                    ]
                }
            ]
        };
    }

    private static Client CreateClient(DateTimeOffset now)
    {
        return new Client
        {
            ClientType = ClientType.Individual,
            Email = "buyer@example.com",
            PhoneRegionCodeId = MxPhoneRegionId,
            PhoneNumber = "+526641234567",
            FullName = "Eligible Client",
            IsActive = true,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedAt = now,
            UpdatedBy = Guid.Empty
        };
    }

    private static BaseSection CreateBaseSection(VenueMap venueMap, DateTimeOffset now)
    {
        return new BaseSection
        {
            BaseZone = new BaseZone
            {
                VenueMap = venueMap,
                Name = "Base Zone",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            },
            Name = "Main",
            SectionType = SectionType.General
        };
    }

    private static VenueMap CreateVenueMap(DateTimeOffset now)
    {
        return new VenueMap
        {
            Venue = new Venue
            {
                Name = "Catalog Venue",
                Category = VenueCategory.Stadium,
                Status = VenueStatus.Active,
                Country = "MX",
                State = "BC",
                City = "Tijuana",
                StreetAddress = "Main",
                ZipCode = "22000",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedBy = Guid.Empty
            },
            Name = "Catalog Venue Map",
            ExternalMapKey = Guid.NewGuid().ToString("N"),
            Capacity = 10,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private const long MxPhoneRegionId = 2;

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Set<PhoneRegionCode>().Add(new PhoneRegionCode
            {
                Id = MxPhoneRegionId,
                RegionCode = "MX",
                DialCode = "52",
                FlagEmoji = string.Empty
            });
            await context.SaveChangesAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
