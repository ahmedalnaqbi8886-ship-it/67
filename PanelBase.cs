using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // اساس النوافذ الثلاث: شريط سفلي فيه Default + ازرار التنقل
    internal abstract class PanelBase : Form
    {
        protected readonly FxChain Fx;
        protected bool Loading;
        protected static readonly Color GrayPanel = Color.FromArgb(217, 217, 217);
        private readonly string _kind;
        private Panel _bar;

        protected PanelBase(FxChain fx, string kind, string title, int w, int h)
        {
            Fx = fx;
            _kind = kind;
            Text = title;
            Font = new Font("Tahoma", 8f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(240, 240, 243);
            ClientSize = new Size(w, h + 34);
            BuildBar(w, h);
        }

        protected abstract void DoDefault();
        public abstract void Reload();

        private void BuildBar(int w, int h)
        {
            _bar = new Panel();
            _bar.SetBounds(0, h, w, 34);
            _bar.Paint += delegate(object s, PaintEventArgs e)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(_bar.ClientRectangle, Color.FromArgb(165, 165, 170), Color.FromArgb(112, 112, 118), 90f))
                {
                    e.Graphics.FillRectangle(b, _bar.ClientRectangle);
                }
            };
            Controls.Add(_bar);

            Button def = new Button();
            def.Text = "Default";
            def.SetBounds(14, 6, 86, 22);
            def.Click += delegate { DoDefault(); Reload(); };
            _bar.Controls.Add(def);

            string[] kinds = new string[] { "eq", "speaker", "mixer" };
            string[] tips = new string[] { "Equalizer", "Speaker Settings", "Surround Mixer" };
            ToolTip tt = new ToolTip();
            for (int i = 0; i < kinds.Length; i++)
            {
                string k = kinds[i];
                NavButton nb = new NavButton(k, k == _kind);
                nb.Location = new Point(116 + i * 34, 5);
                nb.Click += delegate { EqPanel.OpenFrom(k, this); };
                tt.SetToolTip(nb, tips[i]);
                _bar.Controls.Add(nb);
            }
        }

        protected Panel Group(Control parent, string title, int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.SetBounds(x, y, w, h);
            p.BackColor = GrayPanel;
            if (!string.IsNullOrEmpty(title))
            {
                Label l = new Label();
                l.Text = title;
                l.AutoSize = true;
                l.Location = new Point(8, 6);
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

        protected TrackBar VBar(Control parent, int x, int y, int h)
        {
            TrackBar t = new TrackBar();
            t.Orientation = Orientation.Vertical;
            t.Minimum = 0; t.Maximum = 100; t.TickFrequency = 10;
            t.SetBounds(x, y, 45, h);
            parent.Controls.Add(t);
            return t;
        }

        protected TrackBar HBar(Control parent, int x, int y, int w, int min, int max)
        {
            TrackBar t = new TrackBar();
            t.Minimum = min; t.Maximum = max;
            t.TickFrequency = Math.Max(1, (max - min) / 10);
            t.SetBounds(x, y, w, 30);
            parent.Controls.Add(t);
            return t;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) Reload();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!EqPanel.Closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;   // نخفيها فقط (تنفتح من زر البرنامج مرة ثانية)
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
