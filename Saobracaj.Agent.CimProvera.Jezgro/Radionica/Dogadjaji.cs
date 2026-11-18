using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>
    /// v3.0 (2.10, faza 1): događaj iz teksta maila – ETA, ETD, dolazak, polazak, kašnjenje, otkačena kola, broj kola, ID najave, broj voza,
    /// MRN, ostalo važno. Upisuje se u planiranje.tmpJobsDogadjaj (OP_918), u Napomenu jedan red i u istoriju; polja posla se NE menjaju.
    /// </summary>
    internal sealed class Dogadjaj
    {
        public string Tip = "";
        public int? MestoID;
        public string MestoTekst = "";
        /// <summary>Vreme događaja (sa zonom); null kada je u rečenici samo datum bez vremena ili nijedno.</summary>
        public DateTimeOffset? Vreme;
        /// <summary>Vreme kako piše u rečenici („18/morning“, „17.11. 12:00“, „sutra ujutru“).</summary>
        public string VremeTekst = "";
        /// <summary>„CET“, „EET“, „TRT“, „UTC“ – izričito u tekstu ili po pošiljaocu/jeziku.</summary>
        public string VremenskaZona = "";
        public string Vrednost = "";
        public string Recenica = "";
        public string Jezik = "";
        public int Pouzdanost;
        /// <summary>Izraz koji je prepoznat (za rečnik izraza po jezicima).</summary>
        public string Izraz = "";
        /// <summary>v3.0: BROJ_KOLA je raspon ili razlomak („4,5 vagona“, „3-4 wagons“) – vrednost je tekst, nije broj kola posla.</summary>
        public bool Raspon;
        /// <summary>v3.0.1 (5): posao za koji je događaj vezan („512“; više poslova maila – „512, 517“); prazno = nevezan.</summary>
        public string Posao = "";
        /// <summary>v3.0.1 (5): kako je vezan („posao maila“, „nit (In-Reply-To/References)“, „ID najave 1657“, „broj voza 268F“, „relacija + datum“).</summary>
        public string Veza = "";
        /// <summary>v3.0.1 (5): nevezan događaj – kandidati („posao 517: ista nit; posao 521: relacija“), za ručnu odluku.</summary>
        public List<string> Kandidati = new List<string>();
        public override string ToString() => Tip + " " + (VremeTekst.Length > 0 ? VremeTekst + " " : "") + (MestoTekst.Length > 0 ? "@" + MestoTekst + " " : "") + Vrednost + " [" + Jezik + ", " + Pouzdanost + "]";
    }

    internal static class IzvlacenjeDogadjaja
    {
        public static readonly string[] Tipovi = { "ETA", "ETD", "DOLAZAK", "POLAZAK", "KASNJENJE", "OTKACENA_KOLA", "BROJ_KOLA", "ID_NAJAVE", "BROJ_VOZA", "MRN", "OSTALO_VAZNO" };

        /// <summary>Početni rečnik izraza (sr, en, de, hu, ro, bg, tr) po tipu događaja – proširuje se iz arhive (rečnik sa brojem pojavljivanja).</summary>
        internal static readonly (string tip, string jezik, string izraz)[] Recnik =
        {
            ("ETA", "en", @"\bETA\b"), ("ETA", "en", @"expected (time of )?arrival"), ("ETA", "en", @"estimated (time of )?arrival"), ("ETA", "en", @"will arrive"), ("ETA", "en", @"arriving"),
            ("ETA", "sr", @"o[čc]ekivan[io]? dolazak"), ("ETA", "sr", @"o[čc]ekuje se (dolazak|da stigne)"), ("ETA", "sr", @"sti[žz]e"), ("ETA", "sr", @"trebalo bi da stigne"),
            ("ETA", "de", @"voraussichtliche? Ankunft"), ("ETA", "de", @"Ankunft voraussichtlich"), ("ETA", "de", @"wird .{0,30}ankommen"),
            ("ETA", "hu", @"v[áa]rhat[óo] [ée]rkez[ée]s"), ("ETA", "hu", @"[ée]rkezik"),
            ("ETA", "ro", @"sosire estimat[ăa]"), ("ETA", "ro", @"ora estimat[ăa] de sosire"), ("ETA", "ro", @"va sosi"),
            ("ETA", "bg", @"очаквано пристигане"), ("ETA", "bg", @"ще пристигне"),
            ("ETA", "tr", @"tahmini var[ıi][şs]"), ("ETA", "tr", @"varacak"),
            ("ETD", "en", @"\bETD\b"), ("ETD", "en", @"expected (time of )?departure"), ("ETD", "en", @"will (depart|leave)"),
            ("ETD", "sr", @"o[čc]ekivan[io]? polazak"), ("ETD", "sr", @"kre[ćc]e"), ("ETD", "sr", @"polazi"),
            ("ETD", "de", @"voraussichtliche? Abfahrt"), ("ETD", "de", @"f[äa]hrt .{0,20}ab"),
            ("ETD", "hu", @"v[áa]rhat[óo] indul[áa]s"), ("ETD", "hu", @"indul\b"),
            ("ETD", "ro", @"plecare estimat[ăa]"), ("ETD", "ro", @"va pleca"),
            ("ETD", "bg", @"очаквано заминаване"), ("ETD", "bg", @"ще тръгне"),
            ("ETD", "tr", @"tahmini kalk[ıi][şs]"), ("ETD", "tr", @"hareket edecek"),
            ("DOLAZAK", "en", @"(has |have )?arrived"), ("DOLAZAK", "en", @"\barrival\b"),
            ("DOLAZAK", "sr", @"\bsti(gao|gla|glo|gli)\b"), ("DOLAZAK", "sr", @"\bdolazak\b"), ("DOLAZAK", "sr", @"prispe(o|la|lo|li)"),
            ("DOLAZAK", "de", @"\bangekommen\b"), ("DOLAZAK", "de", @"\bAnkunft\b"), ("DOLAZAK", "de", @"eingetroffen"),
            ("DOLAZAK", "hu", @"meg[ée]rkezett"), ("DOLAZAK", "hu", @"\b[ée]rkez[ée]s\b"),
            ("DOLAZAK", "ro", @"a sosit"), ("DOLAZAK", "ro", @"\bsosire\b"),
            ("DOLAZAK", "bg", @"пристигна"), ("DOLAZAK", "bg", @"пристигане"),
            ("DOLAZAK", "tr", @"\bgeldi\b"), ("DOLAZAK", "tr", @"var[ıi][şs]\b"), ("DOLAZAK", "tr", @"ula[şs]t[ıi]"),
            ("POLAZAK", "en", @"\bdeparted\b"), ("POLAZAK", "en", @"\bleft\b"), ("POLAZAK", "en", @"\bdeparture\b"), ("POLAZAK", "en", @"dispatched"),
            ("POLAZAK", "sr", @"\bkrenu(o|la|lo|li)\b"), ("POLAZAK", "sr", @"oti[šs]a?(o|la|lo|li)\b"), ("POLAZAK", "sr", @"otpremljen[aio]?\b"), ("POLAZAK", "sr", @"\bpolazak\b"),
            ("POLAZAK", "de", @"abgefahren"), ("POLAZAK", "de", @"\bAbfahrt\b"), ("POLAZAK", "de", @"abgeschickt"),
            ("POLAZAK", "hu", @"elindult"), ("POLAZAK", "hu", @"\bindul[áa]s\b"),
            ("POLAZAK", "ro", @"a plecat"), ("POLAZAK", "ro", @"\bplecare\b"),
            ("POLAZAK", "bg", @"тръгна"), ("POLAZAK", "bg", @"замина"),
            ("POLAZAK", "tr", @"hareket etti"), ("POLAZAK", "tr", @"\bkalk[ıi][şs]\b"), ("POLAZAK", "tr", @"yola [çc][ıi]kt[ıi]"),
            ("KASNJENJE", "en", @"\bdelay(ed)?\b"), ("KASNJENJE", "en", @"\blate\b"),
            ("KASNJENJE", "sr", @"ka[šs]njenje"), ("KASNJENJE", "sr", @"\bkasni\b"), ("KASNJENJE", "sr", @"zaka[šs]n(io|ela|elo|eli)"),
            ("KASNJENJE", "de", @"Versp[äa]tung"), ("KASNJENJE", "de", @"versp[äa]tet"),
            ("KASNJENJE", "hu", @"k[ée]s[ée]s"), ("KASNJENJE", "hu", @"k[ée]sik"),
            ("KASNJENJE", "ro", @"[îi]nt[âa]rziere"), ("KASNJENJE", "ro", @"[îi]nt[âa]rzie"),
            ("KASNJENJE", "bg", @"закъснение"), ("KASNJENJE", "bg", @"закъснява"),
            ("KASNJENJE", "tr", @"gecikme"), ("KASNJENJE", "tr", @"gecikti"),
            ("OTKACENA_KOLA", "en", @"(wagon|car)s? .{0,40}(detached|uncoupled|set out|removed from the train)"), ("OTKACENA_KOLA", "en", @"\b(detached|uncoupled)\b"),
            ("OTKACENA_KOLA", "sr", @"otka[čc]en[aio]?\b"), ("OTKACENA_KOLA", "sr", @"isklju[čc]en[aio]? iz (voza|sastava)"), ("OTKACENA_KOLA", "sr", @"izba[čc]en[aio]? iz (voza|sastava)"),
            ("OTKACENA_KOLA", "de", @"ausgesetzt"), ("OTKACENA_KOLA", "de", @"abgeh[äa]ngt"), ("OTKACENA_KOLA", "de", @"ausgereiht"),
            ("OTKACENA_KOLA", "hu", @"kisorozva"), ("OTKACENA_KOLA", "hu", @"lekapcsol"),
            ("OTKACENA_KOLA", "ro", @"deta[șs]at"), ("OTKACENA_KOLA", "ro", @"decuplat"),
            ("OTKACENA_KOLA", "bg", @"откачен"), ("OTKACENA_KOLA", "bg", @"извадени от влака"),
            ("OTKACENA_KOLA", "tr", @"ayr[ıi]ld[ıi]"), ("OTKACENA_KOLA", "tr", @"[çc][ıi]kar[ıi]ld[ıi]"),
            ("OSTALO_VAZNO", "en", @"\burgent\b"), ("OSTALO_VAZNO", "en", @"\bcancel(l?ed)?\b"), ("OSTALO_VAZNO", "en", @"\bdamaged?\b"),
            ("OSTALO_VAZNO", "sr", @"\bhitno\b"), ("OSTALO_VAZNO", "sr", @"\botkaz(ana|an|ivanje)?\b"), ("OSTALO_VAZNO", "sr", @"\bo[šs]te[ćc]en[aio]?\b"), ("OSTALO_VAZNO", "sr", @"\bstorn"),
            ("OSTALO_VAZNO", "de", @"\bdringend\b"), ("OSTALO_VAZNO", "de", @"\bstorniert\b"), ("OSTALO_VAZNO", "de", @"besch[äa]digt"),
            ("OSTALO_VAZNO", "hu", @"s[üu]rg[őo]s"), ("OSTALO_VAZNO", "hu", @"t[öo]r[öo]lve"), ("OSTALO_VAZNO", "hu", @"s[ée]r[üu]lt"),
            ("OSTALO_VAZNO", "ro", @"\burgent\b"), ("OSTALO_VAZNO", "ro", @"anulat"), ("OSTALO_VAZNO", "ro", @"deteriorat"),
            ("OSTALO_VAZNO", "bg", @"спешно"), ("OSTALO_VAZNO", "bg", @"отменен"),
            ("OSTALO_VAZNO", "tr", @"\bacil\b"), ("OSTALO_VAZNO", "tr", @"iptal"), ("OSTALO_VAZNO", "tr", @"hasarl[ıi]"),
        };

        private static readonly Regex Mrn = new Regex(@"(?<![A-Z0-9])(\d{2}[A-Z]{2}[A-Z0-9]{12}[0-9A-Z]{2})(?![A-Z0-9])", RegexOptions.Compiled);
        // v3.0 (odluka 18.11.): „4,5 vagona“ / „3-4 wagons“ je raspon ili razlomak – broj kola nije siguran (vrednost kao tekst, niska pouzdanost)
        private static readonly Regex BrojKola = new Regex(@"(?i)(?<![\d,.])(\d{1,3}(?:\s*[,.]\s*\d{1,2}|\s*[-–]\s*\d{1,3})?)\s*(?:x\s*)?(?:wagons?|kola\b|vagon[aie]?\b|vagoane|vagonlar?|Wagen\b|kocsi\b|kocsik|вагона?|wgs?\.?)(?!\w)", RegexOptions.Compiled);
        // v3.0 (odluka 18.11.): oznaka voza („HER-SAD 81“, „SAD-HER 79“, „LDC8“) – njen broj nikad nije kašnjenje
        private static readonly Regex OznakaVozaSaBrojem = new Regex(@"\b[A-Z]{2,6}[-_ ][A-Z]{2,6}\s*\d{1,4}\b|\b[A-Z]{2,6}\d{1,4}\b", RegexOptions.Compiled);
        // v3.0 (odluka 18.11.): ugrađene slike („[cid:image001.png@01DC…]“, „image002.jpg“, „[image: logo.png]“) se izbacuju pre analize
        private static readonly Regex Slike = new Regex(@"(?i)\[\s*(?:cid|image)\s*:[^\]]*\]|\bcid:\S+|\[image\d*\]|\b[\w\-.]{1,60}\.(?:png|jpe?g|gif|bmp|tiff?)(?:@[\w.]+)?\b", RegexOptions.Compiled);
        // v3.0 (odluka 18.11.): disclaimer iz potpisa (poverljivost, pogrešan primalac) nije OSTALO_VAZNO – rečenica se ne analizira
        private static readonly Regex Disclaimer = new Regex(@"(?i)\b(confidential|intended (solely )?for|intended recipient|disclaimer|if you (have )?received this|privileged|do not (copy|disclose|distribute)|"
            + @"c[íi]mzett|bizalmas|amennyiben ezt a lev[ée]l|t[öo]r[öo]lje|vertraulich|Empf[äa]nger|irrt[üu]mlich|poverljiv|namenjen[ao]? isklju[čc]ivo|ovla[šs][ćc]en[io]? primal|confiden[țt]ial|destinatar)", RegexOptions.Compiled);
        // v3.0 (odluka korisnika 18.11.): ID najave samo iz oblika „ID nnnn“ – „Aviso: 46836/15“ je broj avize/voza, ne ID najave
        private static readonly Regex IdNajave = new Regex(@"(?<![\p{L}\d])ID\s*[:#.]?\s*(\d{3,5})(?![\d/])", RegexOptions.Compiled);
        private static readonly Regex BrojVoza = new Regex(@"(?i)(?:\b(?:train|voz|vlak|Zug|tren|vonat|влак)\s*(?:no\.?|nr\.?|br\.?|broj|number|n°|#)?\s*[:.]?\s*(\d{2,6}[A-Z]?)(?![\d])|(?<![\p{L}\d])([A-Z]{2,6}\d{1,4}[_\-][A-Z0-9]{2,10})(?![\p{L}\d]))", RegexOptions.Compiled);

        /// <summary>
        /// Događaji iz novog dela poruke (naslov + telo bez citata i potpisa). <paramref name="domen"/> – domen pošiljaoca (vremenska zona
        /// kada nije izričito navedena); <paramref name="stanica"/> – opciono: naziv mesta → ID stanice iz šifarnika.
        /// </summary>
        public static List<Dogadjaj> Izvuci(string naslov, string telo, DateTimeOffset datumMaila, string domen = "", Func<string, int?> stanica = null)
        {
            var nov = Slike.Replace(Obrada.TekstPosiljaoca(naslov ?? "", telo ?? ""), " ");
            var rez = new List<Dogadjaj>();
            var jezikMaila = Jezik(nov, domen);
            foreach (var rec in Recenice(nov))
            {
                if (Disclaimer.IsMatch(rec)) continue;
                // kratka rečenica („MRN 26RS…“, „ETA Subotica 18/morning“) – jezik celog novog dela poruke
                var jezik = Regex.Matches(rec, @"\p{L}{3,}").Count < 4 ? jezikMaila : Jezik(rec, domen);
                var zona = Zona(rec, domen, jezik);
                var (vreme, vremeTekst) = Vreme(rec, datumMaila, zona);
                var mesto = Mesto(rec);
                int? mestoId = null;
                if (mesto.Length > 0 && stanica != null) try { mestoId = stanica(mesto); } catch { }
                var nadjeni = new HashSet<string>();
                foreach (var (tip, jez, izraz) in Recnik)
                {
                    if (nadjeni.Contains(tip)) continue;
                    var m = Regex.Match(rec, izraz, RegexOptions.IgnoreCase);
                    if (!m.Success) continue;
                    // vremenski tipovi bez vremena ili mesta su slabi (pouzdanost niža); „ETA/ETD“ bez vremena se ipak beleži
                    var vremenski = tip == "ETA" || tip == "ETD" || tip == "DOLAZAK" || tip == "POLAZAK";
                    if (vremenski && vremeTekst.Length == 0 && mesto.Length == 0 && tip != "ETA" && tip != "ETD") continue;
                    nadjeni.Add(tip);
                    rez.Add(new Dogadjaj
                    {
                        Tip = tip, Jezik = jezik, Izraz = m.Value.Trim(), Recenica = Skrati(rec, 500), MestoTekst = mesto, MestoID = mestoId,
                        Vreme = vremenski ? vreme : null, VremeTekst = vremenski || tip == "KASNJENJE" ? vremeTekst : "", VremenskaZona = vremenski && vremeTekst.Length > 0 ? zona : "",
                        Vrednost = tip == "KASNJENJE" ? Kasnjenje(rec) : "",
                        Pouzdanost = Pouzdanost(tip, jez == jezik, vremeTekst.Length > 0, mesto.Length > 0),
                    });
                }
                foreach (Match m in BrojKola.Matches(rec))
                {
                    var izrazKola = Regex.Replace(m.Value, @"[\d,.\-–]+\s*", "").Trim();
                    if (Regex.IsMatch(m.Groups[1].Value, @"[,.\-–]"))
                    { rez.Add(new Dogadjaj { Tip = "BROJ_KOLA", Vrednost = Regex.Replace(m.Groups[1].Value, @"\s+", ""), Jezik = jezik, Izraz = izrazKola, Recenica = Skrati(rec, 500), MestoTekst = mesto, MestoID = mestoId, Pouzdanost = 40, Raspon = true }); break; }
                    if (int.TryParse(m.Groups[1].Value, out var n) && n > 0 && n <= 120)
                    { rez.Add(new Dogadjaj { Tip = "BROJ_KOLA", Vrednost = n.ToString(), Jezik = jezik, Izraz = izrazKola, Recenica = Skrati(rec, 500), MestoTekst = mesto, MestoID = mestoId, Pouzdanost = 80 }); break; }
                }
                foreach (Match m in IdNajave.Matches(rec))
                    rez.Add(new Dogadjaj { Tip = "ID_NAJAVE", Vrednost = m.Groups[1].Value, Jezik = jezik, Izraz = Regex.Replace(m.Value, @"\d+", "").Trim(' ', ':', '.', '#'), Recenica = Skrati(rec, 500), Pouzdanost = 85 });
                foreach (Match m in BrojVoza.Matches(rec))
                {
                    var v = m.Groups[1].Success && m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
                    if (v.Length > 0 && !rez.Any(x => x.Tip == "BROJ_VOZA" && x.Vrednost == v))
                        rez.Add(new Dogadjaj { Tip = "BROJ_VOZA", Vrednost = v, Jezik = jezik, Izraz = m.Groups[1].Success && m.Groups[1].Value.Length > 0 ? Regex.Replace(m.Value, @"\d.*$", "").Trim() : "oznaka voza", Recenica = Skrati(rec, 500), Pouzdanost = m.Groups[1].Success && m.Groups[1].Value.Length > 0 ? 85 : 75 });
                }
                foreach (Match m in Mrn.Matches(rec.ToUpperInvariant()))
                    if (Regex.IsMatch(m.Value, @"\d{2}[A-Z]{2}") && Regex.Matches(m.Value, @"\d").Count >= 4)
                        rez.Add(new Dogadjaj { Tip = "MRN", Vrednost = m.Value, Jezik = jezik, Izraz = "MRN", Recenica = Skrati(rec, 500), Pouzdanost = 90 });
            }
            // isti tip + ista vrednost + isto vreme iz više rečenica → jednom (prvi)
            return rez.GroupBy(d => d.Tip + "|" + d.Vrednost + "|" + d.VremeTekst + "|" + d.MestoTekst).Select(g => g.First()).ToList();
        }

        /// <summary>v3.0: vrednost kašnjenja („3 sata“, „24 delay“) – broj iz oznake voza („HER-SAD 81“) nikad; jedinica kao cela reč.</summary>
        internal static string Kasnjenje(string rec)
        {
            var t = OznakaVozaSaBrojem.Replace(rec ?? "", " ");
            var m = Regex.Match(t, @"(?i)(?<![\d.,/:])(\d{1,3})\s*(h|sat[ai]?|hours?|Stunden?|óra|ore|часа|saat|min(?:utes?|uta)?)\b");
            if (m.Success) return m.Value.Trim();
            m = Regex.Match(t, @"(?i)(?<![\d.,/:])(\d{1,3})\s*(?:\w+\s+)?(delay|ka[šs]njenj\w*|Versp[äa]tung|k[ée]s[ée]s|[îi]nt[âa]rziere|gecikme)");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static int Pouzdanost(string tip, bool istiJezik, bool imaVreme, bool imaMesto)
        {
            var p = 55;
            if (istiJezik) p += 10;
            if (imaVreme) p += 15;
            if (imaMesto) p += 10;
            if (tip == "ETA" || tip == "ETD") p += 5;
            return Math.Min(95, p);
        }

        private static string Skrati(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        /// <summary>Rečenice: redovi, pa unutar reda po „. “ / „! “ / „? “ ispred velikog slova; bez praznih i bez redova samo sa potpisom/telefonom.</summary>
        internal static IEnumerable<string> Recenice(string tekst)
        {
            foreach (var red in (tekst ?? "").Replace("\r", "").Split('\n'))
            {
                var r = Regex.Replace(red, @"\s+", " ").Trim();
                if (r.Length < 3) continue;
                foreach (var d in Regex.Split(r, @"(?<=[.!?])\s+(?=[\p{Lu}\d])"))
                    if (d.Trim().Length >= 3) yield return d.Trim();
            }
        }

        /// <summary>Jezik rečenice (sr, en, de, hu, ro, bg, tr) – po pismu, dijakriticima i rečima.</summary>
        internal static string Jezik(string t, string domen = null)
        {
            if (Regex.IsMatch(t, @"[Ѐ-ӿ]")) return Regex.IsMatch(t, @"[ђћџљњЂЋЏЉЊ]|\b(вагона|воз|станица|у прилогу)\b", RegexOptions.IgnoreCase) ? "sr" : "bg";
            // v3.0 (odluka 18.11.): turski samo po turskim slovima ili rečima; „vagon“ / „tren“ su i srpski / rumunski. Srpski bez dijakritika
            // („Zavrsio kao voz 45660“, „29 vagona“) nije tr; pošiljalac .rs ili KP daje prednost sr
            var srpskiPosiljalac = Regex.IsMatch(domen ?? "", @"(?i)(\.rs$|kprevoz)");
            var turskaSlova = Regex.IsMatch(t, @"[ığşİĞŞ]");
            var turski = turskaSlova || Regex.IsMatch(t, @"(?i)\b(ve|bir|için|vagonlar[ıi]?|treni|saygılarımla|tarihinde)\b");
            if (turski && !(srpskiPosiljalac && !turskaSlova)) return "tr";
            if (srpskiPosiljalac && Regex.IsMatch(t, @"(?i)\b(voz|vagona?|kola|zavr[šs]i[oa]|zavr[šs]en|sati?|danas|sutra|stigao|stigla|prispel[ao]|utovar|istovar|kao|za|od|preostal\w*)\b")
                && !Regex.IsMatch(t, @"(?i)\b(the|and|is|are|we|you|please|train|wagons?|arrival|der|die|und)\b")) return "sr";
            if (Regex.IsMatch(t, @"[őűŐŰ]") || Regex.IsMatch(t, @"(?i)\b(és|vonat|kocsi|érkez|indul|üdvözlettel|holnap)\b")) return "hu";
            if (Regex.IsMatch(t, @"[șțăâîȘȚĂÂÎ]") || Regex.IsMatch(t, @"(?i)\b(și|trenul|vagoane|sosire|plecare|mâine|azi)\b")) return "ro";
            if (Regex.IsMatch(t, @"[ßäöü]") && Regex.IsMatch(t, @"(?i)\b(der|die|das|und|Zug|Wagen|Ankunft|morgen|heute)\b") || Regex.IsMatch(t, @"(?i)\b(Ankunft|Abfahrt|Wagen|Zug|Verspätung)\b")) return "de";
            if (Regex.IsMatch(t, @"[čćšžđČĆŠŽĐ]") || Obrada.JeSrpskiTekst(t)) return "sr";
            return "en";
        }

        /// <summary>
        /// Vremenska zona: izričito u tekstu (CET/CEST/EET/EEST/UTC/GMT/TRT/„lokalno“), inače po domenu pošiljaoca (.ro .bg .tr .ua) ili jeziku
        /// (ro, bg, tr) – RO, BG, UA = EET (UTC+2/+3), TR = TRT (UTC+3), ostalo CET (UTC+1/+2).
        /// </summary>
        internal static string Zona(string t, string domen, string jezik)
        {
            var m = Regex.Match(t, @"\b(CEST|CET|EEST|EET|UTC|GMT|TRT|MSK)\b");
            if (m.Success) return m.Value == "GMT" ? "UTC" : m.Value.Replace("CEST", "CET").Replace("EEST", "EET");
            var d = (domen ?? "").ToLowerInvariant();
            if (Regex.IsMatch(d, @"\.(tr)$") || jezik == "tr") return "TRT";
            if (Regex.IsMatch(d, @"\.(ro|bg|ua|md|gr)$") || jezik == "ro" || jezik == "bg") return "EET";
            return "CET";
        }

        /// <summary>Pomak zone u datom trenutku (letnje računanje: poslednja nedelja marta – poslednja nedelja oktobra; TR uvek +3).</summary>
        internal static TimeSpan Pomak(string zona, DateTime lokalno)
        {
            bool Leto()
            {
                DateTime Poslednja(int mesec) { var d = new DateTime(lokalno.Year, mesec, DateTime.DaysInMonth(lokalno.Year, mesec)); while (d.DayOfWeek != DayOfWeek.Sunday) d = d.AddDays(-1); return d; }
                return lokalno >= Poslednja(3) && lokalno < Poslednja(10);
            }
            switch (zona)
            {
                case "UTC": return TimeSpan.Zero;
                case "TRT": case "MSK": return TimeSpan.FromHours(3);
                case "EET": return TimeSpan.FromHours(Leto() ? 3 : 2);
                default: return TimeSpan.FromHours(Leto() ? 2 : 1);
            }
        }

        private static readonly (string rec, int dana)[] Relativni =
        {
            ("danas", 0), ("today", 0), ("heute", 0), ("ma", 0), ("azi", 0), ("днес", 0), ("bugün", 0), ("tonight", 0), ("večeras", 0), ("vecheras", 0),
            ("sutra", 1), ("tomorrow", 1), ("morgen", 1), ("holnap", 1), ("mâine", 1), ("maine", 1), ("утре", 1), ("yarın", 1), ("yarin", 1),
            ("prekosutra", 2), ("übermorgen", 2), ("holnapután", 2), ("poimâine", 2), ("вдругиден", 2),
            ("juče", -1), ("juce", -1), ("yesterday", -1), ("gestern", -1), ("tegnap", -1), ("ieri", -1), ("вчера", -1), ("dün", -1),
        };

        private static readonly (string rec, int sat)[] DelDana =
        {
            ("morning", 8), ("ujutru", 8), ("ujutro", 8), ("jutro", 8), ("vormittag", 9), ("früh", 7), ("reggel", 8), ("dimineața", 8), ("dimineata", 8), ("сутринта", 8), ("sabah", 8),
            ("noon", 12), ("podne", 12), ("mittag", 12), ("délben", 12), ("prânz", 12), ("обед", 12), ("öğle", 12),
            ("afternoon", 15), ("popodne", 15), ("nachmittag", 15), ("délután", 15), ("după-amiaza", 15), ("следобед", 15), ("öğleden sonra", 15),
            ("evening", 19), ("uveče", 19), ("uvece", 19), ("večeras", 19), ("abend", 19), ("este", 19), ("seara", 19), ("вечерта", 19), ("akşam", 19),
            ("night", 23), ("noću", 23), ("nacht", 23), ("éjjel", 23), ("noaptea", 23), ("нощта", 23), ("gece", 23),
        };

        /// <summary>
        /// Vreme iz rečenice: „17.11.2026 12:00“, „17.11. 12:00“, „2026-11-17 12:00“, „16.11 12.00“, „12:00h“, „12h“, „18/morning“,
        /// relativno („sutra ujutru“, „tomorrow 10:00“, „in 2 hours“, „za 3 sata“). Godina i mesec iz datuma maila kada nisu navedeni.
        /// Vraća vreme u zoni (sa pomakom) i tekst kako piše; (null, "") kada vremena nema.
        /// </summary>
        // v3.0 (c): nazivi meseci – tačni oblici (en, de, sr, hu, tr) i skraćenice; „Mail 15“ nije maj
        private static readonly string[][] Meseci =
        {
            new[] { "january", "jan", "januar", "januara", "jänner", "január", "ocak" },
            new[] { "february", "feb", "februar", "februara", "február", "şubat", "subat" },
            new[] { "march", "mar", "märz", "marz", "mart", "marta", "március", "marcius" },
            new[] { "april", "apr", "aprila", "április", "aprilis", "nisan" },
            new[] { "may", "mai", "maj", "maja", "május", "majus", "mayıs", "mayis" },
            new[] { "june", "jun", "juni", "juna", "június", "junius", "haziran" },
            new[] { "july", "jul", "juli", "jula", "július", "julius", "temmuz" },
            new[] { "august", "aug", "avgust", "avgusta", "augusztus", "ağustos", "agustos" },
            new[] { "september", "sep", "sept", "septembar", "septembra", "szeptember", "eylül", "eylul" },
            new[] { "october", "oct", "okt", "oktober", "oktobar", "oktobra", "október", "ekim" },
            new[] { "november", "nov", "novembar", "novembra", "kasım", "kasim" },
            new[] { "december", "dec", "dez", "dezember", "decembar", "decembra", "aralık", "aralik" },
        };
        private static readonly string MeseciRegex = "(?:" + string.Join("|", Meseci.SelectMany(x => x).Distinct().OrderByDescending(x => x.Length).Select(Regex.Escape)) + ")";
        private static int Mesec(string rec)
        {
            var r = (rec ?? "").Trim('.').ToLowerInvariant();
            for (int i = 0; i < Meseci.Length; i++) if (Meseci[i].Contains(r)) return i + 1;
            return 0;
        }

        internal static (DateTimeOffset? vreme, string tekst) Vreme(string t, DateTimeOffset datumMaila, string zona)
        {
            var baza = datumMaila == DateTimeOffset.MinValue ? DateTime.Now : datumMaila.LocalDateTime;
            int? dan = null, mesec = null, godina = null, sat = null, minut = null;
            var delovi = new List<string>();
            var m = Regex.Match(t, @"(?<!\d)(20\d{2})-(\d{1,2})-(\d{1,2})(?!\d)");
            // v3.0 (c): mađarski „2026.11.15.-én“ / „2026. 11. 15.“ (godina.mesec.dan., uz nastavak -án/-én/-i)
            Match mh = m.Success ? m : Regex.Match(t, @"(?<!\d)(20\d{2})\.\s*(\d{1,2})\.\s*(\d{1,2})\.?(?:\s*-\s*?\p{L}{1,3}\b)?");
            // v3.0 (c): naziv meseca – „15 November 2026“, „November 15, 2026“, „15. novembra“, „15. Nov.“
            Match mn = m.Success || mh.Success ? Match.Empty : Regex.Match(t, @"(?i)(?<![\p{L}\d])(\d{1,2})(?:st|nd|rd|th|\.)?\s+(" + MeseciRegex + @")\.?(?:,?\s+(20\d{2}))?(?!\p{L})");
            Match mn2 = m.Success || mh.Success || mn.Success ? Match.Empty : Regex.Match(t, @"(?i)(?<!\p{L})(" + MeseciRegex + @")\.?\s+(\d{1,2})(?:st|nd|rd|th)?(?:,?\s+(20\d{2}))?(?!\d)");
            if (m.Success) { godina = int.Parse(m.Groups[1].Value); mesec = int.Parse(m.Groups[2].Value); dan = int.Parse(m.Groups[3].Value); delovi.Add(m.Value); }
            else if (mh.Success && int.Parse(mh.Groups[2].Value) is int mh1 && mh1 >= 1 && mh1 <= 12 && int.Parse(mh.Groups[3].Value) is int dh && dh >= 1 && dh <= 31)
            { m = mh; godina = int.Parse(mh.Groups[1].Value); mesec = mh1; dan = dh; delovi.Add(mh.Value.Trim()); }
            else if (mn.Success && int.Parse(mn.Groups[1].Value) is int dn1 && dn1 >= 1 && dn1 <= 31)
            { m = mn; dan = dn1; mesec = Mesec(mn.Groups[2].Value); if (mn.Groups[3].Success) godina = int.Parse(mn.Groups[3].Value); delovi.Add(mn.Value.Trim()); }
            else if (mn2.Success && int.Parse(mn2.Groups[2].Value) is int dn2 && dn2 >= 1 && dn2 <= 31)
            { m = mn2; dan = dn2; mesec = Mesec(mn2.Groups[1].Value); if (mn2.Groups[3].Success) godina = int.Parse(mn2.Groups[3].Value); delovi.Add(mn2.Value.Trim()); }
            else
            {
                // v3.0 (odluka 18.11.): „17/12:30“ = dan 17. (tekući mesec) u 12:30 – broj ispred „hh:mm“ nikad nije mesec
                var dv = Regex.Match(t, @"(?<![\d./:])(\d{1,2})\s*/\s*([01]?\d|2[0-3]):([0-5]\d)(?!\d)");
                if (dv.Success && int.Parse(dv.Groups[1].Value) is int d0 && d0 >= 1 && d0 <= 31)
                {
                    dan = d0; sat = int.Parse(dv.Groups[2].Value); minut = int.Parse(dv.Groups[3].Value); delovi.Add(dv.Value.Trim());
                    m = Match.Empty;
                }
                else
                {
                    // v3.0 (odluka 18.11.): prvi VAŽEĆI datum – „12:00/15.11.2026“: „00/15“ (deo vremena) nije datum, datum je posle kose crte
                    foreach (Match k in Regex.Matches(t, @"(?<![\d.:])(\d{1,2})[./](\d{1,2})(?:[./](20\d{2}|\d{2}))?\.?(?![\d/]|:\d)"))
                    {
                        if (!(int.Parse(k.Groups[1].Value) is int d && int.Parse(k.Groups[2].Value) is int mm && d >= 1 && d <= 31 && mm >= 1 && mm <= 12)) continue;
                        if (Regex.IsMatch(t.Substring(k.Index + k.Length), @"^\s*(%|kg|t\b|m\b)")) continue;
                        m = k; dan = d; mesec = mm;
                        if (k.Groups[3].Success) godina = k.Groups[3].Value.Length == 2 ? 2000 + int.Parse(k.Groups[3].Value) : int.Parse(k.Groups[3].Value);
                        delovi.Add(k.Value.TrimEnd('.'));
                        break;
                    }
                }
            }
            // vreme: 12:00, 12.00 posle datuma, 12h, 12:00h, „at 14“
            var posleDatuma = m.Success ? t.Substring(m.Index + m.Length) : t;
            if (sat != null) posleDatuma = "";
            var v = Regex.Match(posleDatuma, @"(?<![\d.,])([01]?\d|2[0-3])[:h]([0-5]\d)\s*(h|č|ч|Uhr|óra)?(?![\d])", RegexOptions.IgnoreCase);
            if (!v.Success && m.Success) v = Regex.Match(posleDatuma, @"^\s*(?:u|at|um|в|saat|ora|la)?\s*([01]?\d|2[0-3])\.([0-5]\d)(?!\d)", RegexOptions.IgnoreCase);
            if (!v.Success) v = Regex.Match(t, @"(?<![\d.,]|\d:)([01]?\d|2[0-3])[:h]([0-5]\d)(?![\d])", RegexOptions.IgnoreCase);
            if (!v.Success) v = Regex.Match(t, @"(?<![\d.,:])([01]?\d|2[0-3])\s*(?:h|Uhr|óra|ч|часа)\b", RegexOptions.IgnoreCase);
            if (v.Success && sat == null) { sat = int.Parse(v.Groups[1].Value); minut = v.Groups.Count > 2 && v.Groups[2].Success && Regex.IsMatch(v.Groups[2].Value, @"^\d+$") ? int.Parse(v.Groups[2].Value) : 0; delovi.Add(v.Value.Trim()); }
            // „18/morning“, „18. ujutru“
            var dd = Regex.Match(t, @"(?<![\d./])(\d{1,2})\s*[/.]?\s*(" + string.Join("|", DelDana.Select(x => Regex.Escape(x.rec))) + @")\b", RegexOptions.IgnoreCase);
            if (dd.Success && dan == null && int.Parse(dd.Groups[1].Value) is int d2 && d2 >= 1 && d2 <= 31) { dan = d2; delovi.Add(dd.Value); }
            var deo = DelDana.FirstOrDefault(x => Regex.IsMatch(t, @"(?<!\p{L})" + Regex.Escape(x.rec) + @"(?!\p{L})", RegexOptions.IgnoreCase));
            if (sat == null && deo.rec != null) { sat = deo.sat; minut = 0; if (!delovi.Any(x => x.IndexOf(deo.rec, StringComparison.OrdinalIgnoreCase) >= 0)) delovi.Add(deo.rec); }
            var rel = Relativni.FirstOrDefault(x => Regex.IsMatch(t, @"(?<!\p{L})" + Regex.Escape(x.rec) + @"(?!\p{L})", RegexOptions.IgnoreCase));
            if (dan == null && rel.rec != null) { var d3 = baza.Date.AddDays(rel.dana); dan = d3.Day; mesec = d3.Month; godina = d3.Year; delovi.Insert(0, rel.rec); }
            var za = Regex.Match(t, @"(?i)\b(?:in|za|nach|în|след|sonra)\s+(\d{1,2})\s*(h|sat[ai]?|hours?|Stunden?|óra|ore|часа|saat)\b");
            if (dan == null && sat == null && za.Success) { var x = baza.AddHours(int.Parse(za.Groups[1].Value)); dan = x.Day; mesec = x.Month; godina = x.Year; sat = x.Hour; minut = x.Minute; delovi.Add(za.Value); }
            if (dan == null && sat == null) return (null, "");
            var g = godina ?? baza.Year; var mes = mesec ?? baza.Month; var dn = dan ?? baza.Day;
            // samo dan („18/morning“) – mesec maila; ako je dan pre datuma maila za više od 15 dana – sledeći mesec
            if (mesec == null && dan != null && dn < baza.Day - 15) { var s = new DateTime(baza.Year, baza.Month, 1).AddMonths(1); g = s.Year; mes = s.Month; }
            if (godina == null && mesec != null && new DateTime(g, mes, Math.Min(dn, DateTime.DaysInMonth(g, mes))) < baza.Date.AddMonths(-6)) g++;
            DateTime lok;
            try { lok = new DateTime(g, mes, Math.Min(dn, DateTime.DaysInMonth(g, mes)), sat ?? 0, minut ?? 0, 0); } catch { return (null, string.Join(" ", delovi)); }
            return (sat == null ? new DateTimeOffset(lok, Pomak(zona, lok)) : new DateTimeOffset(lok, Pomak(zona, lok)), string.Join(" ", delovi.Distinct()).Trim());
        }

        /// <summary>Mesto: reč(i) posle predloga (u/na/in/at/nach/bei/în/la/в/до) ili ispred „gr.“/„station“; inače poznata granična mesta.</summary>
        internal static string Mesto(string t)
        {
            // v3.0 (odluka 18.11.): „Arrival station of train: Prahovo“, „Stanica: Subotica“ – mesto je posle dvotačke
            var md = Regex.Match(t, @"(?i)\b(?:station|stanica|stanice|Bahnhof|állomás|gara|place|mesto)\b[^:\n]{0,40}:\s*(?<m>\p{L}[\p{L}\-]+(?:\s+(?:gr\.|granica|\p{Lu}[\p{L}\-]+))?)");
            if (md.Success && char.IsUpper(md.Groups["m"].Value[0])) return md.Groups["m"].Value.Trim();
            var m = Regex.Match(t, @"(?:\b(?:u|na|in|at|to|nach|bei|ab|în|la|в|до|on)\s+)(?<m>\p{Lu}[\p{L}\-]+(?:\s+(?:gr\.|granica|border|Grenze|\p{Lu}[\p{L}\-]+))?)");
            if (m.Success && !Regex.IsMatch(m.Groups["m"].Value, @"^(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|January|February|March|April|May|June|July|August|September|October|November|December|ETA|ETD|CET|UTC|The|We|I|KP|RS|SRB|ID)$")) return m.Groups["m"].Value.Trim();
            m = Regex.Match(t, @"(?<m>\b\p{Lu}[\p{L}\-]+)\s+(?:gr\.|granica|border|station|stanica|Bahnhof|állomás)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["m"].Value.Trim();
            m = Regex.Match(t, @"(?i)\b(Subotica|Kelebija|Kelebia|Šid|Sid|Tovarnik|Dimitrovgrad|Dragoman|Vršac|Vrsac|Stamora|Preševo|Presevo|Tabanovci|Zvornik|Brasina|Prijepolje|Bogojevo|Erdut|Constanta|Constanța|Kapikule|Svilengrad|Kalotina|Zmajevo|Prahovo|Bor|Vreoci|Novi Sad|Beograd|Ruma|Futog)\b");
            return m.Success ? m.Value : "";
        }

        /// <summary>Rečnik izraza po jeziku i tipu iz skupa mailova: (jezik, tip, izraz) → broj pojavljivanja (2.10 – početni rečnik iz arhive).</summary>
        public static List<(string jezik, string tip, string izraz, int broj)> RecnikIz(IEnumerable<Dogadjaj> dogadjaji) =>
            dogadjaji.GroupBy(d => (d.Jezik, d.Tip, izraz: d.Izraz.ToLowerInvariant())).Select(g => (g.Key.Jezik, g.Key.Tip, g.Key.izraz, g.Count()))
                .OrderBy(x => x.Item1, StringComparer.Ordinal).ThenBy(x => x.Item2, StringComparer.Ordinal).ThenByDescending(x => x.Item4).ToList();
    }
}
