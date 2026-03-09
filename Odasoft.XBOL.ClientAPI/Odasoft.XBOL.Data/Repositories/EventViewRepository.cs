using Dapper;
using Odasoft.XBOL.Models;

namespace Odasoft.XBOL.Data.Repositories
{
    public class EventViewRepository(XBOLDbContext dbContext) : BaseRepository<EventViews>(dbContext)
    {
        public async Task<bool> TryRegisterViewAsync(
            long eventId,
            string visitorId,
            string ipAddress,
            string platform,
            long duplicateMinutes,
            long rateLimitMinutes,
            long maxViews
        )
        {
            const string lockQuery = @"SELECT pg_advisory_xact_lock(hashtext(@IpAddress));";

            const string query = @"
                WITH inserted AS (
                    INSERT INTO ""EventViews""
                    (""EventId"",""VisitorId"",""IpAddress"",""Platform"",""ViewedAt"")
                    SELECT
                        @EventId,
                        @VisitorId,
                        @IpAddress,
                        @Platform,
                        NOW()
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM ""EventViews""
                        WHERE ""EventId"" = @EventId
                        AND ""VisitorId"" = @VisitorId
                        AND ""ViewedAt"" > NOW() - (@DuplicateMinutes * INTERVAL '1 minute')
                    )
                    AND (
                        SELECT COUNT(*)
                        FROM ""EventViews""
                        WHERE ""IpAddress"" = @IpAddress
                        AND ""ViewedAt"" > NOW() - (@RateLimitMinutes * INTERVAL '1 minute')
                    ) < @MaxViews
                    RETURNING ""EventId""
                )
                UPDATE ""Event""
                SET ""ViewCount"" = ""ViewCount"" + 1
                WHERE ""Id"" IN (SELECT ""EventId"" FROM inserted)
                RETURNING ""Id"";";

            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(
                    lockQuery,
                    new { IpAddress = ipAddress },
                    transaction
                );

                var result = await connection.QueryFirstOrDefaultAsync<long?>(
                    query,
                    new
                    {
                        EventId = eventId,
                        VisitorId = visitorId,
                        IpAddress = ipAddress,
                        Platform = platform,
                        DuplicateMinutes = duplicateMinutes,
                        RateLimitMinutes = rateLimitMinutes,
                        MaxViews = maxViews
                    },
                    transaction
                );

                transaction.Commit();

                return result != null;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
