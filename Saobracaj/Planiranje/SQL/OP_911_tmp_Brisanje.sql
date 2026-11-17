/* =====================================================================
   OP_911_tmp_Brisanje.sql
   Briše privremene tabele agenta CimProvera (OP_910_tmp_Kreiranje.sql, sa sinonimima iz OP_914_tmp_Sinonimi.sql,
   izmene iz OP_916_tmp_Izmene.sql i šifarnik NHM iz OP_917_tmp_NHM_Podaci.sql) i forme iz
   OP_912_tmp_Forme.sql.

   - Briše samo tabele sa spiska ispod (planiranje.tmp*), ne sve tmp% tabele u šemi.
   - Iz dbo.GrupeForme i dbo.Forme briše samo redove formi frmTmpJobs i frmTmpTeretnica.
   - PAŽNJA: briše i sve podatke koje je agent upisao u tmp tabele.
   - Može da se pokrene više puta: ono što ne postoji se preskače.
   - Pokreće se ručno, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_911_tmp_Brisanje.sql
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;

-- 1. forme (OP_912_tmp_Forme.sql)
IF OBJECT_ID(N'dbo.GrupeForme', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Forme', N'U') IS NOT NULL
BEGIN
    DELETE g FROM dbo.GrupeForme g
    JOIN dbo.Forme f ON f.IdForme = g.IdForme
    WHERE RTRIM(f.Code) IN (N'frmTmpJobs', N'frmTmpTeretnica');
    PRINT N'Obrisanih prava: ' + CAST(@@ROWCOUNT AS nvarchar(10));

    DELETE FROM dbo.Forme WHERE RTRIM(Code) IN (N'frmTmpJobs', N'frmTmpTeretnica');
    PRINT N'Obrisanih formi: ' + CAST(@@ROWCOUNT AS nvarchar(10));
END

-- 2. tabele: redosled = prvo tabele koje pokazuju na druge
IF OBJECT_ID(N'planiranje.tmpJobsDogadjaj',    N'U') IS NOT NULL DROP TABLE planiranje.tmpJobsDogadjaj;   -- v3.0 (OP_918)
IF OBJECT_ID(N'planiranje.tmpJobsIstorija',    N'U') IS NOT NULL DROP TABLE planiranje.tmpJobsIstorija;
IF OBJECT_ID(N'planiranje.tmpJobsPolje',       N'U') IS NOT NULL DROP TABLE planiranje.tmpJobsPolje;
IF OBJECT_ID(N'planiranje.tmpTeretnicaStavke', N'U') IS NOT NULL DROP TABLE planiranje.tmpTeretnicaStavke;
IF OBJECT_ID(N'planiranje.tmpTeretnica',       N'U') IS NOT NULL DROP TABLE planiranje.tmpTeretnica;
IF OBJECT_ID(N'planiranje.tmpJobs',            N'U') IS NOT NULL DROP TABLE planiranje.tmpJobs;
IF OBJECT_ID(N'planiranje.tmpDokument',        N'U') IS NOT NULL DROP TABLE planiranje.tmpDokument;
IF OBJECT_ID(N'planiranje.tmpSinonimi',        N'U') IS NOT NULL DROP TABLE planiranje.tmpSinonimi;
IF OBJECT_ID(N'planiranje.tmpNHM',             N'U') IS NOT NULL DROP TABLE planiranje.tmpNHM;   -- OP_916 / OP_917

COMMIT TRANSACTION;
PRINT N'OP_911_tmp_Brisanje: gotovo.';
GO
