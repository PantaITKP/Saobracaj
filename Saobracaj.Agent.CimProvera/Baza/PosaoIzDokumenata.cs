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
        // v2.9.1: podaci lista za spajanje kopija i vozova (jedan voz = jedan posao)
        public string Obrazac = "";
        /// <summary>Pošiljalac (r1) i primalac (r4), normalizovani nazivi.</summary>
        public string Posiljalac = "", Primalac = "";
        public bool Polazi, Zavrsava;
        /// <summary>Prazna kola (CUV, NHM 9921/9922).</summary>
        public bool Prazna;
        /// <summary>NHM grupa (4 cifre) lista.</summary>
        public string Nhm4 = "";
        /// <summary>Ostale NHM grupe u istom listu („3105 ×5, 2106 ×5“).</summary>
        public string OstaliNhm = "";
        /// <summary>Mase lista u kg (null = nije navedeno).</summary>
        public double? NetoKg, TaraKg, BrutoKg;
        /// <summary>Grupe sa spiska (NHM, prazna, brojevi kola) – za raspodelu teretnice po grupama posla.</summary>
        public List<(string nhm, bool prazna, List<string> kola)> Grupe = new List<(string, bool, List<string>)>();

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
            "SerijaKola1", "SerijaKola2", "KljucPosiljke", "Referenca", "BrojPonude", "ImaGreske", "KPTovarniList", "PredajePartneruID", "Tara1", "Tara2",
        };

        /// <summary>Kolone koje spisak kola menja (prednost nad zbirnim podacima tovarnog lista).</summary>
        public static readonly string[] KoloneSpiska =
            { "NHM1", "Bruto1", "BrojKola1", "Duzina1", "Neto1", "Tara1", "OMBroj1", "SerijaKola1", "NHM2", "Bruto2", "BrojKola2", "Duzina2", "Neto2", "Tara2", "OMBroj2", "SerijaKola2" };

        private static string T(Rubrika r) => r == null ? "" : Regex.Replace(r.Tekst ?? "", @"[ \t]+", " ").Trim();

        public static PodaciPosla Napravi(CimList c, Dokument d, RezultatLista l, Poklapanje pk, PraviloKp kp, string naslovMaila = null, string posiljalacMaila = null)
        {
            var p = new PodaciPosla();
            var fajl = d?.Fajl ?? "";
            foreach (var k in new[] { "OrganizatorID", "TipPrevozaID", "Status", "Stanica2ID", "ETD" }) p.V[k] = null;
            p.Obrazac = c.Obrazac ?? "";

            // r8, r14 (i „KP Ponuda 8957“ u r7), r62 – samo vrednosti koje liče na oznaku (v2.9.1, tačka 5.8)
            var r8 = T(c.R8);
            var ref8 = Oznaka(r8);
            p.Postavi("Referenca", ref8, r8, "r8", ref8 == null ? 0 : (int)Math.Round(100 * Math.Min(1, c.R8.Pouzdanost)), r8.Length > 0 && ref8 == null ? "ne liči na oznaku – nije upisano" : "", fajl);
            var r14 = T(c.R14);
            var ponuda = Oznaka(r14);
            var r7 = !string.IsNullOrWhiteSpace(c.R7?.Tekst) ? c.R7.Tekst : string.Join("\n", (c.R7?.Stavke ?? new List<SifraTekst>()).Select(x => (x.Sifra + " " + x.Tekst).Trim()));
            var ponuda7 = ponuda == null ? PonudaIzR7(r7) : null;
            if (ponuda != null) p.Postavi("BrojPonude", ponuda, r14, "r14", (int)Math.Round(100 * Math.Min(1, c.R14.Pouzdanost)), "", fajl);
            else if (ponuda7 != null) p.Postavi("BrojPonude", ponuda7, PodaciPosla.Skrati(r7, 300), "r7", 85, "", fajl);
            else p.Postavi("BrojPonude", null, r14, "r14", 0, r14.Length > 0 ? "ne liči na oznaku – nije upisano" : "", fajl);
            var kpUic = kp?.Uic ?? "3212";
            var r62Tekst = string.Join(" ", new[] { c.R62?.Zemlja, c.R62?.Stanica, c.R62?.Preduzece, c.R62?.Broj }.Where(x => !string.IsNullOrEmpty(x)));
            p.Postavi("KPTovarniList", Regex.Replace(c.R62?.Preduzece ?? "", @"\D", "") == kpUic && !(c.R62?.NepouzdanaZaOdluke ?? false), r62Tekst, "r62", (c.R62?.NepouzdanaZaOdluke ?? false) ? 50 : 100, "", fajl);
            var greske = l?.Nalazi.Where(n => n.Tezina == Tezina.Greska).Select(n => n.Pravilo + " r" + n.Rubrika).Distinct().ToList() ?? new List<string>();
            p.Postavi("ImaGreske", greske.Count > 0, greske.Count == 0 ? "bez grešaka" : string.Join(", ", greske), "provera", 100, "", fajl);
            p.BrojVoza = new[] { r8, T(c.R15), naslovMaila ?? "" }.Select(ExcelCitac.BrojVoza).FirstOrDefault(x => x.Length > 0) ?? "";
            p.BrojOtpravljanja = Regex.Replace(c.R62?.Broj ?? "", @"\D", "");
            p.Posiljalac = Slicnost.Partner(c.R1?.Naziv); p.Primalac = Slicnost.Partner(c.R4?.Naziv);

            // stanice: otpravna (r16/r17; šifra iz r16, inače iz r62 – zemlja + stanica), uputna (r10/r12)
            var otpTekst = !string.IsNullOrWhiteSpace(c.R16?.Mesto) ? c.R16.Mesto + (T(c.R16).Length > 0 && T(c.R16) != c.R16.Mesto ? "\n" + T(c.R16) : "") : T(c.R16);
            var otpSifra = c.R16?.SifraStanice ?? "";
            var r62Stanica = Regex.Replace(c.R62?.Zemlja ?? "", @"\D", "").Length == 2 && Regex.Replace(c.R62?.Stanica ?? "", @"\D", "").Length >= 5 && !(c.R62?.NepouzdanaZaOdluke ?? false)
                ? Regex.Replace(c.R62.Zemlja, @"\D", "") + Regex.Replace(c.R62.Stanica, @"\D", "") : "";
            var otp = pk.Stanica(otpTekst, otpSifra.Length > 0 ? otpSifra : r62Stanica.Length > 0 ? r62Stanica : null, c.R16?.Zemlja);
            if (!otp.Id.HasValue && otpSifra.Length > 0 && r62Stanica.Length > 0) otp = pk.Stanica(otpTekst, r62Stanica, c.R16?.Zemlja);
            var sifraOtp = otpSifra.Length > 0 ? otpSifra : r62Stanica;
            if (otp.Id.HasValue && sifraOtp.Length > 0 && pk.StaniceSaKodom(sifraOtp) is List<int> isti && isti.Count > 1 && !string.IsNullOrWhiteSpace(c.R29?.Mesto))
            {
                var po29 = pk.Stanica(c.R29.Mesto);
                if (po29.Id.HasValue && isti.Contains(po29.Id.Value) && po29.Id != otp.Id)
                { po29.Napomena = "šifra " + sifraOtp + " pripada stanicama " + string.Join(", ", isti) + " – izabrana po mestu ispostavljanja (r29)"; po29.Pouzdanost = Math.Min(85, po29.Pouzdanost); otp = po29; }
            }
            p.Postavi("OtpravnaID", otp, otpSifra.Length == 0 && r62Stanica.Length > 0 ? "r16 / r62 (šifra stanice " + r62Stanica + ")" : "r16", fajl);
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
            var polazi = USrbiji(otp, otpSifra.Length > 0 ? otpSifra : r62Stanica, c.R16?.Zemlja);
            var zavrsava = USrbiji(up, upSifra, c.R10?.Zemlja);
            p.Polazi = polazi; p.Zavrsava = zavrsava;

            // prevoznici i deonica KP
            var put = Prevoznici(c);
            var iKp = put.FindIndex(x => x.Uic == kpUic);
            var pre = iKp > 0 ? put[iKp - 1] : null;
            var posle = iKp >= 0 && iKp + 1 < put.Count ? put[iKp + 1] : null;
            var opisPuta = string.Join(", ", put.Select(x => x.Uic + (x.Naziv.Length > 0 ? " " + x.Naziv : "")));
            string[] Zemlja(PrevoznikNaPutu x) => x == null ? null : DrzaveDeonice(pk, x);

            // PreuzimaOd / PredajeOperateru: iz lanca prevoznika na tovarnom listu (r57 i r13 13.3) – prevoznik pre KP-a, odnosno posle KP-a.
            // v2.9.1 (5.3): KP nije u r57 – voz polazi iz Srbije → PredajeOperateru = prevoznik prve strane deonice; voz se završava u Srbiji →
            // PreuzimaOd = prevoznik poslednje strane deonice, pa prevoznik sa spiska / pošiljalac maila (Prevoznik = 1); pouzdanost najviše 60
            Pogodak izvedenPre = null;
            if (pre == null && iKp < 0 && zavrsava && put.Count > 0 && SusedSrbije(pk, put[put.Count - 1]))
            {
                var x = put[put.Count - 1];
                var pg = pk.Partner(x.Naziv, x.Uic, Zemlja(x), "prevoznik"); pg.Pouzdanost = Math.Min(60, pg.Pouzdanost);
                pg.Napomena = (pg.Napomena + "; izvedeno: KP nije u r57, poslednji prevoznik pre Srbije").Trim(' ', ';');
                // partner iz šifarnika van susedne zemlje (PKP – PL) nije prevoznik koji predaje voz KP-u
                if (pg.Id.HasValue && pk.DrzavaPartnera(pg.Id.Value) is string dz && dz.Length > 0 && dz != "SRB" && !SusedneZemlje.Contains(dz)) pg = null;
                izvedenPre = pg;
            }
            if (pre != null) p.Postavi("PreuzimaOdID", pk.Partner(pre.Naziv, pre.Uic, Zemlja(pre), "prevoznik"), "r57/r13: " + pre, fajl);
            else if (izvedenPre != null) p.Postavi("PreuzimaOdID", izvedenPre, "r57 (izvedeno): " + put[put.Count - 1], fajl);
            else if (iKp < 0 && zavrsava && !string.IsNullOrEmpty(posiljalacMaila) && pk.PrevoznikPoDomenu(posiljalacMaila) is Pogodak pm && pm.Id.HasValue)
            {
                pm.Napomena += "; izvedeno: KP nije u r57, prevoznik nije naveden – pošiljalac maila";
                p.Postavi("PreuzimaOdID", pm, "pošiljalac maila (izvedeno)", fajl);
            }
            else p.Postavi("PreuzimaOdID", null, iKp < 0 ? "KP (3212) nije pronađen u lancu prevoznika r57/r13: " + opisPuta : "KP je prvi u lancu prevoznika (" + opisPuta + ")", "r57/r13", iKp < 0 ? 0 : 100, "", fajl);
            if (posle != null) p.Postavi("PredajeOperateruID", pk.Partner(posle.Naziv, posle.Uic, Zemlja(posle), "prevoznik"), "r57/r13: " + posle, fajl);
            else if (iKp < 0 && polazi && put.Count > 0 && SusedSrbije(pk, put[0]))
            {
                var x = put[0];
                var pg = pk.Partner(x.Naziv, x.Uic, Zemlja(x), "prevoznik"); pg.Pouzdanost = Math.Min(60, pg.Pouzdanost);
                pg.Napomena = (pg.Napomena + "; izvedeno: KP nije u r57, prvi prevoznik posle Srbije").Trim(' ', ';');
                p.Postavi("PredajeOperateruID", pg, "r57 (izvedeno): " + x, fajl);
            }
            else p.Postavi("PredajeOperateruID", null, iKp < 0 ? "KP (3212) nije pronađen u lancu prevoznika r57/r13: " + opisPuta : "KP je poslednji u lancu prevoznika (" + opisPuta + ")", "r57/r13", iKp < 0 ? 0 : 100, "", fajl);
            // v2.9.1 (2.1): KP poslednji u lancu (ili voz se završava u Srbiji, a KP nije u r57) → PredajePartneru = primalac iz r4
            var kpPoslednji = (iKp >= 0 && posle == null) || (iKp < 0 && zavrsava);
            if (kpPoslednji)
            {
                var pr = pk.Partner(((c.R4?.Naziv ?? "").Split('\n', ',', ';')[0]).Trim(), null, DrzaveStrane(c.R4), "primalac");
                pr.Tekst = PodaciPosla.Skrati(T(c.R4).Length > 0 ? T(c.R4) : c.R4?.Naziv, 400);
                p.Postavi("PredajePartneruID", pr, "r4 (KP poslednji u lancu)", fajl);
            }
            else p.Postavi("PredajePartneruID", null, "KP nije poslednji u lancu prevoznika (" + opisPuta + ")", "r4", 100, "", fajl);

            // Stanica1 (ulaz) / Stanica3 (izlaz): tekst r57/r13 i granični kodovi (tmpSinonimi GRANICA), pa zemlja susednog prevoznika (pouzdanost 60)
            if (polazi) p.Postavi("Stanica1ID", otp, "r16 (voz polazi iz Srbije)", fajl);
            else
            {
                var tekstovi = new[] { pre?.Do, iKp >= 0 ? put[iKp].Od : null }.Concat(Prelazi(c)).Concat(new[] { T(c.R20), T(c.R13), T(c.R57), r7 }).ToList();
                var ulaz = TackaPredaje(pk, tekstovi.Take(tekstovi.Count - 3).Concat(new[] { T(c.R20) }), srb);
                if (!ulaz.Id.HasValue || !pk.JeGranicna(ulaz.Id.Value)) ulaz = Granica(pk, tekstovi, Zemlja(pre ?? (iKp < 0 && put.Count > 0 ? put[put.Count - 1] : null)), ulaz);
                p.Postavi("Stanica1ID", ulaz, "r57/r13 (preuzimanje od prethodnog prevoznika)", fajl);
            }
            // Stanica2: utovarno mesto („u.m. Kukujevci“, „Beograd Ranžirna (Ostružnica)“) kada nije otpravna stanica
            var st2 = Stanica2(pk, otp.Id, ("r16", T(c.R16)), ("r16", c.R16?.Mesto), ("naslov maila", naslovMaila), ("ime fajla", System.IO.Path.GetFileNameWithoutExtension(fajl)));
            if (st2 != null) p.Postavi("Stanica2ID", st2, "utovarno mesto", fajl);
            if (zavrsava) p.Postavi("Stanica3ID", up, "r10/r12 (voz se završava u Srbiji)", fajl);
            else
            {
                var prelazi = Prelazi(c).ToList();
                var tekstovi = new[] { iKp >= 0 ? put[iKp].Do : null, posle?.Od }.Concat(polazi ? prelazi : prelazi.Skip(1)).Concat(new[] { T(c.R20) }).ToList();
                var izlaz = TackaPredaje(pk, tekstovi, srb);
                if (!izlaz.Id.HasValue || !pk.JeGranicna(izlaz.Id.Value))
                    izlaz = Granica(pk, tekstovi.Concat(new[] { T(c.R13), T(c.R57), r7 }), Zemlja(posle ?? (iKp < 0 && put.Count > 0 ? put[0] : null)), izlaz);
                p.Postavi("Stanica3ID", izlaz, "r57/r13 (predaja sledećem prevozniku)", fajl);
            }

            // platilac za deonicu KP: uvek firma iz dokumenta – iz r7 (zapis za srpsku deonicu ili ceo put; „for KP firma“ = firma plaća
            // deonicu KP); nikad iz najave ili ugovora (odluka 14.11.). Firma koja nije u Partnerji → NULL i kandidati.
            var a = kp == null ? null : Provere.AnalizirajR7(r7, kp);
            var pl = a?.ZaSrbiju.FirstOrDefault(x => !x.Kp);
            if (pl != null) { var pp = pk.Partner(pl.Naziv, null, null, "platilac"); pp.Tekst = PodaciPosla.Skrati(pl.Zapis, 400) + " → „" + pl.Naziv + "“"; p.Postavi("PlatilacID", pp, "r7", fajl); }
            else p.Postavi("PlatilacID", null, PodaciPosla.Skrati(r7, 400), "r7", 0, a?.ZaSrbiju.Any(x => x.Kp) == true ? "KP je naveden kao platilac – nije upisano" : "platilac za deonicu KP nije pronađen u r7", fajl);

            // roba (v2.9.1, tačka 3.1): šifra iz r24, pa r21; bez šifre – naziv robe; više NHM u istom listu → najčešća grupa (Kod4)
            var nhm = NhmSifre(c);
            var izvorNhm = "r24";
            if (nhm.Count == 0) { nhm = NhmIzTeksta(T(c.R21)); izvorNhm = "r21"; }
            Pogodak g1;
            if (nhm.Count > 0)
            {
                var grupe = nhm.GroupBy(Poklapanje.Nhm4).OrderByDescending(g => g.Count()).ToList();
                g1 = grupe.Count == 1 ? pk.Nhm(nhm[0]) : pk.Nhm(grupe[0].Key);
                if (grupe.Count > 1)
                {
                    p.OstaliNhm = string.Join(", ", grupe.Skip(1).Select(g => g.Key + " ×" + g.Count()));
                    g1.Napomena = (g1.Napomena + "; više NHM u listu – grupa " + grupe[0].Key + " ×" + grupe[0].Count() + ", ostali NHM: " + p.OstaliNhm).Trim(' ', ';');
                }
            }
            else
            {
                var naziv = !string.IsNullOrWhiteSpace(c.R21?.OpisRobe) ? c.R21.OpisRobe : "";
                g1 = naziv.Length > 0 ? pk.NhmPoNazivu(naziv) : new Pogodak { Napomena = "r24 prazna, naziv robe nije pročitan" };
                izvorNhm = "r21 (naziv robe)";
            }
            p.Postavi("NHM1", g1, izvorNhm, fajl);
            p.Nhm4 = Poklapanje.Nhm4(pk.NhmRed(g1.Id)?.Kod8 ?? (nhm.Count > 0 ? nhm[0] : ""));
            p.V["NHM2"] = null;
            // mase (v2.9.1, tačka 5.1): sve što postoji, ostalo 0; tara nikad u bruto
            var (tara, neto, bruto, opisMasa) = Mase(c);
            p.Postavi("Neto1", Tone(neto ?? 0), opisMasa, "r25/r21", neto.HasValue ? (opisMasa.Contains("neto izračunat") ? 80 : 90) : 0, "", fajl);
            p.Postavi("Tara1", Tone(tara ?? 0), opisMasa, "r25/r21", tara.HasValue ? (opisMasa.Contains("tara izračunata") ? 80 : 90) : 0, "", fajl);
            p.Postavi("Bruto1", Tone(bruto ?? 0), opisMasa, "r25/r21", bruto.HasValue ? (opisMasa.Contains("bruto izračunat") ? 80 : 90) : 0, "", fajl);
            p.NetoKg = neto; p.TaraKg = tara; p.BrutoKg = bruto;
            foreach (var k in new[] { "Bruto2", "Neto2", "BrojKola2", "Duzina2", "Tara2" }) p.V[k] = null;
            p.Kola = KolaIzLista(c);
            var brojKola = p.Kola.Count > 0 ? p.Kola.Count : Provere.BrojKolaUTekstu(T(c.R21)) is int n21 && n21 > 0 ? n21 : Provere.BrojKolaUTekstu(T(c.R18));
            p.Postavi("BrojKola1", brojKola, PodaciPosla.Skrati(T(c.R18) + " / " + T(c.R21), 300), "r18/r21", brojKola > 0 ? 80 : 0, "", fajl);
            p.Postavi("Duzina1", null, "dužina nije u tovarnom listu", "–", 0, "", fajl);
            var serija = BrojKolaUic.NajcescaOznaka(p.Kola.Select(k => (k, "")));
            p.Postavi("SerijaKola1", serija.Length == 0 ? null : serija, p.Kola.Count == 0 ? "brojevi kola nisu u tovarnom listu" : "iz brojeva kola (UIC 438)", "r18", serija.Length > 0 ? 80 : 0, "", fajl);
            p.V["SerijaKola2"] = null;
            var rid = RidBroj(c);
            p.Postavi("RIDBroj1", rid.Length == 0 ? null : rid, rid.Length == 0 ? "" : rid, "r7", rid.Length > 0 ? 80 : 0, "", fajl);
            var om = OmBroj(c);
            p.Postavi("OMBroj1", om.Length == 0 ? null : om, om, "r21", om.Length > 0 ? 80 : 0, "", fajl);
            foreach (var k in new[] { "RIDBroj2", "OMBroj2" }) p.V[k] = null;
            // prazna kola: CUV ili NHM 9921/9922
            p.Prazna = string.Equals(c.Obrazac, "CUV", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(p.Nhm4, "^992[12]$");

            // ključ (v2.9.1, tačka 5.8): r62; kada r62 nije potpuna – ref:<r8>|<OtpravnaID>|<UputnaID>; inače dosadašnji ključ lista
            p.KljucPosiljke = Kljuc(c, ref8, otp.Id, up.Id);
            p.Postavi("KljucPosiljke", p.KljucPosiljke == null ? null : PodaciPosla.Skrati(p.KljucPosiljke, 200), p.KljucPosiljke ?? "", "r62 / r8+relacija", 100, "", fajl);
            return p;
        }

        /// <summary>Ključ posla: r62 (Ponavljanja.KljucLista), inače „ref:&lt;r8&gt;|&lt;OtpravnaID&gt;|&lt;UputnaID&gt;“, inače dosadašnji ključ lista (bez OCR teksta kada je moguće).</summary>
        internal static string Kljuc(CimList c, string referenca, int? otpravna, int? uputna)
        {
            var k = Ponavljanja.KljucLista(c);
            if (k != null && k.StartsWith("r62:", StringComparison.Ordinal)) return k;
            if (referenca != null && (otpravna.HasValue || uputna.HasValue))
                return "ref:" + Regex.Replace(Slicnost.Osnovno(referenca), @"\s", "") + "|" + (otpravna?.ToString() ?? "") + "|" + (uputna?.ToString() ?? "");
            return k;
        }

        /// <summary>
        /// v2.9.1 (5.8): vrednost r8/r14 samo ako liči na oznaku – slova/cifre/crtice, bar 3 alfanumerička znaka, bez znakova kao ©, |;
        /// iz „il 1 42-0003-26“ → „42-0003-26“. Inače null („©j 520 o 0“, „Bj 5 2 0 1 0 0“, „sie –“).
        /// </summary>
        internal static string Oznaka(string tekst)
        {
            var t = Regex.Replace(tekst ?? "", @"\s+", " ").Trim();
            if (t.Length == 0) return null;
            if (Regex.IsMatch(t, @"[©®|¦§«»°¬~^`´¨\\{}\[\]<>]")) return null;
            var tokeni = t.Split(' ').Select(x => x.Trim('.', ',', ';', ':', '(', ')', '"', '\'', '„', '“', '”')).Where(x => x.Length > 0).ToList();
            bool Dobar(string x) => Regex.IsMatch(x, @"^[\p{L}\d][\p{L}\d\-/\.]*[\p{L}\d]$") && Regex.Matches(x, @"[\p{L}\d]").Count >= 3;
            var dobri = tokeni.Where(Dobar).ToList();
            if (dobri.Count == 0) return null;
            var sum = tokeni.Count(x => x.Length <= 2);
            // OCR šum: većina reči su jedno-dva slova, a nijedna oznaka nema crticu/kosu crtu ili 5+ znakova
            var jaki = dobri.Where(x => Regex.IsMatch(x, @"[\-/]") && Regex.IsMatch(x, @"\d") || x.Length >= 5).ToList();
            if (sum * 2 >= tokeni.Count && jaki.Count == 0) return null;
            // ceo tekst kada je čist („LDC8-HAMMNOV1“, „SAD HER 79“, „Budamar Coal 17“): kratke reči su manjina, sve reči su slova/cifre
            if (sum * 2 < tokeni.Count && tokeni.All(x => Regex.IsMatch(x, @"^[\p{L}\d][\p{L}\d\-/\.]*$")) && t.Length <= 100) return t;
            var izbor = (jaki.Count > 0 ? jaki : dobri).OrderByDescending(x => Regex.IsMatch(x, @"\d") && Regex.IsMatch(x, @"[\-/]")).ThenByDescending(x => x.Length).First();
            return PodaciPosla.Skrati(izbor, 100);
        }

        /// <summary>Broj ponude iz r7: „KP Ponuda 8957“, „Ponuda br. 42-0003-26“, „Offer 8957“.</summary>
        internal static string PonudaIzR7(string r7)
        {
            var m = Regex.Match(r7 ?? "", @"(?i)(?<!\p{L})(?:ponud[aeiu]|offer|angebot|ajánlat)\s*(?:br\.?|broj|no\.?|nr\.?|number)?\s*[:#]?\s*([\p{L}\d][\p{L}\d\-/]{2,30})");
            return m.Success && Regex.IsMatch(m.Groups[1].Value, @"\d") ? Oznaka(m.Groups[1].Value) : null;
        }

        /// <summary>NHM šifre iz teksta (r21, spisak): „993100 TANK KONTEYNER“, „48109200 – Papír“, „NHM 2520 10“.</summary>
        internal static List<string> NhmIzTeksta(string t)
        {
            var l = new List<string>();
            foreach (Match m in Regex.Matches(t ?? "", @"(?i)(?:nhm|hs|code|šifra|sifra)?\s*(?<![\d.,/\-])(\d{4}(?:[ .]?\d{2}){1,2})(?![\d.,/\-])"))
            {
                var d = Regex.Replace(m.Groups[1].Value, @"\D", "");
                if ((d.Length == 6 || d.Length == 8) && !Regex.IsMatch(d, @"^(19|20)\d\d") && !l.Contains(d)) l.Add(d);
            }
            return l;
        }

        private static readonly HashSet<string> SusedneZemlje = new HashSet<string> { "HU", "HR", "RO", "BG", "MK", "BA", "MNE", "AL" };

        /// <summary>
        /// v2.9.1 (5.3): prevoznik iz r57 je na strani deonici susednoj Srbiji – deonica se završava/počinje na graničnom prelazu sa Srbijom
        /// (tmpSinonimi GRANICA), ili je stanica deonice u susednoj zemlji; bez podataka o deonici – prihvata se (kao do sada).
        /// </summary>
        private static bool SusedSrbije(Poklapanje pk, PrevoznikNaPutu x)
        {
            if (string.IsNullOrWhiteSpace(x.Deonica)) return true;
            if (pk.Granica(x.Deonica).Id.HasValue) return true;
            var d = DrzaveDeonice(pk, x);
            return d == null || d.Any(SusedneZemlje.Contains);
        }

        /// <summary>Država deonice prevoznika (stanica iz deonice r57 van Srbije), za izbor partnera i graničnog prelaza.</summary>
        private static string[] DrzaveDeonice(Poklapanje pk, PrevoznikNaPutu x)
        {
            foreach (var t in new[] { x.Od, x.Do })
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                var st = pk.Stanica(t);
                if (st.Id.HasValue && pk.DrzavaStanice(st.Id.Value) is string dz && dz.Length > 0 && dz != "SRB") return new[] { dz };
            }
            return null;
        }

        /// <summary>Granični prelaz iz tekstova (naziv prelaza, granični kod), pa po zemlji susednog prevoznika; inače zadržava raniji pogodak.</summary>
        private static Pogodak Granica(Poklapanje pk, IEnumerable<string> tekstovi, string[] zemlje, Pogodak ranije)
        {
            foreach (var t in tekstovi.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var g = pk.Granica(t);
                if (g.Id.HasValue) return g;
            }
            if (ranije != null && ranije.Id.HasValue) return ranije;
            foreach (var z in zemlje ?? new string[0])
            {
                var g = pk.Granica(null, z);
                if (g.Id.HasValue) return g;
            }
            return ranije ?? new Pogodak { Napomena = "granični prelaz nije pronađen" };
        }

        private static readonly Regex UmRx = new Regex(@"(?i)(?<![\p{L}])u\.?\s?m\.?\s+([\p{L}][\p{L}\s\.\-]{2,40}?)\s*(?:\)|$|-|–|,|\d)", RegexOptions.Compiled);

        /// <summary>Utovarna mesta („ŠID (u.m. KUKUJEVCI)“ → „KUKUJEVCI“) iz zadatih tekstova, redom (OCR može da odseče naziv – pozivalac bira prvo koje daje stanicu).</summary>
        internal static IEnumerable<(string tekst, string izvorniTekst, string rubrika)> UtovarnaMesta(params (string rubrika, string tekst)[] izvori)
        {
            foreach (var (rub, t) in izvori)
            {
                foreach (Match m in UmRx.Matches(t ?? ""))
                    if (m.Groups[1].Value.Trim().Length >= 3) yield return (m.Groups[1].Value.Trim(), PodaciPosla.Skrati((t ?? "").Trim(), 300), rub + " (utovarno mesto)");
                // v2.9.1: mesto u zagradi iza stanice („Beograd Ranžirna (Ostružnica)“) – bez „u.m.“
                foreach (Match m in Regex.Matches(t ?? "", @"\p{L}{3,}\s*\(\s*([\p{L}][\p{L}\s\.\-]{2,40}?)\s*\)"))
                    if (!Regex.IsMatch(m.Groups[1].Value, @"(?i)\bu\.?\s?m\b")) yield return (m.Groups[1].Value.Trim(), PodaciPosla.Skrati((t ?? "").Trim(), 300), rub + " (utovarno mesto u zagradi)");
            }
        }

        /// <summary>Stanica2 = utovarno mesto koje nije otpravna stanica (prvo koje daje stanicu u Srbiji); null ako ga nema.</summary>
        internal static Pogodak Stanica2(Poklapanje pk, int? otpravna, params (string rubrika, string tekst)[] izvori)
        {
            foreach (var um in UtovarnaMesta(izvori))
            {
                var st = pk.Stanica(um.tekst, null, null, false, new[] { "SRB" }, samoDrzave: new[] { "SRB" });
                if (!st.Id.HasValue || st.Id == otpravna) continue;
                st.Tekst = um.izvorniTekst;
                st.Napomena = (st.Napomena.Length > 0 ? st.Napomena + "; " : "") + um.rubrika;
                return st;
            }
            return null;
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

        // v2.9.1 (tačka 5.1): oznake masa na više jezika; reč pre broja određuje vrstu mase
        private const string OznakeNeto = @"neto|netto|netta|neta|nett|ntto|nto|net|n\s?=|wg\.?\s?ladung|ladung|masa\s+inc(?:a|ă)rcat(?:a|ă)|rakom(?:a|á)ny\s+t(?:o|ö)mege|masa\s+robe|masa";
        private const string OznakeTara = @"tara\s+vagona|tara\s+vg\.?|tara|tare|dara|eigengewicht|t\s?=";
        private const string OznakeBruto = @"bruto|brutto|btto|bto|brute|brutt|gross|gesamt(?:gewicht)?|ukupna\s+te(?:z|ž)ina|masa\s+brut(?:a|ă)|elegyt(?:o|ö)meg|g\s?=";
        private static readonly Regex MasaRx = new Regex(@"(?i)(?<![\p{L}])(" + OznakeTara + "|" + OznakeBruto + "|" + OznakeNeto + @"|ukupno|total)\s*[:=.]?\s*(\d(?:[\d.,]|\s(?=\d))*)(\s*(?:kg|t|to|tona|tone|tons?)\b)?", RegexOptions.Compiled);
        private static readonly Regex OznakaRx = new Regex(@"(?i)(?<![\p{L}])(" + OznakeTara + "|" + OznakeBruto + "|" + OznakeNeto + @")(?![\p{L}])", RegexOptions.Compiled);

        /// <summary>Vrsta mase po oznaci: „t“ tara, „b“ bruto, „n“ neto (i bez oznake: masa robe), „“ nepoznato.</summary>
        internal static string VrstaMase(string oznaka)
        {
            var o = Regex.Replace(oznaka ?? "", @"\s+", " ").Trim();
            if (Regex.IsMatch(o, "^(?i)(" + OznakeTara + ")$")) return "t";
            if (Regex.IsMatch(o, "^(?i)(" + OznakeBruto + ")$")) return "b";
            if (Regex.IsMatch(o, "^(?i)(" + OznakeNeto + "|ukupno|total)$")) return "n";
            return "";
        }

        /// <summary>
        /// v2.9.1 (tačka 5.1): tara, neto i bruto (kg) iz r25 i r21 po oznakama (Neto/Netto/Ladung…, Tara/Tare/Dara…, Bruto/Brutto/Gesamt…).
        /// r25 bez oznaka (jedan broj, „Ukupno“, pojedinačne mase) = masa robe → neto (GLV-CIM: bruto masa robe, bez tare kola). Kada
        /// tekst ima oznake, broj bez oznake se ne koristi; vrednost označena kao tara nikad ne ide u bruto. „x.xxx“ / „X kg“ = nije navedeno.
        /// Dve od tri poznate → treća se računa (bruto = neto + tara), opis „izračunato“.
        /// </summary>
        internal static (double? tara, double? neto, double? bruto, string opis) Mase(CimList c)
        {
            double? tara = null, neto = null, bruto = null;
            var tekst = (T(c.R25) + " / " + T(c.R21)).Trim(' ', '/');
            foreach (Match m in MasaRx.Matches(tekst))
            {
                var v = Kg(m.Groups[2].Value + m.Groups[3].Value);
                if (v == null) continue;
                switch (VrstaMase(m.Groups[1].Value))
                {
                    case "t": tara = tara ?? v; break;
                    case "b": bruto = bruto ?? v; break;
                    case "n": neto = neto ?? v; break;
                }
            }
            var saOznakama = OznakaRx.IsMatch(T(c.R25));
            if (!saOznakama)
            {
                var brojevi = new[] { c.R25?.Ukupno }.Concat(c.R25?.Pojedinacne ?? new List<string>()).Select(Kg).Where(x => x.HasValue && x > 0).Select(x => x.Value).Distinct().OrderBy(x => x).ToList();
                if (neto == null && tara == null && bruto == null && brojevi.Count == 3 && Math.Abs(brojevi[0] + brojevi[1] - brojevi[2]) <= 0.01 * brojevi[2])
                { tara = brojevi[0]; neto = brojevi[1]; bruto = brojevi[2]; }
                // r25 bez oznaka: upisani broj je masa robe (neto); tara iz posebnog reda („Tara …“) ostaje tara
                if (neto == null && Kg(c.R25?.Ukupno) is double u && (!(Kg(c.R25?.Tara) is double tt0) || Math.Abs(tt0 - u) > 0.5)) neto = u;
                if (neto == null && (c.R25?.Pojedinacne?.Count ?? 0) > 0)
                {
                    var zbir = c.R25.Pojedinacne.Select(Kg).Where(x => x.HasValue).Sum(x => x.Value);
                    if (zbir > 0) neto = zbir;
                }
            }
            if (tara == null && Kg(c.R25?.Tara) is double tt) tara = tt;
            // provera bruto = tara + neto: pogrešna jedinica bruto mase (×1000) se ispravlja zbirom
            if (bruto.HasValue && tara.HasValue && neto.HasValue && Math.Abs(bruto.Value - (tara.Value + neto.Value)) > 0.01 * (tara.Value + neto.Value)
                && (Math.Abs(bruto.Value * 1000 - (tara.Value + neto.Value)) < 0.01 * (tara.Value + neto.Value) || Math.Abs(bruto.Value / 1000 - (tara.Value + neto.Value)) < 0.01 * (tara.Value + neto.Value)))
                bruto = tara + neto;
            var izr = "";
            if (bruto == null && tara.HasValue && neto.HasValue) { bruto = tara + neto; izr = "bruto izračunat (neto + tara)"; }
            else if (neto == null && tara.HasValue && bruto.HasValue && bruto >= tara) { neto = bruto - tara; izr = "neto izračunat (bruto − tara)"; }
            else if (tara == null && neto.HasValue && bruto.HasValue && bruto >= neto) { tara = bruto - neto; izr = "tara izračunata (bruto − neto)"; }
            var opis = PodaciPosla.Skrati(tekst, 300) + (tara.HasValue || neto.HasValue || bruto.HasValue ? (izr.Length > 0 ? " [" + izr + "]" : "") : " – mase nisu navedene (0)");
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
        internal sealed class GrupaRobe { public string Nhm = ""; public List<KolaNaSpisku> Kola = new List<KolaNaSpisku>(); public string OstaliNhm = ""; }

        /// <summary>Kola spiska po NHM-u (NHM kola, pa zaglavlje spiska, pa NHM posla), najveća grupa prva.</summary>
        internal static List<GrupaRobe> Grupe(SpisakKola s, string nhmPosla)
        {
            // v2.9.1 (3.1.3): intermodalni voz (tovarne jedinice sa različitom robom) – sva natovarena kola su jedna grupa, NHM = grupa (4 cifre)
            // koja se najčešće javlja po jedinicama; ostale šifre u napomenu. Prazna kola (9921/9922) su posebna grupa.
            bool Prazna(KolaNaSpisku k) => k.Prazna || Regex.IsMatch(Regex.Replace(k.Nhm ?? "", @"\D", ""), "^992[12]");
            var pune = s.Kola.Where(k => !Prazna(k)).ToList();
            var jedinice = s.NhmJedinica.Count > 0 ? s.NhmJedinica : pune.Where(k => k.Uti.Count > 0).Select(k => k.Nhm).Where(x => x.Length > 0).ToList();
            if (pune.Count > 0 && jedinice.Count > 0 && (jedinice.Count > pune.Count || pune.Any(k => k.Uti.Count > 0)) && jedinice.Select(Poklapanje.Nhm4).Distinct().Count() > 1)
            {
                var poGrupi = jedinice.GroupBy(Poklapanje.Nhm4).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).ToList();
                var l = new List<GrupaRobe> { new GrupaRobe { Nhm = poGrupi[0].Key, Kola = pune, OstaliNhm = string.Join(", ", poGrupi.Skip(1).Select(g => g.Key + " ×" + g.Count())) } };
                var pr = s.Kola.Where(Prazna).ToList();
                if (pr.Count > 0) l.Add(new GrupaRobe { Nhm = "992200", Kola = pr });
                return l;
            }
            return s.Kola.GroupBy(k => Poklapanje.Nhm4(k.Nhm.Length > 0 ? k.Nhm : s.NhmZaglavlje.Length > 0 ? s.NhmZaglavlje : nhmPosla ?? ""))
                .Select(g => new GrupaRobe { Nhm = g.First().Nhm.Length > 0 ? g.First().Nhm : s.NhmZaglavlje.Length > 0 ? s.NhmZaglavlje : nhmPosla ?? "", Kola = g.ToList() })
                .OrderByDescending(g => g.Kola.Count).ToList();
        }

        /// <summary>
        /// Vrednosti posla sa spiska (BrojKola, Neto, Tara, Bruto, Duzina, SerijaKola, NHM, OMBroj; spisak ima prednost nad CIM-om).
        /// v2.9.1 (5.1): mase u tonama po kolonama spiska – tara (zbir kada je ima za skoro sva kola), neto (kada je ima), bruto (bruto kola,
        /// ili tara + neto); dve od tri poznate → treća se računa; prazna kola: neto 0. Spisak samo sa tarom (kola se tek tovare) → Tara = zbir,
        /// Neto 0, Bruto 0. Red „Ukupno / Insgesamt“ (zbir sa spiska): zbir stavki mora da se slaže (± 0,5 %), inače se koristi „Ukupno“, a
        /// stavke se označavaju za ručnu proveru (napomena). Dužina: zbir dužina kola, ili dužina voza sa spiska.
        /// </summary>
        internal static PodaciPosla IzSpiska(SpisakKola s, Poklapanje pk, string nhmPosla, string fajl)
        {
            var p = new PodaciPosla();
            var grupe = Grupe(s, nhmPosla);
            var sr = CultureInfo.GetCultureInfo("sr-Latn-RS");
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
                if (g.OstaliNhm.Length > 0)
                {
                    nh.Napomena = (nh.Napomena + "; intermodalni voz – grupa " + Poklapanje.Nhm4(g.Nhm) + " najčešća po jedinicama; ostali NHM: " + g.OstaliNhm).Trim(' ', ';');
                    p.Napomene.Add("Ostali NHM (po tovarnim jedinicama): " + g.OstaliNhm);
                }
                p.Postavi("NHM" + n, nh, "spisak", fajl);
                var prazna = g.Kola.All(k => k.Prazna || Regex.IsMatch(Regex.Replace(k.Nhm ?? "", @"\D", ""), "^992[12]"));
                int bk = g.Kola.Count;
                double? Zbir(Func<KolaNaSpisku, double?> f, double udeo) => g.Kola.Count(k => f(k).HasValue) >= udeo * bk && g.Kola.Any(k => f(k).HasValue) ? g.Kola.Sum(k => f(k) ?? 0) : (double?)null;
                double? tara = Zbir(k => k.TaraKg, 0.9);
                double? neto = Zbir(k => k.NetoKg, 0.0001);
                double? bruto = Zbir(k => k.BrutoKg ?? (k.TaraKg.HasValue && k.NetoKg.HasValue ? k.TaraKg + k.NetoKg : null), 0.9);
                // prazna kola (Bor – Zabrze, CIT 23 praznih kola): jedina masa na spisku je tara kola
                if (prazna && neto > 0 && tara == null && (bruto == null || Math.Abs(bruto.Value - neto.Value) < 1)) { tara = neto; bruto = neto; neto = 0; }
                if (prazna && neto == null) neto = 0;
                var opis = new List<string>();
                // red „Ukupno“ (zbir sa spiska): samo kada spisak ima jednu grupu
                if (grupe.Count == 1)
                {
                    double? Uk(double? zbir, double? ukupno, string ime)
                    {
                        if (!ukupno.HasValue || ukupno <= 0) return zbir;
                        if (!zbir.HasValue) { opis.Add(ime + " iz reda „Ukupno“"); return ukupno; }
                        if (Math.Abs(zbir.Value - ukupno.Value) > 0.005 * ukupno.Value)
                        {
                            opis.Add(ime + ": zbir stavki " + (zbir.Value / 1000).ToString("N3", sr) + " t ≠ red „Ukupno“ " + (ukupno.Value / 1000).ToString("N3", sr) + " t – upisan „Ukupno“, stavke za ručnu proveru");
                            p.Napomene.Add("Spisak kola: " + opis.Last());
                            return ukupno;
                        }
                        return zbir;
                    }
                    tara = Uk(tara, s.UkupnoTaraKg, "tara"); neto = Uk(neto, s.UkupnoNetoKg, "neto"); bruto = Uk(bruto, s.UkupnoBrutoKg, "bruto");
                }
                if (bruto == null && tara.HasValue && neto.HasValue) { bruto = tara + neto; opis.Add("bruto izračunat (neto + tara)"); }
                else if (neto == null && tara.HasValue && bruto.HasValue && bruto >= tara) { neto = bruto - tara; opis.Add("neto izračunat (bruto − tara)"); }
                else if (tara == null && neto.HasValue && bruto.HasValue && bruto >= neto) { tara = bruto - neto; opis.Add("tara izračunata (bruto − neto)"); }
                var imaMase = tara.HasValue || neto.HasValue || bruto.HasValue;
                // spisak bez masa ne briše mase tovarnog lista
                if (imaMase)
                {
                    string Tx(double? kg) => kg.HasValue ? (kg.Value / 1000).ToString("N3", sr) + " t" : "nije navedeno (0)";
                    var tekst = "spisak kola: " + bk + " kola; neto " + Tx(neto) + ", tara " + Tx(tara) + ", bruto " + Tx(bruto) + (opis.Count > 0 ? " [" + string.Join("; ", opis) + "]" : "");
                    // samo mase koje spisak ima (ili izračunate): ostale ostaju iz tovarnog lista (ili 0)
                    if (neto.HasValue) p.Postavi("Neto" + n, Tone(neto.Value), tekst, "spisak", 95, "", fajl);
                    if (tara.HasValue) p.Postavi("Tara" + n, Tone(tara.Value), tekst, "spisak", 95, "", fajl);
                    if (bruto.HasValue) p.Postavi("Bruto" + n, Tone(bruto.Value), tekst, "spisak", 95, "", fajl);
                }
                p.Postavi("BrojKola" + n, bk, "spisak kola: " + bk + " kola", "spisak", 100, "", fajl);
                // dužina: dokument (po kolima ili dužina voza) ima prednost nad istorijom (dbo.TeretnicaStavke); bez dužine bar jednih kola → NULL
                var bezDuz = g.Kola.Count(k => !k.DuzinaM.HasValue);
                var izIst = g.Kola.Count(k => k.DuzinaM.HasValue && k.DuzinaIzvor == "istorija");
                var duz = g.Kola.Sum(k => k.DuzinaM ?? 0);
                var opisDuz = (bk - bezDuz - izIst) + " kola sa spiska, " + izIst + " iz istorije (dbo.TeretnicaStavke), " + bezDuz + " bez dužine";
                if (grupe.Count == 1 && s.DuzinaVozaM is double dv && dv > 0 && (izIst > 0 || bezDuz > 0))
                    p.Postavi("Duzina" + n, Math.Round((decimal)dv, 2), "dužina voza sa spiska " + dv.ToString("0.0", CultureInfo.InvariantCulture) + " m (" + opisDuz + ")", "spisak", 95, "", fajl);
                else if (bezDuz == 0) p.Postavi("Duzina" + n, Math.Round((decimal)duz, 2), "zbir dužina " + duz.ToString("0.00", CultureInfo.InvariantCulture) + " m: " + opisDuz, izIst > 0 ? "istorija" : "spisak", izIst > 0 ? 80 : 95, "", fajl);
                else p.Postavi("Duzina" + n, null, "dužina nije poznata za sva kola: " + opisDuz, "spisak", 0, "", fajl);
                var ser = BrojKolaUic.NajcescaOznaka(g.Kola.Select(k => (k.Broj, k.Serija)));
                p.Postavi("SerijaKola" + n, ser.Length == 0 ? null : ser, g.Kola.Any(k => k.Serija.Length > 0) ? "serija sa spiska" : "iz brojeva kola (UIC 438)", "spisak", ser.Length > 0 ? 85 : 0, "", fajl);
                var om = string.Join(", ", g.Kola.Select(k => k.KemlerUn).Where(x => x.Length > 0).Distinct());
                p.Postavi("OMBroj" + n, om.Length == 0 ? null : PodaciPosla.Skrati(om, 100), om, "spisak", om.Length > 0 ? 90 : 0, "", fajl);
                p.Grupe.Add((g.Nhm, prazna, g.Kola.Select(k => k.Broj).ToList()));
            }
            return p;
        }
    }
}
