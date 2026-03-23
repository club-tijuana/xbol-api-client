using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO;

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
                )
                .Select(s => new SeasonItemDTO
                {
                    Id = s.Id,
                    BannerImageUrl = s.BannerImageUrl,
                    ExternalSeasonKey = s.ExternalSeasonKey,
                })
                .FirstOrDefaultAsync();
        }
    }
}
