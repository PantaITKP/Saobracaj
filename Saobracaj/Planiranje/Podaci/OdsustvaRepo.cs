using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class OdsustvaRepo
    {
        // Pogled spaja bolovanja i godišnje iz dbo sa ručnim odsustvima iz OP_Odsustvo.
        internal static DataTable ZaPeriod(DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT * FROM planiranje.OP_vOdsustvo WHERE [Od] <= @do AND [Do] >= @od",
                    "@od", od, "@do", doDatuma);
        }

        internal static DataTable ZaOsobu(int deSifra)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT Vrsta, [Od], [Do], Napomena, Izvor FROM planiranje.OP_vOdsustvo WHERE DeSifra = @d ORDER BY [Od] DESC",
                    "@d", deSifra);
        }

        // Više čekiranih uloga je unija; bez čekirane uloge prikazuju se svi.
        internal static DataTable Pregled(DateTime od, DateTime doDatuma, bool masinovodje, bool pregledaci, bool vozovodje, bool manevristi)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT Zaposleni, [Od], [Do], Razlog, Odobrio, Napomena FROM planiranje.OP_vOdsustvoPregled " +
                    "WHERE [Od] <= @do AND [Do] >= @od AND (@svi = 1 OR (@m = 1 AND Masinovodja = 1) OR (@p = 1 AND PregledacKola = 1) OR (@v = 1 AND Vozovodja = 1) OR (@a = 1 AND Manevrista = 1)) " +
                    "ORDER BY [Od] DESC, Zaposleni",
                    "@od", od, "@do", doDatuma, "@svi", masinovodje || pregledaci || vozovodje || manevristi ? 0 : 1,
                    "@m", masinovodje ? 1 : 0, "@p", pregledaci ? 1 : 0, "@v", vozovodje ? 1 : 0, "@a", manevristi ? 1 : 0);
        }

        internal static DataTable Razlozi()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, Naziv FROM planiranje.OP_RazlogOdsustva WHERE Aktivan = 1 ORDER BY RB");
        }

        // Aktivni radnici, za izbor zaposlenog i onoga ko je odobrio.
        internal static DataTable Zaposleni()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT DeSifra, Naziv FROM planiranje.OP_vOsoblje WHERE Aktivan = 1 ORDER BY Naziv");
        }

        internal static void DodajRucno(int deSifra, int razlogId, int odobrio, DateTime od, DateTime doDatuma, string napomena)
        {
            OsobljeRepo.ObezbediPlan(deSifra);
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_Odsustvo (DeSifra, Vrsta, [Od], [Do], Napomena, Kreirao, RazlogID, OdobrioDeSifra) " +
                "SELECT @d, r.Vrsta, @od, @do, @n, @k, r.ID, @a FROM planiranje.OP_RazlogOdsustva r WHERE r.ID = @r",
                "@d", deSifra, "@od", od, "@do", doDatuma, "@n", (object)napomena, "@k", PlaniranjeDb.Korisnik, "@a", odobrio, "@r", razlogId))
                cmd.ExecuteNonQuery();
        }
    }
}
