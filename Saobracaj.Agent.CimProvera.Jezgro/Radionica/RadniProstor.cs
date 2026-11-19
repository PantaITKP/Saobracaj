using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    internal sealed class Beleska { public string Ime = "", Tekst = ""; }

    /// <summary>Poruka u radnom prostoru (indeks.json).</summary>
    internal sealed class StavkaProstora
    {
        public string Kljuc = "";
        /// <summary>Kopija poruke u radnom prostoru (poruke\…eml).</summary>
        public string Fajl = "";
        public string Izvor = "", Putanja = "", Od = "", Naslov = "", MessageId = "", Nit = "";
        public DateTimeOffset Datum;
        public List<string> Prilozi = new List<string>();
        public List<Beleska> Beleske = new List<Beleska>();
        /// <summary>Verzija → relativni folder rezultata (rezultati\verzija\…).</summary>
        public Dictionary<string, string> Rezultati = new Dictionary<string, string>();
        public string Status = "", Odluka = "";
        public DateTime? Uvezeno;
    }

    internal sealed class IndeksProstora
    {
        public List<StavkaProstora> Stavke = new List<StavkaProstora>();
    }

    /// <summary>Podešavanja alata za obradu (online/offline, konekcija, verzija rezultata).</summary>
    internal sealed class PodesavanjaAlata
    {
        /// <summary>Online: upis u planiranje.tmp* (kao agent). Offline: upis se ne radi, predlog posla iz lokalnih šifarnika ide u rezultat.json.</summary>
        public bool Online;
        /// <summary>Konekcija (samo u memoriji – iz DPAPI fajla alata ili CIMPROVERA_KONEKCIJA); nikad u rezultat, log ili paket.</summary>
        public string Konekcija;
        public string Prefiks = UpisUBazu.PodrazumevaniPrefiks;
        /// <summary>Oznaka verzije rezultata (folder rezultati\&lt;verzija&gt;); podrazumevano verzija programa.</summary>
        public string Verzija = RezultatJson.VerzijaPrograma;
        /// <summary>Zajednički keš čitanja (C:\CimRadionica\kes).</summary>
        public string KesFolder;
        public List<string> KopijaNa = new List<string> { "priprema@kprevoz.co.rs" };
    }

    /// <summary>
    /// v3.0 (2.1–2.3, 2.11): radni prostor CimRadionica (podrazumevano C:\CimRadionica\Radni\&lt;ime&gt;): poruke (kopije .eml), indeks,
    /// stanje (dosije, ponavljanja – posebno za svaki radni prostor; stanje agenta se nikad ne dira), rezultati po verzijama, Zbirni Excel,
    /// lokalna kopija šifarnika (offline).
    /// </summary>
    internal sealed class RadniProstor
    {
        public readonly string Koren;
        public IndeksProstora Indeks = new IndeksProstora();
        public string Ime => Path.GetFileName(Koren.TrimEnd('\\', '/'));
        public string Poruke => Path.Combine(Koren, "poruke");
        public string StanjeFolder => Path.Combine(Koren, "stanje");
        public string Rezultati => Path.Combine(Koren, "rezultati");
        public string LogFolder => Path.Combine(Koren, "logs");
        private string IndeksFajl => Path.Combine(Koren, "indeks.json");

        private RadniProstor(string koren) { Koren = koren; }

        public static RadniProstor Otvori(string koren)
        {
            var rp = new RadniProstor(Path.GetFullPath(koren));
            foreach (var d in new[] { rp.Koren, rp.Poruke, rp.StanjeFolder, rp.Rezultati, rp.LogFolder }) Directory.CreateDirectory(d);
            if (File.Exists(rp.IndeksFajl))
                rp.Indeks = JsonConvert.DeserializeObject<IndeksProstora>(File.ReadAllText(rp.IndeksFajl, Encoding.UTF8)) ?? new IndeksProstora();
            return rp;
        }

        public void Sacuvaj()
        {
            var tmp = IndeksFajl + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(Indeks, Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(IndeksFajl)) File.Delete(IndeksFajl);
            File.Move(tmp, IndeksFajl);
        }

        /// <summary>Poruke hronološki (po datumu slanja).</summary>
        public List<StavkaProstora> Hronoloski() => Indeks.Stavke.OrderBy(s => s.Datum).ThenBy(s => s.Kljuc, StringComparer.Ordinal).ToList();

        /// <summary>Redni broj poruke (1…) po datumu slanja – NNN u _Analiza.</summary>
        public int Rb(StavkaProstora s) => Hronoloski().FindIndex(x => x.Kljuc == s.Kljuc) + 1;   // po ključu: i za stavku iz ranije otvorenog prostora

        /// <summary>Uvoz putanja (fajl/folder/zip); ista poruka (isti ključ) se ne uvozi dvaput. Vraća nove stavke.</summary>
        public List<StavkaProstora> Uvezi(IEnumerable<string> putanje, List<string> greske = null)
        {
            greske = greske ?? new List<string>();
            var nove = new List<StavkaProstora>();
            foreach (var put in putanje)
                foreach (var p in UvozPoruka.Uvezi(put, greske))
                {
                    var postoji = Indeks.Stavke.FirstOrDefault(x => x.Kljuc == p.Kljuc);
                    if (postoji != null)
                    {
                        // beleške se osvežavaju (korisnik ih dopisuje)
                        postoji.Beleske = p.Beleske.Select(b => new Beleska { Ime = b.ime, Tekst = b.tekst }).ToList();
                        continue;
                    }
                    var ime = BezbednoIme(p.Datum.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "_" + p.Naslov, 60) + "_" + Kratko(p.Kljuc) + ".eml";
                    using (var f = File.Create(Path.Combine(Poruke, ime))) p.Poruka.WriteTo(f);
                    var s = new StavkaProstora
                    {
                        Kljuc = p.Kljuc, Fajl = ime, Izvor = p.Izvor, Putanja = p.Putanja, Od = p.Od, Naslov = p.Naslov, MessageId = p.MessageId, Nit = p.Nit,
                        Datum = p.Datum, Prilozi = p.Prilozi, Beleske = p.Beleske.Select(b => new Beleska { Ime = b.ime, Tekst = b.tekst }).ToList(), Uvezeno = DateTime.Now,
                    };
                    Indeks.Stavke.Add(s);
                    nove.Add(s);
                }
            Sacuvaj();
            return nove;
        }

        public MimeMessage Ucitaj(StavkaProstora s) => MimeMessage.Load(Path.Combine(Poruke, s.Fajl));

        public UlaznaPoruka Ulaz(StavkaProstora s) => new UlaznaPoruka { Poruka = Ucitaj(s), Uid = Rb(s), Folder = "RADIONICA:" + Ime, Kljuc = "radionica|" + s.Kljuc, Izvor = s.Izvor };

        /// <summary>Niti radnog prostora (ključ niti + reference), svaka hronološki.</summary>
        public List<List<StavkaProstora>> Niti()
        {
            var up = Indeks.Stavke.Select(s => (s, p: new UvezenaPoruka { Kljuc = s.Kljuc, MessageId = s.MessageId, Nit = s.Nit, Datum = s.Datum, Poruka = SamoZaglavlja(s) })).ToList();
            return UvozPoruka.Niti(up.Select(x => x.p)).Select(g => g.Select(p => up.First(x => x.p == p).s).ToList()).ToList();
        }

        private MimeMessage SamoZaglavlja(StavkaProstora s)
        {
            try
            {
                using (var f = File.OpenRead(Path.Combine(Poruke, s.Fajl)))
                {
                    var parser = new MimeParser(f, MimeFormat.Entity);
                    var h = parser.ParseHeaders();
                    var m = new MimeMessage();
                    foreach (var x in h) if (x.Id == HeaderId.References || x.Id == HeaderId.InReplyTo || x.Id == HeaderId.MessageId) m.Headers.Add(x);
                    return m;
                }
            }
            catch { return new MimeMessage(); }
        }

        /// <summary>„Kreni ispočetka“: briše stanje radnog prostora (dosije, ponavljanja, upozorenja) – keš čitanja ostaje; rezultati ostaju (po verzijama).</summary>
        public void KreniIspocetka()
        {
            foreach (var f in Directory.GetFiles(StanjeFolder)) File.Delete(f);
            foreach (var d in Directory.GetDirectories(StanjeFolder).Where(d => !Path.GetFileName(d).Equals("kes", StringComparison.OrdinalIgnoreCase))) Directory.Delete(d, true);
            foreach (var s in Indeks.Stavke) { s.Status = ""; s.Odluka = ""; }
            Sacuvaj();
        }

        /// <summary>v3.0: status maila koji nije obrađen zbog greške okruženja (pad radnika, blokiran DLL) – vidi se u Excel-u i _Analiza.</summary>
        public const string StatusNeuspeh = "NijeObradjen";

        public static string OdlukaNeuspeha(string razlog) =>
            "nije obrađen – " + (Stanje.GreskaOkruzenja(razlog) ?? ((razlog ?? "").StartsWith("radnik") ? "greška okruženja (radnik nije završio obradu)" : "greška obrade"));

        /// <summary>
        /// v3.0: posle dela obrade (proces radnika) – poruke dela za koje radnik nije javio ishod dobijaju zapis „nije obrađen“
        /// (i analizu, ako se pravi). Vraća te poruke (prazno kada je sve obrađeno).
        /// </summary>
        public static List<StavkaProstora> ZabeleziNeobradjene(string koren, IEnumerable<string> kljuceviDela, ICollection<string> javljeni, string verzija, string razlog, bool online, bool analiza)
        {
            var rp = Otvori(koren);
            var rez = new List<StavkaProstora>();
            foreach (var k in kljuceviDela.Where(k => !javljeni.Contains(k)))
            {
                var s = rp.Indeks.Stavke.FirstOrDefault(x => x.Kljuc == k);
                if (s == null) continue;
                var folder = rp.ZabeleziNeuspeh(s, verzija, razlog ?? "radnik nije javio ishod obrade", online);
                if (analiza) Analiza.NapisiNeuspeh(rp.Koren, verzija, rp.Rb(s), s, folder);
                rez.Add(s);
            }
            if (rez.Count > 0) rp.Sacuvaj();
            return rez;
        }

        /// <summary>
        /// v3.0: mail nije obrađen (radnik je pao pre ili tokom obrade) – rezultat.json sa statusom „NijeObradjen“ zamenjuje eventualni
        /// stariji rezultat iste verzije (da se u Excel-u ne bi pojavio stari rezultat kao novi). Nikad tiho.
        /// </summary>
        public string ZabeleziNeuspeh(StavkaProstora s, string verzija, string razlog, bool online)
        {
            var folder = FolderRezultata(verzija, s);
            Directory.CreateDirectory(folder);
            foreach (var f in Directory.GetFiles(folder)) File.Delete(f);
            var odluka = OdlukaNeuspeha(razlog);
            var j = new JObject
            {
                ["sema"] = RezultatJson.VerzijaSeme,
                ["verzija"] = new JObject { ["program"] = RezultatJson.VerzijaPrograma, ["pravila"] = "", ["citac"] = LokalniCitac.VerzijaCitaca },
                ["rezim"] = online ? "online" : "offline",
                ["obradjeno"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["poruka"] = new JObject
                {
                    ["izvor"] = s.Izvor, ["uid"] = Rb(s), ["folder"] = "RADIONICA:" + Ime, ["message_id"] = s.MessageId ?? "",
                    ["datum"] = s.Datum.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture), ["od"] = s.Od ?? "", ["naslov"] = s.Naslov ?? "",
                    ["prilozi"] = new JArray(s.Prilozi.Select(p => new JObject { ["ime"] = p })),
                },
                ["korak"] = "NijeObradjena", ["status"] = StatusNeuspeh, ["odluka"] = odluka, ["greska"] = razlog ?? "",
                ["odgovor"] = new JArray(), ["dogadjaji"] = new JArray(), ["trajanje"] = new JObject { ["ukupno"] = 0 },
            };
            File.WriteAllText(Path.Combine(folder, "rezultat.json"), RezultatJson.U(j), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "log.txt"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + odluka + Environment.NewLine + (razlog ?? ""), new UTF8Encoding(false));
            s.Status = StatusNeuspeh; s.Odluka = odluka;
            s.Rezultati[verzija] = folder.Substring(Koren.Length).TrimStart('\\');
            return folder;
        }

        /// <summary>v3.0: poruke čiji rezultat verzije je „nije obrađen“ (greška okruženja ili privremena greška obrade) – „obradi ponovo neuspele“.</summary>
        public List<StavkaProstora> Neuspeli(string verzija) => Hronoloski().Where(s =>
        {
            if (!s.Rezultati.TryGetValue(verzija, out var rel)) return false;
            var f = Path.Combine(Koren, rel, "rezultat.json");
            if (!File.Exists(f)) return true;
            try { var j = JObject.Parse(File.ReadAllText(f, Encoding.UTF8)); return (string)j["status"] == StatusNeuspeh || (string)j["korak"] == nameof(Korak.PrivremenaGreska); }
            catch { return true; }
        }).ToList();

        public string FolderRezultata(string verzija, StavkaProstora s) =>
            Path.Combine(Rezultati, BezbednoIme(verzija, 30), BezbednoIme(s.Datum.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + "_" + s.Naslov, 70) + "_" + Kratko(s.Kljuc));

        /// <summary>Konfiguracija za obradu: režim Live (odgovor se priprema kao u Live režimu, ali se ne šalje), stanje radnog prostora.</summary>
        public AgentConfig Konfiguracija(PodesavanjaAlata pa)
        {
            var nv = new NameValueCollection
            {
                ["CitajSanduce"] = Slanje.TestnaAdresa, ["From"] = Slanje.TestnaAdresa, ["ReplyTo"] = "priprema@kprevoz.co.rs", ["TestPrimalac"] = Slanje.TestnaAdresa,
                ["Rezim"] = "Live", ["KopijaNa"] = string.Join(",", pa.KopijaNa), ["UpozorenjaNa"] = Slanje.TestnaAdresa,
                ["StanjeFolder"] = StanjeFolder, ["LogFolder"] = LogFolder, ["RezultatiFolder"] = "", ["UpisUBazu"] = pa.Online ? "true" : "false", ["IzvorUpisa"] = "Alat",
            };
            var cfg = AgentConfig.Ucitaj(nv);
            cfg.Lozinka = null;
            cfg.Konekcija = pa.Online ? pa.Konekcija : null;
            cfg.KesFolder = pa.KesFolder;
            return cfg;
        }

        /// <summary>
        /// Brisanje PODATAKA iz tmp tabela (DELETE; tmpSinonimi i tmpNHM ostaju) – kao OP_915 i ponovna obrada --obrisi-podatke.
        /// v3.0.3 (3.5): briše samo redove svog izvora (podrazumevano „Alat“); podaci agenta ostaju. Bez kolone Izvor (OP_919 nije pokrenut)
        /// alat ne briše ništa – greška sa uputstvom.
        /// </summary>
        public static void ObrisiTmpPodatke(string konekcija, string prefiks = null, string izvor = UpisUBazu.IzvorAlat)
        {
            var px = prefiks ?? UpisUBazu.PodrazumevaniPrefiks;
            using (var c = UpisUBazu.Otvori(konekcija))
            {
                using (var chk = new SqlCommand(Sifarnici.Tmp("SELECT COL_LENGTH('planiranje.tmpJobs', 'Izvor')", px), c))
                    if (chk.ExecuteScalar() is var dužina && (dužina == null || dužina is DBNull))
                        throw new InvalidOperationException("planiranje.tmp* nema kolonu Izvor – pokrenuti OP_919_tmp_Izvor_Zamenjena.sql (alat bez nje ne briše podatke, da ne obriše podatke agenta).");
                using (var tx = c.BeginTransaction())
                using (var cmd = new SqlCommand(Sifarnici.Tmp(ObrisiSql, px), c, tx) { CommandTimeout = 300 })
                {
                    cmd.Parameters.AddWithValue("@izvor", izvor == UpisUBazu.IzvorAgent ? UpisUBazu.IzvorAgent : UpisUBazu.IzvorAlat);
                    cmd.ExecuteNonQuery();
                    tx.Commit();
                }
            }
        }

        /// <summary>v3.0.3: DELETE po izvoru (redosled zbog stranih ključeva; reference na dokumente i poslove istog izvora se prvo brišu).</summary>
        internal const string ObrisiSql = @"DELETE FROM planiranje.tmpJobsDogadjaj WHERE Izvor = @izvor;
DELETE FROM planiranje.tmpJobsIstorija WHERE Izvor = @izvor; DELETE FROM planiranje.tmpJobsPolje WHERE Izvor = @izvor;
DELETE FROM planiranje.tmpTeretnicaStavke WHERE Izvor = @izvor;
UPDATE planiranje.tmpDokument SET JobID = NULL, TeretnicaID = NULL WHERE Izvor = @izvor;
DELETE FROM planiranje.tmpTeretnica WHERE Izvor = @izvor; DELETE FROM planiranje.tmpJobs WHERE Izvor = @izvor; DELETE FROM planiranje.tmpDokument WHERE Izvor = @izvor;";

        internal static string BezbednoIme(string s, int max)
        {
            var t = Regex.Replace(Regex.Replace(s ?? "", @"[\\/:*?""<>|\r\n\t]", "_"), @"\s+", " ").Trim(' ', '.', '_');
            return t.Length <= max ? t : t.Substring(0, max).TrimEnd(' ', '.', '_');
        }

        internal static string Kratko(string kljuc) => Prilozi.Prilog.Sha256(Encoding.UTF8.GetBytes(kljuc ?? "")).Substring(0, 8);
    }

    /// <summary>Rezultat obrade jedne poruke u alatu (za listu, Excel i analizu).</summary>
    internal sealed class RezultatAlata
    {
        public StavkaProstora Stavka;
        public string Folder;
        public JObject Json;
        public RezultatObrade Obrada;
        public List<string> Log = new List<string>();
    }

    /// <summary>
    /// Obrada skupa poruka u jednom procesu (CimRadionica: delovi od po 20 poruka, svaki u posebnom procesu): isti tok kao agent
    /// (ObradaPoruke), bez slanja – Slanje.Transport ne šalje; odgovor se priprema kao u režimu Live i čuva u odgovor.html.
    /// </summary>
    internal sealed class RadnikObrade : IDisposable
    {
        private readonly RadniProstor rp;
        private readonly PodesavanjaAlata pa;
        private readonly AgentConfig cfg;
        private readonly Log log;
        private readonly PravilaSkup pravila;
        private readonly LokalniCitac citac;
        private readonly ObradaPoruke obrada;
        private readonly Stanje stanje;
        private readonly Sifarnici sifarniciOffline;
        private readonly Obrada citanje;
        private readonly List<string> zapis = new List<string>();

        public RadnikObrade(RadniProstor rp, PodesavanjaAlata pa)
        {
            this.rp = rp; this.pa = pa;
            cfg = rp.Konfiguracija(pa);
            log = new Log(rp.LogFolder) { Tiho = true, Prati = red => { lock (zapis) zapis.Add(red); } };
            // nikad SMTP: pripremljena poruka se samo beleži (ObradaPoruke) i čuva u odgovor.html
            Slanje.Transport = (c, m) => { };
            Slanje.Dnevnik = log;
            Izvestaj.Potpis = PotpisIzvestaja.Iz(cfg);
            pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, log);
            Ponude.Konekcija = pa.Online ? pa.Konekcija : null;
            if (!pa.Online)
            {
                sifarniciOffline = SifarniciKopija.Ucitaj(SifarniciKopija.Folder(rp));
                Ponude.Zamena = sifarniciOffline == null ? (Func<int, Ponude.Rezultat>)null : SifarniciKopija.Ponuda(SifarniciKopija.Folder(rp));
            }
            citac = new LokalniCitac(cfg, log, pravila.Kp);
            var o = new Obrada(pravila, citac, log, cfg.PragPouzdanosti) { RanijiListovi = () => IstorijaListova.Ucitaj(cfg.StanjeFolder) };
            citanje = new Obrada(pravila, citac, log, cfg.PragPouzdanosti);
            stanje = Stanje.Ucitaj(cfg.StanjeFolder);
            obrada = new ObradaPoruke(cfg, log, o, pravila, stanje, false)
            {
                UpozoriSpolja = (k, t, h) => { log.Upozorenje("[upozorenje " + k + "] " + t.Split('\n')[0]); return false; },
            };
            if (sifarniciOffline != null)
            {
                var pk0 = new Poklapanje(sifarniciOffline);
                obrada.StanicaPoNazivu = naziv => pk0.Stanica(naziv).Id;
            }
        }

        public PravilaSkup Pravila => pravila;

        /// <summary>
        /// v3.0 (odluka 18.11.): pred-čitanje – prilozi poruke se samo pročitaju (OCR) i upišu u keš; bez dosijea, ponavljanja, odgovora i
        /// upisa u bazu. Više radnika pred-čitanja radi istovremeno; obrada posle ide hronološki iz keša.
        /// </summary>
        public int Procitaj(StavkaProstora s)
        {
            var m = rp.Ucitaj(s);
            var prilozi = Raspakivanje.PriloziMaila(m).ToList();
            if (prilozi.Count == 0) return 0;
            citanje.Proveri(prilozi, s.Od ?? "", false, s.Datum.LocalDateTime, false);
            return prilozi.Count;
        }

        /// <summary>Obrada jedne poruke i zapis rezultata u rezultati\&lt;verzija&gt;\&lt;datum&gt;_&lt;naslov&gt;\.</summary>
        public RezultatAlata Obradi(StavkaProstora s)
        {
            lock (zapis) zapis.Clear();
            var sw = Stopwatch.StartNew();
            var u = rp.Ulaz(s);
            var ps = new Stanje.PorukaStanje { Folder = u.Folder, Uid = u.Uid, Vreme = DateTime.Now };
            RezultatObrade r;
            try { r = obrada.Obradi(u, ps); }
            catch (Exception ex)
            {
                // v3.0: greška okruženja (blokiran DLL) – „nije obrađen“, kao pad radnika; ostale greške – privremena greška obrade
                var okr = Stanje.GreskaOkruzenja(ex);
                r = new RezultatObrade { Status = okr != null ? RadniProstor.StatusNeuspeh : "Greska", Greska = okr ?? ex.Message, Korak = Korak.PrivremenaGreska };
                log.Greska("obrada nije uspela", ex);
            }
            r.Trajanje["ukupno"] = sw.Elapsed.TotalSeconds;
            stanje.Sacuvaj();
            // offline: predlog posla po tovarnom listu iz lokalnih šifarnika (online: upis je u tmp tabelama)
            List<(string, PodaciPosla)> poslovi = null;
            if (!pa.Online && sifarniciOffline != null && r.Provera != null)
            {
                poslovi = new List<(string, PodaciPosla)>();
                var pk = new Poklapanje(sifarniciOffline);
                foreach (var l in r.Provera.Listovi.Where(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac)))
                    try { poslovi.Add((l.Oznaka, PosaoIzDokumenata.Napravi(l.Cim, l.Dokument, l, pk, pravila.Kp, u.Poruka.Subject, s.Od))); }
                    catch (Exception ex) { log.Upozorenje("predlog posla za " + l.Oznaka + " nije napravljen: " + ex.Message); }
                foreach (var d in r.Provera.Dokumenti.Where(d => d.Spisak.Count > 0 && d.Spisak[0].Kola.Count > 0 && !d.IzDosijea))
                    try { poslovi.Add(("spisak: " + Path.GetFileName(d.Fajl), PosaoIzDokumenata.IzSpiska(d.Spisak[0], pk, "", d.Fajl))); }
                    catch (Exception ex) { log.Upozorenje("predlog sa spiska " + d.Fajl + " nije napravljen: " + ex.Message); }
            }
            var json = RezultatJson.Napravi(u, r, pravila, pa.Online, r.Dogadjaji, poslovi);
            var folder = rp.FolderRezultata(pa.Verzija, s);
            Directory.CreateDirectory(folder);
            foreach (var f in Directory.GetFiles(folder)) File.Delete(f);
            File.WriteAllText(Path.Combine(folder, "rezultat.json"), RezultatJson.U(json), new UTF8Encoding(false));
            if (r.Primer?.Napravljen == true) File.WriteAllBytes(Path.Combine(folder, r.Primer.Ime), r.Primer.Pdf);
            File.WriteAllText(Path.Combine(folder, "odgovor.html"), OdgovorHtml(s, r), new UTF8Encoding(false));
            List<string> linije;
            lock (zapis) linije = zapis.ToList();
            File.WriteAllLines(Path.Combine(folder, "log.txt"), linije, new UTF8Encoding(false));
            s.Status = r.Status; s.Odluka = RezultatJson.Odluka(r);
            s.Rezultati[pa.Verzija] = folder.Substring(rp.Koren.Length).TrimStart('\\');
            return new RezultatAlata { Stavka = s, Folder = folder, Json = json, Obrada = r, Log = linije };
        }

        /// <summary>Pripremljene poruke (kao u Live režimu: primaoci, naslov, telo) – nikad poslate.</summary>
        internal static string OdgovorHtml(StavkaProstora s, RezultatObrade r)
        {
            string H(string x) => System.Net.WebUtility.HtmlEncode(x ?? "");
            var sb = new StringBuilder();
            sb.Append("<!doctype html><meta charset=\"utf-8\"><title>" + H(s.Naslov) + "</title><body style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px;max-width:1000px\">");
            sb.Append("<p style=\"background:#fff3cd;border:1px solid #e0c060;padding:6px\"><b>CimRadionica – odgovor je pripremljen kao u režimu Live i NIJE poslat.</b><br>Mail: „" + H(s.Naslov) + "“ od " + H(s.Od) + ", " + H(s.Datum.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)) + ". Odluka: " + H(RezultatJson.Odluka(r)) + ".</p>");
            if (r.Poruke.Count == 0) sb.Append("<p><i>Nema odgovora (" + H(r.Status) + ").</i></p>");
            foreach (var p in r.Poruke)
            {
                sb.Append("<hr><table style=\"font-size:12px\"><tr><td><b>Vrsta</b></td><td>" + H(p.Vrsta) + "</td></tr><tr><td><b>Za</b></td><td>" + H(string.Join(", ", p.Za)) + "</td></tr>"
                    + (p.Cc.Count > 0 ? "<tr><td><b>Cc</b></td><td>" + H(string.Join(", ", p.Cc)) + "</td></tr>" : "")
                    + "<tr><td><b>Naslov</b></td><td>" + H(p.Naslov) + "</td></tr>" + (p.Prilozi.Count > 0 ? "<tr><td><b>Prilozi</b></td><td>" + H(string.Join(", ", p.Prilozi)) + "</td></tr>" : "") + "</table>");
                sb.Append(string.IsNullOrEmpty(p.Html) ? "<pre style=\"white-space:pre-wrap\">" + H(p.Telo) + "</pre>" : "<div style=\"border:1px solid #ddd;padding:8px\">" + p.Html + "</div>");
            }
            return sb.Append("</body>").ToString();
        }

        public void Dispose() { citac?.Dispose(); }
    }
}
