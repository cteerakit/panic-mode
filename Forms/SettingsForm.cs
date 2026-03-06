using PanicMode.Models;

namespace PanicMode.Forms;

/// <summary>
/// Settings dialog allowing the user to configure hotkey, press count, and startup behaviour.
/// </summary>
public sealed class SettingsForm : Form
{
    // ── UI controls ────────────────────────────────────────────────────────
    private readonly Panel         _headerPanel;
    private readonly Label         _titleLabel;
    private readonly Label         _subtitleLabel;

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

    private readonly CheckBox      _startupCheck;

    private readonly Button        _saveBtn;
    private readonly Button        _cancelBtn;

    // ── State ─────────────────────────────────────────────────────────────
    private Keys _capturedKey = Keys.None;
    private readonly AppSettings _original;
    public AppSettings? ResultSettings { get; private set; }

    public SettingsForm(AppSettings current)
    {
        _original = current;

        // ── Form setup ────────────────────────────────────────────────────
        Text             = "PanicMode Settings";
        FormBorderStyle  = FormBorderStyle.FixedDialog;
        MaximizeBox      = false;
        MinimizeBox      = false;
        StartPosition    = FormStartPosition.CenterScreen;
        Size             = new Size(380, 430);
        BackColor        = Color.FromArgb(30, 30, 30);
        ForeColor        = Color.WhiteSmoke;
        Font             = new Font("Segoe UI", 9f);

        // ── Header ────────────────────────────────────────────────────────
        _headerPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 70,
            BackColor = Color.FromArgb(20, 20, 20)
        };

        _titleLabel = new Label
        {
            Text      = "⚡ PanicMode",
            ForeColor = Color.FromArgb(100, 210, 255),
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location  = new Point(14, 10),
            AutoSize  = true
        };

        _subtitleLabel = new Label
        {
            Text      = "Configure your global panic hotkey",
            ForeColor = Color.FromArgb(150, 150, 160),
            Font      = new Font("Segoe UI", 8.5f),
            Location  = new Point(16, 42),
            AutoSize  = true
        };

        _headerPanel.Controls.AddRange([_titleLabel, _subtitleLabel]);

        // ── Hotkey group ─────────────────────────────────────────────────
        _hotkeyGroup = MakeGroup("Hotkey", 90, 95);

        _ctrlCheck  = MakeModCheck("Ctrl",  10, 25);
        _altCheck   = MakeModCheck("Alt",   70, 25);
        _shiftCheck = MakeModCheck("Shift", 130, 25);
        _winCheck   = MakeModCheck("Win",   200, 25);

        _hotkeyLabel = new Label
        {
            Text      = "Key:",
            ForeColor = Color.FromArgb(170, 170, 180),
            Location  = new Point(10, 57),
            AutoSize  = true
        };

        _hotkeyBox = new TextBox
        {
            Location    = new Point(42, 53),
            Size        = new Size(90, 23),
            ReadOnly    = true,
            BackColor   = Color.FromArgb(45, 45, 48),
            ForeColor   = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor      = Cursors.Arrow,
            TabStop     = false
        };
        _hotkeyBox.KeyDown += OnHotkeyBoxKeyDown;
        _hotkeyBox.Click   += (_, _) => { _hotkeyBox.Text = "Press a key…"; _capturedKey = Keys.None; };

        _hotkeyGroup.Controls.AddRange([_ctrlCheck, _altCheck, _shiftCheck, _winCheck,
                                        _hotkeyLabel, _hotkeyBox]);

        // ── Behavior group ────────────────────────────────────────────────
        _behaviorGroup = MakeGroup("Behavior", 195, 125);

        _pressCountLabel = new Label
        {
            Text     = "Required press count:",
            ForeColor = Color.FromArgb(170, 170, 180),
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
            BackColor  = Color.FromArgb(45, 45, 48),
            ForeColor  = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle
        };

        _pressWindowLabel = new Label
        {
            Text      = "Press window (ms):",
            ForeColor = Color.FromArgb(170, 170, 180),
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
            BackColor  = Color.FromArgb(45, 45, 48),
            ForeColor  = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle
        };

        _panicUrlLabel = new Label
        {
            Text      = "Open URL on Panic:",
            ForeColor = Color.FromArgb(170, 170, 180),
            Location  = new Point(10, 91),
            AutoSize  = true
        };

        _panicUrlBox = new TextBox
        {
            Location    = new Point(135, 88),
            Size        = new Size(200, 23),
            BackColor   = Color.FromArgb(45, 45, 48),
            ForeColor   = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Text        = current.PanicUrl
        };

        _behaviorGroup.Controls.AddRange([_pressCountLabel, _pressCountNum,
                                          _pressWindowLabel, _pressWindowNum,
                                          _panicUrlLabel, _panicUrlBox]);

        // ── Startup checkbox ─────────────────────────────────────────────
        _startupCheck = new CheckBox
        {
            Text      = "Start with Windows",
            ForeColor = Color.FromArgb(200, 200, 210),
            Location  = new Point(20, 340),
            AutoSize  = true,
            Checked   = SettingsManager.IsStartupEnabled()
        };

        // ── Buttons ───────────────────────────────────────────────────────
        _saveBtn = MakeButton("Save", 195, 335);
        _saveBtn.BackColor = Color.FromArgb(0, 122, 204);
        _saveBtn.ForeColor = Color.White;
        _saveBtn.Click += OnSave;

        _cancelBtn = MakeButton("Cancel", 280, 335);
        _cancelBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;

        // ── Compose ───────────────────────────────────────────────────────
        Controls.AddRange([
            _headerPanel, _hotkeyGroup, _behaviorGroup,
            _startupCheck, _saveBtn, _cancelBtn
        ]);

        LoadCurrentSettings(current);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static GroupBox MakeGroup(string text, int top, int height) => new()
    {
        Text      = text,
        ForeColor = Color.FromArgb(100, 210, 255),
        Location  = new Point(12, top),
        Size      = new Size(348, height),
        BackColor = Color.FromArgb(30, 30, 30)
    };

    private static CheckBox MakeModCheck(string text, int x, int y) => new()
    {
        Text      = text,
        ForeColor = Color.FromArgb(200, 200, 210),
        Location  = new Point(x, y),
        AutoSize  = true
    };

    private static Button MakeButton(string text, int x, int y) => new()
    {
        Text        = text,
        Location    = new Point(x, y),
        Size        = new Size(80, 28),
        FlatStyle   = FlatStyle.Flat,
        BackColor   = Color.FromArgb(45, 45, 48),
        ForeColor   = Color.WhiteSmoke,
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
            PanicUrl           = _panicUrlBox.Text?.Trim() ?? string.Empty
        };

        SettingsManager.SetStartup(_startupCheck.Checked);
        DialogResult = DialogResult.OK;
    }
}
