using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Storage abstraction for one aggregate type. Implemented by the local JSON file store
    /// and the SQL store, and shared by both the query (read) and command (write) sides so the
    /// backing store can be swapped by configuration.
    /// </summary>
    public interface IEntityStore<T> where T : Aggregate
    {
        /// <summary>Reads every item. Returns an empty list when there is no data yet.</summary>
        Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams items one at a time instead of materializing the whole collection. Prefer this
        /// for filtered reads so only the matches are retained in memory.
        /// </summary>
        IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Replaces the stored collection with the supplied items.</summary>
        Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);

        /// <summary>Returns the item with the given id, or null when not found.</summary>
        Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Inserts the item, or replaces the existing item with the same id.</summary>
        Task UpsertAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>Removes the item with the given id. Returns true when something was removed.</summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
