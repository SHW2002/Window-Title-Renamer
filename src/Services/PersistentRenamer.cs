using System.Text.Json;

namespace WindowTitleRenamer;

internal sealed class PersistentRenamer : IDisposable
{
    private const int MaximumTitleLength = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };
    private readonly object _syncRoot = new();
    private readonly Dictionary<IntPtr, RuleBinding> _activeRules = [];
    private readonly Dictionary<string, string> _storedRules =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly WindowIdentityResolver _identityResolver;
    private readonly string _rulesPath;
    private readonly CancellationTokenSource _cancellation = new();
    private Thread? _workerThread;

    public PersistentRenamer(
        WindowIdentityResolver? identityResolver = null,
        string? rulesPath = null)
    {
        _identityResolver = identityResolver ?? new WindowIdentityResolver();
        _rulesPath = rulesPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowTitleRenamer",
            "rules.json");
        LoadStoredRules();
    }

    public void Start()
    {
        if (_workerThread is not null)
        {
            return;
        }

        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "PersistentRenamerWorker",
        };
        _workerThread.Start();
    }

    public void AddOrUpdate(IntPtr hwnd, string title)
    {
        string? stableKey = _identityResolver.ResolveStableKey(hwnd);
        lock (_syncRoot)
        {
            RemoveActiveBindingsForKeyLocked(stableKey, hwnd);
            _activeRules[hwnd] = new RuleBinding(stableKey, title);
            if (stableKey is not null)
            {
                _storedRules[stableKey] = title;
                SaveStoredRulesLocked();
            }
        }
    }

    public void Remove(IntPtr hwnd)
    {
        string? stableKey = _identityResolver.ResolveStableKey(hwnd);
        lock (_syncRoot)
        {
            _activeRules.Remove(hwnd);
            if (stableKey is not null)
            {
                _storedRules.Remove(stableKey);
                RemoveActiveBindingsForKeyLocked(stableKey, IntPtr.Zero);
                SaveStoredRulesLocked();
            }
        }
    }

    public bool TryGetRule(IntPtr hwnd, out string? title)
    {
        string? stableKey = _identityResolver.ResolveStableKey(hwnd);
        lock (_syncRoot)
        {
            if (stableKey is not null && _storedRules.TryGetValue(stableKey, out title))
            {
                RemoveActiveBindingsForKeyLocked(stableKey, hwnd);
                _activeRules[hwnd] = new RuleBinding(stableKey, title);
                return true;
            }

            if (_activeRules.TryGetValue(hwnd, out RuleBinding? active))
            {
                title = active.Title;
                return true;
            }
        }

        title = null;
        return false;
    }

    public IReadOnlyDictionary<IntPtr, string> ListRules()
    {
        lock (_syncRoot)
        {
            return _activeRules
                .Where(pair => NativeMethods.IsWindow(pair.Key))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Title);
        }
    }

    public void Stop()
    {
        _cancellation.Cancel();
        _workerThread?.Join(TimeSpan.FromSeconds(2));
    }

    private void WorkerLoop()
    {
        WaitHandle cancellationHandle = _cancellation.Token.WaitHandle;
        while (!cancellationHandle.WaitOne(TimeSpan.FromSeconds(1)))
        {
            RebindStoredRules();

            List<KeyValuePair<IntPtr, RuleBinding>> rules;
            lock (_syncRoot)
            {
                List<IntPtr> closedWindows = _activeRules
                    .Where(pair => !NativeMethods.IsWindow(pair.Key))
                    .Select(pair => pair.Key)
                    .ToList();
                foreach (IntPtr hwnd in closedWindows)
                {
                    _activeRules.Remove(hwnd);
                }

                rules = [.. _activeRules];
            }

            foreach ((IntPtr hwnd, RuleBinding rule) in rules)
            {
                if (NativeMethods.IsWindow(hwnd))
                {
                    NativeMethods.SetWindowTextW(hwnd, rule.Title);
                }
            }
        }
    }

    private void RebindStoredRules()
    {
        Dictionary<string, string> stored;
        lock (_syncRoot)
        {
            stored = new Dictionary<string, string>(_storedRules, StringComparer.OrdinalIgnoreCase);
        }

        foreach ((string stableKey, string title) in stored)
        {
            bool alreadyBound;
            lock (_syncRoot)
            {
                alreadyBound = _activeRules.Any(pair =>
                    string.Equals(pair.Value.StableKey, stableKey, StringComparison.OrdinalIgnoreCase) &&
                    NativeMethods.IsWindow(pair.Key));
            }

            if (alreadyBound)
            {
                continue;
            }

            IntPtr hwnd = _identityResolver.FindWindow(stableKey);
            if (hwnd == IntPtr.Zero)
            {
                continue;
            }

            lock (_syncRoot)
            {
                RemoveActiveBindingsForKeyLocked(stableKey, hwnd);
                _activeRules[hwnd] = new RuleBinding(stableKey, title);
            }
        }
    }

    private void LoadStoredRules()
    {
        try
        {
            if (!File.Exists(_rulesPath))
            {
                return;
            }

            Dictionary<string, string>? rules = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(_rulesPath),
                JsonOptions);
            if (rules is null)
            {
                return;
            }

            foreach ((string key, string title) in rules)
            {
                if (key.StartsWith("project:", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(title) &&
                    title.Length <= MaximumTitleLength)
                {
                    _storedRules[key] = title;
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt or inaccessible cache must not prevent the renamer from
            // handling the current session's windows.
        }
    }

    private void SaveStoredRulesLocked()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_rulesPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = _rulesPath + ".tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(_storedRules, JsonOptions));
            File.Move(temporaryPath, _rulesPath, true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The live title rule remains active even if persistence is blocked.
        }
    }

    private void RemoveActiveBindingsForKeyLocked(string? stableKey, IntPtr except)
    {
        if (stableKey is null)
        {
            return;
        }

        foreach (IntPtr existing in _activeRules
                     .Where(pair => pair.Key != except &&
                         string.Equals(
                             pair.Value.StableKey,
                             stableKey,
                             StringComparison.OrdinalIgnoreCase))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _activeRules.Remove(existing);
        }
    }

    public void Dispose()
    {
        Stop();
        _cancellation.Dispose();
    }

    private sealed record RuleBinding(string? StableKey, string Title);
}
