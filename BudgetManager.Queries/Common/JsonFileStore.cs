using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BudgetManager.Domain;
using Microsoft.Extensions.Options;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Default <see cref="IEntityStore{T}"/> implementation that persists each aggregate type
    /// to its own JSON file (e.g. Account.json, Transaction.json) under the data directory.
    ///
    /// Location resolution:
    ///   1. Settings.DataDirectory when provided, otherwise
    ///   2. %AppData%\BudgetManager  (Environment.SpecialFolder.ApplicationData).
    ///
    /// Writes are serialized per-file with a SemaphoreSlim and performed atomically
    /// (temp file + replace) so a crash mid-write cannot corrupt existing data.
    /// </summary>
    public class JsonFileStore<T> : IEntityStore<T> where T : Aggregate
    {
        // One gate per physical file, shared across every instance/type that targets it.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _filePath;
        private readonly SemaphoreSlim _gate;

        public JsonFileStore(IOptions<Settings> settings)
        {
            string baseDir = JsonStoreLocation.EnsureDirectory(settings?.Value);

            _filePath = Path.Combine(baseDir, $"{typeof(T).Name}.json");
            _gate = _gates.GetOrAdd(_filePath, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>The absolute path of the JSON file backing this store.</summary>
        public string FilePath => _filePath;

        public async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                return await ReadUnlockedAsync(cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                await WriteUnlockedAsync(items.ToList(), cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Stream and short-circuit on the first match instead of loading the whole file.
            await foreach (var item in StreamAllAsync(cancellationToken))
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }

        public async IAsyncEnumerable<T> StreamAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(_filePath))
                    yield break;

                await using var stream = new FileStream(
                    _filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (stream.Length == 0)
                    yield break;

                // DeserializeAsyncEnumerable reads and yields one element at a time,
                // so the full array is never held in memory at once.
                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
                    stream, JsonSerialization.Options, cancellationToken))
                {
                    if (item is not null)
                        yield return item;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task UpsertAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            await _gate.WaitAsync(cancellationToken);
            try
            {
                var items = await ReadUnlockedAsync(cancellationToken);
                int index = items.FindIndex(x => x.Id == item.Id);
                if (index >= 0)
                    items[index] = item;
                else
                    items.Add(item);

                await WriteUnlockedAsync(items, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var items = await ReadUnlockedAsync(cancellationToken);
                int removed = items.RemoveAll(x => x.Id == id);
                if (removed == 0)
                    return false;

                await WriteUnlockedAsync(items, cancellationToken);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        // --- helpers (assume the gate is already held) ---

        private async Task<List<T>> ReadUnlockedAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
                return new List<T>();

            await using var stream = new FileStream(
                _filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (stream.Length == 0)
                return new List<T>();

            var items = await JsonSerializer.DeserializeAsync<List<T>>(
                stream, JsonSerialization.Options, cancellationToken);

            return items ?? new List<T>();
        }

        private async Task WriteUnlockedAsync(List<T> items, CancellationToken cancellationToken)
        {
            // Write to a temp file first, then atomically replace the target.
            string tempPath = _filePath + ".tmp";

            await using (var stream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, items, JsonSerialization.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, destinationBackupFileName: null);
            else
                File.Move(tempPath, _filePath);
        }
    }
}
