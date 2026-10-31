using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Speaker Settings: Speaker Selection / Bass Management / Bass Boost
    internal sealed class SpeakerWindow : PanelBase
    {
        private static readonly string[] Modes = new string[] { "Headphone", "2 Speakers", "2.1 Speakers" };
        private ComboBox _cmb1, _cmb2;
        private TestPic _pic;
        private Label _testState;
        private TrackBar _bmPct, _bmCross, _bbLevel, _bbCut;
        private Label _vBmPct, _vBmCross, _vBbLevel, _vBbCut;
        private CheckBox _bmOn, _bbOn;
        private readonly Timer _tm = new Timer();

        public SpeakerWindow(FxChain fx) : base(fx, "speaker", "Speaker Settings", 500, 300)
        {
            TabControl tc = new TabControl();
            tc.SetBounds(0, 0, 500, 300);
            Controls.Add(tc);
            TabPage p1 = new TabPage("Speaker Selection");
            TabPage p2 = new TabPage("Bass Management");
            TabPage p3 = new TabPage("Bass Boost");
            p1.BackColor = Color.FromArgb(240, 240, 243);
            p2.BackColor = Color.FromArgb(240, 240, 243);
            p3.BackColor = Color.FromArgb(240, 240, 243);
            tc.TabPages.Add(p1); tc.TabPages.Add(p2); tc.TabPages.Add(p3);

            // ---- Speaker Selection ----
            Panel a = Group(p1, "Speaker/Headphone Selection", 4, 6, 236, 258);
            _cmb1 = Combo(a, 12, 30);
            Panel b = Group(p1, "Speaker Test", 246, 6, 246, 258);
            _pic = new TestPic();
            _pic.Location = new Point(24, 30);
            b.Controls.Add(_pic);
            Button bCh = new Button(); bCh.Text = "Channel"; bCh.SetBounds(24, 164, 92, 26); b.Controls.Add(bCh);
            Button bNo = new Button(); bNo.Text = "Noise"; bNo.SetBounds(128, 164, 92, 26); b.Controls.Add(bNo);
            _testState = Cap(b, "", 24, 200, 200, ContentAlignment.MiddleCenter);
            bCh.Click += delegate { Toggle(1); };
            bNo.Click += delegate { Toggle(2); };

            // ---- Bass Management ----
            Panel c = Group(p2, "", 4, 6, 488, 100);
            _cmb2 = Combo(c, 12, 12);
            Cap(c, "Bass level", 240, 14, 80, ContentAlignment.MiddleLeft);
            _vBmPct = Cap(c, "", 320, 14, 150, ContentAlignment.MiddleLeft);
            Cap(c, "0%", 12, 62, 40, ContentAlignment.MiddleLeft);
            _bmPct = HBar(c, 50, 54, 390, 0, 100);
            Cap(c, "100%", 444, 62, 40, ContentAlignment.MiddleLeft);
            Panel d = Group(p2, "", 4, 112, 488, 152);
            _bmOn = new CheckBox(); _bmOn.Text = "Bass Redirection"; _bmOn.AutoSize = true; _bmOn.Location = new Point(18, 16); d.Controls.Add(_bmOn);
            _vBmCross = Cap(d, "", 34, 52, 300, ContentAlignment.MiddleLeft);
            Cap(d, "10 Hz", 18, 92, 50, ContentAlignment.MiddleLeft);
            _bmCross = HBar(d, 70, 84, 360, 10, 200);
            Cap(d, "200 Hz", 436, 92, 50, ContentAlignment.MiddleLeft);

            // ---- Bass Boost ----
            Panel e2 = Group(p3, "", 4, 6, 488, 258);
            _bbOn = new CheckBox(); _bbOn.Text = "Enable Bass Boost"; _bbOn.AutoSize = true; _bbOn.Location = new Point(18, 18); e2.Controls.Add(_bbOn);
            _vBbLevel = Cap(e2, "", 18, 66, 300, ContentAlignment.MiddleLeft);
            Cap(e2, "0%", 18, 98, 40, ContentAlignment.MiddleLeft);
            _bbLevel = HBar(e2, 56, 90, 380, 0, 100);
            Cap(e2, "100%", 442, 98, 44, ContentAlignment.MiddleLeft);
            _vBbCut = Cap(e2, "", 18, 150, 300, ContentAlignment.MiddleLeft);
            Cap(e2, "10 Hz", 18, 182, 50, ContentAlignment.MiddleLeft);
            _bbCut = HBar(e2, 70, 174, 360, 10, 200);
            Cap(e2, "200 Hz", 436, 182, 50, ContentAlignment.MiddleLeft);

            // ---- احداث ----
            foreach (ComboBox cb in new ComboBox[] { _cmb1, _cmb2 })
            {
                ComboBox me = cb;
                me.SelectedIndexChanged += delegate { if (!Loading) { Fx.SpeakerMode = me.SelectedIndex; Reload(); } };
            }
            _bmPct.ValueChanged += delegate { if (!Loading) { Fx.BmPercent = _bmPct.Value; Reload(); } };
            _bmOn.CheckedChanged += delegate { if (!Loading) Fx.BmOn = _bmOn.Checked; };
            _bmCross.ValueChanged += delegate { if (!Loading) { Fx.BmCrossover = _bmCross.Value; Reload(); } };
            _bbOn.CheckedChanged += delegate { if (!Loading) Fx.BassOn = _bbOn.Checked; };
            _bbLevel.ValueChanged += delegate { if (!Loading) { Fx.Bass = _bbLevel.Value; Reload(); } };
            _bbCut.ValueChanged += delegate { if (!Loading) { Fx.BassCutoff = _bbCut.Value; Reload(); } };

            _tm.Interval = 400;
            _tm.Tick += delegate { if (Fx.TestMode == 0 && _testState.Text.Length > 0) _testState.Text = ""; };
            VisibleChanged += delegate { _tm.Enabled = Visible; };

            Reload();
        }

        private static ComboBox Combo(Control parent, int x, int y)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Items.AddRange(Modes);
            c.SetBounds(x, y, 200, 21);
            parent.Controls.Add(c);
            return c;
        }

        private void Toggle(int mode)
        {
            if (Fx.TestMode == mode) { Fx.StopTest(); _testState.Text = ""; }
            else { Fx.StartTest(mode); _testState.Text = mode == 1 ? "Playing: left / right" : "Playing: noise"; }
        }

        protected override void DoDefault() { Fx.ResetSpeaker(); Fx.StopTest(); }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _cmb1.SelectedIndex = Fx.SpeakerMode;
                _cmb2.SelectedIndex = Fx.SpeakerMode;
                _pic.Headphone = Fx.SpeakerMode == 0;
                int pct = (int)Math.Round(Fx.BmPercent);
                _bmPct.Value = pct; _vBmPct.Text = pct + "%";
                _bmOn.Checked = Fx.BmOn;
                int cr = (int)Math.Round(Fx.BmCrossover);
                _bmCross.Value = cr; _vBmCross.Text = "Crossover Frequency: " + cr + " Hz";
                _bbOn.Checked = Fx.BassOn;
                int lv = (int)Math.Round(Fx.Bass);
                _bbLevel.Value = lv; _vBbLevel.Text = "Level: " + lv + " %";
                int cu = (int)Math.Round(Fx.BassCutoff);
                _bbCut.Value = cu; _vBbCut.Text = "Cutoff Frequency: " + cu + " Hz";
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
