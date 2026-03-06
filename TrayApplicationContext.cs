using PanicMode.Forms;
using PanicMode.Models;

namespace PanicMode;

/// <summary>
/// Hosts the system tray icon, context menu, hotkey listener, and window manager.
/// A hidden <see cref="MessageWindow"/> is used to receive WM_HOTKEY messages.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon      _trayIcon;
    private readonly WindowManager   _windowManager  = new();
    private          HotkeyManager?  _hotkeyManager;
    private          AppSettings     _settings;
    private          MessageWindow?  _messageWindow;
    private          ToolStripMenuItem? _startupMenuItem;

    public TrayApplicationContext()
    {
        _settings = SettingsManager.Load();

        // ── Tray icon ────────────────────────────────────────────────────────
        _trayIcon = new NotifyIcon
        {
            Text    = "PanicMode",
            Icon    = LoadIcon(),
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        // ── Context menu ─────────────────────────────────────────────────────
        _startupMenuItem = new ToolStripMenuItem("Run at Startup", null, OnToggleStartup)
        {
            Checked = SettingsManager.IsStartupEnabled()
        };

        _trayIcon.ContextMenuStrip = new ContextMenuStrip();
        _trayIcon.ContextMenuStrip.Items.AddRange([
            new ToolStripMenuItem("Settings", null, (_, _) => ShowSettings()),
            _startupMenuItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitApp())
        ]);

        // ── Message window + hotkey ───────────────────────────────────────────
        _messageWindow = new MessageWindow();
        _messageWindow.HotkeyMessageReceived += OnHotkeyMessage;

        _hotkeyManager = new HotkeyManager(_messageWindow.Handle, _settings);
        _hotkeyManager.HotkeyTriggered += (_, _) => 
        {
            _windowManager.Toggle();
            if (_windowManager.IsHidden && !string.IsNullOrWhiteSpace(_settings.PanicUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _settings.PanicUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        };
        _hotkeyManager.Register();
    }

    // ── Tray menu handlers ────────────────────────────────────────────────────

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings = form.ResultSettings!;
            SettingsManager.Save(_settings);
            _hotkeyManager?.UpdateSettings(_settings);
        }
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        bool newState = !(_startupMenuItem?.Checked ?? false);
        SettingsManager.SetStartup(newState);
        if (_startupMenuItem != null)
            _startupMenuItem.Checked = newState;
    }

    private void ExitApp()
    {
        _hotkeyManager?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── WM_HOTKEY pipe ────────────────────────────────────────────────────────

    private void OnHotkeyMessage(object? sender, EventArgs e)
    {
        _hotkeyManager?.HandleHotkeyMessage();
    }

    // ── Icon loader ───────────────────────────────────────────────────────────

    private static Icon LoadIcon()
    {
        try
        {
            // Try to load the embedded icon
            var stream = typeof(TrayApplicationContext)
                .Assembly
                .GetManifestResourceStream("PanicMode.Resources.icon.ico");
            if (stream != null)
                return new Icon(stream);
        }
        catch { /* fall through to default */ }

        // Fallback: use a stock system icon
        return SystemIcons.Application;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeyManager?.Dispose();
            _messageWindow?.DestroyHandle();
            _messageWindow?.ReleaseHandle();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Invisible window used purely to receive WM_HOTKEY from the OS.
/// </summary>
internal sealed class MessageWindow : NativeWindow
{
    public event EventHandler? HotkeyMessageReceived;

    public MessageWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotkeyManager.WM_HOTKEY)
            HotkeyMessageReceived?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }
}
