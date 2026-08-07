using System.Net;
using System.Net.Http.Json;
using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// <see cref="IEntityStore{T}"/> implementation that talks to the Web API over HTTP, so the
    /// desktop client can run against a remote server. Uses the API's generic CRUD routes
    /// (/api/{resource}). Upsert checks existence to decide POST vs PUT.
    ///
    /// (Authentication will be added in a later phase by attaching a bearer token to the
    /// shared HttpClient.)
    /// </summary>
    public class ApiEntityStore<T> : IEntityStore<T> where T : Aggregate
    {
        private static readonly IReadOnlyDictionary<Type, string> Routes = new Dictionary<Type, string>
        {
            [typeof(Profile)] = "profiles",
            [typeof(Account)] = "accounts",
            [typeof(Transaction)] = "transactions",
            [typeof(Category)] = "categories",
            [typeof(Goal)] = "goals",
            [typeof(Merchant)] = "merchants",
            [typeof(RecurringTransaction)] = "recurring",
            [typeof(Attachment)] = "attachments"
        };

        private readonly HttpClient _http;
        private readonly string _route;

        public ApiEntityStore(HttpClient http)
        {
            _http = http;
            _route = Routes.TryGetValue(typeof(T), out var r) ? r : typeof(T).Name.ToLowerInvariant() + "s";
        }

        private string Collection => $"api/{_route}";
        private string Item(Guid id) => $"api/{_route}/{id}";

        public async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            var list = await _http.GetFromJsonAsync<List<T>>(Collection, JsonSerialization.Options, cancellationToken);
            return list ?? new List<T>();
        }

        public async IAsyncEnumerable<T> StreamAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in await ReadAllAsync(cancellationToken))
                yield return item;
        }

        public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync(Item(id), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonSerialization.Options, cancellationToken);
        }

        public async Task UpsertAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();

            bool exists = await FindAsync(item.Id, cancellationToken) is not null;
            var response = exists
                ? await _http.PutAsJsonAsync(Item(item.Id), item, JsonSerialization.Options, cancellationToken)
                : await _http.PostAsJsonAsync(Collection, item, JsonSerialization.Options, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var response = await _http.DeleteAsync(Item(id), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            return true;
        }

        public async Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
                await UpsertAsync(item, cancellationToken);
        }
    }
}
