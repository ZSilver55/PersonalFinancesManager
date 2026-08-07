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
    /// Multi-tenancy: every user's files live under a per-user subfolder "users/{ownerId}"
    /// (the empty owner — desktop offline / auth disabled — uses "users/{empty}"), so the base
    /// path itself holds only cross-cutting files (Users.json, settings.json) and the users folder.
    /// The path is resolved per operation, so a single shared instance routes each request to the
    /// right user's folder.
    ///
    /// Legacy base-level entity files (from before the per-user layout) are relocated once into the
    /// empty-owner folder on first use, so existing data isn't lost.
    ///
    /// Writes are serialized per-file with a SemaphoreSlim and performed atomically
    /// (temp file + replace) so a crash mid-write cannot corrupt existing data.
    /// </summary>
    public class JsonFileStore<T> : IEntityStore<T> where T : Aggregate
    {
        // One gate per physical file, shared across every instance/type that targets it.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _gates =
            new(StringComparer.OrdinalIgnoreCase);

        // Legacy relocation runs once per process.
        private static int _legacyMigrated;

        private readonly string _baseDir;
        private readonly ICurrentUser _currentUser;

        public JsonFileStore(IOptions<Settings> settings, ICurrentUser currentUser)
        {
            _baseDir = JsonStoreLocation.EnsureDirectory(settings?.Value);
            _currentUser = currentUser;
            MigrateLegacyBaseFilesOnce();
        }

        /// <summary>The absolute path of the JSON file backing the current user's data.</summary>
        public string FilePath => ResolveFilePath();

        /// <summary>
        /// Resolves the backing file for the current owner under users/{ownerId}, creating the
        /// folder on demand.
        /// </summary>
        private string ResolveFilePath()
        {
            Guid owner = _currentUser?.UserId ?? Guid.Empty;
            string dir = Path.Combine(_baseDir, "users", owner.ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{typeof(T).Name}.json");
        }

        /// <summary>
        /// Moves pre-per-user base-level entity files into the empty-owner folder, so the base path
        /// keeps only Users.json / settings.json and the users/ folder. Runs once per process,
        /// best-effort.
        /// </summary>
        private void MigrateLegacyBaseFilesOnce()
        {
            if (Interlocked.Exchange(ref _legacyMigrated, 1) == 1) return;
            try
            {
                string emptyDir = Path.Combine(_baseDir, "users", Guid.Empty.ToString("N"));
                foreach (var file in Directory.GetFiles(_baseDir, "*.json"))
                {
                    var name = Path.GetFileName(file);
                    if (name.Equals("Users.json", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("settings.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Directory.CreateDirectory(emptyDir);
                    var dest = Path.Combine(emptyDir, name);
                    if (!File.Exists(dest)) File.Move(file, dest);
                }
            }
            catch
            {
                // Best-effort: if relocation fails, the store still works against the new paths.
            }
        }

        // Gate for a specific physical file (created on demand, shared across instances/types).
        private static SemaphoreSlim GateFor(string filePath) =>
            _gates.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

        public async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            string filePath = ResolveFilePath();
            var gate = GateFor(filePath);
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await ReadUnlockedAsync(filePath, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            string filePath = ResolveFilePath();
            var gate = GateFor(filePath);
            await gate.WaitAsync(cancellationToken);
            try
            {
                await WriteUnlockedAsync(filePath, items.ToList(), cancellationToken);
            }
            finally
            {
                gate.Release();
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
            string filePath = ResolveFilePath();
            var gate = GateFor(filePath);
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                    yield break;

                await using var stream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                gate.Release();
            }
        }

        public async Task UpsertAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            string filePath = ResolveFilePath();
            var gate = GateFor(filePath);
            await gate.WaitAsync(cancellationToken);
            try
            {
                var items = await ReadUnlockedAsync(filePath, cancellationToken);
                int index = items.FindIndex(x => x.Id == item.Id);
                if (index >= 0)
                    items[index] = item;
                else
                    items.Add(item);

                await WriteUnlockedAsync(filePath, items, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string filePath = ResolveFilePath();
            var gate = GateFor(filePath);
            await gate.WaitAsync(cancellationToken);
            try
            {
                var items = await ReadUnlockedAsync(filePath, cancellationToken);
                int removed = items.RemoveAll(x => x.Id == id);
                if (removed == 0)
                    return false;

                await WriteUnlockedAsync(filePath, items, cancellationToken);
                return true;
            }
            finally
            {
                gate.Release();
            }
        }

        // --- helpers (assume the gate for filePath is already held) ---

        private static async Task<List<T>> ReadUnlockedAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
                return new List<T>();

            await using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (stream.Length == 0)
                return new List<T>();

            var items = await JsonSerializer.DeserializeAsync<List<T>>(
                stream, JsonSerialization.Options, cancellationToken);

            return items ?? new List<T>();
        }

        private static async Task WriteUnlockedAsync(string filePath, List<T> items, CancellationToken cancellationToken)
        {
            // Write to a temp file first, then atomically replace the target.
            string tempPath = filePath + ".tmp";

            await using (var stream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, items, JsonSerialization.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, destinationBackupFileName: null);
            else
                File.Move(tempPath, filePath);
        }
    }
}
