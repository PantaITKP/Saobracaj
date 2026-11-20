using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// v3.0.5: dnevni sažetak agenta. Svaka obrađena poruka i svako slanje (poslato / blokirano prekidačem) upisuju se kao red u
    /// stanje\dnevnik\yyyy-MM-dd.jsonl (bez sadržaja dokumenata: naslov, pošiljalac, status, pravila, brojevi upisa). Jednom dnevno, u prvom
    /// prolazu posle DnevniSazetakSat, agent šalje sažetak prethodnog dana na agenti@ (broj mailova, odluke, izveštaji, greške po pravilu,
    /// upis poslova). Poslednji poslat dan je u stanje\dnevnik\sazetak_poslat.txt.
    /// </summary>
    internal static class DnevniSazetak
    {
        public const string Folder = "dnevnik";
        public const string Oznaka = "sazetak_poslat.txt";

        private static readonly object brava = new object();

        private static string Dir(string stanje) => Path.Combine(stanje, Folder);
        internal static string Fajl(string stanje, DateTime dan) => Path.Combine(Dir(stanje), dan.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");

        private static void Dodaj(string stanje, DateTime vreme, JObject o)
        {
            if (string.IsNullOrEmpty(stanje)) return;
            lock (brava)
            {
                Directory.CreateDirectory(Dir(stanje));
                File.AppendAllText(Fajl(stanje, vreme.Date), o.ToString(Formatting.None) + Environment.NewLine, new UTF8Encoding(false));
            }
        }

        /// <summary>Red za obrađenu poruku (ili pokušaj koji je završio izuzetkom – <paramref name="izuzetak"/>).</summary>
        public static void ZapisiPoruku(string stanje, DateTime vreme, UlaznaPoruka u, RezultatObrade r, string izuzetak = null)
        {
            var m = u?.Poruka;
            var nalazi = new JArray();
            foreach (var n in r?.Provera?.Listovi?.SelectMany(l => l.Nalazi) ?? Enumerable.Empty<Nalaz>())
                nalazi.Add(new JObject { ["p"] = n.Pravilo ?? "", ["t"] = n.Tezina ?? "", ["i"] = n.SamoInterno });
            var o = new JObject
            {
                ["tip"] = "poruka",
                ["vreme"] = vreme.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["folder"] = u?.Folder ?? "",
                ["uid"] = u?.Uid ?? 0,
                ["odlazni"] = u?.Odlazni ?? false,
                ["od"] = PrimaociLive.Ocisti(m?.From.Mailboxes.FirstOrDefault()?.Address),
                ["naslov"] = (m?.Subject ?? "").Trim(),
                ["korak"] = izuzetak != null ? "Izuzetak" : (r?.Korak.ToString() ?? ""),
                ["status"] = r?.Status ?? "",
                ["odluka"] = r?.Odluka ?? "",
                ["poruke"] = new JArray((r?.Poruke ?? new List<PripremljenaPoruka>()).Select(x => x.Vrsta ?? "")),
                ["nalazi"] = nalazi,
            };
            if (r?.Upis != null)
                o["upis"] = new JObject
                {
                    ["dokumenata"] = r.Upis.Dokumenata, ["novih"] = r.Upis.NovihPoslova, ["izmenjenih"] = r.Upis.IzmenjenihPoslova, ["istih"] = r.Upis.IstihPoslova,
                    ["teretnica"] = r.Upis.Teretnica, ["stavki"] = r.Upis.Stavki,
                };
            if (!string.IsNullOrEmpty(r?.GreskaUpisa)) o["greska_upisa"] = r.GreskaUpisa;
            if (!string.IsNullOrEmpty(izuzetak ?? r?.Greska)) o["greska"] = izuzetak ?? r.Greska;
            Dodaj(stanje, vreme, o);
        }

        /// <summary>Red za slanje: poslato ili blokirano prekidačem (razlog).</summary>
        public static void ZapisiSlanje(string stanje, DateTime vreme, MimeMessage m, bool poslato, string razlog)
        {
            var o = new JObject
            {
                ["tip"] = "slanje",
                ["vreme"] = vreme.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["ishod"] = poslato ? "poslato" : "blokirano",
                ["naslov"] = (m?.Subject ?? "").Trim(),
                ["za"] = string.Join(", ", (m?.To.Mailboxes ?? Enumerable.Empty<MailboxAddress>()).Concat(m?.Cc.Mailboxes ?? Enumerable.Empty<MailboxAddress>()).Select(x => x.Address)),
                ["vrsta"] = m?.Headers[Mail.Slanje.ZaglavljeVrste] ?? "",
            };
            if (razlog != null) o["razlog"] = razlog;
            Dodaj(stanje, vreme, o);
        }

        private static List<JObject> Redovi(string stanje, DateTime dan)
        {
            var f = Fajl(stanje, dan);
            var l = new List<JObject>();
            if (!File.Exists(f)) return l;
            foreach (var red in File.ReadAllLines(f, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(red)) continue;
                try { l.Add(JObject.Parse(red)); } catch { }
            }
            return l;
        }

        /// <summary>Tekst sažetka za jedan dan.</summary>
        public static string Sastavi(string stanje, DateTime dan, string rezim, string server, string verzija)
        {
            var redovi = Redovi(stanje, dan);
            var poruke = redovi.Where(x => (string)x["tip"] == "poruka").ToList();
            var slanja = redovi.Where(x => (string)x["tip"] == "slanje").ToList();
            // poslednji pokušaj po poruci (ponovljeni pokušaji iste poruke se ne broje dvaput)
            var poPoruci = poruke.GroupBy(x => (string)x["folder"] + "/" + (long)x["uid"]).Select(g => g.Last()).ToList();
            var sopstvene = poPoruci.Where(x => (string)x["status"] == "Preskoceno").ToList();
            var mailovi = poPoruci.Except(sopstvene).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Dnevni sažetak agenta CimProvera – " + dan.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture) + " (režim " + rezim + ")");
            sb.AppendLine();
            sb.AppendLine("MAILOVI: " + mailovi.Count + " (dolaznih " + mailovi.Count(x => !(bool)x["odlazni"]) + ", odlaznih " + mailovi.Count(x => (bool)x["odlazni"]) + ")"
                + "; sopstvene poruke agenta (preskočene): " + sopstvene.Count
                + "; pokušaja sa privremenom greškom: " + poruke.Count(x => (string)x["korak"] == "PrivremenaGreska" || (string)x["korak"] == "Izuzetak"));
            sb.AppendLine();
            sb.AppendLine("ODLUKE (status maila):");
            if (mailovi.Count == 0) sb.AppendLine("  –");
            foreach (var g in mailovi.GroupBy(x => ((string)x["status"]).Length > 0 ? (string)x["status"] : "(bez statusa – " + (string)x["korak"] + ")").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal))
                sb.AppendLine("  " + g.Key + ": " + g.Count());
            sb.AppendLine();
            sb.AppendLine("IZVEŠTAJI I PORUKE:");
            var vrste = poruke.SelectMany(x => (x["poruke"] as JArray ?? new JArray()).Select(v => (string)v)).GroupBy(v => v).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
            sb.AppendLine("  pripremljeno po vrsti: " + (vrste.Count == 0 ? "–" : string.Join(", ", vrste.Select(g => g.Key + " " + g.Count()))));
            var blok = slanja.Where(x => (string)x["ishod"] == "blokirano").ToList();
            sb.AppendLine("  poslato: " + slanja.Count(x => (string)x["ishod"] == "poslato") + "; blokirano prekidačem: " + blok.Count);
            foreach (var b in blok.GroupBy(x => (string)x["razlog"])) sb.AppendLine("    – " + b.Key + ": " + b.Count());
            sb.AppendLine();
            sb.AppendLine("GREŠKE PO PRAVILU (greška / upozorenje / INTERNO):");
            var nal = mailovi.SelectMany(x => (x["nalazi"] as JArray ?? new JArray()).OfType<JObject>()).ToList();
            var pravila = nal.GroupBy(x => (string)x["p"]).Select(g => new
            {
                p = g.Key,
                gr = g.Count(x => !(bool)x["i"] && (string)x["t"] == Tezina.Greska),
                up = g.Count(x => !(bool)x["i"] && (string)x["t"] == Tezina.Upozorenje),
                it = g.Count(x => (bool)x["i"]),
            }).Where(x => x.gr + x.up + x.it > 0).OrderByDescending(x => x.gr).ThenByDescending(x => x.up).ThenBy(x => x.p, StringComparer.Ordinal).ToList();
            if (pravila.Count == 0) sb.AppendLine("  –");
            foreach (var x in pravila) sb.AppendLine("  " + x.p.PadRight(14) + " " + x.gr + " / " + x.up + " / " + x.it);
            sb.AppendLine();
            var upisi = mailovi.Select(x => x["upis"] as JObject).Where(x => x != null).ToList();
            int S(string k) => upisi.Sum(x => (int?)x[k] ?? 0);
            var greskeUpisa = mailovi.Where(x => x["greska_upisa"] != null).ToList();
            sb.AppendLine("UPIS POSLOVA (planiranje.tmp*, Izvor = Agent): mailova sa upisom " + upisi.Count + "; poslova novih " + S("novih") + ", izmenjenih " + S("izmenjenih")
                + ", istih " + S("istih") + "; teretnica " + S("teretnica") + ", stavki " + S("stavki") + "; grešaka upisa " + greskeUpisa.Count);
            foreach (var x in greskeUpisa) sb.AppendLine("  – UID " + (long)x["uid"] + " „" + (string)x["naslov"] + "“: " + (string)x["greska_upisa"]);
            var greske = poruke.Where(x => x["greska"] != null).ToList();
            if (greske.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("GREŠKE OBRADE:");
                foreach (var x in greske) sb.AppendLine("  – " + (string)x["vreme"] + " UID " + (long)x["uid"] + " „" + (string)x["naslov"] + "“: " + (string)x["greska"]);
            }
            sb.AppendLine();
            sb.AppendLine("Server " + server + ", program " + verzija + ". Detalji: log agenta i stanje\\dnevnik\\" + Path.GetFileName(Fajl(stanje, dan)) + ".");
            return sb.ToString();
        }

        private static string OznakaFajl(string stanje) => Path.Combine(Dir(stanje), Oznaka);

        /// <summary>
        /// Dani za koje treba poslati sažetak: od dana posle poslednjeg poslatog do juče (najviše 7), samo posle sata <paramref name="sat"/>.
        /// Prvi put (nema oznake) se ništa ne šalje – oznaka se postavlja na juče, pa prvi sažetak obuhvata dan uključivanja.
        /// </summary>
        public static List<DateTime> ZaSlanje(string stanje, DateTime sada, int sat)
        {
            var l = new List<DateTime>();
            if (sat < 0 || string.IsNullOrEmpty(stanje)) return l;
            var f = OznakaFajl(stanje);
            if (!File.Exists(f)) { OznaciPoslato(stanje, sada.Date.AddDays(-1)); return l; }
            if (sada.Hour < sat) return l;
            if (!DateTime.TryParseExact(File.ReadAllText(f).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var posl)) posl = sada.Date.AddDays(-2);
            var od = posl.AddDays(1);
            if (od < sada.Date.AddDays(-7)) od = sada.Date.AddDays(-7);
            for (var d = od; d < sada.Date; d = d.AddDays(1)) l.Add(d);
            return l;
        }

        public static void OznaciPoslato(string stanje, DateTime dan)
        {
            Directory.CreateDirectory(Dir(stanje));
            File.WriteAllText(OznakaFajl(stanje), dan.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), new UTF8Encoding(false));
        }
    }
}
