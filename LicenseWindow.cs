using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // بوابة الاشتراك: تظهر نافذة التفعيل اذا ما فيه مفتاح صالح
    internal static class LicenseGate
    {
        private static System.Threading.Timer _timer;

        public static bool Ensure()
        {
            LicState s = License.Check();
            Log.Write("license: " + License.Describe(s) + " hwid=" + License.Hwid());
            if (s == LicState.Ok) return true;
            using (LicenseWindow w = new LicenseWindow(s))
            {
                w.ShowDialog();
                return w.Activated;
            }
        }

        // يفحص كل دقيقة؛ اذا انتهى الاشتراك يوقف المحرك
        public static void Watch(Action onExpired)
        {
            _timer = new System.Threading.Timer(delegate(object o)
            {
                try
                {
                    LicState s = License.Check();
                    if (s != LicState.Ok)
                    {
                        Log.Write("license: " + License.Describe(s) + " - stopping engine");
                        _timer.Dispose();
                        onExpired();
                    }
                }
                catch (Exception) { }
            }, null, 60000, 60000);
        }
    }

    internal sealed class LicenseWindow : Form
    {
        public bool Activated;
        private readonly TextBox _key = new TextBox();
        private readonly Label _status = new Label();

        public LicenseWindow(LicState initial)
        {
            Text = "Activation";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(480, 300);
            Font = new Font("Tahoma", 8.25f);
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;

            Label title = new Label();
            title.Text = "Activate your subscription";
            title.Font = new Font("Tahoma", 11f, FontStyle.Bold);
            title.SetBounds(16, 14, 440, 22);
            Controls.Add(title);

            Label plans = new Label();
            plans.Text = "Plans: Weekly  |  Monthly  |  Lifetime  (bound to this device)";
            plans.ForeColor = Color.FromArgb(90, 90, 90);
            plans.SetBounds(16, 40, 450, 16);
            Controls.Add(plans);

            Label h = new Label();
            h.Text = "Your device ID (HWID) - send it to get a key:";
            h.SetBounds(16, 68, 450, 16);
            Controls.Add(h);

            TextBox hw = new TextBox();
            hw.ReadOnly = true;
            hw.Text = License.HwidPretty();
            hw.Font = new Font("Consolas", 11f, FontStyle.Bold);
            hw.SetBounds(16, 88, 350, 26);
            Controls.Add(hw);

            SkinButton copy = new SkinButton();
            copy.Text = "Copy";
            copy.SetBounds(376, 88, 88, 26);
            copy.Click += delegate
            {
                try { Clipboard.SetText(hw.Text); _status.ForeColor = Color.FromArgb(0, 120, 0); _status.Text = "HWID copied."; } catch (Exception) { }
            };
            Controls.Add(copy);

            Label k = new Label();
            k.Text = "License key:";
            k.SetBounds(16, 128, 300, 16);
            Controls.Add(k);

            _key.Multiline = true;
            _key.SetBounds(16, 148, 448, 62);
            _key.Font = new Font("Consolas", 9f);
            _key.ScrollBars = ScrollBars.Vertical;
            Controls.Add(_key);

            _status.SetBounds(16, 218, 448, 18);
            _status.ForeColor = Color.FromArgb(190, 0, 0);
            _status.Text = initial == LicState.NoKey ? "" : License.Describe(initial);
            Controls.Add(_status);

            SkinButton act = new SkinButton();
            act.Text = "Activate";
            act.SetBounds(262, 252, 100, 28);
            act.Click += delegate { Activate(); };
            Controls.Add(act);

            SkinButton exit = new SkinButton();
            exit.Text = "Close";
            exit.SetBounds(370, 252, 94, 28);
            exit.Click += delegate { Close(); };
            Controls.Add(exit);

            string old = License.Load();
            if (!string.IsNullOrEmpty(old)) _key.Text = old;
        }

        private void Activate()
        {
            string key = _key.Text;
            LicState s = License.Validate(key);
            if (s == LicState.Ok)
            {
                License.Store(key);
                Activated = true;
                _status.ForeColor = Color.FromArgb(0, 120, 0);
                _status.Text = License.Describe(s);
                Close();
            }
            else
            {
                _status.ForeColor = Color.FromArgb(190, 0, 0);
                _status.Text = License.Describe(s);
            }
        }
    }
}
