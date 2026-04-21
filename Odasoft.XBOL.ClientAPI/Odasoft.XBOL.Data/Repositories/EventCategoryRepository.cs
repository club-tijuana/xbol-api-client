using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.DTO;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventCategoryRepository(XBOLDbContext dbContext) : BaseRepository<EventCategory>(dbContext)
    {
        public async Task<List<EventCategoryDTO>> GetEventCategories()
        {
            List<EventCategoryDTO> categories = await DbContext.Set<EventCategory>()
                .Where(ec => ec.IsActive)
                .Select(ec => new EventCategoryDTO
                {
                    Id = ec.Id,
                    Name = ec.Name,
                    DisplayName = ec.DisplayName
                })
                .ToListAsync();

            return categories;
        }
    }
}
