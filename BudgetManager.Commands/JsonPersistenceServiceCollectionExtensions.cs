using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BudgetManager.Commands
{
    /// <summary>
    /// Registers the persistence stack: the entity store (JSON files or SQL, chosen by mode)
    /// plus the generic insert/update/delete commands and the generic all/by-id queries.
    ///
    /// Usage (composition root):
    ///     services.Configure&lt;Settings&gt;(...);           // must supply ConnectionString for SQL
    ///     services.AddBudgetPersistence(PersistenceMode.Sql);
    ///
    /// Then resolve, for example:
    ///     var insert = provider.GetRequiredService&lt;IInsertCommand&lt;Account&gt;&gt;();
    ///     var all    = provider.GetRequiredService&lt;AllQuery&lt;Account&gt;&gt;();
    /// </summary>
    public static class JsonPersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddBudgetPersistence(this IServiceCollection services, PersistenceMode mode)
        {
            // Current user: default single-user (empty owner); the API overrides this with a
            // token-based user. Registered for all modes so any store or service can depend on it.
            // Singleton is safe: SystemCurrentUser is stateless and the API's replacement reads the
            // request user via IHttpContextAccessor (AsyncLocal), also stateless.
            services.AddSingleton<ICurrentUser, SystemCurrentUser>();

            // Entity store: SQL Server (server), remote Web API (desktop online), or local JSON
            // files (desktop offline). All implement IEntityStore<T>, so the commands, queries,
            // controllers and services are identical regardless of the backing store.
            if (mode == PersistenceMode.Sql)
            {
                services.AddSingleton<IDbConnectionFactory, SQLDbConnectionFactory>();
                services.AddScoped(typeof(IEntityStore<>), typeof(SqlEntityStore<>));
            }
            else if (mode == PersistenceMode.Api)
            {
                // Default token provider supplies no token (unauthenticated); the desktop composition
                // root overrides IApiTokenProvider with its sign-in service when auth is configured.
                services.AddSingleton<IApiTokenProvider, NullApiTokenProvider>();
                services.AddSingleton(sp =>
                {
                    var url = sp.GetRequiredService<IOptions<Settings>>().Value.ApiBaseUrl
                              ?? throw new InvalidOperationException("An ApiBaseUrl is required for API persistence.");
                    if (!url.EndsWith('/')) url += "/";
                    // BearerTokenHandler pulls a fresh token per request from the token provider.
                    var handler = new BearerTokenHandler(sp.GetRequiredService<IApiTokenProvider>());
                    return new HttpClient(handler) { BaseAddress = new Uri(url) };
                });
                services.AddSingleton(typeof(IEntityStore<>), typeof(ApiEntityStore<>));
            }
            else
            {
                // JSON files under users/{owner}; the API sets the owner from the token per request.
                services.AddSingleton(typeof(IEntityStore<>), typeof(JsonFileStore<>));
            }

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
