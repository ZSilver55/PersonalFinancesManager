using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using Dapper;
using Microsoft.Extensions.Options;

namespace BudgetManager.Queries.Common.SQL
{
    /// <summary>
    /// SQL Server implementation of <see cref="IEntityStore{T}"/>. Each aggregate type gets its
    /// own table with (Id, OwnerUserId, Data) where Data is the entity serialized as JSON.
    ///
    /// Every operation is scoped to <see cref="ICurrentUser.UserId"/>: reads filter by owner,
    /// writes stamp the owner (ignoring any client-supplied value), and find/delete require a
    /// matching owner. This is the per-user isolation boundary — a caller can never read or
    /// modify another user's data. Tables are created on first use.
    /// </summary>
    public class SqlEntityStore<T> : IEntityStore<T> where T : Aggregate
    {
        private static readonly ConcurrentDictionary<string, bool> _ensured = new();
        private static readonly SemaphoreSlim _ensureGate = new(1, 1);

        private readonly IDbConnectionFactory _factory;
        private readonly string _connectionString;
        private readonly ICurrentUser _currentUser;
        private readonly string _table = typeof(T).Name;

        public SqlEntityStore(IDbConnectionFactory factory, IOptions<Settings> settings, ICurrentUser currentUser)
        {
            _factory = factory;
            _currentUser = currentUser;
            _connectionString = settings.Value.ConnectionString
                ?? throw new InvalidOperationException("A ConnectionString is required for SQL persistence.");
        }

        private Guid Owner => _currentUser.UserId;

        public async Task<List<T>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            var rows = await conn.QueryAsync<string>(
                $"SELECT Data FROM [{_table}] WHERE OwnerUserId = @owner", new { owner = Owner });
            return rows.Select(Deserialize).Where(x => x is not null).Select(x => x!).ToList();
        }

        public async IAsyncEnumerable<T> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in await ReadAllAsync(cancellationToken))
                yield return item;
        }

        public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            var json = await conn.QueryFirstOrDefaultAsync<string>(
                $"SELECT Data FROM [{_table}] WHERE Id = @id AND OwnerUserId = @owner", new { id, owner = Owner });
            return json is null ? null : Deserialize(json);
        }

        public async Task UpsertAsync(T item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();

            await using var conn = await OpenAsync(cancellationToken);
            await UpsertAsync(conn, null, item);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            int affected = await conn.ExecuteAsync(
                $"DELETE FROM [{_table}] WHERE Id = @id AND OwnerUserId = @owner", new { id, owner = Owner });
            return affected > 0;
        }

        public async Task WriteAllAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            foreach (var item in items)
            {
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                await UpsertAsync(conn, tx, item);
            }
            await tx.CommitAsync(cancellationToken);
        }

        // --- helpers ---

        private async Task UpsertAsync(DbConnection conn, DbTransaction? tx, T item)
        {
            // Stamp ownership from the current user, never from the incoming payload.
            item.OwnerUserId = Owner;
            // Atomic update-or-insert (idempotent). Keyed by Id AND owner so it stays within the
            // per-user boundary; HOLDLOCK closes the update/insert race.
            var sql =
                $"MERGE [{_table}] WITH (HOLDLOCK) AS t " +
                $"USING (SELECT @id AS Id, @owner AS OwnerUserId) AS s ON (t.Id = s.Id AND t.OwnerUserId = s.OwnerUserId) " +
                $"WHEN MATCHED THEN UPDATE SET Data = @data " +
                $"WHEN NOT MATCHED THEN INSERT (Id, OwnerUserId, Data) VALUES (@id, @owner, @data);";
            await conn.ExecuteAsync(sql, new { id = item.Id, owner = Owner, data = Serialize(item) }, tx);
        }

        private async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var raw = _factory.Create(_connectionString);
            if (raw is not DbConnection conn)
            {
                raw.Dispose();
                throw new InvalidOperationException("SQL persistence requires a DbConnection-based provider.");
            }

            await conn.OpenAsync(cancellationToken);
            await EnsureTableAsync(conn);
            return conn;
        }

        private async Task EnsureTableAsync(DbConnection conn)
        {
            if (_ensured.ContainsKey(_table)) return;
            await _ensureGate.WaitAsync();
            try
            {
                if (_ensured.ContainsKey(_table)) return;

                // Table/column names come from our own type names, never user input.
                var ddl =
                    $"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = @name) " +
                    $"BEGIN " +
                    $"  CREATE TABLE [{_table}] (" +
                    $"    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_{_table} PRIMARY KEY, " +
                    $"    OwnerUserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_{_table}_Owner DEFAULT('00000000-0000-0000-0000-000000000000'), " +
                    $"    Data NVARCHAR(MAX) NOT NULL" +
                    $"  ); " +
                    $"  CREATE INDEX IX_{_table}_Owner ON [{_table}](OwnerUserId); " +
                    $"END";
                await conn.ExecuteAsync(ddl, new { name = _table });
                _ensured[_table] = true;
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private static string Serialize(T item) => JsonSerializer.Serialize(item, JsonSerialization.Options);
        private static T? Deserialize(string json) => JsonSerializer.Deserialize<T>(json, JsonSerialization.Options);
    }
}
