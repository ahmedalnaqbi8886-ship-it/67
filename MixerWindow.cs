using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Surround Mixer: Basic / Advanced
    internal sealed class MixerWindow : PanelBase
    {
        private CSlider _vol, _fl, _fr, _bass, _trb, _cry, _sur, _fl2, _fr2, _sub;
        private Label _vVol, _vFl, _vFr, _vBass, _vTrb, _vCry, _vSur, _vFl2, _vFr2, _vSub;
        private SkinButton _mute;
        private MeterBar _mL, _mR;
        private readonly Timer _tm = new Timer();
        private float _sl, _sr;

        public MixerWindow(FxChain fx) : base(fx, "mixer", "Surround Mixer", 500, 290)
        {
            Tabs tabs = new Tabs(new string[] { "Basic", "Advanced" });
            tabs.SetBounds(0, 0, 500, 32);
            Body.Controls.Add(tabs);
            Panel basic = new Panel(); basic.SetBounds(0, 34, 500, 256); basic.BackColor = Skin.Back;
            Panel adv = new Panel(); adv.SetBounds(0, 34, 500, 256); adv.BackColor = Skin.Back; adv.Visible = false;
            Body.Controls.Add(basic); Body.Controls.Add(adv);
            tabs.Changed += delegate { basic.Visible = tabs.Selected == 0; adv.Visible = tabs.Selected == 1; };

            // ---- Basic ----
            Panel g1 = Group(basic, "Master Control", 6, 4, 172, 246);
            _vol = Col(g1, 16, "Volume", out _vVol);
            _fl = Col(g1, 70, "Left", out _vFl);
            _fr = Col(g1, 124, "Right", out _vFr);
            _mute = new SkinButton();
            _mute.Text = "Mute";
            _mute.SetBounds(10, 218, 52, 20);
            _mute.Click += delegate { Fx.Mute = !Fx.Mute; Reload(); };
            g1.Controls.Add(_mute);

            Panel g2 = Group(basic, "Effects", 184, 4, 236, 246);
            _bass = Col(g2, 14, "Bass", out _vBass);
            _trb = Col(g2, 72, "Treble", out _vTrb);
            _cry = Col(g2, 130, "Crystal", out _vCry);
            _sur = Col(g2, 186, "Surround", out _vSur);

            Panel g3 = Group(basic, "Output", 426, 4, 68, 246);
            _mL = new MeterBar(); _mL.Size = new Size(14, 150); _mL.Location = new Point(14, 40); g3.Controls.Add(_mL);
            _mR = new MeterBar(); _mR.Size = new Size(14, 150); _mR.Location = new Point(40, 40); g3.Controls.Add(_mR);
            Cap(g3, "L", 10, 196, 22, ContentAlignment.MiddleCenter);
            Cap(g3, "R", 36, 196, 22, ContentAlignment.MiddleCenter);

            // ---- Advanced ----
            Cap(adv, "Speaker Volume Controls:", 8, 2, 260, ContentAlignment.MiddleLeft);
            Panel g4 = Group(adv, "", 6, 22, 300, 230);
            _fl2 = Col(g4, 76, "FrontLeft", out _vFl2);
            _fr2 = Col(g4, 184, "FrontRight", out _vFr2);
            Panel g5 = Group(adv, "", 312, 22, 182, 230);
            _sub = Col(g5, 76, "Subwoofer", out _vSub);

            // ---- احداث ----
            _vol.ValueChanged += delegate { if (!Loading) { Fx.Master = 100 - _vol.Value; Reload(); } };
            _fl.ValueChanged += delegate { if (!Loading) { Fx.VolL = 100 - _fl.Value; Reload(); } };
            _fr.ValueChanged += delegate { if (!Loading) { Fx.VolR = 100 - _fr.Value; Reload(); } };
            _fl2.ValueChanged += delegate { if (!Loading) { Fx.VolL = 100 - _fl2.Value; Reload(); } };
            _fr2.ValueChanged += delegate { if (!Loading) { Fx.VolR = 100 - _fr2.Value; Reload(); } };
            _bass.ValueChanged += delegate { if (!Loading) { Fx.Bass = 100 - _bass.Value; if (Fx.Bass > 0.5f) Fx.BassOn = true; Reload(); } };
            _trb.ValueChanged += delegate { if (!Loading) { Fx.Treble = 100 - _trb.Value; Reload(); } };
            _cry.ValueChanged += delegate { if (!Loading) { Fx.Crystal = 100 - _cry.Value; Reload(); } };
            _sur.ValueChanged += delegate { if (!Loading) { Fx.Surround = 100 - _sur.Value; if (Fx.Surround > 0.5f) Fx.StereoOn = false; Reload(); } };
            _sub.ValueChanged += delegate { if (!Loading) { Fx.Sub = 100 - _sub.Value; Reload(); } };

            _tm.Interval = 70;
            _tm.Tick += delegate
            {
                float l, r;
                Fx.ReadPeaks(out l, out r);
                _sl = Math.Max(_sl * 0.85f, Level(l));
                _sr = Math.Max(_sr * 0.85f, Level(r));
                _mL.Value = _sl; _mR.Value = _sr;
            };
            VisibleChanged += delegate { _tm.Enabled = Visible; };

            Reload();
        }

        // عمود: قيمة فوق، منزلق، اسم تحت
        private CSlider Col(Control parent, int x, string caption, out Label val)
        {
            val = Cap(parent, "", x - 14, 30, 58, ContentAlignment.MiddleCenter);
            CSlider s = VBar(parent, x, 48, 150);
            Cap(parent, caption, x - 14, 200, 58, ContentAlignment.MiddleCenter);
            return s;
        }

        private static float Level(float peak)
        {
            if (peak <= 0.00001f) return 0f;
            float db = 20f * (float)Math.Log10(peak);
            float v = (db + 60f) / 60f;
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        protected override void DoDefault() { Fx.ResetMixer(); Fx.Bass = 0f; }

        private static void Set(CSlider s, Label l, float v)
        {
            int i = (int)Math.Round(v);
            s.Value = 100 - i;
            l.Text = i + "%";
        }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                Set(_vol, _vVol, Fx.Master);
                Set(_fl, _vFl, Fx.VolL); Set(_fr, _vFr, Fx.VolR);
                Set(_fl2, _vFl2, Fx.VolL); Set(_fr2, _vFr2, Fx.VolR);
                Set(_bass, _vBass, Fx.Bass);
                Set(_trb, _vTrb, Fx.Treble);
                Set(_cry, _vCry, Fx.Crystal);
                Set(_sur, _vSur, Fx.Surround);
                Set(_sub, _vSub, Fx.Sub);
                _mute.Checked = Fx.Mute;
                _mute.Invalidate();
            }
            finally { Loading = was; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tm.Dispose();
            base.Dispose(disposing);
        }
    }
}
