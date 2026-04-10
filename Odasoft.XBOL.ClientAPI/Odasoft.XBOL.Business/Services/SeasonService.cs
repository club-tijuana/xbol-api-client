using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class SeasonService
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly OrderRepository _orderRepository;
        private readonly SeasonPassRepository _seasonPassRepository;

        public SeasonService(
            SeasonRepository seasonRepository,
            OrderRepository orderRepository,
            SeasonPassRepository seasonPassRepository
        )
        {
            _seasonRepository = seasonRepository;
            _orderRepository = orderRepository;
            _seasonPassRepository = seasonPassRepository;
        }

        public async Task<SeasonItemDTO?> GetSeasonBannerAsync(long? clientId = null)
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
                return seasonStates
                    .Where(s => s.IsGeneral)
                    .Select(s => new SeasonItemDTO
                    {
                        Id = s.Season.Id,
                        BannerImageUrl = s.Season.BannerImageUrl,
                        ExternalSeasonKey = s.Season.ExternalSeasonKey
                    })
                    .FirstOrDefault();
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

                var result = seasonStatesWithAccess
                    .Where(s =>
                        s.IsGeneral
                        || (s.HasPrevious && (s.IsRenewal || s.IsPreSale))
                    )
                    .Select(s => new SeasonItemDTO
                    {
                        Id = s.Season.Id,
                        BannerImageUrl = s.Season.BannerImageUrl,
                        ExternalSeasonKey = s.Season.ExternalSeasonKey
                    })
                    .FirstOrDefault();

                return result;
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
                                        .Select(s => new { Id = s.Id, PreviousSeasonId = s.PreviousSeasonId.Value })
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
    }
}
