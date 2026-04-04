using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Business.Services
{
    public class SeasonService
    {
        private readonly SeasonRepository _seasonRepository;

        public SeasonService(SeasonRepository seasonRepository)
        {
            _seasonRepository = seasonRepository;
        }

        public async Task<SeasonItemDTO?> GetSeasonBannerAsync()
        {
            var now = DateTimeOffset.UtcNow;

            return await _seasonRepository.Get(
                    filter: season => season.OnSaleDate <= now
                    && season.StartDate >= now
                )
                .Select(s => new SeasonItemDTO
                {
                    Id = s.Id,
                    BannerImageUrl = s.BannerImageUrl,
                    ExternalSeasonKey = s.ExternalSeasonKey,
                })
                .FirstOrDefaultAsync();
        }

        public async Task<long?> GetSeasonIdByExternalKeyAsync(string externalSeasonKey)
        {
            return await _seasonRepository.GetSeasonIdByExternalSeasonKeyAsync(externalSeasonKey);
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
