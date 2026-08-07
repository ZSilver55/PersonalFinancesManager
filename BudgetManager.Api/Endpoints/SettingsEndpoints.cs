using BudgetManager.Api.Settings;
using BudgetManager.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Endpoints
{
    /// <summary>Per-user preference settings (currency, language, safe-to-spend). Requires auth.</summary>
    public static class SettingsEndpoints
    {
        public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/settings",
                async ([FromServices] IUserSettingsStore store, CancellationToken ct) =>
                    Results.Ok(await store.GetAsync(ct)))
               .WithTags("Settings");

            app.MapPut("/api/settings",
                async ([FromBody] UserPreferences preferences, [FromServices] IUserSettingsStore store, CancellationToken ct) =>
                {
                    await store.SaveAsync(preferences, ct);
                    return Results.NoContent();
                })
               .WithTags("Settings");

            return app;
        }
    }
}
