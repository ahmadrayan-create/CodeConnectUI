using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CodeConnect.Logic;
using CodeConnect.Helpers; // GraphicsPathExtensions

namespace CodeConnect.Forms
{
    // Double-buffered panel for smooth rendering
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel() { this.DoubleBuffered = true; }
    }

    // Neon Glass Button with hover glow (unchanged)
    public class GlassButton : Button
    {
        private float _hoverAlpha = 0f;
        private System.Windows.Forms.Timer _animTimer;
        private bool _isHovering = false;
        public Color AccentColor { get; set; } = Color.White;

        public GlassButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) => {
                float target = _isHovering ? 1f : 0f;
                if (Math.Abs(_hoverAlpha - target) > 0.01f)
                {
                    _hoverAlpha += (target - _hoverAlpha) * 0.2f;
                    Invalidate();
                }
            };
            _animTimer.Start();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovering = true; }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovering = false; }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Color.Transparent);

            RectangleF rect = new RectangleF(0, 0, this.Width - 1, this.Height - 1);
            float radius = 10;
            using (GraphicsPath path = GetRoundedRect(rect, radius))
            {
                // Neon glass background
                using (LinearGradientBrush glass = new LinearGradientBrush(rect,
                    Color.FromArgb(48, AccentColor),
                    Color.FromArgb(12, 255, 255, 255), 45f))
                {
                    g.FillPath(glass, path);
                }

                // Glow effect
                if (_hoverAlpha > 0)
                {
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb((int)(_hoverAlpha * 120), AccentColor)))
                        g.FillPath(glow, path);
                }

                // Border
                using (Pen border = new Pen(Color.FromArgb(90 + (int)(_hoverAlpha * 80), AccentColor), 1.8f))
                    g.DrawPath(border, path);

                // Text
                using (Font f = new Font("Segoe UI", 11, FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(this.Text, f);
                    PointF pos = new PointF((this.Width - size.Width) / 2, (this.Height - size.Height) / 2);
                    g.DrawString(this.Text, f, Brushes.White, pos);
                    if (_hoverAlpha > 0)
                        using (SolidBrush accent = new SolidBrush(Color.FromArgb((int)(_hoverAlpha * 200), AccentColor)))
                            g.DrawString(this.Text, f, accent, pos);
                }
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class MainForm : Form
    {
        private const int GridWidth = 20;
        private const int GridHeight = 15;
        private int _currentLevel = 1;
        private int _activeFlowId = 1;

        private Color[] _flowColors = new Color[]
        {
            Color.Transparent,
            Color.Cyan,
            Color.Orange,
            Color.MediumSpringGreen,
            Color.Magenta,
            Color.Yellow,
            Color.Red,
            Color.DodgerBlue,
            Color.MediumPurple
        };

        // Fields initialized in InitializeComponent; suppress definite-assignment warnings
        private DoubleBufferedPanel _gamePanel = null!;
        private Label _lblStatus = null!;
        private Button _btnNext = null!;
        private Button _btnMenu = null!;
        private MainMenuView _menuView = null!;
        private TutorialView _tutorialView = null!;

        // New HUD controls
        private GlassButton _btnUndo = null!;
        private GlassButton _btnRedo = null!;
        private GlassButton _btnHint = null!;
        private GlassButton _btnSave = null!;
        private GlassButton _btnLoad = null!;
        private Label _lblScore = null!;
        private Label _lblHintToast = null!;

        private System.Windows.Forms.Timer _gameAnimTimer;
        private float _pulsePhase = 0f;

        public MainForm()
        {
            InitializeComponent();
            ShowMenu();
        }

        private void InitializeComponent()
        {
            this.Text = "Code Connect - Flow Edition";
            this.Size = new Size(1100, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(12, 8, 24);

            _gameAnimTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _gameAnimTimer.Tick += (s, e) => {
                _pulsePhase += 0.1f;
                if (_gamePanel != null && _gamePanel.Visible) _gamePanel.Invalidate();
            };
            _gameAnimTimer.Start();

            // Game panel
            _gamePanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.Transparent };
            _gamePanel.Paint += GamePanel_Paint;
            _gamePanel.MouseDown += GamePanel_MouseDown;
            _gamePanel.Resize += (s, e) => _gamePanel.Invalidate();
            this.Controls.Add(_gamePanel);

            // Menu and tutorial views
            _menuView = new MainMenuView();
            _menuView.StartClicked += (s, e) => StartGame();
            _menuView.TutorialClicked += (s, e) => ShowTutorial();
            _menuView.ExitClicked += (s, e) => Application.Exit();
            this.Controls.Add(_menuView);

            _tutorialView = new TutorialView { Visible = false };
            _tutorialView.CloseClicked += (s, e) => ShowMenu();
            this.Controls.Add(_tutorialView);

            // Top HUD: TableLayoutPanel with 3 columns (left: status, center: score/hint, right: controls)
            var topPanel = new TableLayoutPanel
            {
                Name = "topPanel",
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.Transparent,
                Padding = new Padding(12),
                ColumnCount = 3,
                RowCount = 1,
                Visible = false
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // status
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // center (score + hint)
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // controls

            // Status label (left)
            _lblStatus = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 8, 6, 8),
                BackColor = Color.Transparent
            };
            topPanel.Controls.Add(_lblStatus, 0, 0);

            // Center panel: score (top) and hint toast (below)
            var centerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            centerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lblScore = new Label
            {
                ForeColor = Color.Lime,
                // Replace this line (incorrect Font constructor and invalid FontStyle):
                // Font = new Font("Segoe UI", 12, FontStyle.SemiBold),

                // With this corrected line (use FontFamily constructor and FontStyle.Bold as closest available):
                Font = new Font(new FontFamily("Segoe UI"), 12, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = "Score: 0",
                Padding = new Padding(6, 6, 6, 6),
                BackColor = Color.Transparent
            };
            centerPanel.Controls.Add(_lblScore, 0, 0);

            _lblHintToast = new Label
            {
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 20, 20, 30),
                AutoSize = false,
                Height = 36,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(8)
            };
            // centerPanel will center the toast horizontally
            var toastWrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            toastWrapper.Controls.Add(_lblHintToast);
            _lblHintToast.Anchor = AnchorStyles.Top;
            _lblHintToast.Left = (toastWrapper.Width - _lblHintToast.Width) / 2;
            centerPanel.Controls.Add(toastWrapper, 0, 1);

            topPanel.Controls.Add(centerPanel, 1, 0);

            // Right-side controls: FlowLayoutPanel (left-to-right)
            var rightButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(6),
                Margin = new Padding(0)
            };

            // Buttons
            _btnUndo = new GlassButton { Text = "UNDO", Width = 86, Height = 40, AccentColor = Color.Orange, Margin = new Padding(6, 6, 6, 6) };
            _btnUndo.Click += (s, e) => { if (BackendConnector.UndoLastMove()) RefreshAfterAction(); };

            _btnRedo = new GlassButton { Text = "REDO", Width = 86, Height = 40, AccentColor = Color.Orange, Margin = new Padding(6, 6, 6, 6) };
            _btnRedo.Click += (s, e) => { if (BackendConnector.RedoLastMove()) RefreshAfterAction(); };

            _btnHint = new GlassButton { Text = "HINT", Width = 86, Height = 40, AccentColor = Color.Cyan, Margin = new Padding(6, 6, 6, 6) };
            _btnHint.Click += (s, e) => ShowHint();

            _btnSave = new GlassButton { Text = "SAVE", Width = 86, Height = 40, AccentColor = Color.MediumSpringGreen, Margin = new Padding(6, 6, 6, 6) };
            _btnSave.Click += (s, e) => SaveProgressToFile();

            _btnLoad = new GlassButton { Text = "LOAD", Width = 86, Height = 40, AccentColor = Color.MediumSpringGreen, Margin = new Padding(6, 6, 6, 6) };
            _btnLoad.Click += (s, e) => LoadProgressFromFile();

            _btnMenu = new GlassButton { Text = "MENU", Width = 100, Height = 40, AccentColor = Color.DodgerBlue, Margin = new Padding(6, 6, 6, 6) };
            _btnMenu.Click += (s, e) => ShowMenu();

            _btnNext = new GlassButton { Text = "NEXT LEVEL", Width = 120, Height = 40, AccentColor = Color.LimeGreen, Margin = new Padding(6, 6, 6, 6), Visible = false };
            _btnNext.Click += (s, e) => LoadLevel(++_currentLevel);

            // Add in natural left-to-right order
            rightButtons.Controls.Add(_btnUndo);
            rightButtons.Controls.Add(_btnRedo);
            rightButtons.Controls.Add(_btnHint);
            rightButtons.Controls.Add(_btnSave);
            rightButtons.Controls.Add(_btnLoad);
            rightButtons.Controls.Add(_btnMenu);
            rightButtons.Controls.Add(_btnNext);

            topPanel.Controls.Add(rightButtons, 2, 0);

            // Add topPanel to form
            this.Controls.Add(topPanel);

            // Improve HUD painting: stronger gradient for readability
            topPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = topPanel.ClientRectangle;
                using (LinearGradientBrush bg = new LinearGradientBrush(r, Color.FromArgb(40, 20, 20, 30), Color.FromArgb(18, 12, 24), 90f))
                {
                    g.FillRectangle(bg, r);
                }
                using (Pen border = new Pen(Color.FromArgb(120, 255, 255, 255), 1))
                    g.DrawLine(border, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
            };
        }

        private void RefreshAfterAction()
        {
            _gamePanel.Invalidate();
            UpdateScoreDisplay();
            if (BackendConnector.IsLevelComplete() == 1) _btnNext.Visible = true;
        }

        private void ShowHint()
        {
            string hint = BackendConnector.GetHint();
            _lblHintToast.Text = hint;
            _lblHintToast.Visible = true;
            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) => { _lblHintToast.Visible = false; t.Stop(); t.Dispose(); };
            t.Start();
        }

        private void SaveProgressToFile()
        {
            try
            {
                string json = BackendConnector.SaveProgress();
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CodeConnect Save|*.ccsave|JSON|*.json";
                    dlg.FileName = $"level{_currentLevel}_save.json";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dlg.FileName, json, Encoding.UTF8);
                        ShowTransientMessage("Progress saved.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}");
            }
        }

        private void LoadProgressFromFile()
        {
            try
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Filter = "CodeConnect Save|*.ccsave;*.json|All Files|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        string json = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                        BackendConnector.LoadProgress(json);
                        RefreshAfterAction();
                        ShowTransientMessage("Progress loaded.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load failed: {ex.Message}");
            }
        }

        private void ShowTransientMessage(string text)
        {
            _lblHintToast.Text = text;
            _lblHintToast.Visible = true;
            var t = new System.Windows.Forms.Timer { Interval = 1800 };
            t.Tick += (s, e) => { _lblHintToast.Visible = false; t.Stop(); t.Dispose(); };
            t.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Deep gradient base
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(12, 8, 24), Color.FromArgb(28, 12, 44), 45f))
            {
                g.FillRectangle(brush, rect);
            }

            // Soft neon vignette top-left
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(-120, -120, 420, 420);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(60, 80, 40, 200);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, path);
                }
            }

            // Soft neon vignette bottom-right
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(this.Width - 320, this.Height - 320, 520, 520);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(60, 200, 60, 140);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, path);
                }
            }

            // Subtle grid overlay for premium terminal feel
            int step = Math.Max(40, this.Width / 30);
            using (Pen gridPen = new Pen(Color.FromArgb(8, 255, 255, 255), 1))
            {
                for (int x = 0; x < this.Width; x += step)
                    g.DrawLine(gridPen, x, 0, x, this.Height);
                for (int y = 0; y < this.Height; y += step)
                    g.DrawLine(gridPen, 0, y, this.Width, y);
            }
        }

        private void ShowMenu() => SwitchView(_menuView);
        private void StartGame()
        {
            SwitchView(_gamePanel);
            var top = this.Controls["topPanel"];
            if (top != null) top.Visible = true;
            LoadLevel(1);
        }
        private void ShowTutorial() => SwitchView(_tutorialView);

        private void SwitchView(Control view)
        {
            _menuView.Visible = (view == _menuView);
            _gamePanel.Visible = (view == _gamePanel);
            _tutorialView.Visible = (view == _tutorialView);
            var top = this.Controls["topPanel"];
            if (top != null) top.Visible = (view == _gamePanel);
        }

        private void LoadLevel(int level)
        {
            if (level > 20) { MessageBox.Show("Master Coder!"); ShowMenu(); return; }
            _currentLevel = level;

            BackendConnector.StartLevel(level);

            _lblStatus.Text = $"LEVEL {level} - CONNECT THE LOGIC";
            _btnNext.Visible = false;
            _activeFlowId = 1;
            UpdateScoreDisplay();
            _gamePanel.Invalidate();
        }

        private void GamePanel_MouseDown(object sender, MouseEventArgs e)
        {
            float cellW = (float)_gamePanel.Width / GridWidth;
            float cellH = (float)_gamePanel.Height / GridHeight;

            int x = (int)(e.X / cellW);
            int y = (int)(e.Y / cellH);

            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            {
                int val = BackendConnector.GetCellType(x, y);
                int type = val & 0x0F;
                int flowId = val >> 4;

                if (type > 0 && type < 4) // It's a node
                {
                    _activeFlowId = flowId;
                }
                else
                {
                    BackendConnector.PlacePath(x, y, _activeFlowId);
                }

                if (BackendConnector.IsLevelComplete() == 1) _btnNext.Visible = true;
                UpdateScoreDisplay();
                _gamePanel.Invalidate();
            }
        }

        private void UpdateScoreDisplay()
        {
            try
            {
                int score = BackendConnector.GetScore();
                _lblScore.Text = $"Score: {score}";
                _lblScore.Refresh();
            }
            catch
            {
                _lblScore.Text = "Score: -";
            }
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cellW = (float)_gamePanel.Width / GridWidth;
            float cellH = (float)_gamePanel.Height / GridHeight;

            // Glass Grid Background
            RectangleF gridRect = new RectangleF(0, 0, _gamePanel.Width, _gamePanel.Height);
            using (SolidBrush gridGlass = new SolidBrush(Color.FromArgb(12, 255, 255, 255)))
                g.FillRectangle(gridGlass, gridRect);

            // Dotted Grid Points aligned to cell centers
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(36, 255, 255, 255)))
            {
                float dotSize = Math.Max(2f, Math.Min(cellW, cellH) * 0.06f);
                for (int gx = 0; gx < GridWidth; gx++)
                {
                    for (int gy = 0; gy < GridHeight; gy++)
                    {
                        float cx = gx * cellW + cellW / 2f;
                        float cy = gy * cellH + cellH / 2f;
                        g.FillEllipse(dotBrush, cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize);
                    }
                }
            }

            // Draw Pipes and Nodes (preserve original visuals)
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int val = BackendConnector.GetCellType(x, y);
                    int type = val & 0x0F;
                    int flowId = val >> 4;

                    if (flowId == 0 || flowId >= _flowColors.Length) continue;
                    Color color = _flowColors[flowId];
                    RectangleF cellRect = new RectangleF(x * cellW, y * cellH, cellW, cellH);

                    if (type == 4) // Path / Pipe
                    {
                        float pipeSize = Math.Min(cellW, cellH) * 0.38f;
                        float centerX = cellRect.X + cellW / 2;
                        float centerY = cellRect.Y + cellH / 2;

                        // Outer glow
                        using (SolidBrush glow = new SolidBrush(Color.FromArgb(80, color)))
                            g.FillEllipse(glow, centerX - pipeSize, centerY - pipeSize, pipeSize * 2, pipeSize * 2);

                        // Main pipe core
                        using (LinearGradientBrush core = new LinearGradientBrush(
                            new RectangleF(centerX - pipeSize * 0.9f, centerY - pipeSize * 0.9f, pipeSize * 1.8f, pipeSize * 1.8f),
                            Color.FromArgb(255, color.R, color.G, color.B),
                            Color.FromArgb(180, Math.Min(255, color.R + 60), Math.Min(255, color.G + 60), Math.Min(255, color.B + 60)),
                            45f))
                        {
                            g.FillEllipse(core, centerX - pipeSize * 0.9f, centerY - pipeSize * 0.9f, pipeSize * 1.8f, pipeSize * 1.8f);
                        }

                        // Inner highlight
                        using (SolidBrush highlight = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                            g.FillEllipse(highlight, centerX - pipeSize * 0.35f, centerY - pipeSize * 0.35f, pipeSize * 0.7f, pipeSize * 0.7f);
                    }
                    else if (type > 0 && type < 4) // Node
                    {
                        float nodeSize = Math.Min(cellW, cellH) * 0.75f;
                        RectangleF nodeRect = new RectangleF(
                            cellRect.X + (cellW - nodeSize) / 2,
                            cellRect.Y + (cellH - nodeSize) / 2,
                            nodeSize, nodeSize);

                        bool isActive = (flowId == _activeFlowId);

                        // Soft shadow
                        using (SolidBrush shadow = new SolidBrush(Color.FromArgb(48, 0, 0, 0)))
                            g.FillEllipse(shadow, nodeRect.X, nodeRect.Y + 6, nodeSize, nodeSize);

                        // Glass Orb Background
                        using (LinearGradientBrush orb = new LinearGradientBrush(nodeRect, Color.FromArgb(isActive ? 120 : 60, 255, 255, 255), Color.FromArgb(10, 255, 255, 255), 90f))
                            g.FillEllipse(orb, nodeRect);

                        // Animated Pulse Ring if active
                        if (isActive)
                        {
                            float pulseRadius = nodeSize + (float)(Math.Sin(_pulsePhase) * 6 + 6);
                            RectangleF pulseRect = new RectangleF(
                                cellRect.X + (cellW - pulseRadius) / 2,
                                cellRect.Y + (cellH - pulseRadius) / 2,
                                pulseRadius, pulseRadius);

                            using (Pen pulsePen = new Pen(Color.FromArgb(160, color), 2.0f))
                                g.DrawEllipse(pulsePen, pulseRect);
                        }

                        // Colored glowing core with radial gradient
                        float coreSize = nodeSize * 0.6f;
                        RectangleF coreRect = new RectangleF(
                            cellRect.X + (cellW - coreSize) / 2,
                            cellRect.Y + (cellH - coreSize) / 2,
                            coreSize, coreSize);

                        using (GraphicsPath corePath = new GraphicsPath())
                        {
                            corePath.AddEllipse(coreRect);
                            using (PathGradientBrush pgb = new PathGradientBrush(corePath))
                            {
                                pgb.CenterColor = Color.FromArgb(255, 255, 255);
                                pgb.SurroundColors = new[] { Color.FromArgb(255, color.R, color.G, color.B) };
                                g.FillPath(pgb, corePath);
                            }
                        }

                        // Inner glossy highlight
                        using (SolidBrush gloss = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                            g.FillEllipse(gloss, coreRect.X + coreSize * 0.05f, coreRect.Y + coreSize * 0.05f, coreSize * 0.35f, coreSize * 0.35f);

                        // Node text
                        StringBuilder sb = new StringBuilder(16);
                        BackendConnector.GetNodeText(x, y, sb);
                        string text = sb.ToString();

                        using (Font f = new Font("Segoe UI", Math.Max(8, (int)(nodeSize * 0.18)), FontStyle.Bold))
                        {
                            SizeF size = g.MeasureString(text, f);
                            PointF pos = new PointF(nodeRect.X + (nodeSize - size.Width) / 2, nodeRect.Y + (nodeSize - size.Height) / 2);

                            // Subtle text shadow
                            g.DrawString(text, f, new SolidBrush(Color.FromArgb(120, 0, 0, 0)), pos.X, pos.Y + 1);
                            g.DrawString(text, f, Brushes.White, pos);
                        }
                    }
                }
            }

            // HUD overlay: small legend and active flow indicator (keeps drawing but smaller)
            DrawHudOverlay(g);
        }

        private void DrawHudOverlay(Graphics g)
        {
            int padding = 12;
            int boxW = 260;
            int boxH = 72;
            RectangleF hudRect = new RectangleF(_gamePanel.Width - boxW - padding, padding, boxW, boxH);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRoundedRectangle(hudRect, 12);
                using (LinearGradientBrush bg = new LinearGradientBrush(hudRect, Color.FromArgb(40, 255, 255, 255), Color.FromArgb(12, 255, 255, 255), 90f))
                    g.FillPath(bg, path);

                using (Pen border = new Pen(Color.FromArgb(120, 255, 255, 255), 1))
                    g.DrawPath(border, path);
            }

            // Active flow color swatch (larger)
            Color activeColor = (_activeFlowId > 0 && _activeFlowId < _flowColors.Length) ? _flowColors[_activeFlowId] : Color.Gray;
            RectangleF swatch = new RectangleF(hudRect.X + 12, hudRect.Y + 12, 56, 56);
            using (SolidBrush sw = new SolidBrush(activeColor))
                g.FillEllipse(sw, swatch);

            using (Pen glow = new Pen(Color.FromArgb(160, activeColor), 3))
                g.DrawEllipse(glow, swatch.X - 2, swatch.Y - 2, swatch.Width + 4, swatch.Height + 4);

            // Text
            using (Font f = new Font("Segoe UI", 10, FontStyle.Bold))
            {
                g.DrawString("Active Flow", f, Brushes.White, hudRect.X + 80, hudRect.Y + 14);
                g.DrawString($"ID: {_activeFlowId}", f, Brushes.LightGray, hudRect.X + 80, hudRect.Y + 34);
            }

            // Score breakdown (small, below HUD)
            try
            {
                string details = BackendConnector.GetScoreDetails();
                using (Font f2 = new Font("Segoe UI", 9, FontStyle.Regular))
                {
                    g.DrawString(details, f2, new SolidBrush(Color.FromArgb(220, 220, 220, 220)), hudRect.X + 12, hudRect.Y + hudRect.Height + 6);
                }
            }
            catch { /* ignore if not available */ }
        }
    }
}
