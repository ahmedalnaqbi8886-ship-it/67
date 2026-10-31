using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Equalizer: ازرار السماعة + EQ 7 نطاقات + Treble / Crystalliser / Surround / Level
    internal sealed class EqWindow : PanelBase
    {
        private readonly TrackBar[] _band = new TrackBar[FxChain.Bands];
        private readonly Label[] _bandVal = new Label[FxChain.Bands];
        private TrackBar _tBass, _tTreb, _tCry, _tSur, _tLvl;
        private Label _vBass, _vTreb, _vCry, _vSur, _vLvl;
        private CheckBox _chk;
        private SpeakerButton _bBass, _bStereo, _bSurround;
        private float _lastBass = 60f;

        public EqWindow(FxChain fx) : base(fx, "eq", "Graphic Equalizer", 480, 470)
        {
            _bBass = new SpeakerButton(SpeakerButton.Kind.Bass, "Bass Boost");
            _bBass.Location = new Point(14, 10);
            _bBass.Click += delegate
            {
                if (Fx.Bass > 0.5f) { _lastBass = Fx.Bass; Fx.Bass = 0f; }
                else { Fx.BassOn = true; Fx.Bass = _lastBass > 0.5f ? _lastBass : 60f; }
                Reload();
            };
            Controls.Add(_bBass);

            _bStereo = new SpeakerButton(SpeakerButton.Kind.Stereo, "Stereo");
            _bStereo.Location = new Point(94, 10);
            _bStereo.Click += delegate { Fx.Surround = 0f; Reload(); };
            Controls.Add(_bStereo);

            _bSurround = new SpeakerButton(SpeakerButton.Kind.Surround, "Surround");
            _bSurround.Location = new Point(174, 10);
            _bSurround.Click += delegate { if (Fx.Surround < 0.5f) Fx.Surround = 50f; Reload(); };
            Controls.Add(_bSurround);

            _chk = new CheckBox();
            _chk.Text = "Enable Equalizer";
            _chk.Location = new Point(276, 14);
            _chk.AutoSize = true;
            _chk.CheckedChanged += delegate { if (!Loading) Fx.EqOn = _chk.Checked; };
            Controls.Add(_chk);

            string[] names = new string[] { "125", "250", "500", "1k", "2k", "4k", "8k" };
            for (int i = 0; i < FxChain.Bands; i++)
            {
                int idx = i;
                TrackBar tb = new TrackBar();
                tb.Orientation = Orientation.Vertical;
                tb.Minimum = -12; tb.Maximum = 12; tb.TickFrequency = 3;
                tb.Size = new Size(45, 150);
                tb.Location = new Point(24 + i * 62, 84);
                tb.ValueChanged += delegate
                {
                    if (Loading) return;
                    Fx.SetBand(idx, -tb.Value);   // فوق = +dB
                    _bandVal[idx].Text = Db(-tb.Value);
                };
                Controls.Add(tb);
                _band[i] = tb;

                Label n = new Label();
                n.Text = names[i];
                n.TextAlign = ContentAlignment.MiddleCenter;
                n.Font = new Font("Tahoma", 8f, FontStyle.Bold);
                n.SetBounds(24 + i * 62 - 4, 236, 53, 16);
                Controls.Add(n);

                Label v = new Label();
                v.TextAlign = ContentAlignment.MiddleCenter;
                v.SetBounds(24 + i * 62 - 4, 252, 53, 16);
                Controls.Add(v);
                _bandVal[i] = v;
            }

            int y = 284;
            _tBass = Row("Bass Boost", y, 0, 100, out _vBass);
            _tTreb = Row("Treble", y + 30, 0, 100, out _vTreb);
            _tCry = Row("Crystalliser", y + 60, 0, 100, out _vCry);
            _tSur = Row("Surround", y + 90, 0, 100, out _vSur);
            _tLvl = Row("Level (dB)", y + 120, -24, 0, out _vLvl);

            _tBass.ValueChanged += delegate { if (!Loading) { Fx.Bass = _tBass.Value; if (_tBass.Value > 0) { Fx.BassOn = true; _lastBass = _tBass.Value; } Reload(); } };
            _tTreb.ValueChanged += delegate { if (!Loading) { Fx.Treble = _tTreb.Value; _vTreb.Text = _tTreb.Value + "%"; } };
            _tCry.ValueChanged += delegate { if (!Loading) { Fx.Crystal = _tCry.Value; _vCry.Text = _tCry.Value + "%"; } };
            _tSur.ValueChanged += delegate { if (!Loading) { Fx.Surround = _tSur.Value; Reload(); } };
            _tLvl.ValueChanged += delegate { if (!Loading) { Fx.LevelDb = _tLvl.Value; _vLvl.Text = _tLvl.Value + " dB"; } };

            Reload();
        }

        private static string Db(float v)
        {
            return (v > 0 ? "+" : "") + ((int)Math.Round(v)) + " dB";
        }

        private TrackBar Row(string text, int y, int min, int max, out Label val)
        {
            Cap(this, text, 14, y + 6, 90, ContentAlignment.MiddleLeft);
            TrackBar tb = HBar(this, 108, y, 280, min, max);
            val = Cap(this, "", 396, y + 6, 70, ContentAlignment.MiddleLeft);
            return tb;
        }

        protected override void DoDefault() { Fx.ResetEq(); Fx.Bass = 0f; Fx.Surround = 0f; }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _chk.Checked = Fx.EqOn;
                for (int i = 0; i < FxChain.Bands; i++)
                {
                    float db = Fx.GetBand(i);
                    _band[i].Value = -(int)Math.Round(db);
                    _bandVal[i].Text = Db(db);
                }
                float bass = Fx.BassOn ? Fx.Bass : 0f;
                _tBass.Value = (int)Math.Round(Fx.Bass); _vBass.Text = _tBass.Value + "%";
                _tTreb.Value = (int)Math.Round(Fx.Treble); _vTreb.Text = _tTreb.Value + "%";
                _tCry.Value = (int)Math.Round(Fx.Crystal); _vCry.Text = _tCry.Value + "%";
                _tSur.Value = (int)Math.Round(Fx.Surround); _vSur.Text = _tSur.Value + "%";
                _tLvl.Value = (int)Math.Round(Fx.LevelDb); _vLvl.Text = _tLvl.Value + " dB";
                _bBass.On = bass > 0.5f;
                _bStereo.On = Fx.Surround <= 0.5f;
                _bSurround.On = Fx.Surround > 0.5f;
            }
            finally { Loading = was; }
        }
    }
}
