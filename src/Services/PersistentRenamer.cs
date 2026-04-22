namespace WindowTitleRenamer;

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
