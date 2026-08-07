namespace BudgetManager.Domain.Enumerations
{
    /// <summary>Where entity data is stored.</summary>
    public enum PersistenceMode
    {
        /// <summary>Local JSON files under the data folder (default, offline).</summary>
        Json,

        /// <summary>SQL Server database (via the configured connection string). Server-side only.</summary>
        Sql,

        /// <summary>Remote Web API (via the configured ApiBaseUrl). Used by the desktop client online.</summary>
        Api
    }
}
