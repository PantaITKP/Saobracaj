using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class OsobaPlan
    {
        internal int DeSifra;
        internal string Naziv;
        internal string Telefon;
        internal string Email;
        internal bool Masinovodja;
        internal bool Pregledac;
        internal bool Manevrista;
        internal bool Vozovodja;
        internal bool Aktivan;
        internal bool UPlaniranju;
        internal int? MaticnaID;
        internal string Turnus;
        internal bool Auto;
        internal bool SamoDomicilna;
    }

    internal sealed class KvalifikacijaOsobe
    {
        internal int DeSifra;
        internal string Vrsta;
        internal int? LokomotivaID;
        internal int? PrugaID;
        internal DateTime? VaziDo;
    }

    internal sealed class SmenaOsobe
    {
        internal int DeSifra;
        internal DateTime Datum;
        internal string Vrsta;
        internal DateTime? Od;
        internal DateTime? Do;
        internal int? LokacijaPosleID;
    }

    internal sealed class OdsustvoOsobe
    {
        internal int DeSifra;
        internal string Vrsta;
        internal DateTime Od;
        internal DateTime Do;
    }

    internal sealed class ZadatakOsobe
    {
        internal int ID;
        internal int PosaoID;
        internal int DeSifra;
        internal string Uloga;
        internal string Status;
        internal int? StanicaID;
        internal DateTime Od;
        internal DateTime Do;
        internal int? PredajaStanicaID;
        internal string BrojNajave;
        internal string Osoba;
        internal string Telefon;
        internal string Email;
        internal byte[] RowVer;
        internal bool Aktivan { get { return Status == "POSLAT" || Status == "PRIHVACEN"; } }
    }

    internal sealed class OsobljeKontekst
    {
        internal PlanKontekst Plan;
        internal List<OsobaPlan> Osobe = new List<OsobaPlan>();
        internal List<KvalifikacijaOsobe> Kvalifikacije = new List<KvalifikacijaOsobe>();
        internal List<SmenaOsobe> Smene = new List<SmenaOsobe>();
        internal List<OdsustvoOsobe> Odsustva = new List<OdsustvoOsobe>();
        internal List<ZadatakOsobe> Zadaci = new List<ZadatakOsobe>();
    }

    internal sealed class TerminPosla
    {
        internal int PosaoID;
        internal string BrojNajave;
        internal DateTime Prijem;
        internal DateTime Polazak;
        internal DateTime Dolazak;
        internal DateTime Predaja;
        internal int? PrijemID;
        internal int? PredajaID;
        internal List<int> Lokomotive = new List<int>();
        internal List<int> Pruge = new List<int>();
    }

    internal sealed class KandidatOsoba
    {
        internal OsobaPlan Osoba;
        internal string Uloga;
        internal string Lokacija;
        internal bool? ImaLokomotivu;
        internal bool? ImaPrugu;
        internal string Zauzetost;
        internal string Ocena;
        internal bool Prolazi;
        internal int MinutaDoLokacije;
        // Put do stanice: osoba je slobodna i ide na teren.
        internal bool NaPutu;
        internal int? LokacijaID;
        // Osoba sa AUTO koja ide na isti teren, sa iste lokacije.
        internal KandidatOsoba Partner;
        // Vozač za osobu bez AUTO koja nije na lokaciji.
        internal KandidatOsoba Prevoz;
        // Početak zadatka kad je raniji od uobičajenog: vožnja para ili zadatak vozača.
        internal DateTime? TerminOd;
        internal DateTime? TerminDo;
        internal int? StanicaID;
        internal string Napomena;
    }

    // Pravilo 8.9.
    internal static class PonudaOsoblja
    {
        internal const string UlogaMasinovodja = "MASINOVODJA";
        internal const string UlogaPrijem = "PREGLEDAC_PRIJEM";
        internal const string UlogaPredaja = "PREGLEDAC_PREDAJA";
        internal const string UlogaPrevozMasinovodje = "PREVOZ_MASINOVODJA";
        internal const string UlogaPrevozPrijem = "PREVOZ_PRIJEM";
        internal const string UlogaPrevozPredaja = "PREVOZ_PREDAJA";

        // Svaka uloga ima svoju ulogu prevoza, jer na jednom poslu može da se vozi više osoba.
        internal static string UlogaPrevoza(string uloga)
        {
            return uloga == UlogaMasinovodja ? UlogaPrevozMasinovodje : uloga == UlogaPrijem ? UlogaPrevozPrijem : UlogaPrevozPredaja;
        }

        internal static bool JePrevoz(string uloga)
        {
            return uloga == UlogaPrevozMasinovodje || uloga == UlogaPrevozPrijem || uloga == UlogaPrevozPredaja;
        }

        internal static string UlogaPutnika(string prevoz)
        {
            return prevoz == UlogaPrevozMasinovodje ? UlogaMasinovodja : prevoz == UlogaPrevozPrijem ? UlogaPrijem : UlogaPredaja;
        }

        private static int MinutaZaKm(decimal km, decimal brzina)
        {
            return brzina > 0 ? (int)Math.Ceiling(km / brzina * 60) : 0;
        }

        private static bool Slobodan(OsobljeKontekst k, int osoba, int posao, DateTime od, DateTime doVreme)
        {
            return Odsustvo(k, osoba, od, doVreme) == null && Preklapanje(k, osoba, posao, od, doVreme, 0) == null
                && k.Smene.Any(s => s.DeSifra == osoba && Pokriva(s, od, doVreme));
        }

        // Najbliži slobodan iz osoblja sa AUTO (mašinovođa, pregledač, vozovođa, manevrista): dolazi do putnika, pa ga vozi do cilja. Rastojanje je po mreži.
        private static KandidatOsoba NadjiPrevoz(OsobljeKontekst k, OsobaPlan putnik, int lokacijaPutnika, int cilj, DateTime dolazak, int posao, string uloga, decimal brzina, OsobaPlan drugiPutnik = null)
        {
            decimal doCilja;
            if (!k.Plan.Graf.Rastojanja(cilj).TryGetValue(lokacijaPutnika, out doCilja)) return null;
            Dictionary<int, decimal> doPutnika = k.Plan.Graf.Rastojanja(lokacijaPutnika);
            string ulogaPrevoza = UlogaPrevoza(uloga);
            KandidatOsoba najbolji = null;
            foreach (OsobaPlan v in k.Osobe.Where(x => x.Auto && (x.Masinovodja || x.Pregledac || x.Vozovodja || x.Manevrista) && x.Aktivan && x.UPlaniranju && x.DeSifra != putnik.DeSifra && (drugiPutnik == null || x.DeSifra != drugiPutnik.DeSifra)))
            {
                string izvor;
                int? lokacija = Lokacija(k, v, dolazak, out izvor);
                decimal km;
                if (!lokacija.HasValue || !doPutnika.TryGetValue(lokacija.Value, out km)) continue;
                int minuta = Math.Max(1, MinutaZaKm(km + doCilja, brzina));
                DateTime od = dolazak.AddMinutes(-minuta);
                if (najbolji != null && minuta >= najbolji.MinutaDoLokacije) continue;
                if (k.Zadaci.Any(z => z.DeSifra == v.DeSifra && z.PosaoID == posao && z.Uloga == ulogaPrevoza && z.Status == "ODBIJEN")) continue;
                if (!Slobodan(k, v.DeSifra, posao, od, dolazak)) continue;
                najbolji = new KandidatOsoba
                {
                    Osoba = v,
                    Uloga = ulogaPrevoza,
                    Prolazi = true,
                    MinutaDoLokacije = minuta,
                    TerminOd = od,
                    TerminDo = dolazak,
                    StanicaID = cilj,
                    Napomena = "Prevoz: " + putnik.Naziv + (drugiPutnik != null ? " i " + drugiPutnik.Naziv : "") + " od " + k.Plan.Naziv(lokacijaPutnika) + " do " + k.Plan.Naziv(cilj) + ", dolazak " + dolazak.ToString("dd.MM. HH:mm") + "."
                };
            }
            return najbolji;
        }

        // Ko nije na lokaciji: sa AUTO ide sam, a bez AUTO mu treba vozač.
        private static void OceniPut(OsobljeKontekst k, KandidatOsoba c, int? lokacija, int? cilj, DateTime dolazak, int posao, decimal brzina, int minuta)
        {
            c.MinutaDoLokacije = minuta;
            c.NaPutu = true;
            c.LokacijaID = lokacija;
            string dolaziZa = "Nije na lokaciji" + (minuta > 0 ? " (+ " + (minuta / 60) + " h " + (minuta % 60) + " min)" : "");
            if (c.Osoba.Auto)
            {
                c.Prolazi = true;
                c.Ocena = dolaziZa;
                return;
            }
            c.Prevoz = lokacija.HasValue && cilj.HasValue ? NadjiPrevoz(k, c.Osoba, lokacija.Value, cilj.Value, dolazak, posao, c.Uloga, brzina) : null;
            c.Prolazi = c.Prevoz != null;
            c.Ocena = c.Prevoz != null ? dolaziZa + ", prevoz: " + c.Prevoz.Osoba.Naziv : "Nema AUTO, nema ko da ga preveze";
        }

        private static string Dolazi(int minuta)
        {
            return "Nije na lokaciji" + (minuta > 0 ? " (+ " + (minuta / 60) + " h " + (minuta % 60) + " min)" : "");
        }

        // Mašinovođa i pregledač za prijem idu u istu stanicu: oboje moraju stići do prijema.
        private static bool MoguZajedno(OsobljeKontekst k, TerminPosla t, KandidatOsoba a, KandidatOsoba b)
        {
            if (!a.NaPutu || !b.NaPutu || !a.LokacijaID.HasValue || a.LokacijaID != b.LokacijaID || a.Osoba.DeSifra == b.Osoba.DeSifra) return false;
            DateTime od = t.Prijem.AddMinutes(-Math.Max(a.MinutaDoLokacije, b.MinutaDoLokacije));
            return Slobodan(k, a.Osoba.DeSifra, t.PosaoID, od, t.Prijem) && Slobodan(k, b.Osoba.DeSifra, t.PosaoID, od, t.Prijem);
        }

        // Ko nema AUTO prvo traži par sa AUTO; vozač iz osoblja ostaje rezerva. Par ide ispred onih kojima treba vozač.
        internal static void Upari(OsobljeKontekst k, TerminPosla t, List<KandidatOsoba> masinovodje, List<KandidatOsoba> zaPrijem)
        {
            UpariListu(k, t, masinovodje, zaPrijem);
            UpariListu(k, t, zaPrijem, masinovodje);
            foreach (List<KandidatOsoba> l in new[] { masinovodje, zaPrijem })
            {
                List<KandidatOsoba> poredak = l.OrderBy(x => x.Prolazi ? 0 : 1).ThenBy(x => x.Prevoz != null && x.Partner == null ? 1 : 0).ThenBy(x => x.MinutaDoLokacije).ThenBy(x => x.Osoba.Naziv).ToList();
                l.Clear();
                l.AddRange(poredak);
            }
        }

        private static void UpariListu(OsobljeKontekst k, TerminPosla t, List<KandidatOsoba> lista, List<KandidatOsoba> drugi)
        {
            foreach (KandidatOsoba c in lista.Where(x => x.NaPutu && !x.Osoba.Auto))
            {
                c.Partner = drugi.Where(x => x.Osoba.Auto && MoguZajedno(k, t, c, x)).OrderBy(x => x.Osoba.Naziv).FirstOrDefault();
                if (c.Partner == null) continue;
                c.Prolazi = true;
                c.Ocena = Dolazi(c.MinutaDoLokacije) + ", ide sa: " + c.Partner.Osoba.Naziv;
            }
        }

        // Vozači za izabrani par. Ako jedno od dvoje ima AUTO, vozač ne treba; ako nema niko, isti vozač vozi oboje.
        internal static List<KandidatOsoba> Prevozi(OsobljeKontekst k, TerminPosla t, KandidatOsoba m, KandidatOsoba p, List<string> greske)
        {
            var vozaci = new List<KandidatOsoba>();
            foreach (KandidatOsoba c in new[] { m, p }.Where(x => x != null)) { c.TerminOd = null; c.Napomena = null; }
            if (m != null && p != null && MoguZajedno(k, t, m, p))
            {
                DateTime od = t.Prijem.AddMinutes(-Math.Max(m.MinutaDoLokacije, p.MinutaDoLokacije));
                if (m.Osoba.Auto || p.Osoba.Auto)
                {
                    KandidatOsoba vozi = m.Osoba.Auto ? m : p, sedi = m.Osoba.Auto ? p : m;
                    vozi.Napomena = "Vozi: " + sedi.Osoba.Naziv + ".";
                    sedi.Napomena = "Putuje sa: " + vozi.Osoba.Naziv + ".";
                    vozi.TerminOd = od;
                    sedi.TerminOd = od;
                    return vozaci;
                }
                decimal brzina = k.Plan.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", t.Prijem, null, null, null, 50);
                KandidatOsoba zajednicki = NadjiPrevoz(k, m.Osoba, m.LokacijaID.Value, t.PrijemID.Value, t.Prijem, t.PosaoID, UlogaMasinovodja, brzina, p.Osoba);
                if (zajednicki != null) vozaci.Add(zajednicki);
                else greske.Add("Niko ne može da preveze: " + m.Osoba.Naziv + " i " + p.Osoba.Naziv + ".");
                return vozaci;
            }
            foreach (KandidatOsoba c in new[] { m, p }.Where(x => x != null && x.NaPutu && !x.Osoba.Auto))
            {
                if (c.Prevoz != null) vozaci.Add(c.Prevoz);
                else greske.Add(c.Osoba.Naziv + " nema AUTO. Izaberite " + (c.Uloga == UlogaMasinovodja ? "pregledača" : "mašinovođu") + " sa AUTO koji ide sa iste lokacije.");
            }
            return vozaci;
        }

        private static bool Pokriva(SmenaOsobe s, DateTime od, DateTime doVreme)
        {
            return s.Od.HasValue && s.Do.HasValue && s.Vrsta != "SLOBODAN" && s.Od.Value <= od && s.Do.Value >= doVreme;
        }

        private static string Odsustvo(OsobljeKontekst k, int osoba, DateTime od, DateTime doVreme)
        {
            OdsustvoOsobe o = k.Odsustva.FirstOrDefault(x => x.DeSifra == osoba && x.Od.Date <= doVreme.Date && x.Do.Date >= od.Date);
            return o == null ? null : Vrsta(o.Vrsta);
        }

        private static string Vrsta(string v)
        {
            switch (v)
            {
                case "BOLOVANJE": return "Bolovanje";
                case "GODISNJI": return "Godišnji";
                case "SLOBODAN": return "Slobodan";
                default: return "Odsustvo";
            }
        }

        private static ZadatakOsobe Preklapanje(OsobljeKontekst k, int osoba, int posao, DateTime od, DateTime doVreme, decimal odmorH)
        {
            return k.Zadaci.FirstOrDefault(z => z.DeSifra == osoba && z.PosaoID != posao && z.Aktivan && z.Od < doVreme.AddHours((double)odmorH) && z.Do > od.AddHours(-(double)odmorH));
        }

        private static bool Kvalifikovan(OsobljeKontekst k, int osoba, string vrsta, int vrednost, DateTime datum)
        {
            return k.Kvalifikacije.Any(q => q.DeSifra == osoba && q.Vrsta == vrsta && (vrsta == "LOKOMOTIVA" ? q.LokomotivaID == vrednost : q.PrugaID == vrednost)
                && (!q.VaziDo.HasValue || q.VaziDo.Value.Date >= datum.Date));
        }

        // Stanica predaje poslednjeg zadatka, zatim lokacija posle smene, zatim matična stanica.
        private static int? Lokacija(OsobljeKontekst k, OsobaPlan o, DateTime trenutak, out string izvor)
        {
            ZadatakOsobe z = k.Zadaci.Where(x => x.DeSifra == o.DeSifra && x.Aktivan && x.Uloga == UlogaMasinovodja && x.Do <= trenutak).OrderByDescending(x => x.Do).FirstOrDefault();
            if (z?.PredajaStanicaID != null) { izvor = "posle smene"; return z.PredajaStanicaID; }
            SmenaOsobe s = k.Smene.Where(x => x.DeSifra == o.DeSifra && x.LokacijaPosleID.HasValue && x.Datum < trenutak.Date).OrderByDescending(x => x.Datum).FirstOrDefault();
            if (s != null) { izvor = "posle smene"; return s.LokacijaPosleID; }
            izvor = "matična";
            return o.MaticnaID;
        }

        private static string Termin(ZadatakOsobe z)
        {
            return "Najava " + z.BrojNajave + " " + z.Od.ToString("dd.MM. HH:mm") + "–" + z.Do.ToString("HH:mm");
        }

        internal static List<KandidatOsoba> Masinovodje(OsobljeKontekst k, TerminPosla t)
        {
            decimal maxRad = k.Plan.Parametar("MAX_RAD_H", t.Polazak, null, null, null, 12);
            decimal odmor = k.Plan.Parametar("ODMOR_POSLE_SMENE_H", t.Polazak, null, null, null, 12);
            decimal brzina = k.Plan.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", t.Polazak, null, null, null, 50);
            Dictionary<int, decimal> dist = t.PrijemID.HasValue ? k.Plan.Graf.Rastojanja(t.PrijemID.Value) : new Dictionary<int, decimal>();
            var lista = new List<KandidatOsoba>();
            foreach (OsobaPlan o in k.Osobe.Where(x => x.Masinovodja && x.Aktivan && x.UPlaniranju))
            {
                string izvor;
                int? lok = Lokacija(k, o, t.Polazak, out izvor);
                var c = new KandidatOsoba { Osoba = o, Uloga = UlogaMasinovodja, Lokacija = k.Plan.Naziv(lok) + " (" + izvor + ")", Zauzetost = "Slobodan" };
                List<int> bezLokomotive = t.Lokomotive.Where(l => !Kvalifikovan(k, o.DeSifra, "LOKOMOTIVA", l, t.Polazak)).ToList();
                List<int> bezPruge = t.Pruge.Where(p => !Kvalifikovan(k, o.DeSifra, "PRUGA", p, t.Polazak)).ToList();
                c.ImaLokomotivu = bezLokomotive.Count == 0;
                c.ImaPrugu = bezPruge.Count == 0;

                string odsustvo = Odsustvo(k, o.DeSifra, t.Polazak, t.Predaja);
                ZadatakOsobe drugi = Preklapanje(k, o.DeSifra, t.PosaoID, t.Polazak, t.Predaja, odmor);
                bool odbio = k.Zadaci.Any(z => z.DeSifra == o.DeSifra && z.PosaoID == t.PosaoID && z.Uloga == UlogaMasinovodja && z.Status == "ODBIJEN");
                bool smena = k.Smene.Any(s => s.DeSifra == o.DeSifra && Pokriva(s, t.Polazak, t.Predaja));
                bool predugo = (decimal)(t.Predaja - t.Polazak).TotalHours > maxRad;

                if (odbio) c.Ocena = "Odbio zadatak";
                else if (odsustvo != null) { c.Ocena = "Nedostupan"; c.Zauzetost = odsustvo; }
                else if (drugi != null) { c.Ocena = "Zauzet"; c.Zauzetost = Termin(drugi); }
                else if (!smena) { c.Ocena = "Nedostupan"; c.Zauzetost = "Nema smenu u terminu"; }
                else if (predugo) { c.Ocena = "Nedostupan"; c.Zauzetost = "Posao je duži od " + maxRad + " h"; }
                else if (bezLokomotive.Count > 0) c.Ocena = "Bez lokomotive " + string.Join(", ", bezLokomotive.Select(l => BrojLokomotive(k, l)));
                else if (bezPruge.Count > 0) c.Ocena = "Bez pruge " + string.Join(", ", bezPruge);
                else
                {
                    if (lok == t.PrijemID) { c.Prolazi = true; c.Ocena = "Preporučen"; }
                    else
                    {
                        decimal km;
                        int min = lok.HasValue && dist.TryGetValue(lok.Value, out km) ? MinutaZaKm(km, brzina) : 0;
                        OceniPut(k, c, lok, t.PrijemID, t.Polazak, t.PosaoID, brzina, min);
                    }
                }
                lista.Add(c);
            }
            return lista.OrderBy(x => x.Prolazi ? 0 : 1).ThenBy(x => x.MinutaDoLokacije).ThenBy(x => x.Osoba.Naziv).ToList();
        }

        internal static List<KandidatOsoba> Pregledaci(OsobljeKontekst k, TerminPosla t, string uloga)
        {
            bool prijem = uloga == UlogaPrijem;
            int? stanica = prijem ? t.PrijemID : t.PredajaID;
            int obrada = (int)k.Plan.Parametar(prijem ? "OBRADA_PRIJEM_MIN" : "OBRADA_PREDAJA_MIN", t.Prijem, stanica, null, null, 60);
            DateTime vreme = prijem ? t.Prijem : t.Dolazak;
            DateTime od = vreme.AddMinutes(-obrada), doVreme = vreme.AddMinutes(obrada);
            var lista = new List<KandidatOsoba>();
            if (!stanica.HasValue) return lista;
            decimal brzina = k.Plan.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", vreme, null, null, null, 50);
            Dictionary<int, decimal> dist = k.Plan.Graf.Rastojanja(stanica.Value);
            // Samo domicilna: pregledač radi samo u svojoj stanici.
            foreach (OsobaPlan o in k.Osobe.Where(x => (x.Pregledac || x.Manevrista) && x.Aktivan && x.UPlaniranju && (!x.SamoDomicilna || x.MaticnaID == stanica)))
            {
                string izvor;
                int? lok = Lokacija(k, o, vreme, out izvor);
                var c = new KandidatOsoba { Osoba = o, Uloga = uloga, Lokacija = k.Plan.Naziv(lok) + " (" + izvor + ")", Zauzetost = "Slobodan" };
                decimal km;
                int minuta = lok.HasValue && lok != stanica && dist.TryGetValue(lok.Value, out km) ? MinutaZaKm(km, brzina) : 0;
                DateTime slobodanOd = vreme.AddMinutes(-minuta);
                string odsustvo = Odsustvo(k, o.DeSifra, od < slobodanOd ? od : slobodanOd, doVreme);
                ZadatakOsobe drugi = Preklapanje(k, o.DeSifra, t.PosaoID, slobodanOd, vreme.AddMinutes(obrada), 0);
                bool odbio = k.Zadaci.Any(z => z.DeSifra == o.DeSifra && z.PosaoID == t.PosaoID && z.Uloga == uloga && z.Status == "ODBIJEN");
                if (odbio) c.Ocena = "Odbio zadatak";
                else if (odsustvo != null) { c.Ocena = "Nedostupan"; c.Zauzetost = odsustvo; }
                else if (drugi != null) { c.Ocena = "Zauzet"; c.Zauzetost = Termin(drugi); }
                else if (!k.Smene.Any(s => s.DeSifra == o.DeSifra && Pokriva(s, od < slobodanOd ? od : slobodanOd, doVreme))) { c.Ocena = "Nedostupan"; c.Zauzetost = "Nema smenu u terminu"; }
                else if (!lok.HasValue) c.Ocena = "Lokacija nije poznata";
                else if (lok == stanica) { c.Ocena = "Preporučen"; c.Prolazi = true; }
                else OceniPut(k, c, lok, stanica, vreme, t.PosaoID, brzina, minuta);
                lista.Add(c);
            }
            return lista.OrderBy(x => x.Prolazi ? 0 : 1).ThenBy(x => x.MinutaDoLokacije).ThenBy(x => x.Osoba.Naziv).ToList();
        }

        private static string BrojLokomotive(OsobljeKontekst k, int lokomotiva)
        {
            return k.Plan.Lokomotive.Where(l => l.ID == lokomotiva).Select(l => l.Broj).FirstOrDefault() ?? lokomotiva.ToString();
        }

        internal static TerminPosla Termin(PlanKontekst k, RezultatPosla r)
        {
            var t = new TerminPosla { PosaoID = r.Posao.ID, BrojNajave = r.Posao.BrojNajave, Prijem = r.Posao.Prijem, Polazak = r.Polazak, Dolazak = r.Dolazak, Predaja = r.Predaja, PrijemID = r.Posao.PrijemID, PredajaID = r.Posao.PredajaID };
            foreach (ZauzetostLok z in k.Kalendar.Where(x => x.PosaoID == r.Posao.ID && x.Vrsta == "POSAO"))
                if (!t.Lokomotive.Contains(z.LokomotivaID)) t.Lokomotive.Add(z.LokomotivaID);
            t.Pruge.AddRange(r.Ruta.Where(d => d.PrugaID.HasValue).Select(d => d.PrugaID.Value).Distinct());
            return t;
        }

        // Pravilo 8.7, deo koji se odnosi na osoblje.
        internal static List<Nalaz> Nalazi(OsobljeKontekst k, IEnumerable<RezultatPosla> rezultati)
        {
            var nalazi = new List<Nalaz>();
            foreach (RezultatPosla r in rezultati)
            {
                if (!k.Plan.Kalendar.Any(z => z.PosaoID == r.Posao.ID && z.Vrsta == "POSAO") || r.Posao.Faza >= 5) continue;
                List<ZadatakOsobe> zadaci = k.Zadaci.Where(z => z.PosaoID == r.Posao.ID && z.Uloga == UlogaMasinovodja && z.Aktivan).ToList();
                TerminPosla termin = Termin(k.Plan, r);
                List<KandidatOsoba> ponuda = Masinovodje(k, termin);
                Upari(k, termin, ponuda, Pregledaci(k, termin, UlogaPrijem));
                if (zadaci.Count == 0 && !ponuda.Any(c => c.Prolazi))
                    nalazi.Add(new Nalaz(NivoNalaza.Konflikt, "Nema mašinovođe sa kvalifikacijom", "Najava " + r.Posao.BrojNajave + " u terminu " + r.Polazak.ToString("dd.MM. HH:mm") + " – " + r.Predaja.ToString("HH:mm") + ".", r.Posao.ID));
                decimal rezerva = k.Plan.Parametar("REZERVA_PRE_POLASKA_MIN", r.Polazak, null, null, null, 30);
                if (k.Plan.Sada >= r.Polazak.AddMinutes(-(double)rezerva) && k.Zadaci.Any(z => z.PosaoID == r.Posao.ID && z.Status == "POSLAT"))
                    nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Zadatak nije prihvaćen", "Najava " + r.Posao.BrojNajave + ": polazak je " + r.Polazak.ToString("dd.MM. HH:mm") + ".", r.Posao.ID));
            }
            return nalazi;
        }
    }
}
