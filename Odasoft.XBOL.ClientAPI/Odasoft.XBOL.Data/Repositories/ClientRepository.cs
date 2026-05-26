using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class ClientRepository(XBOLDbContext dbContext) : BaseRepository<Client>(dbContext)
    {
        public Task<Client?> GetByFirebaseUidAsync(string firebaseUid)
        {
            return DbContext.Set<Client>()
                .Include(x => x.PhoneRegionCode)
                .FirstOrDefaultAsync(x => x.FirebaseUid == firebaseUid);
        }

        public Task<Client?> GetByContactPhoneNumberAsync(string phoneNumber)
        {
            var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
            if (normalizedPhoneNumber.Length == 0)
            {
                return Task.FromResult<Client?>(null);
            }

            return DbContext.Set<Client>()
                .Include(x => x.PhoneRegionCode)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber != null
                    && x.FirebaseUid == null
                    && (x.PhoneNumber
                            .Replace("+", "")
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("(", "")
                            .Replace(")", "") == normalizedPhoneNumber
                        || (x.PhoneRegionCode != null
                            && x.PhoneRegionCode.DialCode.Replace("+", "") + x.PhoneNumber
                                .Replace("+", "")
                                .Replace(" ", "")
                                .Replace("-", "")
                                .Replace("(", "")
                                .Replace(")", "") == normalizedPhoneNumber)));
        }

        private static string NormalizePhoneNumber(string value)
        {
            return new string(value.Where(char.IsDigit).ToArray());
        }
    }
}
