using BudgetManager.Queries.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetManager.Commands
{
    /// <summary>
    /// Registers the local JSON persistence: the file store plus the generic
    /// insert / update / delete commands and the generic all / by-id queries.
    ///
    /// Usage (composition root):
    ///     services.Configure&lt;Settings&gt;(config.GetSection("Settings"));
    ///     services.AddJsonPersistence();
    ///
    /// Then resolve, for example:
    ///     var insert = provider.GetRequiredService&lt;IInsertCommand&lt;Account&gt;&gt;();
    ///     var delete = provider.GetRequiredService&lt;DeleteCommand&lt;Account&gt;&gt;();
    ///     var all    = provider.GetRequiredService&lt;AllQuery&lt;Account&gt;&gt;();
    /// </summary>
    public static class JsonPersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonPersistence(this IServiceCollection services)
        {
            // One JSON file per aggregate type, shared by reads and writes.
            services.AddSingleton(typeof(IJsonStore<>), typeof(JsonFileStore<>));

            // Commands — interface mappings (insert/update map cleanly as open generics).
            services.AddTransient(typeof(IInsertCommand<>), typeof(InsertCommand<>));
            services.AddTransient(typeof(IUpdateCommand<>), typeof(UpdateCommand<>));

            // Concrete open generics so every entity is resolvable, including delete
            // (DeleteCommand<T> implements the closed IDeleteCommand<Guid>, so it is
            // resolved by its concrete type rather than the interface).
            services.AddTransient(typeof(InsertCommand<>));
            services.AddTransient(typeof(UpdateCommand<>));
            services.AddTransient(typeof(DeleteCommand<>));

            // Generic queries.
            services.AddTransient(typeof(AllQuery<>));
            services.AddTransient(typeof(ByIdQuery<>));

            return services;
        }
    }
}
