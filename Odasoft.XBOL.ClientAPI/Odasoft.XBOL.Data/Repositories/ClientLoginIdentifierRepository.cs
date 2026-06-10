using Microsoft.EntityFrameworkCore;
using Odasoft.XBOL.Commons.Enums;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public sealed class ClientLoginIdentifierRepository(XBOLDbContext dbContext)
        : BaseRepository<ClientLoginIdentifier>(dbContext)
    {
        public async Task<IReadOnlyList<ClientLoginIdentifier>> GetVerifiedMatchesAsync(
            IEnumerable<ClientLoginIdentifierLookup> lookups)
        {
            var matches = new List<ClientLoginIdentifier>();
            foreach (var lookup in lookups.Distinct())
            {
                matches.AddRange(await DbContext.Set<ClientLoginIdentifier>()
                    .Include(x => x.Client)
                    .ThenInclude(x => x.PhoneRegionCode)
                    .Where(x => x.Type == lookup.Type
                        && x.NormalizedValue == lookup.NormalizedValue)
                    .ToListAsync());
            }

            return matches;
        }

        public async Task AddMissingAsync(
            Client client,
            IEnumerable<ClientLoginIdentifierLookup> lookups,
            DateTimeOffset verifiedAt)
        {
            var existing = await DbContext.Set<ClientLoginIdentifier>()
                .Where(x => x.ClientId == client.Id)
                .Select(x => new { x.Type, x.NormalizedValue })
                .ToListAsync();
            var existingSet = existing
                .Select(x => new ClientLoginIdentifierLookup(x.Type, x.NormalizedValue))
                .ToHashSet();

            var newIdentifiers = lookups
                .Distinct()
                .Where(lookup => !existingSet.Contains(lookup))
                .Select(lookup => new ClientLoginIdentifier
                {
                    Client = client,
                    ClientId = client.Id,
                    Type = lookup.Type,
                    NormalizedValue = lookup.NormalizedValue,
                    VerifiedAt = verifiedAt,
                    CreatedAt = verifiedAt,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = verifiedAt,
                    UpdatedBy = Guid.Empty
                })
                .ToList();

            if (newIdentifiers.Count > 0)
            {
                await DbContext.Set<ClientLoginIdentifier>().AddRangeAsync(newIdentifiers);
            }
        }

        public async Task<IReadOnlyList<ClientLoginIdentifierLookup>> GetClientLookupsAsync(long clientId)
        {
            return await DbContext.Set<ClientLoginIdentifier>()
                .Where(x => x.ClientId == clientId)
                .Select(x => new ClientLoginIdentifierLookup(x.Type, x.NormalizedValue))
                .ToListAsync();
        }

        public void DetachPendingClientIdentityChanges()
        {
            foreach (var entry in DbContext.ChangeTracker.Entries()
                .Where(entry =>
                    entry.State != EntityState.Unchanged
                    && entry.State != EntityState.Detached
                    && (entry.Entity is Client || entry.Entity is ClientLoginIdentifier)))
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    public sealed record ClientLoginIdentifierLookup(
        ClientLoginIdentifierType Type,
        string NormalizedValue);
}
