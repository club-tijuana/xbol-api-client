using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Odasoft.XBOL.Models;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Odasoft.XBOL.Data.Repositories
{
    public class BaseRepository<M> where M : BaseModel
    {
        protected DbContext DbContext { get; set; }
        protected readonly DbSet<M> DbSet;
        protected bool Disposed;

        public BaseRepository(DbContext dbContext)
        {
            DbContext = dbContext;
            DbSet = DbContext.Set<M>();
            Disposed = false;
        }

        public void Commit()
        {
            DbContext.SaveChanges();
        }

        public async Task CommitAsync()
        {
            await DbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IQueryable<M> Get(
            Expression<Func<M, bool>>? filter = null,
            Func<IQueryable<M>, IOrderedQueryable<M>>? orderBy = null,
            int? pageSize = null,
            int? currentPage = null,
            params string[] includedProperties)
        {
            return GetQuery(filter, orderBy, pageSize, currentPage, includedProperties)
                .AsNoTracking();
        }

        public void Insert(M entity)
        {
            DbSet.Add(entity);
        }

        public async Task InsertAsync(M entity)
        {
            await DbSet.AddAsync(entity);
        }

        public virtual async Task Update(M entity)
        {
            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                var key = GetPrimaryKeys(entity);
                var currentEntry = await GetByIds(key);

                if (currentEntry != null)
                {
                    var attachedEntry = DbContext.Entry(currentEntry);
                    attachedEntry.CurrentValues.SetValues(entity);
                }
                else
                {
                    DbSet.Attach(entity);
                    DbContext.Entry(entity).State = EntityState.Modified;
                }
            }
            else if (entry.State == EntityState.Unchanged)
            {
                DbContext.Entry(entity).State = EntityState.Modified;
            }
        }

        public virtual void UpdateSync(M entity, bool commitChanges = true)
        {
            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                var key = GetPrimaryKeys(entity)[0];
                var currentEntry = GetByIdSync(key);

                if (currentEntry != null)
                {
                    var attachedEntry = DbContext.Entry(currentEntry);
                    attachedEntry.CurrentValues.SetValues(entity);
                }
                else
                {
                    DbSet.Attach(entity);
                    DbContext.Entry(entity).State = EntityState.Modified;
                }
            }
            else if (entry.State == EntityState.Unchanged)
            {
                DbContext.Entry(entity).State = EntityState.Modified;
            }

            if (commitChanges && entry.State == EntityState.Modified)
            {
                DbContext.SaveChanges();
            }
        }

        public async Task UpdateAsync(M entity)
        {
            var entry = DbContext.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                var key = GetPrimaryKeys(entity);
                var currentEntry = await GetByIds(key);

                if (currentEntry != null)
                {
                    var attachedEntry = DbContext.Entry(currentEntry);
                    attachedEntry.CurrentValues.SetValues(entity);
                    entry = attachedEntry;
                }
                else
                {
                    DbSet.Attach(entity);
                    DbContext.Entry(entity).State = EntityState.Modified;
                }
            }

            if (entry.State == EntityState.Unchanged)
            {
                DbContext.Entry(entity).State = EntityState.Modified;
                return;
            }

            if (entry.State == EntityState.Modified)
            {
                await DbContext.SaveChangesAsync();
            }
        }

        public void HardDelete(M entity)
        {
            if (DbContext.Entry(entity).State == EntityState.Detached)
            {
                DbSet.Attach(entity);
            }

            DbSet.Remove(entity);
        }

        public async Task HardDeleteAsync(M entity)
        {
            if (DbContext.Entry(entity).State == EntityState.Detached)
            {
                DbSet.Attach(entity);
            }

            DbSet.Remove(entity);
            await DbContext.SaveChangesAsync();
        }

        public virtual async Task<M?> GetByIds(params object[] ids)
        {
            return await DbSet.FindAsync(ids);
        }

        public virtual M? GetByIdSync(object id)
        {
            return DbSet.Find(id);
        }

        public async Task<M?> GetByIdAsync(long id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual IQueryable<M> GetQuery(
            Expression<Func<M, bool>>? filter = null,
            Func<IQueryable<M>, IOrderedQueryable<M>>? orderBy = null,
            int? skip = null,
            int? take = null,
            params string[] includedProperties)
        {
            IQueryable<M> query = DbSet.AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter).AsNoTracking();
            }

            foreach (var includedProperty in includedProperties)
            {
                query = query.Include(includedProperty).AsNoTracking();
            }

            if (orderBy != null)
            {
                query = orderBy(query).AsNoTracking();
            }

            if (skip.HasValue && take.HasValue)
            {
                query = query.Skip(skip.Value).Take(take.Value);
            }

            return query.AsNoTracking();
        }

        public async Task<IEnumerable<N>> ExecuteStoredProcedureValues<N>(
            string query,
            Dictionary<string, object> parameters,
            string? connectionString = null)
        {
            using IDbConnection connection = GetConnection(connectionString);
            connection.Open();

            var items = await connection.QueryAsync<N>(
                $"{query}",
                GetDynamicParameters(parameters),
                commandType: CommandType.StoredProcedure,
                commandTimeout: 0
            );

            connection.Close();
            return items;
        }

        public IEnumerable<N> ExecuteStoredProcedureValues<N>(
            string query,
            CommandType commandType,
            string? connectionString = null)
        {
            return ExecuteStoredProcedureValuesSync<N>(
                query,
                commandType,
                new Dictionary<string, object>(),
                connectionString
            );
        }

        public IEnumerable<N> ExecuteStoredProcedureValuesSync<N>(
            string query,
            CommandType commandType,
            Dictionary<string, object> parameters,
            string? connectionString = null)
        {
            using IDbConnection connection = GetConnection(connectionString);
            connection.Open();

            var items = connection.Query<N>(
                $"{query}",
                GetDynamicParameters(parameters),
                commandType: commandType,
                commandTimeout: 0
            );

            connection.Close();
            return items;
        }

        public void ExecuteQuerySync(
            string query,
            string? connectionString = null,
            int? commandTimeout = null)
        {
            using IDbConnection connection = GetConnection(connectionString);
            connection.Open();
            var items = connection.Execute($"{query}", commandTimeout: commandTimeout);
            connection.Close();
        }

        public async Task<IEnumerable<N>> ExecuteStoredProcedureValues<N>(
            string query,
            object parameters,
            string? connectionString = null)
        {
            return await ExecuteStoredProcedureValues<N>(
                query,
                GetDictionaryParameters(parameters),
                connectionString
            );
        }

        protected Dictionary<string, object> GetDictionaryParameters(object parameters)
        {
            var sqlParameters = new Dictionary<string, object>();

            foreach (PropertyInfo prop in parameters.GetType().GetProperties())
            {
                var value = prop.GetValue(parameters, null);
                sqlParameters.Add(prop.Name, value ?? DBNull.Value);
            }

            return sqlParameters;
        }

        protected DynamicParameters GetDynamicParameters(Dictionary<string, object> parameters)
        {
            var sqlParameters = new DynamicParameters();

            foreach (var pair in parameters)
            {
                if (pair.Value is DataTable)
                {
                    sqlParameters.Add(pair.Key, ((DataTable)pair.Value).AsTableValuedParameter());
                }
                else
                {
                    sqlParameters.Add(pair.Key, pair.Value);
                }
            }

            return sqlParameters;
        }

        public IDbConnection GetConnection(string? connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var connection = DbContext.Database.GetDbConnection();
                connectionString = connection.ConnectionString;
            }

            return new NpgsqlConnection(connectionString);
        }

        protected void Dispose(bool disposing)
        {
            if (!Disposed && disposing)
            {
                DbContext.Dispose();
            }

            Disposed = true;
        }

        private object[] GetPrimaryKeys(M entity)
        {
            var keyNames = GetKeyNames();
            Type type = typeof(M);
            var keys = new object[keyNames.Length];

            for (int i = 0; i < keyNames.Length; i++)
            {
                keys[i] = type.GetProperty(keyNames[i]).GetValue(entity, null);
            }

            return keys;
        }

        private string[] GetKeyNames()
        {
            return DbContext
                .Model.FindEntityType(typeof(M))
                .FindPrimaryKey()
                .Properties.Select(x => x.Name)
                .ToArray();
        }
    }
}
