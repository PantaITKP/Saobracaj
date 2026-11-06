using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class ZahtevRute
    {
        internal int OdID;
        internal int DoID;
        internal bool Elektricna;
        internal decimal? OsovinskoT;
        internal decimal? DuzinaM;
        internal DateTime Datum = DateTime.Today;
        internal bool SaKiM;
        internal ISet<int> RucnoZatvorene = new HashSet<int>();
        internal int BrojAlternativa = 1;
        // Zatvor iz telegrama ne isključuje deonicu, nego se samo prijavljuje.
        internal bool BezTelegrama;
        // Polazak na dan Datum: zatvori se gledaju od polaska narednih SatiVoznje sati (koliko otprilike traje vožnja).
        // Bez polaska ili za drugi dan važi ceo dan Datum.
        internal DateTime? Polazak;
        internal const int SatiVoznje = 6;
        // Električna lokomotiva: neelektrificirana deonica se preskače kao zatvor, a ne samo prijavljuje.
        internal bool ObaveznaElektro;

        private bool PolazakDanas { get { return Polazak.HasValue && Polazak.Value.Date == Datum.Date; } }
        internal DateTime ProzorOd { get { return PolazakDanas ? Polazak.Value : Datum.Date; } }
        internal DateTime ProzorDo { get { return PolazakDanas ? Polazak.Value.AddHours(SatiVoznje) : Datum.Date.AddDays(1); } }
    }

    // Deo rute koji voz po izabranim uslovima ne bi smeo da prođe.
    internal sealed class ZastojRute
    {
        internal string Tekst;
        internal decimal Km;
    }

    internal sealed class RuteSaZastojima
    {
        internal List<RezultatRute> Rute = new List<RezultatRute>();
        internal RezultatRute BezZastoja;
        // Najkraća ruta da nema telegrama, kad je ponuđena ruta obilazak zatvora.
        internal RezultatRute KrozZatvor;
        // Tražen je elektrificiran put, a postoji samo put sa deonicama bez kontaktne mreže.
        internal bool BezElektro;
        internal Graf Graf;
    }

    internal sealed class RezultatRute
    {
        internal List<DeonicaRute> Deonice = new List<DeonicaRute>();
        internal List<int> Mesta = new List<int>();
        internal List<string> Upozorenja = new List<string>();
        internal decimal Km;
        internal decimal KmMagistralno;
        internal decimal KmRegionalno;
        internal decimal KmLokalno;
        internal decimal KmManipulativno;
        internal decimal KmDizel;
        internal decimal KmElektro;
        internal decimal KmNepoznato;
        internal int? NajmanjiVmax;
        internal decimal? NajveciUspon;
        internal List<ZastojRute> Zastoji = new List<ZastojRute>();
    }

    internal static class RutaTekst
    {
        internal static string OpisSaZastojima(RuteSaZastojima rez)
        {
            RezultatRute prva = rez.Rute[0];
            string s = Opis(prva);
            if (rez.BezElektro)
                s += Environment.NewLine + Environment.NewLine + "NEMA ELEKTRIFICIRANOG PUTA: električna lokomotiva ne može da prođe ceo put; deonice bez kontaktne mreže su među zastojima.";
            if (rez.KrozZatvor != null)
                s += Environment.NewLine + Environment.NewLine + "OBILAZAK ZATVORA: najkraća ruta (" + rez.KrozZatvor.Km.ToString("N1") + " km) prolazi kroz zatvor, pa je ponuđen obilazak duži za "
                    + (prva.Km - rez.KrozZatvor.Km).ToString("N1") + " km." + Environment.NewLine
                    + string.Join(Environment.NewLine, rez.KrozZatvor.Zastoji.Where(z => z.Tekst.StartsWith("Zatvor")).Select(z => "• " + z.Tekst));
            if (prva.Zastoji.Count == 0) return s;
            if (prva.Zastoji.Any(z => z.Tekst.StartsWith("Zatvor")))
                s += Environment.NewLine + Environment.NewLine + "NEMA OBILASKA ZATVORA: svi putevi do odredišta prolaze kroz zatvor iz telegrama, pa je prikazana najkraća ruta kroz zatvor.";
            s += Environment.NewLine + Environment.NewLine + "ZASTOJI (samo obaveštenje):" + Environment.NewLine + string.Join(Environment.NewLine, prva.Zastoji.Select(z => "• " + z.Tekst));
            s += Environment.NewLine + Environment.NewLine + (rez.BezZastoja != null ? "Ruta bez zastoja: " + rez.BezZastoja.Km.ToString("N1") + " km." : "Ruta bez zastoja ne postoji.");
            return s;
        }

        internal static string Opis(RezultatRute r)
        {
            string s = "Ukupno " + r.Km.ToString("N1") + " km   ·   magistralno " + r.KmMagistralno.ToString("N1") + ", regionalno " + r.KmRegionalno.ToString("N1")
                + ", lokalno " + r.KmLokalno.ToString("N1") + (r.KmManipulativno > 0 ? ", manipulativno " + r.KmManipulativno.ToString("N1") : "")
                + "   ·   elektro " + r.KmElektro.ToString("N1") + ", dizel " + r.KmDizel.ToString("N1") + (r.KmNepoznato > 0 ? ", nepoznato " + r.KmNepoznato.ToString("N1") : "")
                + (r.NajmanjiVmax.HasValue ? "   ·   najmanji Vmax " + r.NajmanjiVmax.Value + " km/h" : "")
                + (r.NajveciUspon.HasValue ? "   ·   merodavni nagib " + r.NajveciUspon.Value.ToString("0.##") + " ‰" : "")
                + "   ·   službenih mesta " + r.Mesta.Count;
            return r.Upozorenja.Count == 0 ? s : s + Environment.NewLine + string.Join(Environment.NewLine, r.Upozorenja.Distinct());
        }
    }

    // Ruta po mreži uz ograničenja voza i zatvore iz telegrama (RAZVOJ_Mreza_i_stanice, odeljak 7).
    internal static class Rutiranje
    {
        // Dve najmanje vrednosti iz kolone Klasa određuju osovinsko: A 16, B 18, C 20, D 22,5 t.
        internal static decimal? OsovinskoIzKlase(string klasa)
        {
            if (string.IsNullOrWhiteSpace(klasa)) return null;
            switch (char.ToUpperInvariant(klasa.Trim()[0]))
            {
                case 'A': case 'А': return 16m;
                case 'B': case 'В': return 18m;
                case 'C': case 'С': return 20m;
                case 'D': return 22.5m;
                default: return null;
            }
        }

        // Veza bez podatka o vuči ili klasi ne blokira rutu, nego ide na listu upozorenja.
        private static bool Dozvoljena(VezaMreze v, ZahtevRute z)
        {
            if (v.KiM && !z.SaKiM) return false;
            if (z.RucnoZatvorene.Contains(v.ID)) return false;
            if (z.Elektricna && v.Elektrificirana == false) return false;
            if (z.OsovinskoT.HasValue && v.OsovinskoT.HasValue && v.OsovinskoT.Value < z.OsovinskoT.Value) return false;
            return true;
        }

        private static bool Zatvorena(PlanKontekst k, VezaMreze v, DateTime od, DateTime doVreme)
        {
            var d = new DeonicaRute { VezaID = v.ID, OdID = v.A, DoID = v.B, PrugaID = v.PrugaID };
            return k.Telegrami.Any(t => Ruta.JeZatvor(t) && Ruta.JeAktivan(t, od, doVreme) && Ruta.Pokriva(k, t, d));
        }

        // Mreža uz uslove voza; po njoj se računaju i rute i lanci trasa.
        internal static Graf GrafZa(PlanKontekst k, ZahtevRute z)
        {
            DateTime od = z.ProzorOd, doVreme = z.ProzorDo;
            return new Graf(k.Veze.Where(v => Dozvoljena(v, z) && (z.BezTelegrama || !Zatvorena(k, v, od, doVreme))).ToList());
        }

        // Ekran Mreža i stanice i ponuda trasa na poslu: deonica zatvorena telegramom se preskače i traži se obilazak.
        // Vuča i osovinsko ne blokiraju, nego su zastoji (obaveštenje). Ručni zatvor sa mape važi.
        // Tek kad obilaska nema, ruta ide kroz zatvor, a zatvor je među zastojima.
        internal static RuteSaZastojima IzracunajSaZastojima(PlanKontekst k, ZahtevRute z)
        {
            Func<bool, bool, bool, ZahtevRute> slobodan = (kim, bezTelegrama, elektro) => new ZahtevRute
            {
                OdID = z.OdID, DoID = z.DoID, DuzinaM = z.DuzinaM, Datum = z.Datum, Polazak = z.Polazak, SaKiM = kim,
                RucnoZatvorene = z.RucnoZatvorene, BrojAlternativa = z.BrojAlternativa, BezTelegrama = bezTelegrama, Elektricna = elektro
            };
            // Redom: uz zatvore, pa uz zatvore i preko KiM, pa kroz zatvore, pa kroz zatvore i preko KiM.
            // Za obaveznu elektro vuču isti redosled prvo samo po elektrificiranoj mreži, pa tek onda bez tog uslova.
            var pokusaji = new List<ZahtevRute>();
            foreach (bool elektro in z.ObaveznaElektro && z.Elektricna ? new[] { true, false } : new[] { false })
            {
                pokusaji.Add(slobodan(z.SaKiM, false, elektro));
                if (!z.SaKiM) pokusaji.Add(slobodan(true, false, elektro));
                pokusaji.Add(slobodan(z.SaKiM, true, elektro));
                if (!z.SaKiM) pokusaji.Add(slobodan(true, true, elektro));
            }
            ZahtevRute izabran = pokusaji[pokusaji.Count - 1];
            List<RezultatRute> rute = new List<RezultatRute>();
            foreach (ZahtevRute pokusaj in pokusaji)
            {
                rute = Izracunaj(k, pokusaj);
                if (rute.Count > 0) { izabran = pokusaj; break; }
            }
            var rezultat = new RuteSaZastojima { Rute = rute, Graf = GrafZa(k, izabran), BezElektro = rute.Count > 0 && z.ObaveznaElektro && z.Elektricna && !izabran.Elektricna };
            if (rute.Count > 0 && !izabran.BezTelegrama)
            {
                // Najkraća ruta bez obzira na telegrame (uz istu vuču), da se vidi šta je zaobiđeno.
                RezultatRute najkraca = Izracunaj(k, new ZahtevRute { OdID = z.OdID, DoID = z.DoID, DuzinaM = z.DuzinaM, Datum = z.Datum, Polazak = z.Polazak, SaKiM = izabran.SaKiM, RucnoZatvorene = z.RucnoZatvorene, BezTelegrama = true, Elektricna = izabran.Elektricna }).FirstOrDefault();
                if (najkraca != null && !najkraca.Deonice.Select(d => d.VezaID).SequenceEqual(rute[0].Deonice.Select(d => d.VezaID)))
                {
                    DodajZastoje(k, najkraca, z);
                    if (najkraca.Zastoji.Any(x => x.Tekst.StartsWith("Zatvor"))) rezultat.KrozZatvor = najkraca;
                }
            }
            foreach (RezultatRute r in rute) DodajZastoje(k, r, z);
            if (rute.Count == 0 || rute[0].Zastoji.Count == 0) return rezultat;
            // Ako postoji i put bez zastoja, nudi se kao poslednji.
            var strog = new ZahtevRute { OdID = z.OdID, DoID = z.DoID, Elektricna = z.Elektricna, OsovinskoT = z.OsovinskoT, DuzinaM = z.DuzinaM, Datum = z.Datum, Polazak = z.Polazak, SaKiM = z.SaKiM, RucnoZatvorene = z.RucnoZatvorene };
            rezultat.BezZastoja = Izracunaj(k, strog).FirstOrDefault();
            if (rezultat.BezZastoja != null && !rute.Any(r => r.Deonice.Select(d => d.VezaID).SequenceEqual(rezultat.BezZastoja.Deonice.Select(d => d.VezaID))))
                rute.Add(rezultat.BezZastoja);
            return rezultat;
        }

        // Zastoji na ruti, spojeni u uzastopne deonice istog razloga.
        internal static void DodajZastoje(PlanKontekst k, RezultatRute r, ZahtevRute z)
        {
            DateTime od = z.ProzorOd, doVreme = z.ProzorDo;
            var poId = k.Veze.ToDictionary(v => v.ID);
            var otvoreni = new Dictionary<string, Tuple<int, int, decimal>>();
            var gotovi = new List<Tuple<int, string, int, decimal>>();
            Action<string> zatvori = razlog =>
            {
                Tuple<int, int, decimal> g = otvoreni[razlog];
                gotovi.Add(Tuple.Create(g.Item1, razlog, g.Item2, g.Item3));
                otvoreni.Remove(razlog);
            };
            for (int i = 0; i < r.Deonice.Count; i++)
            {
                DeonicaRute d = r.Deonice[i];
                List<string> razlozi = RazloziZastoja(k, poId[d.VezaID], d, z, od, doVreme);
                foreach (string staro in otvoreni.Keys.Where(x => !razlozi.Contains(x)).ToList()) zatvori(staro);
                foreach (string razlog in razlozi)
                {
                    Tuple<int, int, decimal> g;
                    otvoreni[razlog] = otvoreni.TryGetValue(razlog, out g) ? Tuple.Create(g.Item1, i, g.Item3 + d.Km) : Tuple.Create(i, i, d.Km);
                }
            }
            foreach (string staro in otvoreni.Keys.ToList()) zatvori(staro);
            r.Zastoji = gotovi.OrderBy(g => g.Item1).Select(g => new ZastojRute
            {
                Km = g.Item4,
                Tekst = g.Item2 + ": " + k.Naziv(r.Deonice[g.Item1].OdID) + " – " + k.Naziv(r.Deonice[g.Item3].DoID) + " (" + g.Item4.ToString("N1") + " km)"
            }).ToList();
            if (z.Elektricna)
            {
                int bezPodatka = r.Deonice.Count(d => !d.Elektrificirana.HasValue);
                if (bezPodatka > 0) r.Upozorenja.Add("Deonica bez podatka o elektrifikaciji: " + bezPodatka + ".");
            }
            if (z.OsovinskoT.HasValue)
            {
                int bezKlase = r.Deonice.Count(d => !poId[d.VezaID].OsovinskoT.HasValue);
                if (bezKlase > 0) r.Upozorenja.Add("Deonica bez kategorije pruge: " + bezKlase + ".");
            }
        }

        private static List<string> RazloziZastoja(PlanKontekst k, VezaMreze v, DeonicaRute d, ZahtevRute z, DateTime od, DateTime doVreme)
        {
            var razlozi = new List<string>();
            if (v.KiM && !z.SaKiM) razlozi.Add("Pruga na KiM");
            if (z.Elektricna && v.Elektrificirana == false) razlozi.Add("Bez kontaktne mreže");
            if (z.OsovinskoT.HasValue && v.OsovinskoT.HasValue && v.OsovinskoT.Value < z.OsovinskoT.Value)
                razlozi.Add("Osovinsko " + v.OsovinskoT.Value.ToString("0.#") + " t, a voz traži " + z.OsovinskoT.Value.ToString("0.#") + " t");
            foreach (TelegramOP t in k.Telegrami.Where(x => Ruta.JeZatvor(x) && Ruta.JeAktivan(x, od, doVreme) && Ruta.Pokriva(k, x, d)))
                razlozi.Add("Zatvor, telegram " + t.Broj);
            return razlozi;
        }

        // Ruta sastavljena od delova, kad lanac trasa ne ide najkraćim putem.
        internal static RezultatRute IzDeonica(PlanKontekst k, List<DeonicaRute> deonice, ZahtevRute z)
        {
            for (int i = 0; i < deonice.Count; i++) deonice[i].RB = i + 1;
            return Sazmi(k, deonice, k.Veze.ToDictionary(v => v.ID), z, z.ProzorOd, z.ProzorDo);
        }

        // Prazna lista znači da uz izabrane uslove puta nema.
        internal static List<RezultatRute> Izracunaj(PlanKontekst k, ZahtevRute z)
        {
            DateTime od = z.ProzorOd, doVreme = z.ProzorDo;
            Graf graf = GrafZa(k, z);
            var poId = k.Veze.ToDictionary(v => v.ID);
            return graf.Alternative(z.OdID, z.DoID, Math.Max(1, z.BrojAlternativa))
                .Select(deonice => Sazmi(k, deonice, poId, z, od, doVreme))
                .ToList();
        }

        private static RezultatRute Sazmi(PlanKontekst k, List<DeonicaRute> deonice, Dictionary<int, VezaMreze> poId, ZahtevRute z, DateTime od, DateTime doVreme)
        {
            var r = new RezultatRute { Deonice = deonice };
            r.Mesta.Add(z.OdID);
            foreach (DeonicaRute d in deonice)
            {
                VezaMreze v = poId[d.VezaID];
                r.Mesta.Add(d.DoID);
                r.Km += d.Km;
                switch (v.KategorijaPruge)
                {
                    case "MAGISTRALNA": r.KmMagistralno += d.Km; break;
                    case "REGIONALNA": r.KmRegionalno += d.Km; break;
                    case "LOKALNA": r.KmLokalno += d.Km; break;
                    case "MANIPULATIVNA": r.KmManipulativno += d.Km; break;
                }
                if (v.Elektrificirana == true) r.KmElektro += d.Km;
                else if (v.Elektrificirana == false) r.KmDizel += d.Km;
                else r.KmNepoznato += d.Km;
                if (v.Vmax.HasValue && (!r.NajmanjiVmax.HasValue || v.Vmax.Value < r.NajmanjiVmax.Value)) r.NajmanjiVmax = v.Vmax;
                decimal? uspon = Veci(v.MerodavniUspon, v.MerodavniPad);
                if (uspon.HasValue && (!r.NajveciUspon.HasValue || uspon.Value > r.NajveciUspon.Value)) r.NajveciUspon = uspon;
            }
            DodajUpozorenja(k, r, poId, z, od, doVreme);
            return r;
        }

        private static decimal? Veci(decimal? a, decimal? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return Math.Max(a.Value, b.Value);
        }

        private static void DodajUpozorenja(PlanKontekst k, RezultatRute r, Dictionary<int, VezaMreze> poId, ZahtevRute z, DateTime od, DateTime doVreme)
        {
            if (z.Elektricna)
            {
                int bezPodatka = r.Deonice.Count(d => !d.Elektrificirana.HasValue);
                if (bezPodatka > 0) r.Upozorenja.Add("Deonica bez podatka o elektrifikaciji: " + bezPodatka + ".");
            }
            if (z.OsovinskoT.HasValue)
            {
                int bezKlase = r.Deonice.Count(d => !poId[d.VezaID].OsovinskoT.HasValue);
                if (bezKlase > 0) r.Upozorenja.Add("Deonica bez kategorije pruge: " + bezKlase + ".");
            }
            if (z.DuzinaM.HasValue)
                foreach (DeonicaRute d in r.Deonice)
                {
                    if (!d.PrugaID.HasValue) continue;
                    MestoPruge m = k.MestoNaPruzi(d.PrugaID.Value, d.DoID);
                    if (m == null) continue;
                    int? granica = k.Smer(d) == "BA" ? m.MaxDuzinaBA : m.MaxDuzinaAB;
                    if (granica.HasValue && granica.Value > 0 && z.DuzinaM.Value > granica.Value)
                        r.Upozorenja.Add("Dužina voza " + z.DuzinaM.Value + " m je veća od dozvoljenih " + granica.Value + " m u stanici " + k.Naziv(d.DoID) + ".");
                }
            foreach (TelegramOP t in k.Telegrami.Where(x => !Ruta.JeZatvor(x) && Ruta.JeAktivan(x, od, doVreme)))
            {
                DeonicaRute pogodjena = r.Deonice.FirstOrDefault(d => Ruta.Pokriva(k, t, d));
                if (pogodjena == null) continue;
                // Levi ili desni kolosek na dvokolosečnoj pruzi nije zatvor, nego smanjen kapacitet.
                bool kapacitet = poId[pogodjena.VezaID].Dvokolosecna == true && JedanKolosek(t.Kolosek);
                r.Upozorenja.Add("Telegram " + t.Broj + (kapacitet ? " (smanjen kapacitet)" : "") + ": " + t.Napomena);
            }
        }

        private static bool JedanKolosek(string kolosek)
        {
            string s = (kolosek ?? "").Trim().ToLowerInvariant();
            return s == "levi" || s == "desni";
        }
    }
}
