using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>
    /// v2.9.2 (1): platilac deonice KP preko broja ponude – SAMO ČITANJE, parametrizovan upit nad dbo.Ponudba (samo PonPart i PonStPon;
    /// cene i ostale kolone se ne čitaju) i dbo.Partnerji (naziv). Važenje ponude se ne proverava. Konekcija je ista kao za upis
    /// (Konekcija.dpapi / CIMPROVERA_KONEKCIJA) i nigde se ne ispisuje.
    /// </summary>
    internal static class Ponude
    {
        public enum Stanje { Nadjena, NijeNadjena, Nedostupna }

        public sealed class Rezultat
        {
            public Stanje Stanje;
            public int Broj;
            public int? PartnerId;
            public string Naziv = "";
            public string Greska = "";
        }

        /// <summary>Konekcija (iz config-a); null/prazno = baza nije podešena → „ponuda nije proverena – baza nedostupna“.</summary>
        public static string Konekcija;

        /// <summary>Za testove: zamena upita (broj → rezultat).</summary>
        public static Func<int, Rezultat> Zamena;

        // samo nađene i nenađene ponude (u okviru procesa); nedostupna baza se ne pamti – ponavlja se u sledećem prolazu
        private static readonly Dictionary<int, Rezultat> kes = new Dictionary<int, Rezultat>();

        internal const string Upit = "SELECT PonPart AS SifraPartnera, RTRIM(PaNaziv) AS Platilac FROM dbo.Ponudba INNER JOIN dbo.Partnerji ON Ponudba.PonPart = Partnerji.PaSifra WHERE PonStPon = @BrojPonude";

        public static Rezultat Nadji(int broj)
        {
            if (Zamena != null) return Zamena(broj);
            lock (kes) if (kes.TryGetValue(broj, out var r0)) return r0;
            if (string.IsNullOrEmpty(Konekcija)) return new Rezultat { Stanje = Stanje.Nedostupna, Broj = broj, Greska = "konekcija nije podešena" };
            try
            {
                using (var c = UpisUBazu.Otvori(Konekcija))
                using (var cmd = new SqlCommand(Upit, c) { CommandTimeout = 30 })
                {
                    cmd.Parameters.Add("@BrojPonude", System.Data.SqlDbType.Int).Value = broj;
                    var r = new Rezultat { Stanje = Stanje.NijeNadjena, Broj = broj };
                    using (var rd = cmd.ExecuteReader())
                        if (rd.Read())
                        {
                            r.Stanje = Stanje.Nadjena;
                            r.PartnerId = rd.IsDBNull(0) ? (int?)null : Convert.ToInt32(rd.GetValue(0));
                            r.Naziv = rd.IsDBNull(1) ? "" : Convert.ToString(rd.GetValue(1)).Trim();
                            if (r.PartnerId == null) r.Stanje = Stanje.NijeNadjena;
                        }
                    lock (kes) kes[broj] = r;
                    return r;
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException || ex is TimeoutException)
            {
                return new Rezultat { Stanje = Stanje.Nedostupna, Broj = broj, Greska = ex.Message };
            }
        }

        internal static void OcistiKes() { lock (kes) kes.Clear(); }
    }
}
