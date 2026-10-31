using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Surround Mixer: Basic (Master + Effects + Output meter) / Advanced (Front L/R + Subwoofer)
    internal sealed class MixerWindow : PanelBase
    {
        private TrackBar _vol, _bal, _trb, _cry, _sur, _fl, _fr, _sub;
        private Label _vVol, _vBal, _vTrb, _vCry, _vSur, _vFl, _vFr, _vSub;
        private MeterBar _mL, _mR;
        private readonly Timer _tm = new Timer();
        private float _sl, _sr;

        public MixerWindow(FxChain fx) : base(fx, "mixer", "Surround Mixer", 500, 300)
        {
            TabControl tc = new TabControl();
            tc.SetBounds(0, 0, 500, 300);
            Controls.Add(tc);
            TabPage basic = new TabPage("Basic");
            TabPage adv = new TabPage("Advanced");
            basic.BackColor = Color.FromArgb(240, 240, 243);
            adv.BackColor = Color.FromArgb(240, 240, 243);
            tc.TabPages.Add(basic);
            tc.TabPages.Add(adv);

            // ---- Basic ----
            Panel g1 = Group(basic, "Master Control", 4, 6, 236, 258);
            _vVol = Cap(g1, "100%", 20, 26, 56, ContentAlignment.MiddleCenter);
            _vol = VBar(g1, 26, 44, 150);
            Cap(g1, "Volume", 20, 180, 56, ContentAlignment.MiddleCenter);
            Cap(g1, "Balance", 104, 56, 110, ContentAlignment.MiddleCenter);
            _bal = HBar(g1, 94, 74, 130, -100, 100);
            Cap(g1, "L", 94, 106, 20, ContentAlignment.MiddleLeft);
            Cap(g1, "R", 204, 106, 20, ContentAlignment.MiddleRight);
            _vBal = Cap(g1, "Center", 104, 124, 110, ContentAlignment.MiddleCenter);

            Panel g2 = Group(basic, "Effects", 244, 6, 176, 258);
            _vTrb = Cap(g2, "0%", 6, 26, 50, ContentAlignment.MiddleCenter);
            _trb = VBar(g2, 8, 44, 150);
            Cap(g2, "Treble", 6, 180, 50, ContentAlignment.MiddleCenter);
            _vCry = Cap(g2, "0%", 58, 26, 50, ContentAlignment.MiddleCenter);
            _cry = VBar(g2, 60, 44, 150);
            Cap(g2, "Crystal", 58, 180, 50, ContentAlignment.MiddleCenter);
            _vSur = Cap(g2, "0%", 110, 26, 56, ContentAlignment.MiddleCenter);
            _sur = VBar(g2, 114, 44, 150);
            Cap(g2, "Surround", 110, 180, 56, ContentAlignment.MiddleCenter);

            Panel g3 = Group(basic, "Output", 424, 6, 68, 258);
            _mL = new MeterBar(); _mL.Location = new Point(14, 30); g3.Controls.Add(_mL);
            _mR = new MeterBar(); _mR.Location = new Point(38, 30); g3.Controls.Add(_mR);
            Cap(g3, "L", 10, 204, 20, ContentAlignment.MiddleCenter);
            Cap(g3, "R", 34, 204, 20, ContentAlignment.MiddleCenter);

            // ---- Advanced ----
            Cap(adv, "Speaker Volume Controls:", 6, 6, 250, ContentAlignment.MiddleLeft);
            Panel g4 = Group(adv, "", 4, 24, 330, 240);
            _vFl = Cap(g4, "100%", 50, 12, 56, ContentAlignment.MiddleCenter);
            _fl = VBar(g4, 56, 30, 150);
            Cap(g4, "Front Left", 50, 206, 56, ContentAlignment.MiddleCenter);
            _vFr = Cap(g4, "100%", 190, 12, 56, ContentAlignment.MiddleCenter);
            _fr = VBar(g4, 196, 30, 150);
            Cap(g4, "Front Right", 190, 206, 60, ContentAlignment.MiddleCenter);
            Panel g5 = Group(adv, "", 340, 24, 152, 240);
            _vSub = Cap(g5, "50%", 48, 12, 56, ContentAlignment.MiddleCenter);
            _sub = VBar(g5, 54, 30, 150);
            Cap(g5, "Subwoofer", 48, 206, 64, ContentAlignment.MiddleCenter);

            // ---- احداث ----
            _vol.ValueChanged += delegate { if (!Loading) { Fx.Master = 100 - _vol.Value; Reload(); } };
            _bal.ValueChanged += delegate { if (!Loading) { Fx.Balance = _bal.Value; Reload(); } };
            _trb.ValueChanged += delegate { if (!Loading) { Fx.Treble = 100 - _trb.Value; Reload(); } };
            _cry.ValueChanged += delegate { if (!Loading) { Fx.Crystal = 100 - _cry.Value; Reload(); } };
            _sur.ValueChanged += delegate { if (!Loading) { Fx.Surround = 100 - _sur.Value; Reload(); } };
            _fl.ValueChanged += delegate { if (!Loading) { Fx.VolL = 100 - _fl.Value; Reload(); } };
            _fr.ValueChanged += delegate { if (!Loading) { Fx.VolR = 100 - _fr.Value; Reload(); } };
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

        private static float Level(float peak)
        {
            if (peak <= 0.00001f) return 0f;
            float db = 20f * (float)Math.Log10(peak);
            float v = (db + 60f) / 60f;
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        protected override void DoDefault() { Fx.ResetMixer(); }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _vol.Value = 100 - (int)Math.Round(Fx.Master); _vVol.Text = (int)Math.Round(Fx.Master) + "%";
                int bal = (int)Math.Round(Fx.Balance);
                _bal.Value = bal;
                _vBal.Text = bal == 0 ? "Center" : (bal < 0 ? "Left " + (-bal) + "%" : "Right " + bal + "%");
                _trb.Value = 100 - (int)Math.Round(Fx.Treble); _vTrb.Text = (int)Math.Round(Fx.Treble) + "%";
                _cry.Value = 100 - (int)Math.Round(Fx.Crystal); _vCry.Text = (int)Math.Round(Fx.Crystal) + "%";
                _sur.Value = 100 - (int)Math.Round(Fx.Surround); _vSur.Text = (int)Math.Round(Fx.Surround) + "%";
                _fl.Value = 100 - (int)Math.Round(Fx.VolL); _vFl.Text = (int)Math.Round(Fx.VolL) + "%";
                _fr.Value = 100 - (int)Math.Round(Fx.VolR); _vFr.Text = (int)Math.Round(Fx.VolR) + "%";
                _sub.Value = 100 - (int)Math.Round(Fx.Sub); _vSub.Text = (int)Math.Round(Fx.Sub) + "%";
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
