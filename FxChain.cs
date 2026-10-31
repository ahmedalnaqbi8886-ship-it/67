using System;
using System.Globalization;
using System.IO;

namespace EaxReverbEngine
{
    // EQ 7 نطاقات + Bass Boost + Treble + Crystalliser + Surround + Level
    // يشتغل على الصوت بعد الـ Reverb (قبل تغيير الـ sample rate).
    public sealed class FxChain
    {
        public const int Bands = 7;
        public static readonly float[] Freq = new float[] { 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f };

        private sealed class Bq
        {
            public float b0 = 1f, b1, b2, a1, a2;
            public float[] z1 = new float[2];
            public float[] z2 = new float[2];
        }

        private readonly object _lock = new object();
        private readonly float[] _band = new float[Bands];
        private readonly Bq[] _eq = new Bq[Bands];
        private readonly Bq _bq = new Bq();
        private readonly Bq _tq = new Bq();
        private readonly float[] _lp = new float[2];
        private float _bass, _treble, _crystal, _surround, _levelDb;
        private bool _eqOn = true;
        private float _fs = 48000f;
        private float _lpA;
        private float _gain = 1f;
        private bool _dirty = true;

        public FxChain()
        {
            for (int i = 0; i < Bands; i++) _eq[i] = new Bq();
        }

        private static float Clamp(float v, float lo, float hi)
        {
            if (float.IsNaN(v)) return lo;
            return v < lo ? lo : (v > hi ? hi : v);
        }

        public float GetBand(int i) { lock (_lock) { return _band[i]; } }
        public void SetBand(int i, float db)
        {
            if (i < 0 || i >= Bands) return;
            lock (_lock) { _band[i] = Clamp(db, -12f, 12f); _dirty = true; }
        }
        public float Bass { get { lock (_lock) { return _bass; } } set { lock (_lock) { _bass = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Treble { get { lock (_lock) { return _treble; } } set { lock (_lock) { _treble = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Crystal { get { lock (_lock) { return _crystal; } } set { lock (_lock) { _crystal = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float Surround { get { lock (_lock) { return _surround; } } set { lock (_lock) { _surround = Clamp(value, 0f, 100f); _dirty = true; } } }
        public float LevelDb { get { lock (_lock) { return _levelDb; } } set { lock (_lock) { _levelDb = Clamp(value, -24f, 0f); _dirty = true; } } }
        public bool EqOn { get { lock (_lock) { return _eqOn; } } set { lock (_lock) { _eqOn = value; } } }

        public void Reset()
        {
            lock (_lock)
            {
                for (int i = 0; i < Bands; i++) _band[i] = 0f;
                _bass = _treble = _crystal = _surround = 0f;
                _levelDb = 0f;
                _dirty = true;
            }
        }

        private static void Peak(Bq q, double f0, double dB, double fs)
        {
            double A = Math.Pow(10.0, dB / 40.0);
            double w = 2.0 * Math.PI * f0 / fs;
            double alpha = Math.Sin(w) / (2.0 * 1.2);
            double cw = Math.Cos(w);
            double a0 = 1.0 + alpha / A;
            q.b0 = (float)((1.0 + alpha * A) / a0);
            q.b1 = (float)((-2.0 * cw) / a0);
            q.b2 = (float)((1.0 - alpha * A) / a0);
            q.a1 = (float)((-2.0 * cw) / a0);
            q.a2 = (float)((1.0 - alpha / A) / a0);
        }

        private static void Shelf(Bq q, bool high, double f0, double dB, double fs)
        {
            double A = Math.Pow(10.0, dB / 40.0);
            double w = 2.0 * Math.PI * f0 / fs;
            double cw = Math.Cos(w);
            double alpha = Math.Sin(w) / 2.0 * 1.41421356;
            double sA = 2.0 * Math.Sqrt(A) * alpha;
            double b0, b1, b2, a0, a1, a2;
            if (!high)
            {
                b0 = A * ((A + 1) - (A - 1) * cw + sA);
                b1 = 2 * A * ((A - 1) - (A + 1) * cw);
                b2 = A * ((A + 1) - (A - 1) * cw - sA);
                a0 = (A + 1) + (A - 1) * cw + sA;
                a1 = -2 * ((A - 1) + (A + 1) * cw);
                a2 = (A + 1) + (A - 1) * cw - sA;
            }
            else
            {
                b0 = A * ((A + 1) + (A - 1) * cw + sA);
                b1 = -2 * A * ((A - 1) + (A + 1) * cw);
                b2 = A * ((A + 1) + (A - 1) * cw - sA);
                a0 = (A + 1) - (A - 1) * cw + sA;
                a1 = 2 * ((A - 1) - (A + 1) * cw);
                a2 = (A + 1) - (A - 1) * cw - sA;
            }
            q.b0 = (float)(b0 / a0); q.b1 = (float)(b1 / a0); q.b2 = (float)(b2 / a0);
            q.a1 = (float)(a1 / a0); q.a2 = (float)(a2 / a0);
        }

        private void Recalc()
        {
            for (int i = 0; i < Bands; i++) Peak(_eq[i], Freq[i], _band[i], _fs);
            Shelf(_bq, false, 100.0, _bass * 0.14, _fs);
            Shelf(_tq, true, 6500.0, _treble * 0.12, _fs);
            _lpA = (float)Math.Exp(-2.0 * Math.PI * 3000.0 / _fs);
            _gain = (float)Math.Pow(10.0, _levelDb / 20.0);
            _dirty = false;
        }

        private static void Run(Bq q, float[] x, int ch, int n)
        {
            float z1 = q.z1[ch], z2 = q.z2[ch];
            for (int i = 0; i < n; i++)
            {
                float s = x[i];
                float y = q.b0 * s + z1;
                z1 = q.b1 * s - q.a1 * y + z2;
                z2 = q.b2 * s - q.a2 * y;
                x[i] = y;
            }
            q.z1[ch] = z1; q.z2[ch] = z2;
        }

        public void Process(float[] l, float[] r, int n, int rate)
        {
            lock (_lock)
            {
                if (rate > 0 && Math.Abs(rate - _fs) > 0.5f) { _fs = rate; _dirty = true; }
                if (_dirty) Recalc();

                if (_bass > 0.5f) { Run(_bq, l, 0, n); Run(_bq, r, 1, n); }
                if (_treble > 0.5f) { Run(_tq, l, 0, n); Run(_tq, r, 1, n); }
                if (_eqOn)
                {
                    for (int b = 0; b < Bands; b++)
                    {
                        if (Math.Abs(_band[b]) < 0.01f) continue;
                        Run(_eq[b], l, 0, n); Run(_eq[b], r, 1, n);
                    }
                }
                if (_crystal > 0.5f)
                {
                    float amt = _crystal * 0.01f * 0.6f;
                    for (int i = 0; i < n; i++)
                    {
                        float sl = l[i], sr = r[i];
                        _lp[0] = (1f - _lpA) * sl + _lpA * _lp[0];
                        _lp[1] = (1f - _lpA) * sr + _lpA * _lp[1];
                        l[i] = sl + amt * (float)Math.Tanh((sl - _lp[0]) * 4f);
                        r[i] = sr + amt * (float)Math.Tanh((sr - _lp[1]) * 4f);
                    }
                }
                if (_surround > 0.5f)
                {
                    float k = 1f + _surround * 0.01f * 1.5f;
                    for (int i = 0; i < n; i++)
                    {
                        float m = 0.5f * (l[i] + r[i]);
                        float s = 0.5f * (l[i] - r[i]) * k;
                        l[i] = m + s; r[i] = m - s;
                    }
                }
                if (Math.Abs(_gain - 1f) > 0.0001f)
                {
                    for (int i = 0; i < n; i++) { l[i] *= _gain; r[i] *= _gain; }
                }
            }
        }

        // ---------- حفظ / تحميل (fx.ini جنب engine.ini) ----------
        private static string F(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        public void Save(string dir)
        {
            lock (_lock)
            {
                string[] lines = new string[Bands + 6];
                for (int i = 0; i < Bands; i++) lines[i] = "Band" + i + "=" + F(_band[i]);
                lines[Bands] = "EqOn=" + (_eqOn ? "1" : "0");
                lines[Bands + 1] = "Bass=" + F(_bass);
                lines[Bands + 2] = "Treble=" + F(_treble);
                lines[Bands + 3] = "Crystal=" + F(_crystal);
                lines[Bands + 4] = "Surround=" + F(_surround);
                lines[Bands + 5] = "Level=" + F(_levelDb);
                File.WriteAllLines(Path.Combine(dir, "fx.ini"), lines);
            }
        }

        public void Load(string dir)
        {
            string path = Path.Combine(dir, "fx.ini");
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadAllLines(path))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim();
                float v;
                if (!float.TryParse(line.Substring(eq + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) continue;
                if (k.StartsWith("Band"))
                {
                    int i;
                    if (int.TryParse(k.Substring(4), out i)) SetBand(i, v);
                }
                else if (k == "EqOn") EqOn = v != 0f;
                else if (k == "Bass") Bass = v;
                else if (k == "Treble") Treble = v;
                else if (k == "Crystal") Crystal = v;
                else if (k == "Surround") Surround = v;
                else if (k == "Level") LevelDb = v;
            }
        }
    }
}
