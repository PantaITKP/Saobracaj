using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Rečnik naziva rubrika (sablon\cim_sablon.json) i raspoređivanje reči po rubrikama: za stranu se bira obrazac
    /// (sablon\obrasci\*.json) sa najviše saglasnih sidara, strana se poravna na njega, pa svaka reč ide u rubriku u
    /// kojoj joj je CENTAR. Odštampani nazivi rubrika se ne računaju kao podatak. Ako obrazac nije prepoznat ili
    /// poravnanje nije pouzdano, rubrike se ne dodeljuju (ručna provera).
    /// </summary>
    internal sealed class CimSablon
    {
        public string Verzija { get; private set; }
        public string Hes { get; private set; }
        private readonly Dictionary<string, (double x, double y)> sidra = new Dictionary<string, (double, double)>();
        private readonly HashSet<string> recnik = new HashSet<string>();
        public ObrasciSkup Obrasci { get; private set; } = new ObrasciSkup();

        public static CimSablon Ucitaj(string putanja, string obrasciFolder = null, PragoviPoravnanja prag = null)
        {
            var tekst = File.ReadAllText(putanja, Encoding.UTF8);
            var j = JObject.Parse(tekst);
            var s = new CimSablon { Verzija = (string)j["verzija"] ?? "?" };
            foreach (var a in (JArray)j["sidra"])
                s.sidra[(string)a[0]] = ((double)a[1], (double)a[2]);
            foreach (var r in (JArray)j["recnik"])
                if (((string)r).Length >= 2) s.recnik.Add((string)r);
            var hes = new StringBuilder(tekst);
            if (obrasciFolder != null)
            {
                s.Obrasci = ObrasciSkup.Ucitaj(obrasciFolder, prag);
                if (Directory.Exists(obrasciFolder))
                    foreach (var f in Directory.GetFiles(obrasciFolder, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) hes.Append(File.ReadAllText(f, Encoding.UTF8));
                if (prag != null) hes.Append(Newtonsoft.Json.JsonConvert.SerializeObject(prag));
            }
            s.Hes = Prilozi.Prilog.Sha256(Encoding.UTF8.GetBytes(hes.ToString())).Substring(0, 12);
            return s;
        }

        /// <summary>Mala slova, bez dijakritika, samo slova i cifre (latinica i ćirilica).</summary>
        internal static string Norm(string s)
        {
            var d = (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in d)
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'а' && c <= 'я') || "ђјљњћџ".IndexOf(c) >= 0) sb.Append(c);
            return sb.ToString();
        }

        /// <summary>Broj reči koje su nazivi rubrika CIM/CUV obrasca (za prepoznavanje strane kao tovarnog lista).</summary>
        public int BrojSidara(IEnumerable<Rec> reci) => reci.Select(r => Norm(r.T)).Where(n => n.Length >= 5 && (sidra.ContainsKey(n) || Obrasci.Sabloni.Any(o => o.Sidra.ContainsKey(n)))).Distinct().Count();

        public sealed class Rezultat
        {
            public Dictionary<string, List<Rec>> Rubrike = new Dictionary<string, List<Rec>>();
            public ObrazacSablon Obrazac;
            public Poravnanje Poravnanje;
            /// <summary>Svi probani obrasci (za dijagnostiku), od najboljeg.</summary>
            public List<Poravnanje> Probe = new List<Poravnanje>();
            /// <summary>Svaka reč strane: rubrika kojoj je dodeljena (null = van rubrika) i da li je odštampani naziv.</summary>
            public List<(Rec rec, string rubrika, bool naziv)> Dodela = new List<(Rec, string, bool)>();
            public Raster Raster;
            public bool Poravnato => Poravnanje?.Pouzdano == true;
            public int Sidara => Poravnanje?.Ukupno ?? 0;
            public List<Rec> Rubrika(string id) => Rubrike.TryGetValue(id, out var l) ? l : new List<Rec>();

            /// <summary>Pravougaonik iz šablona preslikan na stranu (obuhvatni pravougaonik), ili null.</summary>
            public double[] NaStrani(double[] r)
            {
                if (r == null || Poravnanje == null) return null;
                var t = new[] { Poravnanje.NaStranu(r[0], r[1]), Poravnanje.NaStranu(r[2], r[1]), Poravnanje.NaStranu(r[2], r[3]), Poravnanje.NaStranu(r[0], r[3]) };
                return new[] { t.Min(p => p.x), t.Min(p => p.y), t.Max(p => p.x), t.Max(p => p.y) };
            }

            /// <summary>
            /// Mastilo u rubrici koje nije odštampani obrazac (linije okvira, nazivi rubrika, brojevi u krugu): udeo površine.
            /// null ako strana nema sliku ili rubrika nije u šablonu.
            /// </summary>
            public double? Mastilo(string id)
            {
                if (Obrazac == null || !Obrazac.Rubrike.TryGetValue(id, out var r0)) return null;
                // gornji levi ugao rubrike (odštampan broj rubrike u krugu, i kada krug nije prepoznat) se ne računa
                var ugao = NaStrani(new[] { r0[0], r0[1], r0[0] + 0.035, r0[1] + 0.022 });
                return Osnova(MastiloU(r0, ugao), id);
            }

            /// <summary>Mastilo podataka u kućicama iz šablona (npr. "16" = mesec–dan–čas); null ako kućica nema u šablonu.</summary>
            public double? MastiloKucica(string id) =>
                Obrazac != null && Obrazac.Kucice.TryGetValue(id, out var r0) ? Osnova(MastiloU(r0), "#" + id) : null;

            /// <summary>Umanjeno za mastilo prazne rubrike izmereno na uzorku obrasca (odštampani natpisi koje OCR ne čita).</summary>
            private double? Osnova(double? m, string kljuc) =>
                m is double v && Obrazac.PraznoMastilo.TryGetValue(kljuc, out var o) ? Math.Max(0, v - o) : m;

            /// <summary>Mastilo koje nije odštampani obrazac (linije, crtice, nazivi rubrika, krugovi) u pravougaoniku šablona.</summary>
            public double? MastiloU(double[] sablonski, double[] iskljuciNaStrani = null)
            {
                if (Raster == null || sablonski == null) return null;
                var r = NaStrani(sablonski);
                const double ivica = 0.003;
                var iskljuci = Dodela.Where(d => d.naziv).Select(d => new[] { d.rec.X0 - 0.002, d.rec.Y0 - 0.002, d.rec.X1 + 0.002, d.rec.Y1 + 0.002 })
                    .Concat(Raster.Krugovi().Select(k => new[] { k.X0 - 0.002, k.Y0 - 0.002, k.X1 + 0.002, k.Y1 + 0.002 }))
                    .Concat(iskljuciNaStrani == null ? Enumerable.Empty<double[]>() : new[] { iskljuciNaStrani });
                return Raster.MastiloPodataka(r[0] + ivica, r[1] + ivica, r[2] - ivica, r[3] - ivica, iskljuci);
            }

            public double[] KucicaNaStrani(string id) => Obrazac != null && Obrazac.Kucice.TryGetValue(id, out var r0) ? NaStrani(r0) : null;

            /// <summary>Stanje polja za označavanje iz šablona: oznaceno / prazno / nesigurno; null ako polja nema u šablonu ili nema slike.</summary>
            public string Polje(string id, double pragOznake = 0.15, double pragPraznog = 0.05) =>
                Gustina(id) is double g ? (g >= pragOznake ? StanjePolja.Oznaceno : g <= pragPraznog ? StanjePolja.Prazno : StanjePolja.Nesigurno) : null;

            /// <summary>Gustina mastila u unutrašnjosti kvadratića (bez ivica okvira).</summary>
            public double? Gustina(string poljeId)
            {
                if (Raster == null || Obrazac == null || !Obrazac.Polja.TryGetValue(poljeId, out var r0)) return null;
                var r = NaStrani(r0);
                double dw = (r[2] - r[0]) * 0.22, dh = (r[3] - r[1]) * 0.22;
                return Raster.Gustina(r[0] + dw, r[1] + dh, r[2] - dw, r[3] - dh, false);
            }
        }

        public Rezultat Rasporedi(IEnumerable<Rec> reci, Raster raster = null)
        {
            var lista = reci.ToList();
            var rez = new Rezultat { Raster = raster };
            var potpisi = PotpisiNaziva(lista);
            if (Obrasci.Sabloni.Count == 0) return rez;

            // Sidra za poravnanje: samo odštampani nazivi (ne podaci koji slučajno liče na naziv).
            var nazivi = lista.Where(r => JeNaziv(r, potpisi)).ToList();
            rez.Probe = Obrasci.Probaj(nazivi.Count >= 5 ? nazivi : lista, raster);
            var naj = rez.Probe.FirstOrDefault();
            if (naj == null || !naj.Pouzdano)
            {
                rez.Poravnanje = naj;
                rez.Obrazac = naj == null ? null : Obrasci.Po(naj.Obrazac);
                foreach (var r in lista) rez.Dodela.Add((r, null, JeNaziv(r, potpisi)));
                return rez;
            }
            rez.Poravnanje = naj;
            rez.Obrazac = Obrasci.Po(naj.Obrazac);
            var obrazac = rez.Obrazac;

            // Reč na mestu sidra šablona ili u krugu broja rubrike je deo odštampanog obrasca.
            var naMestuSidra = new HashSet<Rec>(naj.Parovi.Where(p => p.Vrsta == "tekst").SelectMany(p => lista.Where(r => r.T == p.Oznaka && Math.Abs(r.Cx - p.Px) < 1e-9 && Math.Abs(r.Cy - p.Py) < 1e-9)));
            var krugovi = raster?.Krugovi() ?? new List<Oblik>();
            // Sken: odštampani natpisi su sitniji od upisanih podataka (OCR ih često pogrešno pročita, pa nisu u rečniku).
            var skalaY = Math.Abs(naj.Y[1]);
            bool SitanNatpis(Rec r) => obrazac.VisinaNatpisa > 0 && r.Velicina == 0 && string.IsNullOrEmpty(r.Font)
                && (r.Y1 - r.Y0) * skalaY < obrazac.VisinaNatpisa && Regex.IsMatch(r.T, @"[A-ZА-ЯĐŠČĆŽbdfhkltđćčšžбдфђћ]") && !Regex.IsMatch(r.T, @"^\d[\d\s.,\-/]*$");
            // Broj rubrike u krugu: cela reč je u krugu, a krug je bar 1,5 puta viši od reči (cifra "0" iz podatka nije krug broja).
            bool UKrugu(Rec r) => Regex.IsMatch(r.T, @"^[\(\[|{]?\d{1,2}[ab]?[\)\]}|]?\W*$") && krugovi.Any(k =>
                r.X0 >= k.X0 - 0.003 && r.X1 <= k.X1 + 0.003 && r.Y0 >= k.Y0 - 0.003 && r.Y1 <= k.Y1 + 0.003 && (k.Y1 - k.Y0) >= (r.Y1 - r.Y0) * 1.5);

            // Sken: natpis rubrike koji OCR pročita sa greškom ("Vertraghéner" = "Vertraglicher") – reč na mestu sidra šablona,
            // približno ista (do 35 % slova različito).
            var sidraLista = obrazac.Sidra.Select(kv => (n: kv.Key, x: kv.Value.x, y: kv.Value.y)).ToList();
            bool PribliznoNatpis(Rec r)
            {
                if (r.Velicina > 0 || !string.IsNullOrEmpty(r.Font)) return false;
                var n = Norm(r.T);
                if (n.Length < 4 || Regex.IsMatch(n, @"\d")) return false;
                var (tx, ty) = naj.USablon(r.Cx, r.Cy);
                var dozvoljeno = Math.Max(1, (int)(n.Length * 0.35));
                return sidraLista.Any(s => Math.Abs(s.y - ty) < 0.012 && Math.Abs(s.x - tx) < 0.06 && Math.Abs(s.n.Length - n.Length) <= dozvoljeno
                                           && Saobracaj.Common.Text.TekstUtil.Rastojanje(s.n, n) <= dozvoljeno);
            }
            var pribliznoNatpisi = new HashSet<Rec>(lista.Where(PribliznoNatpis));

            // 1) svaka reč → rubrika po centru (koordinate šablona)
            var poRubrici = new Dictionary<string, List<(Rec r, Rec t, VrstaReci v)>>();
            foreach (var r in lista)
            {
                var vrsta = naMestuSidra.Contains(r) || UKrugu(r) || SitanNatpis(r) || pribliznoNatpisi.Contains(r) ? VrstaReci.Naziv : Vrsta(r, potpisi);
                var (tx, ty) = naj.USablon(r.Cx, r.Cy);
                var id = obrazac.RubrikaZa(tx, ty);
                if (vrsta == VrstaReci.Naziv || id == null) { rez.Dodela.Add((r, id, vrsta == VrstaReci.Naziv)); continue; }
                var (x0, y0) = naj.USablon(r.X0, r.Y0); var (x1, y1) = naj.USablon(r.X1, r.Y1);
                if (!poRubrici.TryGetValue(id, out var l)) poRubrici[id] = l = new List<(Rec, Rec, VrstaReci)>();
                l.Add((r, new Rec { T = r.T, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Velicina = r.Velicina, Font = r.Font }, vrsta));
            }
            // red natpisa: reči na istom redu sa približnim natpisima (npr. "(e) Ugovor gri Vertraghéner Betarcerer-") bez cifara → natpis
            var naziviPoRedu = rez.Dodela.Where(d => d.naziv && pribliznoNatpisi.Contains(d.rec)).Select(d => d.rec).ToList();
            foreach (var kv in poRubrici)
            {
                var izbaciti = kv.Value.Where(x => !Regex.IsMatch(x.r.T, @"\d") && naziviPoRedu.Count(n => Math.Abs(n.Cy - x.r.Cy) < Math.Max(0.004, x.r.Visina * 0.6) && Math.Abs(n.Cx - x.r.Cx) < 0.12) >= 2).ToList();
                foreach (var x in izbaciti) { kv.Value.Remove(x); rez.Dodela.Add((x.r, kv.Key, true)); }
            }

            // 2) u svakoj rubrici: segmenti reda u fontu naziva koji su naziv rubrike (pri vrhu ili iz rečnika) se izbacuju
            foreach (var kv in poRubrici.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var id = kv.Key; var b = obrazac.Rubrike[id];
                var mapa = kv.Value.ToDictionary(x => x.t, x => x);
                foreach (var linija in Raspored.Linije(kv.Value.Select(x => x.t)))
                {
                    var segmenti = new List<List<Rec>>();
                    Rec preth = null;
                    foreach (var w in linija.Reci)
                    {
                        if (preth == null || w.X0 - preth.X1 > Math.Max(preth.Visina, 0.006) * 1.6) segmenti.Add(new List<Rec>());
                        segmenti[segmenti.Count - 1].Add(w);
                        preth = w;
                    }
                    foreach (var seg in segmenti)
                    {
                        var izbaci = new HashSet<Rec>(seg.Where(w => KratakNatpis.Contains(Norm(w.T)) && mapa[w].v == VrstaReci.Kandidat));
                        var duge = seg.Where(w => Norm(w.T).Length >= 3 && !Regex.IsMatch(Norm(w.T), @"^\d+$")).ToList();
                        var sveIzRecnika = duge.Count > 0 && duge.All(w => recnik.Contains(Norm(w.T))) && seg.All(w => duge.Contains(w) || !Regex.IsMatch(w.T, @"\d"));
                        if (sveIzRecnika) foreach (var w in seg) izbaci.Add(w);
                        else if (seg.Any(w => mapa[w].v == VrstaReci.Kandidat))
                        {
                            var kDuge = duge.Where(w => mapa[w].v == VrstaReci.Kandidat).ToList();
                            var udeo = kDuge.Count == 0 ? 0 : kDuge.Count(w => recnik.Contains(Norm(w.T))) / (double)kDuge.Count;
                            // Cifre (kućice r12, r17, r62…) nisu naziv rubrike i kada su istim fontom kao brojevi rubrika.
                            var samoCifre = seg.Any(w => Regex.IsMatch(w.T, @"\d")) && seg.All(w => Regex.IsMatch(w.T, @"^[\d\s,.\-/]+$"));
                            var naVrhu = !samoCifre && seg[0].Cy - b[1] < 0.022 && seg.All(w => mapa[w].v == VrstaReci.Kandidat);
                            if (udeo >= 0.5 || naVrhu) foreach (var w in seg.Where(w => mapa[w].v == VrstaReci.Kandidat)) izbaci.Add(w);
                        }
                        foreach (var w in seg)
                        {
                            // Broj rubrike odštampan u gornjem levom uglu nije podatak.
                            var broj = ReferenceEquals(w, seg[0]) && w.T.Trim('(', ')') == Regex.Match(id, @"^\d+[ab]?").Value && w.X0 - b[0] < 0.03 && w.Cy - b[1] < 0.015;
                            var original = mapa[w].r;
                            if (izbaci.Contains(w) || broj) { rez.Dodela.Add((original, id, true)); continue; }
                            rez.Dodela.Add((original, id, false));
                            if (!rez.Rubrike.TryGetValue(id, out var l)) rez.Rubrike[id] = l = new List<Rec>();
                            l.Add(original);
                        }
                    }
                }
            }
            return rez;
        }

        /// <summary>
        /// Potpisi fonta (font + veličina) kojima su odštampani nazivi rubrika: potpis kod kog su bar petina reči (i bar 5)
        /// iz rečnika naziva, ili font kojim su odštampani samo brojevi rubrika. Podaci su upisani drugim fontom ili veličinom.
        /// </summary>
        internal HashSet<string> PotpisiNaziva(List<Rec> reci)
        {
            var rez = new HashSet<string>();
            foreach (var g in reci.Where(r => r.Velicina > 0).GroupBy(r => r.Potpis))
            {
                var sve = g.ToList();
                if (sve.Count >= 10 && sve.Count(r => Regex.IsMatch(r.T, @"^\d{1,2}[ab]?\)?$")) >= sve.Count * 0.8) { rez.Add(g.Key); continue; }
                var duge = sve.Where(r => Norm(r.T).Length >= 3 && !Regex.IsMatch(Norm(r.T), @"^\d+$")).ToList();
                if (duge.Count < 5) continue;
                var uRecniku = duge.Count(r => recnik.Contains(Norm(r.T)));
                if (uRecniku >= 5 && uRecniku >= duge.Count * 0.2) rez.Add(g.Key);
            }
            return rez;
        }

        /// <summary>Odštampani naziv rubrike (za sidra): u fontu naziva i iz rečnika, ili (OCR) iz rečnika.</summary>
        internal bool JeNaziv(Rec r, HashSet<string> potpisi)
        {
            var v = Vrsta(r, potpisi);
            return v == VrstaReci.Naziv || (v == VrstaReci.Kandidat && (recnik.Contains(Norm(r.T)) || Norm(r.T).Length >= 4));
        }

        /// <summary>Kratki odštampani natpisi u rubrikama (skraćenice obrasca), ne podaci.</summary>
        private static readonly HashSet<string> KratakNatpis = new HashSet<string> { "бр", "br", "nr", "tel", "fax", "faks", "ja", "да", "da", "od", "do", "bis", "von" };

        private enum VrstaReci { Podatak, Naziv, Kandidat }

        /// <summary>Da li je reč odštampani naziv rubrike, podatak, ili kandidat (font naziva – odluka po segmentu reda).</summary>
        private VrstaReci Vrsta(Rec r, HashSet<string> potpisiNaziva)
        {
            if (r.Velicina > 0)
            {
                if (potpisiNaziva.Contains(r.Potpis)) return VrstaReci.Kandidat;
                if (potpisiNaziva.Count > 0) return VrstaReci.Podatak;
                return r.Velicina < 5.5 ? VrstaReci.Naziv : VrstaReci.Podatak;
            }
            // OCR: font nije poznat → rečnik naziva rubrika (reči od 3+ znaka, da se ne izbace podaci kao "KP", "RS").
            if (Regex.IsMatch(r.T, @"^[\(\[|{]?\d{1,2}[ab]?[\)\]}|]\W*$")) return VrstaReci.Naziv;   // odštampan broj rubrike "(57)", "62)"
            var n = Norm(r.T);
            if (n.Length < 3 || Regex.IsMatch(n, @"^\d+$")) return VrstaReci.Podatak;
            return recnik.Contains(n) || SlicnoNazivu(n) ? VrstaReci.Naziv : VrstaReci.Podatak;
        }

        private Dictionary<string, List<string>> recnikPoPocetku;

        /// <summary>OCR greška u nazivu rubrike ("Vrijdenost" → "vrijednost"): 1 slovo za reči do 7 znakova, 2 za duže.</summary>
        private bool SlicnoNazivu(string n)
        {
            if (n.Length < 5) return false;
            if (recnikPoPocetku == null)
                recnikPoPocetku = recnik.Where(x => x.Length >= 5).GroupBy(x => x.Substring(0, 1)).ToDictionary(g => g.Key, g => g.ToList());
            if (!recnikPoPocetku.TryGetValue(n.Substring(0, 1), out var kandidati)) return false;
            var dozvoljeno = n.Length >= 8 ? 2 : 1;
            return kandidati.Any(k => Math.Abs(k.Length - n.Length) <= dozvoljeno && Saobracaj.Common.Text.TekstUtil.Rastojanje(k, n) <= dozvoljeno);
        }
    }

    internal static class StanjePolja
    {
        public const string Oznaceno = "oznaceno";
        public const string Prazno = "prazno";
        public const string Nesigurno = "nesigurno";
    }
}
