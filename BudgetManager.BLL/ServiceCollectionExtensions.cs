using BudgetManager.BLL.Services;
using BudgetManager.Commands;
using BudgetManager.Queries.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.BLL
{
    /// <summary>
    /// Registers the application layer (handlers, controllers, business services) shared by
    /// every host (desktop, Web API). Persistence is registered separately via
    /// AddBudgetPersistence(mode), and each host adds its own concerns (forms, endpoints, etc.).
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBudgetApplication(this IServiceCollection services)
        {
            // Handlers.
            services.AddTransient<CommnadHandler>();
            services.AddTransient(typeof(QueryHandler<>));

            // Generic CRUD controller (Add/Update/Delete/GetAll/GetById for any aggregate).
            services.AddTransient(typeof(BaseController<>));

            // Entity controllers with their specific queries.
            services.AddTransient<ProfileController>();
            services.AddTransient<AccountController>();
            services.AddTransient<TransactionsController>();
            services.AddTransient<CategoriesController>();
            services.AddTransient<GoalController>();
            services.AddTransient<RecurringTransactionsController>();

            // Cross-aggregate business services.
            services.AddTransient<BudgetService>();
            services.AddTransient<RecurringExecutionService>();
            services.AddTransient<InterestExecutionService>();
            services.AddTransient<ProjectionService>();
            services.AddTransient<SafeToSpendService>();

            return services;
        }
    }
}
