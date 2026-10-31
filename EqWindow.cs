using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Graphic Equalizer - نفس تخطيط Creative: 7 نطاقات + Level + Presets
    internal sealed class EqWindow : PanelBase
    {
        private static readonly string[] PresetNames = new string[] { "Custom", "Flat", "Rock", "Pop", "Jazz", "Classical", "Bass", "Vocal", "User" };
        private static readonly int[][] PresetVals = new int[][]
        {
            null,
            new int[] { 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 5, 3, -3, -4, -1, 3, 5 },
            new int[] { -1, 2, 4, 5, 3, -1, -2 },
            new int[] { 4, 2, -2, -2, 0, 3, 5 },
            new int[] { 5, 3, 0, 0, 0, -3, -4 },
            new int[] { 8, 6, 3, 0, 0, 0, 0 },
            new int[] { -2, -1, 2, 4, 4, 2, -1 },
            null
        };
        private static int[] _user = new int[] { 0, 0, 0, 0, 0, 0, 0 };

        private readonly CSlider[] _band = new CSlider[FxChain.Bands];
        private CSlider _lvl;
        private CheckBox _chk;
        private ComboBox _pre;
        private BarIcon _bBass, _bStereo, _bSurround;
        private float _lastBass = 60f;

        public EqWindow(FxChain fx) : base(fx, "eq", "Graphic Equalizer", 500, 262)
        {
            Panel strip = new Panel();
            strip.SetBounds(0, 0, 500, 26);
            strip.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.Clear(Color.FromArgb(246, 246, 248));
                using (Pen p = new Pen(Color.FromArgb(170, 170, 175))) { e.Graphics.DrawLine(p, 0, 25, 500, 25); }
                TextRenderer.DrawText(e.Graphics, "Equalizer", Font, new Rectangle(0, 0, 500, 26), Color.FromArgb(40, 40, 50),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            Body.Controls.Add(strip);

            Panel row = new Panel();
            row.SetBounds(0, 27, 500, 32);
            row.BackColor = Color.FromArgb(222, 222, 226);
            Body.Controls.Add(row);

            _chk = new CheckBox();
            _chk.Text = "Enable Equalizer";
            _chk.AutoSize = true;
            _chk.BackColor = Color.Transparent;
            _chk.Location = new Point(10, 8);
            _chk.CheckedChanged += delegate { if (!Loading) Fx.EqOn = _chk.Checked; };
            row.Controls.Add(_chk);

            Label pl = new Label();
            pl.Text = "Presets";
            pl.AutoSize = true;
            pl.BackColor = Color.Transparent;
            pl.Location = new Point(236, 9);
            row.Controls.Add(pl);

            _pre = new ComboBox();
            _pre.DropDownStyle = ComboBoxStyle.DropDownList;
            _pre.Items.AddRange(PresetNames);
            _pre.SetBounds(284, 5, 140, 21);
            _pre.SelectedIndexChanged += delegate
            {
                if (Loading) return;
                int i = _pre.SelectedIndex;
                int[] v = i == 8 ? _user : PresetVals[i];
                if (v == null) return;
                for (int b = 0; b < FxChain.Bands; b++) Fx.SetBand(b, v[b]);
                Fx.EqOn = true;
                LoadSliders();
            };
            row.Controls.Add(_pre);

            SkinButton save = new SkinButton();
            save.Text = "S";
            save.SetBounds(432, 4, 24, 24);
            save.Click += delegate
            {
                for (int b = 0; b < FxChain.Bands; b++) _user[b] = (int)Math.Round(Fx.GetBand(b));
                Loading = true; _pre.SelectedIndex = 8; Loading = false;
            };
            row.Controls.Add(save);

            SkinButton del = new SkinButton();
            del.Text = "X";
            del.SetBounds(462, 4, 24, 24);
            del.Click += delegate { Fx.ResetEq(); Reload(); };
            row.Controls.Add(del);

            // مقياس dB يسار
            Cap(Body, "12dB", 6, 76, 50, ContentAlignment.MiddleRight);
            Cap(Body, "0dB", 6, 146, 50, ContentAlignment.MiddleRight);
            Cap(Body, "-12dB", 6, 216, 50, ContentAlignment.MiddleRight);

            string[] names = new string[] { "125", "250", "500", "1k", "2k", "4k", "8k" };
            for (int i = 0; i < FxChain.Bands; i++)
            {
                int idx = i;
                Panel col = new Panel();
                col.SetBounds(60 + i * 46, 66, 38, 190);
                col.BackColor = Skin.Back;
                col.Paint += delegate(object s, PaintEventArgs e)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, col.Width - 1, col.Height - 1);
                    using (GraphicsPath gp = Skin.Round(r, 6))
                    {
                        using (Brush b = new SolidBrush(Skin.Panel)) { e.Graphics.FillPath(b, gp); }
                        using (Pen pe = new Pen(Skin.PanelEdge)) { e.Graphics.DrawPath(pe, gp); }
                    }
                };
                Body.Controls.Add(col);

                CSlider tb = new CSlider();
                tb.Orientation = Orientation.Vertical;
                tb.Minimum = -12; tb.Maximum = 12; tb.Ticks = true;
                tb.SetBounds(4, 6, 30, 158);
                tb.ValueChanged += delegate
                {
                    if (Loading) return;
                    Fx.SetBand(idx, -tb.Value);
                    Loading = true; _pre.SelectedIndex = 0; Loading = false;
                };
                col.Controls.Add(tb);
                _band[i] = tb;

                Label n = new Label();
                n.Text = names[i];
                n.TextAlign = ContentAlignment.MiddleCenter;
                n.Font = new Font("Tahoma", 8f, FontStyle.Bold);
                n.BackColor = Color.Transparent;
                n.SetBounds(0, 168, 38, 16);
                col.Controls.Add(n);
            }

            Cap(Body, "0dB", 364, 76, 44, ContentAlignment.MiddleRight);
            Cap(Body, "-24dB", 364, 216, 44, ContentAlignment.MiddleRight);
            _lvl = new CSlider();
            _lvl.Orientation = Orientation.Vertical;
            _lvl.Minimum = 0; _lvl.Maximum = 24;
            _lvl.SetBounds(412, 66, 30, 170);
            _lvl.ValueChanged += delegate { if (!Loading) Fx.LevelDb = -_lvl.Value; };
            Body.Controls.Add(_lvl);
            Cap(Body, "Level", 398, 238, 60, ContentAlignment.MiddleCenter);

            _bBass = AddToggle("bass", "Bass Boost", 236);
            _bBass.Click += delegate
            {
                if (Fx.BassOn && Fx.Bass > 0.5f) { _lastBass = Fx.Bass; Fx.Bass = 0f; }
                else { Fx.BassOn = true; Fx.Bass = _lastBass > 0.5f ? _lastBass : 60f; }
                Reload();
            };
            _bStereo = AddToggle("stereo", "Stereo", 270);
            _bStereo.Click += delegate
            {
                Fx.StereoOn = !Fx.StereoOn;
                if (Fx.StereoOn) Fx.Surround = 0f;
                Reload();
            };
            _bSurround = AddToggle("surround", "Surround", 304);
            _bSurround.Click += delegate
            {
                if (Fx.Surround > 0.5f) Fx.Surround = 0f;
                else { Fx.Surround = 60f; Fx.StereoOn = false; }
                Reload();
            };

            Reload();
        }

        protected override void DoDefault()
        {
            Fx.ResetEq();
            Fx.Bass = 0f;
            Fx.Surround = 0f;
            Fx.StereoOn = false;
        }

        private void LoadSliders()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                for (int i = 0; i < FxChain.Bands; i++) _band[i].Value = -(int)Math.Round(Fx.GetBand(i));
            }
            finally { Loading = was; }
        }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _chk.Checked = Fx.EqOn;
                for (int i = 0; i < FxChain.Bands; i++) _band[i].Value = -(int)Math.Round(Fx.GetBand(i));
                _lvl.Value = -(int)Math.Round(Fx.LevelDb);
                int match = 0;
                for (int p = 1; p < PresetVals.Length; p++)
                {
                    int[] v = p == 8 ? _user : PresetVals[p];
                    bool same = true;
                    for (int b = 0; b < FxChain.Bands; b++)
                        if ((int)Math.Round(Fx.GetBand(b)) != v[b]) { same = false; break; }
                    if (same) { match = p; break; }
                }
                _pre.SelectedIndex = match;
                _bBass.On = Fx.BassOn && Fx.Bass > 0.5f;
                _bStereo.On = Fx.StereoOn;
                _bSurround.On = Fx.Surround > 0.5f;
            }
            finally { Loading = was; }
        }
    }
}
