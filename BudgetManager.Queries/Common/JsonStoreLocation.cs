using BudgetManager.Domain;

namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// Single source of truth for where the local JSON data files live, so the store
    /// and the UI (open folder / export / import) always agree on the same directory.
    /// </summary>
    public static class JsonStoreLocation
    {
        /// <summary>
        /// Resolves the data directory: Settings.DataDirectory when set, otherwise
        /// %AppData%\BudgetManager. Does not create the directory.
        /// </summary>
        public static string ResolveDirectory(Settings? settings)
        {
            return settings?.DataDirectory is { Length: > 0 } configured
                ? configured
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BudgetManager");
        }

        /// <summary>Resolves the directory and ensures it exists on disk.</summary>
        public static string EnsureDirectory(Settings? settings)
        {
            var dir = ResolveDirectory(settings);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
