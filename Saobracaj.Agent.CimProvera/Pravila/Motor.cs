using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Pravila
{
    internal static class Tezina
    {
        public const string Greska = "error";
        public const string Upozorenje = "warning";
        public const string Info = "info";
        public const string Preporuka = "recommendation";

        public static int Red(string t) => t == Greska ? 3 : t == Upozorenje ? 2 : t == Preporuka ? 1 : 0;
    }

    /// <summary>Jedan nalaz u poruci.</summary>
    internal sealed class Nalaz
    {
        public string Pravilo;
        public string Rubrika;
        /// <summary>Težina prikazane stavke (najveća u grupi preklapanja).</summary>
        public string Tezina;
        /// <summary>Težina samog pravila iz JSON-a.</summary>
        public string TezinaPravila;
        public string Tekst;
        /// <summary>Konkretan nalaz za ovaj list (npr. "Nedostaje: adresa, ISO 3166 šifra zemlje.").</summary>
        public string Detalj = "";
        /// <summary>Opšte uputstvo iz pravila (poruka iz JSON-a).</summary>
        public string Uputstvo = "";
        /// <summary>Šta je upisano u rubriku (sa lista), za tabelu u izveštaju.</summary>
        public string Vrednost = "";
        /// <summary>Primer ispravnog upisa (polje "primer" pravila u JSON-u).</summary>
        public string Primer = "";
        /// <summary>Ključ preklapanja: nalazi istog ključa se prikazuju jednom (prvo pojavljivanje), ostali kao referenca.</summary>
        public string Kljuc;
        /// <summary>Samo za internu kopiju izveštaja (KP), ne šalje se spoljnom pošiljaocu (npr. r49 koju popunjava KP kao prevoznik).</summary>
        public bool SamoInterno;
        public List<Nalaz> Takodje = new List<Nalaz>();

        public override string ToString() => Tezina + " " + Pravilo + " r" + Rubrika + ": " + Tekst;
    }

    /// <summary>Ulaz u proveru: svi dokumenti iz maila (ili foldera) i podaci o pošiljaocu.</summary>
    internal sealed class UlazProvere
    {
        public List<Dokument> Dokumenti = new List<Dokument>();
        public string Posiljalac;
        public bool InterniPosiljalac;
        /// <summary>Vreme prijema maila (za "datum u budućnosti").</summary>
        public DateTime ReferentniDatum = DateTime.Today;
        /// <summary>auto | kp | spoljni</summary>
        public string RezimKp = "auto";
        public bool Engleski;
        public double Prag = 0.7;
    }

    internal sealed class RezultatLista
    {
        public Dokument Dokument;
        public CimList Cim;
        public string Oznaka;
        public bool KpRezim;
        public List<Nalaz> Nalazi = new List<Nalaz>();
        public bool ZaRucnuProveru;
    }

    internal sealed class RezultatProvere
    {
        public List<RezultatLista> Listovi = new List<RezultatLista>();
        public List<Dokument> Dokumenti = new List<Dokument>();
        public List<string> NijeImplementirano = new List<string>();
        public List<string> GreskePravila = new List<string>();
        /// <summary>Svi prilozi maila i kako je svaki klasifikovan („Trainlist.xlsx – spisak tovarnih jedinica“) – za izveštaj.</summary>
        public List<string> OpisPriloga = new List<string>();
        public bool ImaTovarniList => Listovi.Count > 0;
        public int BrojGresaka => Listovi.Sum(l => l.Nalazi.Count(n => n.Tezina == Tezina.Greska));
        public int BrojUpozorenja => Listovi.Sum(l => l.Nalazi.Count(n => n.Tezina == Tezina.Upozorenje));
        public bool Ispravan => ImaTovarniList && BrojGresaka == 0;
        public bool ZaRucnuProveru => Listovi.Any(l => l.ZaRucnuProveru);
        public bool ImaInternih => Listovi.Any(l => l.Nalazi.Any(n => n.SamoInterno));

        /// <summary>Isti rezultat bez nalaza "samo interno" (izveštaj za spoljnog pošiljaoca).</summary>
        public RezultatProvere BezInternih() => new RezultatProvere
        {
            Dokumenti = Dokumenti, NijeImplementirano = NijeImplementirano, GreskePravila = GreskePravila, OpisPriloga = OpisPriloga,
            Listovi = Listovi.Select(l => new RezultatLista { Dokument = l.Dokument, Cim = l.Cim, Oznaka = l.Oznaka, KpRezim = l.KpRezim, ZaRucnuProveru = l.ZaRucnuProveru, Nalazi = l.Nalazi.Where(n => !n.SamoInterno).ToList() }).ToList(),
        };
    }

    /// <summary>Pogodak pravila pre formatiranja poruke.</summary>
    internal sealed class Pogodak
    {
        public string Rubrika;
        public string Kljuc;
        public string Tezina;  // samo kada JSON propisuje drugu težinu za slučaj (G11: severity_if_reference_missing)
        public Dictionary<string, string> V = new Dictionary<string, string>();
        public string Dodatak;
        /// <summary>Predlog ispravnog upisa iz pratećih dokumenata ili drugog dela lista (ima prednost nad formatom iz JSON-a).</summary>
        public string Predlog;
        /// <summary>Poruka za ovaj slučaj umesto poruke pravila (npr. R25-MASA za prazna kola).</summary>
        public string Poruka;

        public Pogodak(string rubrika, string kljuc = null) { Rubrika = rubrika; Kljuc = kljuc; }
        public Pogodak S(string k, string v) { V[k] = v; return this; }
        public Pogodak D(string dodatak) { Dodatak = dodatak; return this; }
        public Pogodak Pr(string predlog) { Predlog = predlog; return this; }
    }

    /// <summary>
    /// Deterministički rules engine. Grupe idu redom iz PravilaSkup.RedGrupa. Težina i tekst poruke su iz JSON-a;
    /// kod samo utvrđuje da li je uslov pravila ispunjen.
    /// </summary>
    internal sealed class Motor
    {
        private readonly PravilaSkup p;
        private readonly Log log;

        public Motor(PravilaSkup pravila, Log log = null) { p = pravila; this.log = log; }

        public RezultatProvere Proveri(UlazProvere u)
        {
            var rez = new RezultatProvere { Dokumenti = u.Dokumenti };
            rez.NijeImplementirano = NijeImplementirano(p);

            var listovi = u.Dokumenti.Where(d => TipDokumenta.JeTovarniList(d.Tip)).SelectMany(d => d.Cim.Select(c => (d, c))).ToList();
            int rb = 0;
            foreach (var (dok, cim) in listovi)
            {
                rb++;
                PoznatiPodaciKp(cim, p.Kp);
                var k = new Kontekst(cim, dok, u, p);
                var rl = new RezultatLista
                {
                    Dokument = dok, Cim = cim, KpRezim = k.Kp,
                    Oznaka = listovi.Count == 1 ? dok.Fajl : dok.Fajl + (dok.Cim.Count > 1 || listovi.Count(x => x.d == dok) > 1 ? " – list " + cim.RedniBroj : "") ,
                };

                // Obrazac nije prepoznat (ili poravnanje nije pouzdano): rubrike se ne proveravaju nasumično – ručna provera.
                if (!string.IsNullOrEmpty(cim.NeprepoznatObrazac))
                {
                    var tekst = p.Sistemska("obrazac_nije_prepoznat", u.Engleski, "Tovarni list nije pročitan – {opis}.").Replace("{opis}", cim.NeprepoznatObrazac);
                    rl.Nalazi.Add(new Nalaz { Pravilo = "OBRAZAC", Rubrika = "–", Tezina = Tezina.Upozorenje, TezinaPravila = Tezina.Upozorenje, Tekst = tekst, Uputstvo = tekst, Kljuc = "obrazac" });
                    // G12 (informativni ispis) zavisi samo od zaglavlja – javlja se i kada obrazac nije prepoznat.
                    if (p.PoId.TryGetValue("G12", out var g12) && Provere.JeInformativniIspis(p, cim))
                        rl.Nalazi.Add(Napravi(g12, new Pogodak("–", "g12"), u.Engleski, cim));
                    rl.ZaRucnuProveru = true;
                    rez.Listovi.Add(rl);
                    continue;
                }

                foreach (var def in p.Sva)
                {
                    if (!Provere.Sve.TryGetValue(def.Id, out var fn)) continue;
                    if (!k.Kp && (Provere.SamoKp.Contains(def.Id) || (def.PopunjavaPrevoznik && !Provere.UvekVazi.Contains(def.Id)))) continue;
                    List<Pogodak> pogodci;
                    try { pogodci = fn(k).ToList(); }
                    catch (Exception ex)
                    {
                        rez.GreskePravila.Add(def.Id + ": " + ex.Message);
                        log?.Upozorenje("Pravilo " + def.Id + " nije izvršeno: " + ex.Message);
                        continue;
                    }
                    foreach (var pg in pogodci) Dodaj(rl.Nalazi, Napravi(def, pg, u.Engleski, cim));
                }

                // Nečitljive rubrike: upozorenje, ne greška pošiljaoca; dokument ide na ručnu proveru.
                foreach (var r in k.Necitljive.OrderBy(x => x, StringComparer.Ordinal))
                {
                    var tekst = p.Sistemska("necitljivo", u.Engleski, "Rubrika {field} nije čitljiva na skenu – pošaljite čitljiviji dokument.").Replace("{field}", r);
                    Dodaj(rl.Nalazi, new Nalaz { Pravilo = "CITLJIVOST", Rubrika = r, Tezina = Tezina.Upozorenje, TezinaPravila = Tezina.Upozorenje, Tekst = tekst, Uputstvo = tekst, Vrednost = TekstRubrike(cim, r), Kljuc = "citljivost|" + r });
                    rl.ZaRucnuProveru = true;
                }
                rl.Nalazi = SpojiPoRubrici(rl.Nalazi);
                rez.Listovi.Add(rl);
            }
            return rez;
        }

        /// <summary>
        /// r58a (ugovorni prevoznik) koja približno odgovara poznatim podacima KP (naziv, UIC 3212, adresa – i kada je pečat
        /// pokvario OCR): rubrika je ispravna i dobija tačne podatke KP.
        /// </summary>
        internal static void PoznatiPodaciKp(CimList c, PraviloKp kp)
        {
            var r = c.R58a;
            if (r == null || string.IsNullOrWhiteSpace(r.Tekst) || !kp.JePoznatKp(r.Tekst)) return;
            // Pouzdano pročitana rubrika (šifra 3212 i jasno drugi naziv) se ne prepisuje – KP2 mora da prijavi pogrešan naziv.
            if (r.Status == StatusRubrike.Popunjeno && kp.JeKpSifra(r.Sifra) && r.Pouzdanost >= 0.8
                && !string.IsNullOrWhiteSpace(r.Naziv) && !kp.JeKpNaziv(r.Naziv) && !kp.JePoznatKp(r.Naziv)) return;
            r.Sifra = kp.Uic; r.Naziv = kp.Naziv; r.Adresa = kp.Adresa; r.Potpis = true;
            r.Status = StatusRubrike.Popunjeno; r.Pouzdanost = Math.Max(r.Pouzdanost, 0.9);
            r.Tekst = kp.Uic + " " + kp.Naziv + ", " + kp.Adresa;
        }

        /// <summary>
        /// Jedan nalaz = jedan red (v2.7.1): spajaju se samo pogoci ISTOG pravila u istoj rubrici i iste težine (npr. dve NHM šifre);
        /// različita pravila iste rubrike su posebni redovi. Opšti nalazi (bez rubrike) i preporuke se ne ponavljaju.
        /// </summary>
        internal static List<Nalaz> SpojiPoRubrici(List<Nalaz> nalazi)
        {
            var rez = new List<Nalaz>();
            foreach (var n in nalazi)
            {
                var prvi = n.Rubrika == "–" || n.Tezina == Tezina.Preporuka || n.Tezina == Tezina.Info
                    ? rez.FirstOrDefault(x => x.Tezina == n.Tezina && x.Tekst == n.Tekst)
                    : rez.FirstOrDefault(x => x.Rubrika == n.Rubrika && x.Tezina == n.Tezina && x.Pravilo == n.Pravilo && n.Pravilo != "CITLJIVOST");
                if (prvi == null) { rez.Add(n); continue; }
                if (prvi.Tekst == n.Tekst) continue;
                var dodatak = !string.IsNullOrEmpty(n.Detalj) ? n.Detalj : n.Uputstvo;
                if (!string.IsNullOrEmpty(dodatak) && !(prvi.Detalj ?? "").Contains(dodatak) && !(prvi.Uputstvo ?? "").Contains(dodatak))
                {
                    prvi.Detalj = string.IsNullOrEmpty(prvi.Detalj) ? dodatak : prvi.Detalj.TrimEnd() + (Regex.IsMatch(prvi.Detalj, @"[.!?)]$") ? " " : "; ") + dodatak;
                    prvi.Tekst = prvi.Uputstvo + " " + prvi.Detalj;
                }
                // svaki spojeni nalaz zadržava svoj primer ispravnog upisa
                if (string.IsNullOrEmpty(prvi.Primer)) prvi.Primer = n.Primer ?? "";
                else if (!string.IsNullOrEmpty(n.Primer) && prvi.Primer.IndexOf(n.Primer, StringComparison.OrdinalIgnoreCase) < 0) prvi.Primer += " / " + n.Primer;
                prvi.Takodje.Add(n);
                prvi.Takodje.AddRange(n.Takodje);
            }
            return rez;
        }

        /// <summary>Pravila sa težinom (koja nešto proveravaju) za koja nema implementacije. Fakultativne rubrike bez težine se ne broje.</summary>
        public static List<string> NijeImplementirano(PravilaSkup p) =>
            p.Sva.Where(d => d.Json["severity"] != null && !Provere.Sve.ContainsKey(d.Id)).Select(d => d.Id).ToList();

        /// <summary>Tekst rubrike sa lista ("16" → R16, "58a" → R58a), skraćen za tabelu.</summary>
        internal static Rubrika RubrikaLista(CimList c, string rubrika)
        {
            if (c == null || string.IsNullOrEmpty(rubrika) || !Regex.IsMatch(rubrika, @"^\d+[a-z]?$")) return null;
            return typeof(CimList).GetProperty("R" + rubrika)?.GetValue(c) as Rubrika;
        }

        internal static string TekstRubrike(CimList c, string rubrika)
        {
            var r = RubrikaLista(c, rubrika);
            if (r == null) return "";
            if (r.Status == StatusRubrike.Necitljivo) return "nečitljivo";
            if (r.Status == StatusRubrike.Oznake) { var o = Regex.Replace(r.Tekst ?? "", @"\s*\n\s*", " / ").Trim(); return o.Length > 140 ? o.Substring(0, 137) + "…" : o; }
            if (!r.Popunjena) return "";
            var t = Regex.Replace(r.Tekst ?? "", @"\s*\n\s*", " / ");
            t = Regex.Replace(t, @"\s{2,}", " ").Trim();
            return t.Length > 140 ? t.Substring(0, 137) + "…" : t;
        }

        private Nalaz Napravi(PraviloDef def, Pogodak pg, bool en, CimList cim = null)
        {
            var v = new Dictionary<string, string>(pg.V);
            if (!v.ContainsKey("field")) v["field"] = pg.Rubrika;
            var uputstvo = Regex.Replace(pg.Poruka ?? def.Poruka(en), @"\{(\w+)\}", m => v.TryGetValue(m.Groups[1].Value, out var x) ? x : m.Value);
            // Upisane su samo oznake obrasca bez vrednosti („Tara: … Netto: …“): konkretan opis umesto „rubrika je prazna“.
            var dodatak = pg.Dodatak;
            if (RubrikaLista(cim, pg.Rubrika)?.Status == StatusRubrike.Oznake && (string.IsNullOrEmpty(dodatak) || dodatak.Contains("prazna") || dodatak.Contains("empty")))
                dodatak = en ? "Only the form labels are entered, without values." : "Upisane su samo oznake, bez vrednosti.";
            var tekst = uputstvo + (string.IsNullOrEmpty(dodatak) ? "" : " " + dodatak);
            return new Nalaz
            {
                Pravilo = def.Id, Rubrika = pg.Rubrika, Tezina = pg.Tezina ?? def.Tezina, TezinaPravila = pg.Tezina ?? def.Tezina, Tekst = tekst,
                Uputstvo = uputstvo, Detalj = dodatak ?? "",
                Vrednost = TekstRubrike(cim, pg.Rubrika),
                Primer = !string.IsNullOrEmpty(pg.Predlog) ? pg.Predlog : (en ? (string)def.Json["primer_en"] : null) ?? (string)def.Json["primer"] ?? "",
                Kljuc = pg.Kljuc ?? def.Id + "|" + pg.Rubrika + "|" + tekst,
                SamoInterno = (bool?)def.Json["samo_interno"] == true,
            };
        }

        /// <summary>Preklapanje: isti ključ = isti problem. Prikazuje se prvo pojavljivanje, ostala kao referenca; težina je najveća u grupi.</summary>
        internal static void Dodaj(List<Nalaz> lista, Nalaz n)
        {
            var prvi = lista.FirstOrDefault(x => x.Kljuc == n.Kljuc);
            if (prvi == null) { lista.Add(n); return; }
            if (prvi.Pravilo == n.Pravilo && prvi.Tekst == n.Tekst) return;
            prvi.Takodje.Add(n);
            if (Tezina.Red(n.Tezina) > Tezina.Red(prvi.Tezina)) prvi.Tezina = n.Tezina;
        }
    }

    /// <summary>Podaci za jedan list: sam list + ostali dokumenti iz istog maila.</summary>
    internal sealed class Kontekst
    {
        public readonly CimList C;
        public readonly Dokument Dok;
        public readonly UlazProvere U;
        public readonly PravilaSkup P;
        public readonly bool Kp;
        public readonly List<Dokument> Svi;
        public readonly List<(Dokument dok, SpisakKola s)> Spiskovi;
        public readonly SortedSet<string> Necitljive = new SortedSet<string>(StringComparer.Ordinal);

        public Kontekst(CimList c, Dokument dok, UlazProvere u, PravilaSkup p)
        {
            C = c; Dok = dok; U = u; P = p;
            Svi = u.Dokumenti;
            Spiskovi = Svi.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak.Select(s => (d, s))).ToList();
            // Više tovarnih listova u istom mailu: spisak iz drugog fajla sa drugim brojem otpravljanja pripada drugoj pošiljci.
            if (Svi.Where(d => TipDokumenta.JeTovarniList(d.Tip)).Sum(d => d.Cim.Count) > 1)
            {
                string Broj(Identifikacija r) => System.Text.RegularExpressions.Regex.Replace(r?.Broj ?? "", @"\D", "");
                var moj = Broj(c.R62);
                Spiskovi = Spiskovi.Where(x => x.dok.Fajl == dok?.Fajl || moj.Length == 0 || Broj(x.s.R62).Length == 0 || Broj(x.s.R62) == moj).ToList();
            }
            switch ((u.RezimKp ?? "auto").ToLowerInvariant())
            {
                case "kp": Kp = true; break;
                case "spoljni": Kp = false; break;
                default: Kp = u.InterniPosiljalac || p.Kp.JeKpSifra(c.R62.Preduzece); break;
            }
        }

        /// <summary>
        /// Rubrika je pročitana dovoljno pouzdano za proveru. Ako je nečitljiva ili ispod praga pouzdanosti, beleži se
        /// upozorenje o čitljivosti i pravilo se za tu rubriku ne primenjuje.
        /// </summary>
        public bool Citljivo(Rubrika r, string broj)
        {
            if (r == null) return false;
            if (r.Status == StatusRubrike.Necitljivo || (r.Popunjena && r.Pouzdanost < U.Prag))
            {
                Necitljive.Add(broj);
                return false;
            }
            return true;
        }

        /// <summary>List je pročitan OCR-om (sken) – oznake X i cifre po kućicama se ne vide pouzdano.</summary>
        public bool Ocr => Dok?.Izvor == "ocr";

        public IEnumerable<Dokument> Tip(string tip) => Svi.Where(d => d.Tip == tip);
    }
}
