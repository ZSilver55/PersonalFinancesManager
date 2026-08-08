using System.Data.Common;
using System.Text.Json;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using Dapper;
using Microsoft.Extensions.Options;

namespace BudgetManager.Api.Settings
{
    /// <summary>
    /// Per-user preference storage (currency, language, safe-to-spend). Owner-scoped via
    /// <see cref="ICurrentUser"/>, so each account keeps its own settings. Infrastructure settings
    /// (persistence mode, connection string, API address) are NOT stored here — those belong to the
    /// client/host configuration.
    /// </summary>
    public interface IUserSettingsStore
    {
        Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    }

    /// <summary>JSON-backed: one settings.json per user under users/{owner}.</summary>
    public sealed class JsonUserSettingsStore : IUserSettingsStore
    {
        private readonly IOptions<Domain.Settings> _settings;
        private readonly ICurrentUser _currentUser;

        public JsonUserSettingsStore(IOptions<Domain.Settings> settings, ICurrentUser currentUser)
        {
            _settings = settings;
            _currentUser = currentUser;
        }

        private string PathFor() =>
            System.IO.Path.Combine(
                JsonStoreLocation.EnsureUserDirectory(_settings.Value, _currentUser.UserId), "settings.json");

        public async Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default)
        {
            var path = PathFor();
            if (!File.Exists(path)) return new UserPreferences();
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length == 0) return new UserPreferences();
            return await JsonSerializer.DeserializeAsync<UserPreferences>(stream, JsonSerialization.Options, cancellationToken)
                   ?? new UserPreferences();
        }

        public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            var path = PathFor();
            var temp = path + ".tmp";
            await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, preferences, JsonSerialization.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }

    /// <summary>SQL-backed: a UserSettings row (OwnerUserId, Data) per user, created on first use.</summary>
    public sealed class SqlUserSettingsStore : IUserSettingsStore
    {
        private static bool _ensured;
        private static readonly SemaphoreSlim _ensureGate = new(1, 1);

        private readonly IDbConnectionFactory _factory;
        private readonly string _connectionString;
        private readonly ICurrentUser _currentUser;

        public SqlUserSettingsStore(IDbConnectionFactory factory, IOptions<Domain.Settings> settings, ICurrentUser currentUser)
        {
            _factory = factory;
            _currentUser = currentUser;
            _connectionString = settings.Value.ConnectionString
                ?? throw new InvalidOperationException("A ConnectionString is required for SQL persistence.");
        }

        public async Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            var json = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT Data FROM [UserSettings] WHERE OwnerUserId = @owner", new { owner = _currentUser.UserId });
            return json is null
                ? new UserPreferences()
                : JsonSerializer.Deserialize<UserPreferences>(json, JsonSerialization.Options) ?? new UserPreferences();
        }

        public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken);
            var data = JsonSerializer.Serialize(preferences, JsonSerialization.Options);
            // Atomic update-or-insert by owner (idempotent; HOLDLOCK avoids the update/insert race).
            const string sql =
                "MERGE [UserSettings] WITH (HOLDLOCK) AS t " +
                "USING (SELECT @owner AS OwnerUserId) AS s ON (t.OwnerUserId = s.OwnerUserId) " +
                "WHEN MATCHED THEN UPDATE SET Data=@data " +
                "WHEN NOT MATCHED THEN INSERT (OwnerUserId, Data) VALUES (@owner, @data);";
            await conn.ExecuteAsync(sql, new { owner = _currentUser.UserId, data });
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

        private static async Task EnsureTableAsync(DbConnection conn)
        {
            if (_ensured) return;
            await _ensureGate.WaitAsync();
            try
            {
                if (_ensured) return;
                await conn.ExecuteAsync(
                    "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserSettings') " +
                    "CREATE TABLE [UserSettings] (" +
                    "  OwnerUserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserSettings PRIMARY KEY, " +
                    "  Data NVARCHAR(MAX) NOT NULL);");
                _ensured = true;
            }
            finally
            {
                _ensureGate.Release();
            }
        }
    }
}
