namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Supplies the bearer access token attached to outgoing API requests. Implemented by the
    /// desktop's sign-in service; a null-returning default is used when no auth is configured
    /// (so API calls go out unauthenticated, matching the server's Auth:Enabled=false mode).
    /// </summary>
    public interface IApiTokenProvider
    {
        /// <summary>Returns a valid access token, or null when the user is not signed in.</summary>
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>Default provider that never supplies a token (unauthenticated requests).</summary>
    public sealed class NullApiTokenProvider : IApiTokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}
