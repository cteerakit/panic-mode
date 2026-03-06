using System.Runtime.InteropServices;
using Microsoft.Win32;
using PanicMode.Models;

namespace PanicMode.Forms;

/// <summary>
/// Settings dialog allowing the user to configure hotkey, press count, and startup behaviour.
/// </summary>
public sealed class SettingsForm : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_V2 = 20;

    // ── UI controls ────────────────────────────────────────────────────────
    private readonly GroupBox      _hotkeyGroup;
    private readonly Label         _hotkeyLabel;
    private readonly TextBox       _hotkeyBox;
    private readonly CheckBox      _ctrlCheck;
    private readonly CheckBox      _altCheck;
    private readonly CheckBox      _shiftCheck;
    private readonly CheckBox      _winCheck;

    private readonly GroupBox      _behaviorGroup;
    private readonly Label         _pressCountLabel;
    private readonly NumericUpDown _pressCountNum;
    private readonly Label         _pressWindowLabel;
    private readonly NumericUpDown _pressWindowNum;
    private readonly Label         _panicUrlLabel;
    private readonly TextBox       _panicUrlBox;
    private readonly CheckBox      _maximizeCheck;

    private readonly CheckBox      _startupCheck;

    private readonly Button        _saveBtn;
    private readonly Button        _cancelBtn;

    // ── State ─────────────────────────────────────────────────────────────
    private Keys _capturedKey = Keys.None;
    private readonly AppSettings _original;
    public AppSettings? ResultSettings { get; private set; }

    private bool _isDarkTheme;

    public SettingsForm(AppSettings current)
    {
        _original = current;
        _isDarkTheme = IsDarkTheme();

        // Color palettes
        Color bg = _isDarkTheme ? Color.FromArgb(30, 30, 30) : Color.FromArgb(243, 243, 243);
        Color fg = _isDarkTheme ? Color.WhiteSmoke : Color.FromArgb(20, 20, 20);
        Color headerBg = _isDarkTheme ? Color.FromArgb(20, 20, 20) : Color.FromArgb(225, 225, 225);
        Color accent = _isDarkTheme ? Color.FromArgb(100, 210, 255) : Color.FromArgb(0, 102, 204);
        Color subText = _isDarkTheme ? Color.FromArgb(170, 170, 180) : Color.FromArgb(80, 80, 90);
        Color inputBg = _isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.White;
        Color inputFg = _isDarkTheme ? Color.WhiteSmoke : Color.Black;
        Color btnBg = _isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(220, 220, 220);

        // ── Form setup ────────────────────────────────────────────────────
        Text             = "PanicMode Settings";
        FormBorderStyle  = FormBorderStyle.FixedDialog;
        MaximizeBox      = false;
        MinimizeBox      = false;
        StartPosition    = FormStartPosition.CenterScreen;
        Size             = new Size(380, 390);
        BackColor        = bg;
        ForeColor        = fg;
        Font             = new Font("Segoe UI", 9f);

        // ── Hotkey group ─────────────────────────────────────────────────
        _hotkeyGroup = MakeGroup("Hotkey", 20, 95, accent, bg);

        _ctrlCheck  = MakeModCheck("Ctrl",  10, 25, fg);
        _altCheck   = MakeModCheck("Alt",   70, 25, fg);
        _shiftCheck = MakeModCheck("Shift", 130, 25, fg);
        _winCheck   = MakeModCheck("Win",   200, 25, fg);

        _hotkeyLabel = new Label
        {
            Text      = "Key:",
            ForeColor = subText,
            Location  = new Point(10, 57),
            AutoSize  = true
        };

        _hotkeyBox = new TextBox
        {
            Location    = new Point(42, 53),
            Size        = new Size(90, 23),
            ReadOnly    = true,
            BackColor   = inputBg,
            ForeColor   = inputFg,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor      = Cursors.Arrow,
            TabStop     = false
        };
        _hotkeyBox.KeyDown += OnHotkeyBoxKeyDown;
        _hotkeyBox.Click   += (_, _) => { _hotkeyBox.Text = "Press a key…"; _capturedKey = Keys.None; };

        _hotkeyGroup.Controls.AddRange([_ctrlCheck, _altCheck, _shiftCheck, _winCheck,
                                        _hotkeyLabel, _hotkeyBox]);

        // ── Behavior group ────────────────────────────────────────────────
        _behaviorGroup = MakeGroup("Behavior", 125, 155, accent, bg);

        _pressCountLabel = new Label
        {
            Text     = "Required press count:",
            ForeColor = subText,
            Location  = new Point(10, 25),
            AutoSize  = true
        };

        _pressCountNum = new NumericUpDown
        {
            Location   = new Point(160, 22),
            Size       = new Size(55, 23),
            Minimum    = 1,
            Maximum    = 5,
            Value      = Math.Clamp(current.RequiredPressCount, 1, 5),
            BackColor  = inputBg,
            ForeColor  = inputFg,
            BorderStyle = BorderStyle.FixedSingle
        };

        _pressWindowLabel = new Label
        {
            Text      = "Press window (ms):",
            ForeColor = subText,
            Location  = new Point(10, 58),
            AutoSize  = true
        };

        _pressWindowNum = new NumericUpDown
        {
            Location   = new Point(160, 55),
            Size       = new Size(65, 23),
            Minimum    = 100,
            Maximum    = 2000,
            Increment  = 100,
            Value      = Math.Clamp(current.PressWindowMs, 100, 2000),
            BackColor  = inputBg,
            ForeColor  = inputFg,
            BorderStyle = BorderStyle.FixedSingle
        };

        _panicUrlLabel = new Label
        {
            Text      = "Open URL on Panic:",
            ForeColor = subText,
            Location  = new Point(10, 91),
            AutoSize  = true
        };

        _panicUrlBox = new TextBox
        {
            Location    = new Point(135, 88),
            Size        = new Size(200, 23),
            BackColor   = inputBg,
            ForeColor   = inputFg,
            BorderStyle = BorderStyle.FixedSingle,
            Text        = current.PanicUrl
        };

        _maximizeCheck = new CheckBox
        {
            Text      = "Maximize window on Panic",
            ForeColor = fg,
            Location  = new Point(10, 124),
            AutoSize  = true,
            Checked   = current.MaximizeWindow
        };

        _behaviorGroup.Controls.AddRange([_pressCountLabel, _pressCountNum,
                                          _pressWindowLabel, _pressWindowNum,
                                          _panicUrlLabel, _panicUrlBox,
                                          _maximizeCheck]);

        // ── Startup checkbox ─────────────────────────────────────────────
        _startupCheck = new CheckBox
        {
            Text      = "Start with Windows",
            ForeColor = fg,
            Location  = new Point(20, 300),
            AutoSize  = true,
            Checked   = SettingsManager.IsStartupEnabled()
        };

        // ── Buttons ───────────────────────────────────────────────────────
        _saveBtn = MakeButton("Save", 195, 295, btnBg, fg);
        _saveBtn.BackColor = Color.FromArgb(0, 120, 215); // always distinct
        _saveBtn.ForeColor = Color.White;
        _saveBtn.Click += OnSave;
        
        _btnCancelBg = btnBg;
        _btnCancelFg = fg;
        _cancelBtn = MakeButton("Cancel", 280, 295, btnBg, fg);
        _cancelBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;

        // ── Compose ───────────────────────────────────────────────────────
        Controls.AddRange([
            _hotkeyGroup, _behaviorGroup,
            _startupCheck, _saveBtn, _cancelBtn
        ]);

        LoadCurrentSettings(current);
    }
    
    private readonly Color _btnCancelBg;
    private readonly Color _btnCancelFg;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int useDark = _isDarkTheme ? 1 : 0;
        DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_V2, ref useDark, sizeof(int));
    }

    private bool IsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int lightTheme)
            {
                return lightTheme == 0;
            }
        }
        catch { }
        return true; // Default to dark theme
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private GroupBox MakeGroup(string text, int top, int height, Color fg, Color bg) => new()
    {
        Text      = text,
        ForeColor = fg,
        Location  = new Point(12, top),
        Size      = new Size(348, height),
        BackColor = bg
    };

    private CheckBox MakeModCheck(string text, int x, int y, Color fg) => new()
    {
        Text      = text,
        ForeColor = fg,
        Location  = new Point(x, y),
        AutoSize  = true
    };

    private Button MakeButton(string text, int x, int y, Color bg, Color fg) => new()
    {
        Text        = text,
        Location    = new Point(x, y),
        Size        = new Size(80, 28),
        FlatStyle   = FlatStyle.Flat,
        BackColor   = bg,
        ForeColor   = fg,
        Cursor      = Cursors.Hand
    };

    private void LoadCurrentSettings(AppSettings s)
    {
        _ctrlCheck.Checked  = (s.ModifierKeys & 0x2) != 0; // MOD_CONTROL
        _altCheck.Checked   = (s.ModifierKeys & 0x1) != 0; // MOD_ALT
        _shiftCheck.Checked = (s.ModifierKeys & 0x4) != 0; // MOD_SHIFT
        _winCheck.Checked   = (s.ModifierKeys & 0x8) != 0; // MOD_WIN

        _capturedKey  = (Keys)s.HotKey;
        _hotkeyBox.Text = _capturedKey.ToString();
    }

    private void OnHotkeyBoxKeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;

        // Strip modifier-only presses
        Keys key = e.KeyCode;
        if (key == Keys.ControlKey || key == Keys.Menu ||
            key == Keys.ShiftKey   || key == Keys.LWin || key == Keys.RWin)
            return;

        _capturedKey    = key;
        _hotkeyBox.Text = key.ToString();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_capturedKey == Keys.None)
        {
            MessageBox.Show("Please select a key for the hotkey.", "PanicMode",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int mods = 0;
        if (_ctrlCheck.Checked)  mods |= 0x2;
        if (_altCheck.Checked)   mods |= 0x1;
        if (_shiftCheck.Checked) mods |= 0x4;
        if (_winCheck.Checked)   mods |= 0x8;

        ResultSettings = new AppSettings
        {
            ModifierKeys       = mods,
            HotKey             = (int)_capturedKey,
            RequiredPressCount = (int)_pressCountNum.Value,
            PressWindowMs      = (int)_pressWindowNum.Value,
            RunAtStartup       = _startupCheck.Checked,
            PanicUrl           = _panicUrlBox.Text?.Trim() ?? string.Empty,
            MaximizeWindow     = _maximizeCheck.Checked
        };

        SettingsManager.SetStartup(_startupCheck.Checked);
        DialogResult = DialogResult.OK;
    }
}
