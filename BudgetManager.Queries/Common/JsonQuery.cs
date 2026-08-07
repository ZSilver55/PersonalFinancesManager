using BudgetManager.Domain;
using BudgetManager.Queries.Common.SQL;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Base class for read-side queries backed by a local JSON file.
    /// The JSON counterpart to <see cref="BaseRepository"/> on the SQL side.
    /// </summary>
    public abstract class JsonQuery<T> where T : Aggregate
    {
        protected readonly IJsonStore<T> Store;

        protected JsonQuery(IJsonStore<T> store)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
        }
    }
}
