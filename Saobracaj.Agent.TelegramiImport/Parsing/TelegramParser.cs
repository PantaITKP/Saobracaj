using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.TelegramiImport.Parsing
{
    /// <summary>
    /// Parsira tekst PDF telegrama IŽS-a. Ne zna ništa o bazi: daje tekstualne nazive pruge i stanica,
    /// koje zatim razrešava <see cref="Saobracaj.Agent.TelegramiImport.Data.Sifarnik"/>.
    /// </summary>
    public static class TelegramParser
    {
        public const int NapomenaMax = 250;
        private const RegexOptions O = RegexOptions.CultureInvariant;
        private const RegexOptions OI = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Linija u kojoj počinje telo telegrama (posle zaglavlja sa primaocima)
        private static readonly Regex PocetakTela = new Regex(@"^\s*(?:na pruzi|na zahtev|u sklopu|iz stanice|u stanici|na osnovu|ovim se|prema zahtevu|zbog\b)", O);
        // Kraj tela
        private static readonly Regex KrajTela = new Regex(@"^\s*(?:nadlezno operativno|podrucno operativno|nadlezno podrucno|zainteresovane|menadzer za|menadzer\b|zainteresovani)", O);
        // Linije koje završavaju tekst jedne tačke (dalje su napon, izvođači, kontakti...)
        private static readonly Regex KrajTacke = new Regex(@"^\s*-?\s*(?:bez napona|pod naponom|radove\b|radovi ce|radove ce|napomena|investitor|izvodj|izvodi\b|lica ispred|lice za|lice ispred|lica za|odgovorn|koordin|kordin|tehnicko lice|potreban|iskljucenje napona|kontrol|dok se|u navedenom|zatvaranje\s*/\s*otvaranje|ispred\b|ovaj telegram|tokom radova|lkp\b|u ime\b|stanice .* ce putem|nadlezno|podrucno|zainteresovan|menadzer)", O);
        private static readonly Regex TackaBroj = new Regex(@"^\s*(\d{1,2})\s*\.(?!\d)\s*(?=\S)", O);
        private static readonly Regex TackaSlovo = new Regex(@"^\s*([a-h])\)\s*", O);

        private const string Ime = @"(?:(?i:rasp\.\s*|rasputnic[ae]\s+))?[A-Z""][\w""'.]*(?:\s+(?:[A-Z""][\w""'.]*|\d(?![\d./])|reka|banja|polje|most|selo|brdo|gl\.|glavna|ranzirna|ranz\.|teretna|varos|nova|stara|putnicka|luka|pristaniste|grad|kolonija|naselje|"")){0,3}";
        // "Rasputnica"/"rasp." se ne odbacuje: u šifarniku postoje stanice "Rasputnica Ćuprija", "Rasputnica B" ...
        private const string Prefiks = @"(?:(?:(?i:ukr\.|st\.|sl\.\s*mesta|staj\.)\s*|(?i:ukrsnic[ae]|stanic[ae]|stanica|sluzbenih mesta|sluzbenog mesta|stajalist[ae]|otpremnistv[ao]|ops|psn|ps|cvor[a]?|teretne stanice)\s+)){0,2}";
        private const string Zagrada = @"(?:\s*\([^)]{1,12}\))?";
        private static readonly Regex[] Lokacije =
        {
            // niz: "između službenih mesta Lajkovac-Slovac-Divci - Valjevo" → prva i poslednja stanica (kao u ručnom unosu)
            new Regex(@"(?i:izmedu|izmedju)\s+(?i:sluzbenih mesta|sl\.\s*mesta|stanica)\s+(?<od>" + Ime + @")(?:\s*-\s*(?<do>" + Ime + "))+", O),
            new Regex(@"(?i:otvorenoj pruzi|otvorena pruga|otvorene pruge|deonici|relaciji|dela pruge|delu pruge|deo pruge)\s+" + Prefiks + "(?<od>" + Ime + ")" + Zagrada + @"\s*-\s*" + Prefiks + "(?<do>" + Ime + ")", O),
            new Regex(@"(?i:izmedu|izmedju)\s+" + Prefiks + "(?<od>" + Ime + ")" + Zagrada + @"\s+(?i:i|-)\s+(?:(?<gr>(?i:drzavne granice|drzavna granica|granice|dg\b))|" + Prefiks + "(?<do>" + Ime + "))", O),
            new Regex(@"(?i:iz stanice)\s+(?<od>" + Ime + ")" + Zagrada + @"\s+(?i:za stanicu|do stanice)\s+(?<do>" + Ime + ")", O),
            new Regex(@"(?i:u st\.|u stanici|u stanica|u stanicama|stanice|stanica|u sl\.\s*mestu|u sluzbenom mestu|na stajalistu|stajalista|ukrsnice|rasputnice)\s*(?<od>" + Ime + ")", O),
        };

        public static ParsiranTelegram Parsiraj(string pdfTekst, string naslov = null, DateTime? datumMaila = null)
        {
            var p = new ParsiranTelegram();
            if (string.IsNullOrWhiteSpace(pdfTekst) || pdfTekst.Trim().Length < 40)
            {
                p.BezTeksta = true;
                p.Upozorenja.Add("PDF nema tekst (skeniran dokument?).");
                return p;
            }

            var linije = TekstUtil.UjednaciZnakove(pdfTekst).Split('\n').Select(l => l.TrimEnd()).ToList();
            var fold = linije.Select(l => TekstUtil.BezDijakritika(l).ToLowerInvariant()).ToList();

            ZaglavljeBrojDatum(p, linije, fold, naslov, datumMaila);

            // Telo
            int start = fold.FindIndex(l => PocetakTela.IsMatch(l));
            if (start < 0)
            {
                p.Upozorenja.Add("Nije pronađen početak teksta telegrama (Na pruzi / Na zahtev / U sklopu / Iz stanice).");
                start = Math.Min(fold.Count - 1, 0);
            }
            int end = fold.FindIndex(start + 1, l => KrajTela.IsMatch(l));
            if (end < 0) end = fold.Count;
            var telo = new List<string>();
            var teloFold = new List<string>();
            for (int i = start; i < end; i++)
            {
                if (string.IsNullOrWhiteSpace(linije[i])) continue;
                if (linije[i].Contains("@")) continue;
                telo.Add(linije[i].Trim());
                teloFold.Add(fold[i].Trim());
            }
            p.Telo = string.Join("\n", telo);
            var teloJedanRed = Spoji(teloFold);

            Klasifikuj(p, teloJedanRed);

            // Otkazivanje / van snage: deo pre "daje se novi koji glasi"
            int novi = teloJedanRed.IndexOf("daje se novi", StringComparison.Ordinal);
            var deoOtkazivanja = novi >= 0 ? teloJedanRed.Substring(0, novi) : teloJedanRed;
            Otkazivanja(p, deoOtkazivanja, Spoji(telo));

            if (!p.ZaUpis) return p;

            // Ako postoji "daje se novi koji glasi:", stavke se čitaju samo iz novog teksta
            int odLinije = 0;
            if (novi >= 0)
            {
                int k = teloFold.FindIndex(l => l.Contains("daje se novi") || l.Contains("koji glasi"));
                odLinije = Math.Max(0, k + 1);
            }

            if (p.Vrste.Contains(Vrsta.NarocitaPosiljka))
                NarocitaPosiljka(p, telo, teloFold);
            else
                Zatvori(p, telo.Skip(odLinije).ToList(), teloFold.Skip(odLinije).ToList());

            return p;
        }

        // ------------------------------------------------------------------ zaglavlje

        private static void ZaglavljeBrojDatum(ParsiranTelegram p, List<string> linije, List<string> fold, string naslov, DateTime? datumMaila)
        {
            var broj = new Regex(@"tel(?:egram)?\W{0,6}br(?:oj)?\.?\s*(\d{1,4})\s*(f)?(?![\w])(?!\s*od\s*\d)", O);
            for (int i = 0; i < Math.Min(fold.Count, 80) && p.Broj == null; i++)
            {
                var m = broj.Match(fold[i]);
                if (m.Success)
                {
                    p.Broj = int.Parse(m.Groups[1].Value, Inv);
                    p.Oznaka = p.Broj + (m.Groups[2].Success ? "F" : "");
                }
            }
            var foldNaslov = TekstUtil.BezDijakritika(naslov ?? "").ToLowerInvariant();
            if (p.Broj == null && naslov != null)
            {
                var m = new Regex(@"tel(?:egram)?\W{0,6}br(?:oj)?\.?\s*(\d{1,4})\s*(f)?", O).Match(foldNaslov);
                if (m.Success) { p.Broj = int.Parse(m.Groups[1].Value, Inv); p.Oznaka = p.Broj + (m.Groups[2].Success ? "F" : ""); p.Upozorenja.Add("Broj telegrama pročitan iz naslova mail-a."); }
            }

            var datum = new Regex(@"(\d{1,2})\s*\.\s*(\d{1,2})\s*\.+\s*(\d{4})", O);
            DateTime? pdfDatum = null;
            for (int i = 0; i < Math.Min(fold.Count, 30) && pdfDatum == null; i++)
            {
                if (!Regex.IsMatch(fold[i], @"beograd|datum|godin|god\.", O)) continue;
                var m = datum.Match(fold[i]);
                if (m.Success) pdfDatum = D(m);
            }
            DateTime? naslovDatum = null;
            if (naslov != null)
            {
                var m = new Regex(@"\bod\s*(\d{1,2})\s*\.\s*(\d{1,2})\s*\.+\s*(\d{4})", O).Match(foldNaslov);
                if (m.Success) naslovDatum = D(m);
            }
            var kandidati = new[] { pdfDatum, naslovDatum }.Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            if (kandidati.Count == 0)
            {
                p.Datum = datumMaila?.Date;
                if (p.Datum != null) p.Upozorenja.Add("Datum telegrama nije pronađen; uzet datum mail-a.");
            }
            else if (kandidati.Count == 1 || datumMaila == null)
            {
                p.Datum = pdfDatum ?? naslovDatum;
                if (kandidati.Count > 1) p.Upozorenja.Add("Datum u PDF-u i naslovu se razlikuju; uzet datum iz PDF-a.");
            }
            else
            {
                p.Datum = kandidati.OrderBy(x => Math.Abs((x - datumMaila.Value.Date).TotalDays)).First();
                p.Upozorenja.Add("Datum u PDF-u (" + F(pdfDatum) + ") i naslovu (" + F(naslovDatum) + ") se razlikuju; uzet datum najbliži datumu mail-a: " + F(p.Datum) + ".");
            }
            if (p.Datum != null && datumMaila != null && Math.Abs((p.Datum.Value - datumMaila.Value.Date).TotalDays) > 7)
            {
                var blizi = kandidati.OrderBy(x => Math.Abs((x - datumMaila.Value.Date).TotalDays)).First();
                if (Math.Abs((blizi - datumMaila.Value.Date).TotalDays) > 7)
                {
                    p.Upozorenja.Add("Datum telegrama " + F(p.Datum) + " je daleko od datuma mail-a " + F(datumMaila) + "; uzet datum mail-a.");
                    p.Datum = datumMaila.Value.Date;
                }
            }

            var sve = string.Join(" ", fold.Take(30));
            p.Izdavac = sve.Contains("pristup infrastrukt") || foldNaslov.Contains(" rv") ? "RV" : "SP";
        }

        // ------------------------------------------------------------------ vrsta

        private static void Klasifikuj(ParsiranTelegram p, string t)
        {
            void Dodaj(Vrsta v) { if (!p.Vrste.Contains(v)) p.Vrste.Add(v); }

            if (Regex.IsMatch(t, @"van snage", O)) Dodaj(Vrsta.VanSnage);
            if (Regex.IsMatch(t, @"otkazuj\w* se", O)) Dodaj(Vrsta.Otkazivanje);
            if (Regex.IsMatch(t, @"\bmenja se\b|\bmenjaju se\b|izmenjuje se|vrsi se izmena", O)) Dodaj(Vrsta.Izmena);

            var noviDeo = t;
            int novi = t.IndexOf("daje se novi", StringComparison.Ordinal);
            if (novi >= 0) noviDeo = t.Substring(novi);
            else if (p.Vrste.Count > 0)
            {
                // Čisto otkazivanje: "otkazuje se zatvor" nije novi zatvor
                noviDeo = Regex.Replace(t, @"otkazuj\w* se zatvor\w*", " ", O);
            }

            // NP u telegramima RV-a (ad hoc vozovi) se ne upisuje; ručno je upisivana samo NP Sektora za SP (236)
            if (p.Izdavac != "RV" && Regex.IsMatch(noviDeo, @"izvrsiti prevoz np|prevoz np\b|posebni sigurnosni uslovi|narocit\w* posiljk", O)) Dodaj(Vrsta.NarocitaPosiljka);
            if (Regex.IsMatch(noviDeo, @"zabranjuje se saobracaj", O)) Dodaj(Vrsta.Skretnica);
            // Testiranje vozila (206F, 209F) se ni ručno ne upisuje u Telegrami
            var testiranje = Regex.IsMatch(noviDeo, @"testiranj", O);
            if (!testiranje && Regex.IsMatch(noviDeo, @"odobrava(?:ju)?\s+se\b.{0,80}?zatvor|odobrav\w*\s+zatvor|zatvor\w*\s+se\s+odobrava", O)) Dodaj(Vrsta.Zatvor);
            // Radovi sa isključenjem napona u voznom vodu su ručno upisivani kao zatvor (257F)
            if (!testiranje && !p.Vrste.Contains(Vrsta.Zatvor) && Regex.IsMatch(noviDeo, @"planira\w* se radovi.{0,400}?sa iskljucenjem napona", O)
                && !Regex.IsMatch(noviDeo, @"bez prekida saobracaja|ne zahtevaju|bez zatvora|odobravaju se radovi", O)) Dodaj(Vrsta.Zatvor);
            if (Regex.IsMatch(noviDeo, @"lagan\w* voznj", O)) Dodaj(Vrsta.LaganaVoznja);
            if (Regex.IsMatch(noviDeo, @"\bposed\w*|neposednut", O)) Dodaj(Vrsta.Posedanje);
            if (Regex.IsMatch(noviDeo, @"putn\w* prelaz", O)) Dodaj(Vrsta.PutniPrelaz);
            if (Regex.IsMatch(noviDeo, @"uvodi se u saobracaj voz|najavljen je saobracaj|ad hoc|vanredn\w* voz|racunanj\w* vremena", O)) Dodaj(Vrsta.RedVoznje);
            if (Regex.IsMatch(noviDeo, @"pruznih telefona|servisa za komunikaciju|\bispad\w*|tt uredja", O)) Dodaj(Vrsta.TT);
            if (Regex.IsMatch(noviDeo, @"odobravaju se radovi|odobrava se izvodjenje|bez prekida saobracaja|ne zahtevaju ni zatvor|bez zatvora", O)) Dodaj(Vrsta.Radovi);

            // Skretnica je "zabrana saobraćaja", a ne zatvor; posedanje u zatvoru (npr. "stanica ... biće posednuta") ne menja vrstu
            if (p.Vrste.Count == 0) Dodaj(Vrsta.Ostalo);
        }

        // ------------------------------------------------------------------ otkazivanja

        private static void Otkazivanja(ParsiranTelegram p, string fold, string original)
        {
            if (!p.Vrste.Contains(Vrsta.VanSnage) && !p.Vrste.Contains(Vrsta.Otkazivanje) && !p.Vrste.Contains(Vrsta.Izmena))
                return;

            var refRx = new Regex(@"(?:telegram\w*|tel\.)\s*(?:""?\s*izs\s*""?\s*)?(?:a\.?\s*d\.?\s+)?(?:br(?:oj)?\.?\s*)?(\d{1,4})\s*f?\s*od\s*(\d{1,2})\s*\.\s*(\d{1,2})\s*\.+\s*(\d{4})", O);
            // Rečenice: tačka pa razmak pa veliko slovo (u originalu); u fold tekstu tražimo granice po istim indeksima nije pouzdano,
            // pa delimo fold tekst na ". " + slovo koje nije cifra.
            var recenice = Regex.Split(fold, @"(?<=[^\d]\.)\s+(?=[a-z""])|(?<=\d{4}\.)\s+(?=ovim|ovaj|na |u |telegram)", O);
            foreach (var r in recenice)
            {
                Vrsta? akcija = null;
                if (Regex.IsMatch(r, @"van snage", O)) akcija = Vrsta.VanSnage;
                else if (Regex.IsMatch(r, @"otkazuj\w* se", O)) akcija = Vrsta.Otkazivanje;
                else if (Regex.IsMatch(r, @"\bmenja se\b|\bmenjaju se\b|izmenjuje se|vrsi se izmena", O)) akcija = Vrsta.Izmena;
                if (akcija == null) continue;

                foreach (Match m in refRx.Matches(r))
                {
                    var broj = int.Parse(m.Groups[1].Value, Inv);
                    var datum = D(m.Groups[2], m.Groups[3], m.Groups[4]);
                    if (broj == p.Broj && datum == p.Datum) continue;
                    var c = new CiljniTelegram { Broj = broj, Datum = datum, Akcija = akcija.Value };
                    var tm = Regex.Match(r.Substring(0, m.Index), @"tack\w*\s*(?:pod\s*)?(?:red\.?\s*br\.?\s*)?(\d{1,2}|[a-h])\s*[.)]?\s*(?:u\s*)?$", O);
                    if (tm.Success) c.Tacka = tm.Groups[1].Value;
                    if (akcija == Vrsta.Otkazivanje && Regex.IsMatch(r, @"otkazuj\w* se zatvor", O) && !Regex.IsMatch(r, @"otkazuj\w* se telegram", O))
                    {
                        var vi = VremenskiIzraz.Parsiraj(r.Remove(m.Index, m.Length), p.Datum ?? DateTime.Today);
                        c.Dani.AddRange(vi.Dani);
                        c.Dani.AddRange(vi.Noci);
                    }
                    Dodaj(p, c);
                }
            }

            if (p.Ciljevi.Count == 0)
                p.Upozorenja.Add("Telegram otkazuje / stavlja van snage, ali broj i datum ciljnog telegrama nisu pronađeni.");
        }

        private static void Dodaj(ParsiranTelegram p, CiljniTelegram c)
        {
            var postoji = p.Ciljevi.FirstOrDefault(x => x.Broj == c.Broj && x.Datum == c.Datum);
            if (postoji == null) { p.Ciljevi.Add(c); return; }
            // "Van snage" celog telegrama ima prednost nad delimičnim otkazivanjem
            if (c.Tacka == null && c.Dani.Count == 0)
            {
                postoji.Tacka = null;
                postoji.Dani.Clear();
                if (c.Akcija == Vrsta.VanSnage || postoji.Akcija != Vrsta.VanSnage) postoji.Akcija = c.Akcija;
            }
        }

        // ------------------------------------------------------------------ zatvori

        private sealed class Segment
        {
            public string Tacka;
            public List<string> Orig = new List<string>();
            public List<string> Fold = new List<string>();
            /// <summary>Zajednički uvod za pod-stavke sa crticom (pruga, relacija, vrsta zatvora).</summary>
            public Segment Zajednicko;
        }

        private static void Zatvori(ParsiranTelegram p, List<string> telo, List<string> fold)
        {
            // Pronađi tačke (1., 2. ... ili a), b) ...) u nizu
            var tacke = new List<Tuple<int, string>>();
            int ocekivanBroj = 1; char ocekivanoSlovo = 'a';
            for (int i = 0; i < fold.Count; i++)
            {
                var mb = TackaBroj.Match(fold[i]);
                if (mb.Success && int.Parse(mb.Groups[1].Value, Inv) == ocekivanBroj && !KrajTacke.IsMatch(fold[i].Substring(mb.Length)))
                {
                    tacke.Add(Tuple.Create(i, mb.Groups[1].Value)); ocekivanBroj++; continue;
                }
                var ms = TackaSlovo.Match(fold[i]);
                if (ms.Success && ms.Groups[1].Value[0] == ocekivanoSlovo)
                {
                    tacke.Add(Tuple.Create(i, ms.Groups[1].Value + ")")); ocekivanoSlovo++;
                }
            }
            // Jedna "tačka" nije lista; tačke moraju sadržati zatvor ili datum
            if (tacke.Count < 2 || !tacke.Any(x => Regex.IsMatch(fold[x.Item1], @"dana|zatvor|odobrava|\d{1,2}\.\d{1,2}", O)))
                tacke.Clear();

            var uvod = new Segment();
            int krajUvoda = tacke.Count > 0 ? tacke[0].Item1 : fold.Count;
            // Bez tačaka: tekst zatvora počinje od "Na pruzi" (uvodni pasus "U sklopu realizacije projekta..." se preskače)
            int pocetakUvoda = 0;
            if (tacke.Count == 0)
            {
                int np = fold.FindIndex(l => l.Contains("na pruzi"));
                if (np > 0) pocetakUvoda = np;
            }
            for (int i = pocetakUvoda; i < krajUvoda; i++)
            {
                if (tacke.Count == 0 && i > pocetakUvoda && KrajTacke.IsMatch(fold[i])) break;
                uvod.Orig.Add(telo[i]); uvod.Fold.Add(fold[i]);
            }

            var segmenti = new List<Segment>();
            if (tacke.Count == 0)
            {
                // Pod-stavke sa crticom koje imaju svoj datum ("-dana 29.10 ... od 10.00 do 11.00") su zasebni redovi;
                // crtice samo sa intervalom (3F: "- od 07.50 do 09.30") ostaju zajedničke.
                var crtice = new List<int>();
                for (int i = 0; i < uvod.Fold.Count; i++)
                    // "-dana 29.10.2026 ..." (279F) ili podnaslov "za dan 04.11.2026.godine:" sa intervalima ispod (21F)
                    if (Regex.IsMatch(uvod.Fold[i], @"^\s*(?:-\s*(?:dana|dan|za dan)\b|za\s+dan[a]?\b)\W*\d{1,2}\s*\.\s*\d{1,2}", O)) crtice.Add(i);
                if (crtice.Count >= 2)
                {
                    var zajednicko = new Segment();
                    for (int i = 0; i < crtice[0]; i++) { zajednicko.Orig.Add(uvod.Orig[i]); zajednicko.Fold.Add(uvod.Fold[i]); }
                    for (int k = 0; k < crtice.Count; k++)
                    {
                        var s = new Segment();
                        int kraj = k + 1 < crtice.Count ? crtice[k + 1] : uvod.Fold.Count;
                        for (int i = crtice[k]; i < kraj; i++) { s.Orig.Add(uvod.Orig[i]); s.Fold.Add(uvod.Fold[i]); }
                        s.Zajednicko = zajednicko;
                        segmenti.Add(s);
                    }
                }
                else segmenti.Add(uvod);
            }
            else
            {
                for (int k = 0; k < tacke.Count; k++)
                {
                    var s = new Segment { Tacka = tacke[k].Item2 };
                    int kraj = k + 1 < tacke.Count ? tacke[k + 1].Item1 : fold.Count;
                    for (int i = tacke[k].Item1; i < kraj; i++)
                    {
                        if (i > tacke[k].Item1 && KrajTacke.IsMatch(fold[i])) break;
                        s.Orig.Add(telo[i]); s.Fold.Add(fold[i]);
                    }
                    segmenti.Add(s);
                }
            }

            var uvodTekst = Spoji(uvod.Orig);
            var uvodNorm = TekstUtil.BezDijakritika(uvodTekst);
            p.PrugaTekst = Pruga(uvodNorm);
            var uvodLokacija = Lokacija(uvodNorm);
            var uvodFold = Spoji(uvod.Fold);
            var uvodVreme = VremenskiIzraz.Parsiraj(uvodFold, p.Datum ?? DateTime.Today);

            foreach (var s in segmenti)
            {
                var orig = Spoji(s.Orig);
                var norm = TekstUtil.BezDijakritika(orig);
                var f = Spoji(s.Fold);

                // Ako je bez tačaka, pruga ide u Opis, ali ne i u lokaciju / napomenu
                var lokTekst = norm;
                if (s.Tacka == null)
                {
                    var mp = Regex.Match(norm, @"(?i:na pruzi)\s+.+?(?=,\s|:\s|\s(?i:zbog|odobrava))", O);
                    if (mp.Success) lokTekst = norm.Substring(mp.Index + mp.Length);
                }
                var lokacije = Lokacije2(lokTekst);
                if (lokacije.Count == 0 && uvodLokacija != null) lokacije.Add(uvodLokacija);

                // Pod-stavka sa crticom: kolosek i napomena iz zajedničkog uvoda + sopstvenog reda
                var kontekstOrig = orig;
                var kontekstFold = f;
                if (s.Zajednicko != null)
                {
                    kontekstOrig = Spoji(s.Zajednicko.Orig.Concat(s.Orig));
                    kontekstFold = Spoji(s.Zajednicko.Fold.Concat(s.Fold));
                }

                var vreme = VremenskiIzraz.Parsiraj(f, p.Datum ?? DateTime.Today);
                if (s.Zajednicko != null && vreme.Intervali.Count == 0)
                    vreme.Intervali.AddRange(VremenskiIzraz.Parsiraj(Spoji(s.Zajednicko.Fold), p.Datum ?? DateTime.Today).Intervali);
                if (vreme.Dani.Count == 0 && vreme.Noci.Count == 0 && s.Tacka != null)
                {
                    // datumi zajednički za sve tačke ("dana 03.11.2026 ... prema sledećem: 1. ...")
                    foreach (var d in uvodVreme.Dani) vreme.Dani.Add(d);
                    foreach (var d in uvodVreme.Noci) vreme.Noci.Add(d);
                }
                if (vreme.Intervali.Count == 0 && uvodVreme.Intervali.Count > 0 && s.Tacka != null)
                    vreme.Intervali.AddRange(uvodVreme.Intervali);

                var vrsta = p.Vrste.Contains(Vrsta.Skretnica) && Regex.IsMatch(kontekstFold, @"zabranjuje se saobracaj", O) ? Vrsta.Skretnica : Vrsta.Zatvor;
                var kolosek = Kolosek(kontekstFold, vrsta);
                var napomena = Napomena(kontekstOrig, kontekstFold, s.Tacka != null);

                var redovi = Redovi(vreme, p.Datum);
                if (redovi.Count == 0)
                {
                    p.Upozorenja.Add("Tačka " + (s.Tacka ?? "-") + ": nisu pronađeni datumi.");
                    continue;
                }
                var lista = lokacije.Count > 0 ? lokacije : new List<Tuple<string, string>> { null };
                foreach (var lok in lista)
                    foreach (var red in redovi)
                    {
                        var st = new Stavka
                        {
                            Tacka = s.Tacka,
                            Vrsta = vrsta,
                            PrugaTekst = p.PrugaTekst,
                            OdTekst = lok?.Item1,
                            DoTekst = lok?.Item2,
                            Kolosek = kolosek,
                            VaziOd = red.Item1,
                            VaziDo = red.Item2,
                            Napomena = napomena
                        };
                        st.Upozorenja.AddRange(vreme.Upozorenja);
                        if (red.Item3 != null) st.Upozorenja.Add(red.Item3);
                        if (lok == null) st.Upozorenja.Add("Nije pronađena relacija (stanice).");
                        if (!p.Stavke.Any(x => x.OdTekst == st.OdTekst && x.DoTekst == st.DoTekst && x.VaziOd == st.VaziOd && x.VaziDo == st.VaziDo && x.Kolosek == st.Kolosek))
                            p.Stavke.Add(st);
                    }
            }
        }

        /// <summary>(VaziOd, VaziDo, upozorenje) po nizu uzastopnih dana × dnevnom intervalu, kao u ručnom unosu.</summary>
        private static List<Tuple<DateTime, DateTime, string>> Redovi(VremenskiIzraz v, DateTime? datumTelegrama)
        {
            var rez = new List<Tuple<DateTime, DateTime, string>>();
            var nizovi = v.Nizovi().ToList();
            if (v.DoOpoziva || (v.VaziDoDatuma != null && nizovi.Count <= 1))
            {
                var pocetak = nizovi.Count > 0 ? nizovi.Min(n => n.Item1) : (datumTelegrama ?? DateTime.Today);
                var kraj = v.VaziDoDatuma ?? new DateTime(pocetak.Year, 12, 31);
                nizovi = new List<Tuple<DateTime, DateTime, bool>> { Tuple.Create(pocetak, kraj, false) };
                if (v.Intervali.Count == 0)
                {
                    rez.Add(Tuple.Create(pocetak.Date, kraj.Date, (string)null));
                    return rez;
                }
            }
            if (nizovi.Count == 0) return rez;

            if (v.Intervali.Count == 0)
            {
                foreach (var n in nizovi)
                    rez.Add(Tuple.Create(n.Item1, n.Item3 ? n.Item2.AddDays(1) : n.Item2.Add(new TimeSpan(23, 59, 0)),
                        "Nije pronađen dnevni interval; upisan ceo dan."));
                return rez;
            }
            foreach (var n in nizovi)
                foreach (var iv in v.Intervali)
                {
                    string upoz = null;
                    var od = n.Item1 + iv.Od;
                    var kraj = n.Item2 + iv.Do;
                    if (n.Item3 || iv.PrekoPonoci) kraj = kraj.AddDays(1);
                    if (n.Item3 && !iv.PrekoPonoci) upoz = "Noć (d/d) sa intervalom koji ne prelazi ponoć.";
                    if (!n.Item3 && iv.PrekoPonoci && n.Item1 != n.Item2) upoz = "Interval prelazi ponoć u opsegu dana; kraj je dan posle poslednjeg dana.";
                    rez.Add(Tuple.Create(od, kraj, upoz));
                }
            return rez;
        }

        // ------------------------------------------------------------------ naročita pošiljka

        private static void NarocitaPosiljka(ParsiranTelegram p, List<string> telo, List<string> fold)
        {
            var orig = Spoji(telo);
            var norm = TekstUtil.BezDijakritika(orig);
            var f = Spoji(fold);
            var lok = Lokacija(norm);
            var put = Regex.Match(norm, @"(?i:prevozni put)\s*:\s*(.+?)(?:\.\s+(?=[A-Z])|\.$|$)", O);
            p.PrugaTekst = put.Success ? put.Groups[1].Value.Trim() : null;

            var vreme = VremenskiIzraz.Parsiraj(f, p.Datum ?? DateTime.Today);
            var od = (p.Datum ?? DateTime.Today).Date;
            var kraj = vreme.VaziDoDatuma ?? new DateTime(od.Year, 12, 31);

            var opis = orig;
            var mo = Regex.Match(orig, @"(?i:opisa)\s*:\s*", O);
            if (mo.Success) opis = orig.Substring(mo.Index + mo.Length);
            var kp = opis.IndexOf("POSEBNI SIGURNOSNI", StringComparison.OrdinalIgnoreCase);
            if (kp > 0) opis = opis.Substring(0, kp);

            var st = new Stavka
            {
                Vrsta = Vrsta.NarocitaPosiljka,
                PrugaTekst = p.PrugaTekst,
                OdTekst = lok?.Item1,
                DoTekst = lok?.Item2,
                Kolosek = "pruga",
                VaziOd = od,
                VaziDo = kraj,
                Napomena = TekstUtil.SkratiNaRec(opis.Trim(), NapomenaMax),
                NarocitaPosiljka = true
            };
            if (lok == null) st.Upozorenja.Add("Nisu pronađene polazna i odredišna stanica.");
            p.Stavke.Add(st);
        }

        // ------------------------------------------------------------------ delovi

        /// <summary>Naziv pruge posle "Na pruzi" (bez dijakritika).</summary>
        internal static string Pruga(string norm)
        {
            var m = Regex.Match(norm, @"(?i:na pruzi)\s+(.+?)(?=\s*,\s*(?i:zbog|na zahtev|odobrava|u periodu|dana|sa vaz|po |radi|usled|planira|u stanici|u vremenu|od dana|u cilju)|\s*:\s|\s*:$|\s+(?i:zbog|odobrava|na zahtev|u periodu|predstoj\w*)\b)", O);
            if (!m.Success) return null;
            return m.Groups[1].Value.Trim().TrimEnd(',', ':', ' ');
        }

        /// <summary>(OdStanice, DoStanice) kao tekst; za stanicu obe vrednosti su iste. DoStanice "#GRANICA" = državna granica.</summary>
        /// <summary>
        /// Glavna relacija i dodatna stanica u istoj rečenici ("otvorena pruga Pančevo Glavna-Pančevo Varoš i stanica Pančevo Glavna"),
        /// koju je ručni unos upisivao kao poseban red (194F).
        /// </summary>
        internal static List<Tuple<string, string>> Lokacije2(string norm)
        {
            var rez = new List<Tuple<string, string>>();
            var glavna = Lokacija(norm, out int kraj);
            if (glavna == null) return rez;
            rez.Add(glavna);
            var bez = BezZagrada(norm);
            if (kraj > 0 && kraj < bez.Length)
            {
                var m = new Regex(@"\G\s*(?:,|\bi\b)\s+(?:u\s+)?(?i:stanic[aie]|st\.|sl\.\s*mest[oua])\s*(?<od>" + Ime + ")", O).Match(bez, kraj);
                if (m.Success)
                {
                    var st = Ocisti(m.Groups["od"].Value);
                    var t = Tuple.Create(st, st);
                    if (!rez.Contains(t)) rez.Add(t);
                }
            }
            return rez;
        }

        internal static Tuple<string, string> Lokacija(string norm)
        {
            return Lokacija(norm, out _);
        }

        private static Tuple<string, string> Lokacija(string norm, out int kraj)
        {
            kraj = -1;
            // Relacija zatvora je prva relacija posle reči "zatvor" / "zabranjuje"; druge relacije u tački
            // ("na delu pruge ... radi posluživanja", "bez napona ...") nisu mesto zatvora.
            norm = BezZagrada(norm);
            var anchor = Regex.Match(norm, @"(?i:zatvor|zabranjuje)", O);
            var kandidati = new List<Tuple<int, int, Match>>();
            for (int i = 0; i < Lokacije.Length; i++)
                foreach (Match m in Lokacije[i].Matches(norm))
                    if (m.Groups["od"].Success && m.Groups["od"].Length > 0)
                        kandidati.Add(Tuple.Create(m.Index, i, m));
            if (kandidati.Count == 0) return null;

            Match izbor;
            var posle = anchor.Success ? kandidati.Where(k => k.Item1 >= anchor.Index).ToList() : new List<Tuple<int, int, Match>>();
            if (posle.Count > 0) izbor = posle.OrderBy(k => k.Item1).ThenBy(k => k.Item2).First().Item3;
            else izbor = kandidati.OrderBy(k => k.Item2).ThenBy(k => k.Item1).First().Item3;

            kraj = izbor.Index + izbor.Length;
            var od = Ocisti(izbor.Groups["od"].Value);
            string @do;
            if (izbor.Groups["gr"].Success) @do = "#GRANICA";
            else if (izbor.Groups["do"].Success) @do = Ocisti(izbor.Groups["do"].Captures[izbor.Groups["do"].Captures.Count - 1].Value);
            else @do = od;
            return Tuple.Create(od, @do);
        }

        /// <summary>Uklanja objašnjenja u zagradama (i nezatvorenu zagradu do kraja), da relacije iz njih ne bi bile izabrane.</summary>
        private static string BezZagrada(string s)
        {
            string pre;
            do { pre = s; s = Regex.Replace(s, @"\([^()]*\)", " ", O); } while (s != pre);
            var otvorena = Regex.Match(s, @"\([^()]{12,}$", O);
            if (otvorena.Success) s = s.Substring(0, otvorena.Index);
            return s;
        }

        private static string Ocisti(string ime)
        {
            ime = (ime ?? "").Trim().Trim('"', '\'', ',', ';', ':');
            ime = Regex.Replace(ime, @"\s*\.$", "");
            ime = Regex.Replace(ime, @"\s+", " ");
            return ime;
        }

        /// <summary>Kolosek kao u ručnom unosu: pruga / kolosek / levi / desni / svi / 1,2,5 / skretnica br.14.</summary>
        internal static string Kolosek(string f, Vrsta vrsta)
        {
            // Bira se prvi navod posle reči "zatvor" (269F: "zatvor levog koloseka ..., zatvor skretnica br. 2" → levi)
            var a = Regex.Match(f, @"zatvor|zabranjuje", O);
            var t = a.Success ? f.Substring(a.Index) : f;
            var kandidati = new List<Tuple<int, int, string>>();
            void Probaj(int prioritet, string rx, Func<Match, string> vrednost)
            {
                var m = Regex.Match(t, rx, O);
                if (!m.Success) return;
                var v = vrednost(m);
                if (!string.IsNullOrEmpty(v)) kandidati.Add(Tuple.Create(m.Index, prioritet, v));
            }
            string Brojevi(string s)
            {
                var b = Regex.Matches(s, @"\d+[a-z]?", O).Cast<Match>().Select(x => x.Value).ToList();
                return b.Count > 0 ? string.Join(",", b) : null;
            }

            Probaj(0, @"skretnic\w*\s*(?:br\.?|broj)\s*(\d+\w?)", m => "skretnica br." + m.Groups[1].Value);
            Probaj(1, @"zatvor\w*\s+(?:stanicnih\s+)?koloseka\s+br\.?\s*((?:\d+[a-z]?\s*\.?\s*(?:,|\bi\b)?\s*)+)", m => Brojevi(m.Groups[1].Value));
            Probaj(1, @"zatvor\w*\s+((?:\d+[a-z]?\s*\.?\s*(?:,|\bi\b)?\s*)+)\s*(?:stanicn\w*\s+)?kolosek", m => Brojevi(m.Groups[1].Value));
            Probaj(2, @"(?:od\s*)?(\d+)\s*\.?\s*(?:-|do)\s*(\d+)\s*\.?\s*kolosek", m => m.Groups[1].Value + "-" + m.Groups[2].Value);
            Probaj(3, @"svih\s+(?:stanicnih\s+)?kolosek|zatvor\w* rsvih", m => "svi");
            Probaj(4, @"(?:levog\s+i\s+desnog|desnog\s+i\s+levog)\s+kolosek", m => "levi i desni");
            Probaj(5, @"levog\s+kolosek", m => "levi");
            Probaj(5, @"desnog\s+kolosek", m => "desni");
            Probaj(6, @"zatvor\w*\s+pruge|zatvori\s+pruga", m => "pruga");
            Probaj(7, @"zatvor\w*\s+kolosek|zatvori\s+kolosek", m => "kolosek");

            if (kandidati.Count == 0) return "pruga";
            return kandidati.OrderBy(k => k.Item1).ThenBy(k => k.Item2).First().Item3;
        }

        /// <summary>Tekst tačke za kolonu Napomena (max 250, skraćen na reč).</summary>
        internal static string Napomena(string orig, string fold, bool imaTacku)
        {
            var tekst = orig;
            if (!imaTacku)
            {
                // Bez tačaka: od "odobrava ... / dana ... / zabranjuje ...", posle naziva pruge
                var poPruzi = 0;
                var mp = Regex.Match(fold, @"na pruzi\s+.+?(?=,\s|:\s|\s(?:zbog|odobrava))", O);
                if (mp.Success) poPruzi = mp.Index + mp.Length;
                var mk = Regex.Match(fold.Substring(poPruzi), @"\b(?:odobrava|dana\s+\d|zabranjuje|u periodu|od dana)", O);
                int idx = mk.Success ? poPruzi + mk.Index : poPruzi;
                tekst = idx < orig.Length && orig.Length == fold.Length ? orig.Substring(idx) : (idx < fold.Length ? fold.Substring(idx) : orig);
            }
            return TekstUtil.SkratiNaRec(tekst.Trim(), NapomenaMax);
        }

        // ------------------------------------------------------------------ pomoćno

        /// <summary>Spaja linije u jedan red; reč podeljena crticom na kraju reda se spaja.</summary>
        private static string Spoji(IEnumerable<string> linije)
        {
            var s = string.Join(" ", linije.Select(l => l.Trim()));
            s = Regex.Replace(s, @"(\p{Ll})- (\p{Ll})", "$1$2", O);
            return Regex.Replace(s, @"\s+", " ", O).Trim();
        }

        private static DateTime? D(Match m) { return D(m.Groups[1], m.Groups[2], m.Groups[3]); }

        private static DateTime? D(Group d, Group m, Group g)
        {
            int dd = int.Parse(d.Value, Inv), mm = int.Parse(m.Value, Inv), gg = int.Parse(g.Value, Inv);
            if (mm < 1 || mm > 12 || dd < 1 || dd > 31 || gg < 2000) return null;
            if (dd > DateTime.DaysInMonth(gg, mm)) return null;
            return new DateTime(gg, mm, dd);
        }

        private static string F(DateTime? d) { return d?.ToString("dd.MM.yyyy") ?? "-"; }
    }
}
