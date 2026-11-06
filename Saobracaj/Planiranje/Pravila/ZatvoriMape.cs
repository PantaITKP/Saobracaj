using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Deonica ili stanica pod zatvorom iz telegrama, za prikaz na mapi.
    internal sealed class ZatvorNaMapi
    {
        internal int? VezaID;
        internal int? StanicaID;
        // Najhitniji nivo među telegramima na ovom mestu.
        internal int Nivo;
        // Zatvor cele pruge (Vrsta ZATVOR); inače zatvor koloseka ili skretnice.
        internal bool CelaPruga;
        internal List<Tuple<int, string>> Opisi = new List<Tuple<int, string>>();
    }

    // Isto značenje boja kao na tabu Telegrami u Planeru i dispMain-u.
    internal static class ZatvoriMape
    {
        internal const int USada = 0, Danas = 1, Uskoro = 2;
        private const int DanaUnapred = 3;
        private const int MaxNapomena = 140;

        // U toku sada, pa važi još danas (dnevni interval ili početak kasnije), pa u naredna 3 dana.
        internal static int? Nivo(TelegramOP t, DateTime sada)
        {
            if (Ruta.JeAktivan(t, sada, sada)) return USada;
            if (Ruta.JeAktivan(t, sada, sada.Date.AddDays(1).AddSeconds(-1))) return Danas;
            if (Ruta.JeAktivan(t, sada.Date.AddDays(1), sada.Date.AddDays(1 + DanaUnapred).AddSeconds(-1))) return Uskoro;
            return null;
        }

        internal static string NazivNivoa(int nivo)
        {
            return nivo == USada ? "U toku" : nivo == Danas ? "Danas" : "Naredna 3 dana";
        }

        internal static List<ZatvorNaMapi> Izracunaj(PlanKontekst k, DateTime sada)
        {
            var poVezi = new Dictionary<int, ZatvorNaMapi>();
            var poStanici = new Dictionary<int, ZatvorNaMapi>();
            foreach (TelegramOP t in k.Telegrami)
            {
                int? nivo = Nivo(t, sada);
                if (!nivo.HasValue) continue;
                bool cela = Ruta.JeZatvor(t);
                string opis = Opis(k, t, nivo.Value);
                bool naDeonici = false;
                foreach (VezaMreze v in k.Veze)
                {
                    if (!Ruta.Pokriva(k, t, new DeonicaRute { VezaID = v.ID, OdID = v.A, DoID = v.B, PrugaID = v.PrugaID })) continue;
                    naDeonici = true;
                    Dodaj(poVezi, v.ID, nivo.Value, cela, opis).VezaID = v.ID;
                }
                // Zatvor u stanici (skretnica, kolosek) ili između mesta koja nisu na istoj pruzi: tačka na stanicama iz telegrama.
                if (naDeonici) continue;
                foreach (int s in new[] { t.OdID, t.DoID }.Where(x => x.HasValue).Select(x => x.Value).Distinct())
                    Dodaj(poStanici, s, nivo.Value, cela, opis).StanicaID = s;
            }
            return poVezi.Values.Concat(poStanici.Values).ToList();
        }

        private static ZatvorNaMapi Dodaj(Dictionary<int, ZatvorNaMapi> mesta, int kljuc, int nivo, bool cela, string opis)
        {
            ZatvorNaMapi z;
            if (!mesta.TryGetValue(kljuc, out z))
                mesta[kljuc] = z = new ZatvorNaMapi { Nivo = nivo, CelaPruga = cela };
            else if (nivo < z.Nivo) { z.Nivo = nivo; z.CelaPruga = cela; }
            else if (nivo == z.Nivo) z.CelaPruga |= cela;
            if (!z.Opisi.Any(o => o.Item2 == opis)) z.Opisi.Add(Tuple.Create(nivo, opis));
            return z;
        }

        private static string Opis(PlanKontekst k, TelegramOP t, int nivo)
        {
            string mesto = t.OdID.HasValue && t.DoID.HasValue && t.OdID != t.DoID ? k.Naziv(t.OdID) + " – " + k.Naziv(t.DoID)
                : t.OdID.HasValue ? k.Naziv(t.OdID) : "";
            string kolosek = t.Vrsta == "ZATVOR" ? "zatvor pruge" : Ruta.JeZatvor(t) ? "zatvor svih koloseka" : "zatvor: " + (string.IsNullOrWhiteSpace(t.Kolosek) ? "kolosek" : t.Kolosek.Trim());
            string period = t.VaziOd.ToString("dd.MM. HH:mm") + " – " + (t.VaziDo.HasValue ? t.VaziDo.Value.ToString("dd.MM. HH:mm") : "do opoziva");
            TimeSpan x, y;
            if (Ruta.DnevniProzor(t, out x, out y) && (!t.VaziDo.HasValue || t.VaziDo.Value.Date > t.VaziOd.Date.AddDays(y < x ? 1 : 0)))
                period += ", svaki dan " + x.ToString(@"hh\:mm") + "–" + y.ToString(@"hh\:mm");
            // Napomena je često ceo tekst telegrama; za mapu je dovoljan početak.
            string napomena = (t.Napomena ?? "").Trim();
            if (napomena.Length > MaxNapomena) napomena = napomena.Substring(0, MaxNapomena).TrimEnd() + "…";
            return NazivNivoa(nivo) + ": tel. " + (t.Broj ?? "?") + ", " + mesto + ", " + kolosek + ", " + period
                + (napomena.Length == 0 ? "" : " (" + napomena + ")");
        }
    }
}
