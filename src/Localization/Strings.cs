namespace WindowTitleRenamer.Localization;

internal sealed class Strings
{
    public string AppTitle { get; init; } = "";
    public string AppSubtitle { get; init; } = "";
    public string LanguageLabel { get; init; } = "";
    public string HideToTray { get; init; } = "";
    public string WindowListTitle { get; init; } = "";
    public string LoadingWindows { get; init; } = "";
    public string SearchPlaceholder { get; init; } = "";
    public string Refresh { get; init; } = "";
    public string WindowCount { get; init; } = "";
    public string FilteredWindowCount { get; init; } = "";
    public string ColumnTitle { get; init; } = "";
    public string ColumnStatus { get; init; } = "";
    public string ColumnHandle { get; init; } = "";
    public string Keeping { get; init; } = "";
    public string RenamePanelTitle { get; init; } = "";
    public string NoSelection { get; init; } = "";
    public string CurrentTitle { get; init; } = "";
    public string NewTitle { get; init; } = "";
    public string NewTitlePlaceholder { get; init; } = "";
    public string KeepTitle { get; init; } = "";
    public string KeepDescription { get; init; } = "";
    public string Apply { get; init; } = "";
    public string StopKeeping { get; init; } = "";
    public string SelectWindowHint { get; init; } = "";
    public string Ready { get; init; } = "";
    public string LoadedWindows { get; init; } = "";
    public string NoWindows { get; init; } = "";
    public string NoSearchResults { get; init; } = "";
    public string RenameSucceeded { get; init; } = "";
    public string KeepEnabled { get; init; } = "";
    public string KeepDisabled { get; init; } = "";
    public string EmptyTitleError { get; init; } = "";
    public string WindowGoneError { get; init; } = "";
    public string RenameFailedError { get; init; } = "";
    public string RefreshFailedError { get; init; } = "";
    public string SettingsSaveFailed { get; init; } = "";
    public string TrayShow { get; init; } = "";
    public string TrayExit { get; init; } = "";
    public string TrayRunningTitle { get; init; } = "";
    public string TrayRunningMessage { get; init; } = "";

    public static readonly Strings En = new()
    {
        AppTitle = "Window Title Renamer",
        AppSubtitle = "Find a window, rename it, and keep the title in place.",
        LanguageLabel = "Language",
        HideToTray = "Hide to tray",
        WindowListTitle = "Open windows",
        LoadingWindows = "Loading windows...",
        SearchPlaceholder = "Search window titles...",
        Refresh = "Refresh",
        WindowCount = "{0} windows",
        FilteredWindowCount = "{0} of {1} windows",
        ColumnTitle = "WINDOW TITLE",
        ColumnStatus = "STATUS",
        ColumnHandle = "HWND",
        Keeping = "KEEPING",
        RenamePanelTitle = "Rename selected window",
        NoSelection = "No window selected",
        CurrentTitle = "CURRENT TITLE",
        NewTitle = "NEW TITLE",
        NewTitlePlaceholder = "Enter a new window title",
        KeepTitle = "Keep this title",
        KeepDescription = "Re-apply it every second until the app exits.",
        Apply = "Apply title",
        StopKeeping = "Stop keeping",
        SelectWindowHint = "Select a window from the list to get started.",
        Ready = "Ready",
        LoadedWindows = "Loaded {0} windows · {1} active rules",
        NoWindows = "No renameable windows are currently open.",
        NoSearchResults = "No windows match the current search.",
        RenameSucceeded = "Renamed “{0}” to “{1}”.",
        KeepEnabled = "The title will be kept until this app exits.",
        KeepDisabled = "Persistent title keeping has been stopped.",
        EmptyTitleError = "Enter a non-empty title before applying.",
        WindowGoneError = "The selected window has closed or is no longer available.",
        RenameFailedError = "Rename failed (Win32 error {0}). The window may reject changes or require higher privileges.",
        RefreshFailedError = "Could not refresh the window list: {0}",
        SettingsSaveFailed = "The language changed, but the preference could not be saved: {0}",
        TrayShow = "Show Window Title Renamer",
        TrayExit = "Exit",
        TrayRunningTitle = "Still running",
        TrayRunningMessage = "Window Title Renamer will continue running in the background.",
    };

    public static readonly Strings ZhCN = new()
    {
        AppTitle = "窗口标题重命名工具",
        AppSubtitle = "查找窗口、修改标题，并按需持续保持。",
        LanguageLabel = "语言",
        HideToTray = "隐藏到托盘",
        WindowListTitle = "已打开的窗口",
        LoadingWindows = "正在加载窗口列表…",
        SearchPlaceholder = "搜索窗口标题…",
        Refresh = "刷新",
        WindowCount = "共 {0} 个窗口",
        FilteredWindowCount = "显示 {0} / {1} 个窗口",
        ColumnTitle = "窗口标题",
        ColumnStatus = "状态",
        ColumnHandle = "HWND",
        Keeping = "保持中",
        RenamePanelTitle = "重命名所选窗口",
        NoSelection = "尚未选择窗口",
        CurrentTitle = "当前标题",
        NewTitle = "新标题",
        NewTitlePlaceholder = "输入新的窗口标题",
        KeepTitle = "持续保持此标题",
        KeepDescription = "每秒重新应用一次，直到本程序退出。",
        Apply = "应用标题",
        StopKeeping = "停止保持",
        SelectWindowHint = "请先从左侧列表中选择一个窗口。",
        Ready = "就绪",
        LoadedWindows = "已加载 {0} 个窗口 · {1} 条保持规则",
        NoWindows = "当前没有可重命名的窗口。",
        NoSearchResults = "没有窗口符合当前搜索条件。",
        RenameSucceeded = "已将“{0}”重命名为“{1}”。",
        KeepEnabled = "程序退出前将持续保持此标题。",
        KeepDisabled = "已停止持续保持此标题。",
        EmptyTitleError = "应用前请输入非空的新标题。",
        WindowGoneError = "所选窗口已关闭或不再可用。",
        RenameFailedError = "重命名失败（Win32 错误 {0}）。窗口可能拒绝修改或需要更高权限。",
        RefreshFailedError = "无法刷新窗口列表：{0}",
        SettingsSaveFailed = "语言已切换，但无法保存偏好：{0}",
        TrayShow = "显示窗口标题重命名工具",
        TrayExit = "退出",
        TrayRunningTitle = "程序仍在运行",
        TrayRunningMessage = "窗口标题重命名工具将继续在后台运行。",
    };

    public static Strings Current { get; private set; } = En;

    public static void SetLanguage(string language)
    {
        Current = language.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ? ZhCN : En;
    }
}
