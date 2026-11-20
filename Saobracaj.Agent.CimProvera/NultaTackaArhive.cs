using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Saobracaj.Agent.CimProvera.Mail;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// v3.0.5 (Test režim na serveru): nulta tačka = poslednji UID pokriven arhivom u trenutku uključivanja. Komanda
    /// „nulta-tacka-arhiva“ (Ukljuci-TestRezim.ps1, posle upravo završenog arhiviranja) čita arhiva\izvestaj.txt i arhiva\indeks.csv – bez
    /// IMAP-a i bez lozinke – i postavlja poziciju čitanja agenta (stanje.json) na poslednji UID po folderu. Poruke do nulte tačke su u arhivi
    /// (obrađuju se po potrebi u CimRadionici), agent obrađuje samo poruke posle nje. Pre izmene se pravi rezervna kopija stanja.
    /// Odbija ako: izveštaj arhive nije svež, arhiva ima rupe, poslednji UID INBOX-a na serveru nije u arhivi, zadatak obrade je uključen
    /// ili agent upravo radi.
    /// </summary>
    internal static class NultaTackaArhive
    {
        public const string Ime = "nulta_tacka.txt";

        internal sealed class FolderArhive
        {
            public string Folder;
            public long UidValidity;
            public int NaServeru;
            public long MaxNaServeru;
            public string Rupe;
        }

        internal sealed class Rezultat
        {
            public DateTimeOffset VremeArhive;
            public List<FolderArhive> Folderi = new List<FolderArhive>();
            /// <summary>Folder → nulta tačka (UID).</summary>
            public Dictionary<string, long> Uid = new Dictionary<string, long>();
            public List<string> Greske = new List<string>();
            public bool Ispravan => Greske.Count == 0;
        }

        private static readonly Regex Zaglavlje = new Regex(@"^Arhiva .*? – (?<t>\d{4}-\d{2}-\d{2} \d{2}:\d{2} [+-]\d{2}:\d{2})\s*$");
        private static readonly Regex RedFoldera = new Regex(@"^\s+(?<f>\S.*?) \(UIDVALIDITY (?<v>\d+)\): na serveru (?<n>\d+) poruka, [^;]*?(, UID (?<a>\d+)–(?<b>\d+))?;.*; rupe \(nema u arhivi\): (?<r>.+?)\s*$");

        /// <summary>Nulta tačka iz izveštaja arhiviranja i indeksa arhive (bez IMAP-a).</summary>
        internal static Rezultat Izracunaj(string izvestaj, IEnumerable<ZapisArhive> indeks, DateTimeOffset sada, TimeSpan maxStarost, string dolazni = "INBOX")
        {
            var r = new Rezultat();
            var redovi = (izvestaj ?? "").Replace("\r", "").Split('\n');
            var z = redovi.Select(x => Zaglavlje.Match(x)).FirstOrDefault(x => x.Success);
            if (z == null || !DateTimeOffset.TryParseExact(z.Groups["t"].Value, "yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                r.Greske.Add("izveštaj arhive (arhiva\\izvestaj.txt) nema vreme arhiviranja");
                return r;
            }
            r.VremeArhive = t;
            if (sada - t > maxStarost) r.Greske.Add("izveštaj arhive je od " + t.ToString("dd.MM.yyyy HH:mm") + " – stariji od " + (int)maxStarost.TotalMinutes + " min; prvo pokrenuti arhiviranje");
            if (t - sada > TimeSpan.FromMinutes(5)) r.Greske.Add("vreme izveštaja arhive (" + t.ToString("dd.MM.yyyy HH:mm") + ") je u budućnosti");
            foreach (var x in redovi.Select(x => RedFoldera.Match(x)).Where(x => x.Success))
                r.Folderi.Add(new FolderArhive
                {
                    Folder = x.Groups["f"].Value, UidValidity = long.Parse(x.Groups["v"].Value, CultureInfo.InvariantCulture),
                    NaServeru = int.Parse(x.Groups["n"].Value, CultureInfo.InvariantCulture),
                    MaxNaServeru = x.Groups["b"].Success ? long.Parse(x.Groups["b"].Value, CultureInfo.InvariantCulture) : 0,
                    Rupe = x.Groups["r"].Value.Trim(),
                });
            var inbox = r.Folderi.FirstOrDefault(x => string.Equals(x.Folder, dolazni, StringComparison.OrdinalIgnoreCase));
            if (inbox == null) { r.Greske.Add("u izveštaju arhive nema foldera " + dolazni); return r; }
            var zapisi = (indeks ?? Enumerable.Empty<ZapisArhive>()).ToList();
            foreach (var f in r.Folderi)
            {
                if (!string.Equals(f.Rupe, "nema", StringComparison.OrdinalIgnoreCase)) { r.Greske.Add(f.Folder + ": arhiva ima rupe (" + f.Rupe + ")"); continue; }
                if (f == inbox)
                {
                    var maxArh = zapisi.Where(x => string.Equals(x.Folder, f.Folder, StringComparison.OrdinalIgnoreCase) && x.UidValidity == f.UidValidity).Select(x => x.Uid).DefaultIfEmpty(0).Max();
                    if (f.NaServeru > 0 && maxArh != f.MaxNaServeru)
                    {
                        r.Greske.Add(f.Folder + ": poslednji UID na serveru " + f.MaxNaServeru + ", a u arhivi " + maxArh + " (UIDVALIDITY " + f.UidValidity + ")");
                        continue;
                    }
                    r.Uid[f.Folder] = maxArh;
                }
                // folder poslatih: sve na serveru u trenutku arhiviranja je ili u arhivi ili pre početne tačke (rupa nema)
                else r.Uid[f.Folder] = f.MaxNaServeru;
            }
            return r;
        }

        /// <summary>Postavlja poziciju čitanja i nultu tačku u stanje (ne smanjuje već dostignut UID istog UIDVALIDITY-ja).</summary>
        internal static List<string> Primeni(Stanje s, string nalog, Rezultat r, string dolazni = "INBOX")
        {
            var opis = new List<string>();
            foreach (var f in r.Folderi.Where(x => r.Uid.ContainsKey(x.Folder)))
            {
                var fs = s.Folder(Stanje.KljucSanduceta(nalog, f.Folder));
                var bio = fs.UidValidity == f.UidValidity ? fs.PoslednjiUid : 0;
                var novi = Math.Max(bio, r.Uid[f.Folder]);
                opis.Add(f.Folder + ": poslednji obrađen UID " + (fs.UidValidity == f.UidValidity ? bio.ToString(CultureInfo.InvariantCulture) : "– (drugi UIDVALIDITY)") + " → " + novi
                         + (novi > r.Uid[f.Folder] ? " (agent je već bio dalje od arhive)" : ""));
                fs.UidValidity = f.UidValidity; fs.PoslednjiUid = novi; fs.NultaTacka = r.VremeArhive;
                if (string.Equals(f.Folder, dolazni, StringComparison.OrdinalIgnoreCase))
                {
                    s.NultaTackaUid = novi; s.NultaTackaUidValidity = f.UidValidity; s.NultaTackaNalog = nalog;
                }
            }
            s.NultaTacka = r.VremeArhive;
            return opis;
        }

        /// <summary>Komanda: nulta-tacka-arhiva [--suvo] [--max-min 60] [--bez-zadatka] [--zadatak "Saobracaj\Agent CimProvera"].</summary>
        internal static int Pokreni(AgentConfig cfg, Log log, List<string> a)
        {
            string Op(string ime) { var i = a.IndexOf(ime); return i > 0 && i + 1 < a.Count ? a[i + 1] : null; }
            var suvo = a.Contains("--suvo");
            var maxMin = int.TryParse(Op("--max-min"), out var mm) ? mm : 60;
            var zadatak = Op("--zadatak") ?? ResetStanja.PodrazumevaniZadatak;
            if (!a.Contains("--bez-zadatka"))
            {
                if (ResetStanja.Zadatak(zadatak, "/Query", "/XML", out var xml) != 0) { Console.Error.WriteLine("Zadatak „" + zadatak + "“ ne može da se pročita (schtasks) – nulta tačka nije postavljena."); return 1; }
                if (!xml.Contains("<Enabled>false</Enabled>")) { Console.Error.WriteLine("Zadatak obrade „" + zadatak + "“ je UKLJUČEN – nulta tačka se postavlja samo dok je isključen. Ništa nije promenjeno."); return 1; }
            }
            var koren = Program.ArhivaKoren(cfg);
            var izvF = Path.Combine(koren, "izvestaj.txt");
            if (!File.Exists(izvF)) { Console.Error.WriteLine("Nema " + izvF + " – prvo pokrenuti arhiviranje."); return 1; }
            var r = Izracunaj(File.ReadAllText(izvF, Encoding.UTF8), new Arhiva(koren).Procitaj(), DateTimeOffset.Now, TimeSpan.FromMinutes(maxMin), cfg.DolazniFolder);
            if (!r.Ispravan)
            {
                Console.Error.WriteLine("Nulta tačka NIJE postavljena:");
                foreach (var g in r.Greske) Console.Error.WriteLine("  - " + g);
                log.Upozorenje("Nulta tačka iz arhive nije postavljena: " + string.Join("; ", r.Greske));
                return 1;
            }
            using (var mutex = new Mutex(false, ResetStanja.ImeMuteksa))
            {
                bool imam;
                try { imam = mutex.WaitOne(TimeSpan.FromSeconds(30)); } catch (AbandonedMutexException) { imam = true; }
                if (!imam) { Console.Error.WriteLine("Agent upravo radi – nulta tačka nije postavljena (pokušati ponovo kada završi)."); return 1; }
                try
                {
                    var s = Stanje.Ucitaj(cfg.StanjeFolder);
                    var vreme = DateTimeOffset.Now;
                    string kopija = null;
                    if (!suvo)
                    {
                        kopija = Path.Combine(cfg.StanjeFolder, "arhiva", "pre-nulte-tacke-" + vreme.ToString("yyyy-MM-dd_HHmmss"));
                        Directory.CreateDirectory(kopija);
                        var st = Path.Combine(cfg.StanjeFolder, "stanje.json");
                        if (File.Exists(st)) File.Copy(st, Path.Combine(kopija, "stanje.json"));
                    }
                    var opis = Primeni(s, cfg.Nalog, r, cfg.DolazniFolder);
                    var inbox = r.Folderi.First(x => string.Equals(x.Folder, cfg.DolazniFolder, StringComparison.OrdinalIgnoreCase));
                    var red = vreme.ToString("yyyy-MM-dd HH:mm:ss zzz") + " – nulta tačka Test režima: " + cfg.Nalog + " " + inbox.Folder + " UID " + s.NultaTackaUid
                              + " (UIDVALIDITY " + inbox.UidValidity + "), " + string.Join(", ", r.Folderi.Where(x => x != inbox && r.Uid.ContainsKey(x.Folder)).Select(x => x.Folder + " UID " + s.Folder(Stanje.KljucSanduceta(cfg.Nalog, x.Folder)).PoslednjiUid))
                              + "; arhiva od " + r.VremeArhive.ToString("yyyy-MM-dd HH:mm zzz") + " (" + koren + ")" + (kopija != null ? "; rezervna kopija stanja: " + kopija : "");
                    Console.WriteLine((suvo ? "[SUVO – ništa nije upisano] " : "") + red);
                    foreach (var o in opis) Console.WriteLine("  " + o);
                    if (suvo) return 0;
                    s.Sacuvaj();
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Ime), red + Environment.NewLine, new UTF8Encoding(false));
                    log.Info(red + " | " + string.Join(" | ", opis));
                    return 0;
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
