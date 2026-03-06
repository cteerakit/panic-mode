using System.Runtime.InteropServices;
using System.Text;

namespace PanicMode;

/// <summary>
/// Enumerates, hides, and restores all visible top-level application windows.
/// </summary>
public sealed class WindowManager
{
    // ── P/Invoke ────────────────────────────────────────────────────────────────
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);   // true = minimized

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int SW_MINIMIZE   = 6;
    private const int SW_RESTORE    = 9;
    private const int SW_SHOW       = 5;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly List<(IntPtr handle, bool wasMinimized)> _hidden = new();
    private bool _isHidden;

    public bool IsHidden => _isHidden;

    /// <summary>Toggle between hiding all windows and restoring them.</summary>
    public void Toggle()
    {
        if (_isHidden)
            RestoreAll();
        else
            HideAll();
    }

    private void HideAll()
    {
        _hidden.Clear();

        IntPtr shellWindow  = GetShellWindow();
        uint   ownPid       = (uint)Environment.ProcessId;

        EnumWindows((hWnd, _) =>
        {
            // Skip invisible, shell desktop, and our own process
            if (!IsWindowVisible(hWnd))         return true;
            if (hWnd == shellWindow)            return true;
            if (GetWindowTextLength(hWnd) == 0) return true;

            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == ownPid)                  return true;

            bool wasMinimized = IsIconic(hWnd);
            _hidden.Add((hWnd, wasMinimized));

            if (!wasMinimized)
                ShowWindow(hWnd, SW_MINIMIZE);

            return true;
        }, IntPtr.Zero);

        _isHidden = true;
    }

    private void RestoreAll()
    {
        // Restore in reverse order so Z-order is roughly preserved
        for (int i = _hidden.Count - 1; i >= 0; i--)
        {
            var (hWnd, wasMinimized) = _hidden[i];
            if (!wasMinimized && IsWindowVisible(hWnd))
                ShowWindow(hWnd, SW_RESTORE);
        }

        _hidden.Clear();
        _isHidden = false;
    }
}
