using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Saobracaj.Agent.CimProvera.Baza;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>
    /// v3.0 (2.8): lokalna kopija šifarnika za rad offline (C:\CimRadionica\sifarnici\sifarnici.json), sa datumom: dbo.stanice,
    /// dbo.Partnerji (samo kolone koje obrada koristi), planiranje.tmpNHM, planiranje.tmpSinonimi, učestalost stanica (samo brojevi)
    /// i iz dbo.Ponudba samo PonStPon → PonPart (bez cena). Pravi se samo čitanjem; konekcija se ne čuva u kopiji.
    /// </summary>
    internal static class SifarniciKopija
    {
        private const string Ime = "sifarnici.json";

        internal sealed class Kopija
        {
            public DateTime Datum;
            public string Izvor = "";
            public Sifarnici Sifarnici;
            public Dictionary<int, int> Ponude = new Dictionary<int, int>();
        }

        /// <summary>Zajednički folder kopije: C:\CimRadionica\sifarnici (za radni prostor C:\CimRadionica\Radni\&lt;ime&gt;).</summary>
        public static string Folder(RadniProstor rp)
        {
            var radni = Directory.GetParent(rp.Koren);
            var koren = radni?.Parent ?? radni;
            return Path.Combine(koren?.FullName ?? rp.Koren, "sifarnici");
        }

        /// <summary>Kopija iz baze (samo čitanje). Vraća datum kopije.</summary>
        public static DateTime Napravi(string konekcija, string folder, string prefiks = null)
        {
            var k = new Kopija { Datum = DateTime.Now, Izvor = "Perftech_Beograd (samo čitanje)" };
            using (var c = UpisUBazu.Otvori(konekcija))
            {
                k.Sifarnici = Sifarnici.Ucitaj(c, null, null, prefiks);
                try
                {
                    using (var cmd = new SqlCommand("SELECT PonStPon, PonPart FROM dbo.Ponudba WHERE PonStPon IS NOT NULL AND PonPart IS NOT NULL", c) { CommandTimeout = 120 })
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) k.Ponude[Convert.ToInt32(r.GetValue(0))] = Convert.ToInt32(r.GetValue(1));
                }
                catch (SqlException) { /* bez prava na dbo.Ponudba: ponude offline nisu dostupne */ }
            }
            Directory.CreateDirectory(folder);
            var put = Path.Combine(folder, Ime);
            File.WriteAllText(put + ".tmp", JsonConvert.SerializeObject(k), new UTF8Encoding(false));
            if (File.Exists(put)) File.Copy(put, Path.Combine(folder, "sifarnici_" + File.GetLastWriteTime(put).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".json"), true);
            if (File.Exists(put)) File.Delete(put);
            File.Move(put + ".tmp", put);
            kes = null;
            return k.Datum;
        }

        private static Kopija kes;
        private static string kesPut;

        private static Kopija Procitaj(string folder)
        {
            var put = Path.Combine(folder, Ime);
            if (!File.Exists(put)) return null;
            if (kes != null && kesPut == put) return kes;
            kes = JsonConvert.DeserializeObject<Kopija>(File.ReadAllText(put, Encoding.UTF8));
            kesPut = put;
            return kes;
        }

        /// <summary>Datum lokalne kopije (null = nema kopije).</summary>
        public static DateTime? Datum(string folder) => Procitaj(folder)?.Datum;

        public static Sifarnici Ucitaj(string folder) => Procitaj(folder)?.Sifarnici?.Pripremi();

        /// <summary>Broj ponude → platilac iz kopije (offline zamena za Ponude.Nadji).</summary>
        public static Func<int, Ponude.Rezultat> Ponuda(string folder)
        {
            var k = Procitaj(folder);
            if (k == null) return null;
            var partneri = (k.Sifarnici?.Partneri ?? new List<PartnerZapis>()).GroupBy(p => p.PaSifra).ToDictionary(g => g.Key, g => g.First().PaNaziv);
            return broj => k.Ponude.TryGetValue(broj, out var p)
                ? new Ponude.Rezultat { Stanje = Ponude.Stanje.Nadjena, Broj = broj, PartnerId = p, Naziv = partneri.TryGetValue(p, out var n) ? n : "" }
                : new Ponude.Rezultat { Stanje = Ponude.Stanje.NijeNadjena, Broj = broj };
        }
    }
}
