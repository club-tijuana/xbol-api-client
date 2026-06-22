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

        public Task<List<Client>> GetImportedClaimCandidatesAsync()
        {
            return DbContext.Set<Client>()
                .Include(x => x.PhoneRegionCode)
                .Where(x => x.IsActive
                    && (x.FirebaseUid == null || x.FirebaseUid == string.Empty)
                    && !x.LoginIdentifiers.Any()
                    && (x.Orders.Any()
                        || DbContext.Set<Ticket>().Any(ticket =>
                            ticket.OriginalClientId == x.Id || ticket.CurrentClientId == x.Id)
                        || DbContext.Set<BundlePass>().Any(bundlePass =>
                            bundlePass.ClientId == x.Id)))
                .ToListAsync();
        }

    }
}
