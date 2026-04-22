using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowTitleRenamer
{
    internal static class NativeMethods
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetConsoleTitleW(string lpConsoleTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextLengthW(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetWindowTextW(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    internal sealed class PersistentRenamer : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<IntPtr, string> _rules = new Dictionary<IntPtr, string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Thread? _thread;

        public void Start()
        {
            if (_thread != null) return;

            _thread = new Thread(Worker)
            {
                IsBackground = true,
                Name = "PersistentRenamerWorker"
            };
            _thread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(2000);
            }
        }

        public void AddOrUpdate(IntPtr hwnd, string title)
        {
            lock (_lock)
            {
                _rules[hwnd] = title;
            }
        }

        public void Remove(IntPtr hwnd)
        {
            lock (_lock)
            {
                _rules.Remove(hwnd);
            }
        }

        public Dictionary<IntPtr, string> ListRules()
        {
            lock (_lock)
            {
                return new Dictionary<IntPtr, string>(_rules);
            }
        }

        private void Worker()
        {
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(1000);

                List<KeyValuePair<IntPtr, string>> items;
                lock (_lock)
                {
                    items = new List<KeyValuePair<IntPtr, string>>(_rules);
                }

                if (items.Count == 0) continue;

                List<IntPtr> dead = new List<IntPtr>();

                foreach (var kv in items)
                {
                    var hwnd = kv.Key;
                    var title = kv.Value;

                    if (!NativeMethods.IsWindow(hwnd))
                    {
                        dead.Add(hwnd);
                        continue;
                    }

                    NativeMethods.SetWindowTextW(hwnd, title);
                }

                if (dead.Count > 0)
                {
                    lock (_lock)
                    {
                        foreach (var hwnd in dead)
                        {
                            _rules.Remove(hwnd);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }

    internal sealed class TrayController
    {
        private readonly string _tooltip;
        private bool _shouldShow;
        private bool _shouldExit;

        public TrayController(string tooltip = "Window Title Renamer")
        {
            _tooltip = tooltip;
        }

        public (bool shouldShow, bool shouldExit) Run()
        {
            using ManualResetEvent done = new ManualResetEvent(false);

            Exception? threadEx = null;

            Thread trayThread = new Thread(() =>
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    using TrayApplicationContext ctx = new TrayApplicationContext(
                        _tooltip,
                        onShow: () =>
                        {
                            _shouldShow = true;
                            done.Set();
                        },
                        onExit: () =>
                        {
                            _shouldExit = true;
                            done.Set();
                        });

                    Application.Run(ctx);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                    done.Set();
                }
            });

            trayThread.IsBackground = true;
            trayThread.Name = "TrayControllerThread";
            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.Start();

            done.WaitOne();

            if (threadEx != null)
            {
                throw new InvalidOperationException("托盘线程运行失败。", threadEx);
            }

            return (_shouldShow, _shouldExit);
        }

        private sealed class TrayApplicationContext : ApplicationContext
        {
            private readonly NotifyIcon _notifyIcon;
            private readonly ContextMenuStrip _menu;
            private readonly Action _onShow;
            private readonly Action _onExit;

            public TrayApplicationContext(string tooltip, Action onShow, Action onExit)
            {
                _onShow = onShow;
                _onExit = onExit;

                _menu = new ContextMenuStrip();

                var showItem = new ToolStripMenuItem("Show (回到前台)");
                var exitItem = new ToolStripMenuItem("Exit (退出)");

                showItem.Click += (_, __) =>
                {
                    _onShow();
                    ExitThread();
                };

                exitItem.Click += (_, __) =>
                {
                    _onExit();
                    ExitThread();
                };

                _menu.Items.Add(showItem);
                _menu.Items.Add(exitItem);

                _notifyIcon = new NotifyIcon
                {
                    Text = tooltip,
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true,
                    ContextMenuStrip = _menu
                };

                _notifyIcon.MouseUp += NotifyIcon_MouseUp;
            }

            private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    _menu.Show(Cursor.Position);
                }
            }

            protected override void ExitThreadCore()
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _menu.Dispose();
                base.ExitThreadCore();
            }
        }
    }

    internal readonly struct WindowInfo
    {
        public IntPtr Hwnd { get; }
        public string Title { get; }

        public WindowInfo(IntPtr hwnd, string title)
        {
            Hwnd = hwnd;
            Title = title;
        }
    }

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
            IntPtr consoleHwnd = NativeMethods.GetConsoleWindow();
            if (consoleHwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(consoleHwnd, NativeMethods.SW_HIDE);
            }

            var tray = new TrayController("Window Title Renamer");
            var result = tray.Run();

            if (consoleHwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(consoleHwnd, NativeMethods.SW_SHOW);
                NativeMethods.SetForegroundWindow(consoleHwnd);
            }

            return result;
        }

        private static int ReadIntAllow0(string prompt, int lo, int hi)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine()?.Trim();

                if (int.TryParse(s, out int v))
                {
                    if (v == 0) return 0;
                    if (v >= lo && v <= hi) return v;
                }

                Console.WriteLine($"请输入 0 或 {lo} 到 {hi} 的整数。");
            }
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
            Console.WriteLine("输入编号时输入 0：隐藏到托盘后台运行（Show 返回 / Exit 退出）");
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
                    Console.WriteLine(new string('-', 90));

                    for (int i = 0; i < wins.Count; i++)
                    {
                        var w = wins[i];
                        string mark = rules.ContainsKey(w.Hwnd) ? " *保持中*" : "";
                        Console.WriteLine(
                            $"[{i + 1,3}] {w.Title}{mark}   (HWND=0x{w.Hwnd.ToInt64():X16})");
                    }

                    Console.WriteLine(new string('-', 90));

                    int choice = ReadIntAllow0(
                        "请输入要重命名的窗口编号（或 0 隐藏到托盘）: ",
                        1, wins.Count);

                    if (choice == 0)
                    {
                        var (_, shouldExit) = HideToTray();
                        if (shouldExit)
                            return 0;

                        Console.WriteLine();
                        continue;
                    }

                    WindowInfo selected = wins[choice - 1];

                    if (!NativeMethods.IsWindow(selected.Hwnd))
                    {
                        Console.WriteLine("目标窗口已不存在（可能已关闭）。回到列表。\n");
                        continue;
                    }

                    string newTitle = ReadNonEmpty("请输入新窗口标题: ");
                    bool persist = ReadYesNo("是否长久保持（每秒重复设置一次）？(y/n): ");

                    bool ok = SetWindowTitle(selected.Hwnd, newTitle);
                    if (ok)
                    {
                        Console.WriteLine($"已重命名：{selected.Title} -> {newTitle}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"重命名失败（GetLastError={Marshal.GetLastWin32Error()}）。可能原因：权限不足/窗口不接受 SetWindowText。");
                    }

                    if (persist)
                    {
                        keeper.AddOrUpdate(selected.Hwnd, newTitle);
                        Console.WriteLine("已加入长久保持。\n");
                    }
                    else
                    {
                        keeper.Remove(selected.Hwnd);
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

    internal sealed class KeyboardInterruptException : Exception
    {
    }
}
