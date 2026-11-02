using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Saobracaj.Tema
{
    public enum Ikona
    {
        Korisnik, Brava, Oko, StrelicaDesno, Dole, Levo, Desno, Prva, Poslednja, Osvezi, Cekiraj, Plus, Kanta,
        Filter, Lista, Dokument, Mejl, Stampa, Pretraga, Preuzmi, Sat, Zatvori, Minus, Strelice,
        Knjiga, Klipbord, Voz, Alat, Aktovka, Stit, Kalkulator, Tabla
    }

    // Linijske ikone iz dizajn predloga, crtane u mreži 24 x 24.
    public static class Ikone
    {
        public static Bitmap Slika(Ikona ikona, int velicina, Color boja, float debljina = 1.9f)
        {
            var bmp = new Bitmap(velicina, velicina);
            using (var g = Graphics.FromImage(bmp))
                Crtaj(g, ikona, new RectangleF(0, 0, velicina, velicina), boja, debljina);
            return bmp;
        }

        public static void Crtaj(Graphics g, Ikona ikona, RectangleF okvir, Color boja, float debljina = 1.9f)
        {
            GraphicsState stanje = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(okvir.X, okvir.Y);
            g.ScaleTransform(okvir.Width / 24f, okvir.Height / 24f);
            using (var p = new Pen(boja, debljina) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                Oblik(g, p, ikona);
            g.Restore(stanje);
        }

        private static void Linija(Graphics g, Pen p, params float[] xy)
        {
            var tacke = new PointF[xy.Length / 2];
            for (int i = 0; i < tacke.Length; i++)
                tacke[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            g.DrawLines(p, tacke);
        }

        private static void Oblik(Graphics g, Pen p, Ikona ikona)
        {
            switch (ikona)
            {
                case Ikona.Korisnik:
                    g.DrawEllipse(p, 8, 4, 8, 8);
                    g.DrawArc(p, 4, 13, 16, 16, 180, 180);
                    break;
                case Ikona.Brava:
                    g.DrawRectangle(p, 6, 11, 12, 9);
                    Linija(g, p, 8, 11, 8, 8);
                    g.DrawArc(p, 8, 4, 8, 8, 180, 180);
                    Linija(g, p, 16, 8, 16, 11);
                    break;
                case Ikona.Oko:
                    g.DrawBezier(p, 2, 12, 2, 12, 6, 5, 12, 5);
                    g.DrawBezier(p, 12, 5, 18, 5, 22, 12, 22, 12);
                    g.DrawBezier(p, 22, 12, 22, 12, 18, 19, 12, 19);
                    g.DrawBezier(p, 12, 19, 6, 19, 2, 12, 2, 12);
                    g.DrawEllipse(p, 9, 9, 6, 6);
                    break;
                case Ikona.StrelicaDesno:
                    Linija(g, p, 5, 12, 19, 12);
                    Linija(g, p, 13, 6, 19, 12, 13, 18);
                    break;
                case Ikona.Strelice:
                    Linija(g, p, 5, 6, 12, 13, 5, 20);
                    Linija(g, p, 13, 6, 20, 13, 13, 20);
                    break;
                case Ikona.Dole:
                    Linija(g, p, 6, 9, 12, 15, 18, 9);
                    break;
                case Ikona.Levo:
                    Linija(g, p, 15, 6, 9, 12, 15, 18);
                    break;
                case Ikona.Desno:
                    Linija(g, p, 9, 6, 15, 12, 9, 18);
                    break;
                case Ikona.Prva:
                    Linija(g, p, 18, 6, 12, 12, 18, 18);
                    Linija(g, p, 7, 6, 7, 18);
                    break;
                case Ikona.Poslednja:
                    Linija(g, p, 6, 6, 12, 12, 6, 18);
                    Linija(g, p, 17, 6, 17, 18);
                    break;
                case Ikona.Osvezi:
                    g.DrawArc(p, 4, 4, 16, 16, 0, 313.5f);
                    Linija(g, p, 20, 4, 20, 9, 15, 9);
                    break;
                case Ikona.Cekiraj:
                    Linija(g, p, 5, 12, 10, 17, 19, 7);
                    break;
                case Ikona.Plus:
                    Linija(g, p, 12, 5, 12, 19);
                    Linija(g, p, 5, 12, 19, 12);
                    break;
                case Ikona.Kanta:
                    Linija(g, p, 4, 7, 20, 7);
                    Linija(g, p, 9, 7, 9, 4, 15, 4, 15, 7);
                    Linija(g, p, 6, 7, 7, 20, 17, 20, 18, 7);
                    break;
                case Ikona.Filter:
                    g.DrawPolygon(p, new[] { new PointF(4, 5), new PointF(20, 5), new PointF(14, 12), new PointF(14, 18), new PointF(10, 20), new PointF(10, 12) });
                    break;
                case Ikona.Lista:
                    Linija(g, p, 8, 6, 20, 6);
                    Linija(g, p, 8, 12, 20, 12);
                    Linija(g, p, 8, 18, 20, 18);
                    g.DrawEllipse(p, 3.6f, 5.6f, .8f, .8f);
                    g.DrawEllipse(p, 3.6f, 11.6f, .8f, .8f);
                    g.DrawEllipse(p, 3.6f, 17.6f, .8f, .8f);
                    break;
                case Ikona.Dokument:
                    g.DrawPolygon(p, new[] { new PointF(7, 3), new PointF(14, 3), new PointF(19, 8), new PointF(19, 21), new PointF(7, 21) });
                    Linija(g, p, 14, 3, 14, 8, 19, 8);
                    break;
                case Ikona.Mejl:
                    g.DrawRectangle(p, 3, 6, 18, 12);
                    Linija(g, p, 3, 7, 12, 14, 21, 7);
                    break;
                case Ikona.Stampa:
                    Linija(g, p, 7, 9, 7, 3, 17, 3, 17, 9);
                    Linija(g, p, 7, 17, 4, 17, 4, 10, 20, 10, 20, 17, 17, 17);
                    g.DrawRectangle(p, 7, 14, 10, 7);
                    break;
                case Ikona.Pretraga:
                    g.DrawEllipse(p, 4, 4, 14, 14);
                    Linija(g, p, 20, 20, 16, 16);
                    break;
                case Ikona.Preuzmi:
                    Linija(g, p, 12, 4, 12, 15);
                    Linija(g, p, 7, 10, 12, 15, 17, 10);
                    Linija(g, p, 5, 20, 19, 20);
                    break;
                case Ikona.Sat:
                    g.DrawEllipse(p, 3, 3, 18, 18);
                    Linija(g, p, 12, 7, 12, 12, 15, 14);
                    break;
                case Ikona.Zatvori:
                    Linija(g, p, 6, 6, 18, 18);
                    Linija(g, p, 18, 6, 6, 18);
                    break;
                case Ikona.Minus:
                    Linija(g, p, 6, 12, 18, 12);
                    break;
                case Ikona.Knjiga:
                    Linija(g, p, 6, 3, 19, 3, 19, 19, 6, 19);
                    g.DrawArc(p, 4, 3, 4, 4, 180, 90);
                    Linija(g, p, 4, 5, 4, 21);
                    g.DrawArc(p, 4, 19, 4, 4, 180, 90);
                    Linija(g, p, 8, 7, 15, 7);
                    break;
                case Ikona.Klipbord:
                    g.DrawRectangle(p, 9, 4, 6, 3);
                    Linija(g, p, 7, 5, 5, 5, 5, 21, 19, 21, 19, 5, 17, 5);
                    Linija(g, p, 9, 12, 15, 12);
                    Linija(g, p, 9, 16, 13, 16);
                    break;
                case Ikona.Voz:
                    g.DrawRectangle(p, 5, 3, 14, 14);
                    Linija(g, p, 5, 11, 19, 11);
                    Linija(g, p, 8, 21, 10, 17);
                    Linija(g, p, 16, 21, 14, 17);
                    break;
                case Ikona.Alat:
                    Linija(g, p, 4, 20, 11, 13);
                    g.DrawArc(p, 10, 4, 10, 10, 135, 270);
                    break;
                case Ikona.Aktovka:
                    g.DrawRectangle(p, 4, 8, 16, 11);
                    Linija(g, p, 9, 8, 9, 5, 15, 5, 15, 8);
                    Linija(g, p, 4, 13, 20, 13);
                    break;
                case Ikona.Stit:
                    Linija(g, p, 4, 12, 4, 6, 12, 3, 20, 6, 20, 12);
                    g.DrawBezier(p, 20, 12, 20, 17, 16.5f, 20, 12, 21);
                    g.DrawBezier(p, 12, 21, 7.5f, 20, 4, 17, 4, 12);
                    Linija(g, p, 9, 12, 11, 14, 15, 10);
                    break;
                case Ikona.Kalkulator:
                    g.DrawRectangle(p, 6, 3, 12, 18);
                    Linija(g, p, 9, 7, 15, 7);
                    Linija(g, p, 9, 12, 10, 12);
                    Linija(g, p, 13, 12, 14, 12);
                    Linija(g, p, 9, 16, 10, 16);
                    Linija(g, p, 13, 16, 14, 16);
                    break;
                case Ikona.Tabla:
                    g.DrawRectangle(p, 4, 5, 16, 14);
                    Linija(g, p, 4, 10, 20, 10);
                    Linija(g, p, 10, 10, 10, 19);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ikona));
            }
        }
    }
}
