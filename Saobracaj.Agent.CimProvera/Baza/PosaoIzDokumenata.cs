using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>Poreklo jedne vrednosti (red u planiranje.tmpJobsPolje).</summary>
    internal sealed class PoljePorekla
    {
        public string Kolona = "";
        public object Vrednost;
        public string Tekst = "";
        public string Rubrika = "";
        public int Pouzdanost = 100;
        public string Kandidati = "";
        /// <summary>Fajl dokumenta iz kog je vrednost (za DokumentID).</summary>
        public string Fajl = "";
    }

    /// <summary>Vrednosti posla (kolone planiranje.tmpJobs) sa poreklom, izvedene iz tovarnog lista i/ili spiska kola.</summary>
    internal sealed class PodaciPosla
    {
        public readonly Dictionary<string, object> V = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public readonly List<PoljePorekla> Poreklo = new List<PoljePorekla>();
        /// <summary>Napomene za istoriju (npr. „Broj kola u r21 (26) ne odgovara spisku kola (28)“).</summary>
        public readonly List<string> Napomene = new List<string>();
        public string KljucPosiljke;
        public string BrojVoza = "";
        /// <summary>12-cifreni brojevi kola iz tovarnog lista (r18/r21) – za povezivanje spiska.</summary>
        public List<string> Kola = new List<string>();
        /// <summary>r62 za stavke teretnice (Dokument = broj otpravljanja).</summary>
        public string BrojOtpravljanja = "";

        public void Postavi(string kolona, object v, string tekst, string rubrika, int pouzdanost = 100, string kandidati = "", string fajl = "")
        {
            V[kolona] = v;
            Poreklo.RemoveAll(x => string.Equals(x.Kolona, kolona, StringComparison.OrdinalIgnoreCase));
            Poreklo.Add(new PoljePorekla { Kolona = kolona, Vrednost = v, Tekst = Skrati(tekst, 1000), Rubrika = rubrika, Pouzdanost = Math.Max(0, Math.Min(100, pouzdanost)), Kandidati = Skrati(kandidati, 1000), Fajl = fajl });
        }

        public void Postavi(string kolona, Pogodak p, string rubrika, string fajl)
        {
            var tekst = p.Tekst + (p.Napomena.Length > 0 ? " [" + p.Napomena + "]" : "");
            Postavi(kolona, p.Id, tekst, rubrika, p.Id.HasValue ? p.Pouzdanost : 0, p.Kandidati, fajl);
        }

        internal static string Skrati(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n - 1) + "…";
    }

    /// <summary>Jedan prevoznik iz r13/r57/r58a sa deonicom.</summary>
    internal sealed class PrevoznikNaPutu
    {
        public string Uic = ""; public string Naziv = ""; public string Deonica = "";
        public string Od => Delovi().od; public string Do => Delovi().@do;
        private (string od, string @do) Delovi()
        {
            var d = Regex.Replace(Deonica ?? "", @"\s+", " ").Trim();
            // „PANČEVO GLAVNA (U.M. PANČEVO VAROŠ)-ŠID/TOVARNIK“ – crtica van zagrada
            var bez = Regex.Replace(d, @"\([^)]*\)?", m => new string('_', m.Length));
            var i = bez.LastIndexOf('-');
            if (i < 0) i = bez.LastIndexOf('–');
            return i <= 0 ? (d, "") : (d.Substring(0, i).Trim(), d.Substring(i + 1).Trim());
        }
        public override string ToString() => Uic + " " + Naziv + " [" + Deonica + "]";
    }

    /// <summary>
    /// v2.9.0: tovarni list CIM/CUV (+ spisak kola) → vrednosti posla (tačka 3.1). Bez baze: šifre daje <see cref="Poklapanje"/>.
    /// Deonica KP: iz r13 (13.3 redosled prevoznika, 13.4 granični prelazi) i r57 (deonice); KP = 3212. Kada voz polazi iz Srbije,
    /// PreuzimaOd = pošiljalac i Stanica1 = otpravna; kada se završava u Srbiji, PredajeOperateru = primalac i Stanica3 = uputna;
    /// na predaji između prevoznika upisuje se granična stanica „X gr.“ (kada postoji).
    /// </summary>
    internal static class PosaoIzDokumenata
    {
        public static readonly string[] Kolone =
        {
            "PlatilacID", "OrganizatorID", "TipPrevozaID", "OtpravnaID", "UputnaID", "Status", "PreuzimaOdID", "PredajeOperateruID", "Stanica1ID", "Stanica2ID", "Stanica3ID", "ETD",
            "NHM1", "Bruto1", "BrojKola1", "Duzina1", "Neto1", "RIDBroj1", "OMBroj1", "NHM2", "Bruto2", "BrojKola2", "Duzina2", "Neto2", "RIDBroj2", "OMBroj2",
            "SerijaKola1", "SerijaKola2", "KljucPosiljke", "Referenca", "BrojPonude", "ImaGreske", "KPTovarniList",
        };

        /// <summary>Kolone koje spisak kola menja (prednost nad zbirnim podacima tovarnog lista).</summary>
        public static readonly string[] KoloneSpiska =
            { "NHM1", "Bruto1", "BrojKola1", "Duzina1", "Neto1", "OMBroj1", "SerijaKola1", "NHM2", "Bruto2", "BrojKola2", "Duzina2", "Neto2", "OMBroj2", "SerijaKola2" };

        private static string T(Rubrika r) => r == null ? "" : Regex.Replace(r.Tekst ?? "", @"[ \t]+", " ").Trim();

        public static PodaciPosla Napravi(CimList c, Dokument d, RezultatLista l, Poklapanje pk, PraviloKp kp, string naslovMaila = null)
        {
            var p = new PodaciPosla();
            var fajl = d?.Fajl ?? "";
            p.KljucPosiljke = Ponavljanja.KljucLista(c);
            foreach (var k in new[] { "OrganizatorID", "TipPrevozaID", "Status", "Stanica2ID", "ETD" }) p.V[k] = null;

            // r8, r14, r62
            var r8 = T(c.R8);
            p.Postavi("Referenca", r8.Length == 0 ? null : PodaciPosla.Skrati(r8, 100), r8, "r8", r8.Length > 0 ? (int)Math.Round(100 * Math.Min(1, c.R8.Pouzdanost)) : 0, "", fajl);
            var r14 = T(c.R14);
            p.Postavi("BrojPonude", r14.Length == 0 ? null : PodaciPosla.Skrati(r14, 100), r14, "r14", r14.Length > 0 ? (int)Math.Round(100 * Math.Min(1, c.R14.Pouzdanost)) : 0, "", fajl);
            p.Postavi("KljucPosiljke", p.KljucPosiljke == null ? null : PodaciPosla.Skrati(p.KljucPosiljke, 200), p.KljucPosiljke ?? "", "r62 / r1+r8+relacija", 100, "", fajl);
            var kpUic = kp?.Uic ?? "3212";
            var r62Tekst = string.Join(" ", new[] { c.R62?.Zemlja, c.R62?.Stanica, c.R62?.Preduzece, c.R62?.Broj }.Where(x => !string.IsNullOrEmpty(x)));
            p.Postavi("KPTovarniList", Regex.Replace(c.R62?.Preduzece ?? "", @"\D", "") == kpUic && !(c.R62?.NepouzdanaZaOdluke ?? false), r62Tekst, "r62", (c.R62?.NepouzdanaZaOdluke ?? false) ? 50 : 100, "", fajl);
            var greske = l?.Nalazi.Where(n => n.Tezina == Tezina.Greska).Select(n => n.Pravilo + " r" + n.Rubrika).Distinct().ToList() ?? new List<string>();
            p.Postavi("ImaGreske", greske.Count > 0, greske.Count == 0 ? "bez grešaka" : string.Join(", ", greske), "provera", 100, "", fajl);
            p.BrojVoza = new[] { r8, T(c.R15), naslovMaila ?? "" }.Select(ExcelCitac.BrojVoza).FirstOrDefault(x => x.Length > 0) ?? "";
            p.BrojOtpravljanja = Regex.Replace(c.R62?.Broj ?? "", @"\D", "");

            // stanice: otpravna (r16/r17), uputna (r10/r12)
            var otpTekst = !string.IsNullOrWhiteSpace(c.R16?.Mesto) ? c.R16.Mesto : T(c.R16);
            var otpSifra = c.R16?.SifraStanice ?? "";
            var otp = pk.Stanica(otpTekst, otpSifra, c.R16?.Zemlja);
            p.Postavi("OtpravnaID", otp, "r16", fajl);
            var upTekst = !string.IsNullOrWhiteSpace(c.R10?.Stanica) ? c.R10.Stanica + "\n" + T(c.R10) : T(c.R10);
            var upSifra = !string.IsNullOrWhiteSpace(c.R10?.SifraStanice) ? c.R10.SifraStanice : T(c.R12);
            var up = pk.Stanica(upTekst, upSifra, c.R10?.Zemlja);
            p.Postavi("UputnaID", up, "r10/r12", fajl);

            var srb = new[] { "SRB" };
            bool USrbiji(Pogodak st, string sifra, string zemlja)
            {
                if (st.Id.HasValue) return pk.DrzavaStanice(st.Id.Value) == "SRB";
                return Poklapanje.Drzave(sifra).Contains("SRB") || Poklapanje.Drzave(zemlja).Contains("SRB");
            }
            var polazi = USrbiji(otp, otpSifra, c.R16?.Zemlja);
            var zavrsava = USrbiji(up, upSifra, c.R10?.Zemlja);

            // prevoznici i deonica KP
            var put = Prevoznici(c);
            var iKp = put.FindIndex(x => x.Uic == kpUic);
            var pre = iKp > 0 ? put[iKp - 1] : null;
            var posle = iKp >= 0 && iKp + 1 < put.Count ? put[iKp + 1] : null;
            var opisPuta = string.Join(", ", put.Select(x => x.Uic + (x.Naziv.Length > 0 ? " " + x.Naziv : "")));

            // PreuzimaOd / Stanica1
            if (polazi)
            {
                p.Postavi("PreuzimaOdID", pk.Partner(c.R1?.Naziv, null, DrzaveStrane(c.R1), "posiljalac"), "r1 (voz polazi iz Srbije)", fajl);
                p.Postavi("Stanica1ID", otp, "r16 (voz polazi iz Srbije)", fajl);
            }
            else
            {
                if (pre != null) p.Postavi("PreuzimaOdID", pk.Partner(pre.Naziv, pre.Uic, null, "prevoznik"), "r13/r57: " + pre, fajl);
                else p.Postavi("PreuzimaOdID", null, iKp < 0 ? "KP (3212) nije pronađen u r13/r57: " + opisPuta : "prethodni prevoznik nije naveden", "r13/r57", 0, "", fajl);
                var ulaz = TackaPredaje(pk, new[] { pre?.Do, iKp >= 0 ? put[iKp].Od : null }.Concat(Prelazi(c)).Concat(new[] { T(c.R20) }), srb);
                p.Postavi("Stanica1ID", ulaz, "r57/r13 (preuzimanje od prethodnog prevoznika)", fajl);
            }
            // PredajeOperateru / Stanica3
            if (zavrsava)
            {
                p.Postavi("PredajeOperateruID", pk.Partner(c.R4?.Naziv, null, DrzaveStrane(c.R4), "primalac"), "r4 (voz se završava u Srbiji)", fajl);
                p.Postavi("Stanica3ID", up, "r10/r12 (voz se završava u Srbiji)", fajl);
            }
            else
            {
                if (posle != null) p.Postavi("PredajeOperateruID", pk.Partner(posle.Naziv, posle.Uic, null, "prevoznik"), "r13/r57: " + posle, fajl);
                else p.Postavi("PredajeOperateruID", null, iKp < 0 ? "KP (3212) nije pronađen u r13/r57: " + opisPuta : "sledeći prevoznik nije naveden", "r13/r57", 0, "", fajl);
                var prelazi = Prelazi(c).ToList();
                var izlaz = TackaPredaje(pk, new[] { iKp >= 0 ? put[iKp].Do : null, posle?.Od }.Concat(polazi ? prelazi : prelazi.Skip(1)).Concat(new[] { T(c.R20) }), srb);
                p.Postavi("Stanica3ID", izlaz, "r57/r13 (predaja sledećem prevozniku)", fajl);
            }

            // platilac za deonicu KP (Srbija) iz r7 (ili r15, kao u proveri)
            var r7 = !string.IsNullOrWhiteSpace(c.R7?.Tekst) ? c.R7.Tekst : string.Join("\n", (c.R7?.Stavke ?? new List<SifraTekst>()).Select(x => (x.Sifra + " " + x.Tekst).Trim()));
            var a = kp == null ? null : Provere.AnalizirajR7(r7, kp);
            var pl = a?.ZaSrbiju.FirstOrDefault();
            if (pl == null && kp != null && T(c.R15).Length > 0)
                pl = Provere.AnalizirajR7(Regex.Replace(T(c.R15), @"(?i)streight\s*payer", "freight payer"), kp).ZaSrbiju.FirstOrDefault();
            if (pl != null) { var pp = pk.Partner(pl.Naziv); pp.Tekst = PodaciPosla.Skrati(pl.Zapis, 400) + " → „" + pl.Naziv + "“"; p.Postavi("PlatilacID", pp, "r7", fajl); }
            else p.Postavi("PlatilacID", null, PodaciPosla.Skrati(r7, 400), "r7", 0, "platilac za deonicu KP nije pronađen u r7", fajl);

            // roba i mase iz tovarnog lista (spisak kola ih kasnije zamenjuje)
            var nhm = NhmSifre(c);
            var g1 = nhm.Count > 0 ? pk.Nhm(nhm[0]) : new Pogodak { Napomena = "r24 prazna" };
            p.Postavi("NHM1", g1, "r24", fajl);
            if (nhm.Count > 1 && Poklapanje.Nhm4(nhm[1]) != Poklapanje.Nhm4(nhm[0])) p.Postavi("NHM2", pk.Nhm(nhm[1]), "r24", fajl);
            else p.V["NHM2"] = null;
            var (tara, neto, bruto, opisMasa) = Mase(c);
            p.Postavi("Bruto1", Tone(bruto ?? tara ?? 0), opisMasa, "r25/r21", bruto.HasValue || tara.HasValue ? 90 : 0, "", fajl);
            p.Postavi("Neto1", Tone(neto ?? (bruto.HasValue && tara.HasValue ? bruto.Value - tara.Value : 0)), opisMasa, "r25/r21", neto.HasValue ? 90 : 0, "", fajl);
            foreach (var k in new[] { "Bruto2", "Neto2", "BrojKola2", "Duzina2" }) p.V[k] = p.V["NHM2"] == null ? null : (object)(k.StartsWith("Broj") ? 0 : 0m);
            p.Kola = KolaIzLista(c);
            var brojKola = p.Kola.Count > 0 ? p.Kola.Count : Provere.BrojKolaUTekstu(T(c.R21)) is int n21 && n21 > 0 ? n21 : Provere.BrojKolaUTekstu(T(c.R18));
            p.Postavi("BrojKola1", brojKola, PodaciPosla.Skrati(T(c.R18) + " / " + T(c.R21), 300), "r18/r21", brojKola > 0 ? 80 : 0, "", fajl);
            p.Postavi("Duzina1", 0m, "dužina nije u tovarnom listu", "–", 0, "", fajl);
            var serija = BrojKolaUic.NajcescaOznaka(p.Kola.Select(k => (k, "")));
            p.Postavi("SerijaKola1", serija.Length == 0 ? null : serija, p.Kola.Count == 0 ? "brojevi kola nisu u tovarnom listu" : "iz brojeva kola (UIC 438)", "r18", serija.Length > 0 ? 80 : 0, "", fajl);
            p.V["SerijaKola2"] = null;
            var rid = RidBroj(c);
            p.Postavi("RIDBroj1", rid.Length == 0 ? null : rid, rid.Length == 0 ? "" : rid, "r7", rid.Length > 0 ? 80 : 0, "", fajl);
            var om = OmBroj(c);
            p.Postavi("OMBroj1", om.Length == 0 ? null : om, om, "r21", om.Length > 0 ? 80 : 0, "", fajl);
            foreach (var k in new[] { "RIDBroj2", "OMBroj2" }) p.V[k] = null;
            return p;
        }

        private static string[] DrzaveStrane(Model.Strana s) => s == null ? null : Poklapanje.Drzave(s.SifraZemlje).Concat(Poklapanje.Drzave(s.NazivZemlje)).Distinct().ToArray();

        /// <summary>Tačka predaje između prevoznika u Srbiji (prvi kandidat koji daje stanicu u Srbiji), sa prednošću „X gr.“.</summary>
        private static Pogodak TackaPredaje(Poklapanje pk, IEnumerable<string> kandidati, string[] drzave)
        {
            // prednost ima granična stanica („X gr.“ postoji ili je stanica granična): predaja između prevoznika je na granici;
            // inače prvi kandidat koji daje stanicu u Srbiji (OCR deonice u r57 može da pripiše pogrešnu deonicu prevozniku)
            Pogodak prvi = null, prviNadjen = null;
            foreach (var t in kandidati.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                var p = pk.Stanica(t, null, null, true, drzave, samoDrzave: drzave);
                if (p.Id.HasValue && (p.Napomena.Contains("granična varijanta") || pk.JeGranicna(p.Id.Value))) return p;
                if (p.Id.HasValue) prviNadjen = prviNadjen ?? p;
                prvi = prvi ?? p;
            }
            return prviNadjen ?? prvi ?? new Pogodak { Napomena = "tačka predaje nije pročitana (r57/r13)" };
        }

        /// <summary>Granični prelazi iz r13 („13.4 VIA ŠID/TOVARNIK DOBOVA/SAVSKI MAROF-…“) redom.</summary>
        internal static IEnumerable<string> Prelazi(CimList c)
        {
            var t = T(c.R13) + " " + string.Join(" ", (c.R13?.Stavke ?? new List<SifraTekst>()).Select(x => x.Tekst));
            var m = Regex.Match(t, @"(?i)\b(?:via|preko|über|uber)\b\s+(.+?)(?=\s+13[.,]\d|\s*/\s*13[.,]\d|$)");
            if (!m.Success) yield break;
            foreach (var deo in Regex.Split(m.Groups[1].Value, @"\s*-\s*|\s+(?=\p{Lu}{3,}/)"))
                if (deo.Trim().Length >= 3) yield return deo.Trim();
        }

        /// <summary>
        /// Prevoznici redom: r13 (13.3 „3212 KP, 7853 GPRAIL, 2179 SZ, 2183 MIR“) kada sadrži KP, inače r57 (i r58a); deonica iz r57.
        /// </summary>
        internal static List<PrevoznikNaPutu> Prevoznici(CimList c)
        {
            var r57 = new List<PrevoznikNaPutu>();
            foreach (var x in c.R57?.Prevoznici ?? new List<Prevoznik>())
            {
                var u = Regex.Replace(x.Sifra ?? "", @"\D", "");
                if (u.Length == 4 && !r57.Any(y => y.Uic == u)) r57.Add(new PrevoznikNaPutu { Uic = u, Naziv = (x.Naziv ?? "").Trim(), Deonica = (x.Deonica ?? "").Trim() });
            }
            // sirovi tekst r57 po redovima: „7853 ŠID/TOVARNIK-DOBOVA/SAVSKI MAROF“ – dopuna deonica koje parser nije dodelio
            foreach (var red in Regex.Split(c.R57?.Tekst ?? "", @"\s*(?:\r?\n|/\s(?=\d{4}\s))\s*"))
            {
                var m = Regex.Match(red, @"^\s*(\d{4})\s+(?:([A-ZŠĐČĆŽ]{2,8})\s+)?(.+)$");
                if (!m.Success) continue;
                var u = m.Groups[1].Value;
                var z = r57.FirstOrDefault(y => y.Uic == u);
                if (z == null) r57.Add(z = new PrevoznikNaPutu { Uic = u });
                if (z.Naziv.Length == 0 && m.Groups[2].Success) z.Naziv = m.Groups[2].Value;
                if (z.Deonica.Length == 0 && m.Groups[3].Value.Contains("-")) z.Deonica = m.Groups[3].Value.Trim();
            }
            var t13 = T(c.R13) + " " + string.Join(" ", (c.R13?.Stavke ?? new List<SifraTekst>()).Select(x => x.Sifra + " " + x.Tekst));
            var r13 = new List<PrevoznikNaPutu>();
            foreach (Match m in Regex.Matches(t13, @"(?<![\d.,])(\d{4})\s+([\p{L}][\p{L}\-]{1,15})(?![\d])"))
            {
                var u = m.Groups[1].Value;
                if (r13.Any(y => y.Uic == u) || Regex.IsMatch(u, @"^(19|20)\d\d$")) continue;
                r13.Add(new PrevoznikNaPutu { Uic = u, Naziv = m.Groups[2].Value });
            }
            var kp = "3212";
            var lista = r13.Count >= 2 && r13.Any(x => x.Uic == kp) ? r13 : r57;
            if (lista == r13)
                foreach (var x in r13) { var y = r57.FirstOrDefault(z => z.Uic == x.Uic); if (y != null) { x.Deonica = y.Deonica; if (y.Naziv.Length > x.Naziv.Length) x.Naziv = y.Naziv; } }
            var u58 = Regex.Replace(c.R58a?.Sifra ?? "", @"\D", "");
            if (u58.Length == 4 && !lista.Any(x => x.Uic == u58)) lista.Insert(0, new PrevoznikNaPutu { Uic = u58, Naziv = c.R58a.Naziv ?? "" });
            return lista;
        }

        /// <summary>NHM šifre iz r24 redom (samo cifre, 4–8).</summary>
        internal static List<string> NhmSifre(CimList c)
        {
            var l = new List<string>();
            foreach (var s in c.R24?.Stavke ?? new List<SifraTekst>())
            {
                var d = Regex.Replace(s.Sifra.Length > 0 ? s.Sifra : s.Tekst ?? "", @"\D", "");
                if (d.Length >= 4 && d.Length <= 8 && !l.Contains(d)) l.Add(d);
            }
            if (l.Count == 0)
                foreach (Match m in Regex.Matches(T(c.R24), @"(?<!\d)\d{4}(?:\s?\d{2}){0,2}(?!\d)")) { var d = Regex.Replace(m.Value, @"\s", ""); if (!l.Contains(d)) l.Add(d); }
            return l;
        }

        /// <summary>12-cifreni brojevi kola iz r18 i r21.</summary>
        internal static List<string> KolaIzLista(CimList c) =>
            (c.R18?.Stavke ?? new List<SifraTekst>()).Select(x => Regex.Replace(x.Tekst ?? "", @"\D", ""))
                .Concat(c.R21?.BrojeviKola ?? new List<string>()).Select(x => Regex.Replace(x ?? "", @"\D", "")).Where(x => x.Length == 12).Distinct().ToList();

        private static decimal Tone(double kg) => Math.Round((decimal)kg / 1000m, 3);

        /// <summary>Broj iz teksta mase u kg: „2 234533“, „1.671.863“, „562.670“, „21500“, „1627,580 t“.</summary>
        internal static double? Kg(string s)
        {
            var t = (s ?? "").Trim();
            var tone = Regex.IsMatch(t, @"\d\s*(t|to|tona|tone|tons?)\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(t, @"kg", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"(?i)\s*(kg|t|to|tona|tone|tons?)\.?\s*$", "");
            t = Regex.Replace(t, @"(?<=\d)\s+(?=\d)", "");   // „2 234 533“, OCR „2 234533“
            // oba razdvajača („2.090,52“, „2,090.52“): poslednji je decimalni
            if (t.Contains(".") && t.Contains(",")) t = t.LastIndexOf(',') > t.LastIndexOf('.') ? t.Replace(".", "") : t.Replace(",", "");
            if (Regex.IsMatch(t, @"^\d{1,3}([.,]\d{3})+$") && !tone) t = Regex.Replace(t, @"[.,]", "");
            // više od 3 decimale u masi nije tona nego kg sa izgubljenim razdvajanjem hiljada (OCR „2.234533“ = 2 234 533 kg)
            else if (Regex.IsMatch(t, @"^\d{1,3}[.,]\d{4,}$") && !tone) t = Regex.Replace(t, @"[.,]", "");
            t = t.Replace(",", ".");
            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v <= 0) return null;
            return tone || v < 200 && t.Contains(".") ? v * 1000 : v;
        }

        private static readonly Regex MasaRx = new Regex(@"(?i)\b(tara|tare|t\s?=|neto|netto|netta|neta|netio|nett|ntto|nto|net|n\s?=|bruto|brutto|brute|brutt|btto|bto|gross|g\s?=|masa|ukupno|total)\s*[:=.]?\s*(\d(?:[\d.,]|\s(?=\d))*)(\s*(?:kg|t|to|tona|tone|tons?)\b)?", RegexOptions.Compiled);

        /// <summary>Tara, neto i bruto (kg) iz r25 i r21 (oznake „Tara:“, „Netto:“, „Bruto:“…), pa r25 Ukupno / pojedinačne mase.</summary>
        internal static (double? tara, double? neto, double? bruto, string opis) Mase(CimList c)
        {
            double? tara = null, neto = null, bruto = null;
            var tekst = (T(c.R25) + " / " + T(c.R21)).Trim(' ', '/');
            foreach (Match m in MasaRx.Matches(tekst))
            {
                var o = m.Groups[1].Value.ToLowerInvariant().Replace(" ", "");
                var v = Kg(m.Groups[2].Value + m.Groups[3].Value);
                if (v == null) continue;
                if (o.StartsWith("t")) tara = tara ?? v;
                else if (o.StartsWith("n")) neto = neto ?? v;
                else if (o.StartsWith("b") || o.StartsWith("g")) bruto = bruto ?? v;
                else bruto = bruto ?? v;
            }
            if (bruto == null && Kg(c.R25?.Ukupno) is double u) bruto = u;
            if (bruto == null && (c.R25?.Pojedinacne?.Count ?? 0) > 0)
            {
                var zbir = c.R25.Pojedinacne.Select(Kg).Where(x => x.HasValue).Sum(x => x.Value);
                if (zbir > 0) bruto = zbir;
            }
            if (tara == null && Kg(c.R25?.Tara) is double tt) tara = tt;
            if (bruto == null && tara.HasValue && neto.HasValue) bruto = tara + neto;
            // provera bruto = tara + neto: pogrešna jedinica bruto mase (×1000) se ispravlja zbirom
            if (bruto.HasValue && tara.HasValue && neto.HasValue && Math.Abs(bruto.Value - (tara.Value + neto.Value)) > 0.01 * (tara.Value + neto.Value)
                && (Math.Abs(bruto.Value * 1000 - (tara.Value + neto.Value)) < 0.01 * (tara.Value + neto.Value) || Math.Abs(bruto.Value / 1000 - (tara.Value + neto.Value)) < 0.01 * (tara.Value + neto.Value)))
                bruto = tara + neto;
            var opis = PodaciPosla.Skrati(tekst, 300) + (tara.HasValue || neto.HasValue || bruto.HasValue ? "" : " – mase nisu navedene (0)");
            return (tara, neto, bruto, opis);
        }

        /// <summary>Broj odobrenja (RID): r7 šifra 11, ili tekst sa „odobrenje / approval / Genehmigung“ i brojem.</summary>
        internal static string RidBroj(CimList c)
        {
            var s11 = (c.R7?.Stavke ?? new List<SifraTekst>()).FirstOrDefault(x => Regex.Replace(x.Sifra ?? "", @"^7\.", "") == "11");
            if (s11 != null && Regex.IsMatch(s11.Tekst ?? "", @"\d")) return PodaciPosla.Skrati(s11.Tekst.Trim(), 100);
            var m = Regex.Match(T(c.R7), @"(?i)\b(?:odobrenj\w*|approval|genehmigung|autorizacij\w*)\b\D{0,20}(\d[\d/\-.]{2,30})");
            return m.Success ? m.Groups[1].Value.Trim('.', '-') : "";
        }

        private static readonly Regex KemlerRx = new Regex(@"(?<![\d/])([2-9]\d{1,2}X?)\s*/\s*(\d{4})(?![\d/])", RegexOptions.Compiled);

        /// <summary>Kemler/UN („80/1824, 85/2031“) iz r21 – samo kada je RID označen (r23) ili su upisani UN brojevi.</summary>
        internal static string OmBroj(CimList c)
        {
            var rid = (c.R23?.Popunjena ?? false) || (c.R21?.UnBrojevi?.Count ?? 0) > 0 || (c.R21?.RidKlase?.Count ?? 0) > 0;
            if (!rid) return "";
            var l = KemlerRx.Matches(T(c.R21)).Cast<Match>().Select(m => m.Groups[1].Value + "/" + m.Groups[2].Value).Distinct().ToList();
            return PodaciPosla.Skrati(string.Join(", ", l), 100);
        }

        // ------------------------------------------------------------------------------------------- spisak kola → posao

        /// <summary>Grupa robe sa spiska (za NHM1/NHM2).</summary>
        internal sealed class GrupaRobe { public string Nhm = ""; public List<KolaNaSpisku> Kola = new List<KolaNaSpisku>(); }

        /// <summary>Kola spiska po NHM-u (NHM kola, pa zaglavlje spiska, pa NHM posla), najveća grupa prva.</summary>
        internal static List<GrupaRobe> Grupe(SpisakKola s, string nhmPosla)
        {
            return s.Kola.GroupBy(k => Poklapanje.Nhm4(k.Nhm.Length > 0 ? k.Nhm : s.NhmZaglavlje.Length > 0 ? s.NhmZaglavlje : nhmPosla ?? ""))
                .Select(g => new GrupaRobe { Nhm = g.First().Nhm.Length > 0 ? g.First().Nhm : s.NhmZaglavlje.Length > 0 ? s.NhmZaglavlje : nhmPosla ?? "", Kola = g.ToList() })
                .OrderByDescending(g => g.Kola.Count).ToList();
        }

        /// <summary>
        /// Vrednosti posla sa spiska (tačka 4.3: BrojKola, Bruto, Neto, Duzina, SerijaKola, NHM, OMBroj; spisak ima prednost).
        /// Mase u tonama: bruto = bruto kola, ili tara + neto; prazna kola (samo tara) – bruto = tara, neto = 0; bez masa 0.
        /// </summary>
        internal static PodaciPosla IzSpiska(SpisakKola s, Poklapanje pk, string nhmPosla, string fajl)
        {
            var p = new PodaciPosla();
            var grupe = Grupe(s, nhmPosla);
            for (int i = 0; i < 2; i++)
            {
                var n = (i + 1).ToString();
                if (i >= grupe.Count)
                {
                    foreach (var k in new[] { "NHM", "Bruto", "BrojKola", "Duzina", "Neto", "OMBroj", "SerijaKola" }) p.V[k + n] = null;
                    continue;
                }
                var g = grupe[i];
                var nh = g.Nhm.Length > 0 ? pk.Nhm(g.Nhm) : new Pogodak { Napomena = "NHM nije na spisku" };
                p.Postavi("NHM" + n, nh, "spisak", fajl);
                double Br(KolaNaSpisku k) => k.BrutoKg ?? ((k.TaraKg ?? 0) + (k.NetoKg ?? 0));
                var bruto = g.Kola.Sum(Br); var neto = g.Kola.Sum(k => k.NetoKg ?? 0);
                // zbir sa spiska kada pojedinačne mase nisu pročitane (samo jedna grupa)
                if (bruto == 0 && grupe.Count == 1) bruto = s.BrutoKg ?? ((s.TaraKg ?? 0) + (s.NetoKg ?? 0));
                if (neto == 0 && grupe.Count == 1) neto = s.NetoKg ?? 0;
                var imaMase = bruto > 0 || neto > 0;
                // bruto sa spiska samo kada spisak ima taru ili bruto za (skoro) sva kola – inače je to samo neto, a bruto ostaje iz tovarnog lista
                var saTarom = g.Kola.Count(k => k.BrutoKg.HasValue || k.TaraKg.HasValue);
                var brutoPotpun = saTarom >= 0.9 * g.Kola.Count || (grupe.Count == 1 && (s.BrutoKg.HasValue || s.TaraKg.HasValue));
                // spisak bez masa ne briše mase tovarnog lista (ako ni list nema mase, ostaje 0 iz tovarnog lista)
                if (brutoPotpun && imaMase)
                    p.Postavi("Bruto" + n, Tone(bruto), "spisak kola: " + g.Kola.Count + " kola, bruto " + bruto.ToString("N0", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " kg", "spisak", imaMase ? 95 : 0, "", fajl);
                if (neto > 0 || (imaMase && brutoPotpun)) p.Postavi("Neto" + n, Tone(neto), "spisak kola: neto " + neto.ToString("N0", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " kg", "spisak", imaMase ? 95 : 0, "", fajl);
                p.Postavi("BrojKola" + n, g.Kola.Count, "spisak kola: " + g.Kola.Count + " kola", "spisak", 100, "", fajl);
                var duz = g.Kola.Sum(k => k.DuzinaM ?? 0);
                p.Postavi("Duzina" + n, Math.Round((decimal)duz, 2), duz > 0 ? "spisak kola: zbir dužina " + duz.ToString("0.00", CultureInfo.InvariantCulture) + " m" : "dužina nije na spisku kola (0)", "spisak", duz > 0 ? 95 : 0, "", fajl);
                var ser = BrojKolaUic.NajcescaOznaka(g.Kola.Select(k => (k.Broj, k.Serija)));
                p.Postavi("SerijaKola" + n, ser.Length == 0 ? null : ser, g.Kola.Any(k => k.Serija.Length > 0) ? "serija sa spiska" : "iz brojeva kola (UIC 438)", "spisak", ser.Length > 0 ? 85 : 0, "", fajl);
                var om = string.Join(", ", g.Kola.Select(k => k.KemlerUn).Where(x => x.Length > 0).Distinct());
                p.Postavi("OMBroj" + n, om.Length == 0 ? null : PodaciPosla.Skrati(om, 100), om, "spisak", om.Length > 0 ? 90 : 0, "", fajl);
            }
            return p;
        }
    }
}
