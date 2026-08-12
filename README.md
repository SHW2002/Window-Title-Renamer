<p align="center">
  <img src="res/gui.png" width="85%" alt="Window Title Renamer GUI" />
</p>

# Window Title Renamer

Rename the title of any visible Windows desktop window, including its taskbar label, from a lightweight graphical interface. The app can also keep re-applying a title when another program tries to restore it.

**Windows x64 only.** Releases are self-contained single-file executables and do not require a separate .NET installation.

## Features

- Search and refresh open windows from a modern single-page interface
- Rename a selected window immediately
- Keep a title in place by re-applying it every second
- Run in the system tray while keeping rules active
- Switch between English and Simplified Chinese at runtime
- Automatically remove rules when their target window closes

Persistent rules last for the current app session only and are cleared when Window Title Renamer exits.

## Usage

1. Download the executable from the **Releases** page and run it.
2. Search for a window title or select a window from the automatically refreshed list.
3. Enter the new title, optionally enable **Keep this title**, and choose **Apply title**. Pressing <kbd>Enter</kbd> also applies the change.
4. Use **Stop keeping** to remove an active rule.
5. Minimize or close the main window to send the app to the system tray. Double-click the tray icon to restore it, or use the tray menu to exit completely.

Some elevated applications or windows that reject `SetWindowText` may require running Window Title Renamer with matching privileges or may not support renaming.

## Build

```powershell
dotnet build
dotnet publish -c Release
```

Each publish is archived under `bin/Release-Archives/` as a timestamped ZIP package.

<br>

# 窗口标题重命名工具

通过轻量图形界面修改任意可见 Windows 桌面窗口的标题，包括任务栏按钮标签。当其他程序尝试恢复原标题时，本程序还可以持续重新应用指定标题。

**仅支持 Windows x64。** 发布版本为自包含的单文件程序，无需另行安装 .NET。

## 功能

- 在现代化单页界面中搜索和刷新已打开的窗口
- 立即修改所选窗口的标题
- 每秒重新应用一次标题，防止标题被其他程序还原
- 隐藏到系统托盘后继续维持规则
- 运行时切换英文和简体中文
- 目标窗口关闭后自动移除对应规则

持续保持规则仅在本次程序运行期间有效，彻底退出后会被清空。

## 用法

1. 从**发布页面**下载可执行文件并运行。
2. 搜索窗口标题，或从自动刷新的窗口列表中选择目标窗口。
3. 输入新标题，按需启用**持续保持此标题**，然后点击**应用标题**；也可以按 <kbd>Enter</kbd> 应用。
4. 点击**停止保持**可移除活动规则。
5. 最小化或关闭主窗口会将程序隐藏到系统托盘；双击托盘图标可恢复窗口，也可通过托盘菜单彻底退出。

对于以管理员权限运行或拒绝 `SetWindowText` 的窗口，可能需要以相同权限运行本工具，也可能无法完成重命名。

## 构建

```powershell
dotnet build
dotnet publish -c Release
```

每次发布都会在 `bin/Release-Archives/` 下生成带时间戳的 ZIP 归档包。

<p align="center">
  <img src="https://img.shields.io/github/downloads/SHthemW/Window-Title-Renamer/total" alt="downloads" />
</p>
