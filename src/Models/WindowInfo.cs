namespace WindowTitleRenamer;

internal sealed record WindowInfo(IntPtr Hwnd, string Title)
{
    public string HandleText => $"0x{Hwnd.ToInt64():X16}";
}
