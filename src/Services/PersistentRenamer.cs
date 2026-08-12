namespace WindowTitleRenamer;

internal sealed class PersistentRenamer : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<IntPtr, string> _rules = [];
    private readonly CancellationTokenSource _cancellation = new();
    private Thread? _workerThread;

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
        lock (_syncRoot)
        {
            _rules[hwnd] = title;
        }
    }

    public void Remove(IntPtr hwnd)
    {
        lock (_syncRoot)
        {
            _rules.Remove(hwnd);
        }
    }

    public IReadOnlyDictionary<IntPtr, string> ListRules()
    {
        lock (_syncRoot)
        {
            return new Dictionary<IntPtr, string>(_rules);
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
            List<KeyValuePair<IntPtr, string>> rules;
            lock (_syncRoot)
            {
                rules = [.. _rules];
            }

            List<IntPtr> closedWindows = [];
            foreach ((IntPtr hwnd, string title) in rules)
            {
                if (!NativeMethods.IsWindow(hwnd))
                {
                    closedWindows.Add(hwnd);
                    continue;
                }

                NativeMethods.SetWindowTextW(hwnd, title);
            }

            if (closedWindows.Count == 0)
            {
                continue;
            }

            lock (_syncRoot)
            {
                foreach (IntPtr hwnd in closedWindows)
                {
                    _rules.Remove(hwnd);
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cancellation.Dispose();
    }
}
