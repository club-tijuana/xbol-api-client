using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Odasoft.XBOL.Business.Configs;
using Odasoft.XBOL.Business.Services;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.Models;
using Xunit;
using TicketingClientInterface = Odasoft.XBOL.Business.ITicketingClient;
using TicketingSaleType = Odasoft.XBOL.Business.SaleType;

namespace Odasoft.XBOL.ClientAPI.Tests.Services;

public sealed class EventMediaPropagationTests
{
    [Fact]
    public async Task GetEventDetailAsync_WhenIncludeMediaIsTrue_ReturnsBlobBackedMedia()
    {
        await using var database = await TestDatabase.CreateAsync();
        var eventItem = CreatePublishedEvent(DateTimeOffset.UtcNow);
        database.Context.Events.Add(eventItem);
        await database.Context.SaveChangesAsync();
        database.Context.Media.Add(CreateEventBanner(eventItem.Id));
        await database.Context.SaveChangesAsync();
        var service = CreateEventService(database.Context);

        var result = await service.GetEventDetailAsync(eventItem.Id, includeMedia: true);

        result.Should().NotBeNull();
        result!.Image.Should().Be("https://cdn.example.test/event-banner.jpg");
        result.Media.Should().NotBeNull();
        result.Media!.Banner.Should().NotBeNull();
        result.Media.Banner!.Url.Should().Be("https://cdn.example.test/event-banner.jpg");
    }

    [Fact]
    public void GetUpcomingEventsAsync_ExposesIncludeMediaPropagationParameter()
    {
        typeof(EventRepository)
            .GetMethod(nameof(EventRepository.GetUpcomingEventsAsync), [typeof(int), typeof(int), typeof(bool)])
            .Should()
            .NotBeNull();

        typeof(EventService)
            .GetMethod(nameof(EventService.GetUpcomingEventsAsync), [typeof(int?), typeof(int?), typeof(bool)])
            .Should()
            .NotBeNull();
    }

    private static EventService CreateEventService(XBOLDbContext context)
    {
        var ticketingClient = Substitute.For<TicketingClientInterface>();
        ticketingClient
            .GetZonePricesAsync(Arg.Any<TicketingSaleType>(), Arg.Any<long>())
            .Returns([]);

        return new EventService(
            new EventRepository(context),
            new EventCategoryRepository(context),
            new EventScheduleRepository(context),
            new EventViewRepository(context),
            new SearchSettings(),
            new EventsTrackingSettings(),
            Substitute.For<ILogger<EventService>>(),
            ticketingClient);
    }

    private static Event CreatePublishedEvent(DateTimeOffset now)
    {
        var venueMap = CreateVenueMap(now);
        var baseSection = new BaseSection
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
            Name = "Base Section",
            SectionType = SectionType.General
        };

        return new Event
        {
            VenueMap = venueMap,
            Name = "Media Event",
            Status = EventStatus.Published,
            BannerImageUrl = "",
            PosterImageUrl = "",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            Categories =
            [
                new Models.EventCategory
                {
                    Name = "music",
                    DisplayName = "Music",
                    IsActive = true
                }
            ],
            Schedules =
            [
                new EventSchedule
                {
                    StartDateTime = now.AddDays(7),
                    EndDateTime = now.AddDays(8),
                    OnSaleDate = now.AddDays(-1),
                    OffSaleDate = now.AddDays(6),
                    Status = ScheduleStatus.OnSale,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedBy = Guid.Empty,
                    Sections =
                    [
                        new EventSection
                        {
                            BaseSection = baseSection,
                            DisplayName = "Main",
                            TotalSeats = 10,
                            AvailableSeats = 10
                        }
                    ]
                }
            ]
        };
    }

    private static VenueMap CreateVenueMap(DateTimeOffset now)
    {
        return new VenueMap
        {
            Venue = new Venue
            {
                Name = "Media Venue",
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
            Name = "Media Venue Map",
            ExternalMapKey = Guid.NewGuid().ToString("N"),
            Capacity = 10,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

    private static Media CreateEventBanner(long eventId)
    {
        return new Media
        {
            ReferenceId = eventId,
            ReferenceType = ClientSaleType.Event,
            MediaType = ClientMediaType.Banner,
            BlobAsset = new BlobAsset
            {
                BucketName = "media",
                ObjectName = "events/event-banner.jpg",
                FileName = "event-banner.jpg",
                ContentType = "image/jpeg",
                Url = "https://cdn.example.test/event-banner.jpg",
                Status = BlobAssetStatus.Available,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Order = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
    }

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
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
