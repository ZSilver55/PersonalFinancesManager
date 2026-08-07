using System.Net.Http;
using System.Net.Http.Json;
using BudgetManager.Domain;

namespace BudgetManager.UI.Services
{
    /// <summary>
    /// Reads/writes the signed-in user's preference settings via the API (/api/settings), using the
    /// authenticated HttpClient (bearer token attached). Only registered/used in online mode.
    /// </summary>
    public sealed class ApiUserSettingsClient
    {
        private readonly HttpClient _http;

        public ApiUserSettingsClient(HttpClient http) => _http = http;

        public Task<UserPreferences?> GetAsync(CancellationToken cancellationToken = default) =>
            _http.GetFromJsonAsync<UserPreferences>("api/settings", cancellationToken);

        public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
        {
            var response = await _http.PutAsJsonAsync("api/settings", preferences, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
