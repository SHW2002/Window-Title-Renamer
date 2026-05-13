using System.Text.Json;

namespace WindowTitleRenamer.Settings;

internal static class SettingsManager
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "WindowTitleRenamer");

    private static readonly string ConfigPath =
        Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            }
        }
        catch { }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(ConfigPath, json);
    }

    public static bool IsFirstLaunch() => !File.Exists(ConfigPath);
}
