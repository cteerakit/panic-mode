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
            HandleAudioPanic();
            
            if (_windowManager.IsHidden && !string.IsNullOrWhiteSpace(_settings.PanicUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _settings.PanicUrl,
                        UseShellExecute = true
                    });

                    // Browsers often ignore WindowStyle.Maximized via ShellExecute.
                    // Wait half a second for the browser window to open/focus, then force maximize.
                    if (_settings.MaximizeWindow)
                    {
                        System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
                        {
                            IntPtr fw = GetForegroundWindow();
                            if (fw != IntPtr.Zero)
                            {
                                ShowWindow(fw, SW_MAXIMIZE);
                            }
                        });
                    }
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
    private bool _wasMutedBeforePanic = false;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MAXIMIZE = 3;

    private void HandleAudioPanic()
    {
        try
        {
            using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
            if (device == null) return;

            var volume = device.AudioEndpointVolume;
            
            if (_windowManager.IsHidden)
            {
                // We just entered panic mode
                _wasMutedBeforePanic = volume.Mute;
                if (!_wasMutedBeforePanic)
                {
                    volume.Mute = true;
                }
            }
            else
            {
                // We just left panic mode, restore state
                volume.Mute = _wasMutedBeforePanic;
            }
        }
        catch { }
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
