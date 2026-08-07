using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Generic "get by id" query for any aggregate type,
    /// e.g. ByIdQuery&lt;Account&gt;, ByIdQuery&lt;Transaction&gt;.
    /// </summary>
    public class ByIdQuery<T> : JsonQuery<T>, IQuery<T, Guid> where T : Aggregate
    {
        public ByIdQuery(IJsonStore<T> store) : base(store) { }

        public async Task<QueryResult<T>> ExecuteQueryAsync(Guid id)
        {
            var item = await Store.FindAsync(id);
            return item is null
                ? QueryResult<T>.Failed($"{typeof(T).Name} with id '{id}' was not found.")
                : QueryResult<T>.OK(item);
        }
    }
}
