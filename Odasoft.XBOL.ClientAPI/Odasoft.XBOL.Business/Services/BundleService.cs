using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class BundleService(
        BundleRepository bundleRepository,
        BundlePassRepository bundlePassRepository,
        OrderRepository orderRepository,
        MediaRepository mediaRepository
    )
    {
        public async Task<BundleItemDTO?> GetBundleBannerAsync(long? clientId = null, bool includeMedia = false)
        {
            var now = DateTimeOffset.UtcNow;

            var bundles = (await bundleRepository.Get(
                    b => b.Status == Commons.Enums.EventStatus.Published &&
                    b.BundleType == Commons.Enums.BundleType.SeasonPass,
                    includedProperties: ["BundleSections.BundleSeats"]
                )
                .ToListAsync())
                .Where(bundle => bundle.PublishedDate <= now && bundle.OffSaleDate > now)
                .ToList();

            var bundlesStates = bundles.Select(b => new
            {
                Bundle = b,
                RenewalStart = b.RenewalStartDate,
                IsRenewal = (
                                now >= b.RenewalStartDate
                                && now <= b.RenewalEndDate
                            )
                            && now < b.PreSaleDate,
                IsPreSale = now >= b.PreSaleDate && now < b.OnSaleDate,
                IsGeneral = now >= b.OnSaleDate && now < b.OffSaleDate
            })
            .OrderByDescending(b => b.Bundle.StartDate)
            .ToList();

            if (clientId == null)
            {
                var bundle = bundlesStates
                    .Where(b => b.IsGeneral && EventCatalogService.IsBuyableBundle(b.Bundle))
                    .Select(b => b.Bundle)
                    .FirstOrDefault();

                return bundle == null
                    ? null
                    : await MapBundleItemAsync(bundle, includeMedia);
            }
            else
            {
                var clientBundlePasses = await bundlePassRepository.Get(bp =>
                    bp.ClientId == clientId
                )
                .ToListAsync();
                var clientBundlePassIds = clientBundlePasses
                    .Select(bp => bp.Id)
                    .ToList();
                var clientBundleOrders = await orderRepository.Get(o =>
                    o.ClientId == clientId &&
                    o.OrderType == OrderType.Bundle &&
                    o.Items.Any(i => clientBundlePassIds.Contains(i.ItemReferenceId))
                )
                .Select(o => new
                {
                    o.Id,
                    BundlePassId = o.Items
                        .Where(i => clientBundlePassIds.Contains(i.ItemReferenceId))
                        .Select(i => i.ItemReferenceId)
                        .First()
                })
                .ToListAsync();

                var seasonStatesWithAccess = bundlesStates.Select(b => new
                {
                    b.Bundle,
                    b.IsRenewal,
                    b.IsPreSale,
                    b.IsGeneral,
                    RelatedOrderId = b.Bundle.PreviousBundleId == null
                        ? (long?)null
                        : clientBundleOrders
                            .Where(o => clientBundlePasses.Any(bp =>
                                bp.Id == o.BundlePassId &&
                                bp.BundleId == b.Bundle.PreviousBundleId.Value))
                            .Select(o => (long?)o.Id)
                            .FirstOrDefault()
                }).ToList();

                var season = seasonStatesWithAccess
                    .Where(b =>
                        b.RelatedOrderId.HasValue && (b.IsRenewal || b.IsPreSale) && HasForSaleSeat(b.Bundle)
                        || b.IsGeneral && EventCatalogService.IsBuyableBundle(b.Bundle)
                    )
                    .FirstOrDefault();

                return season == null
                    ? null
                    : await MapBundleItemAsync(
                        season.Bundle,
                        includeMedia,
                        season.IsRenewal,
                        season.IsPreSale,
                        season.IsGeneral,
                        season.IsGeneral ? null : season.RelatedOrderId);
            }
        }

        private static bool HasForSaleSeat(Bundle bundle)
        {
            return bundle.BundleSections
                .SelectMany(section => section.BundleSeats)
                .Any(seat => seat.ForSale);
        }

        public async Task<SeoMetadataDTO> GetBundleMetadataAsync(long bundleId)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId);

            if (bundle == null)
            {
                return new SeoMetadataDTO();
            }

            return new SeoMetadataDTO
            {
                Title = bundle.Name,
                Description = bundle.ShortDescription,
                ImageUrl = bundle.PosterImageUrl
            };
        }

        private async Task<BundleItemDTO> MapBundleItemAsync(
            Bundle bundle,
            bool includeMedia,
            bool isRenewal = false,
            bool isPreSale = false,
            bool isGeneralSale = false,
            long? relatedOrderId = null)
        {
            var media = await mediaRepository
                .Get(filter: m =>
                    m.ReferenceId == bundle.Id &&
                    m.ReferenceType == ClientSaleType.SeasonPass,
                    includedProperties: "BlobAsset"
                )
                .AvailableBlobMedia()
                .ToListAsync();

            var banner = media
                .Where(m => m.MediaType == ClientMediaType.Banner)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            return new BundleItemDTO
            {
                Id = bundle.Id,
                BannerImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : bundle.BannerImageUrl,
                StartDate = bundle.StartDate,
                ExternalKey = bundle.ExternalKey,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(media.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null,
                IsRenewal = isRenewal,
                IsPreSale = isPreSale,
                IsGeneralSale = isGeneralSale,
                RelatedOrderId = relatedOrderId
            };
        }

        public async Task<Bundle?> GetLatestBundleAsync(long originBundleId)
        {
            var now = DateTimeOffset.UtcNow;

            var bundleChainData = await bundleRepository.Get()
                                        .AsNoTracking()
                                        .Where(b =>
                                            b.Status == Commons.Enums.EventStatus.Published
                                            && b.PublishedDate < now
                                            && b.RenewalStartDate <= now
                                            && b.OffSaleDate > now
                                            && b.DeletedAt == null
                                            && b.PreviousBundleId.HasValue
                                        )
                                        .Select(b => new { Id = b.Id, PreviousBundleId = b.PreviousBundleId!.Value })
                                        .ToListAsync();

            var nextBundleLookup = bundleChainData.ToDictionary(s => s.PreviousBundleId, s => s.Id);

            long latestBundleId = originBundleId;

            while (nextBundleLookup.TryGetValue(latestBundleId, out long nextBundleId))
            {
                latestBundleId = nextBundleId;
            }

            return await bundleRepository
                            .Get()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == latestBundleId && s.DeletedAt == null);
        }

        public async Task<List<string>> GetBlockedSeatsAsync(long idClient, long bundleId)
        {
            var now = DateTimeOffset.UtcNow;

            var currentBundle = await bundleRepository.GetByIdAsync(bundleId);

            if (currentBundle == null)
            {
                return new List<string>();
            }

            if (currentBundle.PreviousBundleId == null)
            {
                return new List<string>();
            }

            if (currentBundle.OnSaleDate <= now)
            {
                return new List<string>();
            }

            var blockedSeats = await bundlePassRepository
                .Get(bp =>
                    bp.BundleId == currentBundle.PreviousBundleId
                    && bp.ClientId != idClient
                )
                .Select(bp => bp.TrackingCode)
                .ToListAsync();

            return blockedSeats;
        }
    }
}
