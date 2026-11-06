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
