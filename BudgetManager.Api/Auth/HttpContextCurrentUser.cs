using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BudgetManager.Queries.Common;

namespace BudgetManager.Api.Auth
{
    /// <summary>
    /// Resolves the current user from the authenticated request. The token's subject (which may
    /// be a provider-specific string like "auth0|123" or a GUID) is mapped to a stable Guid so it
    /// can be used as OwnerUserId. When there is no authenticated user (e.g. auth disabled for
    /// local development), it falls back to the empty user.
    /// </summary>
    public sealed class HttpContextCurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

        public Guid UserId
        {
            get
            {
                var principal = _accessor.HttpContext?.User;
                var subject = principal?.FindFirst("sub")?.Value
                              ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return string.IsNullOrEmpty(subject) ? Guid.Empty : ToDeterministicGuid(subject);
            }
        }

        /// <summary>Maps an arbitrary subject string to a stable Guid (MD5 of the UTF-8 bytes).</summary>
        private static Guid ToDeterministicGuid(string subject)
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(subject));
            return new Guid(hash);
        }
    }
}
