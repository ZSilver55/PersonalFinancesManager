namespace BudgetManager.Domain
{
    /// <summary>
    /// The subset of settings that are per-user preferences (roam with the account), as opposed to
    /// device/infrastructure settings (PersistenceMode, ConnectionString, ApiBaseUrl) which stay on
    /// the client. Stored per user by the API and read/written by the client when online.
    /// </summary>
    public class UserPreferences
    {
        public int SchemaVersion { get; set; } = Settings.CurrentSchemaVersion;
        public string DefaultCurrency { get; set; } = "MXN";
        public string Language { get; set; } = "en";
        public decimal SafetyBuffer { get; set; } = 0m;
        public bool ReserveForGoals { get; set; } = true;
    }
}
