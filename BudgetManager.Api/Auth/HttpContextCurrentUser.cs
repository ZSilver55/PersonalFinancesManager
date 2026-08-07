using BudgetManager.Queries.Common;

namespace BudgetManager.Api.Auth
{
    /// <summary>
    /// Resolves the current user's OwnerUserId from the request. The value is the registry id set by
    /// <see cref="UserContextMiddleware"/> (which maps the token's subject to a stable internal user
    /// id, provisioning on first login). When there is no authenticated user (auth disabled, or an
    /// anonymous endpoint), it falls back to the empty user, which uses the shared/base data.
    /// </summary>
    public sealed class HttpContextCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

        public Guid UserId
        {
            get
            {
                var items = _accessor.HttpContext?.Items;
                if (items is not null &&
                    items.TryGetValue(UserContextMiddleware.OwnerIdItemKey, out var value) &&
                    value is Guid id)
                {
                    return id;
                }
                return Guid.Empty;
            }
        }
    }
}
