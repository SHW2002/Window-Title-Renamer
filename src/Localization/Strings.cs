namespace WindowTitleRenamer.Localization;

internal sealed class Strings
{
    public string BannerTitle { get; init; } = "";
    public string BannerSubtitle { get; init; } = "";
    public string PromptSelectWindow { get; init; } = "";
    public string PromptNewTitle { get; init; } = "";
    public string PromptPersist { get; init; } = "";
    public string ErrorEmpty { get; init; } = "";
    public string ErrorNoMatch { get; init; } = "";
    public string ErrorRange { get; init; } = "";
    public string ErrorWindowGone { get; init; } = "";
    public string ErrorYesNo { get; init; } = "";
    public string ErrorRenameFailed { get; init; } = "";
    public string ErrorRenameReason { get; init; } = "";
    public string StatusMatchSingle { get; init; } = "";
    public string StatusMatchMultiple { get; init; } = "";
    public string StatusNoWindows { get; init; } = "";
    public string StatusPersistRules { get; init; } = "";
    public string StatusRenamed { get; init; } = "";
    public string StatusAddedPersist { get; init; } = "";
    public string StatusCtrlC { get; init; } = "";
    public string ColIndex { get; init; } = "";
    public string ColTitle { get; init; } = "";
    public string ColStatus { get; init; } = "";
    public string MarkPersisting { get; init; } = "";
    public string TrayShow { get; init; } = "";
    public string TrayExit { get; init; } = "";

    public static readonly Strings En = new()
    {
        BannerTitle = "Window Title Renamer",
        BannerSubtitle = "Enter a number to select, text to search, 0 to hide to tray",
        PromptSelectWindow = "Enter index or keyword (0 = hide to tray): ",
        PromptNewTitle = "Enter new window title: ",
        PromptPersist = "Keep persistent (re-apply every second)? (y/n): ",
        ErrorEmpty = "Cannot be empty.",
        ErrorNoMatch = "No matching windows. Try again.",
        ErrorRange = "Enter 0 or a number from 1 to {0}.",
        ErrorWindowGone = "  Target window no longer exists (closed?), returning to list.\n",
        ErrorYesNo = "Please enter y/n.",
        ErrorRenameFailed = "  Rename failed (GetLastError={0})",
        ErrorRenameReason = "  Possible cause: insufficient permission or window rejects SetWindowText.",
        StatusMatchSingle = "Matched: {0}",
        StatusMatchMultiple = "\nMatched {0} windows:",
        StatusNoWindows = "  No renameable windows found (visible, non-empty title). Retrying in 1 s...",
        StatusPersistRules = "  Persistent rules: {0} active (re-applied every second)\n",
        StatusRenamed = "  Renamed: {0} → {1}",
        StatusAddedPersist = "  Added to persistent rules.",
        StatusCtrlC = "\nCtrl+C received, exiting.",
        ColIndex = "Index",
        ColTitle = "Window Title",
        ColStatus = "Status",
        MarkPersisting = "*keeping*",
        TrayShow = "Show",
        TrayExit = "Exit",
    };

    public static readonly Strings ZhCN = new()
    {
        BannerTitle = "Window Title Renamer",
        BannerSubtitle = "输入编号选择窗口，输入文字模糊搜索窗口，输入 0 隐藏到托盘",
        PromptSelectWindow = "请输入编号或关键字（0 隐藏到托盘）: ",
        PromptNewTitle = "请输入新窗口标题: ",
        PromptPersist = "是否长久保持（每秒重复设置一次）？(y/n): ",
        ErrorEmpty = "不能为空。",
        ErrorNoMatch = "没有匹配的窗口，请重新输入。",
        ErrorRange = "请输入 0 或 1 到 {0} 的编号。",
        ErrorWindowGone = "  目标窗口已不存在（可能已关闭），回到列表。\n",
        ErrorYesNo = "请输入 y/n（也可以输入 是/否）。",
        ErrorRenameFailed = "  重命名失败（GetLastError={0}）",
        ErrorRenameReason = "  可能原因：权限不足/窗口不接受 SetWindowText。",
        StatusMatchSingle = "匹配到：{0}",
        StatusMatchMultiple = "\n匹配到 {0} 个窗口：",
        StatusNoWindows = "  没有找到可重命名的窗口（可见且标题非空），1 秒后重试…",
        StatusPersistRules = "  长久保持规则：{0} 条（后台每秒重设一次）\n",
        StatusRenamed = "  已重命名：{0} → {1}",
        StatusAddedPersist = "  已加入长久保持。",
        StatusCtrlC = "\n收到 Ctrl+C，退出。",
        ColIndex = "编号",
        ColTitle = "窗口标题",
        ColStatus = "状态",
        MarkPersisting = "*保持中*",
        TrayShow = "Show (回到前台)",
        TrayExit = "Exit (退出)",
    };

    public static Strings Current { get; private set; } = En;

    public static void SetLanguage(string lang)
    {
        Current = lang.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ? ZhCN : En;
    }
}
