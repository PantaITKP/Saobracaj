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
        public const string VerzijaCitaca = "L17";
        /// <summary>Menja se kada se promeni čitanje teksta strana (PDF tekst, OCR, slika strane) – tada se OCR ponavlja.</summary>
        public const string VerzijaTeksta = "L15";

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
            kesFolder = Path.Combine(cfg.StanjeFolder, "kes");
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

            try { Directory.CreateDirectory(kesFolder); File.WriteAllText(kes, Json.U(rez), Encoding.UTF8); }
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
            List<Strana> strane = null;
            var kesStrana = Path.Combine(kesFolder, p.Hes + "_strane_" + Prilog.Sha256(Encoding.UTF8.GetBytes(VerzijaTeksta + cfg.OcrJezici + "|" + cfg.OcrDodatniJezici + cfg.OcrMinPouzdanostReci)).Substring(0, 8) + ".json");
            if (!uvekOcr && File.Exists(kesStrana))
                try { strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Strana>>(File.ReadAllText(kesStrana, Encoding.UTF8)); } catch { strane = null; }
            if (strane != null) return strane;
            try
            {
                if (p.Vrsta == VrstaPriloga.Pdf) strane = PdfStrane.Procitaj(p.Sadrzaj, ocr, 60, uvekOcr, sablon.BrojSidara, TrebaSlika, p.IzWorda);
                else
                {
                    if (ocr == null) throw new TrajnaGreskaCitanja("fotografija/sken, a OCR nije instaliran (tessdata)");
                    var reci = ocr.Prepoznaj(p.Sadrzaj, out var nap);
                    strane = new List<Strana> { new Strana { Broj = 1, Reci = reci, Izvor = reci.Count > 0 ? IzvorTeksta.Ocr : IzvorTeksta.Nema, Napomena = nap } };
                    if (TrebaSlika(reci)) strane[0].PostaviRaster(Raster.IzSlike(p.Sadrzaj));
                }
            }
            catch (TrajnaGreskaCitanja) { throw; }
            catch (IOException ex) { throw new PrivremenaGreskaCitanja("fajl nije dostupan: " + ex.Message, ex); }
            catch (Exception ex) { throw new TrajnaGreskaCitanja("fajl ne može da se otvori: " + ex.Message, ex); }
            if (!uvekOcr)
                try { Directory.CreateDirectory(kesFolder); File.WriteAllText(kesStrana, Newtonsoft.Json.JsonConvert.SerializeObject(strane), Encoding.UTF8); } catch { }
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

        private static readonly string[] SpisakNaslov = { "kocsijegyzek", "wagenliste", "spisak kola", "списак кола", "wagon list", "liste des wagons", "vykaz vozu" };

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
            var naslovSpiska = Ima("spisak kola", "списак кола", "nachweisung", "wagenliste", "wagon list", "liste des wagons", "vykaz vozu");

            // Dodatni list (Pótlap / Zusatzblatt / Dodatni list) po NASLOVU na vrhu strane i odeljcima „Rovat 7 / Feld 7“ – nije drugi tovarni list.
            // (Prednja strana pominje „Siehe Zusatzblatt“ u r7/r20, ali ne u naslovu.)
            var naslov = " " + TekstUtil.Kljuc(string.Join(" ", s.Reci.Where(w => w.Cy < 0.06).Select(w => w.T))) + " ";
            if (DodatniNaslov.Any(k => naslov.Contains(TekstUtil.Kljuc(k))) && Regex.IsMatch(s.Tekst, @"(?im)^\s*(?:rovat|feld|rubrika|рубрика|box|case)\s*\d{1,2}\b"))
            { nadjeno.Add("naslov dodatnog lista"); return Vrati(TipDokumenta.DodatniList); }
            // Izveštaj o sastavu voza / tehnički spisak voza (MÁV VTK „vonatterhelési kimutatás“, Zugvorbereitung, Meldezettel):
            // nije prateći dokument pošiljke – ne proverava se, ne traži se u r9.
            if (!formaCim && Ima("vonatterhelesi kimutatas", "vonatterhelési kimutatás", "vonatosszeallitas", "vonatösszeállítás", "tehervonat vonatterhelesi", "zugvorbereitung", "meldezettel"))
            { nadjeno.Add("izveštaj o sastavu voza"); return Vrati(TipDokumenta.SastavVoza); }
            // Spisak kola po NASLOVU na vrhu strane („Kocsijegyzék / Wagenliste“) – i kada ima mnogo natpisa polja kao obrazac
            if (SpisakNaslov.Any(k => naslov.Contains(TekstUtil.Kljuc(k))) && !CimKljucevi.Any(k => naslov.Contains(TekstUtil.Kljuc(k))))
            { nadjeno.Add("naslov spiska kola"); return Vrati(TipDokumenta.SpisakKola); }

            // Sadržaj strane ima prednost nad imenom fajla ("JCI and CIM….pdf", "WAGON 11.pdf"): obrazac CIM/CUV se prepoznaje
            // po naslovu obrasca ili po velikom broju odštampanih naziva rubrika.
            if ((formaCim && sidara >= 8) || (sidara >= 20 && !naslovSpiska)) { nadjeno.Add(sidara + " naziva rubrika"); return Vrati(TipDokumenta.Cim); }
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
            var jci = Ima("jedinstvena carinska isprava", "carinarnica izvoza", "carinska isprava", "jequhctbeha", "liaphhcka", "ucnpaba")
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

            var poImenu = ExcelCitac.TipPoImenu(fajl);
            if (poImenu != null) { nadjeno.Add("ime fajla"); return Vrati(poImenu); }

            // Carinski i prateći dokumenti po imenu fajla i naslovu – pre spiska kola (izvozna deklaracija takođe nabraja kola).
            if (ImeIma("vdd", "ead", "izvoz", "export", "ex-a", "exa", "ecs") || Ima("export accompanying document", "izvozni prateci", "prateci izvozni", "ausfuhrbegleitdokument", "izvozna deklaracija", "export declaration"))
                return Vrati(TipDokumenta.IzvoznaDeklaracija);
            if (ImeIma("t1", "t-1", "tad", "t2", "ncts") || (Ima("transit accompanying document", "tranzitni prateci", "prateci tranzitni", "versandbegleitdokument", "transit declaration") && mrn))
                return Vrati(TipDokumenta.TranzitnaDeklaracija);
            if (ImeIma("ens", "jci") || Ima("entry summary declaration", "jedinstvena carinska isprava")) return Vrati(TipDokumenta.DrugaCarinska);
            // Faktura pre EUR.1: "Invoice / Purchase Contract / Total" je faktura i kada sadrži "EUR" i broj
            var fakturaZnaci = new[] { "purchase contract", "shipment method", "payment terms", "total", "amount", "price" }.Count(x => t.Contains(" " + x));
            if (ImeIma("fv", "faktura", "invoice", "rechnung") || Ima("invoice", "inovice", "faktura", "rechnung", "racun br", "facture", "proforma") || fakturaZnaci >= 2)
            { if (fakturaZnaci >= 2) nadjeno.Add(fakturaZnaci + " oznaka fakture"); return Vrati(TipDokumenta.Faktura); }
            // EUR.1 samo po natpisu uverenja o kretanju robe (ne po "EUR" + "1" iz zaglavlja tabele)
            if (ImeIma("eur1", "eur.1", "eur 1") || Ima("movement certificate", "uverenje o kretanju robe", "warenverkehrsbescheinigung", "certificat de circulation")) return Vrati(TipDokumenta.Eur1);

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
                rez.StraneOpis.Add("str." + s.Broj + " " + tip + " (" + (razlogTipa.Length > 0 ? razlogTipa + "; " : "") + s.SrednjaPouzdanost.ToString("0.00") + ")");
                if (tip == TipDokumenta.Ostalo && s.Izvor == IzvorTeksta.Ocr && sablon.BrojSidara(s.Reci) >= 3)
                {
                    // Liči na CIM/CUV obrazac, ali OCR nije pročitao dovoljno naziva rubrika – ne sme da se zaključi "nema CIM-a".
                    rez.NeprocitaneStrane++;
                    napomene.Add("str. " + s.Broj + ": liči na CIM/CUV obrazac, ali nije pouzdano pročitana (OCR pouzdanost " + s.SrednjaPouzdanost.ToString("0.00") + ")");
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
                    if (PonoviNecitljive(p, s, raspored, cim) is CimSablon.Rezultat bolji) { raspored = bolji; cim = CimParser.Napravi(raspored, s, Pragovi); }
                    // Word → PDF: vrednosti su tekstualni sloj (bez OCR-a) – nema nečitljivih rubrika
                    if (raspored.IzWorda) CimParser.BezNecitljivih(cim);
                    cim.Sablon = raspored.Obrazac.Id;
                    cim.StranaLista = s.Broj;
                    foreach (var kv in raspored.Obrazac.Rubrike) if (raspored.NaStrani(kv.Value) is double[] ok) cim.Okviri[kv.Key] = ok.Select(v => Math.Round(v, 4)).ToArray();
                    cim.Zaglavlje = Zaglavlje(s);
                    // Prednja strana lista: ima pošiljaoca ili primaoca i bar jednu od rubrika 62/16/29 (prazan obrazac nema).
                    var imaPodataka = (cim.R1.Popunjena || cim.R4.Popunjena) && (cim.R62.Popunjena || cim.R16.Popunjena || cim.R29.Popunjena || cim.R21.Popunjena);
                    if (!imaPodataka) { prethodni = null; prethodniTip = null; continue; }
                    // Drugi primerak istog lista (original, duplikat…): OCR ga pročita sa malo drugačijim šumom, pa se poredi tolerantno.
                    var primerak = Primerak.Od(cim);
                    var isti = primerci.FirstOrDefault(x => x.IstiList(primerak));
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
                slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 300) : p.Sadrzaj);
                foreach (var kv in r.Obrazac.Kucice.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var rub = Regex.Match(kv.Key, @"^\d+[ab]?").Value;
                    var o = r.NaStrani(kv.Value);
                    bool Unutra(Rec w) => w.Cx >= o[0] && w.Cx <= o[2] && w.Cy >= o[1] && w.Cy <= o[3];
                    var postojece = r.Rubrika(rub).Where(Unutra).ToList();
                    var isecak = Slike.Isecak(slika, o[0], o[1], o[2], o[3], true);
                    var (t, pouzd) = ocr.Cifre(isecak, "0123456789-");
                    var dbg = Environment.GetEnvironmentVariable("CIMPROVERA_ISECCI");
                    if (!string.IsNullOrEmpty(dbg)) { Directory.CreateDirectory(dbg); File.WriteAllBytes(Path.Combine(dbg, Regex.Replace(p.Naziv, @"[^\w]", "_") + "_s" + s.Broj + "_k" + kv.Key + "_" + Regex.Replace(t ?? "", @"\W", "") + "_" + (int)(pouzd * 100) + ".png"), isecak); }
                    t = Regex.Replace(t ?? "", @"[^\d\- ]", "").Trim();
                    var cifara = t.Count(char.IsDigit);
                    // Tesseract sa ograničenim skupom znakova često ne daje pouzdanost – prihvata se kada broj cifara odgovara kućici.
                    var ocekivano = OcekivanoCifara(kv.Key);
                    var odgovara = ocekivano != null && ocekivano.Contains(cifara) && Regex.IsMatch(t, @"^[\d\-]+$");
                    if (cifara == 0 || (!odgovara && pouzd < 0.6)) continue;
                    if (odgovara) pouzd = Math.Max(pouzd, 0.85);
                    if (cifara < postojece.Sum(w => w.T.Count(char.IsDigit)) && postojece.Average(w => w.Pouzdanost) >= pouzd) continue;
                    var nova = new Rec { T = t, X0 = o[0], Y0 = o[1], X1 = o[2], Y1 = o[3], Pouzdanost = Math.Round(pouzd, 2) };
                    if (!r.Rubrike.TryGetValue(rub, out var lista)) r.Rubrike[rub] = lista = new List<Rec>();
                    // u kućici ostaju samo slova (npr. naziv mesta); cifre daje ciljani OCR
                    lista.RemoveAll(w => Unutra(w) && !Regex.IsMatch(w.T, @"\p{L}{3,}"));
                    lista.Add(nova);
                    r.Dodela.Add((nova, rub, false));
                }
            }
            catch (Exception ex) { log?.Upozorenje("Ciljani OCR kućica nije uspeo (str. " + s.Broj + "): " + ex.Message); }
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
                using (var slika = Slike.Ucitaj(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, s.Broj, 400) : p.Sadrzaj))
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
                return bolje > 0 ? r2 : null;
            }
            catch (Exception ex) { log?.Upozorenje("Ponovni OCR rubrika nije uspeo (str. " + s.Broj + "): " + ex.Message); return null; }
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
                t.Contains("wagenbrief cuv") || t.Contains("kolski list") ? "kolski list CUV" : "tovarni list CIM/CUV";
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
            if (s.Raster == null) s.PostaviRaster(Raster.IzSlike(p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, brojStrane, 150) : p.Sadrzaj));
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
