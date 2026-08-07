using System.Security.Claims;

namespace BudgetManager.Api.Auth
{
    /// <summary>
    /// For each authenticated request, maps the token identity to a registered user (provisioning
    /// one on first login) and stashes the resulting internal id in HttpContext.Items so the rest
    /// of the request — including <see cref="HttpContextCurrentUser"/> and the owner-scoped stores —
    /// use that id as the OwnerUserId. Anonymous requests pass through untouched.
    /// </summary>
    public sealed class UserContextMiddleware
    {
        public const string OwnerIdItemKey = "OwnerUserId";

        private readonly RequestDelegate _next;

        public UserContextMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context, IUserDirectory directory)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var subject = context.User.FindFirst("sub")?.Value
                              ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(subject))
                {
                    // Only trust the email for seed-linking when the provider marked it verified.
                    bool emailVerified = string.Equals(
                        context.User.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase);
                    string? email = emailVerified ? context.User.FindFirst("email")?.Value : null;
                    string? name = context.User.FindFirst("name")?.Value;

                    var user = await directory.ResolveAsync(subject, email, name, context.RequestAborted);
                    context.Items[OwnerIdItemKey] = user.Id;
                }
            }

            await _next(context);
        }
    }
}
