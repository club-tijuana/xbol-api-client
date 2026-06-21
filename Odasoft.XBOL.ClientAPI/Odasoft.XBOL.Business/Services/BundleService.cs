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
        MediaRepository mediaRepository
    )
    {
        public async Task<BundleItemDTO?> GetBundleBannerAsync(long? clientId = null, bool includeMedia = false)
        {
            var now = DateTimeOffset.UtcNow;

            var bundles = await bundleRepository.Get(
                    b => b.Status == Commons.Enums.EventStatus.Published &&
                    b.BundleType == Commons.Enums.BundleType.SeasonPass &&
                    b.PublishedDate <= now
                    && b.OffSaleDate > now
                ).ToListAsync();

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
                    .Where(b => b.IsGeneral)
                    .Select(b => b.Bundle)
                    .FirstOrDefault();

                return bundle == null
                    ? null
                    : await MapBundleItemAsync(bundle, includeMedia);
            }
            else
            {
                var clientSeasonIds = await bundlePassRepository.Get(bp =>
                    bp.ClientId == clientId
                )
                .Select(bp => bp.BundleId)
                .Distinct()
                .ToListAsync();

                var seasonStatesWithAccess = bundlesStates.Select(b => new
                {
                    b.Bundle,
                    b.IsRenewal,
                    b.IsPreSale,
                    b.IsGeneral,
                    HasPrevious = b.Bundle.PreviousBundleId != null
                        && clientSeasonIds.Contains(b.Bundle.PreviousBundleId.Value)
                }).ToList();

                var season = seasonStatesWithAccess
                    .Where(b =>
                        b.IsGeneral
                        || (b.HasPrevious && (b.IsRenewal || b.IsPreSale))
                    )
                    .Select(b => b.Bundle)
                    .FirstOrDefault();

                return season == null
                    ? null
                    : await MapBundleItemAsync(season, includeMedia);
            }
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

        private async Task<BundleItemDTO> MapBundleItemAsync(Bundle bundle, bool includeMedia)
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
                    : null
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
