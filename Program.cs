using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PiercerCrosshairOverlay
{
    public class CrosshairConfig
    {
        public string Style = "Cross"; // Cross, Dot, Circle, CrossCircle, TShape, XCross
        public bool CenterDot = true;
        public int DotSize = 3;
        public int Length = 10;
        public int Thickness = 2;
        public int Gap = 4;
        public bool Outline = true;
        public int OutlineThickness = 1;
        public Color MainColor = Color.FromArgb(0, 255, 128);
        public double Opacity = 1.0;
        public int OffsetX = 0;
        public int OffsetY = 0;
        public int MonitorIndex = 0;
        public bool OverlayEnabled = true;

        public CrosshairConfig Clone()
        {
            CrosshairConfig c = new CrosshairConfig();
            c.Style = Style;
            c.CenterDot = CenterDot;
            c.DotSize = DotSize;
            c.Length = Length;
            c.Thickness = Thickness;
            c.Gap = Gap;
            c.Outline = Outline;
            c.OutlineThickness = OutlineThickness;
            c.MainColor = MainColor;
            c.Opacity = Opacity;
            c.OffsetX = OffsetX;
            c.OffsetY = OffsetY;
            c.MonitorIndex = MonitorIndex;
            c.OverlayEnabled = OverlayEnabled;
            return c;
        }
    }

    public static class CrosshairRenderer
    {
        public static void Draw(Graphics g, int cx, int cy, CrosshairConfig cfg)
        {
            g.SmoothingMode = SmoothingMode.None;
            if (cfg.Style == "Circle" || cfg.Style == "CrossCircle" || cfg.Style == "XCross")
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            if (cfg.Outline && cfg.OutlineThickness > 0)
            {
                DrawGeometry(g, cx, cy, cfg, Color.Black, cfg.OutlineThickness * 2);
            }
            DrawGeometry(g, cx, cy, cfg, cfg.MainColor, 0);
        }

        private static void DrawGeometry(Graphics g, int cx, int cy, CrosshairConfig cfg, Color col, int extraThick)
        {
            int t = Math.Max(1, cfg.Thickness + extraThick);
            int len = cfg.Length;
            int gap = cfg.Gap;

            using (Pen p = new Pen(col, t))
            using (Brush b = new SolidBrush(col))
            {
                if (cfg.Style == "Cross" || cfg.Style == "CrossCircle" || cfg.Style == "TShape")
                {
                    // Left
                    g.DrawLine(p, cx - gap - len, cy, cx - gap, cy);
                    // Right
                    g.DrawLine(p, cx + gap, cy, cx + gap + len, cy);
                    // Bottom
                    g.DrawLine(p, cx, cy + gap, cx, cy + gap + len);
                    // Top (omit for TShape)
                    if (cfg.Style != "TShape")
                    {
                        g.DrawLine(p, cx, cy - gap - len, cx, cy - gap);
                    }
                }

                if (cfg.Style == "XCross")
                {
                    int d1 = Math.Max(1, (int)(gap * 0.75));
                    int d2 = d1 + Math.Max(2, (int)(len * 0.8));
                    g.DrawLine(p, cx - d2, cy - d2, cx - d1, cy - d1);
                    g.DrawLine(p, cx + d1, cy - d1, cx + d2, cy - d2);
                    g.DrawLine(p, cx - d2, cy + d2, cx - d1, cy + d1);
                    g.DrawLine(p, cx + d1, cy + d1, cx + d2, cy + d2);
                }

                if (cfg.Style == "Circle" || cfg.Style == "CrossCircle")
                {
                    int r = Math.Max(4, gap + len / 2);
                    g.DrawEllipse(p, cx - r, cy - r, r * 2, r * 2);
                }

                if (cfg.CenterDot || cfg.Style == "Dot")
                {
                    int ds = Math.Max(1, cfg.DotSize + extraThick);
                    g.FillRectangle(b, cx - ds / 2, cy - ds / 2, ds, ds);
                }
            }
        }
    }

    public class OverlayForm : Form
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private CrosshairConfig _cfg;
        private static readonly Color ChromaKey = Color.FromArgb(1, 0, 1);

        public OverlayForm(CrosshairConfig cfg)
        {
            _cfg = cfg;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = ChromaKey;
            TransparencyKey = ChromaKey;
            DoubleBuffered = true;
            Width = 320;
            Height = 320;
            StartPosition = FormStartPosition.Manual;
            Reposition();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int ex = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, ex | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW);
        }

        public void UpdateConfig(CrosshairConfig cfg)
        {
            _cfg = cfg;
            Opacity = Math.Max(0.1, Math.Min(1.0, cfg.Opacity));
            Reposition();
            if (cfg.OverlayEnabled)
            {
                if (!Visible) Show();
                Invalidate();
            }
            else
            {
                if (Visible) Hide();
            }
        }

        public void Reposition()
        {
            Screen[] screens = Screen.AllScreens;
            int idx = _cfg.MonitorIndex;
            if (idx < 0 || idx >= screens.Length) idx = 0;
            Rectangle bounds = screens[idx].Bounds;
            int x = bounds.Left + (bounds.Width / 2) - (Width / 2) + _cfg.OffsetX;
            int y = bounds.Top + (bounds.Height / 2) - (Height / 2) + _cfg.OffsetY;
            Location = new Point(x, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(ChromaKey);
            if (_cfg != null && _cfg.OverlayEnabled)
            {
                CrosshairRenderer.Draw(e.Graphics, Width / 2, Height / 2, _cfg);
            }
        }
    }

    public class EditorForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_TOGGLE_OVERLAY = 1001;
        private const int HOTKEY_TOGGLE_EDITOR = 1002;
        private const uint VK_F8 = 0x77;
        private const uint VK_F9 = 0x78;

        private readonly Color BgDark = Color.FromArgb(10, 10, 12);
        private readonly Color PanelDark = Color.FromArgb(20, 20, 24);
        private readonly Color CardDark = Color.FromArgb(30, 30, 36);
        private readonly Color BorderDark = Color.FromArgb(42, 42, 50);
        private readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        private readonly Color TextLight = Color.FromArgb(244, 244, 245);
        private readonly Color MutedText = Color.FromArgb(161, 161, 170);

        private CrosshairConfig _cfg = new CrosshairConfig();
        private OverlayForm _overlay;
        private NotifyIcon _tray;
        private Panel _previewBox;

        private ComboBox _cmbStyle;
        private ComboBox _cmbMonitor;
        private CheckBox _chkEnabled;
        private CheckBox _chkDot;
        private CheckBox _chkOutline;
        private NumericUpDown _numDot;
        private NumericUpDown _numLen;
        private NumericUpDown _numThick;
        private NumericUpDown _numGap;
        private NumericUpDown _numOutline;
        private NumericUpDown _numOffX;
        private NumericUpDown _numOffY;
        private TrackBar _trkOpacity;
        private Button _btnColor;
        private Label _lblStatus;
        private bool _loadingUi = false;

        private string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crosshair_config.ini"); }
        }

        public EditorForm()
        {
            Text = "piercer — Crosshair Overlay Studio (Crosshair X Style)";
            Size = new Size(760, 560);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextLight;
            Font = new Font("Segoe UI", 9.5f);

            LoadConfig();
            BuildUI();
            SetupTray();

            _overlay = new OverlayForm(_cfg);
            _overlay.UpdateConfig(_cfg);

            RegisterHotKey(Handle, HOTKEY_TOGGLE_OVERLAY, 0, VK_F8);
            RegisterHotKey(Handle, HOTKEY_TOGGLE_EDITOR, 0, VK_F9);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_TOGGLE_OVERLAY)
                {
                    _cfg.OverlayEnabled = !_cfg.OverlayEnabled;
                    _loadingUi = true;
                    _chkEnabled.Checked = _cfg.OverlayEnabled;
                    _loadingUi = false;
                    ApplyChanges();
                }
                else if (id == HOTKEY_TOGGLE_EDITOR)
                {
                    if (Visible) Hide();
                    else { Show(); Activate(); }
                }
            }
            base.WndProc(ref m);
        }

        private void SetupTray()
        {
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("Abrir Editor (F9)", (s, e) => { Show(); Activate(); });
            menu.MenuItems.Add("Ligar/Desligar Mira (F8)", (s, e) =>
            {
                _cfg.OverlayEnabled = !_cfg.OverlayEnabled;
                _loadingUi = true;
                _chkEnabled.Checked = _cfg.OverlayEnabled;
                _loadingUi = false;
                ApplyChanges();
            });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("Sair", (s, e) => { _tray.Visible = false; Application.Exit(); });

            _tray = new NotifyIcon();
            _tray.Text = "piercer Crosshair Overlay";
            _tray.Icon = SystemIcons.Application;
            _tray.ContextMenu = menu;
            _tray.Visible = true;
            _tray.DoubleClick += (s, e) => { Show(); Activate(); };
        }

        private void BuildUI()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = PanelDark, Padding = new Padding(16, 10, 16, 10) };
            Label title = new Label
            {
                Text = "CROSSHAIR OVERLAY STUDIO  |  by piercer",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize = true,
                Location = new Point(16, 14)
            };
            _chkEnabled = new CheckBox
            {
                Text = "Mira Ativa (F8)",
                Checked = _cfg.OverlayEnabled,
                AutoSize = true,
                ForeColor = AccentRed,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(580, 16)
            };
            _chkEnabled.CheckedChanged += (s, e) => { if (!_loadingUi) { _cfg.OverlayEnabled = _chkEnabled.Checked; ApplyChanges(); } };
            header.Controls.Add(title);
            header.Controls.Add(_chkEnabled);
            Controls.Add(header);

            // Left: Preview + Presets
            Panel left = new Panel { Location = new Point(16, 68), Size = new Size(300, 440), BackColor = PanelDark };
            _previewBox = new Panel { Location = new Point(14, 14), Size = new Size(272, 220), BackColor = Color.FromArgb(24, 28, 34), BorderStyle = BorderStyle.FixedSingle };
            _previewBox.Paint += (s, e) =>
            {
                // Subtle FPS horizon grid
                using (Pen grid = new Pen(Color.FromArgb(36, 42, 50), 1))
                {
                    e.Graphics.DrawLine(grid, 0, _previewBox.Height / 2, _previewBox.Width, _previewBox.Height / 2);
                    e.Graphics.DrawLine(grid, _previewBox.Width / 2, 0, _previewBox.Width / 2, _previewBox.Height);
                }
                CrosshairRenderer.Draw(e.Graphics, _previewBox.Width / 2, _previewBox.Height / 2, _cfg);
            };
            left.Controls.Add(_previewBox);

            Label lblPresets = new Label { Text = "Presets Rápidos", ForeColor = MutedText, Location = new Point(14, 246), AutoSize = true };
            left.Controls.Add(lblPresets);

            AddPresetBtn(left, "CS2 Clássica", 14, 272, () => SetPreset("Cross", false, 2, 9, 2, 4, true, 1, Color.FromArgb(0, 255, 128)));
            AddPresetBtn(left, "Valorant Dot", 154, 272, () => SetPreset("Dot", true, 4, 0, 2, 0, true, 1, Color.FromArgb(0, 255, 255)));
            AddPresetBtn(left, "T-Shape Pro", 14, 310, () => SetPreset("TShape", false, 2, 8, 2, 3, true, 1, Color.FromArgb(255, 255, 0)));
            AddPresetBtn(left, "Apex / Circle", 154, 310, () => SetPreset("CrossCircle", true, 3, 7, 2, 6, true, 1, Color.FromArgb(239, 68, 68)));
            AddPresetBtn(left, "X-Cross", 14, 348, () => SetPreset("XCross", true, 2, 10, 2, 4, true, 1, Color.FromArgb(255, 255, 255)));
            AddPresetBtn(left, "Minimizar Tray (F9)", 154, 348, () => Hide());

            _lblStatus = new Label
            {
                Text = "Atalhos globais: F8 (Ligar/Desligar) | F9 (Editor)\nClick-through ativo: não atrapalha o jogo.",
                ForeColor = MutedText,
                Location = new Point(14, 392),
                Size = new Size(272, 40),
                Font = new Font("Segoe UI", 8.2f)
            };
            left.Controls.Add(_lblStatus);
            Controls.Add(left);

            // Right: Controls
            Panel right = new Panel { Location = new Point(330, 68), Size = new Size(402, 440), BackColor = PanelDark };
            int y = 16;

            _cmbStyle = AddComboRow(right, "Estilo da Mira:", y, new string[] { "Cross", "Dot", "Circle", "CrossCircle", "TShape", "XCross" });
            _cmbStyle.SelectedItem = _cfg.Style;
            _cmbStyle.SelectedIndexChanged += (s, e) => { if (!_loadingUi) { _cfg.Style = _cmbStyle.SelectedItem.ToString(); ApplyChanges(); } };
            y += 34;

            _numLen = AddNumRow(right, "Comprimento (Length):", y, 0, 50, _cfg.Length, v => { _cfg.Length = v; ApplyChanges(); }); y += 32;
            _numThick = AddNumRow(right, "Espessura (Thickness):", y, 1, 20, _cfg.Thickness, v => { _cfg.Thickness = v; ApplyChanges(); }); y += 32;
            _numGap = AddNumRow(right, "Abertura Central (Gap):", y, 0, 40, _cfg.Gap, v => { _cfg.Gap = v; ApplyChanges(); }); y += 32;

            _chkDot = new CheckBox { Text = "Ponto Central", Checked = _cfg.CenterDot, Location = new Point(16, y), AutoSize = true };
            _chkDot.CheckedChanged += (s, e) => { if (!_loadingUi) { _cfg.CenterDot = _chkDot.Checked; ApplyChanges(); } };
            _numDot = new NumericUpDown { Minimum = 1, Maximum = 20, Value = _cfg.DotSize, Location = new Point(260, y - 2), Width = 120, BackColor = CardDark, ForeColor = TextLight };
            _numDot.ValueChanged += (s, e) => { if (!_loadingUi) { _cfg.DotSize = (int)_numDot.Value; ApplyChanges(); } };
            right.Controls.Add(_chkDot);
            right.Controls.Add(_numDot);
            y += 34;

            _chkOutline = new CheckBox { Text = "Contorno Preto (Outline)", Checked = _cfg.Outline, Location = new Point(16, y), AutoSize = true };
            _chkOutline.CheckedChanged += (s, e) => { if (!_loadingUi) { _cfg.Outline = _chkOutline.Checked; ApplyChanges(); } };
            _numOutline = new NumericUpDown { Minimum = 1, Maximum = 5, Value = _cfg.OutlineThickness, Location = new Point(260, y - 2), Width = 120, BackColor = CardDark, ForeColor = TextLight };
            _numOutline.ValueChanged += (s, e) => { if (!_loadingUi) { _cfg.OutlineThickness = (int)_numOutline.Value; ApplyChanges(); } };
            right.Controls.Add(_chkOutline);
            right.Controls.Add(_numOutline);
            y += 36;

            Label lblCol = new Label { Text = "Cor Principal:", Location = new Point(16, y + 4), AutoSize = true };
            _btnColor = new Button { Text = "Escolher Cor", BackColor = _cfg.MainColor, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Location = new Point(260, y), Size = new Size(120, 26) };
            _btnColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog { Color = _cfg.MainColor })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        _cfg.MainColor = cd.Color;
                        _btnColor.BackColor = cd.Color;
                        ApplyChanges();
                    }
                }
            };
            right.Controls.Add(lblCol);
            right.Controls.Add(_btnColor);
            y += 36;

            Label lblOp = new Label { Text = "Opacidade:", Location = new Point(16, y + 4), AutoSize = true };
            _trkOpacity = new TrackBar { Minimum = 10, Maximum = 100, Value = (int)(_cfg.Opacity * 100), TickStyle = TickStyle.None, Location = new Point(256, y), Size = new Size(128, 28) };
            _trkOpacity.ValueChanged += (s, e) => { if (!_loadingUi) { _cfg.Opacity = _trkOpacity.Value / 100.0; ApplyChanges(); } };
            right.Controls.Add(lblOp);
            right.Controls.Add(_trkOpacity);
            y += 36;

            _numOffX = AddNumRow(right, "Ajuste Fino X (px):", y, -200, 200, _cfg.OffsetX, v => { _cfg.OffsetX = v; ApplyChanges(); }); y += 32;
            _numOffY = AddNumRow(right, "Ajuste Fino Y (px):", y, -200, 200, _cfg.OffsetY, v => { _cfg.OffsetY = v; ApplyChanges(); }); y += 34;

            List<string> monNames = new List<string>();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen sc = Screen.AllScreens[i];
                monNames.Add(string.Format("Monitor {0} ({1}x{2})", i + 1, sc.Bounds.Width, sc.Bounds.Height));
            }
            _cmbMonitor = AddComboRow(right, "Monitor Alvo:", y, monNames.ToArray());
            if (_cfg.MonitorIndex >= 0 && _cfg.MonitorIndex < _cmbMonitor.Items.Count)
                _cmbMonitor.SelectedIndex = _cfg.MonitorIndex;
            _cmbMonitor.SelectedIndexChanged += (s, e) => { if (!_loadingUi) { _cfg.MonitorIndex = _cmbMonitor.SelectedIndex; ApplyChanges(); } };

            Controls.Add(right);
        }

        private void AddPresetBtn(Panel parent, string text, int x, int y, Action onClick)
        {
            Button b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(132, 30),
                BackColor = CardDark,
                ForeColor = TextLight,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderColor = BorderDark;
            b.Click += (s, e) => onClick();
            parent.Controls.Add(b);
        }

        private ComboBox AddComboRow(Panel parent, string label, int y, string[] items)
        {
            Label l = new Label { Text = label, Location = new Point(16, y + 4), AutoSize = true };
            ComboBox c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(260, y), Width = 120, BackColor = CardDark, ForeColor = TextLight };
            c.Items.AddRange(items);
            parent.Controls.Add(l);
            parent.Controls.Add(c);
            return c;
        }

        private NumericUpDown AddNumRow(Panel parent, string label, int y, int min, int max, int val, Action<int> onChange)
        {
            Label l = new Label { Text = label, Location = new Point(16, y + 3), AutoSize = true };
            NumericUpDown n = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, val)),
                Location = new Point(260, y),
                Width = 120,
                BackColor = CardDark,
                ForeColor = TextLight
            };
            n.ValueChanged += (s, e) => { if (!_loadingUi) onChange((int)n.Value); };
            parent.Controls.Add(l);
            parent.Controls.Add(n);
            return n;
        }

        private void SetPreset(string style, bool dot, int dotSize, int len, int thick, int gap, bool outline, int outThick, Color col)
        {
            _cfg.Style = style;
            _cfg.CenterDot = dot;
            _cfg.DotSize = dotSize;
            _cfg.Length = len;
            _cfg.Thickness = thick;
            _cfg.Gap = gap;
            _cfg.Outline = outline;
            _cfg.OutlineThickness = outThick;
            _cfg.MainColor = col;
            SyncUiFromConfig();
            ApplyChanges();
        }

        private void SyncUiFromConfig()
        {
            _loadingUi = true;
            _cmbStyle.SelectedItem = _cfg.Style;
            _chkDot.Checked = _cfg.CenterDot;
            _numDot.Value = _cfg.DotSize;
            _numLen.Value = _cfg.Length;
            _numThick.Value = _cfg.Thickness;
            _numGap.Value = _cfg.Gap;
            _chkOutline.Checked = _cfg.Outline;
            _numOutline.Value = _cfg.OutlineThickness;
            _btnColor.BackColor = _cfg.MainColor;
            _trkOpacity.Value = (int)(_cfg.Opacity * 100);
            _numOffX.Value = _cfg.OffsetX;
            _numOffY.Value = _cfg.OffsetY;
            _loadingUi = false;
        }

        private void ApplyChanges()
        {
            _previewBox.Invalidate();
            if (_overlay != null) _overlay.UpdateConfig(_cfg);
            SaveConfig();
        }

        private void SaveConfig()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Style=" + _cfg.Style);
                sb.AppendLine("CenterDot=" + _cfg.CenterDot);
                sb.AppendLine("DotSize=" + _cfg.DotSize);
                sb.AppendLine("Length=" + _cfg.Length);
                sb.AppendLine("Thickness=" + _cfg.Thickness);
                sb.AppendLine("Gap=" + _cfg.Gap);
                sb.AppendLine("Outline=" + _cfg.Outline);
                sb.AppendLine("OutlineThickness=" + _cfg.OutlineThickness);
                sb.AppendLine("ColorArgb=" + _cfg.MainColor.ToArgb());
                sb.AppendLine("Opacity=" + _cfg.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.AppendLine("OffsetX=" + _cfg.OffsetX);
                sb.AppendLine("OffsetY=" + _cfg.OffsetY);
                sb.AppendLine("MonitorIndex=" + _cfg.MonitorIndex);
                File.WriteAllText(ConfigPath, sb.ToString());
            }
            catch { }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    string k = parts[0].Trim();
                    string v = parts[1].Trim();
                    if (k == "Style") _cfg.Style = v;
                    else if (k == "CenterDot") _cfg.CenterDot = bool.Parse(v);
                    else if (k == "DotSize") _cfg.DotSize = int.Parse(v);
                    else if (k == "Length") _cfg.Length = int.Parse(v);
                    else if (k == "Thickness") _cfg.Thickness = int.Parse(v);
                    else if (k == "Gap") _cfg.Gap = int.Parse(v);
                    else if (k == "Outline") _cfg.Outline = bool.Parse(v);
                    else if (k == "OutlineThickness") _cfg.OutlineThickness = int.Parse(v);
                    else if (k == "ColorArgb") _cfg.MainColor = Color.FromArgb(int.Parse(v));
                    else if (k == "Opacity") _cfg.Opacity = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
                    else if (k == "OffsetX") _cfg.OffsetX = int.Parse(v);
                    else if (k == "OffsetY") _cfg.OffsetY = int.Parse(v);
                    else if (k == "MonitorIndex") _cfg.MonitorIndex = int.Parse(v);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            UnregisterHotKey(Handle, HOTKEY_TOGGLE_OVERLAY);
            UnregisterHotKey(Handle, HOTKEY_TOGGLE_EDITOR);
            if (_tray != null) _tray.Visible = false;
            base.OnFormClosing(e);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new EditorForm());
        }
    }
}
