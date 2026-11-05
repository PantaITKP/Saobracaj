using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class DodelaOptimizatora
    {
        internal int PosaoID;
        internal List<int> Lokomotive = new List<int>();
        internal int? Masinovodja;
        internal int PraznaVoznjaMin;
    }

    internal sealed class OdgovorOptimizatora
    {
        internal int BrojVerzije;
        internal string Status;
        internal double TrajanjeS;
        internal List<DodelaOptimizatora> Dodele = new List<DodelaOptimizatora>();
        internal List<int> Nedodeljeni = new List<int>();
    }

    // Adresa servisa je u appSettings (PlaniranjeOptimizatorUrl). Bez nje „Predloži plan“ radi pohlepnim algoritmom.
    internal static class OptimizatorKlijent
    {
        private static readonly TimeSpan Rok = TimeSpan.FromSeconds(45);

        internal static string Adresa => (ConfigurationManager.AppSettings["PlaniranjeOptimizatorUrl"] ?? "").Trim().TrimEnd('/');
        internal static bool Podesen => Adresa.Length > 0;

        internal static OdgovorOptimizatora Predlozi(bool operativni, DateTime od)
        {
            using (var http = new HttpClient { Timeout = Rok })
            using (var sadrzaj = new StringContent("{\"od\":\"" + od.ToString("yyyy-MM-dd") + "\"}", Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage odgovor = http.PostAsync(Adresa + (operativni ? "/plan/operativni" : "/plan/nedeljni"), sadrzaj).GetAwaiter().GetResult();
                odgovor.EnsureSuccessStatusCode();
                JObject j = JObject.Parse(odgovor.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                return new OdgovorOptimizatora
                {
                    BrojVerzije = (int)j["broj_verzije"],
                    Status = (string)j["status"],
                    TrajanjeS = (double)j["trajanje_s"],
                    Nedodeljeni = j["nedodeljeni"].Select(x => (int)x).ToList(),
                    Dodele = j["dodele"].Select(d => new DodelaOptimizatora
                    {
                        PosaoID = (int)d["posao"],
                        Lokomotive = d["lokomotive"].Select(x => (int)x).ToList(),
                        Masinovodja = (int?)d["masinovodja"],
                        PraznaVoznjaMin = (int)d["prazna_voznja_min"]
                    }).ToList()
                };
            }
        }
    }
}
