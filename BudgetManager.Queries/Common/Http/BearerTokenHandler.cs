using System.Net.Http.Headers;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Delegating handler that attaches "Authorization: Bearer {token}" to every outgoing request,
    /// pulling a fresh token from <see cref="IApiTokenProvider"/> per call (so token refresh is
    /// picked up automatically). If no token is available the request is sent unauthenticated.
    /// </summary>
    public sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly IApiTokenProvider _tokens;

        public BearerTokenHandler(IApiTokenProvider tokens)
        {
            _tokens = tokens;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokens.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
