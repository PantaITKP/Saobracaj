using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    // Jedan red iz „Stanje na mreži“ ili „Najave vozova“, sa vremenom unosa i poslednje izmene (lokalno).
    internal sealed class StavkaPlana
    {
        internal string Opis;
        internal DateTime? Uneto;
        internal DateTime? Izmenjeno;
        internal int DanaStoji;
        internal int DanaBezIzmene;
        // Najava: datum iz kolone „Očekivano vreme“ (slobodan tekst), ako se iz njega može pročitati.
        internal DateTime? Ocekivano;
        // Koliko dana kasni: stanje na mreži od unosa, najava od očekivanog vremena.
        internal int DanaKasni;
        // Stanje na mreži stoji, ili je očekivano vreme najave prošlo, bar PlanMrezeRepo.DanaUpozorenja dana.
        internal bool ZaAkciju;
        internal string Akcija;
        internal string KratkoUpozorenje;
    }

    // Jedno mesto sa mape iz operativnog plana: stanica iz „Stanje na mreži“ ili granični prelaz iz „Najave vozova“.
    internal sealed class MestoPlana
    {
        internal string Naziv;
        internal int? StanicaID;
        internal double Lat;
        internal double Lon;
        // Mesto nije nađeno među stanicama, nego približno iz Geography.json (industrijski kolosek, luka...).
        internal bool Priblizno;
        internal List<StavkaPlana> Stavke = new List<StavkaPlana>();
        internal bool ZaAkciju { get { return Stavke.Any(s => s.ZaAkciju); } }
    }

    internal sealed class PlanNaMapi
    {
        internal List<MestoPlana> Mreza = new List<MestoPlana>();
        internal List<MestoPlana> Najave = new List<MestoPlana>();
        internal List<string> NisuNadjena = new List<string>();

        // Sve stavke za proveru: sloj (Stanje na mreži / Najave), mesto i stavka, najduže prvo.
        internal List<Tuple<string, MestoPlana, StavkaPlana>> ZaAkciju()
        {
            return Mreza.SelectMany(m => m.Stavke.Where(s => s.ZaAkciju).Select(s => Tuple.Create("Stanje na mreži", m, s)))
                .Concat(Najave.SelectMany(m => m.Stavke.Where(s => s.ZaAkciju).Select(s => Tuple.Create("Najave", m, s))))
                .OrderByDescending(x => x.Item3.DanaKasni).ToList();
        }
    }

    // Tabele dbo.OperativniPlanMreza i dbo.OperativniPlanNajave (tabovi u Planeru i dispMain-u) se ovde samo čitaju.
    // Naziv mesta je slobodan tekst („NIŠ RANŽIRNA:“, „RUMA - GUMA:“), pa se vezuje za stanicu po nazivu.
    internal static class PlanMrezeRepo
    {
        // Od koliko dana stajanja (od unosa reda) stavka traži akciju.
        internal const int DanaUpozorenja = 5;
        private const string LokalnoVreme = "DATEADD(minute, DATEDIFF(minute, GETUTCDATE(), GETDATE()), {0}) AS {1}";
        private static readonly string[] KoloneMreze = { "Vagoni", "IDVoza", "UputnaStanica", "Klijent", "BrojVoza", "Kolosek", "Planiranje", "Carina", "TPU", "Napomena" };
        private static readonly string[] KoloneNajava = { "Vagoni", "IDVoza", "UputnaStanica", "OcekivanoVreme", "BrojVoza", "PRP", "Operater", "StatusNajave", "Klijent", "Napomena" };

        // stanice: OP_Stanice (ID, Naziv, Latitude, Longitude, Aktivna, DuplikatOd).
        internal static PlanNaMapi Ucitaj(DataTable stanice)
        {
            DataTable mreza, najave;
            using (var c = Db.Open())
            {
                string vremena = ", " + string.Format(LokalnoVreme, "CreatedUtc", "Uneto") + ", " + string.Format(LokalnoVreme, "IzmenjenoUtc", "PoslednjaIzmena");
                mreza = Db.Table(c, null, "SELECT *" + vremena + " FROM dbo.OperativniPlanMreza ORDER BY ID");
                najave = Db.Table(c, null, "SELECT *" + vremena + " FROM dbo.OperativniPlanNajave ORDER BY ID");
            }
            var poNazivu = PoNazivu(stanice);
            var rez = new PlanNaMapi();
            rez.Mreza = Mesta(mreza, "Stanica", KoloneMreze, poNazivu, rez.NisuNadjena, false);
            rez.Najave = Mesta(najave, "GranicniPrelaz", KoloneNajava, poNazivu, rez.NisuNadjena, true);
            return rez;
        }

        private static StavkaPlana Stavka(DataRow r, string opis, bool najava, DateTime sada)
        {
            var s = new StavkaPlana
            {
                Opis = opis,
                Uneto = r.IsNull("Uneto") ? (DateTime?)null : Convert.ToDateTime(r["Uneto"]),
                Izmenjeno = r.IsNull("PoslednjaIzmena") ? (DateTime?)null : Convert.ToDateTime(r["PoslednjaIzmena"])
            };
            s.DanaStoji = s.Uneto.HasValue ? (int)Math.Floor((sada - s.Uneto.Value).TotalDays) : 0;
            s.DanaBezIzmene = s.Izmenjeno.HasValue ? (int)Math.Floor((sada - s.Izmenjeno.Value).TotalDays) : 0;
            if (najava)
            {
                // Najava sme da stoji duže; upozorenje je tek kad je očekivano vreme prošlo, a ne starost reda.
                s.Ocekivano = OcekivanoVreme(Convert.ToString(r["OcekivanoVreme"]), s.Uneto ?? sada);
                s.DanaKasni = s.Ocekivano.HasValue ? (int)Math.Floor((sada - s.Ocekivano.Value).TotalDays) : 0;
                s.ZaAkciju = s.Ocekivano.HasValue && s.DanaKasni >= DanaUpozorenja;
                if (s.ZaAkciju)
                {
                    s.KratkoUpozorenje = "očekivano " + s.Ocekivano.Value.ToString("dd.MM.") + ", kasni " + s.DanaKasni + " dana";
                    s.Akcija = "Očekivano vreme (" + s.Ocekivano.Value.ToString("dd.MM.yyyy") + ") je prošlo pre " + s.DanaKasni + " dana: proveriti da li je voz ušao, ažurirati očekivano vreme ili ukloniti najavu.";
                }
            }
            else
            {
                s.DanaKasni = s.DanaStoji;
                s.ZaAkciju = s.DanaStoji >= DanaUpozorenja;
                if (s.ZaAkciju)
                {
                    s.KratkoUpozorenje = "stoji " + s.DanaStoji + " dana";
                    s.Akcija = "Stoji " + s.DanaStoji + " dana: organizovati prevoz pri sledećem planu ili ažurirati stanje.";
                }
            }
            return s;
        }

        // „05.11.2026.“, „04.11.2026. 20h“, „05.11.2026. 01h“, „03/04.11.2026.“ (raspon: kasniji dan), „12.11. 14:30“.
        // Tekst bez datuma („?“, „*ČEKA - JAVITI KADA PRIMAMO“) nema očekivano vreme. Datum bez godine je iz godine unosa.
        internal static DateTime? OcekivanoVreme(string tekst, DateTime uneto)
        {
            Match d = Regex.Match(tekst ?? "", @"(\d{1,2})(?:\s*/\s*(\d{1,2}))?\s*\.\s*(\d{1,2})\s*\.(?:\s*(\d{4})\.?)?");
            if (!d.Success) return null;
            int dan = int.Parse(d.Groups[d.Groups[2].Success ? 2 : 1].Value), mesec = int.Parse(d.Groups[3].Value);
            int godina = d.Groups[4].Success ? int.Parse(d.Groups[4].Value) : uneto.Year;
            if (mesec < 1 || mesec > 12 || dan < 1 || dan > DateTime.DaysInMonth(godina, mesec)) return null;
            var datum = new DateTime(godina, mesec, dan);
            // Bez godine: datum mnogo pre unosa je iz sledeće godine (unos u decembru za januar).
            if (!d.Groups[4].Success && datum < uneto.Date.AddMonths(-6)) datum = datum.AddYears(1);
            string ostatak = tekst.Substring(d.Index + d.Length);
            Match v = Regex.Match(ostatak, @"(?<!\d)(\d{1,2})(?:\s*[:.]\s*(\d{2}))?\s*(?:h|č|cas|čas)", RegexOptions.IgnoreCase);
            if (!v.Success) v = Regex.Match(ostatak, @"(?<!\d)(\d{1,2})\s*:\s*(\d{2})");
            if (v.Success)
            {
                int sat = int.Parse(v.Groups[1].Value), minut = v.Groups[2].Success ? int.Parse(v.Groups[2].Value) : 0;
                if (sat < 24 && minut < 60) datum = datum.AddHours(sat).AddMinutes(minut);
            }
            return datum;
        }

        private static Dictionary<string, DataRow> PoNazivu(DataTable stanice)
        {
            var d = new Dictionary<string, DataRow>();
            // Aktivna stanica bez duplikata ima prednost za isti naziv.
            foreach (DataRow r in stanice.Rows.Cast<DataRow>()
                .Where(r => !r.IsNull("Latitude") && !r.IsNull("Longitude"))
                .OrderByDescending(r => Convert.ToBoolean(r["Aktivna"])).ThenBy(r => r.IsNull("DuplikatOd") ? 0 : 1))
            {
                string n = StationNames.Normalize(Convert.ToString(r["Naziv"]));
                if (n.Length > 0 && !d.ContainsKey(n)) d[n] = r;
            }
            // Stari naziv iz starog šifarnika („Beograd Ranžirna“ za Beograd ranžirna A) dolazi posle pravih naziva.
            foreach (DataRow r in stanice.Rows.Cast<DataRow>().Where(r => !r.IsNull("Latitude") && !r.IsNull("Longitude") && Convert.ToBoolean(r["Aktivna"]) && r.Table.Columns.Contains("NazivStari")))
            {
                string n = StationNames.Normalize(Convert.ToString(r["NazivStari"]));
                if (n.Length > 0 && !d.ContainsKey(n)) d[n] = r;
            }
            return d;
        }

        // Redovi bez ičega osim naziva mesta su zaglavlja u tabeli Planera i ne idu na mapu.
        private static List<MestoPlana> Mesta(DataTable t, string kolonaMesta, string[] kolone, Dictionary<string, DataRow> poNazivu, List<string> nisuNadjena, bool najava)
        {
            DateTime sada = DateTime.Now;
            var mesta = new Dictionary<string, MestoPlana>();
            foreach (DataRow r in t.Rows)
            {
                string naziv = Convert.ToString(r[kolonaMesta]).Trim().TrimEnd(':').Trim();
                if (naziv.Length == 0) continue;
                string opis = string.Join(" · ", kolone.Where(k => t.Columns.Contains(k)).Select(k => Opis(k, Convert.ToString(r[k]).Trim())).Where(x => x.Length > 0));
                if (opis.Length == 0) continue;
                string kljuc = StationNames.Normalize(naziv);
                MestoPlana m;
                if (!mesta.TryGetValue(kljuc, out m))
                {
                    m = Nadji(naziv, poNazivu);
                    if (m == null) { if (!nisuNadjena.Contains(naziv)) nisuNadjena.Add(naziv); continue; }
                    mesta[kljuc] = m;
                }
                m.Stavke.Add(Stavka(r, opis, najava, sada));
            }
            return mesta.Values.ToList();
        }

        private static string Opis(string kolona, string v)
        {
            if (v.Length == 0) return "";
            switch (kolona)
            {
                case "UputnaStanica": return "→ " + v;
                case "Kolosek": return "kol. " + v;
                case "BrojVoza": return "voz " + v;
                case "OcekivanoVreme": return "očekuje se " + v;
                default: return v;
            }
        }

        // Tačan naziv, pa naziv pre „ - “ ili zagrade (RUMA - GUMA → Ruma), pa sve kraći početak naziva
        // (SURČIN SILOS → Surčin), i na kraju približno mesto iz Geography.json (isti izvor kao dispMain).
        private static MestoPlana Nadji(string naziv, Dictionary<string, DataRow> poNazivu)
        {
            // Beograd ranžirna bez oznake je grupa A, kao kod trasa.
            var probe = new List<string> { naziv, naziv + " a" };
            Match deo = Regex.Match(naziv, @"^(.+?)\s*(?:[-–(]|\d+\.)");
            if (deo.Success) probe.Add(deo.Groups[1].Value);
            string[] reci = StationNames.Normalize(naziv).Split(' ');
            for (int n = reci.Length - 1; n >= 1; n--) probe.Add(string.Join(" ", reci.Take(n)));
            foreach (string p in probe)
            {
                DataRow s;
                if (poNazivu.TryGetValue(StationNames.Normalize(p), out s))
                    return new MestoPlana { Naziv = naziv, StanicaID = Convert.ToInt32(s["ID"]), Lat = Convert.ToDouble(s["Latitude"]), Lon = Convert.ToDouble(s["Longitude"]) };
            }
            try
            {
                GeographicPlace g = StationGeography.Find(naziv);
                if (g != null) return new MestoPlana { Naziv = naziv, Lat = g.Latitude, Lon = g.Longitude, Priblizno = true };
            }
            catch (Exception)
            {
                // Bez Geography.json mesto ostaje među nenađenima.
            }
            return null;
        }
    }
}
