using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    internal static class IskoriscenjeInfrastruktura
    {
        private sealed class Deonica
        {
            internal string Grupa, Broj, Relacija, Lokomotive, Izvor, Nalog;
            internal DateTime? Polazak, Dolazak;
            internal DateTime DatumGrupe;
            internal long Red;
        }
        private static DateTime? Datum(DataRow r, string column)
        {
            if (!r.Table.Columns.Contains(column) || r.IsNull(column)) return null;
            DateTime value = Convert.ToDateTime(r[column]);
            return value < new DateTime(1900, 1, 2) ? (DateTime?)null : value;
        }
        private static string Tekst(DataRow r, string column)
        {
            return !r.Table.Columns.Contains(column) || r.IsNull(column) ? "" : Convert.ToString(r[column]).Trim();
        }
        private static string Relacija(DataRow r)
        {
            string od = Tekst(r, "StanicaOdNaziv"), kraj = Tekst(r, "StanicaDoNaziv");
            return od.Length == 0 && kraj.Length == 0 ? "" : od + " – " + kraj;
        }
        private static string Broj(DataRow r)
        {
            string actual = Tekst(r, "BrojVoznje"), original = Tekst(r, "BrojIzvorneTrase");
            return original.Length == 0 || actual == original ? actual : actual + " (" + original + ")";
        }
        internal static DataTable Slozi(DataTable pokretanja, DataTable vozovi, DataTable realizacije, DataTable istorija)
        {
            var sections = new List<Deonica>();
            var usedPrevozi = new HashSet<long>();
            foreach (var group in realizacije.Rows.Cast<DataRow>().GroupBy(r => Convert.ToInt64(r["PrevozID"])))
            {
                long id = group.Key;
                usedPrevozi.Add(id);
                // Samo izricito povezana realizacija sme deliti identitet izmedju vise RN.
                // Ne spajamo istorijske voznje samo zato sto imaju isti broj i datum.
                foreach (var same in group.GroupBy(r => new {
                    Broj = Broj(r),
                    Od = Tekst(r, "StanicaOd"),
                    Do = Tekst(r, "StanicaDo"),
                    Polazak = Datum(r, "Polazak"),
                    Dolazak = Datum(r, "Dolazak")
                }))
                {
                    var first = same.First();
                    sections.Add(new Deonica
                    {
                        Grupa = "V" + id,
                        Broj = same.Key.Broj,
                        Relacija = Relacija(first),
                        Polazak = same.Key.Polazak,
                        Dolazak = same.Key.Dolazak,
                        DatumGrupe = group.Select(r => Datum(r, "Polazak") ?? Datum(r, "Dolazak") ?? DateTime.MinValue).Min(),
                        Lokomotive = string.Join(", ", same.SelectMany(r => Tekst(r, "Lokomotive").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)).Select(x => x.Trim()).Distinct()),
                        Izvor = "Evidentirano iz DispMain",
                        Nalog = string.Join(", ", same.Select(r => Tekst(r, "IDRadnogNaloga") + "/" + Tekst(r, "RB")).Distinct()),
                        Red = Convert.ToInt64(first["ID"])
                    });
                }
            }
            var linkedStarts = new HashSet<long>();
            foreach (DataRow r in vozovi.Rows)
            {
                if (!r.IsNull("PokretanjeID")) linkedStarts.Add(Convert.ToInt64(r["PokretanjeID"]));
                long id = Convert.ToInt64(r["ID"]);
                if (usedPrevozi.Contains(id)) continue;
                var start = Datum(r, "StartedAtLocal");
                if (!start.HasValue) continue;
                string original = Tekst(r, "OriginalTrain"), actual = Tekst(r, "Train");
                sections.Add(new Deonica
                {
                    Grupa = "V" + id,
                    Broj = original.Length > 0 && original != actual ? actual + " (" + original + ")" : actual,
                    Relacija = Tekst(r, "From") + " – " + Tekst(r, "To"),
                    Polazak = start,
                    Dolazak = Datum(r, "CompletedAtLocal"),
                    DatumGrupe = start.Value,
                    Lokomotive = Tekst(r, "LocomotiveNumber"),
                    Izvor = "DispMain (jos nije povezan sa RN realizacijom)",
                    Nalog = "",
                    Red = id
                });
            }
            foreach (DataRow r in pokretanja.Rows)
            {
                long id = Convert.ToInt64(r["ID"]);
                if (linkedStarts.Contains(id)) continue;
                var start = Datum(r, "VremePokretanja");
                if (!start.HasValue) continue;
                sections.Add(new Deonica
                {
                    Grupa = "P" + id,
                    Broj = Tekst(r, "BrojTrase"),
                    Relacija = "",
                    Lokomotive = "",
                    Polazak = start,
                    DatumGrupe = start.Value,
                    Izvor = "Pokretanje bez sacuvane veze sa voznjom; relacija i dolazak nisu poznati",
                    Nalog = "",
                    Red = id
                });
            }
            foreach (DataRow r in istorija.Rows)
            {
                var start = Datum(r, "DatumPolaskaReal"); var end = Datum(r, "DatumDolaskaReal");
                if (!start.HasValue && !end.HasValue) continue;
                string rn = Tekst(r, "IDRadnogNaloga"), rb = Tekst(r, "RB");
                sections.Add(new Deonica
                {
                    Grupa = "RN" + rn + "/" + rb,
                    Broj = Tekst(r, "BrojVoza"),
                    Relacija = Relacija(r),
                    Lokomotive = Tekst(r, "Lokomotive"),
                    Polazak = start,
                    Dolazak = end,
                    DatumGrupe = start ?? end.Value,
                    Izvor = "Istorijski RN: nema veze sa identitetom voznje iz DispMain; ne grupisati automatski",
                    Nalog = rn + "/" + rb,
                    Red = Convert.ToInt64(r["RB"])
                });
            }
            var table = new DataTable();
            table.Columns.Add("RedniBroj", typeof(int)); table.Columns.Add("Datum", typeof(DateTime));
            table.Columns.Add("VremePolaska", typeof(DateTime)); table.Columns.Add("VremeDolaska", typeof(DateTime));
            table.Columns.Add("BrojVoza", typeof(string)); table.Columns.Add("Relacija", typeof(string)); table.Columns.Add("Lokomotive", typeof(string));
            table.Columns.Add("GrupaVoznje", typeof(string)); table.Columns.Add("IzvorPodatka", typeof(string)); table.Columns.Add("RadniNalozi", typeof(string));
            int index = 0;
            foreach (var group in sections.GroupBy(x => x.Grupa).OrderBy(g => g.Min(x => x.DatumGrupe)).ThenBy(g => g.First().Broj, StringComparer.Ordinal).ThenBy(g => g.Key, StringComparer.Ordinal))
            {
                index++;
                var date = group.Min(x => x.DatumGrupe).Date;
                foreach (var s in group.OrderBy(x => x.Polazak ?? x.Dolazak ?? DateTime.MinValue).ThenBy(x => x.Red))
                    table.Rows.Add(index, date, (object)s.Polazak ?? DBNull.Value, (object)s.Dolazak ?? DBNull.Value,
                        s.Broj, s.Relacija, s.Lokomotive, s.Grupa, s.Izvor, s.Nalog);
            }
            table.AcceptChanges(); return table;
        }
    }
}
