using System.Globalization;
using WindowTitleRenamer.Localization;
using WindowTitleRenamer.Settings;
using WindowTitleRenamer.UI;

namespace WindowTitleRenamer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppSettings settings = SettingsManager.Load();
        if (SettingsManager.IsFirstLaunch())
        {
            settings.Language = CultureInfo.CurrentUICulture.Name.StartsWith(
                "zh", StringComparison.OrdinalIgnoreCase)
                ? "zh-CN"
                : "en";
            SettingsManager.Save(settings);
        }

        Strings.SetLanguage(settings.Language);

        WindowService windowService = new();
        using PersistentRenamer persistentRenamer = new();
        persistentRenamer.Start();

        Application.Run(new MainForm(windowService, persistentRenamer, settings));
    }
}
