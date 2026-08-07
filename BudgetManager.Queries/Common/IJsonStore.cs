using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Abstraction over a single-entity local JSON file (one file per aggregate type,
    /// stored under %AppData%\BudgetManager). Shared by both the query (read) side and
    /// the command (write) side, mirroring the role IDbConnectionFactory plays for SQL.
    /// </summary>
    public interface IJsonStore<T> where T : Aggregate
    {
        /// <summary>Reads every item from the backing file. Returns an empty list if the file does not exist yet.</summary>
        Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams items from the backing file one at a time instead of materializing the
        /// whole collection. Prefer this for filtered reads so only the matches are retained
        /// in memory. The file lock is held for the duration of the enumeration.
        /// </summary>
        IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Overwrites the backing file with the supplied collection.</summary>
        Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);

        /// <summary>Returns the item with the given id, or null when not found.</summary>
        Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Inserts the item, or replaces the existing item with the same id.</summary>
        Task UpsertAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>Removes the item with the given id. Returns true when something was removed.</summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
