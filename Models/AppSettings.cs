namespace PanicMode.Models;

public class AppSettings
{
    /// <summary>Win32 modifier flags: MOD_ALT=1, MOD_CONTROL=2, MOD_SHIFT=4, MOD_WIN=8</summary>
    public int ModifierKeys { get; set; } = 2; // MOD_CONTROL

    /// <summary>Virtual key code for the hotkey.</summary>
    public int HotKey { get; set; } = (int)Keys.Q;

    /// <summary>How many rapid presses are required to trigger the toggle.</summary>
    public int RequiredPressCount { get; set; } = 1;

    /// <summary>Maximum milliseconds between consecutive presses to count them as rapid.</summary>
    public int PressWindowMs { get; set; } = 500;

    /// <summary>Whether to add PanicMode to HKCU Run on startup.</summary>
    public bool RunAtStartup { get; set; } = false;

    /// <summary>Optional URL to open when panic mode is activated.</summary>
    public string PanicUrl { get; set; } = "https://google.com";
}
