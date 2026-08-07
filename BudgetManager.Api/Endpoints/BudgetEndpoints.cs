using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Domain;
using BudgetManager.Queries.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Endpoints
{
    public static class BudgetEndpoints
    {
        public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
        {
            // Generic CRUD for each aggregate.
            app.MapCrud<Profile>("/api/profiles", "Profiles");
            app.MapCrud<Account>("/api/accounts", "Accounts");
            app.MapCrud<Transaction>("/api/transactions", "Transactions");
            app.MapCrud<Category>("/api/categories", "Categories");
            app.MapCrud<Goal>("/api/goals", "Goals");
            app.MapCrud<Merchant>("/api/merchants", "Merchants");
            app.MapCrud<RecurringTransaction>("/api/recurring", "Recurring");

            // Accounts by profile.
            app.MapGet("/api/accounts/by-profile/{profileId:guid}",
                async (Guid profileId, [FromServices] AccountController c) =>
                    Results.Ok((await c.GetAccounts(profileId)).Data))
               .WithTags("Accounts");

            // Transactions: by account, by date range.
            app.MapGet("/api/transactions/by-account/{accountId:guid}",
                async (Guid accountId, [FromServices] TransactionsController c) =>
                    Results.Ok((await c.GetTransactionsByAccount(accountId)).Data))
               .WithTags("Transactions");

            app.MapGet("/api/transactions/by-range",
                async (DateTime from, DateTime to, [FromServices] TransactionsController c) =>
                    Results.Ok((await c.GetTransactionsByDateRange(new DateRange(from, to))).Data))
               .WithTags("Transactions");

            // Categories by parent (omit parentId for top-level).
            app.MapGet("/api/categories/by-parent",
                async (Guid? parentId, [FromServices] CategoriesController c) =>
                    Results.Ok((await c.GetCategories(parentId)).Data))
               .WithTags("Categories");

            // Recurring: due + run.
            app.MapGet("/api/recurring/due",
                async (DateTime? asOf, [FromServices] RecurringTransactionsController c) =>
                    Results.Ok((await c.GetDueRecurringTransactions(asOf ?? DateTime.Now)).Data))
               .WithTags("Recurring");

            app.MapPost("/api/recurring/run",
                async ([FromServices] RecurringExecutionService s) =>
                    Results.Ok(new { posted = await s.RunDueAsync(DateTime.Now) }))
               .WithTags("Recurring");

            app.MapPost("/api/interest/run",
                async ([FromServices] InterestExecutionService s) =>
                    Results.Ok(new { posted = await s.RunDueAsync(DateTime.Now) }))
               .WithTags("Interest");

            // Analytics.
            app.MapGet("/api/dashboard/{profileId:guid}",
                async (Guid profileId, [FromServices] BudgetService s) =>
                {
                    var now = DateTime.Now;
                    var from = new DateTime(now.Year, now.Month, 1);
                    var to = from.AddMonths(1).AddTicks(-1);
                    return Results.Ok(await s.GetDashboardAsync(profileId, from, to));
                })
               .WithTags("Analytics");

            app.MapGet("/api/projection/{profileId:guid}",
                async (Guid profileId, DateTime? start, [FromServices] ProjectionService s) =>
                    Results.Ok(await s.BuildAsync(profileId, start ?? DateTime.Today)))
               .WithTags("Analytics");

            app.MapGet("/api/safe-to-spend/{profileId:guid}",
                async (Guid profileId, decimal? buffer, bool? reserveGoals, [FromServices] SafeToSpendService s) =>
                    Results.Ok(await s.ComputeAsync(profileId, buffer ?? 0m, reserveGoals ?? true)))
               .WithTags("Analytics");

            return app;
        }

        /// <summary>Maps standard CRUD for an aggregate using the generic BaseController&lt;T&gt;.</summary>
        private static void MapCrud<T>(this IEndpointRouteBuilder app, string route, string tag) where T : Aggregate
        {
            var group = app.MapGroup(route).WithTags(tag);

            group.MapGet("/", async ([FromServices] BaseController<T> c) =>
                Results.Ok((await c.GetAll()).Data));

            group.MapGet("/{id:guid}", async (Guid id, [FromServices] BaseController<T> c) =>
            {
                var r = await c.GetById(id);
                return r.Success ? Results.Ok(r.Data) : Results.NotFound(r.Message);
            });

            group.MapPost("/", async ([FromBody] T entity, [FromServices] BaseController<T> c) =>
            {
                var r = await c.Add(entity);
                return r.Success ? Results.Created($"{route}/{entity.Id}", entity) : Results.BadRequest(r.Message);
            });

            group.MapPut("/{id:guid}", async (Guid id, [FromBody] T entity, [FromServices] BaseController<T> c) =>
            {
                entity.Id = id;
                var r = await c.Update(entity);
                return r.Success ? Results.Ok(entity) : Results.BadRequest(r.Message);
            });

            group.MapDelete("/{id:guid}", async (Guid id, [FromServices] BaseController<T> c) =>
            {
                var r = await c.Delete(id);
                return r.Success ? Results.NoContent() : Results.NotFound(r.Message);
            });
        }
    }
}
