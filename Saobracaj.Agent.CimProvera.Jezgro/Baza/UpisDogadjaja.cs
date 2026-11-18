using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Baza
{
    internal sealed partial class UpisUBazu
    {
        /// <summary>
        /// v3.0.1 (5): naslov bez prefiksa odgovora i prosleđivanja („RE:“, „FW:“, „AW:“, „WG:“, „Odg:“, „TR:“, „SV:“, „[EXTERNAL]“) – za nit.
        /// </summary>
        internal static string NaslovNiti(string naslov)
        {
            var t = (naslov ?? "").Trim();
            string pre;
            do
            {
                pre = t;
                t = Regex.Replace(t, @"^\s*(\[[^\]]{1,20}\]\s*)?(re|fw|fwd|aw|wg|odg|odgovor|tr|sv|vs|rv|r|i|ant|res|enc)\s*(\[\d+\])?\s*:\s*", "", RegexOptions.IgnoreCase).Trim();
            } while (t != pre && t.Length > 0);
            return Regex.Replace(Slicnost.Osnovno(t), @"\s+", " ").Trim();
        }

        /// <summary>
        /// v3.0.1 (5): posao za događaje maila koji nema svoj posao (mail bez dokumenata: „Announcement Surčin – Alesd“, „status for empty
        /// train exGK33“, „Lukavac02“). Redom: (1) nit – In-Reply-To/References i naslov bez RE/FW; (2) ID najave („ID nnnn“);
        /// (3) broj voza ili referenca; (4) relacija iz naslova + datum (± 5 dana). Veza je sigurna samo kada korak daje JEDAN posao;
        /// inače se nastavlja, a nađeni poslovi su kandidati.
        /// </summary>
        internal (int? id, string veza, List<string> kandidati) PosaoZaDogadjaje(MailZaUpis m)
        {
            var kandidati = new List<string>();
            (int? id, string veza, List<string> kandidati) Jedan(IEnumerable<int> ids, string opis)
            {
                var l = ids.Distinct().ToList();
                if (l.Count == 1) return (l[0], opis, new List<string>());
                foreach (var j in l) kandidati.Add("posao " + j + ": " + opis);
                return (null, null, null);
            }
            int Broj(string x) => int.TryParse(x, out var v) ? v : 0;

            // (1) nit: In-Reply-To / References, pa isti naslov (bez RE/FW) u poslednjih 14 dana
            var prethodni = (m.Prethodni ?? new List<string>()).Select(x => (x ?? "").Trim().Trim('<', '>')).Where(x => x.Length > 3).Distinct().ToList();
            if (prethodni.Count > 0)
            {
                var ids = new List<int>();
                foreach (var g in prethodni.Select((x, i) => (x, i)).GroupBy(x => x.i / 50))
                {
                    var arg = new List<object>(); var imena = new List<string>();
                    foreach (var (x, i) in g) { imena.Add("@p" + i); arg.Add("@p" + i); arg.Add(N(x, 400)); arg.Add("@p" + i + "b"); arg.Add(N("<" + x + ">", 400)); }
                    var sql = "SELECT DISTINCT JobID FROM planiranje.tmpDokument WHERE JobID IS NOT NULL AND (MessageID IN (" + string.Join(", ", imena) + ") OR MessageID IN (" + string.Join(", ", imena.Select(x => x + "b")) + "))";
                    ids.AddRange(Kolona(sql, arg.ToArray()).Select(Broj).Where(x => x > 0));
                }
                var r = Jedan(ids, "nit (In-Reply-To/References)");
                if (r.id.HasValue) return r;
            }
            var nit = NaslovNiti(m.Naslov);
            if (nit.Length >= 5)
            {
                var ids = new List<int>();
                using (var cmd = Komanda("SELECT DISTINCT JobID, ISNULL(Naslov, '') FROM planiranje.tmpDokument WHERE JobID IS NOT NULL AND Primljeno >= DATEADD(day, -14, @t) AND Primljeno <= DATEADD(day, 1, @t)", "@t", m.Primljeno))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read()) if (NaslovNiti(rd.GetString(1)) == nit) ids.Add(rd.GetInt32(0));
                var r = Jedan(ids, "nit (isti naslov bez RE/FW)");
                if (r.id.HasValue) return r;
            }

            // (2) ID najave: „ID 1657“ u naslovu, telu ili događaju ID_NAJAVE
            var idNajave = new HashSet<string>(IdIzNaslova(m.Naslov));
            foreach (var x in IdIzNaslova(m.Telo)) idNajave.Add(x);
            foreach (var d in m.Dogadjaji.Where(d => d.Tip == "ID_NAJAVE")) foreach (Match x in Regex.Matches(d.Vrednost ?? "", @"\d{3,5}")) idNajave.Add(x.Value);
            if (idNajave.Count > 0)
            {
                var ids = new List<int>();
                using (var cmd = Komanda("SELECT DISTINCT JobID, ISNULL(Naslov, '') FROM planiranje.tmpDokument WHERE JobID IS NOT NULL AND Primljeno >= DATEADD(day, -30, @t)", "@t", m.Primljeno))
                using (var rd = cmd.ExecuteReader())
                    while (rd.Read()) if (IdIzNaslova(rd.GetString(1)).Overlaps(idNajave)) ids.Add(rd.GetInt32(0));
                var r = Jedan(ids, "ID najave " + string.Join("/", idNajave));
                if (r.id.HasValue) return r;
            }

            // (3) broj voza ili referenca posla (r8): iz događaja BROJ_VOZA, naslova i teksta maila
            var oznake = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in m.Dogadjaji.Where(d => d.Tip == "BROJ_VOZA")) if (NormOznaka(d.Vrednost).Length >= 3) oznake.Add(NormOznaka(d.Vrednost));
            var bvN = ExcelCitac.BrojVoza(m.Naslov);
            if (NormOznaka(bvN).Length >= 3) oznake.Add(NormOznaka(bvN));
            var posloviRef = new List<(int id, string refN, string vozN)>();
            using (var cmd = Komanda("SELECT ID, ISNULL(Referenca, '') FROM planiranje.tmpJobs WHERE Kreirano >= DATEADD(day, -30, @t)", "@t", m.Primljeno))
            using (var rd = cmd.ExecuteReader())
                while (rd.Read()) posloviRef.Add((rd.GetInt32(0), NormOznaka(rd.GetString(1)), NormOznaka(ExcelCitac.BrojVoza(rd.GetString(1)))));
            var naslovN = NormOznaka(m.Naslov);
            var poOznaci = posloviRef.Where(p => (p.vozN.Length >= 3 && oznake.Contains(p.vozN)) || (p.refN.Length >= 4 && (oznake.Contains(p.refN) || naslovN.Contains(p.refN)))).Select(p => p.id).ToList();
            if (poOznaci.Count > 0)
            {
                var r = Jedan(poOznaci, "broj voza / referenca");
                if (r.id.HasValue) return r;
            }

            // (4) relacija iz naslova („Surčin – Alesd“, „Bor teretna - Subotica gr“) + datum ± 5 dana
            if (pk != null)
            {
                var delovi = Regex.Split(m.Naslov ?? "", @"\s+[-–—>]+\s+|\s*–\s*|\s+(?:to|do|nach|za)\s+", RegexOptions.IgnoreCase)
                    .Select(x => Regex.Replace(x, @"(?i)^.*?(announcement|najava|status|aviso|avis|train|voz)\b[^\p{L}]*", "").Trim(' ', ',', ':', '-'))
                    .Where(x => x.Length >= 3).ToList();
                var stanice = new HashSet<int>();
                foreach (var d in delovi.Take(4))
                {
                    var s = pk.Stanica(d);
                    if (s.Id.HasValue && s.Pouzdanost >= 90) stanice.Add(s.Id.Value);
                }
                if (stanice.Count >= 2)
                {
                    var ids = new List<int>();
                    using (var cmd = Komanda(@"SELECT ID, OtpravnaID, UputnaID, Stanica1ID, Stanica3ID FROM planiranje.tmpJobs WHERE Kreirano >= DATEADD(day, -5, @t) AND Kreirano <= DATEADD(day, 5, @t)", "@t", m.Primljeno))
                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read())
                        {
                            var st = Enumerable.Range(1, 4).Where(i => !rd.IsDBNull(i)).Select(i => rd.GetInt32(i)).ToList();
                            if (stanice.Count(st.Contains) >= 2) ids.Add(rd.GetInt32(0));
                        }
                    var r = Jedan(ids, "relacija + datum");
                    if (r.id.HasValue) return r;
                }
            }
            return (null, null, kandidati.Distinct().Take(10).ToList());
        }
    }
}
