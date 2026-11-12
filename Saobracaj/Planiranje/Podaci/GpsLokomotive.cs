using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class PozicijaLokomotive
    {
        internal int IdVozila;
        internal string Oznaka;
        internal string Model;
        internal DateTime? Vreme;
        internal double? Lat;
        internal double? Lon;
        internal double? Brzina;
        internal double? Smer;
        internal bool ImaPoziciju => Lat.HasValue && Lon.HasValue;
    }

    // Technodis (Skywatch) GPS: token, stablo vozila, pa poslednji zapis svakog vozila.
    // Zapis stiže na ~10 s i dok lokomotiva stoji, pa je kratak prozor dovoljan; ako je prazan, gleda se 24 h unazad.
    internal static class GpsLokomotive
    {
        private const string AdresaTokena = "http://85.25.177.168/egoidentityserver/connect/token";
        private const string AdresaVozila = "http://85.25.177.168/gpstrackjourney/api/Vehicles/GetVehiclesTree";
        private const string AdresaZapisa = "http://85.25.177.168/gpstrackjourney/api/journey/records";
        private const string NazivGrupe = "Lokomotive";
        // Prozori od kraćeg ka dužem: dan zapisa jedne lokomotive je ~1 MB, pa se dugi prozor traži samo kad kraći nema ništa.
        private static readonly TimeSpan[] Prozori = { TimeSpan.FromMinutes(15), TimeSpan.FromHours(3), TimeSpan.FromHours(24) };
        internal static readonly TimeSpan Svez = TimeSpan.FromMinutes(15);
        // Lokomotiva bez signala duže od dana (u radionici, van mreže): traži se dan po dan unazad, do poslednje poznate pozicije.
        // Prazan dan je mali odgovor, pa je ovo brzo i za lokomotivu koja se ne javlja nedeljama.
        internal const int NajviseDanaUnazad = 90;

        private static readonly HttpClient http = NoviKlijent();
        private static readonly object brava = new object();
        private static string token;
        private static DateTime tokenVazi = DateTime.MinValue;
        // Poslednji poznati zapis po vozilu i kada je dugi prozor poslednji put tražen, da osvežavanje svakog minuta bude brzo.
        private static readonly Dictionary<int, JObject> poslednji = new Dictionary<int, JObject>();
        private static readonly Dictionary<int, DateTime> dugoTrazeno = new Dictionary<int, DateTime>();
        private static readonly TimeSpan PonoviDugo = TimeSpan.FromMinutes(30);

        // .NET Framework daje 2 veze po serveru, a upit ide posebno za svaku lokomotivu.
        private static HttpClient NoviKlijent()
        {
            System.Net.ServicePointManager.FindServicePoint(new Uri(AdresaZapisa)).ConnectionLimit = 8;
            return new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        }

        internal static List<PozicijaLokomotive> Ucitaj()
        {
            return UcitajAsync().GetAwaiter().GetResult();
        }

        // Ostala vozila (nisu u stablu grupe Lokomotive, ali ih journey/records vraća po idVehicles).
        // API ne daje naziv vozila, pa su oznaka i model iz Sky Watch spiska vozila (spisak_vozila.csv, grupa VOZILA i BG VAGON):
        // idVehicles → { oznaka (uz registraciju kad se razlikuje), model }.
        private static readonly Dictionary<int, string[]> OstalaVozila = new Dictionary<int, string[]>
        {
            { 2964, new[] { "BG VAGON", "" } },
            { 2829, new[] { "BG*1756-NL", "FIAT" } },
            { 2864, new[] { "BG*1927-TB", "500L" } },
            { 2865, new[] { "BG*1927-TC", "500L" } },
            { 2928, new[] { "BG*2452-GC", "FORD" } },
            { 2936, new[] { "BG*2725-VP", "FIAT" } },
            { 2817, new[] { "BG*372-BM", "CRAFTER" } },
            { 2888, new[] { "BG-2153-TB", "" } },
            { 2887, new[] { "BG-2153-TC", "" } },
            { 2901, new[] { "BG-2294-EP", "Panda, FIAT" } },
            { 2902, new[] { "BG-2294-KB", "Panda" } },
            { 2919, new[] { "BG-2510-UK", "FIAT" } },
            { 2921, new[] { "BG-2510-UL", "FIAT" } },
            { 2918, new[] { "BG-2510-UM", "FIAT" } },
            { 2922, new[] { "BG-2510-UN", "FIAT" } },
            { 2920, new[] { "BG-2510-UO", "FIAT" } },
            { 2923, new[] { "BG-2510UJ", "panda" } },
            { 2924, new[] { "BG-2511NZ", "panda" } },
            { 2773, new[] { "BG-2741-XM", "" } },
            { 2930, new[] { "BG-2780-ZR", "DACIA" } },
            { 2950, new[] { "BG-2908ZR", "500 L" } },
            { 2948, new[] { "BG-2980ZX", "" } },
            { 2909, new[] { "BG-HM (BG-2339-HM)", "" } },
            { 2908, new[] { "BG-HN (BG-2339-HN)", "" } },
            { 2949, new[] { "BG1758PL", "Dacia Sandero" } },
            { 2931, new[] { "BG2515VO (BG*2515-VO)", "DACIA" } },
            { 2910, new[] { "Cisterna (BG-2289-KA)", "" } },
            { 2849, new[] { "Honda (BG-2807-RI)", "" } },
            { 2830, new[] { "M/Bgd- JD (BG-2225-JD)", "VW CADDY" } },
            { 2845, new[] { "M/Bgd-RU (BG-1620-RU)", "FIAT" } },
            { 2828, new[] { "M/N.Sad-VA (BG*1331-VA)", "FIAT" } },
            { 2836, new[] { "M/Panč-SK (BG*1506-SK)", "500L" } },
            { 2844, new[] { "M/Ruma-PN (BG*1579-PN)", "" } },
            { 2846, new[] { "M/Ruma-RV (BG-1620-RV)", "" } },
            { 2843, new[] { "M/Zrenj-VD (BG-1818-VD)", "" } },
            { 2837, new[] { "V/Niš-MF (BG-1513-MF)", "500L" } },
        };

        internal static List<PozicijaLokomotive> UcitajVozila()
        {
            return UcitajVozilaAsync().GetAwaiter().GetResult();
        }

        private static async Task<List<PozicijaLokomotive>> UcitajVozilaAsync()
        {
            string t = await Token().ConfigureAwait(false);
            List<PozicijaLokomotive> vozila = OstalaVozila.Select(v => new PozicijaLokomotive
            {
                IdVozila = v.Key,
                Oznaka = v.Value[0],
                Model = v.Value[1].Length > 0 ? v.Value[1] : null
            }).ToList();
            await Task.WhenAll(vozila.Select(v => PoslednjiZapis(t, v))).ConfigureAwait(false);
            return vozila.OrderBy(v => v.Oznaka, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static async Task<List<PozicijaLokomotive>> UcitajAsync()
        {
            string t = await Token().ConfigureAwait(false);
            List<PozicijaLokomotive> vozila = await Vozila(t).ConfigureAwait(false);
            await Task.WhenAll(vozila.Select(v => PoslednjiZapis(t, v))).ConfigureAwait(false);
            return vozila.OrderBy(v => v.Oznaka, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static async Task<string> Token()
        {
            lock (brava)
                if (token != null && DateTime.UtcNow < tokenVazi) return token;
            var forma = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "MyApi"),
                new KeyValuePair<string, string>("client_id", "lokomotive"),
                new KeyValuePair<string, string>("client_secret", "loccoo#moto147")
            });
            using (HttpResponseMessage odg = await http.PostAsync(AdresaTokena, forma).ConfigureAwait(false))
            {
                if (!odg.IsSuccessStatusCode) throw new HttpRequestException("GPS servis nije izdao token (" + (int)odg.StatusCode + ").");
                JObject j = JObject.Parse(await odg.Content.ReadAsStringAsync().ConfigureAwait(false));
                string novi = (string)j["access_token"];
                int traje = (int?)j["expires_in"] ?? 3600;
                lock (brava)
                {
                    token = novi;
                    tokenVazi = DateTime.UtcNow.AddSeconds(Math.Max(60, traje - 60));
                }
                return novi;
            }
        }

        private static async Task<List<PozicijaLokomotive>> Vozila(string t)
        {
            using (var zahtev = new HttpRequestMessage(HttpMethod.Get, AdresaVozila))
            {
                zahtev.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
                using (HttpResponseMessage odg = await http.SendAsync(zahtev).ConfigureAwait(false))
                {
                    if (!odg.IsSuccessStatusCode) throw new HttpRequestException("GPS servis nije vratio spisak vozila (" + (int)odg.StatusCode + ").");
                    JToken stablo = JToken.Parse(await odg.Content.ReadAsStringAsync().ConfigureAwait(false));
                    // Ako postoji grupa Lokomotive, uzimaju se samo njena vozila; inače sva iz stabla.
                    List<JToken> grupe = Cvorovi(stablo).Where(c => c["leaf"]?.Value<bool>() != true && string.Equals((string)c["name"], NazivGrupe, StringComparison.OrdinalIgnoreCase)).ToList();
                    IEnumerable<JToken> listovi = (grupe.Count > 0 ? grupe.SelectMany(Cvorovi) : Cvorovi(stablo))
                        .Where(c => c["leaf"]?.Value<bool>() == true && c["idVehicles"]?.Type == JTokenType.Integer && c["active"]?.Value<bool>() != false);
                    return listovi
                        .GroupBy(c => (int)c["idVehicles"])
                        .Select(g => new PozicijaLokomotive
                        {
                            IdVozila = g.Key,
                            Oznaka = Prazno((string)g.First()["tag"]) ?? Prazno((string)g.First()["name"]) ?? g.Key.ToString(),
                            Model = Prazno((string)g.First()["model"])
                        })
                        .ToList();
                }
            }
        }

        private static IEnumerable<JToken> Cvorovi(JToken koren)
        {
            var stek = new Stack<JToken>();
            if (koren is JArray niz) foreach (JToken c in niz) stek.Push(c);
            else stek.Push(koren);
            while (stek.Count > 0)
            {
                JToken c = stek.Pop();
                yield return c;
                if (c["children"] is JArray deca) foreach (JToken d in deca) stek.Push(d);
            }
        }

        private static async Task PoslednjiZapis(string t, PozicijaLokomotive v)
        {
            DateTime sada = DateTime.Now;
            JObject zapis = null, ranije;
            bool trazitiDugo;
            lock (brava)
            {
                poslednji.TryGetValue(v.IdVozila, out ranije);
                trazitiDugo = !dugoTrazeno.TryGetValue(v.IdVozila, out DateTime kada) || sada - kada > PonoviDugo;
            }
            try
            {
                foreach (TimeSpan prozor in Prozori)
                {
                    if (prozor != Prozori[0])
                    {
                        if (!trazitiDugo) break;
                        lock (brava) dugoTrazeno[v.IdVozila] = sada;
                    }
                    if ((zapis = await Zapis(t, v.IdVozila, sada - prozor, sada).ConfigureAwait(false)) != null) break;
                }
                // Starije od dana: dan po dan unazad, ali ne dalje od već poznatog zapisa.
                DateTime? poznato = ranije != null ? (DateTime?)ranije["recordtime"] : null;
                for (int dan = 1; zapis == null && trazitiDugo && dan < NajviseDanaUnazad; dan++)
                {
                    DateTime doVremena = sada.AddDays(-dan);
                    if (poznato.HasValue && doVremena <= poznato.Value) break;
                    zapis = await Zapis(t, v.IdVozila, doVremena.AddDays(-1), doVremena).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Jedna lokomotiva bez odgovora ne obara ostale; ostaje na poslednjoj poznatoj poziciji.
            }
            if (zapis == null) zapis = ranije;
            else lock (brava) poslednji[v.IdVozila] = zapis;
            if (zapis == null) return;
            v.Vreme = (DateTime?)zapis["recordtime"];
            v.Lat = (double?)zapis["latitude"];
            v.Lon = (double?)zapis["longitude"];
            v.Brzina = Broj(zapis["speed"]);
            v.Smer = Broj(zapis["direction"]);
        }

        // Redosled zapisa u odgovoru nije po vremenu, pa se traži najnoviji ispravan zapis sa koordinatama.
        private static async Task<JObject> Zapis(string t, int idVozila, DateTime od, DateTime doVremena)
        {
            using (var zahtev = new HttpRequestMessage(HttpMethod.Post, AdresaZapisa))
            {
                zahtev.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
                zahtev.Content = new MultipartFormDataContent
                {
                    { new StringContent(idVozila.ToString(CultureInfo.InvariantCulture)), "vehicles" },
                    { new StringContent(od.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)), "from" },
                    { new StringContent(doVremena.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)), "to" }
                };
                using (HttpResponseMessage odg = await http.SendAsync(zahtev).ConfigureAwait(false))
                {
                    if (!odg.IsSuccessStatusCode) return null;
                    if (!(JToken.Parse(await odg.Content.ReadAsStringAsync().ConfigureAwait(false)) is JArray zapisi)) return null;
                    List<JObject> saKoordinatama = zapisi.OfType<JObject>()
                        .Where(z => z["recordtime"]?.Type == JTokenType.Date && Broj(z["latitude"]).GetValueOrDefault() != 0 && Broj(z["longitude"]).GetValueOrDefault() != 0)
                        .ToList();
                    List<JObject> ispravni = saKoordinatama.Where(z => z["valid"]?.Type != JTokenType.Boolean || (bool)z["valid"]).ToList();
                    return (ispravni.Count > 0 ? ispravni : saKoordinatama).OrderByDescending(z => (DateTime)z["recordtime"]).FirstOrDefault();
                }
            }
        }

        private static double? Broj(JToken j)
        {
            if (j == null || j.Type == JTokenType.Null) return null;
            if (j.Type == JTokenType.Float || j.Type == JTokenType.Integer) return (double)j;
            return double.TryParse(Convert.ToString(j, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null;
        }

        private static string Prazno(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
    }
}
