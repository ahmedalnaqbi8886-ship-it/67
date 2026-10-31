using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // نافذة التحكم: EQ 7 نطاقات + Bass / Treble / Crystalliser / Surround / Level
    public sealed class EqPanel : Form
    {
        private static EqPanel _inst;
        private static bool _closing;

        public static void ShowFor(Engine engine)
        {
            if (_inst != null && !_inst.IsDisposed) { _inst.Show(); return; }
            _closing = false;
            _inst = new EqPanel(engine.Fx);
            _inst.Show();
        }

        public static void Shutdown()
        {
            _closing = true;
            EqPanel p = _inst;
            _inst = null;
            if (p != null && !p.IsDisposed) p.Close();
        }

        private readonly FxChain _fx;
        private readonly TrackBar[] _band = new TrackBar[FxChain.Bands];
        private readonly Label[] _bandVal = new Label[FxChain.Bands];
        private TrackBar _tBass, _tTreb, _tCry, _tSur, _tLvl;
        private Label _vBass, _vTreb, _vCry, _vSur, _vLvl;
        private CheckBox _chk;
        private bool _loading;

        private EqPanel(FxChain fx)
        {
            _fx = fx;
            Text = "Graphic Equalizer";
            Font = new Font("Tahoma", 8f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(236, 238, 242);
            ClientSize = new Size(480, 400);

            _chk = new CheckBox();
            _chk.Text = "Enable Equalizer";
            _chk.Location = new Point(14, 10);
            _chk.AutoSize = true;
            _chk.CheckedChanged += delegate { if (!_loading) _fx.EqOn = _chk.Checked; };
            Controls.Add(_chk);

            Button def = new Button();
            def.Text = "Default";
            def.Location = new Point(380, 6);
            def.Size = new Size(86, 24);
            def.Click += delegate { _fx.Reset(); LoadFromFx(); };
            Controls.Add(def);

            string[] names = new string[] { "125", "250", "500", "1k", "2k", "4k", "8k" };
            for (int i = 0; i < FxChain.Bands; i++)
            {
                int idx = i;
                TrackBar tb = new TrackBar();
                tb.Orientation = Orientation.Vertical;
                tb.Minimum = -12; tb.Maximum = 12; tb.TickFrequency = 3;
                tb.Size = new Size(45, 150);
                tb.Location = new Point(24 + i * 62, 40);
                tb.ValueChanged += delegate
                {
                    if (_loading) return;
                    _fx.SetBand(idx, -tb.Value);   // اللي فوق = +dB
                    _bandVal[idx].Text = Db(-tb.Value);
                };
                Controls.Add(tb);
                _band[i] = tb;

                Label n = new Label();
                n.Text = names[i];
                n.TextAlign = ContentAlignment.MiddleCenter;
                n.Font = new Font("Tahoma", 8f, FontStyle.Bold);
                n.Location = new Point(24 + i * 62 - 4, 192);
                n.Size = new Size(53, 16);
                Controls.Add(n);

                Label v = new Label();
                v.TextAlign = ContentAlignment.MiddleCenter;
                v.Location = new Point(24 + i * 62 - 4, 208);
                v.Size = new Size(53, 16);
                Controls.Add(v);
                _bandVal[i] = v;
            }

            int y = 240;
            MakeRow("Bass Boost", y, 0, 100, out _tBass, out _vBass);
            MakeRow("Treble", y + 30, 0, 100, out _tTreb, out _vTreb);
            MakeRow("Crystalliser", y + 60, 0, 100, out _tCry, out _vCry);
            MakeRow("Surround", y + 90, 0, 100, out _tSur, out _vSur);
            MakeRow("Level (dB)", y + 120, -24, 0, out _tLvl, out _vLvl);

            _tBass.ValueChanged += delegate { if (!_loading) { _fx.Bass = _tBass.Value; _vBass.Text = _tBass.Value + "%"; } };
            _tTreb.ValueChanged += delegate { if (!_loading) { _fx.Treble = _tTreb.Value; _vTreb.Text = _tTreb.Value + "%"; } };
            _tCry.ValueChanged += delegate { if (!_loading) { _fx.Crystal = _tCry.Value; _vCry.Text = _tCry.Value + "%"; } };
            _tSur.ValueChanged += delegate { if (!_loading) { _fx.Surround = _tSur.Value; _vSur.Text = _tSur.Value + "%"; } };
            _tLvl.ValueChanged += delegate { if (!_loading) { _fx.LevelDb = _tLvl.Value; _vLvl.Text = _tLvl.Value + " dB"; } };

            LoadFromFx();
        }

        private static string Db(float v)
        {
            return (v > 0 ? "+" : "") + ((int)Math.Round(v)) + " dB";
        }

        private void MakeRow(string text, int y, int min, int max, out TrackBar tb, out Label val)
        {
            Label l = new Label();
            l.Text = text;
            l.Location = new Point(14, y + 6);
            l.Size = new Size(90, 18);
            Controls.Add(l);

            tb = new TrackBar();
            tb.Minimum = min; tb.Maximum = max; tb.TickFrequency = (max - min) / 10;
            tb.Location = new Point(108, y);
            tb.Size = new Size(280, 30);
            Controls.Add(tb);

            val = new Label();
            val.Location = new Point(396, y + 6);
            val.Size = new Size(70, 18);
            Controls.Add(val);
        }

        private void LoadFromFx()
        {
            _loading = true;
            try
            {
                _chk.Checked = _fx.EqOn;
                for (int i = 0; i < FxChain.Bands; i++)
                {
                    float db = _fx.GetBand(i);
                    _band[i].Value = -(int)Math.Round(db);
                    _bandVal[i].Text = Db(db);
                }
                _tBass.Value = (int)Math.Round(_fx.Bass); _vBass.Text = _tBass.Value + "%";
                _tTreb.Value = (int)Math.Round(_fx.Treble); _vTreb.Text = _tTreb.Value + "%";
                _tCry.Value = (int)Math.Round(_fx.Crystal); _vCry.Text = _tCry.Value + "%";
                _tSur.Value = (int)Math.Round(_fx.Surround); _vSur.Text = _tSur.Value + "%";
                _tLvl.Value = (int)Math.Round(_fx.LevelDb); _vLvl.Text = _tLvl.Value + " dB";
            }
            finally { _loading = false; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;                    // لا نغلقها، نصغّرها
                WindowState = FormWindowState.Minimized;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
