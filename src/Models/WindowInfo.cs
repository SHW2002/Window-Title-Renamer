namespace WindowTitleRenamer;

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
