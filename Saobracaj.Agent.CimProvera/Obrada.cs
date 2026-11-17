using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Rezultat obrade jednog maila / foldera.</summary>
    internal sealed class Ishod
    {
        public RezultatProvere Provera;
        public List<string> Preskoceno = new List<string>();
        public List<string> NisuProcitani = new List<string>();
        /// <summary>Prilozi koji nisu mogli da se pročitaju ili klasifikuju – nisu dokaz da tovarnog lista nema.</summary>
        public List<string> Neprepoznati = new List<string>();
        /// <summary>Svaki prilog i njegova klasifikacija („ime – tovarni list CIM“).</summary>
        public List<string> OpisPriloga = new List<string>();
        /// <summary>Prilozi sa stranama skena niske pouzdanosti (ispod 0,75) koje nisu prepoznate kao tovarni list (v2.8).</summary>
        public List<string> NisaPouzdanost = new List<string>();
        public bool PrivremenaGreska;
        public bool Engleski;
    }

    /// <summary>
    /// Zajedničko jezgro za dolazni i odlazni mail i za CLI: svaki prilog se otvara; PDF i slike čita Claude,
    /// Excel se čita direktno; zatim rules engine nad svim dokumentima iz maila.
    /// </summary>
    internal sealed class Obrada
    {
        private readonly PravilaSkup pravila;
        private readonly ICitacDokumenata citac;
        private readonly Log log;
        private readonly double prag;

        public Obrada(PravilaSkup pravila, ICitacDokumenata citac, Log log, double prag)
        {
            this.pravila = pravila; this.citac = citac; this.log = log; this.prag = prag;
        }

        public Ishod Proveri(IEnumerable<(string ime, byte[] sadrzaj)> prilozi, string posiljalac, bool interni, DateTime referentniDatum,
            bool engleski, string rezimKp = "auto")
        {
            var ishod = new Ishod { Engleski = engleski };
            var dokumenti = new List<Dokument>();
            var fajlovi = new List<Prilog>();
            foreach (var (ime, b) in prilozi)
                fajlovi.AddRange(Raspakivanje.Raspakuj(ime, b, ishod.Preskoceno));
            foreach (var x in ishod.Preskoceno)
                if (x.Contains(Raspakivanje.NedozvoljenTip)) log?.Upozorenje("  prilog " + x + " – preskočen (pročitane strane .strane.json se prihvataju samo u testovima)");
                else log?.Info("  prilog " + x + " – preskočen");

            // Prilozi koji se ne čitaju: OpenOffice tabela, Pages, XPS, TIFF… nisu dokaz da tovarnog lista nema.
            foreach (var x in ishod.Preskoceno)
                if (Regex.IsMatch(x, @"\.(ods|pages|xps|tif|tiff|bmp)\b", RegexOptions.IgnoreCase)) ishod.Neprepoznati.Add(Regex.Replace(x, @"\s*\(.*$", ""));
            var lc = citac as LokalniCitac;

            // Isti fajl dvaput u mailu (npr. i u ZIP-u i kao prilog) čita se jednom.
            foreach (var f in fajlovi.GroupBy(x => x.Hes).Select(g => g.First()))
            {
                var pre = dokumenti.Count;
                // Logo, slika iz potpisa, ikona: ne čita se (nema OCR-a) i nije razlog za ručnu proveru
                if (Raspakivanje.Ukras(f) is string ukras)
                {
                    log?.Info("  prilog " + f.Naziv + " – slika iz potpisa/logo, ne proverava se: " + ukras);
                    ishod.OpisPriloga.Add(System.IO.Path.GetFileName(f.Naziv.Split('/').Last()) + " – slika iz potpisa/logo (ne proverava se)");
                    continue;
                }
                // v2.9.2 (3.1c): sopstveni PDF primer agenta (vraćen u prepisci) se ne proverava i ne upisuje
                if (Raspakivanje.SopstveniPrimer(f) is string primer)
                {
                    log?.Info("  prilog " + f.Naziv + " – " + primer + ", preskočeno (ne proverava se i ne upisuje).");
                    ishod.Preskoceno.Add(System.IO.Path.GetFileName(f.Naziv.Split('/').Last()) + " – sopstveni PDF primer (Primer ispravnog popunjavanja – Kombinovani prevoz)");
                    continue;
                }
                try
                {
                    if (f.Vrsta == VrstaPriloga.Excel || f.Vrsta == VrstaPriloga.Word)
                    {
                        // Tovarni list CIM/CUV kao Excel/Word obrazac: čita se iz ćelija (bez OCR-a); inače Excel = spisak kola / tabela MRN.
                        var word = f.Vrsta == VrstaPriloga.Word;
                        // Word tabela (.docx/.docm su ZIP); .doc/.rtf nemaju tabelu u XML-u – idu direktno u PDF
                        List<List<Celija>> celije = null;
                        if (!word) celije = TabelarniCim.IzExcela(f.Sadrzaj);
                        else if (f.Sadrzaj.Length > 4 && f.Sadrzaj[0] == 'P' && f.Sadrzaj[1] == 'K') try { celije = TabelarniCim.IzWorda(f.Sadrzaj); } catch { celije = null; }
                        var tovarni = celije == null ? null : lc?.TovarniIzTabele(f, celije, word ? "word" : "excel");
                        if (tovarni != null) dokumenti.Add(tovarni);
                        else if (!word) dokumenti.AddRange(ExcelCitac.Procitaj(f, pravila.Kp));
                        else ProcitajWord(f, dokumenti, ishod);
                        if (tovarni != null || !word)
                            log?.Info("  prilog " + f.Naziv + " (" + f.Vrsta + "): " + VrstePoStranama(dokumenti.Skip(pre)) + (tovarni != null ? " – " + tovarni.Opis : ""));
                    }
                    else
                    {
                        var r = citac.Procitaj(f);
                        dokumenti.AddRange(r.Dokumenti);
                        log?.Info("  prilog " + f.Naziv + " (" + f.Vrsta + "): " + VrstePoStranama(r.Dokumenti) + (r.NeprocitaneStrane > 0 ? ", nepročitano str.: " + r.NeprocitaneStrane : ""));
                        if (r.StraneOpis.Count > 0) log?.Info("    strane: " + string.Join(" | ", r.StraneOpis));
                        if (r.NeprocitaneStrane > 0 && !r.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)))
                            ishod.NisuProcitani.Add(f.Naziv + ": " + r.NeprocitaneStrane + " str. nije pročitano (" + r.Napomena + ")");
                        if (r.NisaPouzdanost.Count > 0)
                            ishod.NisaPouzdanost.Add(f.Naziv + ": str. " + string.Join(", ", r.NisaPouzdanost) + " skena niske pouzdanosti (ispod 0,75)");
                    }
                }
                catch (PrivremenaGreskaCitanja ex)
                {
                    ishod.PrivremenaGreska = true;
                    ishod.NisuProcitani.Add(f.Naziv + ": " + ex.Message);
                    log?.Upozorenje("  " + f + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    ishod.NisuProcitani.Add(f.Naziv + ": " + ex.Message);
                    log?.Upozorenje("  " + f + ": nije pročitan – " + ex.Message + (ex is TrajnaGreskaCitanja ? "" : Environment.NewLine + ex.StackTrace));
                }
                var novi = dokumenti.Skip(pre).ToList();
                if (novi.Count == 0 && ishod.NisuProcitani.Any(x => x.StartsWith(f.Naziv + ":"))) ishod.Neprepoznati.Add(f.Naziv);
                ishod.OpisPriloga.Add(System.IO.Path.GetFileName(f.Naziv.Split('/').Last()) + " – " + OpisPriloga(f, novi, ishod.Neprepoznati.Contains(f.Naziv)));
            }
            foreach (var n in ishod.Neprepoznati.Where(n => !fajlovi.Any(f => f.Naziv == n))) ishod.OpisPriloga.Add(System.IO.Path.GetFileName(n) + " – nije prepoznat");

            IstiListDvaput(dokumenti, log);

            // v2.8.4 (8): dosije pošiljke – raniji mailovi iste pošiljke (7 dana); prateći dokumenti iz dosijea važe kao priloženi
            PoslednjaPosiljka = null; RazlogVeze = null;
            if (Dosije != null && MailDosijea != null)
                try
                {
                    var pos = Dosije.Nadji(MailDosijea, dokumenti, out var razlog);
                    if (pos != null)
                    {
                        PoslednjaPosiljka = pos; RazlogVeze = razlog;
                        var iz = Dosije.DokumentiIzDosijea(pos);
                        log?.Info("  dosije pošiljke " + pos.Id + " (" + razlog + "; ranije: " + string.Join(", ", pos.Mailovi) + ")" + (iz.Count > 0 ? ", dokumenti iz dosijea: " + string.Join(", ", iz.Select(d => d.Tip + " (" + d.Fajl + ")")) : ""));
                        dokumenti.AddRange(iz);
                    }
                }
                catch (Exception ex) { log?.Upozorenje("  dosije pošiljke nije pročitan: " + ex.Message); }

            var u = new UlazProvere
            {
                Dokumenti = dokumenti, Posiljalac = posiljalac, InterniPosiljalac = interni,
                ReferentniDatum = referentniDatum, Engleski = engleski, RezimKp = rezimKp, Prag = prag,
            };
            ishod.Provera = new Motor(pravila, log).Proveri(u);
            // v2.8: interni izveštaj (KP tovarni list / ručna provera) je uvek na srpskom – nalazi se ponovo sastavljaju na srpskom
            if (engleski && Izvestaj.RazlogInternog(ishod.Provera, pravila) != null)
            {
                u.Engleski = false; ishod.Engleski = false;
                ishod.Provera = new Motor(pravila, log).Proveri(u);
            }
            ishod.Provera.OpisPriloga = ishod.OpisPriloga;
            // v2.8.4 (8): nova verzija istog lista / skenirana i overena verzija već proverenog digitalnog lista
            if (PoslednjaPosiljka != null)
                try { global::Saobracaj.Agent.CimProvera.Dosije.Primeni(PoslednjaPosiljka, ishod.Provera, ishod.Engleski); }
                catch (Exception ex) { log?.Upozorenje("  dosije (verzije) nije primenjen: " + ex.Message); }
            // v2.8.1: CIT 7 – veza sa listom iz istog maila ili iz ranije obrađenih (stanje\listovi.json), izmene i provere (interno)
            try { ishod.Provera.Cit7 = Cit7Provera.Proveri(ishod.Provera, RanijiListovi?.Invoke(), referentniDatum, NaslovMaila); }
            catch (Exception ex) { log?.Upozorenje("  CIT 7 nije proveren: " + ex.Message); }
            // v2.9.0 (7a.3): broj kola u tovarnom listu i na spisku kola (i iz dosijea) – interna napomena u izveštaju
            try { NapomenaBrojaKola(ishod.Provera); }
            catch (Exception ex) { log?.Upozorenje("  napomena o broju kola nije dodata: " + ex.Message); }
            return ishod;
        }

        /// <summary>
        /// v2.9.2 (3.2): nepročitan prilog može biti tovarni list – strana liči na CIM/CUV (natpisi rubrika na nepouzdanom skenu) ili naslov /
        /// tekst maila pominje CIM, CUV, tovarni list, teretnicu ili Frachtbrief. Inače nepročitan prilog ne traži internu poruku.
        /// </summary>
        internal static bool MozdaCim(Ishod ishod, string naslov, string telo)
        {
            if (ishod.NisuProcitani.Any(x => x.IndexOf("liči na CIM", StringComparison.OrdinalIgnoreCase) >= 0 || x.IndexOf("liči na obrazac CIM", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            if (ishod.Neprepoznati.Concat(ishod.NisuProcitani).Any(x => PominjeCim.IsMatch(System.IO.Path.GetFileName((x ?? "").Split(':')[0])))) return true;
            return PominjeCim.IsMatch((naslov ?? "") + " " + (telo ?? ""));
        }

        private static readonly Regex PominjeCim = new Regex(@"(?i)(?<![\p{L}])(cim|cuv|tovarn\p{L}*\s+list\p{L}*|teretnic\p{L}*|frachtbrief\p{L}*|consignment\s+note|wagon\s+note|lettre\s+de\s+voiture|товарн\p{L}*\s+лист\p{L}*|теретниц\p{L}*)(?![\p{L}])", RegexOptions.Compiled);

        /// <summary>
        /// v2.9.0 (7a.3): kada se broj kola u r21 (ili r18) razlikuje od broja kola na spisku kola istog maila / dosijea, u izveštaj ide
        /// interna napomena „Broj kola u r21 (26) ne odgovara spisku kola (28).“ (samo kada su svi spiskovi istog broja kola).
        /// </summary>
        internal static void NapomenaBrojaKola(RezultatProvere r)
        {
            var spiskovi = r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak).Where(s => s.Kola.Count > 0)
                .Select(s => s.Kola.Select(k => k.Broj).Distinct().Count()).Distinct().ToList();
            if (spiskovi.Count != 1) return;
            var naSpisku = spiskovi[0];
            foreach (var l in r.Listovi.Where(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac)))
            {
                var (rub, n) = Provere.BrojKolaUTekstu(l.Cim.R21?.Tekst) is int a && a > 0 ? ("21", a) : ("18", Provere.BrojKolaUTekstu(l.Cim.R18?.Tekst));
                if (n <= 0 || n == naSpisku || l.Nalazi.Any(x => x.Pravilo == "INFO-KOLA")) continue;
                var t = "Broj kola u r" + rub + " (" + n + ") ne odgovara spisku kola (" + naSpisku + ").";
                l.Nalazi.Add(new Nalaz { Pravilo = "INFO-KOLA", Rubrika = rub, Tezina = Tezina.Info, TezinaPravila = Tezina.Info, Tekst = t, Detalj = t, Kljuc = "INFO-KOLA|" + l.Oznaka, SamoInterno = true, Primer = "proverite broj kola u tovarnom listu i na spisku kola" });
            }
        }

        /// <summary>v2.8.4 (8): dosije pošiljaka (stanje\posiljke.json) i podaci o mailu; null = bez dosijea.</summary>
        public Dosije Dosije;
        public MailZaDosije MailDosijea;
        /// <summary>Pošiljka sa kojom je poslednji mail povezan (null = nova pošiljka) i razlog veze.</summary>
        public Dosije.Posiljka PoslednjaPosiljka;
        public string RazlogVeze;

        /// <summary>Zapis poslednjeg maila u dosije (posle odluke o odgovoru) i čuvanje stanja.</summary>
        public void ZapamtiUDosije(RezultatProvere r)
        {
            if (Dosije == null || MailDosijea == null || r == null) return;
            Dosije.Zapamti(PoslednjaPosiljka, MailDosijea, r.Dokumenti.Where(d => !d.IzDosijea).ToList(), r);
            Dosije.Sacuvaj();
        }

        /// <summary>v2.8.1: ranije obrađeni tovarni listovi (za CIT 7 bez lista u istom mailu); null = nema istorije.</summary>
        public Func<List<ZapisLista>> RanijiListovi;
        /// <summary>Naslov maila koji se obrađuje (za istoriju listova).</summary>
        public string NaslovMaila;

        private static readonly Regex ImeTovarnog = new Regex(@"(^|[^a-z])(cim|cuv|smgs)([^a-z]|$)|frachtbrief|wagenbrief|tovarni|consignment|nakladni|lettre", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Word prilog bez tabele obrasca: pretvara se u PDF (Syncfusion) i čita istim putem kao PDF (klasifikacija strana, šablon,
        /// vrednosti iz tekstualnog sloja). Neuspelo pretvaranje, ili ime koje liči na tovarni list a obrazac nije prepoznat → ručna
        /// provera (nikad „nema tovarnog lista“ pošiljaocu). Log: pretvoren, trajanje, broj strana i vrsta svake strane.
        /// </summary>
        private void ProcitajWord(Prilog f, List<Dokument> dokumenti, Ishod ishod)
        {
            var pdf = WordUPdf.Pretvori(f, out var razlog, out var trajanje);
            if (pdf == null)
            {
                if (WordUPdf.NedostajaoKljuc && !WordUPdf.KljucUcitan) log?.Greska("  prilog " + f.Naziv + " (Word): " + razlog);
                else log?.Upozorenje("  prilog " + f.Naziv + " (Word): " + razlog);
                ishod.NisuProcitani.Add(f.Naziv + ": " + razlog);
                return;
            }
            var pp = new Prilog { Naziv = f.Naziv, Sadrzaj = pdf, Vrsta = VrstaPriloga.Pdf, MediaType = "application/pdf", IzWorda = true };
            var r = citac.Procitaj(pp);
            foreach (var d in r.Dokumenti) { d.Fajl = f.Naziv; d.Izvor = "word"; }
            dokumenti.AddRange(r.Dokumenti);
            var strana = r.StraneOpis.Count > 0 ? r.StraneOpis.Count : r.Dokumenti.SelectMany(d => d.Strane).DefaultIfEmpty(0).Max();
            log?.Info("  prilog " + f.Naziv + " (Word): pretvoren u PDF za " + (int)trajanje.TotalMilliseconds + " ms, strana " + strana + " – " + VrstePoStranama(r.Dokumenti)
                      + (r.NeprocitaneStrane > 0 ? ", nepročitano str.: " + r.NeprocitaneStrane : ""));
            if (r.StraneOpis.Count > 0) log?.Info("    strane: " + string.Join(" | ", r.StraneOpis));
            if (!r.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)) && (r.NeprocitaneStrane > 0 || ImeTovarnog.IsMatch(System.IO.Path.GetFileNameWithoutExtension(f.Naziv) ?? "")))
            {
                var zasto = "Word prilog je pretvoren u PDF, ali obrazac tovarnog lista nije prepoznat" + (r.NeprocitaneStrane > 0 ? " (nepročitano strana: " + r.NeprocitaneStrane + ")" : " (ime fajla liči na tovarni list)") + " – potrebna je ručna provera";
                log?.Upozorenje("  prilog " + f.Naziv + ": " + zasto);
                ishod.NisuProcitani.Add(f.Naziv + ": " + zasto);
                if (!ishod.Neprepoznati.Contains(f.Naziv)) ishod.Neprepoznati.Add(f.Naziv);
            }
        }

        /// <summary>
        /// Isti tovarni list i kao PDF i kao Excel/Word (ista identifikacija r62 ili isto mesto i datum r29): proverava se jednom.
        /// Prednost ima PDF ako je potpisan ili pečatiran (sken, potpis u r1, pročitana r58a), inače tabela (pouzdano čitanje).
        /// </summary>
        internal static void IstiListDvaput(List<Dokument> dokumenti, Log log)
        {
            string Kljuc(CimList c) => Regex.Replace((c.R62.Zemlja ?? "") + (c.R62.Stanica ?? "") + (c.R62.Preduzece ?? "") + (c.R62.Broj ?? ""), @"\W", "").ToUpperInvariant() is var k && k.Length >= 8
                ? k : Regex.Replace((c.R29.Mesto ?? "") + (c.R29.Datum ?? ""), @"\W", "").ToUpperInvariant();
            var tabele = dokumenti.Where(d => TipDokumenta.JeTovarniList(d.Tip) && (d.Izvor == "excel" || d.Izvor == "word") && d.Cim.Count == 1 && string.IsNullOrEmpty(d.Cim[0].NeprepoznatObrazac)).ToList();
            foreach (var t in tabele)
            {
                var kt = Kljuc(t.Cim[0]);
                if (kt.Length < 6) continue;
                var pdf = dokumenti.FirstOrDefault(d => d != t && TipDokumenta.JeTovarniList(d.Tip) && d.Izvor != "excel" && d.Izvor != "word" && d.Cim.Any(c => string.IsNullOrEmpty(c.NeprepoznatObrazac) && Kljuc(c) == kt));
                if (pdf == null) continue;
                var c0 = pdf.Cim.First(c => Kljuc(c) == kt);
                var potpisan = pdf.Izvor == "ocr" || c0.R1.Potpis || c0.R58a.Popunjena;
                var izbacen = potpisan ? t : pdf;
                var ostaje = potpisan ? pdf : t;
                ostaje.Opis += "; isti list je i u " + System.IO.Path.GetFileName(izbacen.Fajl) + " – proveren je " + System.IO.Path.GetFileName(ostaje.Fajl) + (potpisan ? " (potpisan/pečatiran PDF)" : " (čitanje iz ćelija)");
                if (izbacen.Cim.Count <= 1) { izbacen.Tip = TipDokumenta.Ostalo; izbacen.Opis = "isti tovarni list kao " + System.IO.Path.GetFileName(ostaje.Fajl) + " (proverava se jednom)"; izbacen.Cim.Clear(); }
                else izbacen.Cim.Remove(izbacen.Cim.First(c => Kljuc(c) == kt));
                log?.Info("  isti tovarni list u " + izbacen.Fajl + " i " + ostaje.Fajl + " – proverava se " + ostaje.Fajl);
            }
        }

        /// <summary>Kako je prilog klasifikovan, za izveštaj: „tovarni list CIM“, „spisak kola“, „izveštaj o sastavu voza (ne proverava se)“…</summary>
        internal static string OpisPriloga(Prilog f, List<Dokument> docs, bool neprepoznat)
        {
            if (neprepoznat || docs.Count == 0) return "nije prepoznat";
            var ime = (f.Naziv ?? "").ToLowerInvariant();
            string Naziv(Dokument d)
            {
                switch (d.Tip)
                {
                    case TipDokumenta.Cim: case TipDokumenta.Cuv:
                        if (d.Cim.Count == 0) return d.Opis;
                        var c = d.Cim[0];
                        if (!string.IsNullOrEmpty(c.NeprepoznatObrazac)) return "tovarni list " + (d.Tip == TipDokumenta.Cuv ? "CUV" : "CIM") + " (obrazac nije automatski prepoznat – ručna provera)";
                        return "tovarni list " + (d.Tip == TipDokumenta.Cuv ? "CUV" : "CIM") + (d.Cim.Count > 1 ? " (" + d.Cim.Count + " lista)" : "") + (d.Izvor == "excel" ? " (Excel)" : d.Izvor == "word" ? " (Word)" : "");
                    case TipDokumenta.SpisakKola:
                        return Regex.IsMatch(ime, @"train ?list|load ?list|ladeliste") || d.Spisak.Any(s => s.Kola.Count > 0 && s.Kola.All(k => k.Uti.Count > 0) && Regex.IsMatch(ime, "train|load"))
                            ? "spisak tovarnih jedinica" : "spisak kola";
                    case TipDokumenta.SastavVoza: return "izveštaj o sastavu voza (ne proverava se)";
                    case TipDokumenta.SpisakJedinica: return "spisak tovarnih jedinica";
                    case TipDokumenta.Cit7: return "CIT 7 – naknadni zahtev (interni izveštaj)";
                    case TipDokumenta.DodatniList: return "dodatni list";
                    case TipDokumenta.IzvoznaDeklaracija: return "izvozna deklaracija";
                    case TipDokumenta.TranzitnaDeklaracija: return "tranzitna deklaracija";
                    case TipDokumenta.DrugaCarinska: return "carinski dokument";
                    case TipDokumenta.Eur1: return "EUR.1";
                    case TipDokumenta.UverenjeOPoreklu: return "uverenje o poreklu";
                    case TipDokumenta.Sertifikat: return "sertifikat";
                    case TipDokumenta.Faktura: return "faktura";
                    case TipDokumenta.TabelaMrn: return "tabela MRN";
                    case TipDokumenta.DozvolaOtpad: return "dozvola za otpad";
                    case TipDokumenta.AnnexVii: return "dokument o kretanju otpada (Annex VII)";
                    case TipDokumenta.IzvestajRadioloski: return "izveštaj o radiološkoj kontroli";
                    case TipDokumenta.Poledjina: return "poleđina lista";
                    case TipDokumenta.Najava: return "najava voza (ne proverava se)";
                    case TipDokumenta.Interni: return "interni obrazac KP";
                    case TipDokumenta.Ugovor: return "ugovor";
                    case TipDokumenta.Ponuda: return "ponuda";
                    default: return string.IsNullOrEmpty(d.Opis) || !d.Opis.StartsWith("isti tovarni list") ? "ostalo (ne proverava se)" : d.Opis;
                }
            }
            var nazivi = docs.Select(Naziv).Distinct().ToList();
            // „ostalo“ uz prepoznate dokumente iz istog PDF-a se ne navodi posebno
            if (nazivi.Count > 1) nazivi.Remove("ostalo (ne proverava se)");
            return string.Join(", ", nazivi);
        }

        /// <summary>Vrsta dokumenta po stranama za log, npr. "str.1 izvozna_deklaracija, str.5-6 tranzitna_deklaracija, str.7 cim, str.8 spisak_kola".</summary>
        internal static string VrstePoStranama(IEnumerable<Dokument> docs)
        {
            var d = docs.ToList();
            if (d.Count == 0) return "bez prepoznatih dokumenata";
            return string.Join(", ", d.Select(x => (x.Strane.Count == 0 ? "" : "str." + (x.Strane.Count == 1 ? x.Strane[0].ToString() : x.Strane.Min() + "-" + x.Strane.Max()) + " ")
                + x.Tip + (x.Tip == TipDokumenta.Cim || x.Tip == TipDokumenta.Cuv ? "(" + (x.Cim.FirstOrDefault()?.Sablon is string s && s.Length > 0 ? s : "?") + ")" : "")));
        }

        private static readonly string[] SrpskiDomeni = { "rs", "ba", "me", "hr", "si" };

        /// <summary>
        /// v2.8: jezik izveštaja po POŠILJAOCU, ne po (citiranom) tekstu maila: interni domen (@kprevoz.co.rs) uvek srpski;
        /// spoljni po vršnom domenu – .rs, .ba, .me, .hr, .si srpski, ostali engleski. Vraća true za engleski.
        /// </summary>
        public static bool EngleskiZaPosiljaoca(string adresa, Func<string, bool> jeInterni) => EngleskiZaPosiljaoca(adresa, jeInterni, null, null);

        /// <summary>
        /// v2.8.1: opšti mail domeni (gmail.com, yahoo.com, outlook.com, hotmail.com, live.com, icloud.com…) ne govore ništa o
        /// jeziku – jezik se određuje po tekstu koji je pošiljalac SAM napisao (naslov i telo bez citiranog dela i potpisa):
        /// srpska slova (č, ć, š, ž, đ, ćirilica) ili srpske reči („poštovani“, „pozdrav“, „vagon“, „tovarni“, „izvoz“) → srpski.
        /// </summary>
        public static bool EngleskiZaPosiljaoca(string adresa, Func<string, bool> jeInterni, string naslov, string telo)
        {
            if (string.IsNullOrWhiteSpace(adresa)) return false;
            if (jeInterni != null && jeInterni(adresa)) return false;
            var domen = adresa.Substring(adresa.LastIndexOf('@') + 1).Trim().TrimEnd('>', '.').ToLowerInvariant();
            var tld = domen.Substring(domen.LastIndexOf('.') + 1);
            if (SrpskiDomeni.Contains(tld)) return false;
            if (JeOpstiDomen(domen)) return !JeSrpskiTekst(TekstPosiljaoca(naslov, telo));
            return true;
        }

        private static readonly Regex OpstiDomen = new Regex(@"^(gmail|googlemail|yahoo|ymail|rocketmail|outlook|hotmail|live|msn|icloud|me|mac|aol|gmx|web|mail|email|proton|protonmail|pm|zoho|yandex|inbox|t-online|freemail|seznam|libero|virgilio|orange|wanadoo|free)\.[a-z.]{2,6}$", RegexOptions.Compiled);

        /// <summary>Opšti (besplatni) mail domen – ne pripada firmi i ne govori ništa o jeziku pošiljaoca.</summary>
        public static bool JeOpstiDomen(string domen) => OpstiDomen.IsMatch((domen ?? "").Trim().ToLowerInvariant());

        private static readonly Regex Citat = new Regex(
            @"^\s*(-{2,}\s*(original message|forwarded message|izvorna poruka|originalna poruka|prosle[dđ]ena poruka|urspr[uü]ngliche nachricht|weitergeleitete nachricht|message d'origine)"
            + @"|_{5,}"
            + @"|(from|od|šalje|salje|von|de|sent|poslato|gesendet|envoy[eé])\s*:\s*\S"
            + @"|on .{4,120}wrote:|dana .{4,120}(je )?napisa[oela]|am .{4,120}schrieb|le .{4,120}a [eé]crit)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Pozdrav = new Regex(
            @"^\s*(s po[sš]tovanjem|srda[cč]an pozdrav|srda[cč]no|pozdrav|pozdravi|lp|best regards|kind regards|regards|best wishes|mit freundlichen gr[uü](ß|ss)en|freundliche gr[uü](ß|ss)e|viele gr[uü](ß|ss)e|lg|cordialement|[uü]dv[oö]zlettel|pozdrawiam|s pozdravom)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Tekst koji je pošiljalac sam napisao: naslov (bez RE:/FW:) i telo do citiranog dela („From:“, „-----Original Message-----“,
        /// „On … wrote:“, redovi sa „&gt;“) i bez potpisa (posle „-- “ ili posle reda sa pozdravom).
        /// </summary>
        public static string TekstPosiljaoca(string naslov, string telo)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Regex.Replace(naslov ?? "", @"^\s*((re|fw|fwd|aw|wg|tr|odg|sv)\s*:\s*)+", "", RegexOptions.IgnoreCase));
            foreach (var red0 in (telo ?? "").Replace("\r", "").Split('\n'))
            {
                var red = red0.TrimEnd();
                if (Citat.IsMatch(red) || Regex.IsMatch(red, @"^--\s*$")) break;
                if (red.TrimStart().StartsWith(">")) continue;
                sb.AppendLine(red);
                if (red.Trim().Length <= 40 && Pozdrav.IsMatch(red)) break;
            }
            return sb.ToString();
        }

        private static readonly string[] SrpskeReci =
        {
            "postovani", "postovana", "pozdrav", "pozdravi", "pozdravom", "srdacan", "srdacno", "molim", "molimo", "hvala", "prilogu", "prilog",
            "dostavljam", "dostavljamo", "saljem", "saljemo", "vagon", "vagona", "vagoni", "vagone", "tovarni", "tovarnog", "tovarne", "tovarnih",
            "izvoz", "uvoz", "kola", "voz", "voza", "vozu", "carina", "carinjenje", "otprema", "isporuka", "dokumentacija", "dokumentaciju",
            "dokumenta", "spisak", "lp", "danas", "sutra", "ispravka", "ispravljen", "ispravljeni",
        };

        /// <summary>Tekst je na srpskom (i srodnim jezicima regiona): srpska slova ili ćirilica, ili bar jedna srpska reč.</summary>
        public static bool JeSrpskiTekst(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            if (Regex.IsMatch(tekst, @"[Ѐ-ӿ]")) return true;
            // č/š/ž postoje i u češkom, slovačkom, poljskom – ali tada ima i slova kojih u srpskom nema (ř, ě, ů, ľ, ą, ł, ő…)
            var tudja = Regex.IsMatch(tekst, "[řěůľťďňôäąęłńśźżőűșțăŘĚŮĽŤĎŇÔÄĄĘŁŃŚŹŻŐŰȘȚĂ]");
            if (!tudja && Regex.IsMatch(tekst, "[čćšžđČĆŠŽĐ]")) return true;
            var t = " " + PraviloKp.Normalizuj(tekst) + " ";
            return SrpskeReci.Any(r => Regex.IsMatch(t, @"(?<![a-z0-9])" + r + @"(?![a-z0-9])"));
        }

        /// <summary>Engleski odgovor ako je mail pošiljaoca na nemačkom ili engleskom (prosta procena po rečima).</summary>
        public static bool JeEngleskiIliNemacki(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            var t = " " + PraviloKp.Normalizuj(tekst) + " ";
            int Broj(params string[] reci) => reci.Sum(r => Regex.Matches(t, " " + r + " ").Count);
            var sr = Broj("postovani", "pozdrav", "pozdravom", "molim", "molimo", "tovarni", "prilogu", "hvala", "srdacan", "srdacno", "lp", "dostavljam", "saljem", "voz", "kola")
                     + Regex.Matches(tekst, "[čćšžđČĆŠŽĐ]").Count / 3;
            var en = Broj("dear", "please", "attached", "regards", "kind", "thank", "thanks", "find", "the", "and", "best",
                          "sehr", "geehrte", "anbei", "freundlichen", "grussen", "bitte", "danke", "und", "die", "der");
            return en > sr && en >= 2;
        }
    }
}
