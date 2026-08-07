using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using Microsoft.Extensions.Options;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// One-time copy of existing local JSON data into SQL, so switching persistence to SQL
    /// carries the user's data over instead of starting empty. Idempotent per entity (upsert
    /// by Id), so re-running is safe.
    /// </summary>
    public class StoreImportService
    {
        private readonly IOptions<Settings> _options;
        private readonly IDbConnectionFactory _factory;

        public StoreImportService(IOptions<Settings> options, IDbConnectionFactory factory)
        {
            _options = options;
            _factory = factory;
        }

        /// <summary>Copies every aggregate type's JSON records into SQL. Returns the total copied.</summary>
        public async Task<int> ImportJsonToSqlAsync()
        {
            int total = 0;
            total += await CopyAsync<Profile>();
            total += await CopyAsync<Account>();
            total += await CopyAsync<Transaction>();
            total += await CopyAsync<Category>();
            total += await CopyAsync<Goal>();
            total += await CopyAsync<Merchant>();
            total += await CopyAsync<RecurringTransaction>();
            return total;
        }

        private async Task<int> CopyAsync<T>() where T : Aggregate
        {
            var json = new JsonFileStore<T>(_options);
            var sql = new SqlEntityStore<T>(_factory, _options);

            var items = await json.ReadAllAsync();
            foreach (var item in items)
                await sql.UpsertAsync(item);

            return items.Count;
        }
    }
}
