using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // زر بشكل سماعة (Bass / Stereo / Surround) - مرسوم بالكود، بدون صور او شعارات
    internal sealed class SpeakerButton : Control
    {
        public enum Kind { Bass, Stereo, Surround }

        private readonly Kind _kind;
        private bool _on;
        private bool _hover;
        public string Caption;

        public SpeakerButton(Kind kind, string caption)
        {
            _kind = kind;
            Caption = caption;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(72, 62);
            Cursor = BlueCursor.Arrow;
        }

        public bool On
        {
            get { return _on; }
            set { if (_on != value) { _on = value; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void Speaker(Graphics g, float cx, float cy, float s, Color c, int wavesRight, int wavesLeft)
        {
            using (Brush b = new SolidBrush(c))
            using (Pen pen = new Pen(c, 1.5f * s))
            {
                g.FillRectangle(b, cx - 9f * s, cy - 4f * s, 5f * s, 8f * s);
                PointF[] cone = new PointF[]
                {
                    new PointF(cx - 4f * s, cy - 4f * s), new PointF(cx + 2f * s, cy - 9f * s),
                    new PointF(cx + 2f * s, cy + 9f * s), new PointF(cx - 4f * s, cy + 4f * s)
                };
                g.FillPolygon(b, cone);
                for (int i = 1; i <= wavesRight; i++)
                {
                    float r = (3f + 3.2f * i) * s;
                    g.DrawArc(pen, cx + 2f * s - r, cy - r, 2f * r, 2f * r, -50f, 100f);
                }
                for (int i = 1; i <= wavesLeft; i++)
                {
                    float r = (3f + 3.2f * i) * s;
                    g.DrawArc(pen, cx - 9f * s - r, cy - r, 2f * r, 2f * r, 130f, 100f);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color bg = _on ? Color.FromArgb(52, 120, 224) : (_hover ? Color.FromArgb(234, 238, 246) : Color.FromArgb(222, 225, 232));
            Color fg = _on ? Color.White : Color.FromArgb(55, 60, 75);
            using (GraphicsPath p = Round(r, 8))
            {
                using (Brush b = new SolidBrush(bg)) { g.FillPath(b, p); }
                using (Pen pen = new Pen(Color.FromArgb(120, 126, 140))) { g.DrawPath(pen, p); }
            }
            float cx = Width / 2f, cy = 23f;
            if (_kind == Kind.Bass) Speaker(g, cx - 3f, cy, 1.45f, fg, 3, 0);
            else if (_kind == Kind.Stereo)
            {
                Speaker(g, cx - 15f, cy, 0.95f, fg, 1, 0);
                Speaker(g, cx + 11f, cy, 0.95f, fg, 1, 0);
            }
            else Speaker(g, cx, cy, 1.1f, fg, 2, 2);

            using (StringFormat sf = new StringFormat())
            using (Brush tb = new SolidBrush(fg))
            using (Font f = new Font("Tahoma", 7.5f, FontStyle.Bold))
            {
                sf.Alignment = StringAlignment.Center;
                g.DrawString(Caption, f, tb, new RectangleF(0, Height - 19, Width, 16), sf);
            }
        }
    }
}
