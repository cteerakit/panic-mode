# 🛡️ PanicMode

**PanicMode** is a lightweight, system-tray utility for Windows that allows you to instantly hide all active windows with a customizable global hotkey. Perfect for maintaining privacy or clearing your workspace in a single keystroke.

![PanicMode Logo](Resources/icon.ico) *(Note: Icon path used here for reference)*

---

## 🚀 Key Features

-   **⌨️ Global Hotkey**: Register a custom key combination (Default: `Ctrl + Q`) to toggle window visibility from anywhere in Windows.
-   **🕵️ Hidden Activity**: Instantly minimizes all top-level application windows.
-   **🔗 Panic URL**: Automatically opens a "safe" URL (e.g., Google, a news site, or a work dashboard) when activated.
-   **🖱️ Tray-Based Interface**: Runs silently in the background via the system tray.
-   **⚡ Rapid-Press Trigger**: Configure the number of rapid presses required to trigger the toggle (e.g., press `Ctrl+Q` three times quickly to hide).
-   **🏠 Run at Startup**: Optional setting to launch automatically with Windows.
-   **🛠️ Customizable Settings**: Tailor the hotkey, modifier keys, and trigger window duration through an easy-to-use interface.

## 🛠️ Requirements

-   **Operating System**: Windows 10/11
-   **Runtime**: [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## 📥 Installation

### Using the Installer
If available, download and run the `PanicMode-Setup.exe` from the [releases](https://github.com/yourusername/panic-mode/releases) page.

### Running from Source
1.  Clone the repository:
    ```bash
    git clone https://github.com/yourusername/panic-mode.git
    ```
2.  Open the project in Visual Studio 2022 (or later).
3.  Restore dependencies and build the solution:
    ```bash
    dotnet build
    ```
4.  Run the application:
    ```bash
    dotnet run --project PanicMode.csproj
    ```

## 🎮 Usage

1.  **Launch PanicMode**: Once running, the application will appear in your system tray.
2.  **Toggle Windows**: Press the hotkey (Default: `Ctrl + Q`) to hide all active windows. Press it again to restore them.
3.  **Settings**: Double-click the tray icon or right-click and select **Settings** to customize:
    -   Hotkey & Modifier Keys
    -   Required trigger count (for rapid-press)
    -   Panic URL (the page to open when triggered)

## 📦 Building an Installer
The project includes an Inno Setup script (`installer.iss`). To generate the installer:
1.  Publish the project for x64:
    ```bash
    dotnet publish -c Release -r win-x64 --self-contained false
    ```
2.  Open `installer.iss` in [Inno Setup Compiler](https://jrsoftware.org/isinfo.php).
3.  Compile to generate `installer_output/PanicMode-Setup.exe`.

---

## 🔒 Technical Details

-   **Language**: C# 13 / .NET 10.0
-   **Framework**: Windows Forms (WinForms)
-   **Win32 APIs**:
    -   `RegisterHotKey` for global hotkey capturing.
    -   `EnumWindows` and `ShowWindow` for window manipulation.
    -   `NativeWindow` subclassing for hidden message handling.
-   **Persistence**: Settings are stored in `%AppData%\PanicMode\settings.json`.

---

## 📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
