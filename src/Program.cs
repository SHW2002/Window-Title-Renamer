using System.Runtime.InteropServices;
using System.Text;

namespace WindowTitleRenamer;

internal static class Program
{
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

        var tray = new TrayController("Window Title Renamer");
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
            Console.Write("请输入编号或关键字（0 隐藏到托盘）: ");
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;

            if (int.TryParse(input, out int num))
            {
                if (num == 0) return null;
                if (num >= 1 && num <= displayedList.Count)
                    return displayedList[num - 1];
                Console.WriteLine($"请输入 0 或 1 到 {displayedList.Count} 的编号。");
                continue;
            }

            var matches = allWindows
                .Where(w => w.Title.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine("没有匹配的窗口，请重新输入。");
                continue;
            }

            if (matches.Count == 1)
            {
                Console.WriteLine($"匹配到：{matches[0].Title}");
                return matches[0];
            }

            Console.WriteLine($"\n匹配到 {matches.Count} 个窗口：");
            PrintWindowList(matches, rules);
            displayedList = matches;
        }
    }

    private static void PrintWindowList(
        List<WindowInfo> windows, Dictionary<IntPtr, string> rules)
    {
        Console.WriteLine(new string('-', 90));
        for (int i = 0; i < windows.Count; i++)
        {
            var w = windows[i];
            string mark = rules.ContainsKey(w.Hwnd) ? " *保持中*" : "";
            Console.WriteLine(
                $"[{i + 1,3}] {w.Title}{mark}   (HWND=0x{w.Hwnd.ToInt64():X16})");
        }
        Console.WriteLine(new string('-', 90));
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

            Console.WriteLine("不能为空。");
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

            Console.WriteLine("请输入 y/n（也可以输入 是/否）。");
        }
    }

    [STAThread]
    private static int Main()
    {
        NativeMethods.SetConsoleTitleW("Window Title Renamer");

        using PersistentRenamer keeper = new PersistentRenamer();
        keeper.Start();

        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("Window Title Renamer");
        Console.WriteLine("输入编号选择窗口，输入文字模糊搜索窗口，输入 0 隐藏到托盘");
        Console.WriteLine();

        try
        {
            while (true)
            {
                List<WindowInfo> wins = ListOpenWindows();
                Dictionary<IntPtr, string> rules = keeper.ListRules();

                if (wins.Count == 0)
                {
                    Console.WriteLine("没有找到可重命名的窗口（可见且标题非空）。1 秒后重试…");
                    Thread.Sleep(1000);
                    continue;
                }

                Console.WriteLine($"当前长久保持规则数：{rules.Count}（后台每秒重设一次）");
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
                    Console.WriteLine("目标窗口已不存在（可能已关闭）。回到列表。\n");
                    continue;
                }

                string newTitle = ReadNonEmpty("请输入新窗口标题: ");
                bool persist = ReadYesNo("是否长久保持（每秒重复设置一次）？(y/n): ");

                bool ok = SetWindowTitle(selected.Value.Hwnd, newTitle);
                if (ok)
                {
                    Console.WriteLine($"已重命名：{selected.Value.Title} -> {newTitle}");
                }
                else
                {
                    Console.WriteLine(
                        $"重命名失败（GetLastError={Marshal.GetLastWin32Error()}）。可能原因：权限不足/窗口不接受 SetWindowText。");
                }

                if (persist)
                {
                    keeper.AddOrUpdate(selected.Value.Hwnd, newTitle);
                    Console.WriteLine("已加入长久保持。\n");
                }
                else
                {
                    keeper.Remove(selected.Value.Hwnd);
                    Console.WriteLine();
                }
            }
        }
        catch (KeyboardInterruptException)
        {
            Console.WriteLine("\n收到 Ctrl+C，退出。");
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
