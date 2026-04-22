using System.Windows.Forms;

namespace WindowTitleRenamer;

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
