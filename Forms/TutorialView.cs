using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CodeConnect.Forms
{
    public class TutorialView : UserControl
    {
        public event EventHandler CloseClicked;

        private RectangleF _btnCloseRect;
        private float _hoverAlpha = 0f;
        private System.Windows.Forms.Timer _animationTimer;
        private bool _isHovering = false;

        public TutorialView()
        {
            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;

            _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            this.MouseMove += TutorialView_MouseMove;
            this.MouseClick += TutorialView_MouseClick;
            this.MouseLeave += (s, e) => { _isHovering = false; Invalidate(); };
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            float target = _isHovering ? 1f : 0f;
            if (Math.Abs(_hoverAlpha - target) > 0.01f)
            {
                _hoverAlpha += (target - _hoverAlpha) * 0.2f;
                Invalidate();
            }
        }

        private void TutorialView_MouseMove(object sender, MouseEventArgs e)
        {
            bool wasHovering = _isHovering;
            _isHovering = _btnCloseRect.Contains(e.Location);
            if (wasHovering != _isHovering) Invalidate();
        }

        private void TutorialView_MouseClick(object sender, MouseEventArgs e)
        {
            if (_btnCloseRect.Contains(e.Location)) CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);

            // Title
            string title = "HOW TO PLAY";
            using (Font titleFont = new Font("Segoe UI", 36, FontStyle.Bold))
            {
                SizeF titleSize = g.MeasureString(title, titleFont);
                PointF titlePos = new PointF((this.Width - titleSize.Width) / 2, 50);
                g.DrawString(title, titleFont, Brushes.White, titlePos);
            }

            // Glass Content Panel
            float panelWidth = this.Width * 0.8f;
            float panelHeight = this.Height * 0.6f;
            RectangleF panelRect = new RectangleF((this.Width - panelWidth) / 2, 130, panelWidth, panelHeight);

            DrawGlassPanel(g, panelRect);

            // Content Text
            string content = "MISSION:\nConnect all matching Assembly statement nodes to clear the logic grid.\n\n" +
                             "FLOWS:\n1. Click a colored Node to select its 'Flow' color.\n" +
                             "2. Click empty cells to draw a Pipe in that color.\n" +
                             "3. Connect the Opcode (e.g. MOV), Register (e.g. EAX), and Value to complete the statement.\n\n" +
                             "RULES:\n- Pipes cannot cross each other.\n- Every node must be part of a completed connection.\n- The Assembly Core validates the entire grid for logical correctness.";

            using (Font contentFont = new Font("Segoe UI", 14, FontStyle.Regular))
            {
                RectangleF textRect = new RectangleF(panelRect.X + 40, panelRect.Y + 40, panelRect.Width - 80, panelRect.Height - 80);
                g.DrawString(content, contentFont, new SolidBrush(Color.FromArgb(230, 255, 255, 255)), textRect);
            }

            // Button
            float btnWidth = 300;
            float btnHeight = 60;
            _btnCloseRect = new RectangleF((this.Width - btnWidth) / 2, this.Height - 120, btnWidth, btnHeight);

            DrawGlassButton(g, _btnCloseRect, "GET STARTED", _hoverAlpha, Color.FromArgb(100, 255, 150));
        }

        private void DrawBackground(Graphics g)
        {
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(18, 12, 34), Color.FromArgb(34, 18, 48), 45f))
            {
                g.FillRectangle(brush, rect);
            }

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(this.Width / 2 - 300, -100, 600, 400);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(50, 100, 200, 255);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, path);
                }
            }
        }

        private void DrawGlassPanel(Graphics g, RectangleF rect)
        {
            float radius = 20;
            using (GraphicsPath path = GetRoundedRect(rect, radius))
            {
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, GetRoundedRect(new RectangleF(rect.X, rect.Y + 8, rect.Width, rect.Height), radius));
                }
                using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(25, 255, 255, 255)))
                {
                    g.FillPath(glassBrush, path);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }
        }

        private void DrawGlassButton(Graphics g, RectangleF rect, string text, float hoverAlpha, Color accent)
        {
            float radius = 15;
            using (GraphicsPath path = GetRoundedRect(rect, radius))
            {
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, GetRoundedRect(new RectangleF(rect.X, rect.Y + 4, rect.Width, rect.Height), radius));
                }

                int baseAlpha = 30;
                int hoverBonus = (int)(hoverAlpha * 40);
                using (SolidBrush glassBrush = new SolidBrush(Color.FromArgb(baseAlpha + hoverBonus, 255, 255, 255)))
                {
                    g.FillPath(glassBrush, path);
                }

                if (hoverAlpha > 0)
                {
                    using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb((int)(hoverAlpha * 30), accent)))
                    {
                        g.FillPath(glowBrush, path);
                    }
                }

                using (Pen borderPen = new Pen(Color.FromArgb(80 + (int)(hoverAlpha * 80), 255, 255, 255), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }

                using (Font font = new Font("Segoe UI", 14, FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(text, font);
                    PointF pos = new PointF(rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
                    g.DrawString(text, font, Brushes.White, pos);
                    if (hoverAlpha > 0)
                    {
                        using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb((int)(hoverAlpha * 255), accent)))
                        {
                            g.DrawString(text, font, accentBrush, pos);
                        }
                    }
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
}
