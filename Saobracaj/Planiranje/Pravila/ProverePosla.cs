using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Svi podaci potrebni računu su u memoriji, pa se račun može raditi van baze i za nacrt plana.
    internal sealed class PlanKontekst
    {
        internal Graf Graf;
        internal List<VezaMreze> Veze = new List<VezaMreze>();
        internal List<TrasaOP> Trase = new List<TrasaOP>();
        internal List<TelegramOP> Telegrami = new List<TelegramOP>();
        internal List<RedMase> Tablica = new List<RedMase>();
        internal List<LokomotivaOP> Lokomotive = new List<LokomotivaOP>();
        internal List<ZauzetostLok> Kalendar = new List<ZauzetostLok>();
        internal Dictionary<string, List<ParametarRed>> Parametri = new Dictionary<string, List<ParametarRed>>();
        internal Dictionary<int, string> NaziviStanica = new Dictionary<int, string>();
        internal List<MestoPruge> MestaPruga = new List<MestoPruge>();
        internal DateTime Sada = DateTime.Now;

        private Dictionary<string, MestoPruge> indeksMesta;

        private MestoPruge Mesto(int pruga, int stanica)
        {
            if (indeksMesta == null)
            {
                indeksMesta = new Dictionary<string, MestoPruge>();
                foreach (MestoPruge m in MestaPruga)
                {
                    string kljuc = m.PrugaID + "|" + m.StanicaID;
                    if (!indeksMesta.ContainsKey(kljuc)) indeksMesta[kljuc] = m;
                }
            }
            MestoPruge r;
            return indeksMesta.TryGetValue(pruga + "|" + stanica, out r) ? r : null;
        }

        internal int? RBNaPruzi(int pruga, int stanica)
        {
            MestoPruge m = Mesto(pruga, stanica);
            return m == null ? (int?)null : m.RB;
        }

        internal MestoPruge MestoNaPruzi(int pruga, int stanica)
        {
            return Mesto(pruga, stanica);
        }

        // AB je smer rasta rednih brojeva mesta na pruzi.
        internal string Smer(DeonicaRute d)
        {
            if (!d.PrugaID.HasValue) return null;
            int? a = RBNaPruzi(d.PrugaID.Value, d.OdID), b = RBNaPruzi(d.PrugaID.Value, d.DoID);
            if (!a.HasValue || !b.HasValue || a == b) return null;
            return a < b ? "AB" : "BA";
        }

        internal decimal Parametar(string kljuc, DateTime datum, int? stanica, int? stanicaDo, int? serija, decimal podrazumevano)
        {
            List<ParametarRed> redovi;
            if (!Parametri.TryGetValue(kljuc, out redovi)) return podrazumevano;
            return ParametriPravila.Nadji(redovi, datum, stanica, stanicaDo, serija) ?? podrazumevano;
        }

        internal decimal? ParametarIliNull(string kljuc, DateTime datum, int? stanica, int? stanicaDo)
        {
            List<ParametarRed> redovi;
            return Parametri.TryGetValue(kljuc, out redovi) ? ParametriPravila.Nadji(redovi, datum, stanica, stanicaDo) : null;
        }

        internal string Naziv(int? stanica)
        {
            string n;
            return stanica.HasValue && NaziviStanica.TryGetValue(stanica.Value, out n) ? n : "(nepoznata stanica)";
        }

        // Godišnje trase važe za godinu prijema; ad hoc odobrene u svom periodu, a zahtev samo za svoju najavu.
        internal IEnumerable<TrasaOP> TraseZa(PosaoPlan p)
        {
            foreach (TrasaOP t in Trase)
            {
                if (!t.AdHoc)
                {
                    if (!t.VaziOd.HasValue || (t.VaziOd.Value.Date <= p.Prijem.Date && (!t.VaziDo.HasValue || p.Prijem.Date <= t.VaziDo.Value.Date)))
                        yield return t;
                }
                else if (t.AdHocStatus == "ZAHTEV")
                {
                    if (t.AdHocNajavaID == p.NajavaID) yield return t;
                }
                else if (t.AdHocStatus == "ODOBRENA" && (!t.VaziOd.HasValue || t.VaziOd.Value.Date <= p.Prijem.Date) && (!t.VaziDo.HasValue || p.Prijem.Date <= t.VaziDo.Value.Date))
                    yield return t;
            }
        }
    }

    internal static class PlanRacun
    {
        // Kola teža od ovoga znače da je red Teretnice unet u kilogramima.
        private const decimal NajvecaMasaKola = 200;
        private const decimal BrzinaBezParametra = 40;

        internal static RezultatPosla Izracunaj(PlanKontekst k, PosaoPlan p)
        {
            var r = new RezultatPosla { Posao = p };
            OdrediMasu(r);
            OdrediVremena(k, r);
            if (r.ZatvorBezAlternative)
                r.Nalazi.Add(new Nalaz(NivoNalaza.Konflikt, "Zatvor bez alternativne rute", "Najava " + p.BrojNajave + ": " + p.Relacija, p.ID));
            if (r.Ruta.Count == 0 && (p.PrijemID != p.PredajaID || !p.PrijemID.HasValue))
                r.Nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Nema rute", "Najava " + p.BrojNajave + ": stanice " + p.Relacija + " nisu povezane u mreži.", p.ID));
            if (r.MasaIzNajave)
                r.Nalazi.Add(new Nalaz(NivoNalaza.Informacija, "Masa iz najave", "Najava " + p.BrojNajave + " čeka Teretnicu.", p.ID));
            if (r.Ruta.Any(d => d.Stanje == "NEMAMO_TRASU"))
                r.Nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Nemamo trasu", "Najava " + p.BrojNajave + ": " + r.Ruta.Count(d => d.Stanje == "NEMAMO_TRASU") + " deonica bez trase.", p.ID));
            if (p.PredvidjenaPredaja.HasValue && r.Predaja > p.PredvidjenaPredaja.Value)
                r.Nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Predaja posle predviđene", "Najava " + p.BrojNajave + ": " + r.Predaja.ToString("dd.MM. HH:mm") + " (predviđeno " + p.PredvidjenaPredaja.Value.ToString("dd.MM. HH:mm") + ").", p.ID));
            return r;
        }

        private static void OdrediMasu(RezultatPosla r)
        {
            PosaoPlan p = r.Posao;
            if (p.BrutoTeretnica.HasValue && p.BrutoTeretnica.Value > 0)
            {
                decimal bruto = p.BrutoTeretnica.Value;
                if (p.BrojKola.GetValueOrDefault() > 0 && bruto / p.BrojKola.Value > NajvecaMasaKola) bruto /= 1000;
                r.Bruto = bruto;
                return;
            }
            r.Bruto = p.BrutoNajava ?? 0;
            r.MasaIzNajave = true;
        }

        // Pravilo 8.3. Datoteka trasa može biti bez vremena, pa se tada računa iz parametara.
        private static void OdrediVremena(PlanKontekst k, RezultatPosla r)
        {
            PosaoPlan p = r.Posao;
            int obradaPrijem = (int)k.Parametar("OBRADA_PRIJEM_MIN", p.Prijem, p.PrijemID, null, null, 60);
            int obradaPredaja = (int)k.Parametar("OBRADA_PREDAJA_MIN", p.Prijem, p.PredajaID, null, null, 60);
            bool bezAlternative;
            DateTime kraj = p.PredvidjenaPredaja.HasValue && p.PredvidjenaPredaja.Value > p.Prijem ? p.PredvidjenaPredaja.Value : p.Prijem.AddHours(24);
            r.Ruta = Ruta.Izracunaj(k, p, p.Prijem, kraj, out bezAlternative, r.Nalazi, r.Telegrami);
            r.ZatvorBezAlternative = bezAlternative;

            DateTime najranijiPolazak = p.Prijem.AddMinutes(obradaPrijem);
            TrasaOP prva = TrasaDeonice(k, r.Ruta.FirstOrDefault()), poslednja = TrasaDeonice(k, r.Ruta.LastOrDefault());
            if (prva != null && poslednja != null && prva.Polazak.HasValue && poslednja.Dolazak.HasValue)
            {
                DateTime polazak = p.Prijem.Date + prva.Polazak.Value;
                if (polazak < najranijiPolazak) polazak = polazak.AddDays(1);
                DateTime dolazak = polazak.Date.AddDays(poslednja.DanPlus) + poslednja.Dolazak.Value;
                while (dolazak <= polazak) dolazak = dolazak.AddDays(1);
                r.Polazak = polazak;
                r.Dolazak = dolazak;
                r.TrasaID = prva.ID == poslednja.ID ? (int?)prva.ID : null;
            }
            else
            {
                r.Polazak = najranijiPolazak;
                decimal? voznja = k.ParametarIliNull("VOZNJA_MIN", p.Prijem, p.PrijemID, p.PredajaID);
                if (!voznja.HasValue)
                {
                    voznja = r.Km / BrzinaBezParametra * 60;
                    if (r.Ruta.Count > 0)
                        r.Nalazi.Add(new Nalaz(NivoNalaza.Informacija, "Vreme vožnje je procena", "Najava " + p.BrojNajave + ": nema parametra VOZNJA_MIN, računato " + BrzinaBezParametra + " km/h.", p.ID));
                }
                r.Dolazak = r.Polazak.AddMinutes((double)voznja.Value);
            }
            r.Predaja = r.Dolazak.AddMinutes(obradaPredaja);
        }

        private static TrasaOP TrasaDeonice(PlanKontekst k, DeonicaRute d)
        {
            return d == null || !d.TrasaID.HasValue ? null : k.Trase.FirstOrDefault(t => t.ID == d.TrasaID.Value);
        }
    }

    // Pravilo 8.4: sedam provera za izabrane lokomotive.
    internal static class ProverePosla
    {
        internal const decimal OsovinaPoKolima = 4;

        internal static List<ProveraStavka> Proveri(PlanKontekst k, RezultatPosla r, IList<LokomotivaOP> izabrane)
        {
            var lista = new List<ProveraStavka>
            {
                Vuca(r, izabrane),
                Masa(k, r, izabrane),
                Duzina(k, r),
                Osovina(k, r),
                Zauzetost(k, r, izabrane),
                Trasa(r),
                Obrada(k, r)
            };
            for (int i = 0; i < lista.Count; i++) lista[i].RB = i + 1;
            return lista;
        }

        private static ProveraStavka S(string naziv, bool ok, string tekst)
        {
            return new ProveraStavka { Naziv = naziv, Ok = ok, Tekst = tekst };
        }

        private static ProveraStavka Vuca(RezultatPosla r, IList<LokomotivaOP> izabrane)
        {
            int ne = r.Ruta.Count(d => d.Elektrificirana == false), nepoznato = r.Ruta.Count(d => !d.Elektrificirana.HasValue);
            if (ne > 0 && izabrane.Any(l => l.Vuca != "D"))
                return S("Vuča", false, ne + " deonica nije elektrificirano, moguće su samo dizel lokomotive.");
            return S("Vuča", true, ne > 0 ? "Dizel vuča na neelektrificiranoj ruti." : nepoznato > 0 ? "Elektrifikacija nije poznata za " + nepoznato + " deonica." : "Sve deonice su elektrificirane.");
        }

        private static ProveraStavka Masa(PlanKontekst k, RezultatPosla r, IList<LokomotivaOP> izabrane)
        {
            if (izabrane.Count == 0) return S("Masa", false, "Lokomotiva nije izabrana.");
            MasaZaSeriju m = BrojLokomotiva.Odredi(k, r.Bruto, izabrane[0], r.Ruta, r.Posao.Prijem);
            if (m.BezTablice) return S("Masa", true, m.Tekst);
            if (m.Broj == 0) return S("Masa", false, m.Tekst);
            return S("Masa", m.Broj <= izabrane.Count, m.Broj <= izabrane.Count ? m.Tekst : "Potrebno je " + m.Broj + " lokomotive, izabrano " + izabrane.Count + ".");
        }

        private static ProveraStavka Duzina(PlanKontekst k, RezultatPosla r)
        {
            int? najmanja = null;
            foreach (DeonicaRute d in r.Ruta)
            {
                if (!d.PrugaID.HasValue) continue;
                string smer = k.Smer(d);
                foreach (int stanica in new[] { d.OdID, d.DoID })
                {
                    MestoPruge m = k.MestoNaPruzi(d.PrugaID.Value, stanica);
                    if (m == null) continue;
                    int? v = smer == "BA" ? m.MaxDuzinaBA : m.MaxDuzinaAB;
                    if (v.HasValue && v.Value > 0 && (!najmanja.HasValue || v.Value < najmanja.Value)) najmanja = v;
                }
            }
            if (!r.Posao.DuzinaM.HasValue || r.Posao.DuzinaM.Value <= 0) return S("Dužina voza", true, "Dužina voza nije uneta.");
            if (!najmanja.HasValue) return S("Dužina voza", true, "Na ruti nema podataka o najvećoj dužini.");
            bool ok = r.Posao.DuzinaM.Value <= najmanja.Value;
            return S("Dužina voza", ok, r.Posao.DuzinaM.Value.ToString("0.#") + " m, najviše " + najmanja.Value + " m na ruti.");
        }

        // Dozvoljeno opterećenje po klasi pruge (UIC); slovo je osnova, a broj se odnosi na dužnu masu.
        private static decimal? DozvoljenoPoOsovini(string klasa)
        {
            if (string.IsNullOrWhiteSpace(klasa)) return null;
            switch (char.ToUpperInvariant(klasa.Trim()[0]))
            {
                case 'A': return 16m;
                case 'B': return 18m;
                case 'C': return 20m;
                case 'D': return 22.5m;
                case 'E': return 25m;
            }
            return null;
        }

        private static ProveraStavka Osovina(PlanKontekst k, RezultatPosla r)
        {
            if (r.Posao.BrojKola.GetValueOrDefault() <= 0) return S("Osovinsko opterećenje", true, "Broj kola nije unet.");
            decimal? dozvoljeno = null;
            string klasa = null;
            foreach (DeonicaRute d in r.Ruta)
            {
                if (!d.PrugaID.HasValue) continue;
                foreach (int stanica in new[] { d.OdID, d.DoID })
                {
                    MestoPruge m = k.MestoNaPruzi(d.PrugaID.Value, stanica);
                    decimal? v = m == null ? null : DozvoljenoPoOsovini(m.Klasa);
                    if (v.HasValue && (!dozvoljeno.HasValue || v.Value < dozvoljeno.Value)) { dozvoljeno = v; klasa = m.Klasa; }
                }
            }
            if (!dozvoljeno.HasValue) return S("Osovinsko opterećenje", true, "Na ruti nema podataka o klasi pruge.");
            decimal poOsovini = r.Bruto / (r.Posao.BrojKola.Value * OsovinaPoKolima);
            return S("Osovinsko opterećenje", poOsovini <= dozvoljeno.Value,
                poOsovini.ToString("0.0", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " t po osovini, klasa " + klasa + " dozvoljava " + dozvoljeno.Value.ToString("0.0", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " t.");
        }

        private static ProveraStavka Zauzetost(PlanKontekst k, RezultatPosla r, IList<LokomotivaOP> izabrane)
        {
            foreach (LokomotivaOP l in izabrane)
            {
                ZauzetostLok z = PredlogLokomotiva.Prepreka(k.Kalendar, l.ID, r.Posao, r.Posao.Prijem, r.Predaja);
                if (z != null) return S("Zauzetost", false, l.Broj + ": " + PredlogLokomotiva.OpisPrepreke(z));
            }
            return S("Zauzetost", izabrane.Count > 0, izabrane.Count > 0 ? "Lokomotive su slobodne u terminu." : "Lokomotiva nije izabrana.");
        }

        private static ProveraStavka Trasa(RezultatPosla r)
        {
            int bez = r.Ruta.Count(d => d.Stanje != "IMA_TRASU");
            if (r.Ruta.Count == 0) return S("Trasa", false, "Ruta nije određena.");
            return S("Trasa", bez == 0, bez == 0 ? "Sve deonice imaju trasu." : bez + " od " + r.Ruta.Count + " deonica nema trasu.");
        }

        private static ProveraStavka Obrada(PlanKontekst k, RezultatPosla r)
        {
            int prijem = (int)k.Parametar("OBRADA_PRIJEM_MIN", r.Posao.Prijem, r.Posao.PrijemID, null, null, 60);
            int predaja = (int)k.Parametar("OBRADA_PREDAJA_MIN", r.Posao.Prijem, r.Posao.PredajaID, null, null, 60);
            bool ok = r.Polazak >= r.Posao.Prijem.AddMinutes(prijem) && r.Predaja >= r.Dolazak.AddMinutes(predaja);
            return S("Vreme obrade", ok, "Prijem " + prijem + " min, predaja " + predaja + " min.");
        }
    }
}
