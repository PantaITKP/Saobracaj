/* =====================================================================
   OP_919_tmp_Izvor_Zamenjena.sql
   v3.0.3 (PROMPT_v3.0.3, tačke 2.1, 2.2 i 3.5): razdvajanje podataka agenta CimProvera i alata CimRadionica u planiranje.tmp*,
   oznaka zamenjene teretnice i novi razlozi istorije za CIT 18.

   - Kolona Izvor ('Agent' / 'Alat') u tabelama koje pišu agent i alat: tmpJobs, tmpJobsPolje, tmpJobsIstorija, tmpJobsDogadjaj,
     tmpTeretnica, tmpTeretnicaStavke, tmpDokument. tmpSinonimi i tmpNHM su zajednički i ne dobijaju kolonu.
       * Podrazumevana vrednost: 'Agent'. Alat posle otvaranja konekcije postavlja SET CONTEXT_INFO 0x416C6174 („Alat“), pa nov red
         dobija 'Alat' bez izmene INSERT naredbi (podrazumevana vrednost čita CONTEXT_INFO()).
       * Jedinstveni indeksi (heš fajla, ključ pošiljke posla, ključ spiska) važe po izvoru – isti fajl / ista pošiljka može da postoji
         i kod agenta i kod alata.
       * Postojeći redovi: na dan 19.11.2026. agent je isključen od 18.11., a tmp podaci su posle toga pisani samo iz alata
         (obrada „Osnovni“ 3.0.2). Skripta zato postojeće redove označava kao 'Alat' (parametar @PostojeciSu ispod).
   - tmpTeretnica: Zamenjena (bit, 0) i ZamenjenaTeretnicaID – spisak iste pošiljke zamenjen novijim spiskom (nije aktivan).
   - tmpJobsIstorija: razlozi CIT18 i CIT18_BEZ_ORIGINALA.
   - dbo.* i planiranje.OP_Jobs se ne diraju. SQL Server 2008 R2 (compat 80): bez JSON, STRING_AGG, TRY_CONVERT, IIF.
   - Može da se pokrene više puta. Pokreće se ručno (sysadmin), posle OP_910, OP_916 i OP_918, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_919_tmp_Izvor_Zamenjena.sql
   - Prava naloga svc_cimprovera su na nivou tabele (OP_913) i važe i za nove kolone.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'planiranje.tmpJobs', N'U') IS NULL OR OBJECT_ID(N'planiranje.tmpJobsDogadjaj', N'U') IS NULL
BEGIN
    RAISERROR(N'Prvo pokrenuti OP_910_tmp_Kreiranje.sql i OP_918_tmp_Dogadjaji.sql. Ništa nije urađeno.', 16, 1);
    SET NOEXEC ON;
END
GO
/* ---------------------------------------------------------------- 1. kolona Izvor */
DECLARE @PostojeciSu varchar(5);
SET @PostojeciSu = 'Alat';   -- izvor postojećih redova (vidi zaglavlje); 'Agent' ako je agent pisao posle poslednjeg brisanja

DECLARE @t sysname, @sql nvarchar(max);
DECLARE t CURSOR LOCAL FAST_FORWARD FOR
    SELECT x FROM (SELECT N'tmpJobs' AS x UNION ALL SELECT N'tmpJobsPolje' UNION ALL SELECT N'tmpJobsIstorija' UNION ALL SELECT N'tmpJobsDogadjaj'
                   UNION ALL SELECT N'tmpTeretnica' UNION ALL SELECT N'tmpTeretnicaStavke' UNION ALL SELECT N'tmpDokument') a;
OPEN t;
FETCH NEXT FROM t INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF COL_LENGTH(N'planiranje.' + @t, N'Izvor') IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE planiranje.' + @t + N' ADD Izvor varchar(5) NULL;';
        EXEC (@sql);
        SET @sql = N'UPDATE planiranje.' + @t + N' SET Izvor = @p WHERE Izvor IS NULL;';
        EXEC sp_executesql @sql, N'@p varchar(5)', @p = @PostojeciSu;
        SET @sql = N'ALTER TABLE planiranje.' + @t + N' ALTER COLUMN Izvor varchar(5) NOT NULL;';
        EXEC (@sql);
        SET @sql = N'ALTER TABLE planiranje.' + @t + N' ADD CONSTRAINT DF_' + @t + N'_Izvor DEFAULT (CASE WHEN SUBSTRING(CONTEXT_INFO(), 1, 4) = 0x416C6174 THEN ''Alat'' ELSE ''Agent'' END) FOR Izvor;';
        EXEC (@sql);
        SET @sql = N'ALTER TABLE planiranje.' + @t + N' ADD CONSTRAINT CK_' + @t + N'_Izvor CHECK (Izvor IN (''Agent'', ''Alat''));';
        EXEC (@sql);
        PRINT N'planiranje.' + @t + N': dodata kolona Izvor (postojeći redovi: ' + @PostojeciSu + N')';
    END
    FETCH NEXT FROM t INTO @t;
END
CLOSE t; DEALLOCATE t;
GO
/* ---------------------------------------------------------------- 2. jedinstveni indeksi po izvoru */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'UX_tmpDokument_Hash')
   AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   WHERE i.object_id = OBJECT_ID(N'planiranje.tmpDokument') AND i.name = N'UX_tmpDokument_Hash' AND c.name = N'Izvor')
    DROP INDEX UX_tmpDokument_Hash ON planiranje.tmpDokument;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'UX_tmpDokument_Hash')
    CREATE UNIQUE INDEX UX_tmpDokument_Hash ON planiranje.tmpDokument (HashFajla, Izvor) WHERE HashFajla IS NOT NULL;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobs') AND name = N'UX_tmpJobs_Kljuc')
   AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   WHERE i.object_id = OBJECT_ID(N'planiranje.tmpJobs') AND i.name = N'UX_tmpJobs_Kljuc' AND c.name = N'Izvor')
    DROP INDEX UX_tmpJobs_Kljuc ON planiranje.tmpJobs;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobs') AND name = N'UX_tmpJobs_Kljuc')
    CREATE UNIQUE INDEX UX_tmpJobs_Kljuc ON planiranje.tmpJobs (KljucPosiljke, Izvor) WHERE KljucPosiljke IS NOT NULL;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND name = N'UX_tmpTeretnica_KljucSpiska')
   AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   WHERE i.object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND i.name = N'UX_tmpTeretnica_KljucSpiska' AND c.name = N'Izvor')
    DROP INDEX UX_tmpTeretnica_KljucSpiska ON planiranje.tmpTeretnica;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND name = N'UX_tmpTeretnica_KljucSpiska')
    CREATE UNIQUE INDEX UX_tmpTeretnica_KljucSpiska ON planiranje.tmpTeretnica (KljucSpiska, Izvor) WHERE KljucSpiska IS NOT NULL;
GO
/* ---------------------------------------------------------------- 3. zamenjena teretnica */
IF COL_LENGTH(N'planiranje.tmpTeretnica', N'Zamenjena') IS NULL
    ALTER TABLE planiranje.tmpTeretnica ADD Zamenjena bit NOT NULL CONSTRAINT DF_tmpTeretnica_Zamenjena DEFAULT (0);
IF COL_LENGTH(N'planiranje.tmpTeretnica', N'ZamenjenaTeretnicaID') IS NULL
    ALTER TABLE planiranje.tmpTeretnica ADD ZamenjenaTeretnicaID int NULL;   -- teretnica (spisak) koja je zamenila ovu
GO
/* ---------------------------------------------------------------- 4. razlozi istorije: CIT 18 */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog' AND definition NOT LIKE N'%CIT18[_]BEZ[_]ORIGINALA%')
    ALTER TABLE planiranje.tmpJobsIstorija DROP CONSTRAINT CK_tmpJobsIstorija_Razlog;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog')
    ALTER TABLE planiranje.tmpJobsIstorija ADD CONSTRAINT CK_tmpJobsIstorija_Razlog
        CHECK (Razlog IN ('NOVI_POSAO','NOVA_VERZIJA_CIM','SPISAK_KOLA','CIT7','POVRATAK_PRAZNIH','PONOVLJEN_MAIL','DOGADJAJ','CIT18','CIT18_BEZ_ORIGINALA'));
GO
/* ---------------------------------------------------------------- provera */
SELECT N'tmpJobs' AS tabela, Izvor, COUNT(*) AS redova FROM planiranje.tmpJobs GROUP BY Izvor
UNION ALL SELECT N'tmpTeretnica', Izvor, COUNT(*) FROM planiranje.tmpTeretnica GROUP BY Izvor
UNION ALL SELECT N'tmpTeretnicaStavke', Izvor, COUNT(*) FROM planiranje.tmpTeretnicaStavke GROUP BY Izvor
UNION ALL SELECT N'tmpDokument', Izvor, COUNT(*) FROM planiranje.tmpDokument GROUP BY Izvor
UNION ALL SELECT N'tmpJobsPolje', Izvor, COUNT(*) FROM planiranje.tmpJobsPolje GROUP BY Izvor
UNION ALL SELECT N'tmpJobsIstorija', Izvor, COUNT(*) FROM planiranje.tmpJobsIstorija GROUP BY Izvor
UNION ALL SELECT N'tmpJobsDogadjaj', Izvor, COUNT(*) FROM planiranje.tmpJobsDogadjaj GROUP BY Izvor;
GO
SET NOEXEC OFF;
GO
