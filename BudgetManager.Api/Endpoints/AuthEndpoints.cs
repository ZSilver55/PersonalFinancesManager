using BudgetManager.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Endpoints
{
    /// <summary>
    /// Public sign-in helper endpoints for thin clients. The client only needs the API address:
    ///   GET  /auth/config  → what to show the user (authorize endpoint, client id, scopes).
    ///   POST /auth/token   → performs the code/refresh exchange server-side (holds the secret).
    /// Both are anonymous so an unauthenticated client can sign in even when the API otherwise
    /// requires a token.
    /// </summary>
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/config",
                async ([FromServices] AuthProviderService auth, CancellationToken ct) =>
                    Results.Ok(await auth.GetConfigAsync(ct)))
               .WithTags("Auth")
               .AllowAnonymous();

            // Maps the logged-in user to their internal id (provisioned by UserContextMiddleware).
            // Requires authentication (no AllowAnonymous), so an unauthenticated call returns 401.
            app.MapGet("/auth/me", (HttpContext ctx) =>
            {
                if (ctx.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                Guid id = ctx.Items.TryGetValue(UserContextMiddleware.OwnerIdItemKey, out var v) && v is Guid g
                    ? g
                    : Guid.Empty;
                var email = ctx.User.FindFirst("email")?.Value;
                var name = ctx.User.FindFirst("name")?.Value;
                return Results.Ok(new { id, email, name });
            })
               .WithTags("Auth");

            app.MapPost("/auth/token",
                async ([FromBody] TokenExchangeRequest req, [FromServices] AuthProviderService auth, CancellationToken ct) =>
                {
                    if (!auth.Enabled)
                        return Results.BadRequest(new { error = "Authentication is not enabled on this server." });

                    (int status, string json) result;
                    if (!string.IsNullOrEmpty(req.RefreshToken))
                    {
                        result = await auth.RefreshAsync(req.RefreshToken!, ct);
                    }
                    else if (!string.IsNullOrEmpty(req.Code) &&
                             !string.IsNullOrEmpty(req.CodeVerifier) &&
                             !string.IsNullOrEmpty(req.RedirectUri))
                    {
                        result = await auth.ExchangeCodeAsync(req.Code!, req.CodeVerifier!, req.RedirectUri!, ct);
                    }
                    else
                    {
                        return Results.BadRequest(new { error = "Provide either a refresh token or code/codeVerifier/redirectUri." });
                    }

                    // Relay the provider's token JSON (and status) straight back to the client.
                    return Results.Content(result.json, "application/json", System.Text.Encoding.UTF8, result.status);
                })
               .WithTags("Auth")
               .AllowAnonymous();

            return app;
        }
    }
}
