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
        protected readonly IEntityStore<T> Store;

        protected JsonQuery(IEntityStore<T> store)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
        }
    }
}
