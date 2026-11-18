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
        /// <summary>
        /// v2.8.1 – PDF primer: šta se crvenom (narandžastom) bojom dopisuje u rubriku – stvarna vrednost iz dokumenata (zemlja, datum,
        /// NHM iz JCI, 3212 KP…) ili realan primer u istom obliku („Marko Marković“, „11-13-08“), nikad opis formata. Prazno = samo oznaka.
        /// </summary>
        public string Dopuna = "";
        /// <summary>Pogrešan deo upisa koji se u primeru precrtava (npr. „2520“ kada je ispravno „252010“); prazno = ništa se ne precrtava.</summary>
        public string Pogresno = "";
        /// <summary>Napomena (narandžasto): dopuna se upisuje samo kada je predlog siguran (NHM iz JCI, ISO šifra zemlje…).</summary>
        public bool DopunaSigurna;
        /// <summary>v2.8.2: dopuna po redovima (r25: „703 000 kg“ uz oznaku „Tare:“ / „X kg“); null = jedan tekst.</summary>
        public List<string> DopunaRedovi;
        /// <summary>Redovi dopune idu uz postojeće oznake u rubrici (npr. „T= …“, „Netto:“), a ne kao poseban blok teksta.</summary>
        public bool DopunaUzOznake;
        /// <summary>v2.8.2: dopuna po poljima obrasca (r62: „62.3“ → „3212“, „62.4“ → „XXXXX-X“); null = jedan tekst.</summary>
        public Dictionary<string, string> DopunaPoPoljima;
        /// <summary>v2.9.2 (2): dopuna samo u novom redu ispod teksta rubrike (ne desno od reda – r57: nikad uz stranu deonicu).</summary>
        public bool DopunaIspod;
        /// <summary>v2.9.2 (2): red rubrike uz koji ide dopuna (r57: srpska deonica „Subotica br. – Bor Teretna“); null = posle poslednjeg reda.</summary>
        public string DopunaUz;

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
        /// <summary>v2.9.1 (4.1): kopije istog tovarnog lista (ista pouzdana r62: original, faktura, list prijema, duplikat…) – proverava se
        /// i prijavljuje samo najbolje pročitana kopija; ostale ostaju ovde (bez nalaza) za upis u bazu (najbolje pročitano polje).</summary>
        public List<RezultatLista> KopijeListova = new List<RezultatLista>();
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
        /// <summary>v2.8.1: CIT 7 (naknadni zahtevi) iz maila – veza sa tovarnim listom, izmene, provere. Samo interno.</summary>
        public List<Cit7Rezultat> Cit7 = new List<Cit7Rezultat>();
        /// <summary>v2.8.4 (8): uvod iz dosijea („Ovo je nova verzija tovarnog lista od …“ / „skenirana i overena verzija…“); null ako nema.</summary>
        public string UvodDosijea;

        /// <summary>Isti rezultat bez nalaza "samo interno" (izveštaj za spoljnog pošiljaoca).</summary>
        public RezultatProvere BezInternih() => new RezultatProvere
        {
            Dokumenti = Dokumenti, NijeImplementirano = NijeImplementirano, GreskePravila = GreskePravila, OpisPriloga = OpisPriloga, UvodDosijea = UvodDosijea,
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
        /// <summary>Samo za interni izveštaj (npr. „r62 nečitljiva – proveriti ručno“), i kada pravilo inače ide pošiljaocu.</summary>
        public bool SamoInterno;
        /// <summary>v2.8.1: dopuna za PDF primer koju je pravilo samo odredilo (ima prednost nad <see cref="Dopune"/>).</summary>
        public string Dopuna;
        public string Pogresno;
        public bool DopunaSigurna;
        public Pogodak Dop(string dopuna, string pogresno = null, bool sigurna = true) { Dopuna = dopuna; Pogresno = pogresno; DopunaSigurna = sigurna; return this; }

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

        /// <summary>Prosečna pouzdanost OCR-a strane ispod koje je sken „nečitljiv“ (v2.8).</summary>
        public const double PragSkena = 0.75;

        public RezultatProvere Proveri(UlazProvere u)
        {
            var rez = new RezultatProvere { Dokumenti = u.Dokumenti };
            rez.NijeImplementirano = NijeImplementirano(p);

            var listovi = u.Dokumenti.Where(d => TipDokumenta.JeTovarniList(d.Tip)).SelectMany(d => d.Cim.Select(c => (d, c))).ToList();
            // v2.9.1 (4.1): kopije istog lista (ista pouzdana r62) – proverava se jednom, najbolje pročitana kopija (tekst PDF-a, pa veća pouzdanost OCR-a)
            var kopijaOd = new Dictionary<CimList, CimList>();
            foreach (var g in listovi.Where(x => string.IsNullOrEmpty(x.c.NeprepoznatObrazac) && !R62Nepouzdana(x.c)).GroupBy(x => Ponavljanja.KljucLista(x.c))
                         .Where(g => g.Key != null && g.Key.StartsWith("r62:", StringComparison.Ordinal) && g.Count() > 1))
            {
                var glavni = g.OrderByDescending(x => x.c.PouzdanostStrane <= 0 ? 1.0 : x.c.PouzdanostStrane).ThenBy(x => listovi.IndexOf(x)).First();
                foreach (var x in g) if (!ReferenceEquals(x.c, glavni.c)) kopijaOd[x.c] = glavni.c;
            }
            int rb = 0;
            foreach (var (dok, cim) in listovi)
            {
                rb++;
                if (kopijaOd.ContainsKey(cim))
                {
                    rez.KopijeListova.Add(new RezultatLista { Dokument = dok, Cim = cim, Oznaka = dok.Fajl + " – list " + cim.RedniBroj + " (kopija)" });
                    continue;
                }
                PoznatiPodaciKp(cim, p.Kp);
                // v2.8.4: r62 na skenu niske pouzdanosti (rukom upisana: YU DIN COMMERCE „72 / 165092 / 3212 / 162600“ pročitano kao
                // „72 10502-2 3242 62600“) ne koristi se za odluke (KP tovarni list, ponavljanje, dosije) – „r62 nečitljiva – ručna provera“
                var r62Procitano = R62Nepouzdana(cim) ? OznaciR62Necitljivu(cim) : null;
                var k = new Kontekst(cim, dok, u, p);
                var rl = new RezultatLista
                {
                    Dokument = dok, Cim = cim, KpRezim = k.Kp,
                    Oznaka = listovi.Count - kopijaOd.Count == 1 ? dok.Fajl : dok.Fajl + (dok.Cim.Count > 1 || listovi.Count(x => x.d.Fajl == dok.Fajl) > 1 ? " – list " + cim.RedniBroj : "") ,
                };
                var brKopija = kopijaOd.Count(x => ReferenceEquals(x.Value, cim));
                if (brKopija > 0) rl.Oznaka += " (+" + brKopija + " " + (brKopija == 1 ? "kopija" : "kopije") + ")";

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
                    if (def.Json["ne_proverava_se"] != null) continue;   // obrisano pravilo (npr. REC3, REC6, KP6 u v2.8) se ne izvršava
                    // v2.8.4 (9): pravilo za rubriku koju po GLV-CIM popunjava prevoznik (popunjava = „prevoznik“) proverava se i na listu
                    // spoljnog pošiljaoca, ali nalaz ide u INTERNO („popunjava prevoznik (KP pri prijemu)“); bez polja „popunjava“ – kao ranije
                    var prevoznikInterno = !k.Kp && def.Popunjava == "prevoznik";
                    if (!k.Kp && !prevoznikInterno && (Provere.SamoKp.Contains(def.Id) || (def.PopunjavaPrevoznik && !Provere.UvekVazi.Contains(def.Id)))) continue;
                    List<Pogodak> pogodci;
                    try { pogodci = fn(k).ToList(); }
                    catch (Exception ex)
                    {
                        rez.GreskePravila.Add(def.Id + ": " + ex.Message);
                        log?.Upozorenje("Pravilo " + def.Id + " nije izvršeno: " + ex.Message);
                        continue;
                    }
                    foreach (var pg in pogodci)
                    {
                        var n = Napravi(def, pg, u.Engleski, cim);
                        // v2.8.4: težina kod spoljnog pošiljaoca (R62: prazan ili nepotpun r62 je napomena; kod KP tovarnog lista ostaje kao u pravilu)
                        if (!k.Kp && (string)def.Json["severity_spoljni"] is string sv && n.Tezina == Tezina.Greska) n.Tezina = sv;
                        if (prevoznikInterno)
                        {
                            var nap = u.Engleski ? " – filled in by the carrier (KP on acceptance)" : " – popunjava prevoznik (KP pri prijemu)";
                            n.SamoInterno = true; n.Tekst += nap; n.Uputstvo += nap;
                        }
                        try { Dopune.Popuni(n, pg, k); }
                        catch (Exception ex) { log?.Upozorenje("Dopuna za PDF primer (" + def.Id + ") nije određena: " + ex.Message); }
                        Dodaj(rl.Nalazi, n);
                    }
                }

                // (bez duplikata: pravilo R62 je već dalo internu napomenu „r62 nečitljiva – proveriti ručno“)
                if (r62Procitano != null && !rl.Nalazi.Any(n => n.Rubrika == "62" && (n.Tekst ?? "").StartsWith("r62 nečitljiva")))
                {
                    var tekst = u.Engleski
                        ? "Box 62 (consignment identification) is not reliably readable (handwritten / OCR " + cim.R62.PouzdanostCifara.ToString("0.00") + ") – not used for decisions (KP consignment note, repetition, file); manual check. Agent read: " + r62Procitano
                        : "r62 nečitljiva – ručna provera: identifikacija pošiljke nije pouzdano pročitana (rukom upisana / OCR " + cim.R62.PouzdanostCifara.ToString("0.00") + ") i ne koristi se za odluke (KP tovarni list, ponavljanje, dosije). Agent je pročitao: " + r62Procitano;
                    Dodaj(rl.Nalazi, new Nalaz { Pravilo = "R62-NECITLJIVA", Rubrika = "62", Tezina = Tezina.Info, TezinaPravila = Tezina.Info, Tekst = tekst, Uputstvo = tekst, SamoInterno = true, Vrednost = r62Procitano, Kljuc = "r62-necitljiva" });
                    rl.ZaRucnuProveru = true;
                }
                // Nečitljive rubrike: upozorenje, ne greška pošiljaoca; dokument ide na ručnu proveru.
                foreach (var r in k.Necitljive.OrderBy(x => x, StringComparer.Ordinal))
                {
                    var tekst = p.Sistemska("necitljivo", u.Engleski, "Rubrika {field} nije čitljiva na skenu – pošaljite čitljiviji dokument.").Replace("{field}", r);
                    Dodaj(rl.Nalazi, new Nalaz { Pravilo = "CITLJIVOST", Rubrika = r, Tezina = Tezina.Upozorenje, TezinaPravila = Tezina.Upozorenje, Tekst = tekst, Uputstvo = tekst, Vrednost = TekstRubrike(cim, r), Kljuc = "citljivost|" + r });
                    rl.ZaRucnuProveru = true;
                }
                // v2.8: sken niske pouzdanosti (OCR strane ispod 0,75): ono što je OCR pročitao nije osnova za grešku ni upozorenje
                // o sadržaju – samo interna napomena „r<N> nečitljiva – proveriti ručno“ (sa onim što je agent pročitao); list ide na
                // ručnu proveru i izveštaj je interni (pošiljaocu ništa dok čovek ne pogleda).
                if (k.Ocr && cim.PouzdanostStrane > 0 && cim.PouzdanostStrane < PragSkena)
                {
                    foreach (var n in rl.Nalazi.Where(n => n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje).ToList())
                    {
                        var procitano = !string.IsNullOrEmpty(n.Detalj) ? n.Detalj : n.Tekst;
                        n.Tekst = (u.Engleski ? "Box " + n.Rubrika + " is not reliably readable on the scan (OCR " : "Rubrika " + n.Rubrika + " nije pouzdano čitljiva na skenu (OCR ")
                                  + cim.PouzdanostStrane.ToString("0.00") + (u.Engleski ? ") – check manually. Agent read: " : ") – proveriti ručno. Agent je pročitao: ") + procitano;
                        n.Uputstvo = n.Tekst; n.Detalj = ""; n.Primer = ""; n.Dopuna = ""; n.Pogresno = ""; n.DopunaRedovi = null;
                        n.Tezina = Tezina.Info; n.SamoInterno = true;
                        n.Kljuc = "rucno|" + n.Kljuc;
                    }
                    rl.ZaRucnuProveru = true;
                    // v2.8.4 (11.2): nepouzdano pročitane rubrike u JEDNOM redu („Nepouzdano pročitane rubrike (OCR 0,70): 1, 4, 7 … – proveriti
                    // ručno.“), ne kao 19 posebnih redova
                    var rucno = rl.Nalazi.Where(n => (n.Kljuc ?? "").StartsWith("rucno|")).ToList();
                    if (rucno.Count > 0)
                    {
                        rl.Nalazi.RemoveAll(n => (n.Kljuc ?? "").StartsWith("rucno|"));
                        var rubrike = rucno.Select(n => n.Rubrika).Where(x => !string.IsNullOrEmpty(x) && x != "–").Distinct()
                            .OrderBy(x => int.TryParse(Regex.Match(x, @"^\d+").Value, out var b) ? b : 999).ThenBy(x => x, StringComparer.Ordinal).ToList();
                        var tekst = (u.Engleski ? "Unreliably read boxes (OCR " : "Nepouzdano pročitane rubrike (OCR ") + cim.PouzdanostStrane.ToString("0.00") + "): "
                                    + string.Join(", ", rubrike) + (u.Engleski ? " – check manually." : " – proveriti ručno.");
                        rl.Nalazi.Add(new Nalaz { Pravilo = "NEPOUZDANO-OCR", Rubrika = "–", Tezina = Tezina.Info, TezinaPravila = Tezina.Info, Tekst = tekst, Uputstvo = tekst, SamoInterno = true, Kljuc = "nepouzdano-ocr" });
                    }
                }
                rl.Nalazi = SpojiPoRubrici(rl.Nalazi);
                // v2.9.2 (1): ponuda nije proverena (baza nedostupna) – pošiljaocu ništa dok se platilac ne proveri
                if (rl.Nalazi.Any(x => x.Kljuc == "ponuda-baza")) rl.ZaRucnuProveru = true;
                // v2.9.2 (3.5): kontrola pre slanja – svaka reč koju PDF primer dopisuje mora biti iz dokumenta, X šablon ili iz rečnika
                // primera; inače se dopuna ne upisuje, a izveštaj ide samo INTERNO (ručna provera)
                foreach (var n in rl.Nalazi.Where(x => !x.SamoInterno && !string.IsNullOrEmpty(x.Dopuna)).ToList())
                {
                    List<string> strane;
                    try { strane = Dopune.NepoznateReci(n, k); }
                    catch (Exception ex) { log?.Upozorenje("Kontrola dopune (" + n.Pravilo + ") nije izvršena: " + ex.Message); continue; }
                    if (strane.Count == 0) continue;
                    var tekst = "PDF primer: dopuna u r" + n.Rubrika + " „" + n.Dopuna + "“ sadrži reči kojih nema u dokumentu ni u rečniku primera ("
                                + string.Join(", ", strane) + ") – nije upisana; ručna provera pre odgovora pošiljaocu.";
                    log?.Upozorenje(tekst);
                    n.Dopuna = ""; n.Pogresno = ""; n.DopunaRedovi = null; n.DopunaPoPoljima = null; n.DopunaSigurna = false;
                    rl.Nalazi.Add(new Nalaz { Pravilo = "PRIMER-KONTROLA", Rubrika = n.Rubrika, Tezina = Tezina.Info, TezinaPravila = Tezina.Info, Tekst = tekst, Uputstvo = tekst, SamoInterno = true, Kljuc = "primer-kontrola|" + n.Rubrika + "|" + n.Pravilo });
                    rl.ZaRucnuProveru = true;
                }
                rez.Listovi.Add(rl);
            }
            return rez;
        }

        /// <summary>Prag pouzdanosti r62 na skenu: ispod njega (rukom upisana, kućice spornog čitanja) r62 ne određuje ništa.</summary>
        public const double PragR62 = 0.85;

        /// <summary>
        /// v2.8.4: r62 na skenu čije cifre nisu pouzdano pročitane (najmanja pouzdanost grupe cifara ispod <see cref="PragR62"/>,
        /// sporno čitanje, ili već nečitljiva) – ne koristi se za odluke. Šum oko cifara („+ gs“) ne čini r62 nepouzdanom.
        /// </summary>
        internal static bool R62Nepouzdana(CimList c) =>
            c != null && c.PouzdanostStrane > 0 && c.R62 != null && c.R62.Popunjena
            && (c.R62.Status == StatusRubrike.Necitljivo || c.R62.NepouzdanoCitanje || c.R62.PouzdanostCifara < PragR62 || (c.R62.NesigurniDelovi?.Count ?? 0) > 0);

        /// <summary>Označava r62 kao nečitljivu (status; tekst ostaje za internu napomenu) i vraća ono što je agent pročitao.</summary>
        internal static string OznaciR62Necitljivu(CimList c)
        {
            var r = c.R62;
            var procitano = string.Join(" / ", new[] { r.Zemlja, r.Stanica, r.Preduzece, r.Broj }.Select(x => string.IsNullOrWhiteSpace(x) ? "?" : x.Trim()));
            if (procitano.Replace("?", "").Replace("/", "").Trim().Length == 0) procitano = Regex.Replace(r.Tekst ?? "", @"\s+", " ").Trim();
            // samo za odluke (status i prikaz ostaju; postojeća pravila r62 rade kao ranije)
            r.NepouzdanaZaOdluke = true;
            return procitano;
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
                // dopuna za PDF primer: spajaju se različite dopune istog pravila u istoj rubrici
                if (string.IsNullOrEmpty(prvi.Dopuna)) { prvi.Dopuna = n.Dopuna ?? ""; prvi.Pogresno = n.Pogresno ?? ""; prvi.DopunaSigurna = n.DopunaSigurna; }
                else if (!string.IsNullOrEmpty(n.Dopuna) && prvi.Dopuna.IndexOf(n.Dopuna, StringComparison.OrdinalIgnoreCase) < 0) prvi.Dopuna += ", " + n.Dopuna;
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
            if (RubrikaLista(cim, pg.Rubrika)?.Status == StatusRubrike.Oznake && (string.IsNullOrEmpty(dodatak) || Regex.IsMatch(dodatak, @"^\s*\(?(rubrika je prazna|(the )?box is empty)", RegexOptions.IgnoreCase)))
                dodatak = en ? "Only the form labels are entered, without values." : "Upisane su samo oznake, bez vrednosti.";
            var tekst = uputstvo + (string.IsNullOrEmpty(dodatak) ? "" : " " + dodatak);
            return new Nalaz
            {
                Pravilo = def.Id, Rubrika = pg.Rubrika, Tezina = pg.Tezina ?? def.Tezina, TezinaPravila = pg.Tezina ?? def.Tezina, Tekst = tekst,
                Uputstvo = uputstvo, Detalj = dodatak ?? "",
                Vrednost = TekstRubrike(cim, pg.Rubrika),
                Primer = !string.IsNullOrEmpty(pg.Predlog) ? pg.Predlog : (en ? (string)def.Json["primer_en"] : null) ?? (string)def.Json["primer"] ?? "",
                Kljuc = pg.Kljuc ?? def.Id + "|" + pg.Rubrika + "|" + tekst,
                SamoInterno = pg.SamoInterno || (bool?)def.Json["samo_interno"] == true,
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
        /// <summary>v2.8.4 (8): i prateći dokumenti iz dosijea pošiljke – SAMO za priloženost (X26/X27 „naveden u r9, a nije priložen“, X22, R18-SPISAK).</summary>
        public readonly List<Dokument> SviSaDosijeom;
        public readonly List<(Dokument dok, SpisakKola s)> SpiskoviSaDosijeom;
        public IEnumerable<Dokument> TipSaDosijeom(string tip) => SviSaDosijeom.Where(d => d.Tip == tip);
        public readonly SortedSet<string> Necitljive = new SortedSet<string>(StringComparer.Ordinal);

        public Kontekst(CimList c, Dokument dok, UlazProvere u, PravilaSkup p)
        {
            C = c; Dok = dok; U = u; P = p;
            SviSaDosijeom = u.Dokumenti;
            Svi = u.Dokumenti.Where(d => !d.IzDosijea).ToList();
            Spiskovi = Svi.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak.Select(s => (d, s))).ToList();
            SpiskoviSaDosijeom = SviSaDosijeom.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak.Select(s => (d, s))).ToList();
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
                // v2.8.4: nečitljiva r62 (rukom upisana na skenu) ne određuje KP tovarni list
                default: Kp = u.InterniPosiljalac || (!c.R62.NepouzdanaZaOdluke && p.Kp.JeKpSifra(c.R62.Preduzece)); break;
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
