using System.Collections.Concurrent;
using System.Management;
using System.Text.RegularExpressions;

namespace WindowTitleRenamer;

/// <summary>
/// Resolves a window to a stable project identity when its process was launched
/// with Unity's -projectPath argument. Window handles are intentionally treated
/// as ephemeral because Unity can recreate its main window while the process is
/// still running.
/// </summary>
internal sealed class WindowIdentityResolver
{
    private static readonly Regex ProjectPathPattern = new(
        @"(?:^|\s)-projectPath(?:\s+|=)(?:""(?<quoted>[^""]+)""|(?<plain>\S+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly ConcurrentDictionary<uint, CachedProjectPath> _cache = new();
    private readonly Func<IntPtr, string?>? _override;

    internal WindowIdentityResolver(Func<IntPtr, string?>? resolverOverride = null)
    {
        _override = resolverOverride;
    }

    public string? ResolveStableKey(IntPtr windowHandle)
    {
        if (_override is not null)
        {
            return _override(windowHandle);
        }

        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
        if (processId == 0)
        {
            return null;
        }

        string? projectPath = GetProjectPath(processId);
        return projectPath is null ? null : BuildProjectKey(projectPath);
    }

    public IntPtr FindWindow(string stableKey)
    {
        if (string.IsNullOrWhiteSpace(stableKey) || !stableKey.StartsWith(
                "project:",
                StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((windowHandle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(windowHandle) ||
                NativeMethods.GetShellWindow() == windowHandle)
            {
                return true;
            }

            if (string.Equals(
                    ResolveStableKey(windowHandle),
                    stableKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                found = windowHandle;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return found;
    }

    internal static string BuildProjectKey(string projectPath) =>
        $"project:{NormalizeProjectPath(projectPath)}";

    internal static bool TryParseProjectPath(
        string? commandLine,
        out string? projectPath)
    {
        projectPath = null;
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        Match match = ProjectPathPattern.Match(commandLine);
        if (!match.Success)
        {
            return false;
        }

        string value = match.Groups["quoted"].Success
            ? match.Groups["quoted"].Value
            : match.Groups["plain"].Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            projectPath = NormalizeProjectPath(value);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or NotSupportedException)
        {
            return false;
        }
    }

    private string? GetProjectPath(uint processId)
    {
        DateTime now = DateTime.UtcNow;
        if (_cache.TryGetValue(processId, out CachedProjectPath cached) &&
            now - cached.CapturedAtUtc < TimeSpan.FromSeconds(5))
        {
            return cached.ProjectPath;
        }

        string? projectPath = null;
        try
        {
            using ManagementObjectSearcher searcher = new(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            foreach (ManagementObject result in searcher.Get())
            {
                if (TryParseProjectPath(result["CommandLine"] as string, out string? parsed))
                {
                    projectPath = parsed;
                }

                break;
            }
        }
        catch (Exception exception) when (
            exception is ManagementException or System.ComponentModel.Win32Exception or
            UnauthorizedAccessException or InvalidOperationException)
        {
            // A protected process may deny command-line inspection. Such windows
            // continue to work with their current-handle-only behavior.
        }

        _cache[processId] = new CachedProjectPath(now, projectPath);
        return projectPath;
    }

    private static string NormalizeProjectPath(string projectPath) =>
        Path.GetFullPath(projectPath.Trim().Replace('/', Path.DirectorySeparatorChar))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private readonly record struct CachedProjectPath(
        DateTime CapturedAtUtc,
        string? ProjectPath);
}
