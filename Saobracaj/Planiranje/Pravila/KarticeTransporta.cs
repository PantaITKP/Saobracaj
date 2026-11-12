using System;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal enum StatusOsobe { Odsutan, Rasporedjen, Slobodan }

    internal enum FilterTaskova { Svi, Otvoreni, Danas, Kasne }

    internal enum FilterJobsTaskova { Svi, BezTaskova, SaTaskovima }

    // Pravila brojeva na karticama ekrana Transport. Iste funkcije koriste i filteri na formama,
    // pa broj na kartici odgovara broju redova koji forma prikaže. SQL u KarticeRepo prati ista pravila.
    internal static class KarticeTransporta
    {
        // Uloge koje se raspoređuju na taskove (OP_027).
        internal static bool Broji(OsobaPlan o)
        {
            return o.Aktivan && o.UPlaniranju && (o.Masinovodja || o.Pregledac || o.Manevrista || o.Vozovodja || o.MasinovodjaManevra);
        }

        // Osoba se broji jednom: odsustvo, pa zadatak koji je u toku, pa slobodan dan iz smene, inače slobodna.
        // Odsustvo sa zadatkom u isto vreme je greška u planu; osoba se tada vodi kao odsutna.
        internal static StatusOsobe Status(OsobljeKontekst k, OsobaPlan o, DateTime sada)
        {
            DateTime danas = sada.Date;
            if (k.Odsustva.Any(x => x.DeSifra == o.DeSifra && x.Od.Date <= danas && x.Do.Date >= danas)) return StatusOsobe.Odsutan;
            if (k.Zadaci.Any(z => z.DeSifra == o.DeSifra && z.Aktivan && z.Od <= sada && z.Do > sada)) return StatusOsobe.Rasporedjen;
            if (k.Smene.Any(s => s.DeSifra == o.DeSifra && s.Datum.Date == danas && s.Vrsta == "SLOBODAN")) return StatusOsobe.Odsutan;
            return StatusOsobe.Slobodan;
        }

        internal static bool Otvoren(TaskOP t)
        {
            return t.Status == StatusTaska.Otvoren || t.Status == StatusTaska.Lansiran;
        }

        // Kraj taska je ETA, a bez nje ETD.
        private static DateTime? Kraj(TaskOP t)
        {
            return t.ETA ?? t.ETD;
        }

        internal static bool Danas(TaskOP t, DateTime danas)
        {
            return Otvoren(t) && t.ETD.HasValue && t.ETD.Value.Date <= danas && Kraj(t).Value.Date >= danas;
        }

        internal static bool Kasni(TaskOP t, DateTime danas)
        {
            return Otvoren(t) && Kraj(t).HasValue && Kraj(t).Value.Date < danas;
        }

        internal static bool Prolazi(TaskOP t, FilterTaskova f, DateTime danas)
        {
            switch (f)
            {
                case FilterTaskova.Otvoreni: return Otvoren(t);
                case FilterTaskova.Danas: return Danas(t, danas);
                case FilterTaskova.Kasne: return Kasni(t, danas);
                default: return true;
            }
        }

        // Zatvor (cela pruga ili svi koloseci) koji je u toku ili još važi danas; isto kao sloj zatvora na mapi.
        internal static bool ZatvorDanas(TelegramOP t, DateTime sada)
        {
            if (!Ruta.JeZatvor(t)) return false;
            int? nivo = ZatvoriMape.Nivo(t, sada);
            return nivo == ZatvoriMape.USada || nivo == ZatvoriMape.Danas;
        }
    }
}
