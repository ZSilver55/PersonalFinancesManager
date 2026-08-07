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

            // Persisted preferences drive the data source. The desktop uses local JSON or the
            // remote API only — never SQL directly (SQL is server-side). A Sql setting or an
            // Api setting without a base URL falls back to local JSON.
            var persisted = LoadPersistedSettings();
            var mode = persisted.PersistenceMode == PersistenceMode.Api
                       && !string.IsNullOrWhiteSpace(persisted.ApiBaseUrl)
                ? PersistenceMode.Api
                : PersistenceMode.Json;

            var services = new ServiceCollection();
            ConfigureServices(services, persisted, mode);

            using var provider = services.BuildServiceProvider();

            // Local schema upgrade only applies to the local JSON files; in API mode the server
            // owns the schema.
            if (mode == PersistenceMode.Json)
                provider.GetRequiredService<SchemaMigrationService>().MigrateAsync().GetAwaiter().GetResult();

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
                s.ApiBaseUrl = persisted.ApiBaseUrl;
                s.ImportedJsonToSql = persisted.ImportedJsonToSql;
            });

            // Data source: local JSON or the remote API (chosen above; never SQL on the desktop).
            services.AddBudgetPersistence(mode);

            // Shared application layer (handlers, controllers, business services).
            services.AddBudgetApplication();

            // Desktop-only services.
            services.AddSingleton<DataPortabilityService>();
            services.AddSingleton<AppSettingsService>();
            services.AddTransient<SchemaMigrationService>();
            services.AddTransient<DataSourceSwitchService>();

            // Sign-in service. Registered as the API token provider (overrides the NullApiTokenProvider
            // from AddBudgetPersistence, since this comes later), so online requests carry the token.
            services.AddSingleton<Services.DesktopAuthService>();
            services.AddSingleton<IApiTokenProvider>(sp => sp.GetRequiredService<Services.DesktopAuthService>());

            // Online-only: reads/writes the user's preference settings via the API.
            if (mode == PersistenceMode.Api)
                services.AddSingleton<Services.ApiUserSettingsClient>();

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
