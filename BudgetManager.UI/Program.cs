using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
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

            var services = new ServiceCollection();
            ConfigureServices(services);

            using var provider = services.BuildServiceProvider();

            // Upgrade any older data/settings files to the current schema before anything reads them.
            provider.GetRequiredService<SchemaMigrationService>().MigrateAsync().GetAwaiter().GetResult();

            // Apply the persisted UI language before any window is created.
            Loc.SetLanguage(provider.GetRequiredService<AppSettingsService>().LoadLanguage());

            Application.Run(provider.GetRequiredService<MainForm>());
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

            // Settings: leave DataDirectory null so the store defaults to %AppData%\BudgetManager.
            services.Configure<Settings>(s => { s.DefaultCurrency = "MXN"; });

            // JSON persistence (store + generic commands/queries).
            services.AddJsonPersistence();

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
    }
}
