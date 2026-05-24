using System.Drawing;
using System.Drawing.Drawing2D;

namespace CodeConnect.Helpers
{
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
