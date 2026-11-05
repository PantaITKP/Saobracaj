using System;
using System.Collections.Generic;
using System.Linq;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class ParametarRed
    {
        internal int? StanicaID;
        internal int? StanicaDoID;
        internal int? SerijaID;
        internal decimal Vrednost;
        internal DateTime VaziOd;
        internal DateTime? VaziDo;
    }

    // Vrednost se traži od najužeg ka najširem opsegu (odeljak 4.4 specifikacije).
    internal static class ParametriPravila
    {
        internal static decimal? Nadji(IEnumerable<ParametarRed> redovi, DateTime datum, int? stanica = null, int? stanicaDo = null, int? serija = null)
        {
            DateTime dan = datum.Date;
            List<ParametarRed> vazeci = redovi
                .Where(r => r.VaziOd.Date <= dan && (!r.VaziDo.HasValue || dan < r.VaziDo.Value.Date))
                .ToList();

            ParametarRed red = null;
            if (stanica.HasValue && stanicaDo.HasValue && serija.HasValue)
                red = Najnoviji(vazeci, stanica, stanicaDo, serija);
            if (red == null && stanica.HasValue && stanicaDo.HasValue)
                red = Najnoviji(vazeci, stanica, stanicaDo, null);
            if (red == null && stanica.HasValue)
                red = Najnoviji(vazeci, stanica, null, null);
            if (red == null)
                red = Najnoviji(vazeci, null, null, null);
            return red == null ? (decimal?)null : red.Vrednost;
        }

        private static ParametarRed Najnoviji(List<ParametarRed> redovi, int? stanica, int? stanicaDo, int? serija)
        {
            return redovi
                .Where(r => r.StanicaID == stanica && r.StanicaDoID == stanicaDo && r.SerijaID == serija)
                .OrderByDescending(r => r.VaziOd)
                .FirstOrDefault();
        }
    }
}
