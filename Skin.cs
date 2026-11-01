using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    internal static class Skin
    {
        public static readonly Color Back = Color.FromArgb(255, 255, 255);
        public static readonly Color Panel = Color.FromArgb(222, 219, 222);
        public static readonly Color PanelEdge = Color.FromArgb(206, 202, 206);
        public static readonly Color EqCol = Color.FromArgb(219, 223, 219);
        public static readonly Color ColEdge = Color.FromArgb(173, 170, 173);
        public static readonly Color Dark = Color.FromArgb(60, 60, 64);
        public static readonly Color Blue = Color.FromArgb(0, 95, 210);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            if (d < 1) d = 1;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal enum SliderKind { Eq, Mixer, Horiz }

    // منزلق مرسوم بالكود بنفس شكل الاصلي
    internal sealed class CSlider : Control
    {
        private int _min, _max = 100, _val;
        private bool _vert, _drag;
        public SliderKind Kind = SliderKind.Mixer;
        public bool Column;          // رسم عمود EQ (خلفية + حدود + ارقام + تدريج)
        public bool DarkGroove;      // مجرى غامق (المنزلقات النشطة في Mixer)
        public int PadTop = 10, PadBottom = 10;
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

        private float Frac() { return _max == _min ? 0f : (float)(_val - _min) / (float)(_max - _min); }

        private int Lo { get { return _vert ? PadTop : PadTop; } }
        private int Span { get { return _vert ? Math.Max(1, Height - PadTop - PadBottom) : Math.Max(1, Width - PadTop - PadBottom); } }

        private void FromMouse(Point p)
        {
            float f = ((_vert ? p.Y : p.X) - PadTop) / (float)Span;
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
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
                int top = PadTop;
                int bot = Height - PadBottom;
                int ty = top + (int)Math.Round((bot - top) * f);
                if (Kind == SliderKind.Eq)
                {
                    if (Column)
                    {
                        using (Brush b = new SolidBrush(Skin.EqCol)) { g.FillRectangle(b, 0, 0, Width - 1, Height - 1); }
                        using (Pen p = new Pen(Skin.ColEdge)) { g.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
                        DrawTicks(g, top, bot, cx);
                    }
                    Rectangle gv = new Rectangle(cx - 2, top - 12, 4, bot - top + 24);
                    using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(91, 90, 91), Color.FromArgb(150, 150, 150), 0f))
                    { g.FillRectangle(b, gv); }
                    Bitmap tb = Assets.Get("thumb_v");
                    if (tb != null) { g.SmoothingMode = SmoothingMode.None; g.DrawImage(tb, cx - tb.Width / 2, ty - tb.Height / 2, tb.Width, tb.Height); }
                    else DrawThumb(g, new Rectangle(cx - 8, ty - 12, 16, 24), true);
                    if (Column && !string.IsNullOrEmpty(Text))
                    {
                        using (Font ft = new Font("Tahoma", 9f, FontStyle.Bold))
                        { TextRenderer.DrawText(g, Text, ft, new Rectangle(0, Height - 24, Width, 20), Color.FromArgb(60, 60, 60), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                    }
                }
                else
                {
                    Rectangle gv = new Rectangle(cx - 3, top - 4, 6, bot - top + 8);
                    using (GraphicsPath gp = Skin.Round(gv, 3))
                    {
                        if (DarkGroove)
                        {
                            using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(41, 40, 41), Color.FromArgb(163, 162, 163), 0f)) { g.FillPath(b, gp); }
                        }
                        else
                        {
                            using (Brush b = new SolidBrush(Color.FromArgb(205, 205, 208))) { g.FillPath(b, gp); }
                            using (Pen p = new Pen(Color.FromArgb(170, 170, 175))) { g.DrawPath(p, gp); }
                        }
                    }
                    DrawThumb(g, new Rectangle(cx - 8, ty - 9, 16, 18), true);
                }
            }
            else
            {
                int cy = Height / 2;
                int left = PadTop;
                int right = Width - PadBottom;
                int tx = left + (int)Math.Round((right - left) * f);
                Rectangle gv = new Rectangle(left - 4, cy - 3, right - left + 8, 6);
                using (GraphicsPath gp = Skin.Round(gv, 3))
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(205, 205, 208))) { g.FillPath(b, gp); }
                    using (Pen p = new Pen(Color.FromArgb(170, 170, 175))) { g.DrawPath(p, gp); }
                }
                DrawThumb(g, new Rectangle(tx - 9, cy - 8, 18, 16), false);
            }
        }

        private void DrawTicks(Graphics g, int top, int bot, int cx)
        {
            g.SmoothingMode = SmoothingMode.None;
            Color gray = Color.FromArgb(155, 159, 155);
            using (Pen tp = new Pen(gray))
            using (Pen rp = new Pen(Color.FromArgb(237, 0, 0)))
            {
                for (int i = 0; i <= 24; i++)
                {
                    int y = top + (int)Math.Round((bot - top) * i / 24.0);
                    int len = (i % 6 == 0) ? 11 : ((i % 3 == 0) ? 8 : 5);
                    Pen p = (i == 0 || i == 24) ? rp : tp;
                    if (i == 12) continue;
                    g.DrawLine(p, 3, y, 3 + len, y);
                    g.DrawLine(p, Width - 4 - len, y, Width - 4, y);
                }
                int zy = top + (bot - top) / 2;
                using (Brush gb = new SolidBrush(gray)) { g.FillRectangle(gb, 3, zy - 1, 11, 3); }
                using (Brush bb = new SolidBrush(Color.Black))
                { g.FillPolygon(bb, new Point[] { new Point(Width - 3, zy - 4), new Point(Width - 3, zy + 4), new Point(Width - 10, zy) }); }
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private static void DrawThumb(Graphics g, Rectangle th, bool vert)
        {
            using (GraphicsPath gp = Skin.Round(th, 3))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(th, Color.FromArgb(255, 255, 255), Color.FromArgb(205, 205, 210), vert ? 90f : 90f))
                { g.FillPath(b, gp); }
                using (Pen p = new Pen(Color.FromArgb(125, 125, 130))) { g.DrawPath(p, gp); }
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
            Font = new Font("Tahoma", 9f);
            ForeColor = Color.Black;
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
            Color c1 = Checked ? Color.FromArgb(205, 225, 252) : Color.FromArgb(255, 255, 255);
            Color c2 = Checked ? Color.FromArgb(150, 185, 235) : (_hover ? Color.FromArgb(232, 236, 244) : Color.FromArgb(222, 222, 226));
            if (_down) { Color t = c1; c1 = c2; c2 = t; }
            using (GraphicsPath gp = Skin.Round(r, 6))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, c1, c2, 90f)) { g.FillPath(b, gp); }
                using (Pen p = new Pen(Color.FromArgb(70, 70, 76), 1.4f)) { g.DrawPath(p, gp); }
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ايقونات الشريط السفلي (صور الاصلية + رسم لـ Stereo)
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
            Size = new Size(27, 27);
            Cursor = BlueCursor.Arrow;
        }
        public bool On { get { return _on; } set { if (_on != value) { _on = value; Invalidate(); } } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private string AssetName()
        {
            switch (_kind)
            {
                case "eq": return "ic_grid";
                case "speaker": return "ic_woof";
                case "mixer": return "ic_caps";
                case "bass": return "ic_plus";
                case "surround": return "ic_swirl";
                default: return null;
            }
        }

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
            Bitmap img = null;
            string an = AssetName();
            if (an != null) img = Assets.Get(an);
            if (img != null)
            {
                g.SmoothingMode = SmoothingMode.None;
                g.DrawImage(img, 1, 1, img.Width, img.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            else
            {
                Rectangle r = new Rectangle(1, 1, 24, 24);
                using (GraphicsPath gp = Skin.Round(r, 3))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(250, 250, 250), Color.FromArgb(200, 200, 204), 90f)) { g.FillPath(b, gp); }
                    using (Pen p = new Pen(Color.FromArgb(90, 90, 94))) { g.DrawPath(p, gp); }
                }
                Color c = Color.FromArgb(40, 42, 50);
                using (Brush b = new SolidBrush(c))
                {
                    Cone(g, b, 3, 13, 0.9f);
                    Cone(g, b, 14, 13, 0.9f);
                }
            }
            if (_on)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(70, 40, 120, 255))) { g.FillRectangle(b, 1, 1, 25, 25); }
                using (Pen p = new Pen(Color.FromArgb(0, 95, 210), 2f)) { g.DrawRectangle(p, 1, 1, 24, 24); }
            }
            else if (_hover)
            {
                using (Pen p = new Pen(Color.FromArgb(0, 95, 210), 1f)) { g.DrawRectangle(p, 1, 1, 24, 24); }
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
            using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(156, 150, 132), Color.FromArgb(255, 245, 190), 90f))
            { g.FillRectangle(b, r); }
            using (Brush b = new SolidBrush(Color.FromArgb(90, 60, 50, 40)))
            { g.FillPolygon(b, new PointF[] { new PointF(0, 0), new PointF(34, 24), new PointF(34, Height - 24), new PointF(0, Height) }); }
            using (Brush b = new SolidBrush(Color.FromArgb(90, 60, 50, 40)))
            { g.FillPolygon(b, new PointF[] { new PointF(Width, 0), new PointF(Width - 34, 24), new PointF(Width - 34, Height - 24), new PointF(Width, Height) }); }
            using (Brush b = new SolidBrush(Color.FromArgb(70, 120, 110, 100)))
            { g.FillRectangle(b, Width / 2 - 40, 6, 80, 12); }
            if (_head)
            {
                using (Pen band = new Pen(Color.FromArgb(25, 25, 28), 5f))
                { g.DrawArc(band, Width / 2 - 38, 36, 76, 80, 180, 180); }
                using (Brush cup = new SolidBrush(Color.FromArgb(30, 30, 34)))
                {
                    g.FillEllipse(cup, Width / 2 - 48, 76, 22, 34);
                    g.FillEllipse(cup, Width / 2 + 26, 76, 22, 34);
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
                byte[] raw = Convert.FromBase64String(CursorData.HandBlue);
                return new Cursor(new System.IO.MemoryStream(raw));
            }
            catch (Exception) { }
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
