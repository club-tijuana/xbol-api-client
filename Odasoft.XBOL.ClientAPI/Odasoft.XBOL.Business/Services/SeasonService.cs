using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Data.Mapping;
using Odasoft.XBOL.Data.Queries;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;
using System.Threading.Tasks;

namespace Odasoft.XBOL.Business.Services
{
    public class SeasonService
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly MediaRepository _mediaRepository;
        private readonly OrderRepository _orderRepository;
        private readonly SeasonPassRepository _seasonPassRepository;

        public SeasonService(
            SeasonRepository seasonRepository,
            MediaRepository mediaRepository,
            OrderRepository orderRepository,
            SeasonPassRepository seasonPassRepository
        )
        {
            _seasonRepository = seasonRepository;
            _mediaRepository = mediaRepository;
            _orderRepository = orderRepository;
            _seasonPassRepository = seasonPassRepository;
        }

        public async Task<SeasonItemDTO?> GetSeasonBannerAsync(long? clientId = null, bool includeMedia = false)
        {
            var now = DateTimeOffset.UtcNow;

            var seasons = await _seasonRepository.Get(
                    s => s.OffSaleDate > now
                ).ToListAsync();

            var seasonStates = seasons.Select(s => new
            {
                Season = s,
                RenewalStart = s.RenewalStartDate,
                IsRenewal = (
                                now >= s.RenewalStartDate
                                && now <= s.RenewalEndDate
                            )
                            && now < s.PreSaleDate,
                IsPreSale = now >= s.PreSaleDate && now < s.OnSaleDate,
                IsGeneral = now >= s.OnSaleDate && now < s.OffSaleDate
            })
            .OrderByDescending(s => s.Season.Id)
            .ToList();

            if (clientId == null)
            {
                var season = seasonStates
                    .Where(s => s.IsGeneral)
                    .Select(s => s.Season)
                    .FirstOrDefault();

                return season == null
                    ? null
                    : await MapSeasonItemAsync(season, includeMedia);
            }
            else
            {
                var clientSeasonIds = await _seasonPassRepository.Get(sp =>
                    sp.ClientId == clientId
                )
                .Select(sp => sp.SeasonId)
                .Distinct()
                .ToListAsync();

                var seasonStatesWithAccess = seasonStates.Select(s => new
                {
                    s.Season,
                    s.IsRenewal,
                    s.IsPreSale,
                    s.IsGeneral,
                    HasPrevious = s.Season.PreviousSeasonId != null
                        && clientSeasonIds.Contains(s.Season.PreviousSeasonId.Value)
                }).ToList();

                var season = seasonStatesWithAccess
                    .Where(s =>
                        s.IsGeneral
                        || (s.HasPrevious && (s.IsRenewal || s.IsPreSale))
                    )
                    .Select(s => s.Season)
                    .FirstOrDefault();

                return season == null
                    ? null
                    : await MapSeasonItemAsync(season, includeMedia);
            }
        }

        public async Task<long?> GetSeasonIdByExternalKeyAsync(string externalSeasonKey)
        {
            return await _seasonRepository.GetSeasonIdByExternalSeasonKeyAsync(externalSeasonKey);
        }

        public async Task<Season?> GetSeasonByExternalKeyAsync(string externalSeasonKey)
        {
            return await _seasonRepository.GetSeasonByExternalSeasonKeyAsync(externalSeasonKey);
        }

        public async Task<Season?> GetLatestSeasonAsync(long originSeasonId)
        {
            var seasonChainData = await _seasonRepository.Get()
                                        .AsNoTracking()
                                        .Where(s => s.DeletedAt == null && s.PreviousSeasonId.HasValue)
                                        .Select(s => new { Id = s.Id, PreviousSeasonId = s.PreviousSeasonId!.Value })
                                        .ToListAsync();

            var nextSeasonLookup = seasonChainData.ToDictionary(s => s.PreviousSeasonId, s => s.Id);

            long latestSeasonId = originSeasonId;

            while (nextSeasonLookup.TryGetValue(latestSeasonId, out long nextSeasonId))
            {
                latestSeasonId = nextSeasonId;
            }

            return await _seasonRepository
                            .Get()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == latestSeasonId && s.DeletedAt == null);
        }

        private async Task<SeasonItemDTO> MapSeasonItemAsync(Season season, bool includeMedia)
        {
            var media = await _mediaRepository
                .Get(filter: m =>
                    m.ReferenceId == season.Id &&
                    m.ReferenceType == ClientSaleType.SeasonPass,
                    includedProperties: "BlobAsset"
                )
                .AvailableBlobMedia()
                .ToListAsync();

            var banner = media
                .Where(m => m.MediaType == ClientMediaType.Banner)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            return new SeasonItemDTO
            {
                Id = season.Id,
                BannerImageUrl = banner != null && banner.Url != null
                    ? banner.Url
                    : season.BannerImageUrl,
                StartDate = season.StartDate,
                ExternalSeasonKey = season.ExternalSeasonKey,
                Media = includeMedia
                    ? EventMediaSetMapper.CreateMediaSet(media.Select(EventMediaSetMapper.CreateMediaResponse))
                    : null
            };
        }

        public async Task<SeoMetadataDTO> GetSeasonMetadataAsync(long seasonId)
        {
            var season = await _seasonRepository.GetByIdAsync(seasonId);

            if (season == null)
            {
                return new SeoMetadataDTO();
            }

            return new SeoMetadataDTO
            {
                Title = season.Name,
                Description = season.Description,
                ImageUrl = season.PosterImageUrl
            };
        }
    }
}
