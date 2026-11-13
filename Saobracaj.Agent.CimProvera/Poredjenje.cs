using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Search;
using MimeKit;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Prvi izveštaj za jedan mail obrađen pre reseta (samo metapodaci: bez teksta izveštaja i bez sadržaja dokumenata).</summary>
    internal sealed class PrviIzvestaj
    {
        public DateTimeOffset Vreme { get; set; }
        public string MessageId { get; set; }
        public string Folder { get; set; }
        public long Uid { get; set; }
        public string Status { get; set; }
        public int Greske { get; set; }
        public int Upozorenja { get; set; }
        /// <summary>ID pravila iz loga ("Nalazi: …").</summary>
        public List<string> Pravila { get; set; } = new List<string>();
        /// <summary>Prilozi maila (imena iz strukture poruke) i fajlovi iz ZIP-a (iz loga).</summary>
        public List<string> Fajlovi { get; set; } = new List<string>();
        public string IzvestajFolder { get; set; }
        public long IzvestajUidValidity { get; set; }
        public long IzvestajUid { get; set; }
        public string IzvestajNaslov { get; set; }
    }

    /// <summary>stanje\prvi_izvestaji.json: za svaki fajl prvi izveštaj (najraniji mail pre nulte tačke u kome je fajl bio).</summary>
    internal sealed class IndeksPrvih
    {
        public DateTimeOffset Izgradjen { get; set; }
        public DateTimeOffset? NultaTacka { get; set; }
        public List<PrviIzvestaj> Poruke { get; set; } = new List<PrviIzvestaj>();
        /// <summary>Normalizovano ime fajla → MessageId prvog maila.</summary>
        public Dictionary<string, string> Fajlovi { get; set; } = new Dictionary<string, string>();

        public const string Ime = "prvi_izvestaji.json";

        public static IndeksPrvih Ucitaj(string stanjeFolder)
        {
            var p = Path.Combine(stanjeFolder, Ime);
            try { return File.Exists(p) ? Json.Iz<IndeksPrvih>(File.ReadAllText(p, Encoding.UTF8)) : null; } catch { return null; }
        }

        public void Sacuvaj(string stanjeFolder) => File.WriteAllText(Path.Combine(stanjeFolder, Ime), Json.U(this), Encoding.UTF8);

        public static string Kljuc(string fajl) => Regex.Replace(Path.GetFileName((fajl ?? "").Trim()), @"\s+", " ").ToLowerInvariant();

        /// <summary>Prvi izveštaji za fajlove ovog maila (jedan mail pre reseta može da pokrije više fajlova).</summary>
        public List<(PrviIzvestaj prvi, List<string> fajlovi)> Za(IEnumerable<string> fajlovi)
        {
            var rez = new List<(PrviIzvestaj, List<string>)>();
            foreach (var f in fajlovi.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Path.GetFileName(x.Trim())).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Fajlovi.TryGetValue(Kljuc(f), out var id)) continue;
                var p = Poruke.FirstOrDefault(x => x.MessageId == id);
                if (p == null) continue;
                var postoji = rez.FindIndex(x => x.Item1 == p);
                if (postoji >= 0) rez[postoji].Item2.Add(f); else rez.Add((p, new List<string> { f }));
            }
            return rez;
        }
    }

    /// <summary>
    /// Poređenje novog izveštaja sa prvim izveštajem za isti fajl (samo režim Test). Prvi izveštaji su mailovi agenta
    /// (zaglavlje X-KP-CimProvera) u INBOX-u i poslatoj pošti, povezani sa originalnim mailom preko In-Reply-To;
    /// broj grešaka i ID pravila su iz loga. Tekst prvog izveštaja se čita iz sandučeta pri poređenju i ne čuva se na disku.
    /// </summary>
    internal static class Poredjenje
    {
        public const string ZaglavljeVrste = "X-KP-CimProvera-Vrsta";

        // ------------------------------------------------------------------------------------- stavke iz teksta izveštaja

        internal sealed class Stavka
        {
            public string Vrsta;      // "greška" | "upozorenje"
            public string Dokument;
            public string Rubrika;    // "16", "13.3" ili "" (opšti nalaz)
            public string Tekst;
            public string Kljuc => Vrsta + "|" + Rubrika;
            public override string ToString() => Vrsta + (Rubrika.Length > 0 ? ", rubrika " + Rubrika : "") + (string.IsNullOrEmpty(Dokument) ? "" : " [" + Dokument + "]") + " – " + Tekst;
        }

        private static readonly Regex Sekcija = new Regex(@"^\s*(GREŠKE|GRESKE|ERRORS|UPOZORENJA|WARNINGS|POTREBNE ISPRAVKE|CORRECTIONS REQUIRED|NAPOMENE|REMARKS|PREPORUKE|RECOMMENDATIONS|PRILOZI KOJI|ATTACHMENTS NOT)\b", RegexOptions.IgnoreCase);
        private static readonly Regex Red = new Regex(@"^\s*\d+[.)]\s+(?:\[(?<dok>[^\]]*)\]\s*)?(?<tekst>.+)$");
        private static readonly Regex RubrikaU = new Regex(@"^(?:Rubrika|Box|Feld|rubr\.)\s+(?<r>\d+(?:[.,]\d+)?[a-z]?)\s*(?:[–\-:]\s*)?", RegexOptions.IgnoreCase);
        /// <summary>Novi oblik (v2.7): ispod stavke „   Šta nije u redu: [dokument] opis“.</summary>
        private static readonly Regex StaRed = new Regex(@"^\s+(?:Šta nije u redu|Issue):\s*(?:\[(?<dok>[^\]]*)\]\s*)?(?<tekst>.*)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Greške i upozorenja iz teksta izveštaja agenta: raniji oblik (GREŠKE / UPOZORENJA, „N. [dokument] Rubrika X – opis“)
        /// i oblik v2.7 (POTREBNE ISPRAVKE / NAPOMENE, „N. Rubrika X – naziv“ + „Šta nije u redu: …“; napomene uključuju i preporuke).
        /// </summary>
        public static List<Stavka> Stavke(string telo)
        {
            var rez = new List<Stavka>();
            string vrsta = null;
            foreach (var linija in (telo ?? "").Replace("\r", "").Split('\n'))
            {
                if (linija.Trim() == "--") break;
                var s = Sekcija.Match(linija);
                if (s.Success)
                {
                    var n = s.Groups[1].Value.ToUpperInvariant();
                    vrsta = n.StartsWith("GRE") || n == "ERRORS" || n.StartsWith("POTREBNE") || n.StartsWith("CORRECTIONS") ? "greška"
                          : n == "UPOZORENJA" || n == "WARNINGS" || n == "NAPOMENE" || n == "REMARKS" ? "upozorenje" : null;
                    continue;
                }
                if (vrsta == null) continue;
                var st = StaRed.Match(linija);
                if (st.Success && rez.Count > 0)
                {
                    rez[rez.Count - 1].Tekst = st.Groups["tekst"].Value.Trim();
                    if (st.Groups["dok"].Success) rez[rez.Count - 1].Dokument = st.Groups["dok"].Value.Trim();
                    continue;
                }
                var m = Red.Match(linija);
                if (!m.Success) continue;
                var tekst = m.Groups["tekst"].Value.Trim();
                var rm = RubrikaU.Match(tekst);
                var rub = rm.Success ? rm.Groups["r"].Value.Replace(',', '.') : "";
                if (rm.Success) tekst = tekst.Substring(rm.Length).Trim();
                rez.Add(new Stavka { Vrsta = vrsta, Dokument = m.Groups["dok"].Value.Trim(), Rubrika = rub, Tekst = tekst });
            }
            return rez;
        }

        /// <summary>Stavke koje se odnose na neki od fajlova (stavke bez naziva dokumenta ostaju).</summary>
        public static List<Stavka> ZaFajlove(List<Stavka> stavke, ICollection<string> fajlovi) =>
            stavke.Where(s => string.IsNullOrEmpty(s.Dokument) || fajlovi.Any(f => s.Dokument.IndexOf(Path.GetFileName(f), StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

        private static string Norm(string t) => Regex.Replace((t ?? "").ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();

        /// <summary>
        /// Nove i nestale stavke: ista vrsta i rubrika = ista stavka (tekst poruke se između verzija menjao). Kada u istoj
        /// rubrici ima više stavki, prvo se uparuju one sa istim tekstom, a višak je nov ili nestao.
        /// </summary>
        public static (List<Stavka> nove, List<Stavka> nestale) Razlike(List<Stavka> pre, List<Stavka> sada)
        {
            var nove = new List<Stavka>(); var nestale = new List<Stavka>();
            foreach (var k in pre.Select(x => x.Kljuc).Union(sada.Select(x => x.Kljuc)).Distinct())
            {
                var p = pre.Where(x => x.Kljuc == k).ToList();
                var s = sada.Where(x => x.Kljuc == k).ToList();
                foreach (var x in s.ToList())
                {
                    var par = p.FirstOrDefault(y => Norm(y.Tekst) == Norm(x.Tekst));
                    if (par != null) { p.Remove(par); s.Remove(x); }
                }
                int isti = Math.Min(p.Count, s.Count);
                nestale.AddRange(p.Skip(isti));
                nove.AddRange(s.Skip(isti));
            }
            return (nove, nestale);
        }

        // ------------------------------------------------------------------------------------------ tekst poređenja

        internal sealed class PrviZaFajl
        {
            public PrviIzvestaj Prvi;
            public List<string> Fajlovi;
            /// <summary>Stavke prvog izveštaja (null = tekst prvog izveštaja nije pronađen u sandučetu).</summary>
            public List<Stavka> StavkePre;
        }

        /// <summary>Odeljak „POREĐENJE SA PRVIM IZVEŠTAJEM“ za test izveštaj. <paramref name="sadaTelo"/> = null kada sada nema izveštaja.</summary>
        public static string Tekst(List<PrviZaFajl> prvi, string sadaOpis, int? greskeSada, int? upozorenjaSada, IEnumerable<string> pravilaSada, string sadaTelo,
            out string zaLog)
        {
            var sb = new StringBuilder();
            var log = new List<string>();
            sb.AppendLine("POREĐENJE SA PRVIM IZVEŠTAJEM (režim Test)");
            var stavkeSada = sadaTelo == null ? new List<Stavka>() : Stavke(sadaTelo);
            var prSada = new HashSet<string>(pravilaSada ?? Enumerable.Empty<string>());
            foreach (var p in prvi)
            {
                var a = p.Prvi;
                sb.AppendLine();
                sb.AppendLine("Fajl: " + string.Join(", ", p.Fajlovi));
                sb.AppendLine("Prvi izveštaj: " + a.Vreme.ToString("dd.MM.yyyy HH:mm") + " (UID " + a.Uid + (string.IsNullOrEmpty(a.IzvestajNaslov) ? "" : ", „" + a.IzvestajNaslov + "“") + ")");
                var ostali = a.Fajlovi.Where(f => !p.Fajlovi.Any(x => string.Equals(Path.GetFileName(x), Path.GetFileName(f), StringComparison.OrdinalIgnoreCase))).ToList();
                if (ostali.Count > 0) sb.AppendLine("  (prvi mail je imao i: " + string.Join(", ", ostali.Take(8)) + (ostali.Count > 8 ? " i još " + (ostali.Count - 8) : "") + " – brojevi su za ceo taj mail, stavke samo za ovaj fajl)");
                var bioIzvestaj = a.Status == "Greske" || a.Status == "Ispravan" || (a.Status ?? "").StartsWith("Greske+") || (a.Status ?? "").StartsWith("Ispravan+");
                var pre = bioIzvestaj ? a.Greske + " grešaka, " + a.Upozorenja + " upozorenja" : "bez izveštaja (" + OpisStatusa(a.Status) + ")";
                var sad = greskeSada.HasValue ? greskeSada + " grešaka, " + upozorenjaSada + " upozorenja" : sadaOpis;
                sb.AppendLine("Pre: " + pre);
                sb.AppendLine("Sada: " + sad);
                log.Add("UID " + a.Uid + " " + (bioIzvestaj ? a.Greske + "/" + a.Upozorenja : a.Status) + " → " + (greskeSada.HasValue ? greskeSada + "/" + upozorenjaSada : sadaOpis));

                if (a.Pravila.Count > 0 || prSada.Count > 0)
                {
                    var novaPr = prSada.Except(a.Pravila).OrderBy(x => x, StringComparer.Ordinal).ToList();
                    var nestalaPr = a.Pravila.Except(prSada).OrderBy(x => x, StringComparer.Ordinal).ToList();
                    if (a.Pravila.Count > 0)
                        sb.AppendLine("Pravila – nova: " + (novaPr.Count > 0 ? string.Join(", ", novaPr) : "nema") + "; nestala: " + (nestalaPr.Count > 0 ? string.Join(", ", nestalaPr) : "nema"));
                }

                if (!bioIzvestaj && sadaTelo == null) continue;
                if (bioIzvestaj && p.StavkePre == null)
                {
                    sb.AppendLine("Tekst prvog izveštaja nije pronađen u sandučetu – stavke ne mogu da se uporede.");
                    continue;
                }
                var predm = ZaFajlove(p.StavkePre ?? new List<Stavka>(), p.Fajlovi);
                var sadm = ZaFajlove(stavkeSada, p.Fajlovi);
                if (ostali.Count > 0)
                    sb.AppendLine("Stavke za ovaj fajl: greške " + predm.Count(x => x.Vrsta == "greška") + " → " + sadm.Count(x => x.Vrsta == "greška")
                                  + ", upozorenja " + predm.Count(x => x.Vrsta == "upozorenje") + " → " + sadm.Count(x => x.Vrsta == "upozorenje"));
                var (nove, nestale) = Razlike(predm, sadm);
                sb.AppendLine("Nove stavke (" + nove.Count + ")" + (nove.Count == 0 ? "" : ":"));
                foreach (var x in nove) sb.AppendLine("  + " + x);
                sb.AppendLine("Nestale stavke (" + nestale.Count + ")" + (nestale.Count == 0 ? "" : ":"));
                foreach (var x in nestale) sb.AppendLine("  − " + x);
                log[log.Count - 1] += ", nove stavke " + nove.Count + ", nestale " + nestale.Count;
            }
            zaLog = string.Join("; ", log);
            return sb.ToString();
        }

        private static string OpisStatusa(string s)
        {
            switch ((s ?? "").Split('+')[0])
            {
                case "NemaCim": return "nije pronađen tovarni list";
                case "SamoSpisak": return "samo spisak kola";
                case "NeprocitanoRucnaProvera": return "prilozi nisu pročitani";
                case "Greska": return "obrada nije uspela";
                case "BezPriloga": return "bez priloga";
                default: return s ?? "nepoznato";
            }
        }

        // ------------------------------------------------------------------------------------------ izgradnja indeksa

        private static readonly Regex LogRed = new Regex(@"^\S+ \S+ \S+ (?:INFO|WARN |ERROR)\s+(?<t>.*)$");

        private sealed class IzLoga
        {
            public string Folder, MessageId;
            public long Uid;
            public List<string> Fajlovi = new List<string>();
            public List<string> Pravila = new List<string>();
            public int? Greske, Upozorenja;
            public string Naslov;
        }

        /// <summary>Iz logova: Message-ID, prilozi (i fajlovi iz ZIP-a), broj grešaka/upozorenja, ID pravila i naslov izveštaja.</summary>
        private static Dictionary<string, IzLoga> CitajLogove(string logFolder)
        {
            var rez = new Dictionary<string, IzLoga>();
            if (!Directory.Exists(logFolder)) return rez;
            string folder = "INBOX";
            IzLoga tek = null;
            var poUid = new Dictionary<string, IzLoga>();
            foreach (var f in Directory.GetFiles(logFolder, AgentConfig.AgentNaziv + "-*.log").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string[] linije;
                try { using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) using (var sr = new StreamReader(fs, Encoding.UTF8)) linije = sr.ReadToEnd().Split('\n'); }
                catch { continue; }
                foreach (var l0 in linije)
                {
                    var lm = LogRed.Match(l0.TrimEnd('\r'));
                    if (!lm.Success) continue;
                    var t = lm.Groups["t"].Value;
                    Match m;
                    if ((m = Regex.Match(t, @"^(\S+) \((dolazni|odlazni)\): novih poruka")).Success) { folder = m.Groups[1].Value; tek = null; continue; }
                    if ((m = Regex.Match(t, @"^UID (\d+) (\S+): \d+ priloga")).Success)
                    {
                        tek = new IzLoga { Folder = folder, Uid = long.Parse(m.Groups[1].Value), MessageId = m.Groups[2].Value };
                        if (!rez.ContainsKey(tek.MessageId)) rez[tek.MessageId] = tek; else tek = rez[tek.MessageId];
                        poUid[folder + "/" + tek.Uid] = tek;
                        continue;
                    }
                    if (tek != null && (m = Regex.Match(t, @"^prilog (.+?) \((?:Pdf|Slika|Excel|Tekst|Word|Msg|Eml|\w+)\):")).Success)
                    {
                        if (!tek.Fajlovi.Contains(m.Groups[1].Value)) tek.Fajlovi.Add(m.Groups[1].Value);
                        continue;
                    }
                    if ((m = Regex.Match(t, @"^UID (\d+): .*?grešaka (\d+), upozorenja (\d+).*?(?:Nalazi: (.*))?$")).Success
                        && poUid.TryGetValue(folder + "/" + m.Groups[1].Value, out var x))
                    {
                        if (x.Greske == null)
                        {
                            x.Greske = int.Parse(m.Groups[2].Value); x.Upozorenja = int.Parse(m.Groups[3].Value);
                            x.Pravila = m.Groups[4].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
                        }
                        continue;
                    }
                    if (tek != null && tek.Naslov == null && (m = Regex.Match(t, @"^izveštaj poslat na [^:]+: (.*)$")).Success) tek.Naslov = m.Groups[1].Value.Trim();
                }
            }
            return rez;
        }

        /// <summary>
        /// Sastavlja indeks prvih izveštaja posle reseta: mailovi iz rezervne kopije stanja (pre nulte tačke), njihovi prilozi
        /// (struktura poruke, bez preuzimanja sadržaja), podaci iz loga i izveštaj agenta (In-Reply-To). Sve samo za čitanje.
        /// </summary>
        public static IndeksPrvih Izgradi(AgentConfig cfg, Saobracaj.Agent.CimProvera.Stanje stanje, IEnumerable<IMailFolder> folderi, Log log)
        {
            var nulta = stanje.NultaTacka ?? DateTimeOffset.Now;
            var ind = new IndeksPrvih { Izgradjen = DateTimeOffset.Now, NultaTacka = stanje.NultaTacka };
            Saobracaj.Agent.CimProvera.Stanje stara = null;
            if (!string.IsNullOrEmpty(stanje.ResetArhiva))
            {
                var p = Path.Combine(stanje.ResetArhiva, "stanje.json");
                if (File.Exists(p)) stara = Json.Iz<Saobracaj.Agent.CimProvera.Stanje>(File.ReadAllText(p, Encoding.UTF8));
            }
            var izLoga = CitajLogove(cfg.LogFolder);
            var folderiPoImenu = folderi.ToDictionary(f => f.FullName, f => f, StringComparer.OrdinalIgnoreCase);

            // Izveštaji agenta pre nulte tačke: In-Reply-To → najraniji izveštaj.
            var izvestaji = new Dictionary<string, (IMailFolder f, UniqueId uid, DateTimeOffset vreme, string naslov)>();
            foreach (var f in folderiPoImenu.Values)
            {
                f.Open(FolderAccess.ReadOnly);
                var uids = f.Search(SearchQuery.HeaderContains(Slanje.ZaglavljeAgenta, "1").And(SearchQuery.DeliveredBefore(nulta.LocalDateTime.Date.AddDays(1))));
                if (uids.Count == 0) continue;
                foreach (var s in f.Fetch(uids, new FetchRequest(MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate | MessageSummaryItems.Headers)))
                {
                    var vreme = s.InternalDate ?? s.Envelope?.Date ?? DateTimeOffset.MinValue;
                    if (vreme >= nulta) continue;
                    var vrsta = s.Headers?[ZaglavljeVrste] ?? "";
                    var naslov = s.Envelope?.Subject ?? "";
                    if (vrsta.Length > 0 && vrsta != "izvestaj") continue;
                    if (naslov.StartsWith("[CimProvera] Upozorenje", StringComparison.OrdinalIgnoreCase) || naslov.StartsWith("[INTERNO]")) continue;
                    var na = (s.Envelope?.InReplyTo ?? "").Trim().Trim('<', '>');
                    if (na.Length == 0) continue;
                    if (!izvestaji.TryGetValue(na, out var post) || vreme < post.vreme) izvestaji[na] = (f, s.UniqueId, vreme, naslov);
                }
            }

            // Obrađeni mailovi pre reseta: iz rezervne kopije stanja, dopunjeno logom.
            var poruke = new Dictionary<string, PrviIzvestaj>();
            if (stara != null)
                foreach (var kv in stara.Poruke)
                {
                    var ps = kv.Value;
                    if (ps.Status == "Preskoceno" || ps.Status == "BezPriloga") continue;
                    var id = Regex.Replace(kv.Key, @"^[^|@\s]+@[^|\s]+\|", "");   // ključ po sanduču (v2.7.1): „nalog|Message-ID“
                    poruke[id] = new PrviIzvestaj { MessageId = id, Vreme = ps.Vreme, Folder = ps.Folder, Uid = ps.Uid, Status = ps.Status, Greske = ps.Greske, Upozorenja = ps.Upozorenja };
                }
            foreach (var x in izLoga.Values)
                if (!poruke.ContainsKey(x.MessageId))
                    poruke[x.MessageId] = new PrviIzvestaj { MessageId = x.MessageId, Folder = x.Folder, Uid = x.Uid, Status = x.Greske.HasValue ? (x.Greske > 0 ? "Greske" : "Ispravan") : "?",
                                                             Greske = x.Greske ?? 0, Upozorenja = x.Upozorenja ?? 0 };

            foreach (var p in poruke.Values)
            {
                if (izLoga.TryGetValue(p.MessageId, out var l))
                {
                    p.Pravila = l.Pravila;
                    foreach (var f in l.Fajlovi) if (!p.Fajlovi.Contains(f)) p.Fajlovi.Add(f);
                    p.IzvestajNaslov = l.Naslov;
                }
                if (izvestaji.TryGetValue(p.MessageId, out var iz))
                {
                    p.IzvestajFolder = iz.f.FullName; p.IzvestajUidValidity = iz.f.UidValidity; p.IzvestajUid = iz.uid.Id;
                    if (string.IsNullOrEmpty(p.IzvestajNaslov)) p.IzvestajNaslov = iz.naslov;
                }
                // Imena priloga iz strukture originalnog maila (bez preuzimanja sadržaja).
                if (p.Uid > 0 && folderiPoImenu.TryGetValue(p.Folder ?? "", out var fo))
                {
                    try
                    {
                        fo.Open(FolderAccess.ReadOnly);
                        var uidValid = stara != null && stara.Folderi.TryGetValue(fo.FullName, out var fs) ? fs.UidValidity : fo.UidValidity;
                        if (uidValid == fo.UidValidity)
                        {
                            var s = fo.Fetch(new[] { new UniqueId((uint)p.Uid) }, new FetchRequest(MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure | MessageSummaryItems.InternalDate)).FirstOrDefault();
                            if (s != null && string.Equals((s.Envelope?.MessageId ?? "").Trim('<', '>'), p.MessageId, StringComparison.Ordinal))
                            {
                                foreach (var b in s.BodyParts.Where(b => !string.IsNullOrEmpty(b.FileName)))
                                    if (!p.Fajlovi.Contains(b.FileName)) p.Fajlovi.Insert(0, b.FileName);
                                if (p.Vreme == default(DateTimeOffset)) p.Vreme = s.InternalDate ?? s.Envelope?.Date ?? p.Vreme;
                            }
                        }
                    }
                    catch (Exception ex) { log?.Upozorenje("Prvi izveštaji: struktura maila UID " + p.Uid + " nije pročitana: " + ex.Message); }
                }
                ind.Poruke.Add(p);
            }

            foreach (var p in ind.Poruke.OrderBy(x => x.Vreme))
                foreach (var f in p.Fajlovi)
                {
                    var k = IndeksPrvih.Kljuc(f);
                    if (k.Length > 0 && !ind.Fajlovi.ContainsKey(k)) ind.Fajlovi[k] = p.MessageId;
                }
            log?.Info("Prvi izveštaji: " + ind.Poruke.Count + " mailova pre nulte tačke, " + ind.Fajlovi.Count + " fajlova, izveštaja agenta pronađeno " + ind.Poruke.Count(x => x.IzvestajUid > 0) + ".");
            return ind;
        }

        /// <summary>Tekst prvog izveštaja iz sandučeta (samo za čitanje), parsiran u stavke; null ako nije pronađen.</summary>
        public static List<Stavka> StavkePrvog(PrviIzvestaj p, Func<string, IMailFolder> folder)
        {
            if (p.IzvestajUid <= 0 || string.IsNullOrEmpty(p.IzvestajFolder)) return null;
            var f = folder(p.IzvestajFolder);
            if (f == null) return null;
            f.Open(FolderAccess.ReadOnly);
            if (p.IzvestajUidValidity != 0 && f.UidValidity != p.IzvestajUidValidity) return null;
            var m = f.GetMessage(new UniqueId((uint)p.IzvestajUid));
            var telo = m.TextBody;
            if (string.IsNullOrEmpty(telo) && !string.IsNullOrEmpty(m.HtmlBody)) telo = HtmlUTekst(m.HtmlBody);
            return Stavke(telo);
        }

        /// <summary>HTML izveštaja (tabele) u tekst istog oblika kao tekstualni izveštaj: „N. [dokument] Rubrika X – opis“.</summary>
        internal static string HtmlUTekst(string html)
        {
            var sb = new StringBuilder();
            string Cist(string s) => Regex.Replace(System.Net.WebUtility.HtmlDecode(Regex.Replace(s, "<[^>]+>", " ")), @"[\s ]+", " ").Trim();
            foreach (Match blok in Regex.Matches(html, @"<h3[^>]*>(?<h>.*?)</h3>|<tr>(?<tr>.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                if (blok.Groups["h"].Success) { sb.AppendLine(Cist(blok.Groups["h"].Value)); continue; }
                var td = Regex.Matches(blok.Groups["tr"].Value, @"<td[^>]*>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase).Cast<Match>().Select(x => Cist(x.Groups[1].Value)).ToList();
                if (td.Count < 3) continue;
                if (Regex.IsMatch(td[0], @"^\d+$")) td.RemoveAt(0);                  // tabela grešaka: prva kolona je redni broj
                if (td.Count == 3)
                {
                    // oblik v2.7: Rubrika („16 – Preuzimanje na prevoz“) | Šta nije u redu | Kako treba da izgleda
                    sb.AppendLine("1. Rubrika " + td[0]);
                    sb.AppendLine("   Šta nije u redu: " + td[1]);
                    continue;
                }
                var rub = td.Count > 1 && Regex.IsMatch(td[1], @"^\d+(?:\.\d+)?[a-z]?$") ? "Rubrika " + td[1] + " – " : "";
                sb.AppendLine("1. [" + td[0] + "] " + rub + td.Last());
            }
            return sb.ToString();
        }
    }
}
