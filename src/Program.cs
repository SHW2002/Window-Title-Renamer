using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using WindowTitleRenamer.Localization;
using WindowTitleRenamer.Settings;

namespace WindowTitleRenamer;

internal static class Program
{
    private static Icon? _appIcon;
    private static Strings L => Strings.Current;

    private static Icon? LoadAppIcon()
    {
        string exeDir = AppContext.BaseDirectory;
        string iconPath = Path.Combine(exeDir, "res", "icon.png");
        if (!File.Exists(iconPath))
            return null;

        using var bmp = new Bitmap(iconPath);
        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private static void SetConsoleWindowIcon(Icon icon)
    {
        IntPtr hwnd = NativeMethods.GetConsoleWindow();
        if (hwnd == IntPtr.Zero) return;

        NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_SMALL, icon.Handle);
        NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_BIG, icon.Handle);
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int len = NativeMethods.GetWindowTextLengthW(hwnd);
        if (len <= 0) return string.Empty;

        StringBuilder sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowTextW(hwnd, sb, sb.Capacity);
        return sb.ToString().Trim();
    }

    private static List<WindowInfo> ListOpenWindows()
    {
        IntPtr shell = NativeMethods.GetShellWindow();
        List<WindowInfo> windows = new List<WindowInfo>();

        bool ok = NativeMethods.EnumWindows((hwnd, lParam) =>
        {
            if (hwnd == shell) return true;
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;

            string title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title)) return true;

            windows.Add(new WindowInfo(hwnd, title));
            return true;
        }, IntPtr.Zero);

        if (!ok)
        {
            throw new InvalidOperationException(
                $"EnumWindows failed, GetLastError={Marshal.GetLastWin32Error()}");
        }

        windows.Sort((a, b) =>
        {
            int c = string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            return a.Hwnd.ToInt64().CompareTo(b.Hwnd.ToInt64());
        });

        return windows;
    }

    private static bool SetWindowTitle(IntPtr hwnd, string newTitle)
    {
        return NativeMethods.SetWindowTextW(hwnd, newTitle);
    }

    private static bool IsWideChar(char c)
    {
        return (c >= 0x1100 && c <= 0x115F) ||
               (c >= 0x2E80 && c <= 0x303E) ||
               (c >= 0x3040 && c <= 0x33BF) ||
               (c >= 0x3400 && c <= 0x4DBF) ||
               (c >= 0x4E00 && c <= 0xA4CF) ||
               (c >= 0xAC00 && c <= 0xD7A3) ||
               (c >= 0xF900 && c <= 0xFAFF) ||
               (c >= 0xFE10 && c <= 0xFE6B) ||
               (c >= 0xFF01 && c <= 0xFF60) ||
               (c >= 0xFFE0 && c <= 0xFFE6);
    }

    private static int DisplayWidth(string s)
    {
        int w = 0;
        foreach (char c in s)
            w += IsWideChar(c) ? 2 : 1;
        return w;
    }

    private static string PadRightByDisplay(string s, int totalWidth)
    {
        int padding = totalWidth - DisplayWidth(s);
        return padding > 0 ? s + new string(' ', padding) : s;
    }

    private static string TruncateByDisplay(string s, int maxWidth)
    {
        int w = 0;
        for (int i = 0; i < s.Length; i++)
        {
            int cw = IsWideChar(s[i]) ? 2 : 1;
            if (w + cw > maxWidth)
                return s[..i];
            w += cw;
        }
        return s;
    }

    private static (bool shouldShow, bool shouldExit) HideToTray()
    {
        const string consoleTitle = "Window Title Renamer";
        IntPtr consoleHwnd = NativeMethods.GetConsoleWindow();

        var hiddenWindows = new List<(IntPtr hwnd, int savedStyle)>();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;
            string title = GetWindowTitle(hwnd);
            if (title.IndexOf(consoleTitle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hiddenWindows.Add((hwnd,
                    NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE)));
            }
            return true;
        }, IntPtr.Zero);

        if (consoleHwnd != IntPtr.Zero)
        {
            bool found = false;
            foreach (var (hwnd, _) in hiddenWindows)
                if (hwnd == consoleHwnd) { found = true; break; }
            if (!found)
                hiddenWindows.Add((consoleHwnd,
                    NativeMethods.GetWindowLong(consoleHwnd, NativeMethods.GWL_EXSTYLE)));
        }

        foreach (var (hwnd, savedStyle) in hiddenWindows)
        {
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                (savedStyle | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW);
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
        }

        var tray = new TrayController("Window Title Renamer", _appIcon, L.TrayShow, L.TrayExit);
        var result = tray.Run();

        foreach (var (hwnd, savedStyle) in hiddenWindows)
        {
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, savedStyle);
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
        }

        if (hiddenWindows.Count > 0)
            NativeMethods.SetForegroundWindow(hiddenWindows[0].hwnd);

        return result;
    }

    private static WindowInfo? SelectWindow(
        List<WindowInfo> allWindows, Dictionary<IntPtr, string> rules)
    {
        var displayedList = allWindows;

        while (true)
        {
            Console.Write(L.PromptSelectWindow);
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;

            if (int.TryParse(input, out int num))
            {
                if (num == 0) return null;
                if (num >= 1 && num <= displayedList.Count)
                    return displayedList[num - 1];
                Console.WriteLine(string.Format(L.ErrorRange, displayedList.Count));
                continue;
            }

            var matches = allWindows
                .Where(w => w.Title.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine(L.ErrorNoMatch);
                continue;
            }

            if (matches.Count == 1)
            {
                Console.WriteLine(string.Format(L.StatusMatchSingle, matches[0].Title));
                return matches[0];
            }

            Console.WriteLine(string.Format(L.StatusMatchMultiple, matches.Count));
            PrintWindowList(matches, rules);
            displayedList = matches;
        }
    }

    private static void PrintWindowList(
        List<WindowInfo> windows, Dictionary<IntPtr, string> rules)
    {
        const int statusW = 8;
        const int hwndW = 18;
        const int maxTitleW = 45;

        int titleW = DisplayWidth(L.ColTitle);
        foreach (var w in windows)
        {
            int tw = DisplayWidth(w.Title);
            if (tw > titleW) titleW = tw;
        }
        titleW = Math.Min(titleW, maxTitleW);

        string sep1 = new string('─', 7);
        string sep2 = new string('─', titleW + 2);
        string sep3 = new string('─', statusW + 2);
        string sep4 = new string('─', hwndW + 1);

        Console.WriteLine($"{sep1}┬{sep2}┬{sep3}┬{sep4}");
        Console.WriteLine(
            $" {PadRightByDisplay(L.ColIndex, 5)} │ {PadRightByDisplay(L.ColTitle, titleW)} │ {PadRightByDisplay(L.ColStatus, statusW)} │ {"HWND",-hwndW}");
        Console.WriteLine($"{sep1}┼{sep2}┼{sep3}┼{sep4}");

        for (int i = 0; i < windows.Count; i++)
        {
            var w = windows[i];
            string mark = rules.ContainsKey(w.Hwnd) ? L.MarkPersisting : "";
            string title = w.Title;
            if (DisplayWidth(title) > titleW)
                title = TruncateByDisplay(title, titleW - 2) + "..";

            Console.WriteLine(
                $" [{i + 1,3}] │ {PadRightByDisplay(title, titleW)} │ {PadRightByDisplay(mark, statusW)} │ 0x{w.Hwnd.ToInt64():X16}");
        }

        Console.WriteLine($"{sep1}┴{sep2}┴{sep3}┴{sep4}");
    }

    private static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            Console.WriteLine(L.ErrorEmpty);
        }
    }

    private static bool ReadYesNo(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            if (s == "y" || s == "yes" || s == "是" || s == "true" || s == "1")
                return true;
            if (s == "n" || s == "no" || s == "否" || s == "false" || s == "0")
                return false;

            Console.WriteLine(L.ErrorYesNo);
        }
    }

    private static string PromptLanguage()
    {
        const string prompt = "Select language / 选择语言:\n  1. English\n  2. 简体中文\nChoice / 选择 (1/2): ";
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Trim();
            if (input == "1") return "en";
            if (input == "2") return "zh-CN";
            Console.WriteLine("Please enter 1 or 2. / 请输入 1 或 2。");
        }
    }

    [STAThread]
    private static int Main()
    {
        NativeMethods.SetConsoleTitleW("Window Title Renamer");

        _appIcon = LoadAppIcon();
        if (_appIcon != null)
            SetConsoleWindowIcon(_appIcon);

        Console.OutputEncoding = Encoding.UTF8;

        var settings = SettingsManager.Load();
        if (SettingsManager.IsFirstLaunch())
        {
            settings.Language = PromptLanguage();
            SettingsManager.Save(settings);
            Console.WriteLine();
        }
        Strings.SetLanguage(settings.Language);

        using PersistentRenamer keeper = new PersistentRenamer();
        keeper.Start();

        string headerLine = new string('═', 60);
        Console.WriteLine(headerLine);
        Console.WriteLine($"  {L.BannerTitle}");
        Console.WriteLine($"  {L.BannerSubtitle}");
        Console.WriteLine(headerLine);
        Console.WriteLine();

        try
        {
            while (true)
            {
                List<WindowInfo> wins = ListOpenWindows();
                Dictionary<IntPtr, string> rules = keeper.ListRules();

                if (wins.Count == 0)
                {
                    Console.WriteLine(L.StatusNoWindows);
                    Thread.Sleep(1000);
                    continue;
                }

                if (rules.Count > 0)
                    Console.WriteLine(string.Format(L.StatusPersistRules, rules.Count));

                PrintWindowList(wins, rules);

                WindowInfo? selected = SelectWindow(wins, rules);

                if (selected == null)
                {
                    var (_, shouldExit) = HideToTray();
                    if (shouldExit)
                        return 0;

                    Console.WriteLine();
                    continue;
                }

                if (!NativeMethods.IsWindow(selected.Value.Hwnd))
                {
                    Console.WriteLine(L.ErrorWindowGone);
                    continue;
                }

                string newTitle = ReadNonEmpty(L.PromptNewTitle);
                bool persist = ReadYesNo(L.PromptPersist);

                bool ok = SetWindowTitle(selected.Value.Hwnd, newTitle);
                Console.WriteLine();
                if (ok)
                {
                    Console.WriteLine(string.Format(L.StatusRenamed, selected.Value.Title, newTitle));
                }
                else
                {
                    Console.WriteLine(
                        string.Format(L.ErrorRenameFailed, Marshal.GetLastWin32Error()));
                    Console.WriteLine(L.ErrorRenameReason);
                }

                if (persist)
                {
                    keeper.AddOrUpdate(selected.Value.Hwnd, newTitle);
                    Console.WriteLine(L.StatusAddedPersist);
                }
                else
                {
                    keeper.Remove(selected.Value.Hwnd);
                }
                Console.WriteLine();
            }
        }
        catch (KeyboardInterruptException)
        {
            Console.WriteLine(L.StatusCtrlC);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        finally
        {
            keeper.Stop();
        }
    }
}
