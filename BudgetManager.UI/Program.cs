using System.Text.Json;
using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using BudgetManager.UI.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudgetManager.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Persisted preferences drive persistence mode/connection string, so read them first.
            var persisted = LoadPersistedSettings();
            var mode = persisted.PersistenceMode == PersistenceMode.Sql
                       && !string.IsNullOrWhiteSpace(persisted.ConnectionString)
                ? PersistenceMode.Sql
                : PersistenceMode.Json; // safe fallback when SQL is requested without a connection string

            var services = new ServiceCollection();
            ConfigureServices(services, persisted, mode);

            using var provider = services.BuildServiceProvider();

            // Upgrade older data/settings files to the current schema before anything reads them.
            provider.GetRequiredService<SchemaMigrationService>().MigrateAsync().GetAwaiter().GetResult();

            // One-time import of existing JSON data into SQL when SQL was just enabled.
            if (mode == PersistenceMode.Sql && !persisted.ImportedJsonToSql)
            {
                try
                {
                    provider.GetRequiredService<StoreImportService>().ImportJsonToSqlAsync().GetAwaiter().GetResult();
                    var appSettings = provider.GetRequiredService<AppSettingsService>();
                    var s = appSettings.LoadSettings();
                    s.ImportedJsonToSql = true;
                    appSettings.Save(s);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Data import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // Apply the persisted UI language before any window is created.
            Loc.SetLanguage(provider.GetRequiredService<AppSettingsService>().LoadLanguage());

            Application.Run(provider.GetRequiredService<MainForm>());
        }

        private static void ConfigureServices(IServiceCollection services, Settings persisted, PersistenceMode mode)
        {
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

            // Surface the persisted settings (connection string, data dir, mode, etc.) to the stores.
            services.Configure<Settings>(s =>
            {
                s.SchemaVersion = persisted.SchemaVersion;
                s.DefaultCurrency = string.IsNullOrWhiteSpace(persisted.DefaultCurrency) ? "MXN" : persisted.DefaultCurrency;
                s.ConnectionString = persisted.ConnectionString;
                s.DataDirectory = persisted.DataDirectory;
                s.Language = persisted.Language;
                s.SafetyBuffer = persisted.SafetyBuffer;
                s.ReserveForGoals = persisted.ReserveForGoals;
                s.PersistenceMode = mode;
                s.ImportedJsonToSql = persisted.ImportedJsonToSql;
            });

            // Persistence (store + generic commands/queries), JSON or SQL per mode.
            services.AddBudgetPersistence(mode);
            if (mode == PersistenceMode.Sql)
                services.AddTransient<StoreImportService>();

            // Handlers.
            services.AddTransient<CommnadHandler>();
            services.AddTransient(typeof(QueryHandler<>));

            // Controllers.
            services.AddTransient<ProfileController>();
            services.AddTransient<AccountController>();
            services.AddTransient<TransactionsController>();
            services.AddTransient<CategoriesController>();
            services.AddTransient<GoalController>();
            services.AddTransient<RecurringTransactionsController>();

            // Domain services.
            services.AddTransient<BudgetService>();
            services.AddTransient<RecurringExecutionService>();
            services.AddTransient<InterestExecutionService>();
            services.AddTransient<ProjectionService>();
            services.AddTransient<SafeToSpendService>();
            services.AddSingleton<DataPortabilityService>();
            services.AddSingleton<AppSettingsService>();
            services.AddTransient<SchemaMigrationService>();

            // Forms.
            services.AddTransient<MainForm>();
        }

        /// <summary>Reads settings.json from the default data folder (best-effort) to bootstrap config.</summary>
        private static Settings LoadPersistedSettings()
        {
            try
            {
                var path = Path.Combine(JsonStoreLocation.ResolveDirectory(null), "settings.json");
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonSerialization.Options) ?? new Settings();
            }
            catch
            {
                // Fall back to defaults if the file is missing or unreadable.
            }
            return new Settings();
        }
    }
}
