using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// One-time, on-startup upgrade of persisted files when the schema version increases.
    ///
    /// New fields deserialize to their defaults when read, so the app already works with old
    /// files. This service makes the upgrade durable: it rewrites settings.json and round-trips
    /// each entity file (read → write) so the newly added fields are physically written with
    /// their defaults. It runs only when the stored version is behind, and is best-effort:
    /// a malformed file is skipped rather than blocking startup.
    /// </summary>
    public class SchemaMigrationService
    {
        private readonly AppSettingsService _appSettings;
        private readonly IJsonStore<Profile> _profiles;
        private readonly IJsonStore<Account> _accounts;
        private readonly IJsonStore<Transaction> _transactions;
        private readonly IJsonStore<Category> _categories;
        private readonly IJsonStore<Goal> _goals;
        private readonly IJsonStore<Merchant> _merchants;
        private readonly IJsonStore<RecurringTransaction> _recurring;
        private readonly IJsonStore<Attachment> _attachments;

        public SchemaMigrationService(
            AppSettingsService appSettings,
            IJsonStore<Profile> profiles,
            IJsonStore<Account> accounts,
            IJsonStore<Transaction> transactions,
            IJsonStore<Category> categories,
            IJsonStore<Goal> goals,
            IJsonStore<Merchant> merchants,
            IJsonStore<RecurringTransaction> recurring,
            IJsonStore<Attachment> attachments)
        {
            _appSettings = appSettings;
            _profiles = profiles;
            _accounts = accounts;
            _transactions = transactions;
            _categories = categories;
            _goals = goals;
            _merchants = merchants;
            _recurring = recurring;
            _attachments = attachments;
        }

        /// <summary>Runs the migration if the persisted schema is behind the current version.</summary>
        public async Task MigrateAsync()
        {
            var settings = _appSettings.LoadSettings();
            if (settings.SchemaVersion >= Settings.CurrentSchemaVersion)
                return;

            // Rewrite each entity file that has data so new fields are persisted with defaults.
            await NormalizeAsync(_profiles);
            await NormalizeAsync(_accounts);
            await NormalizeAsync(_transactions);
            await NormalizeAsync(_categories);
            await NormalizeAsync(_goals);
            await NormalizeAsync(_merchants);
            await NormalizeAsync(_recurring);
            await NormalizeAsync(_attachments);

            // Stamp the current version and persist the (already default-filled) settings.
            settings.SchemaVersion = Settings.CurrentSchemaVersion;
            _appSettings.Save(settings);
        }

        private static async Task NormalizeAsync<T>(IJsonStore<T> store) where T : Aggregate
        {
            try
            {
                var items = await store.ReadAllAsync();
                if (items.Count > 0)
                    await store.WriteAllAsync(items); // re-serializes with the full current schema
            }
            catch
            {
                // Best-effort: skip files that can't be read/written rather than blocking startup.
            }
        }
    }
}
