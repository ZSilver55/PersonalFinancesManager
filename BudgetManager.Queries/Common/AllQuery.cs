using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Generic "get everything" query that works for any aggregate type,
    /// e.g. AllQuery&lt;Account&gt;, AllQuery&lt;Goal&gt;, AllQuery&lt;Merchant&gt;.
    /// </summary>
    public class AllQuery<T> : JsonQuery<T>, IQueryAll<IEnumerable<T>> where T : Aggregate
    {
        public AllQuery(IJsonStore<T> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<T>>> ExecuteQueryAsync()
        {
            var items = await Store.ReadAllAsync();
            return QueryResult<IEnumerable<T>>.OK(items);
        }
    }
}
