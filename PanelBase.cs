using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // اساس النوافذ: ترويسة (ايقونة + عنوان + جهاز + ؟) + جسم + شريط سفلي
    internal abstract class PanelBase : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        protected const int HeadH = 42;
        protected const int BarH = 38;

        protected readonly FxChain Fx;
        protected readonly Panel Body;
        protected bool Loading;
        private readonly string _kind;
        private readonly string _title;
        private Panel _head, _bar;
        private BarIcon[] _nav;

        protected PanelBase(FxChain fx, string kind, string title, int w, int h)
        {
            Fx = fx;
            _kind = kind;
            _title = title;
            Text = title;
            Font = new Font("Tahoma", 8f);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            BackColor = Skin.Back;
            ClientSize = new Size(w, HeadH + h + BarH);

            Body = new Panel();
            Body.SetBounds(0, HeadH, w, h);
            Body.BackColor = Skin.Back;
            Controls.Add(Body);

            BuildHead(w);
            BuildBar(w, HeadH + h);
            Cursor = BlueCursor.Arrow;
            BlueCursor.Apply(this);
            BlueCursor.Pin(this);
        }

        protected abstract void DoDefault();
        public abstract void Reload();

        private void BuildHead(int w)
        {
            _head = new Panel();
            _head.SetBounds(0, 0, w, HeadH);
            _head.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rr = new Rectangle(0, 0, _head.Width, _head.Height);
                using (LinearGradientBrush b = new LinearGradientBrush(rr, Color.FromArgb(255, 255, 255), Color.FromArgb(222, 222, 226), 90f))
                { g.FillRectangle(b, rr); }
                using (Pen p = new Pen(Color.FromArgb(150, 150, 155))) { g.DrawLine(p, 0, HeadH - 1, _head.Width, HeadH - 1); }
                Rectangle ic = new Rectangle(8, 6, 30, 30);
                using (GraphicsPath gp = Skin.Round(ic, 6))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(ic, Color.FromArgb(90, 95, 110), Color.FromArgb(25, 25, 32), 90f)) { g.FillPath(b, gp); }
                    using (Pen p = new Pen(Color.FromArgb(40, 40, 46))) { g.DrawPath(p, gp); }
                }
                using (Pen ip = new Pen(Color.White, 2f))
                using (Brush ib = new SolidBrush(Color.FromArgb(120, 190, 255)))
                {
                    if (_kind == "speaker")
                    {
                        g.DrawEllipse(ip, 13, 11, 20, 20);
                        g.FillEllipse(ib, 19, 17, 8, 8);
                    }
                    else if (_kind == "mixer")
                    {
                        for (int i = 0; i < 3; i++) { g.DrawLine(ip, 15 + i * 7, 11, 15 + i * 7, 31); g.FillRectangle(ib, 12 + i * 7, 14 + i * 5, 6, 4); }
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++) { g.DrawLine(ip, 13 + i * 6, 11, 13 + i * 6, 31); g.FillRectangle(ib, 11 + i * 6, 25 - (i * 7) % 14, 5, 4); }
                    }
                }
                using (Font f = new Font("Tahoma", 10.5f, FontStyle.Bold))
                { TextRenderer.DrawText(g, _title, f, new Point(46, 11), Color.FromArgb(25, 25, 30)); }
            };
            _head.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
            };
            Controls.Add(_head);

            ComboBox dev = new ComboBox();
            dev.DropDownStyle = ComboBoxStyle.DropDownList;
            dev.Items.Add("Virtual Audio Output");
            dev.SelectedIndex = 0;
            dev.SetBounds(w - 300, 10, 170, 21);
            _head.Controls.Add(dev);

            SkinButton help = new SkinButton();
            help.Text = "?";
            help.Font = new Font("Tahoma", 9f, FontStyle.Bold);
            help.SetBounds(w - 122, 9, 38, 24);
            help.Click += delegate { MessageBox.Show(this, "Audio effects panel.\r\nEqualizer, Surround Mixer and Speaker Settings.", _title); };
            _head.Controls.Add(help);

            Label min = new Label();
            min.Text = "—"; min.Font = new Font("Tahoma", 9f, FontStyle.Bold);
            min.BackColor = Color.Transparent; min.Cursor = BlueCursor.Arrow;
            min.SetBounds(w - 66, 10, 24, 22); min.TextAlign = ContentAlignment.MiddleCenter;
            min.Click += delegate { WindowState = FormWindowState.Minimized; };
            _head.Controls.Add(min);

            Label close = new Label();
            close.Text = "X"; close.Font = new Font("Tahoma", 9f, FontStyle.Bold);
            close.BackColor = Color.Transparent; close.Cursor = BlueCursor.Arrow;
            close.SetBounds(w - 38, 10, 24, 22); close.TextAlign = ContentAlignment.MiddleCenter;
            close.Click += delegate { Close(); };
            _head.Controls.Add(close);
        }

        private void BuildBar(int w, int y)
        {
            _bar = new Panel();
            _bar.SetBounds(0, y, w, BarH);
            _bar.Paint += delegate(object s, PaintEventArgs e)
            {
                Rectangle rr = new Rectangle(0, 0, _bar.Width, _bar.Height);
                using (LinearGradientBrush b = new LinearGradientBrush(rr, Color.FromArgb(246, 246, 248), Color.FromArgb(176, 176, 182), 90f))
                { e.Graphics.FillRectangle(b, rr); }
                using (Pen p = new Pen(Color.FromArgb(120, 120, 126))) { e.Graphics.DrawLine(p, 0, 0, _bar.Width, 0); }
            };
            Controls.Add(_bar);

            SkinButton def = new SkinButton();
            def.Text = "Default";
            def.SetBounds(14, 7, 96, 24);
            def.Click += delegate { DoDefault(); Reload(); };
            _bar.Controls.Add(def);

            string[] kinds = new string[] { "eq", "speaker", "mixer" };
            string[] tips = new string[] { "Equalizer", "Speaker Settings", "Surround Mixer" };
            ToolTip tt = new ToolTip();
            _nav = new BarIcon[3];
            for (int i = 0; i < kinds.Length; i++)
            {
                string k = kinds[i];
                BarIcon nb = new BarIcon(k);
                nb.On = (k == _kind);
                nb.Location = new Point(124 + i * 34, 6);
                nb.Click += delegate { EqPanel.OpenFrom(k, this); };
                tt.SetToolTip(nb, tips[i]);
                _bar.Controls.Add(nb);
                _nav[i] = nb;
            }
        }

        // ايقونة تبديل اضافية في الشريط السفلي
        protected BarIcon AddToggle(string kind, string tip, int x)
        {
            BarIcon b = new BarIcon(kind);
            b.Location = new Point(x, 6);
            new ToolTip().SetToolTip(b, tip);
            _bar.Controls.Add(b);
            return b;
        }

        // لوح رمادي مدور مثل Creative
        protected Panel Group(Control parent, string title, int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.SetBounds(x, y, w, h);
            p.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (GraphicsPath gp = Skin.Round(r, 6))
                {
                    using (Brush b = new SolidBrush(Skin.Panel)) { e.Graphics.FillPath(b, gp); }
                    using (Pen pe = new Pen(Skin.PanelEdge)) { e.Graphics.DrawPath(pe, gp); }
                }
            };
            p.BackColor = Skin.Back;
            if (!string.IsNullOrEmpty(title))
            {
                Label l = new Label();
                l.Text = title;
                l.AutoSize = true;
                l.BackColor = Color.Transparent;
                l.Location = new Point(12, 8);
                p.Controls.Add(l);
            }
            parent.Controls.Add(p);
            return p;
        }

        protected Label Cap(Control parent, string text, int x, int y, int w, ContentAlignment al)
        {
            Label l = new Label();
            l.Text = text;
            l.TextAlign = al;
            l.SetBounds(x, y, w, 16);
            l.BackColor = Color.Transparent;
            parent.Controls.Add(l);
            return l;
        }

        protected CSlider VBar(Control parent, int x, int y, int h)
        {
            CSlider t = new CSlider();
            t.Orientation = Orientation.Vertical;
            t.Minimum = 0; t.Maximum = 100;
            t.SetBounds(x, y, 30, h);
            parent.Controls.Add(t);
            return t;
        }

        protected CSlider HBar(Control parent, int x, int y, int w, int min, int max)
        {
            CSlider t = new CSlider();
            t.Minimum = min; t.Maximum = max;
            t.SetBounds(x, y, w, 26);
            parent.Controls.Add(t);
            return t;
        }

        // علامة تبويب مرسومة (صف علوي)
        protected sealed class Tabs : Control
        {
            private readonly string[] _names;
            private int _sel;
            public event EventHandler Changed;
            public Tabs(string[] names)
            {
                _names = names;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Height = 32;
                Cursor = BlueCursor.Arrow;
            }
            public int Selected { get { return _sel; } set { if (value != _sel) { _sel = value; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); } } }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                int w = Width / _names.Length;
                int i = e.X / Math.Max(1, w);
                if (i >= _names.Length) i = _names.Length - 1;
                Selected = i;
                base.OnMouseDown(e);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Skin.Back);
                int w = Width / _names.Length;
                for (int i = 0; i < _names.Length; i++)
                {
                    Rectangle r = new Rectangle(i * w + 2, 2, w - 4, Height + 8);
                    bool sel = i == _sel;
                    using (GraphicsPath gp = Skin.Round(r, 8))
                    {
                        using (Brush b = new SolidBrush(sel ? Skin.Panel : Color.FromArgb(250, 250, 250))) { g.FillPath(b, gp); }
                        using (Pen p = new Pen(sel ? Color.FromArgb(120, 120, 126) : Color.FromArgb(190, 190, 195))) { g.DrawPath(p, gp); }
                    }
                    TextRenderer.DrawText(g, _names[i], Font, new Rectangle(i * w, 0, w, Height - 4), Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(90, 90, 96))) { e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { Reload(); BlueCursor.Apply(this); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!EqPanel.Closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
