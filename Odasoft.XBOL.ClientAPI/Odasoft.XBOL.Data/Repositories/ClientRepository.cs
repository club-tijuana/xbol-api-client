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
    }
}
