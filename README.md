**English** | [简体中文](README.zh-CN.md)

<p align="center">
  <img src="res/gui.png" width="85%" alt="Window Title Renamer GUI" />
</p>

# Window Title Renamer

Rename the title of any visible Windows desktop window, including its taskbar label, from a lightweight graphical interface. The app can also keep re-applying a title when another program tries to restore it.

**Windows x64 only.** Releases are self-contained single-file executables and do not require a separate .NET installation.

## Features

- Search and refresh open windows from a modern single-page interface
- Load and refresh the window list in the background without blocking the interface
- Rename a selected window immediately
- Keep a title in place by re-applying it every second
- Run in the system tray while keeping rules active
- Switch between English and Simplified Chinese at runtime
- Automatically remove rules when their target window closes

Persistent rules last for the current app session only and are cleared when Window Title Renamer exits.

## Usage

1. Download the executable from the **Releases** page and run it.
2. Search for a window title or select a window from the automatically refreshed list. The list refreshes every two seconds while the main window is visible.
3. Enter the new title, optionally enable **Keep this title**, and choose **Apply title**. Pressing <kbd>Enter</kbd> also applies the change.
4. Use **Stop keeping** to remove an active rule.
5. Minimize or close the main window to send the app to the system tray. Double-click the tray icon to restore it, or use the tray menu to exit completely.

Some elevated applications or windows that reject `SetWindowText` may require running Window Title Renamer with matching privileges or may not support renaming.

## Build

```powershell
dotnet build
dotnet publish -c Release -r win-x64
```

Each publish is archived under `bin/Release-Archives/` as a timestamped self-contained ZIP package.

<p align="center">
  <img src="https://img.shields.io/github/downloads/SHthemW/Window-Title-Renamer/total" alt="downloads" />
</p>
