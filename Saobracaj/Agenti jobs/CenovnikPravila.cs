using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace Saobracaj.Agenti_jobs
{
    public static class CenovnikPravila
    {
        public static void ProveriPeriod(DateTime od, DateTime doDana)
        {
            if (od.Year < 1753 || doDana.Year > 9998) throw new InvalidOperationException("Period je izvan podržanog opsega.");
            if (od.Date > doDana.Date) throw new InvalidOperationException("Datum od ne može biti posle datuma do.");
        }
        public static int Godina(DateTime od, DateTime doDana)
        {
            ProveriPeriod(od, doDana);
            return doDana.Year;
        }
        public static decimal Broj(decimal value)
        {
            if (Math.Abs(value) > 9999999999999999.99m) throw new InvalidOperationException("Vrednost je izvan opsega decimal(18,2).");
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }
        // Only blank numeric fields in an existing annual record may be filled.
        public static Dictionary<string, decimal> Dopune(DataRow postojeca, RouteInfo source)
        {
            var candidates = new Dictionary<string, decimal?>
            {
                { "TezinaVoza", source.MaxWeight },
                { "DuzinaVoza", source.LengthValue },
                { "ProcenatKocenja", source.BrakePercent },
                { "Cena", source.Price },
                { "ProcenatTrase", source.BrakePercent }
            };
            var changes = new Dictionary<string, decimal>();
            foreach (var pair in candidates)
            {
                if (!pair.Value.HasValue) continue;
                bool empty = postojeca.IsNull(pair.Key);
                decimal current = empty ? 0m : Convert.ToDecimal(postojeca[pair.Key]);
                decimal value = Broj(pair.Value.Value);
                if ((empty || current == 0m) && (empty || current != value))
                    changes.Add(pair.Key, value);
            }
            return changes;
        }

        public static Dictionary<string, object> Vrednosti(DataRow prethodna, RouteInfo source, string brojVoza, int godina, int od, int doStanice, bool adHoc = false)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in SifarnikKolone) values[name] = 0m;
            values["Pocetna"] = od; values["Krajnja"] = doStanice;
            values["Relacija"] = string.IsNullOrWhiteSpace(source.Relation) ? source.From + " - " + source.To : source.Relation;
            values["OpisRelacije"] = ""; values["Voz"] = brojVoza; values["Rang"] = "F";
            values["Prevoznik"] = 1;
            values["VremePolaska"] = new DateTime(1900, 1, 1);
            values["VremeDolaska"] = new DateTime(1900, 1, 1);
            values["Napomena"] = DBNull.Value;
            if (prethodna != null)
                foreach (var column in SifarnikKolone) values[column] = prethodna[column];
            values["Voz"] = brojVoza;
            values["Godina"] = godina.ToString("0000", CultureInfo.InvariantCulture);
            if (source.MaxWeight.HasValue) values["TezinaVoza"] = Broj(source.MaxWeight.Value);
            if (source.LengthValue.HasValue) values["DuzinaVoza"] = Broj(source.LengthValue.Value);
            if (source.BrakePercent.HasValue)
            {
                values["ProcenatKocenja"] = Broj(source.BrakePercent.Value);
                values["ProcenatTrase"] = Broj(source.BrakePercent.Value);
            }
            if (source.Price.HasValue) values["Cena"] = Broj(source.Price.Value);
            if (adHoc) values["OpisRelacije"] = "AdHoc";
            else
            {
                string oldDescription = StationNames.Normalize(Convert.ToString(values["OpisRelacije"])).Replace(" ", "");
                if (oldDescription == "adhoc" || oldDescription == "adhock")
                    values["OpisRelacije"] = "";
            }
            if (brojVoza.Length > 10) throw new InvalidOperationException("Broj voza je duži od 10 znakova.");
            if (Convert.ToString(values["Relacija"]).Length > 150)
                throw new InvalidOperationException("Relacija je duža od 150 znakova: " + brojVoza);
            return values;
        }

        public static readonly string[] SifarnikKolone =
        {
            "Pocetna","Krajnja","Relacija","OpisRelacije","Voz","Rang","TezinaVoza","TezinaLokomotive",
            "Prevoznik","DuzinaVoza","ProcenatKocenja","Cena","Rastojanje","VremePolaska","VremeDolaska",
            "DuzinaTrajanja","Rezi","Godina","CenaKalk","RastojanjeMag","RastojanjeReg","RastojanjeLok",
            "Dana","NajveciOtpor","NajkraciKol","OsovinskoOpter","TipED","ElektroKM","DizelKM",
            "PrevoznoRastojanje","Napomena","ProcenatTrase"
        };
    }

    public sealed class SifarnikDokument
    {
        public long ID;
        public string Naziv;
        public DateTime? Od, Do;
        public bool NedostajaoPeriod;
    }

    public sealed class SifarnikPrevoz
    {
        public long ID;
        public byte[] Verzija;
        public string Broj, BrojVoznje;
        public int Godina;
        public DateTime? Od, Do;
        public RouteInfo Excel;
        public SifarnikDokument Dokument;
        public bool PotrebneStanice, AdHoc;
        public int StanicaOd, StanicaDo;
        public int RNStanicaOd, RNStanicaDo;
        public System.Collections.Generic.List<string> RNLokomotive;
        public string RNVucnaLokomotiva;
        public System.Collections.Generic.List<RNRaspodela> RNStavke;
    }
}
