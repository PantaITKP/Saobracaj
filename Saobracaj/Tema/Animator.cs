using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Saobracaj.Tema
{
    public static class Animator
    {
        public static double IzlazKubni(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        // cubic-bezier(.2, .8, .2, 1)
        public static double Uvod(double t)
        {
            double a = 0, b = 1;
            for (int i = 0; i < 20; i++)
            {
                double u = (a + b) / 2;
                if (Bezier(u, .2, .2) < t) a = u; else b = u;
            }
            return Bezier((a + b) / 2, .8, 1);
        }

        private static double Bezier(double u, double p1, double p2)
        {
            double v = 1 - u;
            return 3 * v * v * u * p1 + 3 * v * u * u * p2 + u * u * u;
        }

        // Vraća timer da pozivalac može da prekine animaciju (npr. kad miš napusti dugme pre kraja).
        public static Timer Pokreni(Control vlasnik, int trajanjeMs, Func<double, double> kriva, Action<double> korak, Action kraj = null)
        {
            if (!Theme.AnimacijeUkljucene)
            {
                korak(1);
                kraj?.Invoke();
                return null;
            }
            var sat = Stopwatch.StartNew();
            var timer = new Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                if (vlasnik.IsDisposed)
                {
                    timer.Dispose();
                    return;
                }
                double t = Math.Min(1, sat.ElapsedMilliseconds / (double)trajanjeMs);
                korak(kriva(t));
                if (t >= 1)
                {
                    timer.Dispose();
                    kraj?.Invoke();
                }
            };
            timer.Start();
            return timer;
        }
    }
}
