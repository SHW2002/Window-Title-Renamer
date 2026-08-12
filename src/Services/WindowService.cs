using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowTitleRenamer;

internal sealed class WindowService
{
    private readonly uint _currentProcessId = (uint)Environment.ProcessId;
    private readonly NativeMethods.EnumWindowsProc _enumWindowsCallback;
    private List<WindowInfo>? _enumeratedWindows;

    public WindowService()
    {
        _enumWindowsCallback = EnumerateWindow;
    }

    public IReadOnlyList<WindowInfo> ListOpenWindows()
    {
        _enumeratedWindows = [];

        try
        {
            if (!NativeMethods.EnumWindows(_enumWindowsCallback, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumWindows failed.");
            }

            _enumeratedWindows.Sort((left, right) =>
            {
                int titleComparison = string.Compare(
                    left.Title, right.Title, StringComparison.CurrentCultureIgnoreCase);
                return titleComparison != 0
                    ? titleComparison
                    : left.Hwnd.ToInt64().CompareTo(right.Hwnd.ToInt64());
            });

            return _enumeratedWindows;
        }
        finally
        {
            _enumeratedWindows = null;
        }
    }

    public static bool IsWindowAvailable(IntPtr hwnd) => NativeMethods.IsWindow(hwnd);

    public static RenameResult Rename(IntPtr hwnd, string title)
    {
        if (!NativeMethods.IsWindow(hwnd))
        {
            return RenameResult.WindowGone;
        }

        bool success = NativeMethods.SetWindowTextW(hwnd, title);
        return success
            ? RenameResult.Success
            : new RenameResult(false, Marshal.GetLastWin32Error(), false);
    }

    private bool EnumerateWindow(IntPtr hwnd, IntPtr _)
    {
        if (_enumeratedWindows is null || hwnd == NativeMethods.GetShellWindow())
        {
            return true;
        }

        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            return true;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == _currentProcessId)
        {
            return true;
        }

        string title = GetWindowTitle(hwnd);
        if (!string.IsNullOrWhiteSpace(title))
        {
            _enumeratedWindows.Add(new WindowInfo(hwnd, title));
        }

        return true;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int length = NativeMethods.GetWindowTextLengthW(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        StringBuilder buffer = new(length + 1);
        return NativeMethods.GetWindowTextW(hwnd, buffer, buffer.Capacity) > 0
            ? buffer.ToString().Trim()
            : string.Empty;
    }
}

internal readonly record struct RenameResult(bool Succeeded, int ErrorCode, bool WindowNoLongerExists)
{
    public static RenameResult Success { get; } = new(true, 0, false);
    public static RenameResult WindowGone { get; } = new(false, 0, true);
}
