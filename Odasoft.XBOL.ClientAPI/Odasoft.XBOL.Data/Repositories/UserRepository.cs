using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class UserRepository(XBOLDbContext context)
    {
        private readonly XBOLDbContext _context = context;

        public Task<User?> GetByFirebaseUidAsync(string firebaseUid)
        {
            return _context.Users.FirstOrDefaultAsync(x => x.FirebaseUid == firebaseUid);
        }
    }
}
