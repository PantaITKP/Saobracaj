/* =====================================================================
   OP_916_tmp_Izmene.sql
   Izmene tmp tabela za agenta CimProvera v2.9.1 (zadatak PROMPT_v2.9.1, tačka 2).
   (U zadatku se zove „OP_914_tmp_Izmene.sql“; broj 914 je već zauzet skriptom sa sinonimima, a 915 brisanjem podataka.)

   - Menja SAMO planiranje.tmp* tabele. dbo.* i planiranje.OP_Jobs se ne diraju.
   - tmpJobs: PredajePartneruID, Tara1, Tara2 (t). NHM1/NHM2 od sada čuvaju planiranje.tmpNHM.ID.
   - tmpTeretnica: Grupa (1 = natovarena kola, 2 = prazna kola / druga roba).
   - tmpNHM: šifarnik NHM iz tarife Srbija Kargo (Deo 2 – Klasifikacija robe); punjenje: OP_917_tmp_NHM_Podaci.sql.
   - tmpSinonimi: novi tip GRANICA (granični prelaz → stanice.ID).
   - tmpJobsIstorija: novi razlozi POVRATAK_PRAZNIH, PONOVLJEN_MAIL.
   - Može da se pokrene više puta. Pokreće se ručno (sysadmin), posle OP_910:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_916_tmp_Izmene.sql
   - Posle ove skripte pokrenuti OP_913_tmp_Prava.sql (SELECT na tmpNHM za svc_cimprovera).
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
IF OBJECT_ID(N'planiranje.tmpJobs', N'U') IS NULL
BEGIN
    RAISERROR(N'Prvo pokrenuti OP_910_tmp_Kreiranje.sql. Ništa nije urađeno.', 16, 1);
    SET NOEXEC ON;
END
GO

/* 1. tmpJobs ---------------------------------------------------------- */
IF COL_LENGTH(N'planiranje.tmpJobs', N'PredajePartneruID') IS NULL
    ALTER TABLE planiranje.tmpJobs ADD PredajePartneruID int NULL;   -- Partnerji.PaSifra: primalac iz r4 kada je KP poslednji u lancu
IF COL_LENGTH(N'planiranje.tmpJobs', N'Tara1') IS NULL
    ALTER TABLE planiranje.tmpJobs ADD Tara1 decimal(12,3) NOT NULL CONSTRAINT DF_tmpJobs_Tara1 DEFAULT (0);   -- t
IF COL_LENGTH(N'planiranje.tmpJobs', N'Tara2') IS NULL
    ALTER TABLE planiranje.tmpJobs ADD Tara2 decimal(12,3) NOT NULL CONSTRAINT DF_tmpJobs_Tara2 DEFAULT (0);   -- t
GO

/* 2. tmpTeretnica: grupa posla ---------------------------------------- */
IF COL_LENGTH(N'planiranje.tmpTeretnica', N'Grupa') IS NULL
    ALTER TABLE planiranje.tmpTeretnica ADD Grupa tinyint NULL;      -- 1 = natovarena kola (NHM1), 2 = prazna kola / druga roba (NHM2)
GO

/* 3. tmpNHM ------------------------------------------------------------ */
IF OBJECT_ID(N'planiranje.tmpNHM', N'U') IS NULL
CREATE TABLE planiranje.tmpNHM
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpNHM PRIMARY KEY,
    Kod8                char(8)        NOT NULL,      -- „25201000“
    Kod6                char(6)        NOT NULL,
    Kod4                char(4)        NOT NULL,
    Nivo                tinyint        NOT NULL,      -- 2 = glava („XX00 00 00“), 4 = „XXXX 00 00“, 6 = „XXXX XX 00“, 8 = ostalo
    NazivCir            nvarchar(500)  NULL,
    NazivLat            nvarchar(500)  NULL,
    NazivNorm           nvarchar(500)  NULL,          -- latinica, bez dijakritika, mala slova
    TarifniRazred       tinyint        NULL,
    Napomena            nvarchar(500)  NULL,
    Izvor               nvarchar(200)  NULL,
    Kreirano            datetime       NOT NULL CONSTRAINT DF_tmpNHM_Kreirano DEFAULT (GETDATE()),
    CONSTRAINT CK_tmpNHM_Nivo CHECK (Nivo IN (2, 4, 6, 8))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpNHM') AND name = N'UX_tmpNHM_Kod8')
    CREATE UNIQUE INDEX UX_tmpNHM_Kod8 ON planiranje.tmpNHM (Kod8);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpNHM') AND name = N'IX_tmpNHM_Kod6')
    CREATE INDEX IX_tmpNHM_Kod6 ON planiranje.tmpNHM (Kod6);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpNHM') AND name = N'IX_tmpNHM_Kod4')
    CREATE INDEX IX_tmpNHM_Kod4 ON planiranje.tmpNHM (Kod4, Nivo);
GO

/* 4. tmpSinonimi: tip GRANICA ---------------------------------------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpSinonimi_Tip' AND definition NOT LIKE N'%GRANICA%')
    ALTER TABLE planiranje.tmpSinonimi DROP CONSTRAINT CK_tmpSinonimi_Tip;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpSinonimi_Tip')
    ALTER TABLE planiranje.tmpSinonimi ADD CONSTRAINT CK_tmpSinonimi_Tip CHECK (Tip IN ('PARTNER','STANICA','NHM','GRANICA'));
GO

/* 5. tmpJobsIstorija: novi razlozi ------------------------------------ */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog' AND definition NOT LIKE N'%POVRATAK_PRAZNIH%')
    ALTER TABLE planiranje.tmpJobsIstorija DROP CONSTRAINT CK_tmpJobsIstorija_Razlog;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog')
    ALTER TABLE planiranje.tmpJobsIstorija ADD CONSTRAINT CK_tmpJobsIstorija_Razlog
        CHECK (Razlog IN ('NOVI_POSAO','NOVA_VERZIJA_CIM','SPISAK_KOLA','CIT7','POVRATAK_PRAZNIH','PONOVLJEN_MAIL'));
GO

PRINT N'OP_916_tmp_Izmene: gotovo.';
SELECT t.name AS Tabela, c.name AS Kolona, ty.name AS Tip
FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.schema_id = SCHEMA_ID(N'planiranje') AND ((t.name = N'tmpJobs' AND c.name IN (N'PredajePartneruID', N'Tara1', N'Tara2'))
   OR (t.name = N'tmpTeretnica' AND c.name = N'Grupa') OR t.name = N'tmpNHM')
ORDER BY t.name, c.column_id;
GO
SET NOEXEC OFF;
GO
