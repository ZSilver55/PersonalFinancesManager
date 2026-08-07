using System.Text.Json;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using Microsoft.Extensions.Options;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// Reads/writes user preferences (currently the UI language) to settings.json in the
    /// data folder, so choices persist across sessions. Values fall back to the DI-configured
    /// <see cref="Settings"/> defaults when the file is missing.
    /// </summary>
    public class AppSettingsService
    {
        private readonly string _path;
        private readonly Settings _defaults;

        public AppSettingsService(IOptions<Settings> settings)
        {
            _defaults = settings.Value;
            _path = Path.Combine(JsonStoreLocation.EnsureDirectory(_defaults), "settings.json");
        }

        /// <summary>Returns the persisted language ("en"/"es"), or the default when unset.</summary>
        public string LoadLanguage()
        {
            var lang = LoadSettings().Language;
            return string.IsNullOrWhiteSpace(lang) ? "en" : lang;
        }

        /// <summary>Persists the chosen language, preserving any other saved settings.</summary>
        public void SaveLanguage(string language)
        {
            var current = LoadSettings();
            current.Language = string.IsNullOrWhiteSpace(language) ? "en" : language;
            Save(current);
        }

        /// <summary>Persists the whole preferences object (best-effort; IO errors are ignored).</summary>
        public void Save(Settings settings)
        {
            try
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonSerialization.Options));
            }
            catch
            {
                // Persisting preferences is best-effort; ignore IO failures.
            }
        }

        /// <summary>Reads the persisted preferences, falling back to configured defaults.</summary>
        public Settings LoadSettings()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path), JsonSerialization.Options);
                    if (loaded is not null) return loaded;
                }
            }
            catch
            {
                // Corrupt/unreadable file: fall back to defaults.
            }

            return new Settings
            {
                DefaultCurrency = _defaults.DefaultCurrency,
                Language = string.IsNullOrWhiteSpace(_defaults.Language) ? "en" : _defaults.Language
            };
        }
    }
}
