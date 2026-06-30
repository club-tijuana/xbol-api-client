using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Odasoft.XBOL.Data;

namespace Odasoft.XBOL.Business.Services
{
    public class SeatManagementService(
        XBOLDbContext dbContext,
        ITicketingClient ticketingClient,
        ILogger<SeatManagementService> logger)
    {
        public async Task<string?> GetScheduleExternalKeyAsync(long id)
        {
            var externalKey = await dbContext.EventSchedules
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => s.ExternalEventKey)
                .FirstOrDefaultAsync();

            return externalKey;
        }
    }
}
