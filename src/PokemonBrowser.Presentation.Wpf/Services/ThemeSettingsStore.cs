using System.IO;
using System.Text.Json;

namespace PokemonBrowser.Presentation.Wpf.Services;

public sealed class ThemeSettingsStore
{
    private readonly string _settingsPath;

    public ThemeSettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PokemonBrowser");

        // Saved per-user (AppData\Roaming) so theme survives restarts.
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppTheme? LoadTheme()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<ThemeSettings>(json);

            if (settings is null)
            {
                return null;
            }

            return Enum.TryParse<AppTheme>(settings.Theme, ignoreCase: true, out var parsed)
                ? parsed
                : null;
        }
        catch
        {
            return null;
        }
    }

    public void SaveTheme(AppTheme theme)
    {
        try
        {
            var folder = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var settings = new ThemeSettings(theme.ToString());
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // best-effort
        }
    }

    private sealed record ThemeSettings(string Theme);
}
