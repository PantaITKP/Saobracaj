using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>Jedna reč sa položajem na strani (0–1, y od vrha) i pouzdanošću (tekst iz PDF-a = 0.98, OCR = Tesseract).</summary>
    internal sealed class Rec
    {
        public string T;
        public double X0, Y0, X1, Y1;
        public double Pouzdanost;
        /// <summary>Veličina fonta u pt (tekst iz PDF-a); 0 = nepoznato (OCR).</summary>
        public double Velicina;
        /// <summary>Naziv fonta (tekst iz PDF-a); prazno za OCR.</summary>
        public string Font = "";
        public string Potpis => Font + "|" + (Math.Round(Velicina * 2) / 2).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        public double Cx => (X0 + X1) / 2;
        public double Cy => (Y0 + Y1) / 2;
        public double Visina => Y1 - Y0;
        public override string ToString() => T + "@" + Cx.ToString("0.000") + "," + Cy.ToString("0.000");
    }

    internal enum IzvorTeksta { Tekst, Ocr, Nema }

    /// <summary>Strana priloga: reči, izvor (tekst iz PDF-a ili OCR) i prosečna pouzdanost.</summary>
    internal sealed class Strana
    {
        public int Broj;
        public List<Rec> Reci = new List<Rec>();
        public IzvorTeksta Izvor;
        public double SrednjaPouzdanost => Reci.Count == 0 ? 0 : Reci.Average(r => r.Pouzdanost);
        public string Napomena = "";
        /// <summary>Crno-bela slika strane (PNG 1 bit, ~150 dpi) za linije okvira, polja za označavanje i prazne rubrike; null ako nije iscrtana.</summary>
        public byte[] Slika;
        /// <summary>Ista strana sa svetlijim pragom (samo za linije okvira).</summary>
        public byte[] SlikaLinije;

        private Raster raster;
        [Newtonsoft.Json.JsonIgnore]
        public Raster Raster => raster ?? (Slika == null ? null : raster = Raster.IzPng(Slika, SlikaLinije));
        public void PostaviRaster(Raster r) { raster = r; Slika = r?.UPng(); SlikaLinije = r?.UPngLinije(); }

        private string tekst;
        /// <summary>Tekst strane red po red (reči sortirane po redovima, pa po x).</summary>
        public string Tekst => tekst ?? (tekst = string.Join("\n", Raspored.Linije(Reci).Select(l => l.Tekst)));
    }

    /// <summary>Red teksta sastavljen od reči iste visine; razmaci između reči čuvaju se kao 1 ili 3+ razmaka (granica ćelije).</summary>
    internal sealed class Linija
    {
        public List<Rec> Reci = new List<Rec>();
        public double Y => Reci.Average(r => r.Cy);
        public string Tekst
        {
            get
            {
                var sb = new StringBuilder();
                Rec preth = null;
                var visina = Reci.Count == 0 ? 0.01 : Reci.Average(r => r.Visina);
                foreach (var r in Reci)
                {
                    if (preth != null) sb.Append(r.X0 - preth.X1 > visina * 1.6 ? "   " : " ");
                    sb.Append(r.T);
                    preth = r;
                }
                return sb.ToString();
            }
        }

        /// <summary>Ćelije reda: delovi razdvojeni širokim razmakom.</summary>
        public string[] Celije => Tekst.Split(new[] { "   " }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
    }

    internal static class Raspored
    {
        public static List<Linija> Linije(IEnumerable<Rec> reci)
        {
            var linije = new List<Linija>();
            foreach (var r in reci.OrderBy(r => r.Cy))
            {
                var visina = Math.Max(r.Visina, 0.004);
                var l = linije.LastOrDefault(x => Math.Abs(x.Y - r.Cy) < visina * 0.55);
                if (l == null) linije.Add(l = new Linija());
                l.Reci.Add(r);
            }
            foreach (var l in linije) l.Reci = l.Reci.OrderBy(r => r.X0).ToList();
            return linije.OrderBy(l => l.Y).ToList();
        }
    }
}
