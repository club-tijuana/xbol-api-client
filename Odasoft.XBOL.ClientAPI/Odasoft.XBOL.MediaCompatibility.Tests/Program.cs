using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

var available = new Media
{
    Id = 10,
    ReferenceId = 20,
    ReferenceType = ClientSaleType.Event,
    MediaType = ClientMediaType.Banner,
    BlobAssetId = 30,
    BlobAsset = new BlobAsset
    {
        Id = 30,
        FileName = "banner.jpg",
        ContentType = "image/jpeg",
        Url = "https://cdn.example.test/banner.jpg",
        Status = BlobAssetStatus.Available
    },
    Order = 1
};

var response = EventMediaSetMapper.CreateMediaResponse(available);

AssertEqual(available.BlobAsset.Url, response.Url, "MediaResponse.Url must come from BlobAsset.Url.");
AssertEqual(available.BlobAsset.FileName, response.FileName, "MediaResponse.FileName must come from BlobAsset.FileName.");
AssertEqual(available.BlobAsset.ContentType, response.ContentType, "MediaResponse.ContentType must come from BlobAsset.ContentType.");

var logoResponse = new MediaResponse
{
    Id = 13,
    Url = "https://cdn.example.test/logo.jpg",
    ContentType = "image/jpeg",
    FileName = "logo.jpg",
    MediaType = ClientMediaType.Logo,
    Order = 0
};

var mediaSet = EventMediaSetMapper.CreateMediaSet([response, logoResponse]);
AssertEqual(response.Url, mediaSet.Banner?.Url, "Media sets must expose banner media.");
AssertEqual(logoResponse.Url, mediaSet.Logo?.Url, "Media sets must expose logo media for bundle catalog items.");

var unavailable = new Media
{
    Id = 11,
    ReferenceId = 20,
    ReferenceType = ClientSaleType.Event,
    MediaType = ClientMediaType.Banner,
    BlobAssetId = 31,
    BlobAsset = new BlobAsset
    {
        Id = 31,
        FileName = "pending.jpg",
        ContentType = "image/jpeg",
        Url = "https://cdn.example.test/pending.jpg",
        Status = BlobAssetStatus.PendingUpload
    },
    Order = 2
};

var softDeleted = new Media
{
    Id = 12,
    ReferenceId = 20,
    ReferenceType = ClientSaleType.Event,
    MediaType = ClientMediaType.Banner,
    BlobAssetId = 32,
    BlobAsset = new BlobAsset
    {
        Id = 32,
        FileName = "deleted.jpg",
        ContentType = "image/jpeg",
        Url = "https://cdn.example.test/deleted.jpg",
        Status = BlobAssetStatus.Available
    },
    DeletedAt = DateTimeOffset.UtcNow,
    Order = 3
};

var filtered = new[] { available, unavailable, softDeleted }
    .AsQueryable()
    .AvailableBlobMedia()
    .ToList();

AssertEqual(1, filtered.Count, "AvailableBlobMedia must only return active rows backed by available blob assets.");
AssertEqual(available.Id, filtered[0].Id, "AvailableBlobMedia returned the wrong media row.");

var catalogItem = new EventCatalogItemDTO
{
    Id = 40,
    ItemType = EventCatalogItemType.Bundle,
    BundleType = BundleType.Basic,
    Status = EventStatus.Published,
    ScheduledStartDate = DateTimeOffset.UtcNow,
    Name = "Bundle Catalog Item",
    AvailableSeats = 100,
    TotalSeats = 120,
    Media = mediaSet,
    PosterImageUrl = "https://cdn.example.test/logo.jpg",
    BannerImageUrl = "https://cdn.example.test/banner.jpg"
};

AssertEqual(EventCatalogItemType.Bundle, catalogItem.ItemType, "Catalog items must distinguish bundles from events.");
AssertEqual("https://cdn.example.test/logo.jpg", catalogItem.Media?.Logo?.Url, "Catalog media logo URL must come from blob media.");
AssertEqual("https://cdn.example.test/banner.jpg", catalogItem.Media?.Banner?.Url, "Catalog media banner URL must come from blob media.");
AssertEqual("https://cdn.example.test/logo.jpg", catalogItem.PosterImageUrl, "Catalog poster media URL must be exposed.");
AssertEqual("https://cdn.example.test/banner.jpg", catalogItem.BannerImageUrl, "Catalog banner media URL must be exposed.");

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }
}
