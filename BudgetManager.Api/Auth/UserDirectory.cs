using System.Data.Common;
using System.Text.Json;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using Dapper;
using Microsoft.Extensions.Options;

namespace BudgetManager.Api.Auth
{
    /// <summary>
    /// Global directory that maps a logged-in identity to a stable internal user id (the OwnerUserId
    /// used to partition all data). Unlike the entity stores, this is NOT per-user scoped — it is the
    /// registry everything else is scoped by.
    /// </summary>
    public interface IUserDirectory
    {
        /// <summary>
        /// Returns the user for a provider subject, creating one on first login. If a seed row exists
        /// with a matching (verified) email but no subject yet, it is linked (subject backfilled) so
        /// the account inherits that row's id.
        /// </summary>
        Task<User> ResolveAsync(string subject, string? email, string? name, CancellationToken cancellationToken = default);

        /// <summary>Ensures a seed user exists with the given id and email (idempotent).</summary>
        Task EnsureSeedAsync(Guid id, string email, CancellationToken cancellationToken = default);
    }

    /// <summary>Shared resolve/seed logic over primitive read-all/upsert operations.</summary>
    public abstract class UserDirectoryBase : IUserDirectory
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        protected abstract Task<List<User>> ReadAllAsync(CancellationToken cancellationToken);
        protected abstract Task UpsertAsync(User user, CancellationToken cancellationToken);

        public async Task<User> ResolveAsync(string subject, string? email, string? name, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var users = await ReadAllAsync(cancellationToken);

                // 1) Known subject → return it (refresh email/name if they changed).
                var bySubject = users.FirstOrDefault(u => u.Subject == subject);
                if (bySubject is not null)
                {
                    if (!string.IsNullOrEmpty(email) && !string.Equals(bySubject.Email, email, StringComparison.OrdinalIgnoreCase))
                    {
                        bySubject.Email = email;
                        bySubject.Name = name ?? bySubject.Name;
                        await UpsertAsync(bySubject, cancellationToken);
                    }
                    return bySubject;
                }

                // 2) Pre-seeded row with matching email but no subject → link it to this account.
                if (!string.IsNullOrEmpty(email))
                {
                    var bySeedEmail = users.FirstOrDefault(u =>
                        string.IsNullOrEmpty(u.Subject) &&
                        string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                    if (bySeedEmail is not null)
                    {
                        bySeedEmail.Subject = subject;
                        bySeedEmail.Name = name ?? bySeedEmail.Name;
                        await UpsertAsync(bySeedEmail, cancellationToken);
                        return bySeedEmail;
                    }
                }

                // 3) New user.
                var created = new User
                {
                    Id = Guid.NewGuid(),
                    Subject = subject,
                    Email = email,
                    Name = name,
                    CreatedUtc = DateTime.UtcNow
                };
                return await CreateAsync(created, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Persists a brand-new user and returns the stored record. Overridable so a backing store
        /// can make creation idempotent under concurrency (get-or-create by subject) and return the
        /// row that actually won, instead of creating a duplicate.
        /// </summary>
        protected virtual async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
        {
            await UpsertAsync(user, cancellationToken);
            return user;
        }

        public async Task EnsureSeedAsync(Guid id, string email, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                var users = await ReadAllAsync(cancellationToken);
                bool exists = users.Any(u => u.Id == id ||
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
                if (exists) return;

                await UpsertAsync(new User
                {
                    Id = id,
                    Email = email,
                    Subject = null,
                    CreatedUtc = DateTime.UtcNow
                }, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    /// <summary>JSON-backed directory: a single global Users.json in the base data folder.</summary>
    public sealed class JsonUserDirectory : UserDirectoryBase
    {
        private readonly string _path;

        public JsonUserDirectory(IOptions<Domain.Settings> settings)
        {
            _path = Path.Combine(JsonStoreLocation.EnsureDirectory(settings.Value), "Users.json");
        }

        protected override async Task<List<User>> ReadAllAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_path)) return new List<User>();
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length == 0) return new List<User>();
            var users = await JsonSerializer.DeserializeAsync<List<User>>(stream, JsonSerialization.Options, cancellationToken);
            return users ?? new List<User>();
        }

        protected override async Task UpsertAsync(User user, CancellationToken cancellationToken)
        {
            var users = await ReadAllAsync(cancellationToken);
            int index = users.FindIndex(u => u.Id == user.Id);
            if (index >= 0) users[index] = user; else users.Add(user);

            string tempPath = _path + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, users, JsonSerialization.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            if (File.Exists(_path)) File.Replace(tempPath, _path, null);
            else File.Move(tempPath, _path);
        }
    }

    /// <summary>SQL-backed directory: a global Users table (created on first use).</summary>
    public sealed class SqlUserDirectory : UserDirectoryBase
    {
        private static bool _ensured;
        private static readonly SemaphoreSlim _ensureGate = new(1, 1);

        private readonly IDbConnectionFactory _factory;
        private readonly string _connectionString;

        public SqlUserDirectory(IDbConnectionFactory factory, IOptions<Domain.Settings> settings)
        {
            _factory = factory;
            _connectionString = settings.Value.ConnectionString
                ?? throw new InvalidOperationException("A ConnectionString is required for SQL persistence.");
        }

        protected override async Task<List<User>> ReadAllAsync(CancellationToken cancellationToken)
        {
            await using var conn = await OpenAsync(cancellationToken);
            var rows = await conn.QueryAsync<User>(
                "SELECT Id, Subject, Email, Name, CreatedUtc FROM [Users]");
            return rows.ToList();
        }

        protected override async Task UpsertAsync(User user, CancellationToken cancellationToken)
        {
            await using var conn = await OpenAsync(cancellationToken);
            // Atomic update-or-insert by primary key (idempotent; HOLDLOCK avoids the update/insert race).
            const string sql =
                "MERGE [Users] WITH (HOLDLOCK) AS t " +
                "USING (SELECT @Id AS Id) AS s ON (t.Id = s.Id) " +
                "WHEN MATCHED THEN UPDATE SET Subject=@Subject, Email=@Email, Name=@Name, CreatedUtc=@CreatedUtc " +
                "WHEN NOT MATCHED THEN INSERT (Id, Subject, Email, Name, CreatedUtc) " +
                "VALUES (@Id, @Subject, @Email, @Name, @CreatedUtc);";
            await conn.ExecuteAsync(sql, user);
        }

        protected override async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
        {
            // A login-created user always has a subject; make creation idempotent on it. If a row
            // with this subject already exists (a concurrent first login or a retry), no duplicate is
            // inserted and the existing row is returned. HOLDLOCK serializes concurrent inserts, and
            // the unique index on Subject is the final backstop.
            if (string.IsNullOrEmpty(user.Subject))
            {
                await UpsertAsync(user, cancellationToken);
                return user;
            }

            await using var conn = await OpenAsync(cancellationToken);
            const string insertIfAbsent =
                "MERGE [Users] WITH (HOLDLOCK) AS t " +
                "USING (SELECT @Subject AS Subject) AS s ON (t.Subject = s.Subject) " +
                "WHEN NOT MATCHED THEN INSERT (Id, Subject, Email, Name, CreatedUtc) " +
                "VALUES (@Id, @Subject, @Email, @Name, @CreatedUtc);";
            await conn.ExecuteAsync(insertIfAbsent, user);

            var stored = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT Id, Subject, Email, Name, CreatedUtc FROM [Users] WHERE Subject = @Subject",
                new { user.Subject });
            return stored ?? user;
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
                // Idempotent: create the table and each index only if missing, so this is safe on a
                // fresh database and on one that already has the table from an earlier version.
                var ddl =
                    "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users') " +
                    "  CREATE TABLE [Users] (" +
                    "    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY, " +
                    "    Subject NVARCHAR(256) NULL, " +
                    "    Email NVARCHAR(256) NULL, " +
                    "    Name NVARCHAR(256) NULL, " +
                    "    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT(SYSUTCDATETIME())" +
                    "  ); " +
                    // Unique (filtered) so a subject maps to at most one user; seed rows (null subject) are exempt.
                    // Guarded against pre-existing duplicates so the upgrade never hard-fails.
                    "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Subject' AND object_id = OBJECT_ID('Users')) " +
                    "  AND NOT EXISTS (SELECT Subject FROM [Users] WHERE Subject IS NOT NULL GROUP BY Subject HAVING COUNT(*) > 1) " +
                    "  CREATE UNIQUE INDEX UX_Users_Subject ON [Users](Subject) WHERE Subject IS NOT NULL; " +
                    "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users')) " +
                    "  CREATE INDEX IX_Users_Email ON [Users](Email);";
                await conn.ExecuteAsync(ddl);
                _ensured = true;
            }
            finally
            {
                _ensureGate.Release();
            }
        }
    }
}
