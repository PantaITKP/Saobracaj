using System;
using System.Collections.Generic;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class TurnusPodaci
    {
        internal TimeSpan Pocetak;
        internal int TrajanjeMin;
        internal int DanaRada;
        internal int DanaOdmora;
        internal bool Nocna;
    }

    internal sealed class SmenaDana
    {
        internal DateTime Datum;
        internal string Vrsta;
        internal DateTime? Od;
        internal DateTime? Do;
    }

    // Pravilo 8.8: dan d je radni ako je (d - Od) mod (DanaRada + DanaOdmora) < DanaRada.
    internal static class Turnusi
    {
        internal static bool JeRadni(DateTime dan, DateTime pocetakCiklusa, int danaRada, int danaOdmora)
        {
            int ciklus = danaRada + danaOdmora;
            if (ciklus <= 0) return false;
            int razlika = (dan.Date - pocetakCiklusa.Date).Days;
            int ostatak = ((razlika % ciklus) + ciklus) % ciklus;
            return ostatak < danaRada;
        }

        internal static IEnumerable<SmenaDana> Smene(TurnusPodaci t, DateTime dodelaOd, DateTime? dodelaDo, DateTime periodOd, DateTime periodDo)
        {
            DateTime prvi = periodOd.Date > dodelaOd.Date ? periodOd.Date : dodelaOd.Date;
            DateTime poslednji = dodelaDo.HasValue && dodelaDo.Value.Date < periodDo.Date ? dodelaDo.Value.Date : periodDo.Date;
            for (DateTime d = prvi; d <= poslednji; d = d.AddDays(1))
            {
                if (!JeRadni(d, dodelaOd, t.DanaRada, t.DanaOdmora))
                {
                    yield return new SmenaDana { Datum = d, Vrsta = "SLOBODAN" };
                    continue;
                }
                DateTime od = d + t.Pocetak;
                yield return new SmenaDana { Datum = d, Vrsta = t.Nocna ? "NOCNA" : "DNEVNA", Od = od, Do = od.AddMinutes(t.TrajanjeMin) };
            }
        }
    }
}
