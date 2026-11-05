using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class MestoRegistra
    {
        internal int RB;
        internal int Red;
        internal string NazivOriginal;
        internal string Naziv;
        internal string UIC;
        internal string VrstaMesta;
        internal int? VrstaKod;
        internal int? SifraIZS;
        internal string Kljuc;
        internal bool Inostranstvo;
        internal bool Granicna;
        internal bool KiM;
        internal int? StacionazaM;
        internal int? RastojanjeOdPrethodnogM;
        internal int? RastojanjeDoSledecegM;
        internal byte? Kolosecnost;
        internal string Kategorija;
        internal string Klasa;
        internal int? VmaxDesni;
        internal int? VmaxLevi;
        internal int? MaxDuzinaVozaAB;
        internal string KoloseciAB;
        internal int? MaxDuzinaVozaBA;
        internal string KoloseciBA;
        internal string NacinRegulisanja;
        internal string NacinOsiguranja;
        internal string Vaga;
        internal string Rampa;
        internal string Posednutost;
        internal string OtvorenoZa;
        internal int? MinPoluprecnikM;
        internal decimal? NagibStanice;
        internal decimal? MerodavniUspon;
        internal decimal? MerodavniPad;
        internal decimal? MerodavniOtporUspon;
        internal decimal? MerodavniOtporPad;
        internal string TovarniProfil;
        internal decimal? NadmorskaVisina;
        internal bool Aktivno = true;
        internal bool JeOznaka;
        internal int? StanicaID;
        internal string PovezanoPo;
        internal string Promena;
        internal string OpisPromene;
    }

    internal sealed class PrugaRegistra
    {
        internal int RedniBroj;
        internal string NazivCir;
        internal string Naziv;
        internal string Kategorija;
        internal int? IzvorniID;
        internal string IdentBroj;
        internal bool? Elektrificirana;
        internal string DboNaziv;
        internal string Sifra;
        internal bool KiM;
        internal readonly List<MestoRegistra> Mesta = new List<MestoRegistra>();

        // Redni brojevi kreću iznova u svakoj kategoriji.
        internal string Kljuc { get { return Kategorija + " " + RedniBroj; } }
    }

    internal sealed class SadrzajRegistra
    {
        internal readonly List<PrugaRegistra> Pruge = new List<PrugaRegistra>();
        internal readonly List<string> Problemi = new List<string>();
        internal int BrojRedova;
    }

    // Pravilo 8.12: list Sheet1; zbog spojenih ćelija naziv pruge se ponavlja i u koloni F, pa se redovi pruge prepoznaju pre mesta.
    internal static class RegistarCitac
    {
        private static readonly int B = XlsxCitac.Kolona("B"), E = XlsxCitac.Kolona("E"), F = XlsxCitac.Kolona("F"), G = XlsxCitac.Kolona("G"),
            H = XlsxCitac.Kolona("H"), I = XlsxCitac.Kolona("I"), J = XlsxCitac.Kolona("J"), K = XlsxCitac.Kolona("K"), L = XlsxCitac.Kolona("L"),
            M = XlsxCitac.Kolona("M"), N = XlsxCitac.Kolona("N"), O = XlsxCitac.Kolona("O"), P = XlsxCitac.Kolona("P"), Q = XlsxCitac.Kolona("Q"),
            R = XlsxCitac.Kolona("R"), S = XlsxCitac.Kolona("S"), T = XlsxCitac.Kolona("T"), U = XlsxCitac.Kolona("U"), V = XlsxCitac.Kolona("V"),
            W = XlsxCitac.Kolona("W"), X = XlsxCitac.Kolona("X"), Y = XlsxCitac.Kolona("Y"), Z = XlsxCitac.Kolona("Z"), AA = XlsxCitac.Kolona("AA"),
            AB = XlsxCitac.Kolona("AB"), AC = XlsxCitac.Kolona("AC"), AD = XlsxCitac.Kolona("AD"), AE = XlsxCitac.Kolona("AE");

        internal const int PrviRedPodataka = 5;
        private const int NajvecaRazlikaM = 2000;
        private const int NajvecaDeonicaM = 50000;
        private static readonly int D = XlsxCitac.Kolona("D");

        internal static SadrzajRegistra Procitaj(Stream izvor)
        {
            XlsxList list = XlsxCitac.Procitaj(izvor, "Sheet1");
            var rezultat = new SadrzajRegistra();
            string kategorija = null;
            PrugaRegistra pruga = null;
            for (int r = PrviRedPodataka; r <= list.PoslednjiRed; r++)
            {
                if (!list.Redovi.ContainsKey(r)) continue;
                string b = list.Celija(r, B).Trim();
                // Legenda: odavde više nema podataka.
                if (b.StartsWith("*)") && b.Contains("Косова")) break;
                rezultat.BrojRedova++;

                string kat = Kategorija(b);
                if (kat != null)
                {
                    kategorija = kat;
                    continue;
                }
                // Datum u koloni B ("01.10.1928.") nije red pruge: posle rednog broja mora doći tekst.
                Match mp = Regex.Match(b, @"^(\d+)\.\s*(\D.*)$");
                if (mp.Success)
                {
                    string nazivCir = Regex.Replace(mp.Groups[2].Value, @"\s+", " ").Trim();
                    pruga = new PrugaRegistra
                    {
                        RedniBroj = int.Parse(mp.Groups[1].Value, CultureInfo.InvariantCulture),
                        NazivCir = nazivCir,
                        Naziv = Latinica.IzCirilice(nazivCir),
                        Kategorija = kategorija,
                        Sifra = SifraPruge(kategorija, int.Parse(mp.Groups[1].Value, CultureInfo.InvariantCulture)),
                        KiM = nazivCir.EndsWith("*)")
                    };
                    rezultat.Pruge.Add(pruga);
                    continue;
                }
                string f = list.Celija(r, F).Trim();
                if (f.Length == 0) continue;
                if (pruga == null)
                {
                    rezultat.Problemi.Add("Red " + r + ": mesto „" + f + "“ je pre prve pruge.");
                    continue;
                }
                MestoRegistra mesto = Mesto(list, r, f, pruga.Mesta.Count + 1, rezultat.Problemi);
                // Sve granice se u Registru zovu isto; jedna zajednička stanica bi u grafu spojila nepovezane pruge.
                if (mesto.VrstaMesta == "državna granica")
                    mesto.Naziv = Skrati(mesto.Naziv + " (" + (mesto.UIC ?? pruga.Kljuc) + ")", 150);
                pruga.Mesta.Add(mesto);
            }
            foreach (PrugaRegistra p in rezultat.Pruge)
            {
                for (int i = 0; i + 1 < p.Mesta.Count; i++)
                    p.Mesta[i].RastojanjeDoSledecegM = Rastojanje(p, p.Mesta[i], p.Mesta[i + 1], rezultat.Problemi);
                // Pruge označene sa *) (Kosovo i Metohija) u Registru nemaju redove mesta.
                if (p.Mesta.Count == 0 && !p.NazivCir.EndsWith("*)")) rezultat.Problemi.Add("Pruga " + p.Kljuc + " nema nijedno mesto.");
                OznaciPoPruzi(p);
            }
            return rezultat;
        }

        private static string SifraPruge(string kategorija, int redniBroj)
        {
            switch (kategorija)
            {
                case "MAGISTRALNA": return "M" + redniBroj;
                case "REGIONALNA": return "R" + redniBroj;
                case "LOKALNA": return "L" + redniBroj;
                case "MANIPULATIVNA": return "MP" + redniBroj;
                default: return null;
            }
        }

        // Ključ mesta, KiM, granična stanica i mesta iza državne granice unutar iste pruge.
        private static void OznaciPoPruzi(PrugaRegistra p)
        {
            bool izaLinije = false, izaGranice = false;
            for (int i = 0; i < p.Mesta.Count; i++)
            {
                MestoRegistra m = p.Mesta[i];
                bool granica = m.VrstaKod == 15 && m.NazivOriginal.Contains("ДРЖАВНА");
                bool linija = m.VrstaKod == 15 && m.NazivOriginal.Contains("АДМИНИСТРАТИВНА");
                if (m.JeOznaka || m.NazivOriginal.Contains("ПОЧЕТАК ПРУГЕ")) m.JeOznaka = true;
                m.Inostranstvo = izaGranice;
                m.KiM = p.KiM || izaLinije;
                if (linija) izaLinije = true;
                m.Kljuc = m.JeOznaka || m.Inostranstvo ? null
                    : m.VrstaKod == 15 ? "GR:" + p.Sifra + ":" + m.RB
                    : NaziviRegistra.Kljuc(m.SifraIZS, m.NazivOriginal);
                if (!granica) continue;
                OznaciGranicnu(p, i, -1);
                OznaciGranicnu(p, i, 1);
                // Mesta iza granice su samo u pruzi koja se završava u inostranstvu.
                if (i > 0 && i < p.Mesta.Count - 1 && p.Mesta.Skip(i + 1).Any(x => !x.JeOznaka)) izaGranice = true;
            }
        }

        // Granična je prva stanica ili ukrsnica od reda sa državnom granicom, u oba smera pruge.
        private static void OznaciGranicnu(PrugaRegistra p, int granica, int korak)
        {
            for (int j = granica + korak; j >= 0 && j < p.Mesta.Count; j += korak)
            {
                if (p.Mesta[j].VrstaKod == 15) return;
                if (p.Mesta[j].VrstaKod == 1 || p.Mesta[j].VrstaKod == 2) { p.Mesta[j].Granicna = true; return; }
            }
        }

        // Stacionaža na nekim mestima kreće iznova; tada važi kolona D (rastojanje od prethodnog mesta).
        private static int? Rastojanje(PrugaRegistra p, MestoRegistra a, MestoRegistra b, List<string> problemi)
        {
            if (!a.StacionazaM.HasValue || !b.StacionazaM.HasValue) return b.RastojanjeOdPrethodnogM;
            int razlika = Math.Abs(b.StacionazaM.Value - a.StacionazaM.Value);
            int? d = b.RastojanjeOdPrethodnogM;
            if (d.HasValue && d.Value > 0 && Math.Abs(razlika - d.Value) > NajvecaRazlikaM)
            {
                problemi.Add("Pruga " + p.Kljuc + ", " + a.Naziv + " – " + b.Naziv + ": stacionaža daje " + razlika + " m, a kolona D " + d.Value + " m; koristi se kolona D.");
                return d;
            }
            if (!d.HasValue && razlika > NajvecaDeonicaM)
            {
                problemi.Add("Pruga " + p.Kljuc + ", " + a.Naziv + " – " + b.Naziv + ": stacionaža daje " + razlika + " m; veza se ne pravi dok se podatak ne proveri.");
                return null;
            }
            return razlika;
        }

        private static string Kategorija(string b)
        {
            if (b.StartsWith("МАГИСТРАЛНЕ")) return "MAGISTRALNA";
            if (b.StartsWith("РЕГИОНАЛНЕ")) return "REGIONALNA";
            if (b.StartsWith("ЛОКАЛНЕ")) return "LOKALNA";
            if (b.StartsWith("МАНИПУЛАТИВНЕ")) return "MANIPULATIVNA";
            return null;
        }

        // Tekst o stanju pruge je spojen preko kolona M–AE i nije podatak tih kolona.
        private static bool JeStanjePruge(string s)
        {
            return s != null && (s.Contains("ПРУГА ЗАТВОРЕНА") || s.Contains("ПРУГА ВАН ЕКСПЛОАТАЦИЈЕ"));
        }

        private static MestoRegistra Mesto(XlsxList l, int r, string naziv, int rb, List<string> problemi)
        {
            Func<int, string> c = k =>
            {
                string v = Prazno(l.Celija(r, k));
                return JeStanjePruge(v) ? null : v;
            };
            string orig = NaziviRegistra.UjednaciCirilicu(Regex.Replace(naziv, @"\s+", " ").Trim());
            // Kilometarska oznaka nije službeno mesto, a „Rasputnica Km 2+290“ jeste.
            bool oznaka = orig.Contains("КРАЈ ПРУГЕ") || Regex.IsMatch(orig, @"^(КМ\s*)?\d+\+\d+(\s+ПБ)?$", RegexOptions.IgnoreCase);
            bool zatvorena = JeStanjePruge(orig) || JeStanjePruge(l.Celija(r, M)) || JeStanjePruge(l.Celija(r, S));
            string h = c(H);
            string sifraVrste = Regex.Replace(c(G) ?? "", @"\D", "");
            var m = new MestoRegistra
            {
                RB = rb,
                Red = r,
                NazivOriginal = Skrati(orig, 150),
                Naziv = Skrati(LepNaziv(orig), 150),
                UIC = Skrati(c(S), 10),
                VrstaMesta = oznaka ? "oznaka" : RegistarSifre.Vrsta(sifraVrste),
                VrstaKod = Ceo(sifraVrste),
                SifraIZS = NaziviRegistra.Sifra(c(S)),
                StacionazaM = Ceo(c(E)),
                RastojanjeOdPrethodnogM = Regex.IsMatch(c(D) ?? "", @"^\d+$") ? Ceo(c(D)) : null,
                Kolosecnost = h == "Ј" || h == "J" ? (byte?)1 : h == "Д" || h == "D" ? (byte?)2 : null,
                Kategorija = Skrati(Latinica.IzCirilice(c(I)), 10),
                Klasa = Skrati(Latinica.IzCirilice(c(J)), 10),
                VmaxDesni = Ceo(c(K)),
                VmaxLevi = Ceo(c(L)),
                MaxDuzinaVozaAB = Ceo(c(M)),
                KoloseciAB = Skrati(c(N), 30),
                MaxDuzinaVozaBA = Ceo(c(O)),
                KoloseciBA = Skrati(c(P), 30),
                NacinRegulisanja = Skrati(Latinica.IzCirilice(c(Q)), 50),
                NacinOsiguranja = Skrati(RegistarSifre.Osiguranje(c(R)) ?? Latinica.IzCirilice(c(R)), 50),
                Vaga = Skrati(Latinica.IzCirilice(c(T)), 20),
                Rampa = Skrati(Latinica.IzCirilice(c(U)), 20),
                Posednutost = Skrati(Latinica.IzCirilice(c(V)), 10),
                OtvorenoZa = Skrati(Latinica.IzCirilice(c(W)), 10),
                MinPoluprecnikM = Ceo(c(X)),
                NagibStanice = Dec(c(Y)),
                MerodavniUspon = Dec(c(Z)),
                MerodavniPad = Dec(c(AA)),
                MerodavniOtporUspon = Dec(c(AC)),
                MerodavniOtporPad = Dec(c(AB)),
                TovarniProfil = Skrati(Latinica.IzCirilice(c(AD)), 20),
                NadmorskaVisina = Dec(c(AE)),
                Aktivno = !zatvorena,
                JeOznaka = oznaka
            };
            if (m.VrstaMesta == null && !m.JeOznaka && c(G) != null)
                problemi.Add("Red " + r + " („" + m.Naziv + "“): nepoznata vrsta „" + c(G) + "“.");
            return m;
        }

        // Nazivi su u Registru pisani velikim slovima.
        internal static string LepNaziv(string cirilica)
        {
            string lat = Latinica.IzCirilice(cirilica ?? "");
            bool velika = lat.Any(char.IsLetter) && !lat.Any(char.IsLower);
            return velika ? CultureInfo.GetCultureInfo("sr-Latn-RS").TextInfo.ToTitleCase(lat.ToLower(CultureInfo.GetCultureInfo("sr-Latn-RS"))) : lat;
        }

        private static string Prazno(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private static string Skrati(string s, int n)
        {
            return s != null && s.Length > n ? s.Substring(0, n) : s;
        }

        // Decimalni zarez postaje tačka; broj sa dodatkom ("361*") se uzima bez dodatka.
        internal static decimal? Dec(string s)
        {
            string t = (s ?? "").Trim().Replace(',', '.');
            if (t.Length == 0) return null;
            decimal v;
            if (decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            Match m = Regex.Match(t, @"-?\d+(\.\d+)?");
            return m.Success && decimal.TryParse(m.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : (decimal?)null;
        }

        private static int? Ceo(string s)
        {
            decimal? d = Dec(s);
            return d.HasValue && Math.Abs(d.Value) < int.MaxValue ? (int?)Math.Round(d.Value) : null;
        }
    }

    // Pravilo 8.13: redosled je naziv pa UIC, a ostalo se povezuje ručno.
    internal static class PovezivanjeRegistra
    {
        internal sealed class StanicaZaVezu
        {
            internal int ID;
            internal string Naziv;
            internal string UIC;
            internal int? SifraIZS;
            internal string RegKljuc;
            internal string NazivRegistar;
            internal string AltNazivi;
        }

        // Mesto koje je već usklađeno sa Registrom nalazi se po šifri ili nazivu iz Registra.
        private static StanicaZaVezu PoRegistru(MestoRegistra m, List<StanicaZaVezu> uskladjene, Dictionary<string, StanicaZaVezu> poKljucu)
        {
            StanicaZaVezu s;
            if (m.Kljuc != null && poKljucu.TryGetValue(m.Kljuc, out s)) return s;
            if (m.SifraIZS.HasValue && m.SifraIZS.Value != NaziviRegistra.DeljenaSifra)
            {
                s = uskladjene.FirstOrDefault(x => x.SifraIZS == m.SifraIZS);
                if (s != null) return s;
            }
            string n = NaziviRegistra.Normalizuj(m.NazivOriginal);
            if (n.Length == 0) return null;
            return uskladjene.FirstOrDefault(x => NaziviRegistra.Normalizuj(x.NazivRegistar) == n
                || (x.AltNazivi ?? "").Split(';').Any(a => NaziviRegistra.Normalizuj(a) == n));
        }

        internal static void PoveziMesta(SadrzajRegistra sadrzaj, IList<StanicaZaVezu> stanice)
        {
            List<StanicaZaVezu> uskladjene = stanice.Where(s => s.RegKljuc != null).ToList();
            var poKljucu = new Dictionary<string, StanicaZaVezu>();
            foreach (StanicaZaVezu s in uskladjene) if (!poKljucu.ContainsKey(s.RegKljuc)) poKljucu[s.RegKljuc] = s;
            var poNazivu = stanice.GroupBy(s => NaziviStanica.Normalizuj(s.Naziv)).Where(g => g.Key.Length > 0).ToDictionary(g => g.Key, g => g.ToList());
            var poUic = stanice.Where(s => !string.IsNullOrWhiteSpace(s.UIC)).GroupBy(s => s.UIC.Trim()).ToDictionary(g => g.Key, g => g.First());
            foreach (PrugaRegistra p in sadrzaj.Pruge)
                foreach (MestoRegistra m in p.Mesta)
                {
                    if (m.JeOznaka || m.Inostranstvo) continue;
                    // Državne granice se razlikuju po šifri ili po pruzi, ne po nazivu.
                    StanicaZaVezu poRegistru = m.VrstaKod == 15 && !m.SifraIZS.HasValue && m.Kljuc != null && !poKljucu.ContainsKey(m.Kljuc)
                        ? uskladjene.FirstOrDefault(x => x.RegKljuc != null && x.RegKljuc.StartsWith("GR:" + p.Sifra + ":"))
                        : PoRegistru(m, uskladjene, poKljucu);
                    if (poRegistru != null)
                    {
                        m.StanicaID = poRegistru.ID;
                        m.PovezanoPo = "REGISTAR";
                        m.Kljuc = poRegistru.RegKljuc;
                        continue;
                    }
                    List<StanicaZaVezu> kandidati;
                    if (poNazivu.TryGetValue(NaziviStanica.Normalizuj(m.Naziv), out kandidati))
                    {
                        // Kod više stanica istog naziva prednost ima ona sa istim UIC-om.
                        StanicaZaVezu izbor = kandidati.FirstOrDefault(k => !string.IsNullOrEmpty(m.UIC) && k.UIC == m.UIC) ?? kandidati[0];
                        m.StanicaID = izbor.ID;
                        m.PovezanoPo = "NAZIV";
                        if (!string.IsNullOrEmpty(m.UIC) && !string.IsNullOrEmpty(izbor.UIC) && izbor.UIC != m.UIC)
                            sadrzaj.Problemi.Add("Pruga " + p.Kljuc + ", " + m.Naziv + ": povezano po nazivu, a UIC se razlikuje (" + m.UIC + " / " + izbor.UIC + ").");
                        continue;
                    }
                    StanicaZaVezu poSifri;
                    if (!string.IsNullOrWhiteSpace(m.UIC) && poUic.TryGetValue(m.UIC.Trim(), out poSifri))
                    {
                        m.StanicaID = poSifri.ID;
                        m.PovezanoPo = "UIC";
                    }
                }
        }

        private const double NajmanjaOcenaPruge = 0.6;

        internal sealed class PrugaZaVezu
        {
            internal int IzvorniID;
            internal string Naziv;
            internal string IdentBroj;
            internal bool? Elektrificirana;
        }

        internal static void PoveziPruge(SadrzajRegistra sadrzaj, IList<PrugaZaVezu> stare)
        {
            foreach (PrugaRegistra p in sadrzaj.Pruge)
            {
                PrugaZaVezu najbolja = null;
                // Slabija sličnost bi povezala pogrešnu prugu, a preko nje idu kvalifikacije i elektrifikacija.
                double ocena = NajmanjaOcenaPruge - 0.001;
                foreach (PrugaZaVezu s in stare)
                {
                    double o = NaziviStanica.Oceni(p.Naziv, new KandidatStanice { Naziv = s.Naziv });
                    if (o > ocena) { ocena = o; najbolja = s; }
                }
                if (najbolja == null) continue;
                p.IzvorniID = najbolja.IzvorniID;
                p.IdentBroj = najbolja.IdentBroj;
                p.Elektrificirana = najbolja.Elektrificirana;
                p.DboNaziv = najbolja.Naziv;
            }
        }

        // Isti naziv se može ponoviti na pruzi, pa ključ ima i redni broj pojavljivanja.
        private static string KljucMesta(Dictionary<string, int> pojavljivanja, string pruga, string naziv)
        {
            string k = pruga + "|" + NaziviStanica.Normalizuj(naziv);
            int n;
            pojavljivanja.TryGetValue(k, out n);
            pojavljivanja[k] = ++n;
            return k + "|" + n;
        }

        // Razlike prema važećem Registru.
        internal static List<string> Razlike(SadrzajRegistra novi, IList<MestoZaPoredjenje> vazeca)
        {
            var opisi = new List<string>();
            var uStarom = new Dictionary<string, int>();
            var stara = new Dictionary<string, MestoZaPoredjenje>();
            foreach (MestoZaPoredjenje v in vazeca) stara[KljucMesta(uStarom, v.Pruga, v.Naziv)] = v;
            var uNovom = new Dictionary<string, int>();
            var nadjeni = new HashSet<string>();
            foreach (PrugaRegistra p in novi.Pruge)
                foreach (MestoRegistra m in p.Mesta)
                {
                    string kljuc = KljucMesta(uNovom, p.Kljuc, m.Naziv);
                    nadjeni.Add(kljuc);
                    MestoZaPoredjenje s;
                    if (!stara.TryGetValue(kljuc, out s))
                    {
                        if (vazeca.Count == 0) continue;
                        m.Promena = "NOVO";
                        opisi.Add("Pruga " + p.Kljuc + ", " + m.Naziv + ": novo mesto.");
                        continue;
                    }
                    var d = new List<string>();
                    if (s.Klasa != m.Klasa) d.Add("Klasa " + s.Klasa + " → " + m.Klasa);
                    if (s.VmaxDesni != m.VmaxDesni) d.Add("Vmax desni " + s.VmaxDesni + " → " + m.VmaxDesni);
                    if (s.VmaxLevi != m.VmaxLevi) d.Add("Vmax levi " + s.VmaxLevi + " → " + m.VmaxLevi);
                    if (s.MaxDuzinaVozaAB != m.MaxDuzinaVozaAB) d.Add("Dužina A→B " + s.MaxDuzinaVozaAB + " → " + m.MaxDuzinaVozaAB);
                    if (s.MaxDuzinaVozaBA != m.MaxDuzinaVozaBA) d.Add("Dužina B→A " + s.MaxDuzinaVozaBA + " → " + m.MaxDuzinaVozaBA);
                    if (s.Aktivno != m.Aktivno) d.Add(m.Aktivno ? "Pruga ponovo otvorena" : "Pruga zatvorena");
                    if (d.Count == 0) continue;
                    m.Promena = "IZMENJENO";
                    m.OpisPromene = string.Join("; ", d);
                    if (m.OpisPromene.Length > 400) m.OpisPromene = m.OpisPromene.Substring(0, 400);
                    opisi.Add("Pruga " + p.Kljuc + ", " + m.Naziv + ": " + m.OpisPromene);
                }
            foreach (KeyValuePair<string, MestoZaPoredjenje> s in stara)
                if (!nadjeni.Contains(s.Key))
                    opisi.Add("Pruga " + s.Value.Pruga + ", " + s.Value.Naziv + ": nema ga u datoteci.");
            return opisi;
        }
    }

    internal sealed class MestoZaPoredjenje
    {
        internal string Pruga;
        internal string Naziv;
        internal bool Aktivno;
        internal string Klasa;
        internal int? VmaxDesni;
        internal int? VmaxLevi;
        internal int? MaxDuzinaVozaAB;
        internal int? MaxDuzinaVozaBA;
    }
}
