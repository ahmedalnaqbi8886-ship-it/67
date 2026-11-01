using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    internal static class Skin
    {
        public static readonly Color Back = Color.FromArgb(238, 238, 238);
        public static readonly Color Panel = Color.FromArgb(217, 217, 217);
        public static readonly Color PanelEdge = Color.FromArgb(190, 190, 190);
        public static readonly Color Dark = Color.FromArgb(60, 60, 64);
        public static readonly Color Blue = Color.FromArgb(70, 135, 230);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // منزلق مرسوم بالكود (نفس شكل Creative: مجرى غامق + مقبض فاتح بخط ازرق)
    internal sealed class CSlider : Control
    {
        private int _min, _max = 100, _val;
        private bool _vert, _drag;
        public bool Ticks;
        public int TickFrequency = 10;
        public event EventHandler ValueChanged;

        public CSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(120, 26);
        }

        public Orientation Orientation
        {
            get { return _vert ? Orientation.Vertical : Orientation.Horizontal; }
            set { _vert = value == Orientation.Vertical; Invalidate(); }
        }
        public int Minimum { get { return _min; } set { _min = value; if (_val < _min) _val = _min; Invalidate(); } }
        public int Maximum { get { return _max; } set { _max = value; if (_val > _max) _val = _max; Invalidate(); } }
        public int Value
        {
            get { return _val; }
            set
            {
                int v = value < _min ? _min : (value > _max ? _max : value);
                if (v == _val) return;
                _val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private float Frac()
        {
            return _max == _min ? 0f : (float)(_val - _min) / (float)(_max - _min);
        }

        private int Pad { get { return 10; } }

        private void FromMouse(Point p)
        {
            float f;
            if (_vert) f = (p.Y - Pad) / (float)Math.Max(1, Height - 2 * Pad);
            else f = (p.X - Pad) / (float)Math.Max(1, Width - 2 * Pad);
            if (f < 0f) f = 0f; if (f > 1f) f = 1f;
            Value = _min + (int)Math.Round(f * (_max - _min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _drag = true; Capture = true; FromMouse(e.Location); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag) FromMouse(e.Location);
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _drag = false; Capture = false;
            base.OnMouseUp(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int step = Math.Max(1, (_max - _min) / 20);
            Value = _val + (e.Delta > 0 ? -step : step) * (_vert ? 1 : -1);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float f = Frac();
            if (_vert)
            {
                int cx = Width / 2;
                Rectangle groove = new Rectangle(cx - 3, Pad - 4, 6, Height - 2 * Pad + 8);
                using (GraphicsPath gp = Skin.Round(groove, 3))
                using (LinearGradientBrush b = new LinearGradientBrush(groove, Color.FromArgb(40, 40, 44), Color.FromArgb(130, 130, 135), 0f))
                { g.FillPath(b, gp); }
                if (Ticks)
                {
                    using (Pen tp = new Pen(Color.FromArgb(150, 150, 150)))
                    {
                        for (int i = 0; i <= 8; i++)
                        {
                            int y = Pad + (int)((Height - 2 * Pad) * i / 8f);
                            g.DrawLine(tp, cx - 12, y, cx - 7, y); g.DrawLine(tp, cx + 7, y, cx + 12, y);
                        }
                    }
                }
                int ty = Pad + (int)Math.Round((Height - 2 * Pad) * f);
                Rectangle th = new Rectangle(cx - 11, ty - 7, 22, 14);
                DrawThumb(g, th, true);
            }
            else
            {
                int cy = Height / 2;
                Rectangle groove = new Rectangle(Pad - 4, cy - 3, Width - 2 * Pad + 8, 6);
                using (GraphicsPath gp = Skin.Round(groove, 3))
                using (LinearGradientBrush b = new LinearGradientBrush(groove, Color.FromArgb(40, 40, 44), Color.FromArgb(130, 130, 135), 90f))
                { g.FillPath(b, gp); }
                int tx = Pad + (int)Math.Round((Width - 2 * Pad) * f);
                Rectangle th = new Rectangle(tx - 7, cy - 11, 14, 22);
                DrawThumb(g, th, false);
            }
        }

        private static void DrawThumb(Graphics g, Rectangle th, bool vert)
        {
            using (GraphicsPath gp = Skin.Round(th, 3))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(th, Color.FromArgb(252, 252, 252), Color.FromArgb(190, 190, 195), vert ? 90f : 0f))
                { g.FillPath(b, gp); }
                using (Pen p = new Pen(Color.FromArgb(70, 70, 76))) { g.DrawPath(p, gp); }
            }
            using (Pen bp = new Pen(Skin.Blue, 2f))
            {
                if (vert) g.DrawLine(bp, th.X + 3, th.Y + th.Height / 2, th.Right - 3, th.Y + th.Height / 2);
                else g.DrawLine(bp, th.X + th.Width / 2, th.Y + 3, th.X + th.Width / 2, th.Bottom - 3);
            }
        }
    }

    // زر مدور (Default / Channel / Noise)
    internal sealed class SkinButton : Control
    {
        private bool _hover, _down;
        public bool Checked;
        public SkinButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(86, 24);
            Cursor = BlueCursor.Arrow;
            Font = new Font("Tahoma", 8f);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color c1 = Checked ? Color.FromArgb(185, 205, 240) : Color.FromArgb(255, 255, 255);
            Color c2 = Checked ? Color.FromArgb(130, 165, 225) : (_hover ? Color.FromArgb(225, 225, 230) : Color.FromArgb(205, 205, 210));
            if (_down) { Color t = c1; c1 = c2; c2 = t; }
            using (GraphicsPath gp = Skin.Round(r, 6))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, c1, c2, 90f)) { g.FillPath(b, gp); }
                using (Pen p = new Pen(Color.FromArgb(70, 70, 76))) { g.DrawPath(p, gp); }
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ايقونات الشريط السفلي (Bass / Stereo / Surround / Eq / Speaker / Mixer)
    internal sealed class BarIcon : Control
    {
        private readonly string _kind;
        private bool _on, _hover;
        public BarIcon(string kind)
        {
            _kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(30, 26);
            Cursor = BlueCursor.Arrow;
        }
        public bool On { get { return _on; } set { if (_on != value) { _on = value; Invalidate(); } } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private static void Cone(Graphics g, Brush b, float x, float cy, float s)
        {
            g.FillRectangle(b, x, cy - 3f * s, 3f * s, 6f * s);
            g.FillPolygon(b, new PointF[] {
                new PointF(x + 3f * s, cy - 3f * s), new PointF(x + 8f * s, cy - 7f * s),
                new PointF(x + 8f * s, cy + 7f * s), new PointF(x + 3f * s, cy + 3f * s) });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            if (_on || _hover)
            {
                using (GraphicsPath gp = Skin.Round(r, 5))
                {
                    using (Brush b = new SolidBrush(_on ? Color.FromArgb(170, 200, 245) : Color.FromArgb(120, 255, 255, 255))) { g.FillPath(b, gp); }
                    using (Pen p = new Pen(_on ? Color.FromArgb(50, 100, 190) : Color.FromArgb(120, 120, 125))) { g.DrawPath(p, gp); }
                }
            }
            Color c = Color.FromArgb(40, 42, 50);
            using (Brush b = new SolidBrush(c))
            using (Pen pen = new Pen(c, 1.5f))
            {
                float cy = Height / 2f;
                if (_kind == "bass")
                {
                    Cone(g, b, 5, cy, 1.2f);
                    g.DrawArc(pen, 12, cy - 4, 6, 8, -60, 120);
                    g.DrawArc(pen, 14, cy - 7, 9, 14, -60, 120);
                    g.DrawArc(pen, 16, cy - 10, 12, 20, -60, 120);
                }
                else if (_kind == "stereo")
                {
                    Cone(g, b, 2, cy, 0.9f);
                    Cone(g, b, 16, cy, 0.9f);
                }
                else if (_kind == "surround")
                {
                    Cone(g, b, 10, cy, 1.0f);
                    g.DrawArc(pen, 19, cy - 5, 6, 10, -60, 120);
                    g.DrawArc(pen, 21, cy - 8, 9, 16, -60, 120);
                    g.DrawArc(pen, 2, cy - 5, 6, 10, 120, 120);
                    g.DrawArc(pen, 0, cy - 8, 9, 16, 120, 120);
                }
                else if (_kind == "eq")
                {
                    g.FillRectangle(b, 6, 8, 3, 11); g.FillRectangle(b, 13, 5, 3, 14); g.FillRectangle(b, 20, 10, 3, 9);
                    using (Brush w = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        g.FillRectangle(w, 5, 10, 5, 3); g.FillRectangle(w, 12, 8, 5, 3); g.FillRectangle(w, 19, 13, 5, 3);
                    }
                }
                else if (_kind == "speaker")
                {
                    g.FillEllipse(b, 5, 3, 20, 20);
                    using (Brush w = new SolidBrush(Color.FromArgb(235, 235, 235)))
                    {
                        g.FillEllipse(w, 9, 7, 12, 12);
                    }
                    g.FillEllipse(b, 12, 10, 6, 6);
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int x = 6 + i * 6;
                        g.DrawLine(pen, x, 5, x, 21);
                        g.FillRectangle(b, x - 2, 8 + (i * 5) % 9, 5, 4);
                    }
                }
            }
        }
    }

    // صورة غرفة الاختبار (سماعة راس او سماعتين)
    internal sealed class RoomPic : Control
    {
        private bool _head = true;
        public RoomPic()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(220, 140);
        }
        public bool Headphone { get { return _head; } set { if (_head != value) { _head = value; Invalidate(); } } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(120, 112, 100), Color.FromArgb(250, 205, 150), 90f))
            { g.FillRectangle(b, r); }
            using (Brush b = new SolidBrush(Color.FromArgb(70, 255, 235, 200)))
            { g.FillPolygon(b, new PointF[] { new PointF(0, 0), new PointF(30, 22), new PointF(30, Height - 30), new PointF(0, Height) }); }
            using (Brush b = new SolidBrush(Color.FromArgb(60, 255, 235, 200)))
            { g.FillPolygon(b, new PointF[] { new PointF(Width, 0), new PointF(Width - 30, 22), new PointF(Width - 30, Height - 30), new PointF(Width, Height) }); }
            using (Pen p = new Pen(Color.FromArgb(90, 40, 30, 20), 1f))
            { g.DrawRectangle(p, 30, 22, Width - 60, Height - 52); }
            if (_head)
            {
                using (Pen band = new Pen(Color.FromArgb(25, 25, 28), 5f))
                {
                    g.DrawArc(band, Width / 2 - 38, 30, 76, 80, 180, 180);
                }
                using (Brush cup = new SolidBrush(Color.FromArgb(30, 30, 34)))
                {
                    g.FillEllipse(cup, Width / 2 - 48, 70, 22, 34);
                    g.FillEllipse(cup, Width / 2 + 26, 70, 22, 34);
                }
            }
            else
            {
                using (Brush cab = new SolidBrush(Color.FromArgb(28, 28, 32)))
                using (Brush cone = new SolidBrush(Color.FromArgb(90, 90, 98)))
                {
                    g.FillRectangle(cab, 46, 48, 40, 64); g.FillRectangle(cab, Width - 86, 48, 40, 64);
                    g.FillEllipse(cone, 52, 78, 28, 28); g.FillEllipse(cone, Width - 80, 78, 28, 28);
                    g.FillEllipse(cone, 59, 56, 14, 14); g.FillEllipse(cone, Width - 73, 56, 14, 14);
                }
            }
        }
    }

    // مؤشر ماوس ازرق (يطبق على كل النوافذ وعلى نافذة الكرت نفسها)
    internal static class BlueCursor
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }
        [DllImport("user32.dll")] private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO ii);
        [DllImport("user32.dll")] private static extern IntPtr CreateIconIndirect(ref ICONINFO ii);
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr h);

        private static Cursor _arrow;

        public static Cursor Arrow
        {
            get
            {
                if (_arrow == null) _arrow = Make();
                return _arrow;
            }
        }

        private static Cursor Make()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        PointF[] pts = new PointF[]
                        {
                            new PointF(1.5f, 1.5f), new PointF(1.5f, 21f), new PointF(6.5f, 16.5f),
                            new PointF(10.5f, 25f), new PointF(14f, 23.5f), new PointF(10f, 15.5f), new PointF(17f, 15.5f)
                        };
                        using (Brush b = new LinearGradientBrush(new Rectangle(0, 0, 18, 26), Color.FromArgb(90, 170, 255), Color.FromArgb(20, 90, 215), 90f))
                        { g.FillPolygon(b, pts); }
                        using (Pen p = new Pen(Color.White, 1.6f)) { p.LineJoin = LineJoin.Round; g.DrawPolygon(p, pts); }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    ICONINFO ii;
                    if (!GetIconInfo(hIcon, out ii)) { DestroyIcon(hIcon); return Cursors.Arrow; }
                    ii.fIcon = false; ii.xHotspot = 1; ii.yHotspot = 1;
                    IntPtr hCur = CreateIconIndirect(ref ii);
                    if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
                    if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
                    DestroyIcon(hIcon);
                    if (hCur == IntPtr.Zero) return Cursors.Arrow;
                    return new Cursor(hCur);
                }
            }
            catch (Exception) { return Cursors.Arrow; }
        }

        // يطبق المؤشر الازرق على نافذة وكل ما فيها، ويثبته حتى لو البرنامج غيّره
        public static void Apply(Control root)
        {
            if (root == null) return;
            try
            {
                Cursor c = Arrow;
                root.Cursor = c;
                foreach (Control ch in root.Controls) Apply(ch);
                root.ControlAdded += delegate(object s, ControlEventArgs e) { Apply(e.Control); };
            }
            catch (Exception) { }
        }

        public static void Pin(Control form)
        {
            if (form == null) return;
            try
            {
                Cursor c = Arrow;
                form.MouseMove += delegate { if (!object.ReferenceEquals(form.Cursor, c)) form.Cursor = c; Cursor.Current = c; };
                form.MouseDown += delegate { form.Cursor = c; Cursor.Current = c; };
                form.MouseUp += delegate { form.Cursor = c; Cursor.Current = c; };
            }
            catch (Exception) { }
        }
    }
}
