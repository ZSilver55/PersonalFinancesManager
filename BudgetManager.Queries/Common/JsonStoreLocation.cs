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

        /// <summary>
        /// Per-user data directory: {base}/users/{owner}. All entity data lives here so the base
        /// path itself only holds cross-cutting files (Users.json, settings.json) and the users folder.
        /// </summary>
        public static string UserDirectory(Settings? settings, Guid owner)
            => Path.Combine(ResolveDirectory(settings), "users", owner.ToString("N"));

        /// <summary>Resolves the per-user directory and ensures it exists on disk.</summary>
        public static string EnsureUserDirectory(Settings? settings, Guid owner)
        {
            var dir = UserDirectory(settings, owner);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
