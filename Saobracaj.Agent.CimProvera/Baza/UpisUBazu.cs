using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>Podaci jednog obrađenog maila za upis (posle provere i izveštaja).</summary>
    internal sealed class MailZaUpis
    {
        public string MessageId = "";
        public long Uid;
        public string Sanduce = "";
        public string Posiljalac = "";
        public string Naslov = "";
        public DateTime Primljeno = DateTime.Now;
        public string Telo = "";
        public List<(string ime, byte[] sadrzaj)> Prilozi = new List<(string, byte[])>();
        public RezultatProvere Provera;
        /// <summary>Dosije pošiljke sa kojim je mail povezan (oznake ranijih mailova „UID 262“), null = nova pošiljka.</summary>
        public Dosije.Posiljka Dosije;
    }

    /// <summary>Rezultat upisa jednog maila (za log).</summary>
    internal sealed class IshodUpisa
    {
        public int Dokumenata, NovihPoslova, IzmenjenihPoslova, IstihPoslova, Teretnica, Stavki, Povezano;
        public List<string> Napomene = new List<string>();
        /// <summary>v2.9.2 (3.3): izmene postojećih poslova u ovom mailu („posao 297: Tara1 632.400 → 666.700“) – za kratku internu poruku.</summary>
        public List<string> Promene = new List<string>();
        public override string ToString() =>
            "dokumenata " + Dokumenata + ", poslova novih " + NovihPoslova + " / izmenjenih " + IzmenjenihPoslova + " / istih " + IstihPoslova
            + ", teretnica " + Teretnica + " (stavki " + Stavki + ")" + (Povezano > 0 ? ", naknadno povezano " + Povezano : "")
            + (Napomene.Count > 0 ? "; " + string.Join("; ", Napomene) : "")
            + (Promene.Count > 0 ? "; izmene: " + string.Join("; ", Promene.Take(12)) + (Promene.Count > 12 ? " (+" + (Promene.Count - 12) + ")" : "") : "");
    }

    /// <summary>
    /// v2.9.0: upis obrađenog maila u privremene tabele planiranje.tmp* (tačka 4 zahteva). Jedan mail = jedna transakcija; greška
    /// → rollback i izuzetak pozivaocu (provera i izveštaj se ne menjaju). Agent piše samo u planiranje.tmp*, šifarnike samo čita.
    /// </summary>
    internal sealed partial class UpisUBazu
    {
        public const string Korisnik = "CimProvera";
        /// <summary>Prefiks tmp tabela; za lokalne probe „planiranje.tmpProba“ (iste tabele pod drugim imenom, validate --prefiks).</summary>
        public const string PodrazumevaniPrefiks = "planiranje.tmp";
        public string Prefiks = PodrazumevaniPrefiks;
        private const int DanaZaVezu = 10;
        private readonly string konekcija;
        private readonly Log log;
        private readonly PraviloKp kp;
        private Sifarnici sif;
        private Poklapanje pk;
        private SqlConnection c;
        private SqlTransaction tx;

        public UpisUBazu(string konekcija, Log log, PraviloKp kp) { this.konekcija = konekcija; this.log = log; this.kp = kp; }

        /// <summary>Šifarnici (za testove se mogu zadati unapred).</summary>
        public Sifarnici Sifarnici { get => sif; set { sif = value; pk = value == null ? null : new Poklapanje(value); } }

        /// <summary>v2.9.1: partneri za dopunu šifarnika (nije nađen / nema UIC / nema oznaku Prevoznik) – dnevna interna lista.</summary>
        public IEnumerable<string> ZaSifarnik => pk?.ZaSifarnik ?? Enumerable.Empty<string>();

        /// <summary>v2.9.1: SRB kodovi stanica iz dokumenata kojih nema u šifarniku: kod → „naziv iz dokumenta;predlog“.</summary>
        public IDictionary<string, string> KodoviBezSifre => pk?.KodoviBezSifre ?? new SortedDictionary<string, string>();

        /// <summary>CSV predlog kodova stanica (kod;naziv u dokumentu;predlog ID) – šifarnik se ne menja.</summary>
        public static void SacuvajPredlogKodova(IDictionary<string, string> kodovi, string put)
        {
            if (kodovi.Count == 0) return;
            var postojeci = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (System.IO.File.Exists(put))
                foreach (var red in System.IO.File.ReadAllLines(put, Encoding.UTF8).Skip(1))
                {
                    var i = red.IndexOf(';');
                    if (i > 0) postojeci[red.Substring(0, i)] = red.Substring(i + 1);
                }
            foreach (var kv in kodovi) if (!postojeci.ContainsKey(kv.Key)) postojeci[kv.Key] = kv.Value;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(put));
            System.IO.File.WriteAllLines(put, new[] { "Kod;NazivUDokumentu;PredlogStanica" }.Concat(postojeci.Select(kv => kv.Key + ";" + kv.Value)), new UTF8Encoding(true));
        }

        public static SqlConnection Otvori(string konekcija)
        {
            var k = new SqlConnection(konekcija);
            k.Open();
            // baza je na nivou kompatibilnosti 80: filtrirani indeksi tmp tabela traže ove opcije pri svakom upisu
            using (var cmd = new SqlCommand("SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF; SET XACT_ABORT ON;", k))
                cmd.ExecuteNonQuery();
            return k;
        }

        public IshodUpisa Upisi(MailZaUpis m) => Upisi(m, null);

        /// <summary>Upis u zadatoj konekciji/transakciji (integracioni test sa rollback-om), inače nova konekcija i transakcija.</summary>
        public IshodUpisa Upisi(MailZaUpis m, SqlTransaction spoljna)
        {
            var ishod = new IshodUpisa();
            promeneMaila = ishod.Promene;
            var svoja = spoljna == null;
            c = svoja ? Otvori(konekcija) : spoljna.Connection;
            try
            {
                if (sif == null) Sifarnici = Sifarnici.Ucitaj(c, log, spoljna, Prefiks);
                tx = svoja ? c.BeginTransaction(IsolationLevel.ReadCommitted) : spoljna;
                try
                {
                    UpisiMail(m, ishod);
                    if (svoja) tx.Commit();
                }
                catch
                {
                    if (svoja) try { tx.Rollback(); } catch { }
                    throw;
                }
            }
            finally
            {
                if (svoja) { tx?.Dispose(); c.Dispose(); }
                tx = null; c = null;
            }
            return ishod;
        }

        // ------------------------------------------------------------------------------------------- tok upisa

        private void UpisiMail(MailZaUpis m, IshodUpisa ishod)
        {
            var r = m.Provera;
            // v2.9.1 (5.7): ponovljeni mail (isti prilozi, odgovor se ne šalje) – prijem se beleži (BrojPrijema) i tekst maila ide u Napomenu posla
            if (r == null) { UpisiPonovljeni(m, ishod); return; }
            var dokumenti = r.Dokumenti.Where(d => !d.IzDosijea).ToList();
            // 1. prijem: svaki prilog (isti heš = isti red)
            var fajlovi = new List<Prilog>();
            foreach (var (ime, b) in m.Prilozi) fajlovi.AddRange(Raspakivanje.Raspakuj(ime, b, new List<string>()));
            var dokId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fajlovi.GroupBy(x => x.Hes).Select(g => g.First()))
            {
                if (Raspakivanje.Ukras(f) != null) continue;
                if (Raspakivanje.SopstveniPrimer(f) != null) continue;   // v2.9.2 (3.1c): sopstveni PDF primer se ne upisuje
                var docs = dokumenti.Where(d => IstiFajl(d.Fajl, f.Naziv)).ToList();
                dokId[f.Naziv] = UpisiDokument(m, f, docs);
                ishod.Dokumenata++;
            }
            int? DokId(string fajl) => dokId.Where(kv => IstiFajl(fajl, kv.Key)).Select(kv => (int?)kv.Value).FirstOrDefault();

            // 2. tovarni listovi → kopije istog lista se spajaju (4.1) → listovi istog voza su jedan posao (4.2, 4.3)
            var posloviMaila = new List<(int id, PodaciPosla p)>();
            var listovi = new List<ListPosla>();
            foreach (var l in r.Listovi.Concat(r.KopijeListova).Where(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac)))
            {
                var p = PosaoIzDokumenata.Napravi(l.Cim, l.Dokument, l, pk, kp, m.Naslov, m.Posiljalac);
                if (p.KljucPosiljke == null) { ishod.Napomene.Add(l.Oznaka + ": tovarni list bez ključa pošiljke – posao nije upisan"); continue; }
                var did = DokId(l.Dokument?.Fajl);
                listovi.Add(new ListPosla { P = p, DokId = did, Oznaka = l.Oznaka, SviDokumenti = did.HasValue ? new List<int> { did.Value } : new List<int>() });
            }
            var spojeni = SpojiKopije(listovi);
            if (spojeni.Count < listovi.Count) ishod.Napomene.Add(listovi.Count + " listova = " + spojeni.Count + " tovarnih listova (kopije spojene)");
            foreach (var voz in Vozovi(spojeni))
            {
                var id = UpisiVoz(voz, m, ishod);
                foreach (var l in voz.Listovi)
                {
                    posloviMaila.Add((id, l.P));
                    foreach (var did in l.SviDokumenti.Distinct())
                        // v2.9.2 (3a.7): isti fajl primljen ponovo ne „odvaja“ raniji posao od dokumenta – JobID se ne prepisuje
                        Izvrsi("UPDATE planiranje.tmpDokument SET JobID = ISNULL(JobID, @j), KljucPosiljke = ISNULL(KljucPosiljke, @k) WHERE ID = @d", "@j", id, "@k", PodaciPosla.Skrati(l.P.KljucPosiljke, 200), "@d", did);
                }
            }

            // 3. spiskovi kola → teretnice (spiskovi istih kola u istom mailu – Excel i PDF – su jedan spisak)
            var spiskovi = dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola && d.Spisak.Count > 0 && d.Spisak[0].Kola.Count > 0).ToList();
            var grupe = new List<List<Dokument>>();
            // u istom mailu (ista verzija) Excel/Word ima prednost nad PDF-om i skenom, pa spisak sa više podataka
            foreach (var d in spiskovi.OrderByDescending(x => x.Izvor == "excel" || x.Izvor == "word").ThenByDescending(Bogatstvo))
            {
                var g = grupe.FirstOrDefault(x => Preklapanje(x[0].Spisak[0], d.Spisak[0]) >= 0.6);
                if (g != null) g.Add(d); else grupe.Add(new List<Dokument> { d });
            }
            foreach (var g in grupe)
            {
                var glavni = g[0];
                var spisak = Spoji(g);
                var jobId = PoveziSpisak(spisak, glavni, posloviMaila, m);
                var tid = UpisiTeretnicu(spisak, glavni, jobId, g.Select(x => DokId(x.Fajl)).FirstOrDefault(x => x.HasValue), m, ishod);
                foreach (var d in g) if (DokId(d.Fajl) is int di) Izvrsi("UPDATE planiranje.tmpDokument SET TeretnicaID = @t, JobID = ISNULL(JobID, @j) WHERE ID = @d", "@t", tid, "@j", (object)jobId ?? DBNull.Value, "@d", di);
                if (jobId.HasValue) { Stanica2IzSpiska(jobId.Value, g.Select(x => x.Spisak[0].OtpravnaTekst).Concat(g.Select(x => System.IO.Path.GetFileNameWithoutExtension(x.Fajl ?? ""))).Concat(new[] { m.Naslov }).ToList(), DokId(glavni.Fajl)); PosaoIzTeretnice(jobId.Value, tid, DokId(glavni.Fajl), "SPISAK_KOLA", ishod, spisak); }
            }

            // 3a. spisak koji je stigao pre tovarnog lista: nepovezane teretnice za nove/izmenjene poslove
            foreach (var (id, p) in posloviMaila) ishod.Povezano += PoveziNepovezane(id, p, m, ishod);

            // 4. CIT 7 povezan sa poslom
            foreach (var c7 in r.Cit7) PrimeniCit7(c7, DokId(c7.Fajl), m, ishod);
        }

        private static bool IstiFajl(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            string N(string x) => x.Replace('\\', '/').Trim();
            return string.Equals(N(a), N(b), StringComparison.OrdinalIgnoreCase) || string.Equals(System.IO.Path.GetFileName(N(a)), System.IO.Path.GetFileName(N(b)), StringComparison.OrdinalIgnoreCase);
        }

        private static int Bogatstvo(Dokument d)
        {
            var s = d.Spisak[0];
            return s.Kola.Count(k => k.TaraKg.HasValue || k.NetoKg.HasValue) * 3 + s.Kola.Count(k => k.Osovine.HasValue || k.DuzinaM.HasValue) + (d.Izvor == "excel" ? 1 : 0);
        }

        internal static double Preklapanje(SpisakKola a, SpisakKola b)
        {
            var x = new HashSet<string>(a.Kola.Select(k => k.Broj)); var y = new HashSet<string>(b.Kola.Select(k => k.Broj));
            if (x.Count == 0 || y.Count == 0) return 0;
            return (double)x.Intersect(y).Count() / Math.Min(x.Count, y.Count);
        }

        /// <summary>Spisak sa najviše podataka, dopunjen podacima drugih spiskova istih kola iz istog maila.</summary>
        internal static SpisakKola Spoji(List<Dokument> g)
        {
            var s = g[0].Spisak[0];
            foreach (var d in g.Skip(1))
            {
                var o = d.Spisak[0];
                if (s.R62.Broj.Length == 0 && o.R62.Broj.Length > 0) s.R62 = o.R62;
                if (s.BrojVoza.Length == 0) s.BrojVoza = o.BrojVoza;
                if (s.OtpravnaTekst.Length == 0) s.OtpravnaTekst = o.OtpravnaTekst;
                if (s.UputnaTekst.Length == 0) s.UputnaTekst = o.UputnaTekst;
                if (s.NhmZaglavlje.Length == 0) s.NhmZaglavlje = o.NhmZaglavlje;
                foreach (var k in s.Kola)
                {
                    var z = o.Kola.FirstOrDefault(x => x.Broj == k.Broj);
                    if (z == null) continue;
                    k.TaraKg = k.TaraKg ?? z.TaraKg; k.NetoKg = k.NetoKg ?? z.NetoKg; k.BrutoKg = k.BrutoKg ?? z.BrutoKg; k.Osovine = k.Osovine ?? z.Osovine; k.DuzinaM = k.DuzinaM ?? z.DuzinaM;
                    if (k.Serija.Length == 0) k.Serija = z.Serija;
                    if (k.Nhm.Length == 0) k.Nhm = z.Nhm;
                    if (k.Napomena.Length == 0) k.Napomena = z.Napomena;
                    if (k.KemlerUn.Length == 0) k.KemlerUn = z.KemlerUn;
                    if (k.Rb == 0) k.Rb = z.Rb;
                }
            }
            return s;
        }

        // ------------------------------------------------------------------------------------------- dokumenti

        internal static string TipZaBazu(IEnumerable<Dokument> docs, string ime)
        {
            var t = docs.Select(d => d.Tip).ToList();
            if (t.Contains(TipDokumenta.Cim)) return "CIM";
            if (t.Contains(TipDokumenta.Cuv)) return "CUV";
            if (t.Contains(TipDokumenta.Cit7)) return "CIT7";
            if (t.Contains(TipDokumenta.SpisakKola)) return Regex.IsMatch(ime ?? "", @"(?i)load\s*list|train\s*list|ladeliste") ? "LOADLIST" : "SPISAK_KOLA";
            if (t.Contains(TipDokumenta.SpisakJedinica)) return "LOADLIST";
            if (t.Contains(TipDokumenta.TranzitnaDeklaracija)) return "T1";
            if (t.Contains(TipDokumenta.IzvoznaDeklaracija) || t.Contains(TipDokumenta.DrugaCarinska)) return "JCI";
            if (t.Contains(TipDokumenta.Najava)) return "NAJAVA";
            if (t.Contains(TipDokumenta.SastavVoza)) return "SASTAV_VOZA";
            return "OSTALO";
        }

        private int UpisiDokument(MailZaUpis m, Prilog f, List<Dokument> docs)
        {
            var hes = f.Hes;
            var postoji = Skalar("SELECT ID FROM planiranje.tmpDokument WHERE HashFajla = @h", "@h", hes);
            if (postoji != null)
            {
                Izvrsi("UPDATE planiranje.tmpDokument SET BrojPrijema = BrojPrijema + 1, PoslednjePrimljeno = @p WHERE ID = @id", "@p", m.Primljeno, "@id", Convert.ToInt32(postoji));
                return Convert.ToInt32(postoji);
            }
            var strane = string.Join(", ", docs.Where(d => d.Strane.Count > 0).Select(d => "str." + (d.Strane.Count == 1 ? d.Strane[0].ToString() : d.Strane.Min() + "–" + d.Strane.Max()) + " " + d.Tip));
            return Convert.ToInt32(Skalar(@"INSERT planiranje.tmpDokument (MessageID, UID, Sanduce, Posiljalac, Naslov, Primljeno, ImeFajla, HashFajla, TipDokumenta, Strane, TekstMaila, PoslednjePrimljeno)
OUTPUT INSERTED.ID VALUES (@mid, @uid, @san, @pos, @nas, @pri, @ime, @h, @tip, @str, @tekst, @pri)",
                "@mid", N(m.MessageId, 400), "@uid", m.Uid > 0 ? (object)m.Uid : DBNull.Value, "@san", N(m.Sanduce, 200), "@pos", N(m.Posiljalac, 320), "@nas", N(m.Naslov, 500),
                "@pri", m.Primljeno, "@ime", N(f.Naziv, 400), "@h", hes, "@tip", TipZaBazu(docs, f.Naziv), "@str", N(strane, 100), "@tekst", (object)m.Telo ?? DBNull.Value));
        }

        // ------------------------------------------------------------------------------------------- posao

        /// <summary>Zaglavlje bloka napomene za mail: „&lt;datum vreme&gt; &lt;pošiljalac&gt;: &lt;naslov&gt;“ (po njemu se mail upisuje tačno jednom).</summary>
        internal static string ZaglavljeNapomene(MailZaUpis m) =>
            m.Primljeno.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) + " " + m.Posiljalac + ": " + Regex.Replace(m.Naslov ?? "", @"\s+", " ").Trim();

        private static readonly Regex Pozdrav = new Regex(@"(?i)^\s*(s\s+po[šs]tovanjem|srda[čc]an\s+pozdrav|s\s+pozdravom|pozdrav|lp|best\s+regards|kind\s+regards|regards|br|thanks?\s*(?:&|and)\s*regards|mit\s+freundlichen\s+gr(?:ü|u)(?:ß|ss)en|mfg|viele\s+gr(?:ü|u)(?:ß|ss)e|üdvözlettel|udvozlettel|cu\s+stim[aă]|cordiali\s+saluti|saygılarımla|z\s+pozdravem|s\s+pozdravom)[\s,.!]*$");
        private static readonly Regex Odricanje = new Regex(@"(?i)^\s*(disclaimer|confidential|this (e-?mail|message) (and any|is|may)|ova (poruka|e-?mail)|napomena o poverljivosti|the information (contained|in this)|diese (e-?mail|nachricht)|before printing|please consider the environment)");

        /// <summary>
        /// Blok napomene: zaglavlje + telo bez citata; v2.9.1 (5.8): posle pozdrava („S poštovanjem“, „Best regards“, „Mit freundlichen
        /// Grüßen“, „S pozdravom“, „Üdvözlettel“…) ostaje samo ime (prvi sledeći red), bez potpisa, pravnih upozorenja, linkova i telefona.
        /// </summary>
        internal static string BlokNapomene(MailZaUpis m)
        {
            var telo = Obrada.TekstPosiljaoca("", m.Telo ?? "");
            var redovi = telo.Replace("\r", "").Split('\n').Select(x => x.TrimEnd()).ToList();
            var kraj = redovi.FindIndex(x => Odricanje.IsMatch(x));
            if (kraj >= 0) redovi = redovi.Take(kraj).ToList();
            var poz = redovi.FindIndex(x => Pozdrav.IsMatch(x));
            if (poz >= 0)
            {
                // ime posle pozdrava (TekstPosiljaoca se zaustavlja na pozdravu – ime se uzima iz celog tela)
                var sirovi = (m.Telo ?? "").Replace("\r", "").Split('\n').Select(x => x.Trim()).ToList();
                var ip = sirovi.FindIndex(x => Pozdrav.IsMatch(x));
                var ime = redovi.Skip(poz + 1).FirstOrDefault(x => x.Trim().Length > 0) ?? (ip >= 0 ? sirovi.Skip(ip + 1).FirstOrDefault(x => x.Length > 0) : null);
                redovi = redovi.Take(poz + 1).ToList();
                if (ime != null && ime.Trim().Length <= 60 && !Regex.IsMatch(ime, @"\d|@|http|www\.")) redovi.Add(ime.Trim());
            }
            redovi = redovi.Select(x => Regex.Replace(x, @"(?i)\b(?:https?://|www\.)\S+|<mailto:[^>]+>|<https?:[^>]+>", "").TrimEnd())
                .Where(x => !Regex.IsMatch(x, @"^\s*(?:(?:tel|phone|mob|mobile|fax|t|m|f|e|e-mail|email)\.?\s*[:.]?\s*)?[+\d][\d\s/().\-]{6,}\s*$", RegexOptions.IgnoreCase)).ToList();
            var t = Regex.Replace(string.Join("\n", redovi), @"\n{3,}", "\n\n").Trim();
            return ZaglavljeNapomene(m) + (t.Length > 0 ? "\n" + t : "");
        }

        /// <summary>Napomena sa blokom maila na vrhu; null kada je mail već upisan (isto zaglavlje) – svaki mail tačno jednom.</summary>
        private static string NovaNapomena(int jobId, MailZaUpis m, string blok, string stara)
        {
            var z = ZaglavljeNapomene(m);
            if (!string.IsNullOrEmpty(stara) && stara.Contains(z)) return null;
            return SpojiNapomenu(blok, stara);
        }

        /// <summary>Novi blok na početak, stari ostaju (najnoviji gore); preko 2000 znakova skraćuju se najstariji blokovi („…“).</summary>
        internal static string SpojiNapomenu(string nova, string stara)
        {
            var t = string.IsNullOrWhiteSpace(stara) ? nova : nova + "\n\n" + stara;
            return t.Length <= 2000 ? t : t.Substring(0, 1999) + "…";
        }

        /// <summary>
        /// Nov posao (<paramref name="postojeci"/> null) ili nova verzija postojećeg: samo promenjena polja, Verzija + 1, istorija.
        /// Ključ posla se ne menja (posao voza ima ključ glavnog lista grupe 1). Tekst maila ide u Napomenu tačno jednom (v2.9.1).
        /// </summary>
        private int UpisiPosao(PodaciPosla p, int? postojeci, int? dokId, MailZaUpis m, string razlog, IshodUpisa ishod)
        {
            object id = postojeci;
            var poVozu = true;
            var blok = BlokNapomene(m);
            if (p.Napomene.Count > 0) blok += "\n[agent] " + string.Join("\n[agent] ", p.Napomene);
            if (id == null)
            {
                // ključ posla je jedinstven (UX_tmpJobs_Kljuc): drugi voz sa istim ključem „ref:“ dobija nastavak „#2“, „#3“…
                if (p.V.TryGetValue("KljucPosiljke", out var kv0) && kv0 is string k0 && k0.Length > 0)
                    for (int n = 2; Skalar("SELECT ID FROM planiranje.tmpJobs WHERE KljucPosiljke = @k", "@k", p.V["KljucPosiljke"]) != null && n < 50; n++)
                        p.V["KljucPosiljke"] = PodaciPosla.Skrati(k0, 195) + "#" + n;
                var kol = PosaoIzDokumenata.Kolone.Where(k => p.V.ContainsKey(k)).ToList();
                var sql = "INSERT planiranje.tmpJobs (" + string.Join(", ", kol) + ", Napomena, Kreirao, Verzija, PoslednjiDokumentID) OUTPUT INSERTED.ID VALUES ("
                          + string.Join(", ", kol.Select(k => "@" + k)) + ", @Napomena, @Kreirao, 1, @dok)";
                var arg = new List<object>();
                foreach (var k in kol) { arg.Add("@" + k); arg.Add(Db(p.V[k])); }
                arg.AddRange(new object[] { "@Napomena", SpojiNapomenu(blok, null), "@Kreirao", Korisnik, "@dok", (object)dokId ?? DBNull.Value });
                var novi = Convert.ToInt32(Skalar(sql, arg.ToArray()));
                foreach (var po in p.Poreklo) UpisiPoreklo(novi, null, po, dokId);
                Istorija(novi, null, null, null, "Ključ " + p.KljucPosiljke, dokId, "NOVI_POSAO", null);
                ishod.NovihPoslova++;
                return novi;
            }
            var jid = Convert.ToInt32(id);
            var stari = Red("SELECT * FROM planiranje.tmpJobs WHERE ID = @id", "@id", jid);
            var rucno = Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", jid);
            // spisak kola ima prednost: kada posao ima teretnicu, zbirni podaci tovarnog lista ne menjaju kolone spiska
            // samo kolone čija je vrednost zaista sa spiska (poreklo „spisak“) – npr. bruto ostaje iz lista kada spisak nema taru
            var izSpiska = new HashSet<string>(Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND Rubrika IN ('spisak', 'istorija')", "@id", jid), StringComparer.OrdinalIgnoreCase);
            var promene = Uporedi(stari, p.V, PosaoIzDokumenata.Kolone.Where(k => !izSpiska.Contains(k) && !(poVozu && k == "KljucPosiljke")));
            // vrednost koja u novoj verziji nije pročitana (NULL / 0, pouzdanost 0) ne briše vrednost koja je već pronađena
            foreach (var k in promene.ToList())
            {
                var nova = p.V[k]; var stara = stari.TryGetValue(k, out var sv) ? sv : null;
                var prazna = nova == null || (nova is decimal dn && dn == 0) || (nova is int i && i == 0 && k.StartsWith("BrojKola"));
                var nijeProcitano = p.Poreklo.FirstOrDefault(x => x.Kolona == k)?.Pouzdanost == 0;
                if (stara != null && prazna && nijeProcitano)
                {
                    promene.Remove(k);
                    Istorija(jid, null, k, Tekst(stara), Tekst(nova), dokId, razlog, "u novom dokumentu nije pročitano – zadržana ranija vrednost");
                }
            }
            var napomena = NovaNapomena(jid, m, blok, stari["Napomena"] as string);
            if (promene.Count == 0)
            {
                Izvrsi("UPDATE planiranje.tmpJobs SET PoslednjiDokumentID = ISNULL(@dok, PoslednjiDokumentID)" + (napomena != null ? ", Napomena = @nap" : "") + " WHERE ID = @id",
                    "@dok", (object)dokId ?? DBNull.Value, "@id", jid, "@nap", (object)napomena ?? DBNull.Value);
                ishod.IstihPoslova++;
                return jid;
            }
            PrimeniPromene(jid, stari, p, promene, rucno, dokId, razlog, napomena);
            ishod.IzmenjenihPoslova++;
            return jid;
        }

        /// <summary>Kolone čija se vrednost razlikuje (poređenje kao tekst: brojevi bez suvišnih nula, bit kao 0/1).</summary>
        internal static List<string> Uporedi(IDictionary<string, object> stari, IDictionary<string, object> novi, IEnumerable<string> kolone) =>
            kolone.Where(k => novi.ContainsKey(k) && Tekst(stari.TryGetValue(k, out var s) ? s : null) != Tekst(novi[k])).ToList();

        internal static string Tekst(object v)
        {
            if (v == null || v is DBNull) return null;
            switch (v)
            {
                case bool b: return b ? "1" : "0";
                case decimal d: return d.ToString("0.###", CultureInfo.InvariantCulture);
                case double d: return d.ToString("0.###", CultureInfo.InvariantCulture);
                case DateTime t: return t.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                default: return Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>v2.9.2 (3.3): izmene poslova u mailu koji se trenutno upisuje.</summary>
        private List<string> promeneMaila = new List<string>();

        private void PrimeniPromene(int jid, IDictionary<string, object> stari, PodaciPosla p, List<string> promene, List<string> rucno, int? dokId, string razlog, string napomena)
        {
            var menja = promene.Where(k => !rucno.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (var k in promene.Where(k => rucno.Contains(k, StringComparer.OrdinalIgnoreCase)))
                Istorija(jid, null, k, Tekst(stari[k]), Tekst(p.V[k]), dokId, razlog, "ručno izmenjeno – nije menjano");
            if (menja.Count == 0) return;
            var sql = "UPDATE planiranje.tmpJobs SET " + string.Join(", ", menja.Select(k => k + " = @" + k))
                      // Verzija raste sa novom verzijom tovarnog lista (i CIT 7); podaci sa spiska kola je ne menjaju
                      + (razlog == "SPISAK_KOLA" ? "" : ", Verzija = Verzija + 1")
                      + ", Izmenio = @izm, Izmenjeno = SYSDATETIME(), PoslednjiDokumentID = ISNULL(@dok, PoslednjiDokumentID)"
                      + (napomena != null ? ", Napomena = @nap" : "") + " WHERE ID = @id";
            var arg = new List<object>();
            foreach (var k in menja) { arg.Add("@" + k); arg.Add(Db(p.V[k])); }
            arg.AddRange(new object[] { "@izm", Korisnik, "@dok", (object)dokId ?? DBNull.Value, "@id", jid });
            if (napomena != null) { arg.Add("@nap"); arg.Add(napomena); }
            Izvrsi(sql, arg.ToArray());
            foreach (var k in menja)
            {
                Istorija(jid, null, k, Tekst(stari[k]), Tekst(p.V[k]), dokId, razlog, null);
                if (stari[k] != null && !(stari[k] is DBNull)) promeneMaila.Add("posao " + jid + ": " + k + " " + Tekst(stari[k]) + " → " + Tekst(p.V[k]) + " (" + razlog + ")");
                var po = p.Poreklo.FirstOrDefault(x => string.Equals(x.Kolona, k, StringComparison.OrdinalIgnoreCase));
                if (po != null) UpisiPoreklo(jid, null, po, dokId);
            }
        }

        private void UpisiPoreklo(int? jobId, int? teretnicaId, PoljePorekla po, int? dokId)
        {
            const string sql = @"IF EXISTS (SELECT 1 FROM planiranje.tmpJobsPolje WHERE ((@t IS NULL AND JobID = @j AND TeretnicaID IS NULL) OR (TeretnicaID = @t)) AND Kolona = @k)
    UPDATE planiranje.tmpJobsPolje SET Vrednost = @v, TekstIzDokumenta = @tekst, Rubrika = @r, DokumentID = @d, Pouzdanost = @p, Kandidati = @kan, Izmenjeno = SYSDATETIME()
    WHERE ((@t IS NULL AND JobID = @j AND TeretnicaID IS NULL) OR (TeretnicaID = @t)) AND Kolona = @k AND RucnoIzmenjeno = 0
ELSE
    INSERT planiranje.tmpJobsPolje (JobID, TeretnicaID, Kolona, Vrednost, TekstIzDokumenta, Rubrika, DokumentID, Pouzdanost, Kandidati)
    VALUES (@j, @t, @k, @v, @tekst, @r, @d, @p, @kan)";
            Izvrsi(sql, "@j", (object)jobId ?? DBNull.Value, "@t", (object)teretnicaId ?? DBNull.Value, "@k", po.Kolona, "@v", N(Tekst(po.Vrednost), 400),
                "@tekst", N(po.Tekst, 1000), "@r", N(po.Rubrika, 50), "@d", (object)dokId ?? DBNull.Value, "@p", (byte)Math.Max(0, Math.Min(100, po.Pouzdanost)), "@kan", N(po.Kandidati, 1000));
        }

        private void Istorija(int? jobId, int? teretnicaId, string kolona, string stara, string nova, int? dokId, string razlog, string napomena)
        {
            Izvrsi(@"INSERT planiranje.tmpJobsIstorija (JobID, TeretnicaID, Kolona, StaraVrednost, NovaVrednost, DokumentID, Razlog, Napomena)
VALUES (@j, @t, @k, @s, @n, @d, @r, @nap)", "@j", (object)jobId ?? DBNull.Value, "@t", (object)teretnicaId ?? DBNull.Value, "@k", N(kolona, 128), "@s", N(stara, 2000), "@n", N(nova, 2000),
                "@d", (object)dokId ?? DBNull.Value, "@r", razlog, "@nap", N(napomena, 400));
        }

        // ------------------------------------------------------------------------------------------- spisak → posao

        private sealed class KandidatPosla { public int Id; public string Kljuc = ""; public string Referenca = ""; public string Naslovi = ""; public int Otpravna; public int Uputna; }

        private List<KandidatPosla> NedavniPoslovi()
        {
            var l = new List<KandidatPosla>();
            using (var cmd = Komanda(@"SELECT j.ID, ISNULL(j.KljucPosiljke, ''), ISNULL(j.Referenca, ''), ISNULL(j.OtpravnaID, 0), ISNULL(j.UputnaID, 0),
    ISNULL((SELECT TOP 1 d.Naslov FROM planiranje.tmpDokument d WHERE d.JobID = j.ID ORDER BY d.ID DESC), '')
FROM planiranje.tmpJobs j WHERE j.Kreirano >= DATEADD(day, -@dana, SYSDATETIME())", "@dana", DanaZaVezu))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) l.Add(new KandidatPosla { Id = r.GetInt32(0), Kljuc = r.GetString(1), Referenca = r.GetString(2), Otpravna = r.GetInt32(3), Uputna = r.GetInt32(4), Naslovi = r.GetString(5) });
            return l;
        }

        private static string R62Kljuc(Identifikacija r)
        {
            string C(string x) => Regex.Replace(x ?? "", @"\D", "");
            return C(r?.Zemlja).Length == 2 && C(r?.Stanica).Length >= 5 && C(r?.Preduzece).Length == 4 && C(r?.Broj).Length >= 5
                ? "r62:" + C(r.Zemlja) + "-" + C(r.Stanica) + "-" + C(r.Preduzece) + "-" + C(r.Broj) : null;
        }

        /// <summary>
        /// Posao za spisak (tačka 4.3): isti mail → identifikacija sa spiska (r62) → referenca (broj voza iz r8 / naslova) → brojevi
        /// kola (teretnica istih kola) → dosije pošiljke. null = posao još ne postoji (spisak pre tovarnog lista).
        /// </summary>
        private int? PoveziSpisak(SpisakKola s, Dokument d, List<(int id, PodaciPosla p)> posloviMaila, MailZaUpis m)
        {
            var kola = s.Kola.Select(k => k.Broj).ToList();
            if (posloviMaila.Count == 1) return posloviMaila[0].id;
            if (posloviMaila.Count > 1)
            {
                var po = posloviMaila.OrderByDescending(x => x.p.Kola.Intersect(kola).Count()).First();
                return po.id;
            }
            var r62 = R62Kljuc(s.R62);
            if (r62 != null && Skalar("SELECT ID FROM planiranje.tmpJobs WHERE KljucPosiljke = @k", "@k", r62) is object o) return Convert.ToInt32(o);
            var voz = BrojVozaSpiska(s, d, m);
            var nedavni = NedavniPoslovi();
            if (voz.Length > 0)
            {
                var istiVoz = nedavni.Where(j => ExcelCitac.BrojVoza(j.Referenca) == voz || ExcelCitac.BrojVoza(j.Naslovi) == voz).OrderByDescending(j => j.Id).ToList();
                if (istiVoz.Count >= 1) return istiVoz[0].Id;
            }
            // v2.9.2: natovaren spisak ne ide uz posao praznih istih kola (exLDC7 → LDC8); spisak praznih kola sme uz natovaren posao
            // (povratak praznih kola – grupa 2)
            var tKola = TeretnicaIstihKola(kola, true, Natovaren(s) == true ? true : (bool?)null);
            if (tKola?.jobId != null) return tKola.Value.jobId;
            return PosaoIzDosijea(m);
        }

        /// <summary>Broj voza za spisak: zaglavlje spiska, ime fajla, naslov maila.</summary>
        private static string BrojVozaSpiska(SpisakKola s, Dokument d, MailZaUpis m) =>
            new[] { s.BrojVoza, ExcelCitac.BrojVoza(System.IO.Path.GetFileNameWithoutExtension(d?.Fajl ?? "")), ExcelCitac.BrojVoza(m.Naslov) }.FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";

        /// <summary>Posao iz ranijeg maila istog dosijea pošiljke (oznake „UID 262“ → tmpDokument.UID).</summary>
        private int? PosaoIzDosijea(MailZaUpis m)
        {
            if (m.Dosije == null) return null;
            foreach (var oz in m.Dosije.Mailovi.AsEnumerable().Reverse())
            {
                var u = Regex.Match(oz ?? "", @"^UID (\d+)$");
                if (!u.Success) continue;
                var j = Skalar("SELECT TOP 1 JobID FROM planiranje.tmpDokument WHERE UID = @u AND Sanduce = @s AND JobID IS NOT NULL ORDER BY ID DESC", "@u", long.Parse(u.Groups[1].Value), "@s", N(m.Sanduce, 200));
                if (j != null) return Convert.ToInt32(j);
            }
            return null;
        }

        /// <summary>Teretnica sa bar 60 % istih kola (poslednjih 10 dana); samoSaPoslom = samo teretnice povezane sa poslom.</summary>
        /// <summary>
        /// v2.9.2 (3a.2): spisak natovarenih kola (neto na kolima koja nisu prazna) – true; spisak praznih kola – false; bez masa – null.
        /// Ista kola u suprotnom stanju su drugi voz (exLDC7 prazan Kiskőrös → Šid, pa LDC8 natovaren Šid → Hamm).
        /// </summary>
        internal static bool? Natovaren(SpisakKola s)
        {
            if (s == null || s.Kola.Count == 0) return null;
            if (s.Kola.Any(k => !k.Prazna && (k.NetoKg ?? 0) > 0) || (s.UkupnoNetoKg ?? 0) > 0 && !s.Kola.All(k => k.Prazna)) return true;
            if (s.Kola.All(k => k.Prazna) || s.Kola.Any(k => k.TaraKg > 0) && s.Kola.All(k => (k.NetoKg ?? 0) == 0)) return false;
            return null;
        }

        private (int id, int? jobId)? TeretnicaIstihKola(List<string> kola, bool samoSaPoslom, bool? natovaren = null)
        {
            if (kola.Count == 0) return null;
            var brojevi = kola.Select(BrojKolaUic.ZaTeretnicu).ToList();
            var parametri = new List<object>();
            var imena = new List<string>();
            for (int i = 0; i < brojevi.Count && i < 200; i++) { imena.Add("@k" + i); parametri.Add("@k" + i); parametri.Add(brojevi[i]); }
            parametri.Add("@dana"); parametri.Add(DanaZaVezu);
            parametri.Add("@nat"); parametri.Add(natovaren.HasValue ? (object)(natovaren.Value ? 1 : 0) : DBNull.Value);
            var sql = @"SELECT TOP 1 t.ID, t.JobID, COUNT(*) AS n, (SELECT COUNT(*) FROM planiranje.tmpTeretnicaStavke x WHERE x.BrojTeretnice = t.ID) AS ukupno
FROM planiranje.tmpTeretnica t JOIN planiranje.tmpTeretnicaStavke s ON s.BrojTeretnice = t.ID
WHERE s.BrojKola IN (" + string.Join(", ", imena) + ") AND t.Kreirano >= DATEADD(day, -@dana, SYSDATETIME())" + (samoSaPoslom ? " AND t.JobID IS NOT NULL" : "") + @"
  AND (@nat IS NULL OR NOT EXISTS (SELECT 1 FROM planiranje.tmpTeretnicaStavke y WHERE y.BrojTeretnice = t.ID) OR
       (CASE WHEN EXISTS (SELECT 1 FROM planiranje.tmpTeretnicaStavke x WHERE x.BrojTeretnice = t.ID AND x.Neto > 0) THEN 1 ELSE 0 END) = @nat)
GROUP BY t.ID, t.JobID ORDER BY COUNT(*) DESC, t.ID DESC";
            using (var cmd = Komanda(sql, parametri.ToArray()))
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return null;
                var n = r.GetInt32(2); var ukupno = r.GetInt32(3);
                if (n < 0.6 * Math.Min(brojevi.Count, Math.Max(1, ukupno))) return null;
                return (r.GetInt32(0), r.IsDBNull(1) ? (int?)null : r.GetInt32(1));
            }
        }

        // ------------------------------------------------------------------------------------------- teretnica

        private sealed class StavkaVrednosti
        {
            public int Rb; public string BrojKola = ""; public string Serija; public decimal Osovine; public decimal? Duzina; public decimal Tara; public decimal Neto;
            public string Vrnp; public string Rid; public string Primedba;
            public Dictionary<string, object> Kolone() => new Dictionary<string, object>
            {
                ["RB"] = Rb, ["Serija"] = Serija, ["BrojOsovina"] = Osovine, ["Duzina"] = Duzina, ["Tara"] = Tara, ["Neto"] = Neto, ["VRNP"] = Vrnp, ["RID"] = Rid, ["Primedba"] = Primedba,
            };
        }

        /// <summary>Stavke teretnice sa spiska (jedinice kao dbo.TeretnicaStavke: dužina u m, tara i neto u t – 19.990 / 59.757; bez podatka 0).</summary>
        private static List<StavkaVrednosti> Stavke(SpisakKola s, string nhmPosla)
        {
            var l = new List<StavkaVrednosti>();
            int rb = 0;
            foreach (var k in s.Kola)
            {
                rb++;
                var nhm = Poklapanje.Nhm4(k.Nhm.Length > 0 ? k.Nhm : s.NhmZaglavlje.Length > 0 ? s.NhmZaglavlje : nhmPosla);
                var bruto = k.BrutoKg; var tara = k.TaraKg; var neto = k.NetoKg;
                if (neto == null && bruto.HasValue && tara.HasValue) neto = bruto - tara;
                // v2.9.1: prazna kola (9921/9922) sa jednom masom na spisku – to je tara kola
                if ((k.Prazna || nhm.StartsWith("9921") || nhm.StartsWith("9922")) && tara == null && neto > 0) { tara = neto; neto = 0; }
                var serija = k.Serija.Length > 0 ? k.Serija : BrojKolaUic.Slovo(k.Broj);
                l.Add(new StavkaVrednosti
                {
                    Rb = k.Rb > 0 ? k.Rb : rb, BrojKola = BrojKolaUic.ZaTeretnicu(k.Broj), Serija = serija.Length == 0 ? null : serija,
                    Osovine = k.Osovine ?? BrojKolaUic.Osovine(k.Broj), Duzina = k.DuzinaM.HasValue ? Math.Round((decimal)k.DuzinaM.Value, 3) : (decimal?)null,
                    Tara = Math.Round((decimal)(tara ?? 0) / 1000m, 3), Neto = Math.Round((decimal)(neto ?? 0) / 1000m, 3), Vrnp = nhm.Length == 0 ? null : nhm,
                    Rid = k.KemlerUn.Length > 0 ? k.KemlerUn : null, Primedba = k.Napomena.Length > 0 ? k.Napomena : null,
                });
            }
            return l;
        }

        private static string KljucSpiska(SpisakKola s, Dokument d, MailZaUpis m)
        {
            var r62 = R62Kljuc(s.R62);
            if (r62 != null) return r62;
            var voz = BrojVozaSpiska(s, d, m);
            if (voz.Length > 0) return "voz:" + voz;
            return "kola:" + Prilog.Sha256(Encoding.UTF8.GetBytes(string.Join(",", s.Kola.Select(k => k.Broj).OrderBy(x => x, StringComparer.Ordinal)))).Substring(0, 16);
        }

        /// <summary>
        /// Teretnica za spisak (tačka 4.3): nova, ili nova verzija istog spiska (isti ključ ili ista kola u poslednjih 10 dana) – dodata kola
        /// se dodaju, uklonjena brišu, izmenjene vrednosti menjaju; sve u istoriju.
        /// </summary>
        private int UpisiTeretnicu(SpisakKola s, Dokument d, int? jobId, int? dokId, MailZaUpis m, IshodUpisa ishod)
        {
            var kljuc = KljucSpiska(s, d, m);
            var nhmPosla = jobId.HasValue ? NhmPosla(jobId.Value) : "";
            DopuniDuzine(s);
            var nove = Stavke(s, nhmPosla);
            var postoji = Skalar("SELECT TOP 1 ID FROM planiranje.tmpTeretnica WHERE KljucSpiska LIKE @k + '#%' AND Kreirano >= DATEADD(day, -@dana, SYSDATETIME()) ORDER BY ID DESC", "@k", kljuc, "@dana", DanaZaVezu);
            int? tid = postoji == null ? (int?)null : Convert.ToInt32(postoji);
            if (tid == null && TeretnicaIstihKola(s.Kola.Select(k => k.Broj).ToList(), false, Natovaren(s)) is var tk && tk != null && (jobId == null || tk.Value.jobId == null || tk.Value.jobId == jobId)) tid = tk.Value.id;
            var napomena = N(System.IO.Path.GetFileName(d?.Fajl ?? "") + (d?.Strane.Count > 0 ? ", str. " + (d.Strane.Count == 1 ? d.Strane[0].ToString() : d.Strane.Min() + "–" + d.Strane.Max()) : ""), 512);
            var brojTer = s.R62.Broj.Length > 0 ? N(string.Join(" ", new[] { s.R62.Zemlja, s.R62.Stanica, s.R62.Preduzece, s.R62.Broj }.Where(x => !string.IsNullOrEmpty(x))), 100) : (object)DBNull.Value;
            var referenca = N(BrojVozaSpiska(s, d, m) is var v && v.Length > 0 ? "voz " + v : m.Naslov, 100);
            if (tid == null)
            {
                tid = Convert.ToInt32(Skalar(@"INSERT planiranje.tmpTeretnica (JobID, BrojTeretnice, Prijemna, Predajna, Prevozna, Carinska, Korisnik, Napomena, rb, DokumentID, Verzija, KljucSpiska, KljucPosiljke, Referenca)
OUTPUT INSERTED.ID VALUES (@j, @b, 1, 0, 0, 0, @kor, @nap, 0, @d, 1, @ks, @kp, @ref)",
                    "@j", (object)jobId ?? DBNull.Value, "@b", brojTer, "@kor", Korisnik, "@nap", napomena, "@d", (object)dokId ?? DBNull.Value,
                    "@ks", N(kljuc + "#" + m.Primljeno.ToString("yyyyMMdd", CultureInfo.InvariantCulture), 200), "@kp", (object)R62Kljuc(s.R62) ?? DBNull.Value, "@ref", referenca));
                foreach (var st in nove) DodajStavku(tid.Value, st, dokId);
                Istorija(jobId, tid, null, null, nove.Count + " kola", dokId, "SPISAK_KOLA", "nova teretnica (" + napomena + ")");
                ishod.Teretnica++; ishod.Stavki += nove.Count;
            }
            else
            {
                var stare = new Dictionary<string, Dictionary<string, object>>();
                using (var cmd = Komanda("SELECT * FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", tid.Value))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) { var red = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase); for (int i = 0; i < r.FieldCount; i++) red[r.GetName(i)] = r.GetValue(i); stare[(string)red["BrojKola"]] = red; }
                int izmena = 0;
                // prioritet izvora (odluka 14.11.): novija verzija spiska ima prednost nad starijom (dodata/uklonjena/izmenjena kola);
                // u istom mailu Excel ima prednost nad skenom (Spoji); OCR skena ne gazi popunjenu vrednost iz Excel-a
                bool Tabela(string f) => System.Text.RegularExpressions.Regex.IsMatch(f ?? "", @"(?i)\.(xlsx?|xlsm|xlsb|csv|docx?)$");
                var staraTabela = Tabela(Skalar("SELECT d.ImeFajla FROM planiranje.tmpTeretnica t JOIN planiranje.tmpDokument d ON d.ID = t.DokumentID WHERE t.ID = @t", "@t", tid.Value) as string);
                var ocr = d?.Izvor == "ocr";
                var zadrzano = 0;
                foreach (var st in nove)
                {
                    if (!stare.TryGetValue(st.BrojKola, out var red)) { DodajStavku(tid.Value, st, dokId); Istorija(jobId, tid, "BrojKola", null, st.BrojKola, dokId, "SPISAK_KOLA", "dodata kola"); izmena++; continue; }
                    var kol = st.Kolone();
                    var raz = Uporedi(red, kol, kol.Keys);
                    // novija verzija ima prednost nad starijom; OCR skena ne gazi popunjenu vrednost iz Excel-a (izvor stavke = njen dokument)
                    if (ocr && Tabela(ImeDokumenta(red["DokumentID"]) ?? (staraTabela ? ".xlsx" : "")))
                    { var pre = raz.Count; raz = raz.Where(k => red[k] == null || red[k] is DBNull || (red[k] is decimal dv && dv == 0)).ToList(); zadrzano += pre - raz.Count; }
                    if (raz.Count == 0) continue;
                    var arg = new List<object>();
                    foreach (var k in raz) { arg.Add("@" + k); arg.Add(Db(kol[k])); }
                    arg.AddRange(new object[] { "@id", Convert.ToInt32(red["ID"]), "@d", (object)dokId ?? DBNull.Value });
                    Izvrsi("UPDATE planiranje.tmpTeretnicaStavke SET " + string.Join(", ", raz.Select(k => k + " = @" + k)) + ", DokumentID = ISNULL(@d, DokumentID) WHERE ID = @id", arg.ToArray());
                    foreach (var k in raz) Istorija(jobId, tid, k, Tekst(red[k]), Tekst(kol[k]), dokId, "SPISAK_KOLA", st.BrojKola);
                    izmena++;
                }
                foreach (var kv in stare.Where(x => !nove.Any(n => n.BrojKola == x.Key)))
                {
                    Izvrsi("DELETE FROM planiranje.tmpTeretnicaStavke WHERE ID = @id", "@id", Convert.ToInt32(kv.Value["ID"]));
                    Istorija(jobId, tid, "BrojKola", kv.Key, null, dokId, "SPISAK_KOLA", "uklonjena kola");
                    izmena++;
                }
                if (zadrzano > 0)
                    Istorija(jobId, tid, null, null, null, dokId, "SPISAK_KOLA", PodaciPosla.Skrati("novi spisak (" + System.IO.Path.GetFileName(d?.Fajl ?? "") + (ocr ? ", OCR" : ", PDF")
                        + ") – " + zadrzano + " vrednosti nije menjano (ostaju vrednosti iz pouzdanijeg spiska)", 400));
                // izvorni dokument teretnice ostaje tabela kada nova verzija nije iz tabele
                var noviDok = ocr && staraTabela ? (object)DBNull.Value : (object)dokId ?? DBNull.Value;
                if (izmena > 0)
                    Izvrsi(@"UPDATE planiranje.tmpTeretnica SET Verzija = Verzija + 1, Izmenjeno = SYSDATETIME(), DokumentID = ISNULL(@d, DokumentID), Napomena = CASE WHEN @d IS NULL THEN Napomena ELSE @nap END,
    JobID = ISNULL(JobID, @j), BrojTeretnice = ISNULL(@b, BrojTeretnice) WHERE ID = @t",
                        "@d", noviDok, "@nap", napomena, "@j", (object)jobId ?? DBNull.Value, "@b", brojTer, "@t", tid.Value);
                else Izvrsi("UPDATE planiranje.tmpTeretnica SET JobID = ISNULL(JobID, @j) WHERE ID = @t", "@j", (object)jobId ?? DBNull.Value, "@t", tid.Value);
                ishod.Teretnica++; ishod.Stavki += izmena;
            }
            if (jobId.HasValue) StaniceTeretnice(jobId.Value, tid.Value);
            var izIst = s.Kola.Count(k => k.DuzinaIzvor == "istorija"); var bez = s.Kola.Count(k => !k.DuzinaM.HasValue);
            UpisiPoreklo(null, tid, new PoljePorekla { Kolona = "Duzina", Vrednost = bez == 0 ? (object)Math.Round((decimal)s.Kola.Sum(k => k.DuzinaM ?? 0), 2) : null, Rubrika = izIst > 0 ? "istorija" : "spisak",
                Tekst = (s.Kola.Count - izIst - bez) + " kola sa spiska, " + izIst + " iz istorije (poslednja dužina istih kola u dbo.TeretnicaStavke), " + bez + " bez dužine (NULL)", Pouzdanost = bez > 0 ? 0 : izIst > 0 ? 80 : 95 }, dokId);
            return tid.Value;
        }

        /// <summary>Ime fajla dokumenta (tmpDokument.ID), null ako nema.</summary>
        private string ImeDokumenta(object id) => id == null || id is DBNull ? null : Skalar("SELECT ImeFajla FROM planiranje.tmpDokument WHERE ID = @d", "@d", Convert.ToInt32(id)) as string;

        /// <summary>
        /// Poslednja dužina (m) istih kola iz dbo.TeretnicaStavke, po broju kola („33839338201-9“). Jedan upit po spisku (tabela nema indekse).
        /// Može se zameniti u testovima.
        /// </summary>
        public Func<IList<string>, Dictionary<string, double>> DuzineIzIstorije;

        private Dictionary<string, double> DuzineIzBaze(IList<string> brojevi)
        {
            var rez = new Dictionary<string, double>(StringComparer.Ordinal);
            if (brojevi.Count == 0) return rez;
            var imena = new List<string>(); var arg = new List<object>();
            for (int i = 0; i < brojevi.Count && i < 200; i++) { imena.Add("@k" + i); arg.Add("@k" + i); arg.Add(brojevi[i]); }
            using (var cmd = Komanda("SELECT BrojKola, Duzina FROM dbo.TeretnicaStavke WHERE BrojKola IN (" + string.Join(", ", imena) + ") AND Duzina > 0 ORDER BY ID DESC", arg.ToArray()))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) { var b = r.GetString(0).Trim(); if (!rez.ContainsKey(b)) rez[b] = Convert.ToDouble(r.GetValue(1)); }
            return rez;
        }

        /// <summary>Kola bez dužine na spisku dobijaju poslednju dužinu istih kola iz dbo.TeretnicaStavke (poreklo „istorija“); inače ostaje NULL.</summary>
        private void DopuniDuzine(SpisakKola s)
        {
            var bez = s.Kola.Where(k => !k.DuzinaM.HasValue).ToList();
            if (bez.Count == 0) return;
            var duz = (DuzineIzIstorije ?? DuzineIzBaze)(bez.Select(k => BrojKolaUic.ZaTeretnicu(k.Broj)).Distinct().ToList());
            foreach (var k in bez)
                if (duz.TryGetValue(BrojKolaUic.ZaTeretnicu(k.Broj), out var m) && m >= 5 && m <= 50) { k.DuzinaM = Math.Round(m, 3); k.DuzinaIzvor = "istorija"; }
        }

        private void DodajStavku(int tid, StavkaVrednosti st, int? dokId)
        {
            Izvrsi(@"INSERT planiranje.tmpTeretnicaStavke (RB, BrojTeretnice, BrojKola, Serija, BrojOsovina, Duzina, Tara, Neto, G, P, R, RR, RucKoc, VRNP, RID, Primedba, NamernaGreska, Popisana, DokumentID)
VALUES (@rb, @t, @bk, @s, @o, @du, @ta, @ne, 0, 0, 0, 0, 0, @v, @rid, @pr, 0, 0, @d)",
                "@rb", st.Rb, "@t", tid, "@bk", st.BrojKola, "@s", (object)st.Serija ?? DBNull.Value, "@o", st.Osovine, "@du", (object)st.Duzina ?? DBNull.Value, "@ta", st.Tara, "@ne", st.Neto,
                "@v", (object)st.Vrnp ?? DBNull.Value, "@rid", (object)st.Rid ?? DBNull.Value, "@pr", (object)st.Primedba ?? DBNull.Value, "@d", (object)dokId ?? DBNull.Value);
        }

        private string NhmPosla(int jobId)
        {
            var id = Skalar("SELECT NHM1 FROM planiranje.tmpJobs WHERE ID = @j", "@j", jobId);
            return id == null || id is DBNull ? "" : sif.Nhm.FirstOrDefault(z => z.Id == Convert.ToInt32(id))?.Kod8 ?? "";
        }

        /// <summary>
        /// Stanice teretnice i stavki iz posla (kao u postojećim podacima): StanicaOd/Do i Otpravna/Uputna = deonica KP (Stanica1/Stanica3),
        /// Izvozna = otpravna, Uvozna = krajnja uputna; Dokument = broj otpravljanja iz r62 posla; VRNP iz NHM posla kada ga spisak nema.
        /// </summary>
        private void StaniceTeretnice(int jobId, int tid)
        {
            var kljuc = Skalar("SELECT KljucPosiljke FROM planiranje.tmpJobs WHERE ID = @j", "@j", jobId) as string ?? "";
            var otp = Regex.Match(kljuc, @"^r62:\d+-\d+-\d+-(\d+)$");
            var nhm = Poklapanje.Nhm4(NhmPosla(jobId));
            Izvrsi(@"UPDATE t SET StanicaOd = j.Stanica1ID, StanicaDo = j.Stanica3ID FROM planiranje.tmpTeretnica t JOIN planiranje.tmpJobs j ON j.ID = t.JobID WHERE t.ID = @t;
UPDATE s SET Otpravna = j.Stanica1ID, Uputna = j.Stanica3ID, Izvozna = j.OtpravnaID, Uvozna = j.UputnaID, Dokument = ISNULL(s.Dokument, @dok), VRNP = ISNULL(s.VRNP, @nhm)
FROM planiranje.tmpTeretnicaStavke s JOIN planiranje.tmpTeretnica t ON t.ID = s.BrojTeretnice JOIN planiranje.tmpJobs j ON j.ID = t.JobID WHERE t.ID = @t;",
                "@t", tid, "@dok", otp.Success ? (object)otp.Groups[1].Value : DBNull.Value, "@nhm", nhm.Length > 0 ? (object)nhm : DBNull.Value);
        }

        /// <summary>Stanica2 (utovarno mesto, „u.m. Pančevo Varoš“) iz zaglavlja spiska, imena fajla i naslova maila – kada posao još nema Stanica2.</summary>
        private void Stanica2IzSpiska(int jobId, List<string> tekstovi, int? dokId)
        {
            var stari = Red("SELECT * FROM planiranje.tmpJobs WHERE ID = @id", "@id", jobId);
            if (stari["Stanica2ID"] != null) return;
            var st = PosaoIzDokumenata.Stanica2(pk, stari["OtpravnaID"] as int?, tekstovi.Select(x => ("spisak / mail", x)).ToArray());
            if (st == null) return;
            var p = new PodaciPosla(); p.Postavi("Stanica2ID", st, "utovarno mesto", "");
            PrimeniPromene(jobId, stari, p, new List<string> { "Stanica2ID" }, new List<string>(), dokId, "SPISAK_KOLA", null);
        }

        /// <summary>Nepovezane teretnice (spisak pre tovarnog lista) za posao: isti voz, ista kola, r62 ili isti dosije. Vraća broj povezanih.</summary>
        private int PoveziNepovezane(int jobId, PodaciPosla p, MailZaUpis m, IshodUpisa ishod)
        {
            var nepovezane = new List<(int id, string ref_, string kp, string kljuc)>();
            using (var cmd = Komanda("SELECT ID, ISNULL(Referenca, ''), ISNULL(KljucPosiljke, ''), ISNULL(KljucSpiska, '') FROM planiranje.tmpTeretnica WHERE JobID IS NULL AND Kreirano >= DATEADD(day, -@dana, SYSDATETIME())", "@dana", DanaZaVezu))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) nepovezane.Add((r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3)));
            if (nepovezane.Count == 0) return 0;
            var dosijeUid = new HashSet<long>((m.Dosije?.Mailovi ?? new List<string>()).Select(x => Regex.Match(x ?? "", @"^UID (\d+)$")).Where(x => x.Success).Select(x => long.Parse(x.Groups[1].Value)));
            int n = 0;
            foreach (var t in nepovezane)
            {
                var razlog = "";
                if (t.kp.Length > 0 && t.kp == p.KljucPosiljke) razlog = "r62";
                else if (p.BrojVoza.Length > 0 && (ExcelCitac.BrojVoza(t.ref_) == p.BrojVoza || t.kljuc.StartsWith("voz:" + p.BrojVoza + "#"))) razlog = "broj voza " + p.BrojVoza;
                else if (p.Kola.Count > 0)
                {
                    var kola = Kolona("SELECT BrojKola FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", t.id).Select(BrojKolaUic.Cifre).ToList();
                    if (kola.Count > 0 && kola.Intersect(p.Kola).Count() >= 0.6 * Math.Min(kola.Count, p.Kola.Count)) razlog = "ista kola";
                }
                if (razlog.Length == 0 && dosijeUid.Count > 0)
                {
                    var uid = Skalar("SELECT TOP 1 d.UID FROM planiranje.tmpDokument d WHERE d.TeretnicaID = @t AND d.Sanduce = @s", "@t", t.id, "@s", N(m.Sanduce, 200));
                    if (uid != null && !(uid is DBNull) && dosijeUid.Contains(Convert.ToInt64(uid))) razlog = "dosije pošiljke";
                }
                if (razlog.Length == 0) continue;
                Izvrsi("UPDATE planiranje.tmpTeretnica SET JobID = @j, Izmenjeno = SYSDATETIME() WHERE ID = @t AND JobID IS NULL", "@j", jobId, "@t", t.id);
                Izvrsi("UPDATE planiranje.tmpDokument SET JobID = @j WHERE TeretnicaID = @t AND JobID IS NULL", "@j", jobId, "@t", t.id);
                Istorija(jobId, t.id, "JobID", null, jobId.ToString(), null, "SPISAK_KOLA", "naknadno povezan spisak (" + razlog + ")");
                StaniceTeretnice(jobId, t.id);
                PosaoIzTeretnice(jobId, t.id, Skalar("SELECT DokumentID FROM planiranje.tmpTeretnica WHERE ID = @t", "@t", t.id) is int d ? d : (int?)null, "SPISAK_KOLA", ishod);
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------------------------------- SQL pomoćne

        private static object N(string s, int max) => string.IsNullOrEmpty(s) ? (object)DBNull.Value : s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        private static object Db(object v)
        {
            if (v == null) return DBNull.Value;
            if (v is string s) return s.Length == 0 ? (object)DBNull.Value : s;
            return v;
        }

        private SqlCommand Komanda(string sql, params object[] arg)
        {
            var cmd = new SqlCommand(Sifarnici.Tmp(sql, Prefiks), c, tx) { CommandTimeout = 120 };
            for (int i = 0; i + 1 < arg.Length; i += 2)
            {
                var v = arg[i + 1] ?? DBNull.Value;
                var p = cmd.Parameters.AddWithValue((string)arg[i], v);
                if (v is string || v is DBNull) { p.SqlDbType = SqlDbType.NVarChar; p.Size = -1; }
                if (v is DateTime) p.SqlDbType = SqlDbType.DateTime2;
            }
            return cmd;
        }

        private void Izvrsi(string sql, params object[] arg) { using (var cmd = Komanda(sql, arg)) cmd.ExecuteNonQuery(); }

        private object Skalar(string sql, params object[] arg)
        {
            using (var cmd = Komanda(sql, arg))
            {
                var v = cmd.ExecuteScalar();
                return v is DBNull ? null : v;
            }
        }

        private Dictionary<string, object> Red(string sql, params object[] arg)
        {
            var red = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = Komanda(sql, arg))
            using (var r = cmd.ExecuteReader())
                if (r.Read()) for (int i = 0; i < r.FieldCount; i++) red[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            return red;
        }

        private List<string> Kolona(string sql, params object[] arg)
        {
            var l = new List<string>();
            using (var cmd = Komanda(sql, arg))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) if (!r.IsDBNull(0)) l.Add(Convert.ToString(r.GetValue(0), CultureInfo.InvariantCulture));
            return l;
        }
    }
}
