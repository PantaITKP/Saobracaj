using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Jedna lokomotiva u ponudi za task.
    internal sealed class KandidatTaska
    {
        internal LokomotivaOP Lokomotiva;
        internal bool VucaOdgovara;
        internal string NapomenaVuce;
        internal int? StanicaID;
        internal string Lokacija;
        internal string IzvorLokacije;
        internal decimal? Km;
        internal int? DolazakMin;
        internal DateTime? Stize;
        internal int KasniMin;
        internal bool Zauzeta;
        internal string Zauzetost;
        internal string Sledeci;
    }

    // Elektrifikacija rute taska po deonicama mreže.
    internal sealed class ElektroRute
    {
        internal decimal KmElektro;
        internal decimal KmBezMreze;
        internal decimal KmNepoznato;
        internal bool Elektrificirana { get { return KmElektro > 0 && KmBezMreze == 0 && KmNepoznato == 0; } }
        internal string Tekst { get { return EkranTekst(); } }

        private string EkranTekst()
        {
            if (Elektrificirana) return "Ruta je elektrificirana: prednost imaju električne lokomotive.";
            if (KmBezMreze > 0) return "Ruta ima " + KmBezMreze.ToString("N1") + " km bez kontaktne mreže: prednost imaju dizel lokomotive.";
            return "Elektrifikacija rute nije poznata za " + KmNepoznato.ToString("N1") + " km: vuča ne određuje redosled.";
        }
    }

    // Ponuda lokomotiva za task: gde je lokomotiva u vreme polaska taska, koliko je daleko i kad stiže,
    // da li njena vuča odgovara ruti i da li je zauzeta drugim taskom ili poslom u vremenu taska.
    internal static class PonudaLokomotivaTaska
    {
        internal static ElektroRute Elektrifikacija(PlanKontekst k, TaskOP t)
        {
            var veze = new Dictionary<long, VezaMreze>();
            foreach (VezaMreze v in k.Veze)
            {
                veze[Kljuc(v.A, v.B)] = v;
                veze[Kljuc(v.B, v.A)] = v;
            }
            var e = new ElektroRute();
            for (int i = 1; i < t.Stanice.Count; i++)
            {
                decimal km = t.Stanice[i].Km - t.Stanice[i - 1].Km;
                VezaMreze v;
                bool? el = veze.TryGetValue(Kljuc(t.Stanice[i - 1].StanicaID, t.Stanice[i].StanicaID), out v) ? v.Elektrificirana : null;
                if (el == true) e.KmElektro += km;
                else if (el == false) e.KmBezMreze += km;
                else e.KmNepoznato += km;
            }
            return e;
        }

        private static long Kljuc(int a, int b)
        {
            return ((long)a << 32) | (uint)b;
        }

        private static string Period(DateTime? od, DateTime? doV)
        {
            return (od.HasValue ? od.Value.ToString("dd.MM. HH:mm") : "?") + " – " + (doV.HasValue ? doV.Value.ToString("dd.MM. HH:mm") : "?");
        }

        // Redosled: slobodne, pa vuča koja odgovara ruti, pa poznata lokacija, pa stiže na vreme, pa najkraći dolazak i najmanje km.
        internal static List<KandidatTaska> Izracunaj(PlanKontekst k, TaskOP t, IList<TaskOP> svi, IDictionary<int, GpsLokacija> gps, DateTime sada)
        {
            DateTime polazak = t.Od ?? sada;
            DateTime kraj = t.Do ?? polazak;
            ElektroRute el = Elektrifikacija(k, t);
            Dictionary<int, decimal> dist = k.Graf.Rastojanja(t.OdID);
            decimal brzina = k.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", polazak, t.OdID, null, null, 50);
            var posloviTaska = new HashSet<int>(t.Najave.Where(n => n.PosaoID.HasValue).Select(n => n.PosaoID.Value));
            List<TaskOP> drugi = svi.Where(x => x.ID != t.ID && x.Status != StatusTaska.Storniran).ToList();

            var lista = new List<KandidatTaska>();
            foreach (LokomotivaOP l in k.Lokomotive)
            {
                if (t.Lokomotive.Any(x => x.LokomotivaID == l.ID)) continue;
                var c = new KandidatTaska { Lokomotiva = l };

                // Vuča: elektrificirana ruta traži električnu, ruta sa delom bez mreže dizel.
                if (el.Elektrificirana) { c.VucaOdgovara = l.Vuca == "E"; c.NapomenaVuce = l.Vuca == "E" ? "" : "dizel na elektrificiranoj ruti"; }
                else if (el.KmBezMreze > 0) { c.VucaOdgovara = l.Vuca == "D"; c.NapomenaVuce = l.Vuca == "D" ? "" : "ruta ima " + el.KmBezMreze.ToString("N1") + " km bez mreže"; }
                else { c.VucaOdgovara = true; c.NapomenaVuce = ""; }

                // Lokacija u vreme polaska: kraj poslednjeg taska ili posla pre polaska, inače GPS, inače matična stanica.
                TaskOP preTask = drugi.Where(x => x.Lokomotive.Any(y => y.LokomotivaID == l.ID) && x.Do.HasValue && x.Do.Value <= polazak)
                    .OrderByDescending(x => x.Do.Value).FirstOrDefault();
                ZauzetostLok prePosao = k.Kalendar.Where(z => z.LokomotivaID == l.ID && z.Vrsta == "POSAO" && z.Do <= polazak && !(z.PosaoID.HasValue && posloviTaska.Contains(z.PosaoID.Value)))
                    .OrderByDescending(z => z.Do).FirstOrDefault();
                DateTime? slobodnaOd = null;
                if (preTask != null && (prePosao == null || preTask.Do.Value >= prePosao.Do))
                {
                    c.StanicaID = preTask.DoID; slobodnaOd = preTask.Do; c.IzvorLokacije = "Task " + preTask.ID;
                }
                else if (prePosao != null)
                {
                    c.StanicaID = prePosao.PredajaStanicaID; slobodnaOd = prePosao.Do; c.IzvorLokacije = "Posao (plan)";
                }
                GpsLokacija g;
                if (gps != null && gps.TryGetValue(l.ID, out g) && g.StanicaID.HasValue && (!slobodnaOd.HasValue || slobodnaOd.Value <= sada))
                {
                    c.StanicaID = g.StanicaID; slobodnaOd = null; c.IzvorLokacije = "GPS";
                }
                if (!c.StanicaID.HasValue && l.MaticnaStanicaID.HasValue) { c.StanicaID = l.MaticnaStanicaID; c.IzvorLokacije = "Matična stanica"; }
                if (!c.StanicaID.HasValue) c.IzvorLokacije = "Nepoznato";
                c.Lokacija = c.StanicaID.HasValue ? k.Naziv(c.StanicaID) : "Nepoznata";

                decimal km;
                if (c.StanicaID.HasValue && dist.TryGetValue(c.StanicaID.Value, out km))
                {
                    c.Km = km;
                    c.DolazakMin = brzina > 0 ? (int)Math.Ceiling(km / brzina * 60) : 0;
                    DateTime krece = slobodnaOd.HasValue && slobodnaOd.Value > sada ? slobodnaOd.Value : sada;
                    c.Stize = krece.AddMinutes(c.DolazakMin.Value);
                    c.KasniMin = c.Stize.Value > polazak ? (int)Math.Ceiling((c.Stize.Value - polazak).TotalMinutes) : 0;
                }

                // Zauzetost u vremenu taska: drugi task sa tom lokomotivom (ETD/ATD – ETA/ATA i trajanje), posao druge najave, servis, namenska.
                TaskOP preklapa = drugi.FirstOrDefault(x => x.Lokomotive.Any(y => y.LokomotivaID == l.ID) && x.Od.HasValue && x.Do.HasValue && x.Od.Value < kraj && x.Do.Value > polazak);
                ZauzetostLok kalendar = k.Kalendar.FirstOrDefault(z => z.LokomotivaID == l.ID && z.Od < kraj && z.Do > polazak && !(z.PosaoID.HasValue && posloviTaska.Contains(z.PosaoID.Value)));
                if (preklapa != null)
                {
                    c.Zauzeta = true;
                    c.Zauzetost = "Zauzeta: task " + preklapa.ID + " " + Period(preklapa.Od, preklapa.Do);
                }
                else if (kalendar != null)
                {
                    c.Zauzeta = true;
                    c.Zauzetost = "Zauzeta: " + PredlogLokomotiva.OpisPrepreke(kalendar).TrimEnd('.');
                }
                else
                {
                    int taskova = drugi.Count(x => x.Lokomotive.Any(y => y.LokomotivaID == l.ID));
                    c.Zauzetost = taskova == 0 ? "Slobodna" : "Slobodna (drugih taskova: " + taskova + ")";
                }
                TaskOP sledeci = drugi.Where(x => x.Lokomotive.Any(y => y.LokomotivaID == l.ID) && x.Od.HasValue && x.Od.Value >= kraj)
                    .OrderBy(x => x.Od.Value).FirstOrDefault();
                c.Sledeci = sledeci == null ? "" : "task " + sledeci.ID + " od " + sledeci.Od.Value.ToString("dd.MM. HH:mm") + " (" + k.Naziv(sledeci.OdID) + ")";
                lista.Add(c);
            }
            return lista
                .OrderBy(c => c.Zauzeta ? 1 : 0)
                .ThenBy(c => c.VucaOdgovara ? 0 : 1)
                .ThenBy(c => c.Km.HasValue ? 0 : 1)
                .ThenBy(c => c.KasniMin)
                .ThenBy(c => c.DolazakMin ?? int.MaxValue)
                .ThenBy(c => c.Km ?? decimal.MaxValue)
                .ThenBy(c => c.Lokomotiva.Broj).ToList();
        }
    }
}
