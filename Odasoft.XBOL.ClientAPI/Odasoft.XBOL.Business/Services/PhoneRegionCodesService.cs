using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Data.Repositories;
using Odasoft.XBOL.DTO.Results;

namespace Odasoft.XBOL.Business.Services
{
    public class PhoneRegionCodesService
    {
        private readonly PhoneRegionCodeRepository _phoneRegionCodeRepository;

        private readonly List<string> _allowedRegionCodes = new List<string> { "CA", "MX", "US" };

        public PhoneRegionCodesService(PhoneRegionCodeRepository phoneRegionCodeRepository)
        {
            _phoneRegionCodeRepository = phoneRegionCodeRepository;
        }

        public async Task<List<PhoneRegionCodeResponse>> GetPhoneRegionCodesAsync()
        {
            var result = await _phoneRegionCodeRepository
                                    .Get()
                                    .AsNoTracking()
                                    .Where(x => _allowedRegionCodes.Contains(x.RegionCode))
                                    .Select(c => new PhoneRegionCodeResponse
                                    {
                                        Id = c.Id,
                                        RegionCode = c.RegionCode,
                                        DialCode = c.DialCode,
                                        FlagEmoji = c.FlagEmoji
                                    }).ToListAsync();

            return result;
        }
    }
}
