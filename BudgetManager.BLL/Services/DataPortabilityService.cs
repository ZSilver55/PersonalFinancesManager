using System.IO.Compression;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using Microsoft.Extensions.Options;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// Backup / restore for the local JSON store. Exposes the data folder location and
    /// exports/imports all *.json files as a single zip archive.
    /// </summary>
    public class DataPortabilityService
    {
        public DataPortabilityService(IOptions<Settings> settings, ICurrentUser currentUser)
        {
            // Back up the current user's folder (users/{owner}); on the desktop this is the empty owner.
            DataDirectory = JsonStoreLocation.EnsureUserDirectory(settings?.Value, currentUser.UserId);
        }

        /// <summary>Absolute path of the folder holding the JSON data files.</summary>
        public string DataDirectory { get; }

        /// <summary>Number of *.json data files currently stored.</summary>
        public int FileCount =>
            Directory.Exists(DataDirectory) ? Directory.GetFiles(DataDirectory, "*.json").Length : 0;

        /// <summary>
        /// Writes every *.json data file into <paramref name="targetZipPath"/> (overwriting it).
        /// Returns the path written.
        /// </summary>
        public string ExportToZip(string targetZipPath)
        {
            if (string.IsNullOrWhiteSpace(targetZipPath))
                throw new ArgumentException("A target path is required.", nameof(targetZipPath));

            if (File.Exists(targetZipPath))
                File.Delete(targetZipPath);

            using var zip = ZipFile.Open(targetZipPath, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(DataDirectory, "*.json"))
                zip.CreateEntryFromFile(file, Path.GetFileName(file));

            return targetZipPath;
        }

        /// <summary>
        /// Restores *.json entries from <paramref name="sourceZipPath"/> into the data folder,
        /// overwriting existing files. Returns the number of files restored.
        /// </summary>
        public int ImportFromZip(string sourceZipPath)
        {
            if (!File.Exists(sourceZipPath))
                throw new FileNotFoundException("Backup file not found.", sourceZipPath);

            Directory.CreateDirectory(DataDirectory);

            int restored = 0;
            using var zip = ZipFile.OpenRead(sourceZipPath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue; // skip directory entries
                if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                var destination = Path.Combine(DataDirectory, Path.GetFileName(entry.FullName));
                entry.ExtractToFile(destination, overwrite: true);
                restored++;
            }

            return restored;
        }
    }
}
