using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.Domain
{
    public class Settings
    {
        /// <summary>
        /// The schema version this build writes. Bump when new persisted fields are added so the
        /// migration can rewrite existing files with the new fields. Stored files carry their own
        /// version; a lower value triggers a one-time upgrade.
        /// </summary>
        public const int CurrentSchemaVersion = 6;

        /// <summary>Schema version of the persisted settings file (0 for pre-versioning files).</summary>
        public int SchemaVersion { get; set; } = 0;

        public string DefaultCurrency { get; set; } = "MXN";

        /// <summary>
        /// SQL connection string (used by the legacy SQL persistence).
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Optional override for the folder where the local JSON data files are stored.
        /// When null or empty, the store defaults to
        /// %AppData%\BudgetManager (Environment.SpecialFolder.ApplicationData).
        /// </summary>
        public string? DataDirectory { get; set; }

        /// <summary>
        /// UI language code ("en" or "es"). Persisted in the data folder's settings.json
        /// so the chosen language is remembered between sessions.
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Minimum balance the "safe to spend" calculation keeps untouched (a cushion).
        /// </summary>
        public decimal SafetyBuffer { get; set; } = 0m;

        /// <summary>
        /// When true, the "safe to spend" calculation sets aside a daily amount toward goals
        /// that have a future due date.
        /// </summary>
        public bool ReserveForGoals { get; set; } = true;

        /// <summary>
        /// Where entity data is stored. Defaults to local JSON files; set to Sql (with a
        /// ConnectionString) to use SQL Server.
        /// </summary>
        public PersistenceMode PersistenceMode { get; set; } = PersistenceMode.Json;

        /// <summary>
        /// Set once the one-time import of existing JSON data into SQL has run, so it isn't repeated.
        /// </summary>
        public bool ImportedJsonToSql { get; set; } = false;
    }
}
