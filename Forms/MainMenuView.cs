using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CodeConnect.Forms
{
    public class MainMenuView : UserControl
    {
        public event EventHandler StartClicked;
        public event EventHandler TutorialClicked;
        public event EventHandler ExitClicked;

        // Keep original fields and semantics intact
        private RectangleF _btnStartRect;
        private RectangleF _btnTutorialRect;
        private RectangleF _btnExitRect;

        private int _hoveredButton = -1; // 0=Start, 1=Tutorial, 2=Exit
        private float[] _hoverAlphas = new float[3] { 0f, 0f, 0f };
        private System.Windows.Forms.Timer _animationTimer;

        // Additional presentation-only state (does not affect logic)
        private float _backgroundPulse = 0f;
        private float _titleGlow = 0f;
        private readonly Color _neonCyan = Color.FromArgb(100, 200, 255);
        private readonly Color _neonMint = Color.FromArgb(100, 255, 150);
        private readonly Color _neonRose = Color.FromArgb(255, 100, 140);

        public MainMenuView()
        {
            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;

            _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            this.MouseMove += MainMenuView_MouseMove;
            this.MouseClick += MainMenuView_MouseClick;
            this.MouseLeave += (s, e) => { _hoveredButton = -1; Invalidate(); };

            // Make fonts render smoothly across DPI
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            bool needsRedraw = false;

            // Hover alpha easing (preserve original behavior)
            for (int i = 0; i < 3; i++)
            {
                float target = (_hoveredButton == i) ? 1f : 0f;
                if (Math.Abs(_hoverAlphas[i] - target) > 0.01f)
                {
                    _hoverAlphas[i] += (target - _hoverAlphas[i]) * 0.2f;
                    needsRedraw = true;
                }
            }

            // Background pulse and title glow (presentation only)
            float bgTarget = 1f;
            _backgroundPulse += 0.02f;
            _titleGlow = (float)((Math.Sin(Environment.TickCount * 0.002) + 1.0) * 0.5f);

            if (needsRedraw) Invalidate();
            else
            {
                // Still occasionally repaint to keep subtle animations alive
                if ((int)(_backgroundPulse * 100) % 6 == 0) Invalidate();
            }
        }

        private void MainMenuView_MouseMove(object sender, MouseEventArgs e)
        {
            int oldHover = _hoveredButton;
            _hoveredButton = -1;

            // Use Contains with PointF compatibility
            if (_btnStartRect.Contains(e.Location)) _hoveredButton = 0;
            else if (_btnTutorialRect.Contains(e.Location)) _hoveredButton = 1;
            else if (_btnExitRect.Contains(e.Location)) _hoveredButton = 2;

            if (oldHover != _hoveredButton) Invalidate();
        }

        private void MainMenuView_MouseClick(object sender, MouseEventArgs e)
        {
            if (_btnStartRect.Contains(e.Location)) StartClicked?.Invoke(this, EventArgs.Empty);
            else if (_btnTutorialRect.Contains(e.Location)) TutorialClicked?.Invoke(this, EventArgs.Empty);
            else if (_btnExitRect.Contains(e.Location)) ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);
            DrawTitleBlock(g);
            LayoutButtons();
            DrawGlassButton(g, _btnStartRect, "FREE PLAY", _hoverAlphas[0], _neonCyan, 0);
            DrawGlassButton(g, _btnTutorialRect, "HOW TO PLAY", _hoverAlphas[1], _neonMint, 1);
            DrawGlassButton(g, _btnExitRect, "EXIT", _hoverAlphas[2], _neonRose, 2);

            DrawFooter(g);
        }

        private void DrawBackground(Graphics g)
        {
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Deep cyberpunk gradient base
            using (LinearGradientBrush brush = new LinearGradientBrush(rect,
                Color.FromArgb(14, 8, 28),
                Color.FromArgb(28, 10, 44), 45f))
            {
                g.FillRectangle(brush, rect);
            }

            // Animated soft orbs (glassmorphism)
            float pulse = (float)(Math.Sin(Environment.TickCount * 0.0015) * 0.5 + 0.5);
            int orbAlpha = 40 + (int)(pulse * 30);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(-120, -120, 420, 420);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(orbAlpha, 80, 40, 200);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, path);
                }
            }

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(this.Width - 320, this.Height - 320, 520, 520);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(orbAlpha, 200, 60, 140);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, path);
                }
            }

            // Subtle grid of faint dots for premium terminal feel
            using (SolidBrush dot = new SolidBrush(Color.FromArgb(10, 255, 255, 255)))
            {
                int spacing = Math.Max(28, this.Width / 40);
                for (int x = 0; x < this.Width; x += spacing)
                    for (int y = 0; y < this.Height; y += spacing)
                        g.FillEllipse(dot, x - 1, y - 1, 2, 2);
            }

            // Decorative neon lines (top and bottom)
            using (Pen neon = new Pen(Color.FromArgb(30, 120, 200, 255), 2))
            {
                neon.DashStyle = DashStyle.Dash;
                g.DrawLine(neon, 20, 60, this.Width - 20, 60);
                g.DrawLine(neon, 20, this.Height - 60, this.Width - 20, this.Height - 60);
            }
        }

        private void DrawTitleBlock(Graphics g)
        {
            string title = "CodeConnect";
            string subTitle = "ASSEMBLY LOGIC EDITION";

            // Title glow intensity derived from title glow state
            float glow = Math.Min(1f, Math.Max(0f, _titleGlow));
            int glowAlpha = 120 + (int)(glow * 80);

            using (Font titleFont = new Font("Segoe UI", Math.Max(28, this.Width / 28), FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", Math.Max(10, this.Width / 80), FontStyle.Regular))
            {
                SizeF titleSize = g.MeasureString(title, titleFont);
                PointF titlePos = new PointF((this.Width - titleSize.Width) / 2, this.Height * 0.12f);

                // Neon outline
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddString(title, titleFont.FontFamily, (int)titleFont.Style, g.DpiY * titleFont.Size / 72f, titlePos, StringFormat.GenericDefault);
                    using (Pen outline = new Pen(Color.FromArgb(glowAlpha, 100, 200, 255), 6) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(outline, gp);
                    }
                    using (SolidBrush fill = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                    {
                        g.FillPath(fill, gp);
                    }
                }

                // Subtitle
                SizeF subSize = g.MeasureString(subTitle, subFont);
                PointF subPos = new PointF((this.Width - subSize.Width) / 2, titlePos.Y + titleSize.Height - 6);
                g.DrawString(subTitle, subFont, new SolidBrush(Color.FromArgb(200, 200, 220, 255)), subPos);
            }

            // Small animated holographic scanline under title
            float scanX = (float)((Math.Sin(Environment.TickCount * 0.002) + 1) * 0.5);
            int scanWidth = Math.Max(120, this.Width / 6);
            RectangleF scanRect = new RectangleF((this.Width - scanWidth) * scanX, this.Height * 0.22f, scanWidth, 6);
            using (LinearGradientBrush scan = new LinearGradientBrush(scanRect, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(120, 255, 255, 255), 0f))
            {
                g.FillRectangle(scan, scanRect);
            }
        }

        private void LayoutButtons()
        {
            // Responsive button sizing
            float btnWidth = Math.Min(420, this.Width * 0.28f);
            float btnHeight = Math.Min(72, this.Height * 0.08f);
            float spacing = Math.Min(28, btnHeight * 0.4f);
            float startY = this.Height * 0.45f;
            float startX = (this.Width - btnWidth) / 2;

            _btnStartRect = new RectangleF(startX, startY, btnWidth, btnHeight);
            _btnTutorialRect = new RectangleF(startX, startY + btnHeight + spacing, btnWidth, btnHeight);
            _btnExitRect = new RectangleF(startX, startY + (btnHeight + spacing) * 2, btnWidth, btnHeight);
        }

        private void DrawGlassButton(Graphics g, RectangleF rect, string text, float hoverAlpha, Color accent, int index)
        {
            float radius = Math.Max(12f, rect.Height * 0.18f);

            // Shadow (slight lift on hover)
            float shadowOffset = 6f + hoverAlpha * -2f;
            using (GraphicsPath shadowPath = GetRoundedRect(new RectangleF(rect.X, rect.Y + shadowOffset, rect.Width, rect.Height), radius))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            // Glass background with subtle inner gradient
            using (GraphicsPath path = GetRoundedRect(rect, radius))
            using (LinearGradientBrush glass = new LinearGradientBrush(rect,
                Color.FromArgb(30, 255, 255, 255),
                Color.FromArgb(8, 255, 255, 255), 90f))
            {
                g.FillPath(glass, path);
            }

            // Neon accent glow (grows with hoverAlpha)
            if (hoverAlpha > 0.01f)
            {
                int glowAlpha = (int)(hoverAlpha * 140);
                RectangleF glowRect = new RectangleF(rect.X - 8, rect.Y - 8, rect.Width + 16, rect.Height + 16);
                using (GraphicsPath glowPath = GetRoundedRect(glowRect, radius + 8))
                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, accent)))
                {
                    g.FillPath(glowBrush, glowPath);
                }
            }

            // Inner accent stripe (left)
            RectangleF stripe = new RectangleF(rect.X + 8, rect.Y + 8, Math.Max(6, rect.Width * 0.04f), rect.Height - 16);
            using (LinearGradientBrush stripeBrush = new LinearGradientBrush(stripe, Color.FromArgb(220, accent), Color.FromArgb(120, Color.White), 90f))
            {
                using (GraphicsPath stripePath = GetRoundedRect(stripe, radius / 2f))
                    g.FillPath(stripeBrush, stripePath);
            }

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.5f))
            {
                using (GraphicsPath path = GetRoundedRect(rect, radius))
                    g.DrawPath(borderPen, path);
            }

            // Button label with subtle neon overlay on hover
            using (Font font = new Font("Segoe UI", Math.Max(12, rect.Height / 3), FontStyle.Bold))
            {
                SizeF size = g.MeasureString(text, font);
                PointF pos = new PointF(rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);

                // Drop shadow text
                g.DrawString(text, font, new SolidBrush(Color.FromArgb(160, 0, 0, 0)), pos.X, pos.Y + 1);

                // Base text
                g.DrawString(text, font, Brushes.White, pos);

                // Neon overlay
                if (hoverAlpha > 0.01f)
                {
                    using (SolidBrush neon = new SolidBrush(Color.FromArgb((int)(hoverAlpha * 255), accent)))
                    {
                        g.DrawString(text, font, neon, pos);
                    }
                }
            }

            // Small holographic icon circle to the left (purely decorative)
            RectangleF iconRect = new RectangleF(rect.X + 16, rect.Y + (rect.Height - 28) / 2, 28, 28);
            using (GraphicsPath iconPath = new GraphicsPath())
            {
                iconPath.AddEllipse(iconRect);
                using (PathGradientBrush pgb = new PathGradientBrush(iconPath))
                {
                    pgb.CenterColor = Color.FromArgb(255, Color.White);
                    pgb.SurroundColors = new[] { Color.FromArgb(255, accent.R, accent.G, accent.B) };
                    g.FillPath(pgb, iconPath);
                }

                using (Pen p = new Pen(Color.FromArgb(120, Color.White), 1f))
                    g.DrawPath(p, iconPath);
            }

            // Micro-interaction: subtle ripple when hovered (presentation only)
            if (hoverAlpha > 0.02f)
            {
                float ripple = (float)(Math.Sin(Environment.TickCount * 0.01 + index) * 0.5 + 0.5f) * hoverAlpha;
                RectangleF rippleRect = new RectangleF(rect.X + rect.Width - 28 - ripple * 12, rect.Y + 8 - ripple * 6, 12 + ripple * 24, rect.Height - 16 + ripple * 12);
                using (SolidBrush rb = new SolidBrush(Color.FromArgb((int)(ripple * 40), Color.White)))
                    g.FillEllipse(rb, rippleRect);
            }
        }

        private void DrawFooter(Graphics g)
        {
            string footer = "v1.0 • Crafted for premium code play";
            using (Font f = new Font("Segoe UI", Math.Max(8, this.Width / 140), FontStyle.Regular))
            {
                SizeF size = g.MeasureString(footer, f);
                PointF pos = new PointF((this.Width - size.Width) / 2, this.Height - size.Height - 12);
                g.DrawString(footer, f, new SolidBrush(Color.FromArgb(140, 200, 220, 255)), pos);
            }
        }

        // Reuse original rounded rect implementation to preserve behavior and compatibility
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
}
