using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// v2.8.1: čitanje obrasca CIT 7 – Naknadni zahtev (GLV-CIM, Prilog 7). Vrednosti se uzimaju iz zona između odštampanih natpisa
    /// (Pošiljalac, Primalac, Mesto izdavanja, Podaci o pošiljci, Preuzimanje, Kola, Zahtev, Napomene, Potpis) – natpisi na srpskom
    /// (ćirilica i latinica), nemačkom i francuskom. Kada strana ima upisana polja obrasca (AcroForm), vrednosti su samo ta polja;
    /// na skenu se izbacuju redovi natpisa. Tražene izmene: „Box 4: …“, „Rubrika 10: …“, „Feld 12: …“ u napomenama, ili tekst
    /// uz šifru 3 (novi primalac) / 4 (novo mesto izdavanja).
    /// </summary>
    internal static class Cit7Citac
    {
        private const double Sredina = 0.5;

        private static readonly Regex NatpisReci = new Regex(
            @"[Ѐ-ӿ]|^(Name|Adresse|Anschrift|Land|Absender|Empf[aä]nger|Bahnhof|Ort|Datum|Monat|Tag|Stunde|Wagen|Nr\.?|UTI|Frachtbrief|Wagenbrief|Unternehmen|Versand|Sendungs|Identifikation|[ÜU]bernahme|Ablieferungsort|Verf[uü]gung|Bemerkungen|Unterschrift|des|der|Bef[oö]rderers|Postadresse|Code|[ÄA]nderung|nom|adresse|exp[eé]diteur|destinataire|gare|lieu|date|signature|wagon)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Cit7Zahtev Procitaj(Strana s)
        {
            var z = new Cit7Zahtev();
            var reci = s?.Reci ?? new List<Rec>();
            if (reci.Count == 0) return z;
            var imaPolja = reci.Any(w => w.JePodatak);
            var natpisi = reci.Where(w => !w.JePodatak).ToList();
            List<Rec> Podaci(double x0, double x1, double y0, double y1) =>
                reci.Where(w => w.Cx >= x0 && w.Cx < x1 && w.Cy > y0 && w.Cy < y1 && (imaPolja ? w.JePodatak : !NatpisReci.IsMatch(w.T))).ToList();
            // dvojezični redovi natpisa (ćirilica + latinica u istom redu: „У мом присуству – in meiner Anwesenheit“) nisu podatak
            string Tekst(IEnumerable<Rec> r) => string.Join("\n", Raspored.Linije(r).Select(l => Regex.Replace(l.Tekst, @"\s+", " ").Trim(' ', '–', '-', ','))
                .Where(l => l.Length > 0 && !Regex.IsMatch(l, @"^[–\-.\s]+$") && !(Regex.IsMatch(l, @"[\u0400-\u04FF]{2}") && Regex.IsMatch(l, @"(?<![\u0400-\u04FF])[A-Za-z]{3,}"))));
            Rec Natpis(string obrazac, Func<Rec, bool> uslov = null) =>
                natpisi.Where(w => Regex.IsMatch(w.T, "^(" + obrazac + ")", RegexOptions.IgnoreCase) && (uslov == null || uslov(w))).OrderBy(w => w.Y0).FirstOrDefault();
            bool Levo(Rec w) => w.Cx < Sredina;
            bool Desno(Rec w) => w.Cx >= Sredina - 0.02;

            var pos = Natpis("Пошиљалац|Po[sš]iljalac|Absender|Exp[eé]diteur|Consignor", Levo);
            var prim = Natpis("Прималац|Primalac|Empf[aä]nger|Destinataire|Consignee", w => Levo(w) && (pos == null || w.Y0 > pos.Y0 + 0.01));
            var mesto = Natpis("издавања|izdavanja|isporuke|Ablieferungsort|livraison", w => Levo(w) && (prim == null || w.Y0 > prim.Y0 + 0.01));
            var adrPrev = Natpis("Поштанска|Po[sš]tanska|Postadresse|Adresse", w => Levo(w) && (mesto == null || w.Y0 > mesto.Y0 + 0.01));
            var podLab = Natpis("Подаци|Podaci|Sendungs|Identification", Desno);
            var preuz = Natpis("Преузимање|Preuzimanje|[ÜU]bernahme|Prise", Desno);
            var kola = Natpis("Кола|Kola|Wagen\b|Wagen$|Wagon", w => Desno(w) && preuz != null && w.Y0 > preuz.Y0 + 0.01);
            var zahtev = Natpis("Захтев|Zahtev|Verf[uü]gung|Ordre", w => w.Cx < 0.3 && adrPrev != null && w.Y0 > adrPrev.Y0 + 0.02);
            var nap = Natpis("Напомене|Napomene|Bemerkungen|Remarques", Desno);
            var dodatne = Natpis("Додатне|Dodatne|Erg[aä]nzende|Indications", Levo);
            var kraj = natpisi.Where(w => Regex.IsMatch(w.T, "^(Место|Mesto|Ort|Lieu)", RegexOptions.IgnoreCase) && w.Y0 > 0.6).OrderByDescending(w => w.Y0).FirstOrDefault();
            double Y0(Rec r, double d) => r?.Y0 ?? d;
            double Y1(Rec r, double d) => r?.Y1 ?? d;

            if (pos != null) z.Posiljalac = Tekst(Podaci(0, Sredina, Y1(pos, 0), Y0(prim, Y0(mesto, 0.25)))).Replace("\n", ", ");
            if (prim != null) z.Primalac = Tekst(Podaci(0, Sredina, Y1(prim, 0), Y0(mesto, Y0(adrPrev, 0.3)))).Replace("\n", ", ");
            if (mesto != null)
            {
                var linije = Tekst(Podaci(0, Sredina, Y1(mesto, 0), Y0(adrPrev, Y0(zahtev, 0.4)))).Split('\n').Where(x => x.Length > 0).ToList();
                var st = linije.FirstOrDefault(l => Regex.IsMatch(l, @"\b\d{2}\s?\d{5}-?\d\b"));
                z.StanicaIzdavanja = st ?? "";
                z.MestoIzdavanja = string.Join(", ", linije.Where(l => l != st));
            }

            // identifikacija pošiljke i vrsta lista (X uz „Tovarni list CIM“ / „Kolski list CUV“)
            if (podLab != null)
            {
                var zona = Podaci(Sredina - 0.02, 1, Y1(podLab, 0), Y0(preuz, 0.2));
                foreach (var x in zona.Where(JeOznaka))
                    if (NajblizaOznaci(reci, x, Sredina - 0.05, 1) is string lab && Regex.IsMatch(lab, "CUV|Колски|Kolski|Wagenbrief", RegexOptions.IgnoreCase)) z.Cuv = true;
                var grupe = zona.Where(w => !JeOznaka(w)).OrderBy(w => Math.Round(w.Cy, 2)).ThenBy(w => w.X0).Select(w => w.T).ToList();
                foreach (var g in grupe.SelectMany(t => Regex.Matches(t, @"\d+(?:-\d)?").Cast<Match>().Select(m => m.Value)))
                {
                    var c = Regex.Replace(g, @"\D", "");
                    if (z.Zemlja == "" && z.Stanica == "" && c.Length == 2) z.Zemlja = c;
                    else if (z.Stanica == "" && z.Preduzece == "" && c.Length >= 5 && c.Length <= 7) z.Stanica = g;
                    else if (z.Preduzece == "" && z.Broj == "" && c.Length == 4 && !g.Contains("-")) z.Preduzece = c;
                    else if (z.Broj == "" && c.Length >= 5 && c.Length <= 7) z.Broj = g;
                }
            }
            if (preuz != null) z.Preuzimanje = Tekst(Podaci(Sredina - 0.02, 1, Y1(preuz, 0), Y0(kola, Y0(adrPrev, 0.3)))).Replace("\n", ", ");
            if (kola != null)
            {
                z.Kola = Tekst(Podaci(Sredina - 0.02, 1, Y1(kola, 0), Y0(adrPrev, Y0(zahtev, 0.4)))).Replace("\n", ", ");
                z.BrojeviKola = Regex.Matches(Regex.Replace(z.Kola, @"(?<=\d)[\s\-](?=\d)", ""), @"(?<!\d)\d{12}(?!\d)").Cast<Match>().Select(m => m.Value).Distinct().ToList();
            }

            // označene šifre zahteva (levo) i carina (desno)
            var dole = Y0(kraj, 0.95);
            if (zahtev != null)
            {
                foreach (var x in Podaci(0, Sredina, Y0(zahtev, 0), Y0(dodatne, dole)).Where(JeOznaka))
                {
                    var sifra = reci.Where(w => Regex.IsMatch(w.T, "^[1-6]$") && Math.Abs(w.Cy - x.Cy) < 0.012 && w.Cx > x.Cx && w.Cx - x.Cx < 0.12)
                        .OrderBy(w => w.Cx - x.Cx).FirstOrDefault();
                    if (sifra != null && !z.Sifre.Contains(int.Parse(sifra.T))) z.Sifre.Add(int.Parse(sifra.T));
                }
                foreach (var x in Podaci(Sredina - 0.02, 1, Y0(zahtev, 0), Y0(nap, dole)).Where(JeOznaka))
                {
                    var lab = NajblizaOznaci(reci, x, Sredina - 0.02, 1) ?? "";
                    if (Regex.IsMatch(lab, @"Није|Nije|nicht erforderlich|pas n[eé]cessaire|not necessary", RegexOptions.IgnoreCase)) z.BezCarine = true;
                    else if (Regex.IsMatch(lab, @"Одобрење|Odobrenje|Genehmigung|autorisation|approval", RegexOptions.IgnoreCase)) z.OdobrenjeCarine = true;
                }
                z.Sifre.Sort();
            }
            var kodTekst = zahtev == null ? "" : Tekst(Podaci(0.1, Sredina, Y1(zahtev, 0), Y0(dodatne, dole)).Where(w => !JeOznaka(w)));
            var napomene = nap == null ? "" : Tekst(Podaci(Sredina - 0.02, 1, Y1(nap, 0), dole - 0.005).Where(w => !JeOznaka(w)));
            var dodatneTekst = dodatne == null ? "" : Tekst(Podaci(0, Sredina, Y1(dodatne, 0), dole - 0.005).Where(w => !JeOznaka(w)));
            z.Napomene = string.Join("\n", new[] { napomene, dodatneTekst }.Where(x => x.Length > 0));

            // potpis podnosioca (levo, ispod „Mesto, datum – Potpis pošiljaoca / primaoca“)
            if (kraj != null)
            {
                var potpis = Tekst(Podaci(0, Sredina, kraj.Y1, Math.Min(1, kraj.Y1 + 0.06))).Replace("\n", " ");
                var d = Regex.Match(potpis, @"\b[0-3]?\d[./][01]?\d[./](20)?\d{2}\.?");
                if (d.Success) { z.Datum = d.Value.TrimEnd('.'); potpis = potpis.Replace(d.Value, " "); }
                z.Podnosilac = Regex.Replace(potpis, @"\s+", " ").Trim(' ', ',', '–', '-');
            }

            z.Tekst = string.Join(" ", (imaPolja ? reci.Where(w => w.JePodatak) : reci).Select(w => w.T));
            z.Izmene = Izmene(z.Napomene + "\n" + kodTekst);
            if (z.Izmene.Count == 0 && kodTekst.Length > 0)
            {
                if (z.Sifre.Contains(3)) z.Izmene.Add(new Cit7Izmena { Rubrika = "4", Vrednost = kodTekst.Replace("\n", ", ") });
                else if (z.Sifre.Contains(4)) z.Izmene.Add(new Cit7Izmena { Rubrika = "10", Vrednost = kodTekst.Replace("\n", ", ") });
            }
            return z;
        }

        private static bool JeOznaka(Rec w) => Regex.IsMatch(w.T.Trim(), @"^[xX×✓✔☒]$");

        /// <summary>Natpis u istom redu desno od oznake X (tekst reda natpisa).</summary>
        private static string NajblizaOznaci(List<Rec> natpisi, Rec x, double x0, double x1) // natpisi = sve reči strane (i polja obrasca sa natpisom)
        {
            var red = natpisi.Where(w => !JeOznaka(w) && w.Cx > x.Cx && w.Cx >= x0 && w.Cx < x1 && Math.Abs(w.Cy - x.Cy) < 0.012).OrderBy(w => w.X0).ToList();
            return red.Count == 0 ? null : string.Join(" ", red.Select(w => w.T));
        }

        private static readonly Regex Oznaka = new Regex(@"^\s*(?:box|rubrika|rubr\.|rub\.|polje|feld|case|r\.)\s*(\d{1,2}[ab]?)\s*[:.\-–]?\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>„Box 4: GRANEXPORT…⏎Luka Dunav br.5⏎Box 10: Sremska Mitrovica“ → (4, „GRANEXPORT…, Luka Dunav br.5“), (10, „Sremska Mitrovica“).</summary>
        internal static List<Cit7Izmena> Izmene(string tekst)
        {
            var rez = new List<Cit7Izmena>();
            Cit7Izmena tekuca = null;
            foreach (var red in (tekst ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                var m = Oznaka.Match(red);
                if (m.Success) { tekuca = new Cit7Izmena { Rubrika = m.Groups[1].Value.ToLowerInvariant(), Vrednost = m.Groups[2].Value.Trim() }; rez.Add(tekuca); continue; }
                if (tekuca != null) tekuca.Vrednost = (tekuca.Vrednost.Length > 0 ? tekuca.Vrednost + ", " : "") + red;
            }
            return rez.Where(x => x.Vrednost.Length > 0).ToList();
        }
    }
}
