using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>Greška čitanja koja se ne ponavlja automatski (prilog ide na ručnu proveru).</summary>
    internal sealed class TrajnaGreskaCitanja : Exception
    {
        public TrajnaGreskaCitanja(string poruka, Exception inner = null) : base(poruka, inner) { }
    }

    /// <summary>Privremena greška (npr. fajl zauzet): mail se obrađuje ponovo u sledećem pokretanju.</summary>
    internal sealed class PrivremenaGreskaCitanja : Exception
    {
        public PrivremenaGreskaCitanja(string poruka, Exception inner = null) : base(poruka, inner) { }
    }

    internal interface ICitacDokumenata
    {
        RezultatPriloga Procitaj(Prilog p);
    }

    /// <summary>
    /// Lokalno čitanje priloga, bez spoljnih servisa i ključeva (isti princip kao TelegramiImport):
    /// PDF sa tekstom → PdfPig; sken i fotografija → Tesseract OCR na serveru. Svaka strana se klasifikuje po ključnim
    /// rečima; CIM/CUV strane se mapiraju na rubrike po rasporedu CIT obrasca (sablon\cim_sablon.json).
    /// </summary>
    internal sealed class LokalniCitac : ICitacDokumenata, IDisposable
    {
        /// <summary>Menja se kada se promeni logika čitanja – stari keš se tada ne koristi.</summary>
        public const string VerzijaCitaca = "L23";   // v3.0 (e): opšti OCR kućice bez potvrde isečka ne dobija 0,85; pouzdanost delova r62. L22 v3.0 (0a.5): keš čuva reči i natpise po rubrikama; L21 v2.9.2: prelivanje r57, ponovni OCR r57/r58a, klasifikacija (Wagenliste, mreža obrasca); L20 v2.8.4: ciljani OCR – kućica van strane se preskače (ranije „Out of memory.“), L19: nastavak spiska kola CIT 23, orijentacija skena
        /// <summary>Menja se kada se promeni čitanje teksta strana (PDF tekst, OCR, slika strane) – tada se OCR ponavlja.</summary>
        public const string VerzijaTeksta = "L18";   // v2.8.4: orijentacija skena ispod 0,75 (OSD + proba 90°/270°), crno-beli sken, FreeText anotacije

        private readonly AgentConfig cfg;
        private readonly Log log;
        private readonly CimSablon sablon;
        private readonly PraviloKp kp;
        private readonly Ocr ocr;
        private readonly string kesFolder;

        public int BrojOcrStrana { get; private set; }

        public LokalniCitac(AgentConfig cfg, Log log, PraviloKp kp)
        {
            this.cfg = cfg; this.log = log; this.kp = kp;
            sablon = CimSablon.Ucitaj(cfg.SablonFajl, cfg.ObrasciFolder, cfg.PragPoravnanja);
            if (Ocr.Dostupan(cfg.TessdataFolder, cfg.OcrJezici, out var razlog)) ocr = new Ocr(cfg.TessdataFolder, cfg.OcrJezici, cfg.OcrMinPouzdanostReci, cfg.OcrDodatniJezici);
            else log?.Upozorenje("OCR nije dostupan (" + razlog + ") – skenovi i fotografije se ne čitaju.");
            // v3.0: CimRadionica deli jedan keš za sve radne prostore (KesFolder); agent – stanje\kes kao ranije
            kesFolder = string.IsNullOrEmpty(cfg.KesFolder) ? Path.Combine(cfg.StanjeFolder, "kes") : cfg.KesFolder;
        }

        /// <summary>
        /// v3.0: upis u keš preko privremenog fajla i premeštanja – više radnika CimRadionice (pred-čitanje) može istovremeno da pročita
        /// isti prilog; drugi upis istog ključa se preskače (sadržaj je isti), a čitalac nikad ne vidi polovično upisan fajl.
        /// </summary>
        internal static void UpisiAtomicno(string put, string sadrzaj)
        {
            var tmp = put + "." + System.Diagnostics.Process.GetCurrentProcess().Id + "." + Guid.NewGuid().ToString("N").Substring(0, 6) + ".tmp";
            File.WriteAllText(tmp, sadrzaj, Encoding.UTF8);
            try { if (File.Exists(put)) File.Delete(tmp); else File.Move(tmp, put); }
            catch (IOException) { try { File.Delete(tmp); } catch { } }
        }

        public RezultatPriloga Procitaj(Prilog p)
        {
            // Ključ keša rezultata: verzija čitača, šabloni, OCR jezici i pragovi – svaka promena znači novo čitanje.
            // (MVID: svaki novi build programa poništava keš rezultata – logika čitanja se menja sa kodom.)
            var podesavanja = Prilog.Sha256(Encoding.UTF8.GetBytes(string.Join("|", VerzijaTeksta, cfg.OcrJezici, cfg.OcrDodatniJezici, cfg.OcrMinPouzdanostReci,
                cfg.PragMastila, cfg.PragOznake, cfg.PragPraznogPolja, typeof(LokalniCitac).Assembly.ManifestModule.ModuleVersionId))).Substring(0, 8);
            var kes = Path.Combine(kesFolder, p.Hes + "_" + VerzijaCitaca + "_" + sablon.Hes + "_" + podesavanja + ".json");
            if (File.Exists(kes))
            {
                try
                {
                    var r = Json.Iz<RezultatPriloga>(File.ReadAllText(kes, Encoding.UTF8));
                    foreach (var d in r.Dokumenti) d.Fajl = p.Naziv;
                    log?.Info("  " + p.Naziv + ": iz keša (" + Opis(r) + ")");
                    return r;
                }
                catch { /* čita se ponovo */ }
            }
            if (p.Sadrzaj.Length > cfg.MaxPrilogMB * 1024L * 1024L)
                throw new TrajnaGreskaCitanja("Prilog je veći od " + cfg.MaxPrilogMB + " MB.");

            var t0 = DateTime.Now;
            var strane = Strane(p);
            BrojOcrStrana += strane.Count(s => s.Izvor == IzvorTeksta.Ocr);
            var rez = Klasifikuj(p, strane);
            foreach (var d in rez.Dokumenti) d.Fajl = p.Naziv;

            try { Directory.CreateDirectory(kesFolder); UpisiAtomicno(kes, Json.U(rez)); }
            catch (Exception ex) { log?.Upozorenje("Keš nije upisan: " + ex.Message); }
            log?.Info("  " + p.Naziv + ": " + strane.Count + " str. (" + string.Join(",", strane.Select(s => s.Izvor == IzvorTeksta.Ocr ? "ocr" : s.Izvor == IzvorTeksta.Tekst ? "tekst" : "-"))
                + ") za " + (int)(DateTime.Now - t0).TotalSeconds + " s – " + Opis(rez));
            return rez;
        }

        /// <summary>
        /// Strane priloga (tekst/OCR i slika strane). Keš teksta po stranama (OCR je spor): parser i šabloni mogu da se
        /// menjaju bez ponovnog OCR-a. uvekOcr: PDF strane se iscrtavaju i čitaju OCR-om i kada imaju tekst (bez keša).
        /// </summary>
        internal List<Strana> Strane(Prilog p, bool uvekOcr = false)
        {
            if (p.Strane != null) return p.Strane;
            List<Strana> strane = null;
            var kesStrana = Path.Combine(kesFolder, p.Hes + "_strane_" + Prilog.Sha256(Encoding.UTF8.GetBytes(VerzijaTeksta + cfg.OcrJezici + "|" + cfg.OcrDodatniJezici + cfg.OcrMinPouzdanostReci)).Substring(0, 8) + ".json");
            if (!uvekOcr && File.Exists(kesStrana))
                try { strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Strana>>(File.ReadAllText(kesStrana, Encoding.UTF8)); } catch { strane = null; }
            // v2.8.4 (2): ugao okrenute strane iz keša – iscrtavanja (raster, kućice, PDF primer) okreću stranu isto kao pri OCR-u
            if (strane != null) { foreach (var s in strane.Where(x => x.Ugao != 0)) PdfCrtac.ZapamtiUgao(p.Sadrzaj, s.Broj, s.Ugao); return strane; }
            try
            {
                if (p.Vrsta == VrstaPriloga.Pdf) strane = PdfStrane.Procitaj(p.Sadrzaj, ocr, 60, uvekOcr, sablon.BrojSidara, TrebaSlika, p.IzWorda);
                else
                {
                    if (ocr == null) throw new TrajnaGreskaCitanja("fotografija/sken, a OCR nije instaliran (tessdata)");
                    var reci = ocr.Prepoznaj(p.Sadrzaj, out var nap);
                    strane = new List<Strana> { new Strana { Broj = 1, Reci = reci, Izvor = reci.Count > 0 ? IzvorTeksta.Ocr : IzvorTeksta.Nema, Napomena = nap, Ugao = ocr.PoslednjiUgao } };
                    PdfCrtac.ZapamtiUgao(p.Sadrzaj, 1, strane[0].Ugao);
                    if (TrebaSlika(reci)) strane[0].PostaviRaster(Raster.IzSlike(PdfCrtac.SlikaOkrenuta(p.Sadrzaj)));
                }
            }
            catch (TrajnaGreskaCitanja) { throw; }
            catch (IOException ex) { throw new PrivremenaGreskaCitanja("fajl nije dostupan: " + ex.Message, ex); }
            catch (Exception ex) { throw new TrajnaGreskaCitanja("fajl ne može da se otvori: " + ex.Message, ex); }
            if (!uvekOcr)
                try { Directory.CreateDirectory(kesFolder); UpisiAtomicno(kesStrana, Newtonsoft.Json.JsonConvert.SerializeObject(strane)); } catch { }
            return strane;
        }

        private static string Opis(RezultatPriloga r) =>
            string.Join(", ", r.Dokumenti.GroupBy(d => d.Tip).Select(g => g.Key + (g.Count() > 1 ? "×" + g.Count() : ""))) + (r.NeprocitaneStrane > 0 ? ", nepročitano str.: " + r.NeprocitaneStrane : "");

        // ------------------------------------------------------------------------------------------- klasifikacija

        private static readonly string[] CimKljucevi =
            { "frachtbrief cim", "lettre de voiture cim", "consignment note", "tovarni list", "товарни лист", "nakladni list", "wagenbrief cuv", "wagon note", "lettre wagon cuv", "колски лист", "kolski list",
              "fuvarlevel", "cim tasima belgesi", "list przewozowy", "scrisoare de trasura", "товарителница" };
        private static readonly string[] CimImena = { "cim", "cuv", "frachtbrief", "tovarni", "consignment", "nakladni" };
        private static readonly string[] SpisakImena = { "cit", "vykaz", "wagenliste", "spisak", "wagonlist", "wagon list", "o-v9", "ov9" };

        internal string TipStrane(Strana s, string fajl) => TipStrane(s, fajl, out _);

        /// <summary>Ime fajla nosi oznaku tovarnog lista (cim, cuv, frachtbrief…) kao celu reč.</summary>
        private static bool ImeFajlaCim(string fajl)
        {
            var ime = TekstUtil.Kljuc(Path.GetFileNameWithoutExtension(fajl ?? ""));
            return CimImena.Any(x => Regex.IsMatch(ime, @"(^|[^a-z0-9])" + Regex.Escape(x) + @"([^a-z]|$)"));
        }

        private static readonly Regex MrnUTekstu = new Regex(@"(?<![A-Z0-9])(\d{2})?[A-Z]{2}[A-Z0-9]{14}(?![A-Z0-9])", RegexOptions.Compiled);
        internal static readonly Regex JciBroj = new Regex(@"(?<!\d)(\d{5})\s*/\s*(?:C1\s*/\s*)?(\d{5})\s*/\s*(?:[0-3]?\d\.[01]?\d\.)?(20\d{2})(?!\d)", RegexOptions.Compiled);

        /// <summary>
        /// Vrsta strane po SADRŽAJU (natpisi dokumenta), pa po imenu fajla. razlog = natpisi po kojima je prepoznata (za log).
        /// Natpisi se traže i u obliku koji OCR daje kada ćirilicu pročita latiničnim slovima istog oblika ("JEQUHCTBEHA", "TPAH3MT").
        /// </summary>
        internal string TipStrane(Strana s, string fajl, out string razlog)
        {
            var t = " " + TekstUtil.Kljuc(s.Tekst) + " ";
            var ime = TekstUtil.Kljuc(Path.GetFileNameWithoutExtension(fajl ?? ""));
            var nadjeno = new List<string>();
            bool Ima(params string[] k)
            {
                var x = k.FirstOrDefault(y => t.Contains(TekstUtil.Kljuc(y)));
                if (x != null && !nadjeno.Contains(x)) nadjeno.Add(x);
                return x != null;
            }
            // cela reč (kratke oznake: lrn, ucr, tir…)
            bool Rec(params string[] k)
            {
                var x = k.FirstOrDefault(y => t.Contains(" " + TekstUtil.Kljuc(y) + " "));
                if (x != null && !nadjeno.Contains(x)) nadjeno.Add(x);
                return x != null;
            }
            // Kratke oznake (cim, cuv, t1, tad, ens, vdd…) samo kao cela reč: "HOLCIM" nije CIM. Duže (frachtbrief, wagenliste) i kao deo reči.
            bool ImeIma(params string[] k) => k.Any(x =>
            {
                var kx = TekstUtil.Kljuc(x);
                var da = Regex.IsMatch(ime, @"(^|[^a-z0-9])" + Regex.Escape(kx) + @"([^a-z]|$)") || (kx.Replace(" ", "").Length >= 6 && ime.Replace(" ", "").Contains(kx.Replace(" ", "")));
                if (da && !nadjeno.Contains("ime: " + x)) nadjeno.Add("ime: " + x);
                return da;
            });
            razlog = "";
            var tipStrane = TipStraneJezgro(s, fajl, nadjeno, Ima, Rec, ImeIma);
            razlog = string.Join(", ", nadjeno);
            return tipStrane;
        }

        /// <summary>Tekst zaglavlja strane (gornjih 9 %) – za G12 (informativni ispis, „Nem fuvarlevél“).</summary>
        private static string Zaglavlje(Strana s) => string.Join(" ", Raspored.Linije(s.Reci.Where(w => w.Cy < 0.09).ToList()).Select(l => l.Tekst));

        /// <summary>
        /// Odeljci „Rovat 7 / Feld 7“, „Rovat 13 / Feld 13“ (dodatni list, mađarski spisak kola) → rubrike 7 i 13 poslednjeg pročitanog
        /// tovarnog lista u istom fajlu. null kada nema takvog lista (obrazac nije prepoznat ili list ne postoji).
        /// </summary>
        private static List<string> DopuniRubrikeLista(RezultatPriloga rez, Strana s)
        {
            var list = rez.Dokumenti.LastOrDefault(x => TipDokumenta.JeTovarniList(x.Tip))?.Cim.LastOrDefault();
            if (list == null || !string.IsNullOrEmpty(list.NeprepoznatObrazac)) return null;
            return CimParser.DopuniIzDodatnogLista(list, s);
        }

        /// <summary>
        /// Nazivi primeraka CIM/CUV lista (polje „primerak“ u dnu obrasca) na jezicima CIT obrazaca: 1 Original, 2 Feuille de route,
        /// 3 Bulletin d'arrivée/Douane, 4 Duplicata, 5 Souche d'expédition (IT/FR/DE/HU/SR…).
        /// </summary>
        private static readonly (string kljuc, string naziv)[] PrimerakNazivi =
        {
            ("foglio di via", "Foglio di via / Feuille de route"), ("feuille de route", "Feuille de route"),
            ("bollettino d arrivo", "Bollettino d'arrivo / Bulletin d'arrivée"), ("bulletin d arrivee", "Bulletin d'arrivée"),
            ("duplicato", "Duplicato / Duplicata"), ("duplicata", "Duplicata"), ("matrice", "Matrice / Souche d'expédition"), ("souche", "Souche d'expédition"),
            ("frachtbriefdoppel", "Frachtbriefdoppel"), ("ankunftsschein", "Ankunftsschein"), ("versandschein", "Versandschein"), ("empfangsschein", "Empfangsschein"),
            ("menetlevel", "Menetlevél"), ("erkezesi ertesito", "Érkezési értesítő"), ("masodlat", "Másodlat"), ("feladasi szelveny", "Feladási szelvény"),
            ("izvestaj o prispecu", "Izveštaj o prispeću"), ("duplikat", "Duplikat"), ("primerak za otpravnu", "Primerak za otpravnu stanicu"),
            ("originale", "Originale / Original"), ("eredeti", "Eredeti"), ("original", "Original"),
        };

        /// <summary>Naziv primerka sa strane (donji deo obrasca), ili null.</summary>
        internal static string NazivPrimerka(Strana s)
        {
            var dno = " " + TekstUtil.Kljuc(string.Join(" ", s.Reci.Where(w => w.Cy > 0.9).Select(w => w.T))) + " ";
            foreach (var (k, n) in PrimerakNazivi) if (dno.Contains(" " + TekstUtil.Kljuc(k))) return n;
            return null;
        }

        /// <summary>v2.9.2 (3a.8): broj oznaka sadržaja tovarnog lista u tekstu strane (normalizovan tekst sa razmacima na krajevima).</summary>
        internal static int SadrzajTovarnogLista(string t)
        {
            var n = 0;
            if (Regex.IsMatch(t, @" (frachtzahler|frachtzaler|frachwshler|payer|platilac|zahler) ")) n++;
            if (Regex.IsMatch(t, @" cim ?6 ")) n++;
            if (Regex.IsMatch(t, @" 13 ?[34] ")) n++;
            if (Regex.IsMatch(t, @" (cit ?23|list of wagons|wagenliste|spisak kola) ")) n++;
            if (Regex.IsMatch(t, @" (tara|netto|brutto|nhm) ")) n++;
            if (Regex.IsMatch(t, @" (absender|empfanger|consignor|consignee|posiljalac|primalac) ")) n++;
            return n;
        }

        /// <summary>v2.9.2 (3.1b): strana ima mrežu obrasca CIM/CUV – šablon je pouzdano poravnat (natpisi, brojevi rubrika i linije).</summary>
        private bool MrezaObrasca(Strana s)
        {
            try { return sablon.Rasporedi(s.Reci, s.Raster).Poravnato; }
            catch { return true; }   // u sumnji – kao ranije (obrazac), nikad „nema CIM-a“
        }

        private static readonly string[] SpisakNaslov = { "kocsijegyzek", "wagenliste", "wagen liste", "spisak kola", "списак кола", "wagon list", "liste des wagons", "lista de vagoane", "vykaz vozu" };

        private static readonly string[] DodatniNaslov = { "potlap", "zusatzblatt", "erganzungsblatt", "ergaenzungsblatt", "dodatni list", "додатни лист", "additional sheet", "feuille complementaire" };

        private string TipStraneJezgro(Strana s, string fajl, List<string> nadjeno, Func<string[], bool> ima, Func<string[], bool> rec, Func<string[], bool> imeIma)
        {
            var t = " " + TekstUtil.Kljuc(s.Tekst) + " ";
            bool Ima(params string[] k) => ima(k);
            bool Rec(params string[] k) => rec(k);
            bool ImeIma(params string[] k) => imeIma(k);
            string Vrati(string tip) => tip;

            var spisakTekst = ExcelCitac.SpisakKljucevi.Any(k => t.Contains(TekstUtil.Kljuc(k)));
            var formaCim = CimKljucevi.Any(k => t.Contains(TekstUtil.Kljuc(k)));
            var sidara = sablon.BrojSidara(s.Reci);
            var naslovSpiska = Ima("spisak kola", "списак кола", "nachweisung", "wagenliste", "wagen liste", "wagon list", "liste des wagons", "lista de vagoane", "vykaz vozu");

            // v2.8.3: najava / otkaz voza („Najava voza / Train announcement / Zuganmeldung“, „BROJ VOZA / TRAIN NUMBER“) – poseban tip,
            // ne proverava se (natpisi POŠILJALAC/PRIMALAC/ROBA nisu dovoljni za tovarni list bez naslova obrasca)
            if (!formaCim && (Ima(NajavaVoza.Natpisi) || (Ima("broj voza") && Ima("train number"))))
            { nadjeno.Add("natpis najave voza"); return Vrati(TipDokumenta.Najava); }
            // Dodatni list (Pótlap / Zusatzblatt / Dodatni list) po NASLOVU na vrhu strane i odeljcima „Rovat 7 / Feld 7“ – nije drugi tovarni list.
            // (Prednja strana pominje „Siehe Zusatzblatt“ u r7/r20, ali ne u naslovu.)
            var naslov = " " + TekstUtil.Kljuc(string.Join(" ", s.Reci.Where(w => w.Cy < 0.06).Select(w => w.T))) + " ";
            if (DodatniNaslov.Any(k => naslov.Contains(TekstUtil.Kljuc(k))) && Regex.IsMatch(s.Tekst, @"(?im)^\s*(?:rovat|feld|rubrika|рубрика|box|case)\s*\d{1,2}\b"))
            { nadjeno.Add("naslov dodatnog lista"); return Vrati(TipDokumenta.DodatniList); }
            // Izveštaj o sastavu voza / tehnički spisak voza (MÁV VTK „vonatterhelési kimutatás“, Zugvorbereitung, Meldezettel):
            // nije prateći dokument pošiljke – ne proverava se, ne traži se u r9.
            if (!formaCim && Ima("vonatterhelesi kimutatas", "vonatterhelési kimutatás", "vonatosszeallitas", "vonatösszeállítás", "tehervonat vonatterhelesi", "zugvorbereitung", "meldezettel",
                // v2.8.3: bugarski „Натурен лист на влак“ (BDŽ / Ekspres servis) – sastav voza, ne tovarni list
                "натурен лист на влак", "naturen list na vlak"))
            { nadjeno.Add("izveštaj o sastavu voza"); return Vrati(TipDokumenta.SastavVoza); }
            // Spisak kola po NASLOVU na vrhu strane („Kocsijegyzék / Wagenliste“) – i kada ima mnogo natpisa polja kao obrazac
            if (SpisakNaslov.Any(k => naslov.Contains(TekstUtil.Kljuc(k))) && !CimKljucevi.Any(k => naslov.Contains(TekstUtil.Kljuc(k))))
            { nadjeno.Add("naslov spiska kola"); return Vrati(TipDokumenta.SpisakKola); }
            // v2.9.2 (3.1a): „Wagen Liste / Wagenliste / Lista de vagoane / CIT 23“ u naslovu je spisak kola i kada naslov pominje
            // Frachtbrief / tovarni list („Wagenliste zum Frachtbrief“, CIT 23 Zmajevo-CR.pdf, ID 1655) – osim pravog obrasca CIM/CUV sa
            // mnogo natpisa rubrika (r18 „siehe Wagenliste“ nije u naslovu)
            var naslovSire = " " + TekstUtil.Kljuc(string.Join(" ", s.Reci.Where(w => w.Cy < 0.12).Select(w => w.T))) + " ";
            if ((SpisakNaslov.Any(k => naslovSire.Contains(TekstUtil.Kljuc(k))) || Regex.IsMatch(naslovSire, @" cit\s?-?\s?23 ")) && sidara < 20)
            { nadjeno.Add("naslov spiska kola (i uz pomen tovarnog lista)"); return Vrati(TipDokumenta.SpisakKola); }
            // v2.8.4 (1): nastavak spiska kola CIT 23 (strana 2/2: zaglavlje kolona „Kola br. – Wagen Nr.“, „Code NHM“, „Tara kola“,
            // „Bruto masa“ i red zbirova „Σ“) – nikad tovarni list, ni kada ima 8 natpisa koji liče na rubrike
            if (!formaCim && sidara < 20 && NastavakSpiska(s.Tekst, out var opisNastavka)) { nadjeno.Add(opisNastavka); return Vrati(TipDokumenta.SpisakKola); }

            // CIT 7 – naknadni nalog / izmena ugovora o prevozu (Nachträgliche Verfügung): poseban dokument, nije CIM/CUV
            if (Ima("nachtragliche verfugung", "nachtraegliche verfuegung", "naknadni zahtev", "naknadni nalog", "накнадни захтев", "накнадни налог", "subsequent order", "ordre ulterieur", "modification of the contract of carriage")
                || (Rec("cit7") || Ima("cit 7 ")) && !formaCim)
            { nadjeno.Add("CIT 7 naknadni nalog"); return Vrati(TipDokumenta.Cit7); }

            // Sadržaj strane ima prednost nad imenom fajla ("JCI and CIM….pdf", "WAGON 11.pdf"): obrazac CIM/CUV se prepoznaje
            // po naslovu obrasca ili po velikom broju odštampanih naziva rubrika.
            if (formaCim && sidara >= 8) { nadjeno.Add(sidara + " naziva rubrika"); return Vrati(TipDokumenta.Cim); }
            // v2.9.2 (3.1b): bez naslova obrasca CIM se prepoznaje po mreži rubrika (šablon poravnat: natpisi, brojevi i linije na
            // očekivanim mestima), ne po rečima u tekstu – opšti uslovi prodaje (Imerys, str. 6 u 11162026Scan150312.pdf) imaju reči
            // „consignor / consignee / carrier“, ali ne i mrežu obrasca
            if (sidara >= 20 && !naslovSpiska)
            {
                if (sidara >= 40 || MrezaObrasca(s)) { nadjeno.Add(sidara + " naziva rubrika"); return Vrati(TipDokumenta.Cim); }
                nadjeno.Add(sidara + " reči kao nazivi rubrika, bez mreže obrasca – nije tovarni list");
                return Vrati(TipDokumenta.Ostalo);
            }
            // Spisak kola (CIT 23) po naslovu – pre carinskih (spisak navodi i MRN / carinski dokument)
            if (naslovSpiska && Ima("cit-23", "cit 23", "cit23", "spisak kola", "списак кола", "wagenliste") && sidara < 20) return Vrati(TipDokumenta.SpisakKola);

            // Dozvola za prekogranično kretanje otpada (rešenje ministarstva) i dokument o kretanju otpada (Annex VII)
            if (Ima("upravljanju otpadom", "zastite zivotne sredine", "zivotne sredine", "ministarstvo zastite") && Ima("dozvol", "ao3bo", "resenje", "pewebe", "otpad", "ornan", "ormana", "izvoz"))
                return Vrati(TipDokumenta.DozvolaOtpad);
            if (Ima("resenje o izdavanju dozvole", "dozvola za izvoz", "dozvole za izvoz", "dozvol", "ao3bo") && Ima("otpad", "ornan", "ormana")) return Vrati(TipDokumenta.DozvolaOtpad);
            if (Ima("annex vii", "prilog vii", "anhang vii") && Ima("waste", "otpad", "abfall", "rifiuti", "information accompanying", "informacije koje prate")) return Vrati(TipDokumenta.AnnexVii);
            if (Ima("radiolosk", "radiometric", "radiometrijsk", "kontrola radioaktivnosti", "radioaktivnost", "radiological")) return Vrati(TipDokumenta.IzvestajRadioloski);
            // Overa javnog beležnika – nije carinski dokument (ni nastavak prethodnog)
            if (Ima("javni beleznik", "javnog beleznika", "javni belježnik", "bejekhhk", "notar", "potvrduje se da je ova kopija")) return Vrati(TipDokumenta.Ostalo);

            // JCI (jedinstvena carinska isprava): izvozna ako je "carinarnica izvoza" / "izvoz", inače ostali carinski dokument
            // (v2.8.4: i „Specifikacija prevoznih sredstava uz rubriku 18 jedinstvene carinske isprave“ – prilog JCI, pouzdano carinski dokument)
            var jci = Ima("jedinstvena carinska isprava", "jedinstvene carinske isprave", "specifikacija prevoznih sredstava", "carinarnica izvoza", "carinska isprava", "jequhctbeha", "liaphhcka", "ucnpaba")
                      || (Ima("deklaracija", "deklaracij") && JciBroj.IsMatch(s.Tekst));
            if (jci) return Vrati(Ima("izvoz", "export", "m3bo", "u3bo", "izvoza") ? TipDokumenta.IzvoznaDeklaracija : TipDokumenta.DrugaCarinska);
            // Tranzitna deklaracija (T1/TAD): natpis tranzitnog dokumenta I (MRN ili polja NCTS LRN + UCR/TIR)
            // MRN: (godina) + ISO šifra zemlje + 14 znakova sa bar 4 cifre – ne bilo koji niz velikih slova
            var mrn = MrnUTekstu.Matches(s.Tekst.ToUpperInvariant()).Cast<Match>().Any(m =>
            {
                var v = m.Value; var z = v.Length == 18 ? v.Substring(2, 2) : v.Substring(0, 2);
                return MrnZemlje.Contains(z) && v.Count(char.IsDigit) >= 6;
            });
            if (Ima("tranzitni prateci dokument", "transit accompanying document", "tranzitni prateci", "prateci tranzitni", "versandbegleitdokument", "tranzitnog", "tramzitn", "tpah3", "tpahsh", "tpahзит")
                && (mrn || (Rec("lrn") && Rec("ucr", "tir"))))
                return Vrati(TipDokumenta.TranzitnaDeklaracija);
            // EU tranzitna deklaracija na mađarskom/nemačkom („Árunyilatkozat … T2 … Indító vámhivatal“): odlazna carinarnica + T1/T2 + MRN ili LRN
            if (Ima("indito vamhivatal", "abgangsstelle", "office of departure", "polazna carinarnica") && Rec("t1", "t2", "t2l", "t2f") && (mrn || Rec("lrn")))
                return Vrati(TipDokumenta.TranzitnaDeklaracija);
            // Naslov nečitljiv na skenu (npr. ćirilica kroz OCR): sva tri NCTS polja (LRN, UCR, TIR) i odredišna carinska ispostava.
            if (Rec("lrn") && Rec("ucr") && Rec("tir") && Ima("odredisn", "одредишн", "office of destination", "bestimmungsstelle"))
                return Vrati(TipDokumenta.TranzitnaDeklaracija);

            // ---- v2.8: SADRŽAJ pre imena fajla. Ime fajla nikad ne određuje tip strane kada sadržaj daje jasan rezultat
            // („SET EXPORT TREN SARE.pdf“: str. 4 je CUV, str. 3 faktura, str. 6 EUR.1 – ne sve „izvozna deklaracija“).
            // Natpis obrasca CIM/CUV („Lettre de voiture CIM“, „Wagenbrief CUV“, „Tovarni list“…) i bar 2 natpisa rubrika (sken)
            if (formaCim && sidara >= 2 && !naslovSpiska) { nadjeno.Add("naslov obrasca, " + sidara + " naziva rubrika"); return Vrati(TipDokumenta.Cim); }
            // Natpisi rubrika bez naslova obrasca: kopija lista („Duplikat“, list 2/3, „Feuille …“) ili poleđina – ne faktura
            // („Frankaturrechnung“ je natpis rubrike 52, ne faktura)
            if (sidara >= 8 && !naslovSpiska) { nadjeno.Add(sidara + " naziva rubrika bez naslova – kopija ili poleđina lista"); return Vrati(TipDokumenta.KopijaLista); }
            // v2.9.2 (3a.8): ime fajla kaže CIM, a strana ima sadržaj tovarnog lista (platilac „Frachtzahler …“, „CIM 6: …“, 13.3 prevoznici,
            // „List of wagons CIT 23“, „Tara:“) – sken čiji natpisi obrasca nisu pročitani („CIM za voz br. 262 ….pdf“: crno-beli sken, 2 natpisa)
            // je tovarni list, ne faktura (r9 navodi „Faktura/invoice“)
            if (ImeIma(CimImena) && !naslovSpiska && SadrzajTovarnogLista(t) >= 3) { nadjeno.Add("ime fajla i sadržaj tovarnog lista"); return Vrati(TipDokumenta.Cim); }
            // Faktura pre EUR.1: "Invoice / Purchase Contract / Total" je faktura i kada sadrži "EUR" i broj; reči samo kao cele reči
            var fakturaZnaci = new[] { "purchase contract", "shipment method", "payment terms", "total", "amount", "price" }.Count(x => t.Contains(" " + x));
            if (Rec("invoice", "inovice", "faktura", "rechnung", "facture", "factura", "proforma") || Ima("racun br", "commercial invoice") || fakturaZnaci >= 2)
            { if (fakturaZnaci >= 2) nadjeno.Add(fakturaZnaci + " oznaka fakture"); return Vrati(TipDokumenta.Faktura); }
            // EUR.1 samo po natpisu uverenja o kretanju robe (ne po "EUR" + "1" iz zaglavlja tabele)
            if (Ima("movement certificate", "uverenje o kretanju robe", "warenverkehrsbescheinigung", "certificat de circulation")) return Vrati(TipDokumenta.Eur1);
            if (Ima("export accompanying document", "izvozni prateci", "prateci izvozni", "ausfuhrbegleitdokument", "izvozna deklaracija", "export declaration"))
                return Vrati(TipDokumenta.IzvoznaDeklaracija);
            if (Ima("transit accompanying document", "tranzitni prateci", "prateci tranzitni", "versandbegleitdokument", "transit declaration") && mrn)
                return Vrati(TipDokumenta.TranzitnaDeklaracija);
            if (Ima("entry summary declaration", "jedinstvena carinska isprava")) return Vrati(TipDokumenta.DrugaCarinska);
            if (spisakTekst && Ima("wagenliste", "spisak kola", "wagon list", "списак кола", "liste des wagons", "cit 23", "cit23") && sidara < 20) return Vrati(TipDokumenta.SpisakKola);

            // ---- ime fajla: samo kada sadržaj strane nije dao rezultat
            var poImenu = ExcelCitac.TipPoImenu(fajl);
            if (poImenu != null) { nadjeno.Add("ime fajla"); return Vrati(poImenu); }
            if (ImeIma("vdd", "ead", "izvoz", "export", "ex-a", "exa", "ecs")) return Vrati(TipDokumenta.IzvoznaDeklaracija);
            if (ImeIma("t1", "t-1", "tad", "t2", "ncts")) return Vrati(TipDokumenta.TranzitnaDeklaracija);
            if (ImeIma("ens", "jci")) return Vrati(TipDokumenta.DrugaCarinska);
            if (ImeIma("fv", "faktura", "invoice", "rechnung")) return Vrati(TipDokumenta.Faktura);
            if (ImeIma("eur1", "eur.1", "eur 1")) return Vrati(TipDokumenta.Eur1);

            if (ImeIma(SpisakImena) && !formaCim) return Vrati(TipDokumenta.SpisakKola);
            if (formaCim && sidara >= 8 && !(spisakTekst && sidara < 15 && !Ima("frachtbrief cim", "consignment note", "tovarni list", "товарни лист"))) return Vrati(TipDokumenta.Cim);
            if (spisakTekst && Ima("wagenliste", "spisak kola", "wagon list", "списак кола", "liste des wagons", "cit 23", "cit23") && sidara < 20 && !ImeIma(CimImena)) return Vrati(TipDokumenta.SpisakKola);
            if (ImeIma(CimImena) && s.Reci.Count >= 20) return Vrati(TipDokumenta.Cim); // elektronski ispis bez odštampanih naziva rubrika
            if (spisakTekst) return Vrati(TipDokumenta.SpisakKola);

            if (Ima("export accompanying document", "izvozni prateci", "prateci izvozni", "ausfuhrbegleitdokument", "izvozna deklaracija", "export declaration", "declaration d export")
                || ImeIma("vdd", "ead", "izvoz", "export", "ex-a", "exa")) return Vrati(TipDokumenta.IzvoznaDeklaracija);
            if ((Ima("transit accompanying document", "tranzitni prateci", "prateci tranzitni", "versandbegleitdokument", "transit declaration") && mrn)
                || ImeIma("t1", "t-1", "tad", "t2")) return Vrati(TipDokumenta.TranzitnaDeklaracija);
            if (Ima("entry summary declaration", "jedinstvena carinska", "customs declaration", "carinska deklaracija") || ImeIma("ens", "jci")) return Vrati(TipDokumenta.DrugaCarinska);
            if (Ima("certificate of origin", "uverenje o poreklu", "ursprungszeugnis")) return Vrati(TipDokumenta.UverenjeOPoreklu);
            if (Ima("dodatni list", "additional sheet", "zusatzblatt", "erganzungsblatt", "feuille complementaire") || ImeIma("dodatni")) return Vrati(TipDokumenta.DodatniList);
            if (Ima("certificate", "sertifikat", "zertifikat", "certifikat")) return Vrati(TipDokumenta.Sertifikat);
            if (Ima("k-200", "kol-65", "teretnica")) return Vrati(TipDokumenta.Interni);
            if (Ima("najava", "наява", "announcement")) return Vrati(TipDokumenta.Najava);
            nadjeno.Clear();
            return Vrati(TipDokumenta.Ostalo);
        }

        /// <summary>Reči natpisa obrasca koje OCR često delimično pokvari („Frachtbrlef“, „Wagenbnef“, „Fuvarleve!“).</summary>
        private static readonly string[] NatpisObrascaReci = { "frachtbrief", "wagenbrief", "fuvarlevel", "frachtbriefdoppel" };

        /// <summary>
        /// v2.8.4: strana liči na obrazac CIM/CUV – opis zašto (za napomenu ručne provere), ili null: bar 8 natpisa rubrika, delimično
        /// pročitan natpis obrasca (reč do 2 slova razlike), ili poznat šablon poravnat po okviru i brojevima rubrika (uz bar 3 natpisa).
        /// </summary>
        private string LiciNaObrazac(Strana s)
        {
            var sidara = sablon.BrojSidara(s.Reci);
            // v2.9.2 (3.1b): reči koje liče na natpise rubrika nisu dovoljne (opšti uslovi prodaje, carinske strane) – potreban je
            // natpis obrasca, mreža rubrika (šablon) ili vrlo mnogo natpisa
            if (sidara >= 40) return sidara + " natpisa rubrika";
            var t = " " + TekstUtil.Kljuc(s.Tekst) + " ";
            if (CimKljucevi.Any(k => t.Contains(" " + TekstUtil.Kljuc(k) + " "))) return "natpis obrasca";
            var natpis = Regex.Matches(t, @"\p{L}{8,}").Cast<Match>().Select(m => m.Value)
                .FirstOrDefault(w => NatpisObrascaReci.Any(k => Math.Abs(w.Length - k.Length) <= 2 && TekstUtil.Rastojanje(w, k) <= 2));
            if (natpis != null) return "delimično pročitan natpis obrasca „" + natpis + "“";
            if (sidara >= 3 && s.Raster != null && sablon.Rasporedi(s.Reci, s.Raster) is CimSablon.Rezultat pr && pr.Poravnato) return "okvir i brojevi rubrika obrasca " + pr.Obrazac.Id;
            return null;
        }

        /// <summary>Šifra stanice UIC sa kontrolnom cifrom: zemlja (2 cifre) + stanica (5 cifara) + „-k“ („72 16513-4“, „83 05319-9“).</summary>
        internal static readonly Regex SifraStaniceUic = new Regex(@"(?<![\d.,/])\d{2}\s?\d{5}\s?-\s?\d(?![\d.,/])");

        /// <summary>Broj različitih brojeva kola (12 cifara, „33 84 0764 340-6“, „3384 0764 340-6“) u tekstu strane.</summary>
        internal static int BrojKolaNaStrani(string tekst) =>
            Regex.Matches(tekst ?? "", @"(?<!\d)\d{2}\s?\d{2}\s?\d{4}\s?\d{3}\s?-?\s?\d(?!\d)").Cast<Match>().Select(m => Regex.Replace(m.Value, @"\D", "")).Distinct().Count();

        /// <summary>Zaglavlja kolona spiska kola CIT 23 (svaka grupa se broji jednom).</summary>
        private static readonly string[][] KoloneSpiska =
        {
            new[] { "wagen nr", "wagennr", "wagen nummer", "kola br", "wagon no", "wagon nr" },
            new[] { "nhm" },
            new[] { "tara", "eigenmasse", "eigengewicht" },
            new[] { "bruto", "brutto", "bruttomasse", "gross" },
            new[] { "lfd. nr", "lfd nr", "redni broj", "r. br" },
        };

        /// <summary>
        /// v2.8.4 (1): nastavak spiska kola CIT 23 – bar 3 grupe zaglavlja kolona („Wagen Nr“, „NHM“, „Tara/Eigenmasse“, „Bruto/Brutto“,
        /// „Lfd. Nr“) ili red zbirova „Σ 112 … 1.884.000 / 563.150 / 2.447.150“ uz bar 2 grupe zaglavlja.
        /// </summary>
        internal static bool NastavakSpiska(string tekst, out string opis)
        {
            opis = null;
            var t = " " + TekstUtil.Kljuc(tekst ?? "") + " ";
            var grupa = KoloneSpiska.Count(g => g.Any(k => t.Contains(k)));
            // red zbirova: „Σ“ (OCR/tekst: „S“, „∑“, „E“) pa broj kola i bar dve mase sa tačkama hiljada
            var zbir = Regex.IsMatch(tekst ?? "", @"(?m)^\s*[ΣS∑E]\s+\d{1,4}\s+\d{1,3}(?:[.,]\d{3})+(?:\s+\d{1,3}(?:[.,]\d{3})+){1,}");
            if (grupa >= 3) { opis = "nastavak spiska kola (" + grupa + " zaglavlja kolona CIT 23" + (zbir ? ", red zbirova Σ" : "") + ")"; return true; }
            if (zbir && grupa >= 2) { opis = "nastavak spiska kola (red zbirova Σ)"; return true; }
            return false;
        }

        /// <summary>Neprepoznata strana je nastavak carinske deklaracije samo ako i sama liči na carinski dokument (polja "[18 06]", MRN, specifikacija…).</summary>
        private static bool LiciNaCarinsku(Strana s)
        {
            var t = " " + TekstUtil.Kljuc(s.Tekst) + " ";
            return Regex.IsMatch(s.Tekst, @"\[\s?\d{2}\s?\d{2}\s?[\]\}\)]") || MrnUTekstu.IsMatch(s.Tekst.ToUpperInvariant())
                   || new[] { "deklarac", "carin", "specifikacij", "dodatni list", "customs", "zoll" }.Any(k => t.Contains(k));
        }

        internal RezultatPriloga Klasifikuj(Prilog p, List<Strana> strane)
        {
            var rez = new RezultatPriloga();
            var napomene = new List<string>();
            var primerci = new List<Primerak>();
            int rb = 0;
            Dokument prethodni = null;
            string prethodniTip = null;
            var redoviSpiska = new List<Linija>();

            foreach (var s in strane)
            {
                if (!string.IsNullOrEmpty(s.Napomena)) napomene.Add("str. " + s.Broj + ": " + s.Napomena);
                if (s.Reci.Count < 5)
                {
                    rez.NeprocitaneStrane++;
                    napomene.Add("str. " + s.Broj + ": " + (s.Izvor == IzvorTeksta.Nema ? "bez teksta (sken koji OCR nije pročitao)" : "skoro prazna"));
                    prethodni = null; prethodniTip = null;
                    continue;
                }
                var tip = TipStrane(s, p.Naziv, out var razlogTipa);
                // v2.8.4 (2): sken obrasca čiji natpisi rubrika nisu pročitani (svetlo zelena štampa; okrenut sken voza 258: „Vidi spisak
                // kola CIT 23“ i „Wagon list“ u r9 → po rečima „spisak kola“): ako se šablon CIM/CUV pouzdano poravna po okviru i
                // brojevima rubrika, strana je tovarni list. Samo kada tip nije utvrđen naslovom ili zaglavljem kolona i nema redova kola.
                // (uz to sadržaj obrasca: šifra stanice sa kontrolnom cifrom „72 16513-4“ iz r12/r17/r62 – tabela „Specifikacija prevoznih sredstava
                // uz rubriku 18 JCI“ ima linije i cifre koje liče na okvir i krugove, ali nema šifru stanice)
                if ((tip == TipDokumenta.SpisakKola || tip == TipDokumenta.Ostalo) && s.Izvor == IzvorTeksta.Ocr && s.Raster != null
                    && !razlogTipa.Contains("naslov spiska") && !razlogTipa.Contains("nastavak spiska") && BrojKolaNaStrani(s.Tekst) < 3
                    && (SifraStaniceUic.IsMatch(s.Tekst) || (rez.Dokumenti.LastOrDefault() is Dokument pd && TipDokumenta.JeTovarniList(pd.Tip) && pd.Strane.Contains(s.Broj - 1))))
                {
                    var pr = sablon.Rasporedi(s.Reci, s.Raster);
                    if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1")
                    {
                        razlogTipa += " [šablon: " + pr.Poravnanje?.Obrazac + " " + (pr.Poravnato ? "poravnato" : pr.Poravnanje?.Razlog) + "; krugova " + s.Raster.Krugovi().Count + ", usp " + s.Raster.Uspravne.Count + ", vod " + s.Raster.Vodoravne.Count + ", ugao " + s.Ugao + "]";
                        try { File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "cimdbg_" + Regex.Replace(p.Naziv ?? "", @"[^\w]", "_") + "_" + s.Broj + ".png"), s.Slika); } catch { }
                    }
                    if (pr.Poravnato)
                    {
                        razlogTipa = "obrazac " + pr.Obrazac.Id + " prepoznat po okviru i brojevima rubrika (" + pr.Sidara + " sidara; po rečima: " + (razlogTipa.Length > 0 ? razlogTipa : tip) + ")";
                        tip = TipDokumenta.Cim;
                    }
                }
                rez.StraneOpis.Add("str." + s.Broj + " " + tip + " (" + (razlogTipa.Length > 0 ? razlogTipa + "; " : "") + s.SrednjaPouzdanost.ToString("0.00") + ")");
                // v2.8.4 (1): niska pouzdanost OCR-a blokira zaključak „nema CIM-a“ samo za stranu čiji tip nije utvrđen po sadržaju
                // (ostalo, ili samo po imenu fajla: „JCI and CIM….pdf“) – spisak kola prepoznat po natpisu „CIT 23“ nije skriveni tovarni list
                if (s.Izvor == IzvorTeksta.Ocr && s.SrednjaPouzdanost < 0.75 && !TipDokumenta.JeTovarniList(tip) && tip != TipDokumenta.KopijaLista
                    && (tip == TipDokumenta.Ostalo || razlogTipa.Length == 0 || razlogTipa.Contains("ime"))) rez.NisaPouzdanost.Add(s.Broj);
                // (v2.8.4: samo nepouzdan sken – pouzdano pročitana strana bez natpisa obrasca nije tovarni list, tačka 1.2)
                if (tip == TipDokumenta.Ostalo && s.Izvor == IzvorTeksta.Ocr && s.SrednjaPouzdanost < 0.75 && sablon.BrojSidara(s.Reci) >= 3)
                {
                    // Liči na CIM/CUV obrazac, ali OCR nije pročitao dovoljno naziva rubrika – ne sme da se zaključi "nema CIM-a".
                    rez.NeprocitaneStrane++;
                    napomene.Add("str. " + s.Broj + ": liči na CIM/CUV obrazac, ali nije pouzdano pročitana (OCR pouzdanost " + s.SrednjaPouzdanost.ToString("0.00") + ")");
                }

                // Kopija ili poleđina lista (natpisi rubrika bez naslova obrasca): pripaja se poslednjem tovarnom listu u fajlu;
                // nije faktura i nije prilog u r9. Bez lista ispred – čita se kao tovarni list.
                if (tip == TipDokumenta.KopijaLista)
                {
                    var list = rez.Dokumenti.LastOrDefault(d => TipDokumenta.JeTovarniList(d.Tip));
                    if (list != null)
                    {
                        list.Strane.Add(s.Broj); list.Opis += "; str. " + s.Broj + " kopija/poleđina lista";
                        prethodni = null; prethodniTip = null;
                        continue;
                    }
                    // v2.8.4 (1): bez lista ispred – tovarni list je samo strana sa obeležjima obrasca (natpis „Frachtbrief / Lettre de voiture /
                    // Tovarni list / Consignment note / Wagenbrief“ i brojevi rubrika). Natpisi koji samo liče na rubrike (zaglavlje tabele,
                    // „Masse“, „NHM“) nisu „neprepoznat CIM“. Izuzeci (oprez – list se ne sme prevideti): ime fajla CIM/CUV ili nepouzdan sken.
                    var nepouzdanSken = s.Izvor == IzvorTeksta.Ocr && s.SrednjaPouzdanost < 0.75;
                    if (!ImeFajlaCim(p.Naziv) && !nepouzdanSken)
                    {
                        tip = TipDokumenta.Ostalo;
                        rez.StraneOpis[rez.StraneOpis.Count - 1] = "str." + s.Broj + " ostalo (" + razlogTipa + " – bez natpisa obrasca, nije tovarni list; " + s.SrednjaPouzdanost.ToString("0.00") + ")";
                    }
                    else tip = TipDokumenta.Cim;
                }

                // Poleđina lista (računski odseci, preslikan tekst prednje strane) nije novi list i ne proverava se.
                if (tip != TipDokumenta.Cim && sablon.Obrasci.Poledjina(s.Tekst) != null && rez.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)))
                {
                    DodajPoledjinu(rez, s, napomene, sablon.Obrasci.Poledjina(s.Tekst));
                    prethodni = null; prethodniTip = null;
                    continue;
                }

                if (tip == TipDokumenta.Cim)
                {
                    var raspored = Rasporedi(p, s);
                    var izvor = s.Izvor == IzvorTeksta.Ocr ? "ocr" : "tekst";
                    if (!raspored.Poravnato)
                    {
                        var pol = sablon.Obrasci.Poledjina(s.Tekst);
                        if (pol != null)
                        {
                            DodajPoledjinu(rez, s, napomene, pol);
                            prethodni = null; prethodniTip = null;
                            continue;
                        }
                        // Drugi primerak istog (neprepoznatog) lista: strana sa nazivom primerka iza neprepoznatog lista iz istog fajla – nije novi list.
                        var primerakNep = NazivPrimerka(s);
                        var poslednji = rez.Dokumenti.LastOrDefault();
                        // (samo primerci 2–5; „Original“ može biti prvi primerak sledećeg lista)
                        if (primerakNep != null && !primerakNep.StartsWith("Original") && primerakNep != "Eredeti" && poslednji != null && TipDokumenta.JeTovarniList(poslednji.Tip) && poslednji.Cim.Count == 1 && !string.IsNullOrEmpty(poslednji.Cim[0].NeprepoznatObrazac))
                        {
                            poslednji.Strane.Add(s.Broj); poslednji.Opis += "; str. " + s.Broj + " primerak „" + primerakNep + "“";
                            prethodni = null; prethodniTip = null;
                            continue;
                        }
                        // Obrazac nije prepoznat ili poravnanje nije pouzdano: rubrike se ne proveravaju (ne nasumično) – ručna provera.
                        var opis = OpisNeprepoznatog(s, raspored);
                        var nep = new CimList { RedniBroj = ++rb, NeprepoznatObrazac = opis, Sablon = raspored.Poravnanje?.Obrazac ?? "", Zaglavlje = Zaglavlje(s) };
                        rez.Dokumenti.Add(new Dokument { Tip = TipDokumenta.Cim, Strane = { s.Broj }, Cim = { nep }, Izvor = izvor, Opis = "tovarni list – " + opis });
                        Zapisi(p.Naziv, s, raspored, nep);
                        prethodni = null; prethodniTip = null;
                        continue;
                    }
                    var cim = CimParser.Napravi(raspored, s, Pragovi);
                    // Nečitljive rubrike skena: ponovni OCR samo njihovog pravougaonika (veća rezolucija, srp/hrv/deu).
                    reciPonovljene = null;
                    if (PonoviNecitljive(p, s, raspored, cim) is CimSablon.Rezultat bolji) { raspored = bolji; cim = CimParser.Napravi(raspored, s, Pragovi); }
                    // v2.8.4 (3): r7 skena bez platioca za Srbiju (opšti OCR: „p / i / GPR, SŽ-TP ij is Beograd.“) – isečak r7 se ponovo čita
                    if (PonoviR7(p, s, raspored, cim) is CimSablon.Rezultat boljiR7) { raspored = boljiR7; cim = CimParser.Napravi(raspored, s, Pragovi); }
                    // v2.9.2 (2): r57 / r58a skena bez KP (OCR: „32 / 04 jKARPAT“ umesto „3212 KP / 3404 H-KÁRPÁT“; pečat „UIC CODE KP 3212“
                    // preko r58) – isečak se ponovo čita; novo čitanje se prihvata samo kada u njemu ima KP (naziv ili 3212)
                    if (kp != null)
                    {
                        if (PonoviRubriku(p, s, raspored, cim, "57", c0 => { var (n, u) = Provere.KpUR57(c0.R57, kp); return n || u; },
                                c0 => { var (n, u) = Provere.KpUR57(c0.R57, kp); return (n ? 1 : 0) + (u ? 1 : 0); }) is CimSablon.Rezultat bolji57)
                        { raspored = bolji57; cim = CimParser.Napravi(raspored, s, Pragovi); }
                        if (PonoviRubriku(p, s, raspored, cim, "58a", c0 => kp.JeKpSifra(c0.R58a.Sifra) || kp.JeKp(c0.R58a.Tekst), c0 => kp.JeKpSifra(c0.R58a.Sifra) || kp.JeKp(c0.R58a.Tekst) ? 1 : 0) is CimSablon.Rezultat bolji58)
                        { raspored = bolji58; cim = CimParser.Napravi(raspored, s, Pragovi); }
                    }
                    // Word → PDF: vrednosti su tekstualni sloj (bez OCR-a) – nema nečitljivih rubrika
                    // PDF sa poljima obrasca (AcroForm, npr. GK30): vrednosti su u poljima – štampane tačkice u r25 nisu nečitljiv upis
                    if (raspored.IzWorda || s.Reci.Any(w => w.Font == "AcroForm")) CimParser.BezNecitljivih(cim);
                    // pouzdanost OCR-a strane: sken ispod 0,75 – nalazi o sadržaju rubrika postaju interne napomene (Motor, v2.8)
                    if (s.Izvor == IzvorTeksta.Ocr) cim.PouzdanostStrane = Math.Round(s.SrednjaPouzdanost, 2);
                    cim.Sablon = raspored.Obrazac.Id;
                    cim.StranaLista = s.Broj;
                    foreach (var kv in raspored.Obrazac.Rubrike) if (raspored.NaStrani(kv.Value) is double[] ok) cim.Okviri[kv.Key] = ok.Select(v => Math.Round(v, 4)).ToArray();
                    // v2.8.2: kućice (62.1–62.4, 16, 12…) – PDF primer upisuje svaki deo u svoje polje
                    foreach (var kv in raspored.Obrazac.Kucice) if (raspored.NaStrani(kv.Value) is double[] ok) cim.KuciceOkviri[kv.Key] = ok.Select(v => Math.Round(v, 4)).ToArray();
                    cim.Zaglavlje = Zaglavlje(s);
                    // Prednja strana lista: ima pošiljaoca ili primaoca i bar jednu od rubrika 62/16/29 (prazan obrazac nema).
                    var imaPodataka = (cim.R1.Popunjena || cim.R4.Popunjena) && (cim.R62.Popunjena || cim.R16.Popunjena || cim.R29.Popunjena || cim.R21.Popunjena);
                    if (!imaPodataka) { prethodni = null; prethodniTip = null; continue; }
                    // Drugi primerak istog lista (original, duplikat…): OCR ga pročita sa malo drugačijim šumom, pa se poredi tolerantno.
                    var primerak = Primerak.Od(cim);
                    // v2.8.3: strana sa nazivom primerka „Original“ je prvi primerak NOVOG lista (TCDD: 12 listova istog pošiljaoca i
                    // primaoca, r62/r18 na skenu nepouzdani) – nikad se ne pripaja prethodnom
                    var np0 = NazivPrimerka(s);
                    var isti = np0 != null && (np0.StartsWith("Original") || np0 == "Eredeti") ? null : primerci.FirstOrDefault(x => x.IstiList(primerak));
                    if (isti != null)
                    {
                        // primerak („Foglio di via“, „Duplicata“, „Ankunftsschein“…): strana ide uz isti list
                        var dl = rez.Dokumenti.LastOrDefault(x => x.Cim.Any(c => Primerak.Od(c).IstiList(primerak)));
                        if (dl != null) { dl.Strane.Add(s.Broj); dl.Opis += "; str. " + s.Broj + " primerak" + (NazivPrimerka(s) is string np ? " „" + np + "“" : ""); }
                        prethodni = null; continue;
                    }
                    primerci.Add(primerak);
                    cim.RedniBroj = ++rb;
                    var d = new Dokument
                    {
                        Tip = cim.Obrazac == "CUV" ? TipDokumenta.Cuv : TipDokumenta.Cim, Strane = { s.Broj }, Cim = { cim }, Izvor = izvor,
                        Opis = (cim.Obrazac == "CUV" ? "CUV" : "CIM") + " list " + rb + " (" + (p.IzWorda ? "Word pretvoren u PDF, podaci iz tekstualnih okvira" : s.Izvor == IzvorTeksta.Ocr ? "OCR, pouzdanost " + s.SrednjaPouzdanost.ToString("0.00") : "tekst iz PDF-a")
                               + ", obrazac " + raspored.Obrazac.Id + ", " + raspored.Sidara + " sidara)",
                    };
                    rez.Dokumenti.Add(d);
                    Zapisi(p.Naziv, s, raspored, cim);
                    prethodni = null; prethodniTip = null;
                    continue;
                }

                if (tip == TipDokumenta.SpisakKola && prethodniTip == TipDokumenta.SpisakKola && prethodni != null)
                {
                    prethodni.Strane.Add(s.Broj);
                    redoviSpiska.AddRange(Raspored.Linije(s.Reci));
                    ZameniSpisak(prethodni, redoviSpiska, s);
                    DopuniRubrikeLista(rez, s);
                    continue;
                }
                // Nastavak dokumenta (ista vrsta), ili neprepoznata strana odmah iza carinske deklaracije (druga strana T1/JCI, specifikacija).
                if (prethodni != null && ((tip == prethodniTip && tip != TipDokumenta.Ostalo) || (tip == TipDokumenta.Ostalo && TipDokumenta.JeCarinski(prethodniTip) && LiciNaCarinsku(s))))
                {
                    prethodni.Strane.Add(s.Broj);
                    if (prethodni.Carinski.Count > 0) DopuniCarinske(prethodni.Carinski[0], s);
                    if (prethodni.Tip == TipDokumenta.DozvolaOtpad && prethodni.Carinski.Count > 0) DopuniEwc(prethodni.Carinski[0], s);
                    if (prethodni.Incoterms.Length == 0) prethodni.Incoterms = Incoterms(s.Tekst);
                    continue;
                }

                // v2.8.4: „bez odgovora“ samo kada je sigurno – samostalna strana „ostalo“ koja liči na obrazac CIM/CUV (8+ natpisa rubrika,
                // delimično pročitan natpis obrasca, ili okvir i brojevi rubrika poznatog šablona), a nije pročitana kao tovarni list → ručna provera
                if (tip == TipDokumenta.Ostalo && !(s.Izvor == IzvorTeksta.Ocr && s.SrednjaPouzdanost < 0.75 && sablon.BrojSidara(s.Reci) >= 3)
                    && LiciNaObrazac(s) is string licnost)
                {
                    rez.NeprocitaneStrane++;
                    napomene.Add("str. " + s.Broj + ": liči na obrazac CIM/CUV (" + licnost + "), a nije pročitana kao tovarni list – ručna provera");
                }
                var dok = new Dokument { Tip = tip, Strane = { s.Broj }, Izvor = s.Izvor == IzvorTeksta.Ocr ? "ocr" : "tekst", Incoterms = Incoterms(s.Tekst) };
                switch (tip)
                {
                    case TipDokumenta.SpisakKola:
                        redoviSpiska = Raspored.Linije(s.Reci);
                        ZameniSpisak(dok, redoviSpiska, s);
                        // mađarski „Kocsijegyzék“ nosi i odeljke „Rovat 7 / Feld 7“, „Rovat 13 / Feld 13“ (kao dodatni list)
                        if (DopuniRubrikeLista(rez, s) is List<string> sp && sp.Count > 0) dok.Opis += " (r" + string.Join(", r", sp) + " spojene sa listom)";
                        break;
                    case TipDokumenta.IzvoznaDeklaracija: case TipDokumenta.TranzitnaDeklaracija: case TipDokumenta.DrugaCarinska:
                        var c = new CarinskiPodaci(); DopuniCarinske(c, s); dok.Carinski.Add(c);
                        dok.Opis = c.Mrn.Count > 0 ? "MRN " + string.Join(", ", c.Mrn.Take(3)) : "";
                        break;
                    case TipDokumenta.DozvolaOtpad:
                        var co = new CarinskiPodaci(); DopuniEwc(co, s); dok.Carinski.Add(co);
                        break;
                    case TipDokumenta.Najava:
                        // v2.8.3: najava voza se ne proverava; u log samo broj voza, stanice, broj kola, težina, dužina i ETA
                        dok.Opis = NajavaVoza.Opis(s.Tekst);
                        if (rez.StraneOpis.Count > 0) rez.StraneOpis[rez.StraneOpis.Count - 1] += " – " + dok.Opis;
                        break;
                    case TipDokumenta.Cit7:
                        // v2.8.1: naknadni zahtev se čita (izmene, identifikacija, podnosilac) – proverava se uz tovarni list na koji se odnosi
                        dok.Cit7 = Cit7Citac.Procitaj(s);
                        dok.Opis = "CIT 7 – naknadni zahtev" + (dok.Cit7.Identifikacija.Length > 0 ? " za " + dok.Cit7.Identifikacija : "")
                                   + (dok.Cit7.Izmene.Count > 0 ? " (izmena r" + string.Join(", r", dok.Cit7.Izmene.Select(x => x.Rubrika)) + ")" : "");
                        break;
                    case TipDokumenta.DodatniList:
                        dok.DodatniList.Add(new DodatniListPodaci
                        {
                            BrojPosiljke = Regex.IsMatch(s.Tekst, @"\b\d{5}-?\d\b"),
                            DatumPreuzimanja = CimParser.NadjiDatum(s.Tekst, out _, out _).Length > 0,
                        });
                        // Odeljci „Rovat 7 / Feld 7“, „Rovat 13 / Feld 13“ dopunjuju rubrike prethodnog (pročitanog) tovarnog lista u istom fajlu.
                        var spojene = DopuniRubrikeLista(rez, s);
                        dok.Opis = spojene == null ? "dodatni list" : "dodatni list uz CIM list " + rez.Dokumenti.Last(x => TipDokumenta.JeTovarniList(x.Tip)).Cim.Last().RedniBroj
                                   + (spojene.Count > 0 ? " (r" + string.Join(", r", spojene) + " spojene sa listom)" : "");
                        break;
                }
                // Tabela MRN po kontejneru u PDF-u (npr. "TABELICA")
                if (tip == TipDokumenta.Ostalo && Regex.Matches(s.Tekst.ToUpperInvariant(), @"\b\d{2}[A-Z]{2}[A-Z0-9]{14}\b").Count >= 2)
                {
                    dok.Tip = TipDokumenta.TabelaMrn;
                    var c = new CarinskiPodaci(); DopuniCarinske(c, s); dok.Carinski.Add(c);
                }
                rez.Dokumenti.Add(dok);
                prethodni = dok; prethodniTip = dok.Tip;
            }
            DopuniMrnOcrom(p, rez);
            foreach (var d in rez.Dokumenti.Where(x => TipDokumenta.JeCarinski(x.Tip) && x.Carinski.Count > 0))
                d.Opis = d.Carinski[0].Mrn.Count > 0 ? "MRN " + string.Join(", ", d.Carinski[0].Mrn.Take(3)) : "";
            rez.Napomena = string.Join("; ", napomene);
            return rez;
        }

        /// <summary>
        /// EWC (indeksni) brojevi otpada iz dozvole: grupe "dd dd dd" sa poglavljem 01–20 ("12 01 01", "19 12 02"),
        /// ne tarifne oznake (7204 10) ni brojevi rešenja.
        /// </summary>
        private static void DopuniEwc(CarinskiPodaci c, Strana s)
        {
            foreach (Match m in Regex.Matches(s.Tekst, @"(?<![\d/.,])(\d{2}) (\d{2}) (\d{2})(\*?)(?![\d/.,])"))
            {
                var pogl = int.Parse(m.Groups[1].Value);
                if (pogl < 1 || pogl > 20 || m.Groups[2].Value == "00") continue;
                var ewc = m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value + m.Groups[4].Value;
                if (!c.Ewc.Contains(ewc)) c.Ewc.Add(ewc);
            }
        }

        private static readonly HashSet<string> MrnZemlje = new HashSet<string>
        {
            "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU","IE","IT","LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","ES","SE",
            "RS","BA","ME","MK","AL","XK","TR","UA","MD","CH","NO","GB","XI","AD","SM","GE","BY","RU",
        };

        /// <summary>MRN: 2 cifre godine + ISO šifra zemlje + 14 znakova (npr. 26RS011096C97P8ZK5); "22HIR…" nije MRN.</summary>
        internal static bool JeMrn(string m) => Regex.IsMatch(m ?? "", @"^\d{2}[A-Z]{2}[A-Z0-9]{14}$") && MrnZemlje.Contains(m.Substring(2, 2));

        /// <summary>Uslov isporuke (Incoterms + mesto) iz teksta fakture / deklaracije, npr. "DAP Alesd".</summary>
        internal static string Incoterms(string tekst)
        {
            var m = Regex.Match(tekst ?? "", @"(?<![A-Za-z])(EXW|FCA|FAS|FOB|CFR|CIF|CPT|CIP|DAP|DPU|DDP|DAF|DDU)(?![A-Za-z])[\s|:,\-]*([A-Za-zĀ-žА-я][A-Za-zĀ-žА-я\-\.]{2,30})?");
            if (!m.Success) return "";
            var mesto = m.Groups[2].Value.Trim('.', '-');
            if (Regex.IsMatch(mesto, @"^(incoterms?|paritet|delivery|terms?)$", RegexOptions.IgnoreCase)) mesto = "";
            return (m.Groups[1].Value + " " + (mesto.Length > 0 ? char.ToUpperInvariant(mesto[0]) + mesto.Substring(1).ToLowerInvariant() : "")).Trim();
        }

        /// <summary>
        /// Carinska deklaracija bez ispravno pročitanog MRN (opšti OCR sa ćirilicom kvari latinične kodove): strana se
        /// čita još jednom samo velikim latiničnim slovima i ciframa i traži se MRN.
        /// </summary>
        private void DopuniMrnOcrom(Prilog p, RezultatPriloga rez)
        {
            foreach (var d in rez.Dokumenti.Where(x => TipDokumenta.JeCarinski(x.Tip) && x.Carinski.Count > 0))
            {
                var c = d.Carinski[0];
                c.Mrn.RemoveAll(m => !JeMrn(m));
                if (c.Mrn.Count > 0 || ocr == null || p?.Sadrzaj == null || p.Vrsta != VrstaPriloga.Pdf || d.Izvor != "ocr") continue;
                foreach (var broj in d.Strane.Take(2))
                {
                    try
                    {
                        var t = Regex.Replace(ocr.Alfanum(PdfCrtac.StranaUPng(p.Sadrzaj, broj, 300)), @"\s+", " ");
                        foreach (Match m in Regex.Matches(t, @"\b\d{2}[A-Z]{2}[A-Z0-9]{14}\b"))
                            if (JeMrn(m.Value) && !c.Mrn.Contains(m.Value)) c.Mrn.Add(m.Value);
                    }
                    catch (Exception ex) { log?.Upozorenje("MRN OCR nije uspeo (str. " + broj + "): " + ex.Message); }
                }
            }
        }

        /// <summary>Otisak lista za prepoznavanje primeraka istog tovarnog lista u jednom PDF-u.</summary>
        internal sealed class Primerak
        {
            public string Imena;   // pošiljalac + primalac, samo slova
            public string Kola;    // cifre iz r18
            public string Broj;    // broj otpravljanja iz r62

            public static Primerak Od(CimList c) => new Primerak
            {
                Imena = Regex.Replace(TekstUtil.Kljuc(c.R1.Naziv + " " + c.R1.Adresa + " " + c.R4.Naziv), @"[^a-z]", ""),
                Kola = Regex.Replace(c.R18.Tekst ?? "", @"\D", ""),
                Broj = Regex.Replace(c.R62.Broj ?? "", @"\D", ""),
            };

            public bool IstiList(Primerak o)
            {
                if (Imena.Length < 8 || o.Imena.Length < 8) return Imena == o.Imena && Kola == o.Kola && Broj == o.Broj;
                // različita kola ili različit broj pošiljke (kada su pouzdano pročitani) = različiti listovi (npr. 14 listova TCDD)
                if (Kola.Length >= 10 && o.Kola.Length >= 10 && TekstUtil.Rastojanje(Kola, o.Kola) > 2) return false;
                if (Broj.Length >= 5 && o.Broj.Length >= 5 && TekstUtil.Rastojanje(Broj, o.Broj) > 1) return false;
                var a = Imena.Length > 60 ? Imena.Substring(0, 60) : Imena;
                var b = o.Imena.Length > 60 ? o.Imena.Substring(0, 60) : o.Imena;
                return TekstUtil.Rastojanje(a, b) <= Math.Max(a.Length, b.Length) * 0.25;
            }
        }

        private void ZameniSpisak(Dokument d, List<Linija> linije, Strana s)
        {
            var redovi = linije.Select(l => l.Celije.Select(c => Regex.IsMatch(c, @"^(\d\s+)+\d$") ? Regex.Replace(c, @"\s", "") : c).ToArray()).ToList();
            var izvor = s.Izvor == IzvorTeksta.Ocr ? "ocr" : "tekst";
            var docs = ExcelCitac.AnalizaRedova(redovi, kp, d.Fajl, izvor, s.Izvor == IzvorTeksta.Ocr ? "OCR" : "PDF", s.Izvor == IzvorTeksta.Ocr ? s.SrednjaPouzdanost : 0.95);
            var sp = docs.FirstOrDefault(x => x.Tip == TipDokumenta.SpisakKola)?.Spisak.FirstOrDefault() ?? new SpisakKola { KpNaveden = linije.Any(l => kp.JeKp(l.Tekst)) };
            if (!sp.R62.Popunjena) sp.R62 = R62PoNatpisima(string.Join("\n", linije.Select(l => l.Tekst))) ?? R62SaSpiska(linije);
            if (!sp.R62.Popunjena || string.IsNullOrEmpty(sp.R62.Stanica) || string.IsNullOrEmpty(sp.R62.Broj))
                sp.R62 = R62PoObliku(string.Join("\n", linije.Select(l => l.Tekst)), kp.Uic) ?? sp.R62;
            sp.RidOznaka |= linije.Any(l => Regex.IsMatch(l.Tekst, @"\bRID\b[^\n]{0,20}\b[XxХх]\b"));
            sp.Prevoznici = linije.Select(l => l.Tekst).Where(t => Regex.IsMatch(t, @"(?<!\d)\d{4}(?!\d)") && (kp.JeKp(t) || Regex.IsMatch(t, @"cargo|rail|prevoz|train|logistic", RegexOptions.IgnoreCase))).Take(10).ToList();
            sp.Datum = CimParser.NadjiDatum(string.Join("\n", linije.Select(l => l.Tekst)), out _, out _);
            sp.MasaUpisana |= ExcelCitac.MasaNaSpisku(linije.Select(l => l.Tekst));
            d.Spisak.Clear(); d.Spisak.Add(sp);
            d.Carinski.Clear();
            var mrn = docs.FirstOrDefault(x => x.Tip == TipDokumenta.TabelaMrn);
            if (mrn != null) d.Carinski.AddRange(mrn.Carinski);
            d.Opis = "spisak kola (" + (s.Izvor == IzvorTeksta.Ocr ? "OCR" : "PDF") + ", " + sp.Kola.Count + " kola" + (sp.R62.Popunjena ? ", r62 " + sp.R62.Tekst : "") + ")";
        }

        /// <summary>
        /// Identifikacija pošiljke po natpisima polja (mađarski/nemački spisak kola i dodatni list):
        /// „Versandbahnhof: 55 12740-7“, „Versand EVU: 2155“, „Versandnummer: 71166-3“. null ako nema sva tri.
        /// </summary>
        internal static Identifikacija R62PoNatpisima(string tekst)
        {
            var st = Regex.Match(tekst ?? "", @"(?:Versandbahnhof|gare expéditrice|otpravna stanica)\W{0,4}(\d{2})\s*[/ ]?\s*(\d{5}-?\d)(?!\d)", RegexOptions.IgnoreCase);
            var evu = Regex.Match(tekst ?? "", @"(?:Versand\s*EVU|EVU)\W{0,4}(\d{4})(?!\d)", RegexOptions.IgnoreCase);
            var br = Regex.Match(tekst ?? "", @"(?:Versandnummer|numéro d.expédition|broj otpravljanja)\W{0,4}(\d{5}-?\d)(?!\d)", RegexOptions.IgnoreCase);
            if (!st.Success || !evu.Success || !br.Success) return null;
            var r = new Identifikacija { Zemlja = st.Groups[1].Value, Stanica = st.Groups[2].Value, Preduzece = evu.Groups[1].Value, Broj = br.Groups[1].Value, Status = StatusRubrike.Popunjeno, Pouzdanost = 0.95 };
            r.Tekst = r.Zemlja + " " + r.Stanica + " " + r.Preduzece + " " + r.Broj;
            return r;
        }

        /// <summary>Identifikacija pošiljke na spisku kola: redovi sa ciframa posle oznake 62 / "Sendungs-Identifikation" / "Podaci o pošiljci".</summary>
        private static Identifikacija R62SaSpiska(List<Linija> linije)
        {
            var i = linije.FindIndex(l => Regex.IsMatch(TekstUtil.Kljuc(l.Tekst), @"sendungs|identifik|podaci o posiljci|identification|идентифик|drzava|otpravljanje|versand|otpremanje"));
            var r = new Identifikacija();
            if (i < 0) return r;
            var brojevni = linije.Skip(i + 1).Take(6)
                .Where(l => Regex.IsMatch(l.Tekst, @"\d") && !Regex.IsMatch(l.Tekst, @"\p{L}{4,}") || l.Celije.Count(c => Regex.IsMatch(c, @"^[\d\s\-]+$")) >= 2)
                .Take(2).ToList();
            if (brojevni.Count == 0) return r;
            var samoBrojevi = brojevni.Select(l =>
            {
                var cel = l.Celije.Where(c => Regex.IsMatch(c, @"^[\d\s\-]+$"));
                var nl = new Linija();
                nl.Reci.Add(new Rec { T = string.Join("   ", cel) });
                return nl;
            }).ToList();
            r.Status = StatusRubrike.Popunjeno;
            r.Pouzdanost = brojevni.SelectMany(l => l.Reci).Average(x => x.Pouzdanost);
            r.Tekst = string.Join(" / ", samoBrojevi.Select(l => l.Tekst));
            CimParser.PopuniR62(r, samoBrojevi);
            return r;
        }

        /// <summary>
        /// Identifikacija pošiljke sa spiska kola po obliku brojeva: zemlja + stanica ("72/16550-6", "72 16550-6"), šifra
        /// preduzeća uz KP/UIC ("KP 3212") i broj otpravljanja ("15690-0", različit od stanice). null ako nema stanice i broja.
        /// </summary>
        internal static Identifikacija R62PoObliku(string tekst, string uic)
        {
            var st = Regex.Match(tekst ?? "", @"(?<![\d/])(\d{2})\s*[/ ]\s*(\d{5}-\d)(?![\d])");
            if (!st.Success) return null;
            var broj = Regex.Matches(tekst, @"(?<![\d/])(\d{5}-\d)(?![\d])").Cast<Match>().Select(m => m.Groups[1].Value).FirstOrDefault(x => x != st.Groups[2].Value);
            if (broj == null) return null;
            var pred = Regex.Match(tekst, @"(?:KP|UIC)\W{0,3}(\d{4})(?!\d)|(?<!\d)(\d{4})\W{0,3}KP\b");
            var r = new Identifikacija { Zemlja = st.Groups[1].Value, Stanica = st.Groups[2].Value, Broj = broj, Status = StatusRubrike.Popunjeno, Pouzdanost = 0.8 };
            r.Preduzece = pred.Success ? pred.Groups[1].Value + pred.Groups[2].Value : "";
            r.Tekst = string.Join(" ", new[] { r.Zemlja, r.Stanica, r.Preduzece, r.Broj }.Where(x => x.Length > 0));
            return r;
        }

        private static void DopuniCarinske(CarinskiPodaci c, Strana s)
        {
            foreach (var l in Raspored.Linije(s.Reci))
            {
                var t = l.Tekst.ToUpperInvariant();
                var mrn = Regex.Matches(t, @"\b\d{2}[A-Z]{2}[A-Z0-9]{14}\b").Cast<Match>().Select(m => m.Value).ToList();
                var uti = Regex.Match(t, @"\b[A-Z]{3}[UJZ]\s?-?\d{6}\s?-?\d\b").Value;
                foreach (var m in mrn)
                {
                    if (!c.Mrn.Contains(m)) c.Mrn.Add(m);
                    if (uti.Length > 0) c.MrnPoUti.Add(new Mrn { Broj = m, Uz = Regex.Replace(uti, @"[\s-]", "") });
                }
                foreach (Match a in Regex.Matches(t, @"\b\d{2}[A-Z]{2}[A-Z0-9]{16,17}\b"))
                    if (t.Contains("ARC") && !c.Arc.Contains(a.Value)) c.Arc.Add(a.Value);
                // Tarifna oznaka robe: 6, 8 ili 10 cifara ("262190", "2621 90 00"); čuva se prvih 6 cifara (HS).
                if (!Regex.IsMatch(l.Tekst, @"\b(PIB|PDV|VAT|EORI|tel|fax|IBAN|account)\b", RegexOptions.IgnoreCase))
                    foreach (Match h in Regex.Matches(l.Tekst, @"(?<![\d.,/])(\d{4})\s?(\d{2})(?:\s?(\d{2}))?(?:\s?(\d{2}))?(?![\d.,/])"))
                    {
                        var hs = h.Groups[1].Value + h.Groups[2].Value;
                        var pogl = int.Parse(hs.Substring(0, 2));
                        if (pogl >= 1 && pogl <= 97 && !c.Tarife.Contains(hs)) c.Tarife.Add(hs);
                    }
                // Rok za podnošenje robe (tranzit): datum u redu sa "rok" / "[15 11" / "time limit"
                if (c.Rok.Length == 0 && Regex.IsMatch(l.Tekst, @"\brok\b|\bpox\b|\[15 11|time limit|frist|рок", RegexOptions.IgnoreCase))
                {
                    var dm = Regex.Match(l.Tekst, @"\b([0-3]?\d)[/.]([01]?\d)[/.](20\d{2})\b");
                    if (dm.Success) c.Rok = int.Parse(dm.Groups[1].Value).ToString("00") + "." + int.Parse(dm.Groups[2].Value).ToString("00") + "." + dm.Groups[3].Value;
                }
            }
            // MRN bez dve cifre godine ("RS011096C97P8ZK5" umesto "26RS011096C97P8ZK5"): dopuna godinom iz datuma na strani (predlog).
            if (!c.Mrn.Any(JeMrn))
            {
                var tekst = s.Tekst.ToUpperInvariant();
                var godina = Regex.Matches(tekst, @"\b[0-3]?\d[/.][01]?\d[/.]20(\d{2})\b").Cast<Match>().Select(m => m.Groups[1].Value).GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
                             ?? (Regex.Match(c.Rok ?? "", @"20(\d{2})$") is var rg && rg.Success ? rg.Groups[1].Value : null);
                if (godina != null)
                    foreach (Match m in Regex.Matches(tekst, @"(?<![A-Z0-9])([A-Z]{2}\d{6}[A-Z0-9]{8})(?![A-Z0-9])"))
                    {
                        var kandidat = godina + m.Groups[1].Value;
                        if (JeMrn(kandidat) && Regex.Matches(kandidat, @"\d").Count >= 8 && !c.Mrn.Contains(kandidat)) c.Mrn.Add(kandidat);
                    }
            }
            // Broj izvozne deklaracije (JCI) "42072 / 23309 / 31.10.2026" → "42072/C1/23309/2026" i carinarnica ("CIS. MITROVICA" → "CI S. Mitrovica")
            if (c.BrojDeklaracije.Length == 0 && JciBroj.Match(s.Tekst) is var jb && jb.Success)
                c.BrojDeklaracije = jb.Groups[1].Value + "/C1/" + jb.Groups[2].Value + "/" + jb.Groups[3].Value;
            if (c.Carinarnica.Length == 0 && Regex.Match(s.Tekst, @"(?<![A-Za-z])C[IU]\s?([A-ZŠĐČĆŽ])\.?\s*([A-ZŠĐČĆŽ]{3,})") is var ca && ca.Success)
                c.Carinarnica = "CI " + ca.Groups[1].Value + ". " + ca.Groups[2].Value.Substring(0, 1) + ca.Groups[2].Value.Substring(1).ToLowerInvariant();
            // Polje "Sigurnost" [11 07]: jedna cifra ispod naziva polja.
            if (c.Sigurnost.Length == 0)
            {
                var naziv = s.Reci.FirstOrDefault(w => Regex.IsMatch(TekstUtil.Kljuc(w.T), @"^(sigurnost|security|curypnoc)"));
                if (naziv != null)
                {
                    var v = s.Reci.Where(w => Regex.IsMatch(w.T, @"^[0-4]$") && w.Cy > naziv.Cy + 0.004 && w.Cy < naziv.Cy + 0.03 && Math.Abs(w.Cx - naziv.Cx) < 0.06)
                        .OrderBy(w => w.Cy).FirstOrDefault();
                    if (v != null) c.Sigurnost = v.T;
                }
            }
        }

        /// <summary>Raspored reči po rubrikama za stranu (izbor obrasca, poravnanje) + ciljani OCR kućica na skenu.</summary>
        internal CimSablon.Rezultat Rasporedi(Prilog p, Strana s)
        {
            var r = sablon.Rasporedi(s.Reci, s.Raster);
            if (r.Poravnato) CiljaniOcr(p, s, r);
            return r;
        }

        /// <summary>
        /// Kućice iz šablona (mesec–dan–čas, NHM, r12, r62…) na skenu: isečak strane 300 dpi bez pregrada kućica, OCR samo
        /// cifara. Rezultat zamenjuje cifre opšteg OCR-a u toj kućici kada je pouzdan.
        /// </summary>
        private void CiljaniOcr(Prilog p, Strana s, CimSablon.Rezultat r)
        {
            if (ocr == null || r.Obrazac == null || r.Obrazac.Kucice.Count == 0 || s.Izvor != IzvorTeksta.Ocr || p?.Sadrzaj == null) return;
            System.Drawing.Bitmap slika = null;
            try
            {
                slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 300) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj));
                foreach (var kv in r.Obrazac.Kucice.OrderBy(k => k.Key, StringComparer.Ordinal))
                try
                {
                    var rub = Regex.Match(kv.Key, @"^\d+[ab]?").Value;
                    var o = r.NaStrani(kv.Value);
                    // v2.8.4: kućica preslikana van strane (ZIJIN C1-2298 str. 5, CIT: r62 donji red na y 1,02–1,03) – nema šta da se čita.
                    // Ranije je isečak van slike rušio GDI+ („Out of memory.“) i prekidao ciljani OCR svih ostalih kućica strane.
                    if (o == null || o[0] >= 1 || o[1] >= 1 || o[2] <= 0 || o[3] <= 0) continue;
                    bool Unutra(Rec w) => w.Cx >= o[0] && w.Cx <= o[2] && w.Cy >= o[1] && w.Cy <= o[3];
                    var postojece = r.Rubrika(rub).Where(Unutra).ToList();
                    var ocekivano = OcekivanoCifara(kv.Key);
                    var dbg = Environment.GetEnvironmentVariable("CIMPROVERA_ISECCI");
                    // dva isečka: osnovni i očišćen (proširen, bez isprekidanih pregrada i tačaka) – v2.8.1
                    var pokusaji = new List<(string t, double p, bool odg)>();
                    var cist = Slike.IsecakCifara(slika, o[0], o[1], o[2], o[3]);
                    // v2.8.4 (4.5): i zadebljan isečak (tanke, isprekidane cifre skena)
                    var debeo = Slike.IsecakCifara(slika, o[0], o[1], o[2], o[3], true);
                    foreach (var (vrsta, isecak, rec) in new[] { ("a", Slike.Isecak(slika, o[0], o[1], o[2], o[3], true), false), ("b", cist, false), ("c", cist, true), ("d", debeo, false), ("e", debeo, true) })
                    {
                        var (t0, p0) = ocr.Cifre(isecak, "0123456789-", rec);
                        if (!string.IsNullOrEmpty(dbg)) { Directory.CreateDirectory(dbg); File.WriteAllBytes(Path.Combine(dbg, Regex.Replace(p.Naziv, @"[^\w]", "_") + "_s" + s.Broj + "_k" + kv.Key + vrsta + "_" + Regex.Replace(t0 ?? "", @"\W", "") + "_" + (int)(p0 * 100) + ".png"), isecak); }
                        t0 = Regex.Replace(t0 ?? "", @"[^\d\- ]", "").Trim();
                        var n0 = t0.Count(char.IsDigit);
                        pokusaji.Add((t0, p0, ocekivano != null && ocekivano.Contains(n0) && Regex.IsMatch(t0, @"^[\d\-]+$")));
                    }
                    // i cifre opšteg OCR-a u kućici su jedan glas (kada imaju očekivan broj cifara)
                    var opste = string.Concat(postojece.OrderBy(w => w.X0).Select(w => Regex.Replace(w.T, @"[^\d]", "")));
                    if (ocekivano != null && ocekivano.Contains(opste.Length)) pokusaji.Add((opste, postojece.Average(w => w.Pouzdanost), true));
                    // glasanje: čitanje sa očekivanim brojem cifara koje daje više isečaka; nerešeno → veća pouzdanost
                    var odg = pokusaji.Where(x => x.odg).ToList();
                    var glasovi = odg.GroupBy(x => x.t).Select(g => (t: g.Key, n: g.Count(), p: g.Max(x => x.p))).OrderByDescending(g => g.n).ThenByDescending(g => g.p).ToList();
                    var izbor = glasovi.Count > 0 ? pokusaji.First(x => x.odg && x.t == glasovi[0].t) : pokusaji.OrderByDescending(x => x.p).First();
                    var t = izbor.t; var pouzd = glasovi.Count > 0 ? glasovi[0].p : izbor.p;
                    var cifara = t.Count(char.IsDigit);
                    var odgovara = izbor.odg;
                    if (cifara == 0 || (!odgovara && pouzd < 0.6)) continue;
                    // Tesseract sa ograničenim skupom znakova često ne daje pouzdanost – prihvata se kada broj cifara odgovara kućici.
                    // v2.8.1: kada isečci daju RAZLIČITE brojeve bez većine („498900“ / „158900“), čitanje nije pouzdano (≤ 0,5).
                    var vecina = glasovi.Count > 0 && glasovi[0].n >= 2;
                    // v2.8.4 (4.5): i jedno čitanje vrlo niske pouzdanosti kome drugi isečak protivreči („180100“ 0,12 uz „88“) – nije pouzdano
                    var sporno = (glasovi.Count > 1 && !vecina)
                                 || (!vecina && izbor.p < 0.3 && pokusaji.Any(x => x.t.Length > 0 && x.t != izbor.t && !izbor.t.Contains(x.t)));
                    // v3.0 (e, mail 782): jedini važeći glas je opšti OCR (nijedan isečak ga ne potvrđuje – rukom pisano „001784“ pročitano
                    // kao „007777“) – zadržava svoju pouzdanost, ne podiže se na 0,85 (inače postaje „siguran“ ključ r62)
                    var samoOpste = odg.Count > 0 && odg.All(x => x.t == opste) && !pokusaji.Take(5).Any(x => x.odg);
                    if (odgovara && !sporno && !samoOpste) pouzd = Math.Max(pouzd, vecina ? 0.9 : 0.85);
                    else if (sporno) pouzd = Math.Min(pouzd, 0.5);
                    // v2.8.4: sporno čitanje kućice ne zamenjuje cifre koje je opšti OCR već pročitao („2520“ u GIPS r24)
                    if (sporno && r.Rubrika(rub).Any(w => Regex.IsMatch(w.T ?? "", @"\d{2,}"))) continue;
                    if (cifara < postojece.Sum(w => w.T.Count(char.IsDigit)) && postojece.Average(w => w.Pouzdanost) >= pouzd) continue;
                    var nova = new Rec { T = t, X0 = o[0], Y0 = o[1], X1 = o[2], Y1 = o[3], Pouzdanost = Math.Round(pouzd, 2) };
                    if (!r.Rubrike.TryGetValue(rub, out var lista)) r.Rubrike[rub] = lista = new List<Rec>();
                    // u kućici ostaju samo slova (npr. naziv mesta); cifre daje ciljani OCR
                    lista.RemoveAll(w => Unutra(w) && !Regex.IsMatch(w.T, @"\p{L}{3,}"));
                    lista.Add(nova);
                    r.Dodela.Add((nova, rub, false));
                }
                // greška jedne kućice ne prekida ostale
                catch (Exception ex) { log?.Upozorenje("Ciljani OCR kućice " + kv.Key + " nije uspeo (str. " + s.Broj + "): " + ex.GetType().Name + ": " + ex.Message); }
            }
            catch (Exception ex) { log?.Upozorenje("Ciljani OCR kućica nije uspeo (str. " + s.Broj + "): " + ex.GetType().Name + ": " + ex.Message); }
            finally { slika?.Dispose(); }
        }

        private static readonly string[] VazneRubrike = { "1", "4", "7", "9", "10", "13", "14", "16", "18", "20", "21", "24", "25", "29", "57", "58a", "62" };

        /// <summary>
        /// Nečitljive rubrike skena (šum, pečat preko teksta): pravougaonik rubrike se iscrta sa 400 dpi i ponovo čita OCR-om
        /// (srp_latn + hrv + deu), reči te rubrike se zamene novim i strana se ponovo rasporedi. null ako nema poboljšanja.
        /// </summary>
        private CimSablon.Rezultat PonoviNecitljive(Prilog p, Strana s, CimSablon.Rezultat r, CimList c)
        {
            if (ocr == null || s.Izvor != IzvorTeksta.Ocr || p?.Sadrzaj == null || r.Obrazac == null) return null;
            var ids = VazneRubrike.Where(id => r.Obrazac.Rubrike.ContainsKey(id) && RubrikaLista(c, id) is Rubrika rub && rub.Status == StatusRubrike.Necitljivo).ToList();
            if (ids.Count == 0) return null;
            try
            {
                var reci = s.Reci.ToList();
                using (var slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 400) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj)))
                    foreach (var id in ids)
                    {
                        var o = r.NaStrani(r.Obrazac.Rubrike[id]);
                        int a = Math.Max(0, (int)(o[0] * slika.Width)), b = Math.Max(0, (int)(o[1] * slika.Height));
                        int w = Math.Min(slika.Width - a, (int)((o[2] - o[0]) * slika.Width)), h = Math.Min(slika.Height - b, (int)((o[3] - o[1]) * slika.Height));
                        if (w < 20 || h < 10) continue;
                        byte[] png;
                        using (var isecak = slika.Clone(new System.Drawing.Rectangle(a, b, w, h), System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        using (var ms = new MemoryStream()) { isecak.SetResolution(400, 400); isecak.Save(ms, System.Drawing.Imaging.ImageFormat.Png); png = ms.ToArray(); }
                        var nove = ocr.PrepoznajJezicima(png, "srp_latn+hrv+deu", 400)
                            .Select(x => new Rec { T = x.T, Pouzdanost = x.Pouzdanost, X0 = a / (double)slika.Width + x.X0 * w / slika.Width, X1 = a / (double)slika.Width + x.X1 * w / slika.Width,
                                                   Y0 = b / (double)slika.Height + x.Y0 * h / slika.Height, Y1 = b / (double)slika.Height + x.Y1 * h / slika.Height }).ToList();
                        if (nove.Count == 0) continue;
                        reci.RemoveAll(x => x.Cx >= o[0] && x.Cx <= o[2] && x.Cy >= o[1] && x.Cy <= o[3]);
                        reci.AddRange(nove);
                    }
                var r2 = sablon.Rasporedi(reci, s.Raster);
                if (!r2.Poravnato) return null;
                CiljaniOcr(p, s, r2);
                var c2 = CimParser.Napravi(r2, s, Pragovi);
                var bolje = ids.Count(id => RubrikaLista(c2, id)?.Status != StatusRubrike.Necitljivo);
                if (bolje > 0) reciPonovljene = reci;
                return bolje > 0 ? r2 : null;
            }
            catch (Exception ex) { log?.Upozorenje("Ponovni OCR rubrika nije uspeo (str. " + s.Broj + "): " + ex.Message); return null; }
        }

        /// <summary>
        /// v2.8.4 (3): r7 skena u kojoj analiza ne nalazi platioca za deonicu kroz Srbiju: pravougaonik r7 se iscrta sa 400 dpi i ponovo čita
        /// OCR-om (deu + srp_latn + eng). Novo čitanje se prihvata samo ako u njemu postoji platilac za Srbiju (ili nejasan zapis
        /// „SRB: KP 3212“) – inače ostaje prvo čitanje. null ako nema poboljšanja.
        /// </summary>
        /// <summary>Reči strane posle <see cref="PonoviNecitljive"/> (null ako nije bilo poboljšanja) – osnova za <see cref="PonoviR7"/>.</summary>
        private List<Rec> reciPonovljene;

        private CimSablon.Rezultat PonoviR7(Prilog p, Strana s, CimSablon.Rezultat r, CimList c)
        {
            if (ocr == null || s.Izvor != IzvorTeksta.Ocr || p?.Sadrzaj == null || r.Obrazac == null || !r.Obrazac.Rubrike.ContainsKey("7") || kp == null) return null;
            // naziv platioca je ceo kada se završava pravnim oblikom ili rečju od bar 3 slova („Pansped i“ je odsečen OCR-om)
            bool Ceo(string n) => Regex.IsMatch(n ?? "", @"(?i)(d\.?\s?o\.?\s?o\.?|a\.?\s?d\.?|gmbh|kft\.?|zrt\.?|s\.?r\.?l\.?|s\.?r\.?o\.?|a\.?\s?s\.?|spa|ltd\.?)$|\p{L}{3,}\.?$");
            bool Ima(string t) { var a = Provere.AnalizirajR7(t, kp); return a.ZaSrbiju.Any() || a.Nejasni.Count > 0; }
            bool ImaCeo(string t) { var a = Provere.AnalizirajR7(t, kp); return a.Nejasni.Count > 0 || a.ZaSrbiju.Any(x => Ceo(x.Naziv)); }
            var t0 = !string.IsNullOrWhiteSpace(c.R7.Tekst) ? c.R7.Tekst : string.Join("\n", c.R7.Stavke.Select(x => (x.Sifra + " " + x.Tekst).Trim()));
            if (ImaCeo(t0)) return null;
            var imaPrvo = Ima(t0);
            try
            {
                var o = r.NaStrani(r.Obrazac.Rubrike["7"]);
                byte[] png; int a, b, w, h, sw, sh;
                using (var slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 400) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj)))
                {
                    sw = slika.Width; sh = slika.Height;
                    a = Math.Max(0, (int)(o[0] * sw)); b = Math.Max(0, (int)(o[1] * sh));
                    w = Math.Min(sw - a, (int)((o[2] - o[0]) * sw)); h = Math.Min(sh - b, (int)((o[3] - o[1]) * sh));
                    if (w < 20 || h < 10) return null;
                    using (var isecak = slika.Clone(new System.Drawing.Rectangle(a, b, w, h), System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    using (var ms = new MemoryStream()) { isecak.SetResolution(400, 400); isecak.Save(ms, System.Drawing.Imaging.ImageFormat.Png); png = ms.ToArray(); }
                }
                if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1") try { File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "cimdbg_r7_" + s.Broj + ".png"), png); } catch { }
                // dva načina čitanja isečka: retke reči (SparseText), pa blok redova (SingleBlock) – prihvata se prvo dovoljno
                foreach (var (nacin, citaj) in new (string, Func<List<Rec>>)[]
                {
                    ("isečak 400 dpi", () => ocr.PrepoznajJezicima(png, "deu+srp_latn+eng", 400)),
                    ("isečak 400 dpi, blok redova", () => ocr.PrepoznajBlok(png, "deu+srp_latn+eng", 400).Where(x => x.Pouzdanost >= 0.1).ToList()),
                })
                {
                    var nove = citaj().Select(x => new Rec { T = x.T, Pouzdanost = x.Pouzdanost, X0 = a / (double)sw + x.X0 * w / sw, X1 = a / (double)sw + x.X1 * w / sw,
                                                             Y0 = b / (double)sh + x.Y0 * h / sh, Y1 = b / (double)sh + x.Y1 * h / sh }).ToList();
                    if (nove.Count == 0) continue;
                    // v2.8.4: od reči posle ponovnog čitanja nečitljivih rubrika (inače se gubi poboljšanje r24 GIPS-a)
                    var reci = (reciPonovljene ?? s.Reci).ToList();
                    reci.RemoveAll(x => x.Cx >= o[0] && x.Cx <= o[2] && x.Cy >= o[1] && x.Cy <= o[3]);
                    reci.AddRange(nove);
                    var r2 = sablon.Rasporedi(reci, s.Raster);
                    if (!r2.Poravnato) continue;
                    CiljaniOcr(p, s, r2);
                    var c2 = CimParser.Napravi(r2, s, Pragovi);
                    var t2 = !string.IsNullOrWhiteSpace(c2.R7.Tekst) ? c2.R7.Tekst : string.Join("\n", c2.R7.Stavke.Select(x => (x.Sifra + " " + x.Tekst).Trim()));
                    if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1") log?.Info("  [dbg] r7 ponovo (" + nacin + "): „" + t2.Replace("\n", " / ") + "“");
                    // novo čitanje se prihvata samo sa CELIM nazivom platioca (ili nejasnim „SRB: KP“) – ne „jujoyde doo Beograd i“
                    if (!ImaCeo(t2)) continue;
                    s.Napomena += (s.Napomena.Length > 0 ? "; " : "") + "r7 ponovo pročitana (" + nacin + ")";
                    reciPonovljene = reci;   // v2.9.2: osnova za ponovno čitanje r57/r58a (ne gubi se nova r7)
                    return r2;
                }
                return null;
            }
            catch (Exception ex) { log?.Upozorenje("Ponovni OCR r7 nije uspeo (str. " + s.Broj + "): " + ex.Message); return null; }
        }

        /// <summary>
        /// v2.9.2 (2): rubrika skena (r57, r58a) u kojoj prvo čitanje ne daje traženi podatak (KP) – pravougaonik rubrike (proširen naniže
        /// za red koji prelazi liniju) se iscrta sa 400 dpi i ponovo čita (deu + srp_latn + eng; retke reči, pa blok redova). Novo čitanje
        /// se prihvata samo kada je bolje po oceni (<paramref name="ocena"/>). null ako nema poboljšanja.
        /// </summary>
        private CimSablon.Rezultat PonoviRubriku(Prilog p, Strana s, CimSablon.Rezultat r, CimList c, string id, Func<CimList, bool> dovoljno, Func<CimList, int> ocena)
        {
            if (ocr == null || s.Izvor != IzvorTeksta.Ocr || p?.Sadrzaj == null || r.Obrazac == null || !r.Obrazac.Rubrike.ContainsKey(id)) return null;
            if (ocena(c) >= 2 || (id == "58a" && dovoljno(c))) return null;
            var pre = ocena(c);
            try
            {
                var o = r.NaStrani(r.Obrazac.Rubrike[id]);
                if (o == null) return null;
                var o3 = o[3];   // bez proširenja: red koji prelazi liniju pokriva CimSablon.PrelivanjeR57 (ispod r57 je na nekim obrascima r58a)
                byte[] png; int a, b, w, h, sw, sh;
                using (var slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 400) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj)))
                {
                    sw = slika.Width; sh = slika.Height;
                    a = Math.Max(0, (int)(o[0] * sw)); b = Math.Max(0, (int)(o[1] * sh));
                    w = Math.Min(sw - a, (int)((o[2] - o[0]) * sw)); h = Math.Min(sh - b, (int)((o3 - o[1]) * sh));
                    if (w < 20 || h < 10) return null;
                    using (var isecak = slika.Clone(new System.Drawing.Rectangle(a, b, w, h), System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    using (var ms = new MemoryStream()) { isecak.SetResolution(400, 400); isecak.Save(ms, System.Drawing.Imaging.ImageFormat.Png); png = ms.ToArray(); }
                }
                foreach (var (nacin, citaj) in new (string, Func<List<Rec>>)[]
                {
                    ("isečak 400 dpi", () => ocr.PrepoznajJezicima(png, "deu+srp_latn+eng", 400)),
                    ("isečak 400 dpi, blok redova", () => ocr.PrepoznajBlok(png, "deu+srp_latn+eng", 400).Where(x => x.Pouzdanost >= 0.1).ToList()),
                })
                {
                    var nove = citaj().Select(x => new Rec { T = x.T, Pouzdanost = x.Pouzdanost, X0 = a / (double)sw + x.X0 * w / sw, X1 = a / (double)sw + x.X1 * w / sw,
                                                             Y0 = b / (double)sh + x.Y0 * h / sh, Y1 = b / (double)sh + x.Y1 * h / sh }).ToList();
                    if (nove.Count == 0) continue;
                    var reci = (reciPonovljene ?? s.Reci).ToList();
                    reci.RemoveAll(x => x.Cx >= o[0] && x.Cx <= o[2] && x.Cy >= o[1] && x.Cy <= o3);
                    reci.AddRange(nove);
                    var r2 = sablon.Rasporedi(reci, s.Raster);
                    if (!r2.Poravnato) continue;
                    CiljaniOcr(p, s, r2);
                    var c2 = CimParser.Napravi(r2, s, Pragovi);
                    if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1") log?.Info("  [dbg] r" + id + " ponovo (" + nacin + "): „" + (Motor.RubrikaLista(c2, id)?.Tekst ?? "").Replace("\n", " / ") + "“");
                    if (!dovoljno(c2) || ocena(c2) <= pre || Motor.RubrikaLista(c2, id)?.Status == StatusRubrike.Necitljivo) continue;
                    s.Napomena += (s.Napomena.Length > 0 ? "; " : "") + "r" + id + " ponovo pročitana (" + nacin + ")";
                    reciPonovljene = reci;
                    return r2;
                }
                // r57: kolona prevozilaca (leva polovina) samo velikim slovima i ciframa – gusti redovi „3212 KP / 3404 H-KÁRPÁT“ koje opšti OCR
                // spoji u jedan („32 04 jKARPAT“); pročitani red sa KP se dodaje rubrici kao poseban red
                if (id == "57" && kp != null)
                {
                    byte[] pngL; int wl;
                    using (var slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 400) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj)))
                    {
                        wl = Math.Max(20, w * 45 / 100);
                        using (var isecak = slika.Clone(new System.Drawing.Rectangle(a, b, wl, h), System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        using (var ms = new MemoryStream()) { isecak.SetResolution(400, 400); isecak.Save(ms, System.Drawing.Imaging.ImageFormat.Png); pngL = ms.ToArray(); }
                    }
                    var tekst = ocr.Alfanum(pngL) ?? "";
                    if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1") log?.Info("  [dbg] r57 kolona prevozilaca: „" + tekst.Replace("\n", " / ") + "“");
                    var redovi = tekst.Split('\n').Select(x => Regex.Replace(x, @"\s+", " ").Trim()).Where(x => Regex.IsMatch(kp.OcrIspravka(x), @"(?<!\d)" + kp.Uic + @"(?!\d)") && Regex.IsMatch(x, @"\p{L}{2,}")).ToList();
                    if (redovi.Count == 0) return null;
                    var r2 = sablon.Rasporedi((reciPonovljene ?? s.Reci).ToList(), s.Raster);
                    if (!r2.Poravnato) return null;
                    CiljaniOcr(p, s, r2);
                    if (!r2.Rubrike.TryGetValue("57", out var l57)) r2.Rubrike["57"] = l57 = new List<Rec>();
                    var y = o[1] + 0.004;
                    foreach (var red in redovi)
                    {
                        var nova = new Rec { T = red, X0 = o[0] + 0.004, X1 = o[0] + 0.004 + (o[2] - o[0]) * 0.3, Y0 = y - 0.003, Y1 = y + 0.003, Pouzdanost = 0.8 };
                        l57.Add(nova); r2.Dodela.Add((nova, "57", false));
                        y += 0.007;
                    }
                    var c2 = CimParser.Napravi(r2, s, Pragovi);
                    if (!dovoljno(c2) || ocena(c2) <= pre) return null;
                    s.Napomena += (s.Napomena.Length > 0 ? "; " : "") + "r57 ponovo pročitana (kolona prevozilaca, velika slova i cifre)";
                    return r2;
                }
                return null;
            }
            catch (Exception ex) { log?.Upozorenje("Ponovni OCR r" + id + " nije uspeo (str. " + s.Broj + "): " + ex.Message); return null; }
        }

        /// <summary>Očekivan broj cifara u kućicama: 16 mesec–dan(–čas), 12 šifra stanice, 24 NHM, 62.1 zemlja, 62.2 stanica, 62.3 preduzeće, 62.4 broj otpravljanja.</summary>
        private static int[] OcekivanoCifara(string kucica)
        {
            switch (kucica)
            {
                case "16": return new[] { 4, 6 };
                case "12": case "17": return new[] { 7, 8 };
                case "24": return new[] { 4, 6, 8 };
                case "62.1": return new[] { 2 };
                case "62.2": return new[] { 5, 6 };
                case "62.3": return new[] { 4 };
                case "62.4": return new[] { 5, 6 };
                default: return null;
            }
        }

        internal CimParser.Pragovi Pragovi => new CimParser.Pragovi { Mastilo = cfg?.PragMastila ?? 0.004, Oznaka = cfg?.PragOznake ?? 0.15, PraznoPolje = cfg?.PragPraznogPolja ?? 0.05 };

        /// <summary>Strana se iscrtava (linije okvira, kućice) kada liči na tovarni list ili njegovu poleđinu.</summary>
        private bool TrebaSlika(List<Rec> reci)
        {
            if (sablon.BrojSidara(reci) >= 3) return true;
            var t = " " + TekstUtil.Kljuc(string.Join(" ", reci.Select(r => r.T))) + " ";
            return CimKljucevi.Any(k => t.Contains(TekstUtil.Kljuc(k))) || sablon.Obrasci.Poledjina(t) != null;
        }

        private static void DodajPoledjinu(RezultatPriloga rez, Strana s, List<string> napomene, string obrazac)
        {
            var list = rez.Dokumenti.LastOrDefault(d => TipDokumenta.JeTovarniList(d.Tip));
            if (list != null) { list.Strane.Add(s.Broj); list.Opis += "; str. " + s.Broj + " poleđina"; }
            else rez.Dokumenti.Add(new Dokument { Tip = TipDokumenta.Poledjina, Strane = { s.Broj }, Izvor = s.Izvor == IzvorTeksta.Ocr ? "ocr" : "tekst", Opis = "poleđina tovarnog lista" });
            napomene.Add("str. " + s.Broj + ": poleđina lista" + (string.IsNullOrEmpty(obrazac) ? "" : " (" + obrazac + ")") + " – računski odseci, ne proverava se kao prednja strana");
        }

        /// <summary>Koji je to dokument (po naslovu) i zašto obrazac nije prepoznat – za izveštaj („potreban je novi šablon“).</summary>
        private static string OpisNeprepoznatog(Strana s, CimSablon.Rezultat r)
        {
            var t = " " + TekstUtil.Kljuc(s.Tekst) + " ";
            string vrsta =
                t.Contains("fuvarlevel") ? "mađarski tovarni list (fuvarlevél)" :
                t.Contains("cd cargo") || t.Contains("ceske drahy") ? "elektronski CIM ČD Cargo" :
                t.Contains("tcdd") || t.Contains("cim tasima") ? "tovarni list TCDD" :
                // v2.8.3: „Wagenbrief CUV / kolski list“ je štampan na SVAKOM obrascu CIM/CUV – nije dokaz vrste; neprepoznat list je klasifikovan kao CIM
                "tovarni list CIM";
            var naj = r.Poravnanje;
            return vrsta + ": obrazac nije prepoznat – potreban je novi šablon, ručna provera"
                   + (naj == null ? "" : " (najbliži šablon " + naj.Obrazac + ", " + naj.Ukupno + " sidara" + (naj.Razlog.Length > 0 ? "; " + naj.Razlog : "") + ")");
        }

        /// <summary>Režim Test: JSON sa pročitanim rubrikama i slika okvira za svaki list u RezultatiFolder\yyyy-MM-dd.</summary>
        private void Zapisi(string fajl, Strana s, CimSablon.Rezultat r, CimList c)
        {
            if (cfg == null || cfg.Rezim != Rezim.Test || string.IsNullOrEmpty(cfg.RezultatiFolder)) return;
            try { Snimi(Path.Combine(cfg.RezultatiFolder, DateTime.Today.ToString("yyyy-MM-dd")), fajl, s, r, c); }
            catch (Exception ex) { log?.Upozorenje("Rezultat čitanja nije sačuvan (" + cfg.RezultatiFolder + "): " + ex.Message); }
        }

        /// <summary>Upisuje &lt;fajl&gt;_strN.json (obrazac, poravnanje, sidra, rubrike: vrednost, stanje, pouzdanost, strana, okvir, reči) i &lt;fajl&gt;_strN.png.</summary>
        internal static void Snimi(string folder, string fajl, Strana s, CimSablon.Rezultat r, CimList c)
        {
            Directory.CreateDirectory(folder);
            var ime = Regex.Replace(Path.GetFileNameWithoutExtension(fajl ?? "prilog"), @"[^\w\-\. ]", "_") + "_str" + s.Broj;
            var rubrike = new SortedDictionary<string, object>(StringComparer.Ordinal);
            if (r.Obrazac != null)
                foreach (var kv in r.Obrazac.Rubrike.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var rub = RubrikaLista(c, kv.Key);
                    var reci = r.Rubrika(kv.Key);
                    rubrike[kv.Key] = new
                    {
                        vrednost = rub?.Tekst ?? string.Join(" ", reci.Select(w => w.T)),
                        stanje = rub?.Status ?? (reci.Count > 0 ? StatusRubrike.Popunjeno : StatusRubrike.Prazno),
                        pouzdanost = rub?.Pouzdanost ?? (reci.Count > 0 ? Math.Round(reci.Average(w => w.Pouzdanost), 2) : 1),
                        strana = s.Broj,
                        okvir = r.NaStrani(kv.Value)?.Select(x => Math.Round(x, 4)).ToArray(),
                        mastilo = r.Mastilo(kv.Key) is double m ? Math.Round(m, 4) : (double?)null,
                        reci = reci.Select(w => new { t = w.T, x0 = Math.Round(w.X0, 4), y0 = Math.Round(w.Y0, 4), x1 = Math.Round(w.X1, 4), y1 = Math.Round(w.Y1, 4), p = Math.Round(w.Pouzdanost, 2) }),
                    };
                }
            var json = new
            {
                fajl, strana = s.Broj, izvor = s.Izvor.ToString(), verzija_citaca = VerzijaCitaca,
                obrazac = r.Obrazac?.Id, neprepoznat = c?.NeprepoznatObrazac,
                poravnanje = r.Poravnanje == null ? null : new
                {
                    opis = r.Poravnanje.Opis, x = r.Poravnanje.X, y = r.Poravnanje.Y, pouzdano = r.Poravnanje.Pouzdano,
                    sidra = r.Poravnanje.Parovi.Select(q => new { q.Vrsta, q.Oznaka, px = Math.Round(q.Px, 4), py = Math.Round(q.Py, 4), tx = double.IsNaN(q.Tx) ? (double?)null : Math.Round(q.Tx, 4), ty = double.IsNaN(q.Ty) ? (double?)null : Math.Round(q.Ty, 4), odstupanje = Math.Round(q.Odstupanje, 4) }),
                },
                probani_obrasci = r.Probe.Select(p => p.Opis),
                polja = r.Obrazac?.Polja.Keys.OrderBy(k => k, StringComparer.Ordinal).ToDictionary(k => k, k => new { stanje = r.Polje(k), gustina = r.Gustina(k) is double g ? Math.Round(g, 3) : (double?)null }),
                rubrike,
                van_rubrika = r.Dodela.Where(d => d.rubrika == null && !d.naziv).Select(d => new { t = d.rec.T, x = Math.Round(d.rec.Cx, 4), y = Math.Round(d.rec.Cy, 4) }),
            };
            File.WriteAllText(Path.Combine(folder, ime + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
            if (s.Raster != null)
                File.WriteAllBytes(Path.Combine(folder, ime + ".png"), Slike.Okviri(s.Raster, r.Obrazac, r.Poravnanje ?? new Poravnanje(), r.Dodela, fajl + " – str. " + s.Broj + (r.Obrazac != null ? " – obrazac " + r.Obrazac.Id : "")));
        }

        /// <summary>Rubrika lista po broju ("1" → R1, "58a" → R58a, "30cim" → R30).</summary>
        internal static Rubrika RubrikaLista(CimList c, string id)
        {
            if (c == null) return null;
            var m = Regex.Match(id ?? "", @"^(\d+[ab]?)");
            if (!m.Success) return null;
            var p = typeof(CimList).GetProperty("R" + m.Groups[1].Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(c) as Rubrika;
        }

        /// <summary>Komanda "sablon-nacrt": nacrt šablona novog obrasca iz jedne strane uzorka (JSON + slika za proveru).</summary>
        public (Newtonsoft.Json.Linq.JObject json, byte[] png, List<string> napomene) NacrtSablona(Prilog p, int brojStrane, string id, string naziv, bool uvekOcr, Newtonsoft.Json.Linq.JObject mapa)
        {
            var strane = Strane(p, uvekOcr);
            var s = strane.FirstOrDefault(x => x.Broj == brojStrane) ?? throw new ArgumentException("nema strane " + brojStrane);
            if (s.Raster == null) s.PostaviRaster(Raster.IzSlike(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, brojStrane, 150) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj)));
            var napomene = new List<string>();
            var json = SablonNacrt.Napravi(s, sablon, id, naziv, p.Naziv + ", str. " + brojStrane, mapa, napomene);
            byte[] png;
            if (mapa == null) png = Slike.Nacrt(s.Raster, s.Raster.Celije(), s.Raster.Kvadratici(), s.Raster.Krugovi());
            else
            {
                var obrazac = ObrazacSablon.IzJson(json);
                var potpisi = sablon.PotpisiNaziva(s.Reci);
                png = Slike.Okviri(s.Raster, obrazac, new Poravnanje { Obrazac = obrazac.Id }, s.Reci.Select(w => (w, obrazac.RubrikaZa(w.Cx, w.Cy), sablon.JeNaziv(w, potpisi))), "ŠABLON " + obrazac.Id + " – " + p.Naziv + " str. " + brojStrane);
            }
            return (json, png, napomene);
        }

        /// <summary>Komanda "slike": za svaku CIM stranu priloga upisuje JSON i sliku okvira u folder (bez obzira na režim).</summary>
        public List<string> SnimiSlike(Prilog p, string folder, bool uvekOcr = false)
        {
            var poruke = new List<string>();
            p = KaoPdf(p);
            var strane = Strane(p, uvekOcr);
            var rez = Klasifikuj(p, strane);
            foreach (var d in rez.Dokumenti.Where(x => TipDokumenta.JeTovarniList(x.Tip)))
            {
                var s = strane.First(x => x.Broj == d.Strane[0]);
                var r = Rasporedi(p, s);
                Snimi(folder, p.Naziv, s, r, d.Cim.FirstOrDefault());
                poruke.Add(p.Naziv + " str. " + s.Broj + ": " + (r.Poravnanje?.Opis ?? "bez poravnanja") + (d.Cim.FirstOrDefault()?.NeprepoznatObrazac is string n && n.Length > 0 ? " → " + n : ""));
            }
            return poruke;
        }

        /// <summary>Tekst ćelije tabelarnog obrasca je natpis (rečnik natpisa šablona).</summary>
        internal bool JeNatpisCelije(string tekst) => sablon.JeNatpisCelije(tekst);

        /// <summary>
        /// Tovarni list CIM/CUV iz tabele (Excel list ili Word tabele): prvi list koji je tovarni list. Naslov obrasca bez
        /// dovoljno brojeva rubrika → list na ručnu proveru (obrazac nije pouzdano pročitan). Null kada tabela nije tovarni list.
        /// </summary>
        internal Dokument TovarniIzTabele(Prilog p, List<List<Prilozi.Celija>> listovi, string izvor)
        {
            string razlogNaslova = null;
            foreach (var cel in listovi.Where(x => x.Count > 0))
            {
                if (Prilozi.TabelarniCim.JeTovarniList(cel, out var razlog))
                {
                    var cim = Prilozi.TabelarniCim.Procitaj(cel, sablon.JeNatpisCelije, Pragovi, out var po);
                    cim.RedniBroj = 1;
                    var naziv = izvor == "word" ? "Word obrazac" : "Excel obrazac";
                    return new Dokument
                    {
                        Tip = cim.Obrazac == "CUV" ? TipDokumenta.Cuv : TipDokumenta.Cim, Strane = { 1 }, Cim = { cim }, Izvor = izvor, Fajl = p.Naziv,
                        Opis = "tovarni list " + cim.Obrazac + " (" + naziv + ", čitano iz ćelija; " + razlog + "; rubrika sa vrednošću " + po.Count + ")",
                    };
                }
                if (razlog.StartsWith("naslov")) razlogNaslova = razlog;
            }
            if (razlogNaslova == null) return null;
            var nep = new CimList { RedniBroj = 1, NeprepoznatObrazac = (izvor == "word" ? "Word" : "Excel") + " obrazac tovarnog lista nije pouzdano pročitan (" + razlogNaslova + ")", Sablon = "TABELA" };
            return new Dokument { Tip = TipDokumenta.Cim, Strane = { 1 }, Cim = { nep }, Izvor = izvor, Fajl = p.Naziv, Opis = "tovarni list – " + nep.NeprepoznatObrazac };
        }

        /// <summary>Dijagnostika (komanda "rubrike"): za svaku stranu vrsta, izvor teksta, poravnanje i tekst svake rubrike.</summary>
        public string Dijagnostika(Prilog p, bool uvekOcr = false)
        {
            p = KaoPdf(p);
            return Dijagnostika(Strane(p, uvekOcr), p.Naziv, p);
        }

        /// <summary>Word prilog → PDF prilog (Syncfusion) za dijagnostičke komande; ostali prilozi ostaju isti.</summary>
        internal static Prilog KaoPdf(Prilog p)
        {
            if (p.Vrsta != VrstaPriloga.Word) return p;
            var pdf = Prilozi.WordUPdf.Pretvori(p, out var razlog, out _);
            if (pdf == null) throw new TrajnaGreskaCitanja(razlog);
            return new Prilog { Naziv = p.Naziv, Sadrzaj = pdf, Vrsta = VrstaPriloga.Pdf, MediaType = "application/pdf", IzWorda = true };
        }

        /// <summary>Dijagnostika nad već pročitanim stranama (npr. iz keša sa servera, komanda "iz-kesa").</summary>
        public string Dijagnostika(List<Strana> strane, string naziv, Prilog izvor = null)
        {
            var sb = new StringBuilder();
            var p = izvor ?? new Prilog { Naziv = naziv };
            foreach (var s in strane)
            {
                var tip = TipStrane(s, p.Naziv, out var razlogTipa);
                sb.AppendLine("===== strana " + s.Broj + ": " + tip + (razlogTipa.Length > 0 ? " (" + razlogTipa + ")" : "") + ", izvor " + s.Izvor + ", reči " + s.Reci.Count + ", pouzdanost " + s.SrednjaPouzdanost.ToString("0.00")
                              + ", sidara " + sablon.BrojSidara(s.Reci) + (s.Napomena.Length > 0 ? ", " + s.Napomena : ""));
                if (tip != TipDokumenta.Cim)
                {
                    foreach (var l in Raspored.Linije(s.Reci).Take(40)) sb.AppendLine("   | " + l.Tekst);
                    continue;
                }
                var r = Rasporedi(p, s);
                foreach (var pr in r.Probe) sb.AppendLine("   obrazac " + pr.Opis);
                if (r.Poravnanje != null)
                    sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "   izabran: {0}, x = {1:0.0000}·x {2:+0.0000;-0.0000}·y {3:+0.0000;-0.0000}, y = {4:+0.0000;-0.0000}·x {5:0.0000}·y {6:+0.0000;-0.0000}",
                        r.Poravnato ? r.Obrazac.Id : "NIJEDAN (ručna provera)", r.Poravnanje.X[0], r.Poravnanje.X[1], r.Poravnanje.X[2], r.Poravnanje.Y[0], r.Poravnanje.Y[1], r.Poravnanje.Y[2]));
                if (r.SkriveniPodaci.Count > 0) sb.AppendLine("   skriveni tekst Word okvira (ne vidi se na strani, ne čita se): " + string.Join(" ", r.SkriveniPodaci.Select(x => x.ToString())));
                if (!r.Poravnato) continue;
                foreach (var kv in r.Rubrike.OrderBy(k => Regex.Match(k.Key, @"\d+").Success ? int.Parse(Regex.Match(k.Key, @"\d+").Value) : 0).ThenBy(k => k.Key, StringComparer.Ordinal))
                    sb.AppendLine("   r" + kv.Key.PadRight(6) + " " + string.Join(" / ", Raspored.Linije(kv.Value).Select(l => l.Tekst)));
                foreach (var pid in r.Obrazac.Polja.Keys.OrderBy(k => k, StringComparer.Ordinal))
                    sb.AppendLine("   □" + pid.PadRight(8) + " " + r.Polje(pid, Pragovi.Oznaka, Pragovi.PraznoPolje) + " (gustina " + (r.Gustina(pid) ?? 0).ToString("0.000") + ")");
                var c = CimParser.Napravi(r, s, Pragovi);
                sb.AppendLine("   → r16 datum=" + c.R16.Datum + " čas=" + c.R16.Cas + " mesto=" + c.R16.Mesto + " | r29 " + c.R29.Mesto + " " + c.R29.Datum
                              + " | r30 CIM=" + c.R30.Cim + " CUV=" + c.R30.Cuv + " | r62 " + c.R62.Zemlja + "/" + c.R62.Stanica + "/" + c.R62.Preduzece + "/" + c.R62.Broj
                              + " | r24 " + string.Join(",", c.R24.Stavke.Select(x => x.Sifra)) + " | r57 " + string.Join("; ", c.R57.Prevoznici.Select(x => x.Sifra + " [" + x.Deonica + "] " + x.StatusPrevoznika)));
            }
            return sb.ToString();
        }

        public void Dispose() => ocr?.Dispose();
    }
}
