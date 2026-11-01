using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    internal sealed class CenaPokretanja
    {
        internal long ID;
        internal decimal? StaraCena;
    }

    internal sealed class IskoriscenjeZbir
    {
        internal DataTable Tabela;
        internal readonly Dictionary<DataRow, List<CenaPokretanja>> Grupe =
            new Dictionary<DataRow, List<CenaPokretanja>>();
    }

    internal static class IskoriscenjeTrasaObracun
    {
        internal static IskoriscenjeZbir Grupisi(DataTable detalji)
        {
            var result = new IskoriscenjeZbir { Tabela = new DataTable() };
            result.Tabela.Columns.Add("BrojTrase", typeof(string));
            result.Tabela.Columns.Add("adHoc", typeof(bool));
            result.Tabela.Columns.Add("Ponavljanje", typeof(int));
            result.Tabela.Columns.Add("Cena", typeof(decimal));
            result.Tabela.Columns.Add("CenaUkupno", typeof(decimal), "Ponavljanje * Cena");

            // A changed unit price must form a separate group, otherwise count * price is misleading.
            var groups = detalji.Rows.Cast<DataRow>().GroupBy(r => new
            {
                Broj = Convert.ToString(r["BrojTrase"]).Trim(),
                AdHoc = Convert.ToBoolean(r["adHoc"]),
                Cena = r.IsNull("Cena") ? (decimal?)null : Convert.ToDecimal(r["Cena"])
            }).OrderBy(g => g.Key.Broj).ThenBy(g => g.Key.AdHoc).ThenBy(g => g.Key.Cena);

            foreach (var group in groups)
            {
                var row = result.Tabela.NewRow();
                row["BrojTrase"] = group.Key.Broj;
                row["adHoc"] = group.Key.AdHoc;
                row["Ponavljanje"] = group.Count();
                row["Cena"] = (object)group.Key.Cena ?? DBNull.Value;
                result.Tabela.Rows.Add(row);
                result.Grupe.Add(row, group.Select(r => new CenaPokretanja
                {
                    ID = Convert.ToInt64(r["ID"]),
                    StaraCena = group.Key.Cena
                }).ToList());
            }
            result.Tabela.AcceptChanges();
            return result;
        }

        internal static decimal? ProcitajCenu(object input)
        {
            string text = Convert.ToString(input, CultureInfo.CurrentCulture).Trim();
            if (text.Length == 0) return null;
            decimal value;
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                throw new FormatException("Unesite ispravnu cenu prema regionalnim podešavanjima računara.");
            if (value < 0 || value > 9999999999999999.99m || decimal.Round(value, 2) != value)
                throw new FormatException("Cena mora biti nenegativna, sa najviše dve decimale.");
            return value;
        }

        internal static DateTime KrajPerioda(DateTime inclusiveDate)
        {
            if (inclusiveDate.Date == DateTime.MaxValue.Date)
                throw new ArgumentOutOfRangeException("inclusiveDate", "Izaberite raniji krajnji datum.");
            return inclusiveDate.Date.AddDays(1);
        }
    }
}
