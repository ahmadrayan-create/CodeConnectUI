using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using CodeConnect.Logic;

namespace CodeConnect.Forms
{
    // Double-buffered panel for smooth rendering
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel() { this.DoubleBuffered = true; }
    }

    // Neon Glass Button with hover glow
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
            float radius = 12;
            using (GraphicsPath path = GetRoundedRect(rect, radius))
            {
                // Neon glass background
                using (LinearGradientBrush glass = new LinearGradientBrush(rect,
                    Color.FromArgb(40, AccentColor),
                    Color.FromArgb(10, 255, 255, 255), 45f))
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
                using (Pen border = new Pen(Color.FromArgb(80 + (int)(_hoverAlpha * 100), AccentColor), 2))
                    g.DrawPath(border, path);

                // Text
                using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(this.Text, f);
                    PointF pos = new PointF((this.Width - size.Width) / 2, (this.Height - size.Height) / 2);
                    g.DrawString(this.Text, f, Brushes.White, pos);
                    if (_hoverAlpha > 0)
                        using (SolidBrush accent = new SolidBrush(Color.FromArgb((int)(_hoverAlpha * 255), AccentColor)))
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

            _gamePanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.Transparent };
            _gamePanel.Paint += GamePanel_Paint;
            _gamePanel.MouseDown += GamePanel_MouseDown;
            _gamePanel.Resize += (s, e) => _gamePanel.Invalidate();
            this.Controls.Add(_gamePanel);

            _menuView = new MainMenuView();
            _menuView.StartClicked += (s, e) => StartGame();
            _menuView.TutorialClicked += (s, e) => ShowTutorial();
            _menuView.ExitClicked += (s, e) => Application.Exit();
            this.Controls.Add(_menuView);

            _tutorialView = new TutorialView { Visible = false };
            _tutorialView.CloseClicked += (s, e) => ShowMenu();
            this.Controls.Add(_tutorialView);

            DoubleBufferedPanel topPanel = new DoubleBufferedPanel
            {
                Height = 80,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Name = "topPanel",
                Visible = false,
                Padding = new Padding(20, 15, 20, 15)
            };
            topPanel.Paint += (s, e) => {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(0, 0, topPanel.Width, topPanel.Height);
                using (LinearGradientBrush glass = new LinearGradientBrush(rect,
                    Color.FromArgb(30, 255, 255, 255),
                    Color.FromArgb(10, 255, 255, 255), 90f))
                {
                    g.FillRectangle(glass, rect);
                }
                using (Pen border = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                    g.DrawLine(border, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
            };

            _lblStatus = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(10, 10, 10, 10),
                BackColor = Color.Transparent
            };
            topPanel.Controls.Add(_lblStatus);

            Panel nextWrapper = new Panel { Dock = DockStyle.Right, Width = 170, BackColor = Color.Transparent, Padding = new Padding(10, 0, 0, 0) };
            _btnNext = new GlassButton
            {
                Text = "NEXT LEVEL",
                Dock = DockStyle.Fill,
                Visible = false,
                AccentColor = Color.LimeGreen
            };
            _btnNext.Click += (s, e) => LoadLevel(++_currentLevel);
            nextWrapper.Controls.Add(_btnNext);
            topPanel.Controls.Add(nextWrapper);

            Panel menuWrapper = new Panel { Dock = DockStyle.Right, Width = 130, BackColor = Color.Transparent, Padding = new Padding(10, 0, 0, 0) };
            _btnMenu = new GlassButton
            {
                Text = "MENU",
                Dock = DockStyle.Fill,
                AccentColor = Color.DodgerBlue
            };
            _btnMenu.Click += (s, e) => ShowMenu();
            menuWrapper.Controls.Add(_btnMenu);
            topPanel.Controls.Add(menuWrapper);

            this.Controls.Add(topPanel);
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
        private void StartGame() { SwitchView(_gamePanel); this.Controls["topPanel"].Visible = true; LoadLevel(1); }
        private void ShowTutorial() => SwitchView(_tutorialView);

        private void SwitchView(Control view)
        {
            _menuView.Visible = (view == _menuView);
            _gamePanel.Visible = (view == _gamePanel);
            _tutorialView.Visible = (view == _tutorialView);
            if (view != _gamePanel) this.Controls["topPanel"].Visible = false;
        }

        private void LoadLevel(int level)
        {
            if (level > 20) { MessageBox.Show("Master Coder!"); ShowMenu(); return; }
            _currentLevel = level;
            BackendConnector.GenerateLevel(level);
            _lblStatus.Text = $"LEVEL {level} - CONNECT THE LOGIC";
            _btnNext.Visible = false;
            _activeFlowId = 1;
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
                    BackendConnector.TogglePath(x, y, _activeFlowId);
                }

                if (BackendConnector.IsLevelComplete() == 1) _btnNext.Visible = true;
                _gamePanel.Invalidate();
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

            // Dotted Grid Points for premium look
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(36, 255, 255, 255)))
            {
                for (int x = 0; x <= GridWidth; x++)
                    for (int y = 0; y <= GridHeight; y++)
                        g.FillEllipse(dotBrush, x * cellW - 2, y * cellH - 2, 4, 4);
            }

            // Draw Pipes and Nodes
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

                            using (Pen pulsePen = new Pen(Color.FromArgb(160, color), 2.5f))
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

                        using (Font f = new Font("Segoe UI", 10, FontStyle.Bold))
                        {
                            SizeF size = g.MeasureString(text, f);
                            PointF pos = new PointF(nodeRect.X + (nodeSize - size.Width) / 2, nodeRect.Y + (nodeSize - size.Height) / 2);

                            // Text shadow
                            g.DrawString(text, f, new SolidBrush(Color.FromArgb(160, 0, 0, 0)), pos.X, pos.Y + 1);
                            g.DrawString(text, f, Brushes.White, pos);
                        }
                    }
                }
            }

            // HUD overlay: small legend and active flow indicator
            DrawHudOverlay(g);
        }

        private void DrawHudOverlay(Graphics g)
        {
            int padding = 12;
            int boxW = 220;
            int boxH = 64;
            RectangleF hudRect = new RectangleF(_gamePanel.Width - boxW - padding, padding, boxW, boxH);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRoundedRectangle(hudRect, 10);
                using (LinearGradientBrush bg = new LinearGradientBrush(hudRect, Color.FromArgb(30, 255, 255, 255), Color.FromArgb(8, 255, 255, 255), 90f))
                    g.FillPath(bg, path);

                using (Pen border = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                    g.DrawPath(border, path);
            }

            // Active flow color swatch
            Color activeColor = (_activeFlowId > 0 && _activeFlowId < _flowColors.Length) ? _flowColors[_activeFlowId] : Color.Gray;
            RectangleF swatch = new RectangleF(hudRect.X + 12, hudRect.Y + 12, 40, 40);
            using (SolidBrush sw = new SolidBrush(activeColor))
                g.FillEllipse(sw, swatch);

            using (Pen glow = new Pen(Color.FromArgb(120, activeColor), 3))
                g.DrawEllipse(glow, swatch.X - 2, swatch.Y - 2, swatch.Width + 4, swatch.Height + 4);

            // Text
            using (Font f = new Font("Segoe UI", 10, FontStyle.Bold))
            {
                g.DrawString("Active Flow", f, Brushes.White, hudRect.X + 64, hudRect.Y + 14);
                g.DrawString($"ID: {_activeFlowId}", f, Brushes.LightGray, hudRect.X + 64, hudRect.Y + 34);
            }
        }
    }

    // Extension helper for rounded rectangle path (keeps presentation code tidy)
    internal static class GraphicsPathExtensions
    {
        public static void AddRoundedRectangle(this GraphicsPath path, RectangleF rect, float radius)
        {
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }
    }
}
