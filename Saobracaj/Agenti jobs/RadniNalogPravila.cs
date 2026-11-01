using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    public sealed class RNRaspodela
    {
        public int IDNajave { get; set; }
        public int RadniNalog { get; set; }
        public int RB { get; set; } // 0 = nova stavka; isti RN/RB moze imati vise najava.
        public int BrojKola { get; set; }
        public bool Tovareno { get; set; }
        public decimal PlaniranaMasa { get; set; }
        internal DataRow NajavaSnapshot, StavkaSnapshot;
        internal string StatusSnapshot;
    }

    public static class RadniNalogPravila
    {
        public static int Trajanje(DateTime od, DateTime doVremena)
        {
            if (od < new DateTime(1900, 1, 2) || doVremena < od)
                throw new InvalidOperationException("Unesite ispravan stvarni polazak i dolazak (dolazak ne sme biti pre polaska).");
            return checked((int)Math.Round((doVremena - od).TotalMinutes, MidpointRounding.AwayFromZero));
        }

        public static void ProveriRaspodelu(IEnumerable<RNRaspodela> source, bool rezi, int osnovnaNajava)
        {
            var rows = source.ToList();
            if (osnovnaNajava <= 0 || !rows.Any(r => r.IDNajave == osnovnaNajava))
                throw new InvalidOperationException("Raspodela mora obuhvatiti najavu povezanu sa vozom u DispMain.");
            foreach (var r in rows)
            {
                if (r.IDNajave <= 0 || r.RadniNalog <= 0 || r.RB < 0 || r.PlaniranaMasa < 0 ||
                    (rezi ? r.BrojKola != 0 : r.BrojKola <= 0))
                    throw new InvalidOperationException("Proverite nalog, najavu, masu i broj kola. Rezi voznja ima 0 kola, ostale voznje vise od 0.");
            }
            if (rows.GroupBy(r => new { r.RadniNalog, r.IDNajave }).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Istu najavu na istom nalogu unesite jednom za ovu deonicu.");
            if (rows.GroupBy(r => r.RadniNalog).Any(g => g.Select(r => r.RB).Distinct().Count() > 1))
                throw new InvalidOperationException("Za jednu voznju i jedan nalog izaberite isti RB za sve najave.");
        }
    }
}
