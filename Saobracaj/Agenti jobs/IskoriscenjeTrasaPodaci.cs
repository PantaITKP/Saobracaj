using System;
using System.Collections.Generic;
using System.Data;

namespace Saobracaj.Agenti_jobs
{
    internal static class IskoriscenjeTrasaPodaci
    {
        internal static DataTable Ucitaj(DateTime od, DateTime doIskljucivo)
        {
            using (var connection = Db.Open())
                return Db.Table(connection, null, @"
SELECT ID,BrojTrase,adHoc,VremePokretanja,Cena
FROM dbo.OperativniPlanPokretanja
WHERE VremePokretanja>=@od AND VremePokretanja<@do
ORDER BY VremePokretanja,BrojTrase,ID",
                    "@od", od.Date, "@do", doIskljucivo);
        }

        internal static DataTable UcitajInfrastrukturu(DateTime od, DateTime doIskljucivo, DataTable pokretanja)
        {
            using (var c = Db.Open())
            {
                var features = Db.Table(c, null, @"SELECT
CASE WHEN OBJECT_ID('dbo.OperativniPlanRNRealizacija','U') IS NULL THEN 0 ELSE 1 END AS ImaRN,
CASE WHEN COL_LENGTH('dbo.RadniNalogLokNaTrasi','RBTrase') IS NULL THEN 0 ELSE 1 END AS ImaRB").Rows[0];
                bool imaRN = Convert.ToInt32(features["ImaRN"]) == 1;
                bool imaRB = Convert.ToInt32(features["ImaRB"]) == 1;
                var vozovi = Db.Table(c, null, @"SELECT ID,Train,OriginalTrain,[From],[To],LocomotiveNumber,
StartedAtLocal,CompletedAtLocal,PokretanjeID FROM dbo.OperativniPlanVozovi
WHERE Started=1 AND StartedAtLocal>=@od AND StartedAtLocal<@do ORDER BY StartedAtLocal,ID",
                    "@od", od, "@do", doIskljucivo);
                var realizacije = new DataTable();
                if (imaRN)
                    realizacije = Db.Table(c, null, @"SELECT p.*,RTRIM(s1.Opis) AS StanicaOdNaziv,RTRIM(s2.Opis) AS StanicaDoNaziv,
" + (imaRB ? LokomotiveSql("p.IDRadnogNaloga", "p.IDTrase", "AND l.RBTrase=p.RB") : "CAST(N'' AS nvarchar(max))") + @" AS Lokomotive
FROM dbo.OperativniPlanRNRealizacija p
LEFT JOIN dbo.stanice s1 ON s1.ID=p.StanicaOd LEFT JOIN dbo.stanice s2 ON s2.ID=p.StanicaDo
WHERE p.Polazak>=@od AND p.Polazak<@do ORDER BY p.Polazak,p.PrevozID,p.ID",
                        "@od", od, "@do", doIskljucivo);
                // Stari RN se prikazuju odvojeno, bez pogadjanja identiteta voznje po broju/danu.
                var istorija = Db.Table(c, null, @"SELECT r.IDRadnogNaloga,r.RB,r.DatumPolaskaReal,r.DatumDolaskaReal,
RTRIM(t.Voz) AS BrojVoza,RTRIM(s1.Opis) AS StanicaOdNaziv,RTRIM(s2.Opis) AS StanicaDoNaziv,
" + LokomotiveSql("r.IDRadnogNaloga", "r.IDTrase", imaRB ? "AND (l.RBTrase=r.RB OR l.RBTrase IS NULL)" : "") + @" AS Lokomotive
FROM dbo.RadniNalogTrase r LEFT JOIN dbo.Trase t ON t.ID=r.IDTrase
LEFT JOIN dbo.stanice s1 ON s1.ID=r.StanicaOd LEFT JOIN dbo.stanice s2 ON s2.ID=r.StanicaDo
WHERE ((r.DatumPolaskaReal>=@od AND r.DatumPolaskaReal<@do)
OR ((r.DatumPolaskaReal IS NULL OR r.DatumPolaskaReal<'19000102')
AND r.DatumDolaskaReal>=@od AND r.DatumDolaskaReal<@do))
" + (imaRN ? @"AND NOT EXISTS(SELECT 1 FROM dbo.OperativniPlanRNRealizacija p
WHERE p.IDRadnogNaloga=r.IDRadnogNaloga AND p.RB=r.RB)" : "") + @"
ORDER BY r.DatumPolaskaReal,r.IDRadnogNaloga,r.RB", "@od", od, "@do", doIskljucivo);
                return IskoriscenjeInfrastruktura.Slozi(pokretanja, vozovi, realizacije, istorija);
            }
        }
        private static string LokomotiveSql(string nalog, string trasa, string uslovRB)
        {
            // Argumenti su iskljucivo konstante iz upita iznad, nikada unos korisnika.
            return @"STUFF((SELECT DISTINCT N'; ' + RTRIM(l.SMSifra)
FROM dbo.RadniNalogLokNaTrasi l WHERE l.IDRadnogNaloga=" + nalog + " AND l.IDTrase=" + trasa + " " + uslovRB + @"
FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,2,N'')";
        }

        internal static void SacuvajCenu(List<CenaPokretanja> pokretanja, decimal? cena)
        {
            using (var connection = Db.Open())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var item in pokretanja)
                    {
                        using (var command = Db.Command(connection, transaction, @"
UPDATE dbo.OperativniPlanPokretanja SET Cena=@cena
WHERE ID=@id AND adHoc=1 AND
((Cena=@stara) OR (Cena IS NULL AND @stara IS NULL))",
                            "@cena", cena, "@id", item.ID, "@stara", item.StaraCena))
                        {
                            if (command.ExecuteNonQuery() != 1)
                                throw new DBConcurrencyException(
                                    "Neko pokretanje je obrisano ili mu je cena promenjena na drugom računaru. Nijedna cena iz grupe nije sačuvana. Ponovo učitajte period.");
                        }
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
