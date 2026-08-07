using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;
using Microsoft.Extensions.Options;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// Copies all data between the local JSON store and the remote API store when the desktop
    /// switches between offline and online. Builds the source/target stores from a Settings
    /// snapshot (so a freshly entered ApiBaseUrl is honored without needing an app restart to
    /// perform the copy). Copy is upsert-by-Id, so it's safe to re-run.
    /// </summary>
    public class DataSourceSwitchService
    {
        public async Task<int> MigrateAsync(Settings settings, PersistenceMode from, PersistenceMode to)
        {
            if (from == to) return 0;

            using var http = BuildHttpClient(settings, from, to);
            int total = 0;
            total += await CopyAsync<Profile>(settings, from, to, http);
            total += await CopyAsync<Account>(settings, from, to, http);
            total += await CopyAsync<Transaction>(settings, from, to, http);
            total += await CopyAsync<Category>(settings, from, to, http);
            total += await CopyAsync<Goal>(settings, from, to, http);
            total += await CopyAsync<Merchant>(settings, from, to, http);
            total += await CopyAsync<RecurringTransaction>(settings, from, to, http);
            return total;
        }

        private static HttpClient? BuildHttpClient(Settings settings, PersistenceMode from, PersistenceMode to)
        {
            if (from != PersistenceMode.Api && to != PersistenceMode.Api)
                return null;

            var url = settings.ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("An API address is required for online mode.");
            if (!url.EndsWith('/')) url += "/";
            return new HttpClient { BaseAddress = new Uri(url) };
        }

        private static IEntityStore<T> Build<T>(PersistenceMode mode, Settings settings, HttpClient? http) where T : Aggregate
            => mode == PersistenceMode.Api
                ? new ApiEntityStore<T>(http!)
                : new JsonFileStore<T>(Options.Create(settings), new SystemCurrentUser());

        private static async Task<int> CopyAsync<T>(Settings settings, PersistenceMode from, PersistenceMode to, HttpClient? http)
            where T : Aggregate
        {
            var source = Build<T>(from, settings, http);
            var target = Build<T>(to, settings, http);

            var items = await source.ReadAllAsync();
            foreach (var item in items)
                await target.UpsertAsync(item);

            return items.Count;
        }
    }
}
