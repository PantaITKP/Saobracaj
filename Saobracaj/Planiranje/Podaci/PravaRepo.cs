using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class PravaForme
    {
        internal bool Pristup;
        internal bool Upis;
        internal bool Izmena;
        internal bool Brisanje;
    }

    // Isti model prava kao u postojećim formama; dbo tabele se ovde samo čitaju.
    internal static class PravaRepo
    {
        internal static PravaForme Ucitaj(string code)
        {
            var prava = new PravaForme();
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null,
                    "SELECT gf.Upis, gf.Izmena, gf.Brisanje " +
                    "FROM dbo.GrupeForme gf " +
                    "JOIN dbo.Forme f ON f.IdForme = gf.IdForme " +
                    "JOIN dbo.KorisnikGrupa kg ON kg.IdGrupe = gf.IdGrupe " +
                    "WHERE RTRIM(f.Code) = @code AND RTRIM(kg.Korisnik) = @korisnik",
                    "@code", code, "@korisnik", PlaniranjeDb.Korisnik);
                foreach (DataRow r in t.Rows)
                {
                    prava.Pristup = true;
                    prava.Upis |= Convert.ToBoolean(r["Upis"]);
                    prava.Izmena |= Convert.ToBoolean(r["Izmena"]);
                    prava.Brisanje |= Convert.ToBoolean(r["Brisanje"]);
                }
            }
            return prava;
        }
    }
}
