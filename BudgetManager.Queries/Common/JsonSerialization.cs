using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Shared System.Text.Json options used by the local JSON persistence so that
    /// reads and writes always agree on the on-disk format.
    /// </summary>
    public static class JsonSerialization
    {
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            // Persist enums (AccountType, TransactionType, Tag, ...) as readable strings.
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}
