/* =====================================================================
   OP_913_tmp_Prava.sql
   Prava za agenta CimProvera (v2.9.0) koji radi pod Windows nalogom KPSRV-NOVI\svc_cimprovera
   (Integrated Security, bez lozinke; nalog pravi Instaliraj.ps1 agenta).

   - Pisanje (SELECT/INSERT/UPDATE/DELETE) samo u planiranje.tmp* tabelama iz OP_910_tmp_Kreiranje.sql.
   - Čitanje šifarnika: dbo.stanice, dbo.Partnerji, dbo.NHM; v2.9.2: dbo.Ponudba (broj ponude → platilac, samo čitanje).
   - v3.0: planiranje.tmpJobsDogadjaj (OP_918) – pisanje, kada tabela postoji.
   - Samo čitanje: planiranje.OP_Jobs, dbo.Teretnica, dbo.Najava (poređenje, tačka 8) i dbo.TeretnicaStavke (poređenje i
     poslednja dužina istih kola kada je nema na spisku – odluka 14.11.; agent je ne menja).
   - Nema db_datareader / db_datawriter: nalog ne vidi ni ne menja ništa drugo.
   - Može da se pokrene više puta. Pokreće se ručno, posle OP_910, pod sysadmin nalogom, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_913_tmp_Prava.sql
   - Agent na serveru se povezuje LOKALNO: Data Source=.\SQLEXPRESS2008K (Windows prijava lokalnog naloga
     preko IP adrese ne prolazi, server nije u domenu). Poništavanje je na dnu.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'planiranje.tmpJobs', N'U') IS NULL
BEGIN
    RAISERROR(N'Prvo pokrenuti OP_910_tmp_Kreiranje.sql. Ništa nije urađeno.', 16, 1);
    SET NOEXEC ON;
END
GO
IF SUSER_ID(N'KPSRV-NOVI\svc_cimprovera') IS NULL
BEGIN
    CREATE LOGIN [KPSRV-NOVI\svc_cimprovera] FROM WINDOWS WITH DEFAULT_DATABASE = [Perftech_Beograd];
    PRINT N'Login napravljen.';
END
GO
IF DATABASE_PRINCIPAL_ID(N'svc_cimprovera') IS NULL
BEGIN
    CREATE USER [svc_cimprovera] FOR LOGIN [KPSRV-NOVI\svc_cimprovera] WITH DEFAULT_SCHEMA = [dbo];
    PRINT N'Korisnik baze napravljen.';
END
GO
-- pisanje: samo tmp tabele
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpDokument        TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpJobs            TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpTeretnica       TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpTeretnicaStavke TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpJobsPolje       TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpJobsIstorija    TO [svc_cimprovera];
GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpSinonimi        TO [svc_cimprovera];
-- čitanje šifarnika
GRANT SELECT ON dbo.stanice   TO [svc_cimprovera];
GRANT SELECT ON dbo.Partnerji TO [svc_cimprovera];
GRANT SELECT ON dbo.NHM       TO [svc_cimprovera];
GRANT SELECT ON planiranje.tmpNHM TO [svc_cimprovera];   -- v2.9.1: šifarnik NHM iz tarife (OP_916 / OP_917)
-- samo čitanje, za poređenje u tački 8
GRANT SELECT ON planiranje.OP_Jobs    TO [svc_cimprovera];
GRANT SELECT ON dbo.Teretnica         TO [svc_cimprovera];
GRANT SELECT ON dbo.TeretnicaStavke   TO [svc_cimprovera];
GRANT SELECT ON dbo.Najava            TO [svc_cimprovera];
-- v2.9.2: platilac deonice KP preko broja ponude (samo čitanje; agent čita samo PonStPon i PonPart)
GRANT SELECT ON dbo.Ponudba           TO [svc_cimprovera];
GO
-- v3.0: događaji iz teksta maila (OP_918_tmp_Dogadjaji.sql) – pisanje samo u tmp tabelu; preskače se dok OP_918 nije pokrenut
IF OBJECT_ID(N'planiranje.tmpJobsDogadjaj', N'U') IS NOT NULL
    EXEC (N'GRANT SELECT, INSERT, UPDATE, DELETE ON planiranje.tmpJobsDogadjaj TO [svc_cimprovera];');
ELSE
    PRINT N'planiranje.tmpJobsDogadjaj ne postoji – pokrenuti OP_918_tmp_Dogadjaji.sql pa ponovo OP_913.';
GO
PRINT N'OP_913_tmp_Prava: gotovo. Prava naloga svc_cimprovera:';
SELECT s.name + N'.' + o.name AS Objekat, p.permission_name AS Pravo
FROM sys.database_permissions p
JOIN sys.objects o ON o.object_id = p.major_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE p.grantee_principal_id = DATABASE_PRINCIPAL_ID(N'svc_cimprovera') AND p.state = 'G'
ORDER BY 1, 2;
GO
SET NOEXEC OFF;
GO

/* ---------------------------------------------------------------------
   Poništavanje (ručno, kada se tmp tabele obrišu):
     DROP USER [svc_cimprovera];
     DROP LOGIN [KPSRV-NOVI\svc_cimprovera];
   Prava na tmp tabele nestaju sama kada OP_911_tmp_Brisanje.sql obriše tabele.
   --------------------------------------------------------------------- */
