using System.Runtime.InteropServices;
using PanicMode.Models;

namespace PanicMode;

/// <summary>
/// Manages registering/unregistering a global hotkey with Windows and tracking
/// rapid consecutive presses before firing the <see cref="HotkeyTriggered"/> event.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    // ── P/Invoke ────────────────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 9001;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly IntPtr _windowHandle;
    private AppSettings _settings;
    private int _pressCount;
    private DateTime _lastPressTime = DateTime.MinValue;
    private bool _disposed;

    /// <summary>Raised when the hotkey has been pressed the required number of times within the configured window.</summary>
    public event EventHandler? HotkeyTriggered;

    public HotkeyManager(IntPtr windowHandle, AppSettings settings)
    {
        _windowHandle = windowHandle;
        _settings = settings;
    }

    /// <summary>Register (or re-register after a settings change) the hotkey.</summary>
    public bool Register()
    {
        Unregister();
        bool ok = RegisterHotKey(_windowHandle, HotkeyId, (uint)_settings.ModifierKeys, (uint)_settings.HotKey);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            MessageBox.Show(
                $"Failed to register hotkey (Win32 error {err}).\n" +
                "Another application may already be using this key combination.",
                "PanicMode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return ok;
    }

    public void Unregister()
    {
        UnregisterHotKey(_windowHandle, HotkeyId);
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        _pressCount = 0;
        Register();
    }

    /// <summary>
    /// Call this from your WndProc when <c>msg == WM_HOTKEY</c> and
    /// <c>wParam == HotkeyId</c>.
    /// </summary>
    public void HandleHotkeyMessage()
    {
        DateTime now = DateTime.UtcNow;
        double elapsed = (now - _lastPressTime).TotalMilliseconds;

        if (elapsed > _settings.PressWindowMs)
        {
            // Window expired — reset the counter
            _pressCount = 1;
        }
        else
        {
            _pressCount++;
        }

        _lastPressTime = now;

        if (_pressCount >= _settings.RequiredPressCount)
        {
            _pressCount = 0;
            _lastPressTime = DateTime.MinValue;
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Unregister();
        _disposed = true;
    }
}
