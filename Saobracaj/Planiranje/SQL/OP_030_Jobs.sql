/* =====================================================================
   OP_030_Jobs.sql
   JOBS (Zadatak 14): posao koji zamenjuje najavu u planiranju.

   - planiranje.OP_Jobs      posao: platilac, organizator, tip prevoza, otpravna,
                             uputna, status (dbo.StatusVoza), napomena, preuzima od
                             i predaje operateru, stanice 1–3, ETD i roba po pravcu.
                             Stanica 2 je stanica istovara: bez nje posao ima jedan
                             pravac (1 → 3), a sa njom dva (1 → 2 i 2 → 3).
                             Šifra je ID; posao prenet iz najave zadržava broj najave,
                             a novi posao počinje od 1.000.000.
   - planiranje.OP_JobsLog   LOG: ko je i kada napravio, izmenio ili preneo posao.
   - planiranje.OP_vJobPravac  jedan red po pravcu posla (stanice i roba pravca).
   - OP_vNajava              od sada čita OP_Jobs (jedan red po poslu, stanica 1 → 3),
                             a OP_vPosao čita pravac posla (OP_Posao.RBDeonice = pravac).
                             Planiranje (plan, Taskovi, Ponuda osoblja) time radi sa JOBS-om;
                             najava ulazi u plan samo prenosom u JOBS.
   - OP_vTaskNajava          redosled taskova po pravcu (posao), a ne samo po najavi.

   - U dbo se ništa ne menja (dbo.Najava, Partnerji, Stanice, StatusVoza,
     TipSaobPrevoza i NHM se samo čitaju). Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_029. Program ne radi bez nje.
   - Serija kola (OP_032) nije ovde: posle ponovnog puštanja ove skripte pokrenuti i OP_032,
     jer ova vraća pogledima praznu seriju.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'planiranje.OP_OsobljeSerija', N'U') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_029_OsobljeSerijeProvere.sql.', 16, 1);
GO

IF OBJECT_ID(N'planiranje.OP_Jobs', N'U') IS NULL
CREATE TABLE planiranje.OP_Jobs
(
    ID                  int IDENTITY(1000000,1) NOT NULL CONSTRAINT PK_OP_Jobs PRIMARY KEY,   -- šifra
    -- Opšti podaci
    PlatilacID          int            NULL,          -- dbo.Partnerji.PaSifra
    OrganizatorID       int            NULL,          -- dbo.Partnerji.PaSifra
    TipPrevozaID        int            NULL,          -- dbo.TipSaobPrevoza.ID
    OtpravnaID          int            NULL,          -- dbo.stanice.ID
    UputnaID            int            NULL,          -- dbo.stanice.ID
    Status              int            NOT NULL CONSTRAINT DF_OP_Jobs_Status DEFAULT (1),   -- dbo.StatusVoza.ID
    Napomena            nvarchar(1000) NULL,
    -- Relacija
    PreuzimaOdID        int            NULL,          -- dbo.Partnerji.PaSifra (operater od koga se preuzima)
    PredajeOperateruID  int            NULL,          -- dbo.Partnerji.PaSifra (operater kome se predaje)
    Stanica1ID          int            NOT NULL,      -- dbo.stanice.ID: preuzimanje
    Stanica2ID          int            NULL,          -- dbo.stanice.ID: istovar (drugi pravac)
    Stanica3ID          int            NOT NULL,      -- dbo.stanice.ID: predaja
    ETD                 datetime2(0)   NULL,
    -- Roba, pravac 1 i pravac 2
    NHM1                int            NULL,          -- dbo.NHM.ID
    Bruto1              decimal(18,3)  NULL,
    BrojKola1           int            NULL,
    Duzina1             decimal(18,2)  NULL,
    Neto1               decimal(18,3)  NULL,
    RIDBroj1            nvarchar(50)   NULL,
    OMBroj1             nvarchar(50)   NULL,
    NHM2                int            NULL,
    Bruto2              decimal(18,3)  NULL,
    BrojKola2           int            NULL,
    Duzina2             decimal(18,2)  NULL,
    Neto2               decimal(18,3)  NULL,
    RIDBroj2            nvarchar(50)   NULL,
    OMBroj2             nvarchar(50)   NULL,
    IzvornaNajavaID     int            NULL,          -- dbo.Najava.ID kod prenosa
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Jobs_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_Jobs_Kreirano DEFAULT (SYSDATETIME()),
    Izmenio             nvarchar(50)   NULL,
    Izmenjeno           datetime2(0)   NULL,
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_Jobs_Stanice CHECK (Stanica2ID IS NOT NULL OR Stanica1ID <> Stanica3ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Jobs_ETD')
    CREATE INDEX IX_OP_Jobs_ETD ON planiranje.OP_Jobs(ETD) INCLUDE (Status, PlatilacID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Jobs_Najava')
    CREATE UNIQUE INDEX UX_OP_Jobs_Najava ON planiranje.OP_Jobs(IzvornaNajavaID) WHERE IzvornaNajavaID IS NOT NULL;
GO

IF OBJECT_ID(N'planiranje.OP_JobsLog', N'U') IS NULL
CREATE TABLE planiranje.OP_JobsLog
(
    ID          bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_JobsLog PRIMARY KEY,
    JobID       int            NOT NULL,              -- bez stranog ključa: log ostaje i kad se posao obriše
    Vreme       datetime2(0)   NOT NULL CONSTRAINT DF_OP_JobsLog_Vreme DEFAULT (SYSDATETIME()),
    Korisnik    nvarchar(50)   NOT NULL,
    Akcija      nvarchar(20)   NOT NULL,              -- NOV, IZMENA, PRENOS, BRISANJE
    Opis        nvarchar(max)  NULL                   -- izmenjena polja: staro → novo
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_JobsLog_Job')
    CREATE INDEX IX_OP_JobsLog_Job ON planiranje.OP_JobsLog(JobID, Vreme);
GO

-- Pravac posla: 1 = stanica 1 → stanica 2 (ili 3 bez stanice 2), 2 = stanica 2 → stanica 3.
IF OBJECT_ID(N'planiranje.OP_vJobPravac', N'V') IS NOT NULL DROP VIEW planiranje.OP_vJobPravac;
GO
DECLARE @sql nvarchar(max);
SET @sql = N'CREATE VIEW planiranje.OP_vJobPravac
AS
SELECT  j.ID                    AS NajavaID,
        x.Pravac,
        CAST(j.ID AS nvarchar(20)) + CASE WHEN j.Stanica2ID IS NULL THEN N'''' ELSE N''/'' + CAST(x.Pravac AS nvarchar(1)) END AS BrojNajave,
        x.OdID                  AS OtpravnaID,
        x.DoID                  AS UputnaID,
        CAST(NULL AS int)       AS OtpravnaGrID,
        CAST(NULL AS int)       AS UputnaGrID,
        x.OdID                  AS PlanOtpravnaID,
        x.DoID                  AS PlanUputnaID,
        RTRIM(s1.Opis)          AS PlanOtpravna,
        RTRIM(s2.Opis)          AS PlanUputna,
        RTRIM(s1.Opis)          AS Otpravna,
        RTRIM(s2.Opis)          AS Uputna,
        CAST(NULL AS nvarchar(100)) AS OtpravnaGr,
        CAST(NULL AS nvarchar(100)) AS UputnaGr,
        j.PlatilacID,
        j.PreuzimaOdID          AS PrevoznikID,
        x.Bruto                 AS BrutoT,
        x.Duzina                AS DuzinaM,
        x.BrojKola,
        CAST(NULL AS nvarchar(50)) AS SerijaVagona,
        CAST(NULL AS int)       AS Tovareno,
        x.RIDBroj               AS RID,
        CAST(NULL AS bit)       AS AdHoc,
        j.ETD                   AS PredvidjenoPrimanje,
        CAST(NULL AS datetime2(0)) AS StvarnoPrimanje,
        CAST(NULL AS datetime2(0)) AS PredvidjenaPredaja,
        CAST(NULL AS datetime2(0)) AS StvarnaPredaja,
        j.Status                AS StatusNajave,
        @STATUS@                AS StatusOpis,
        j.Napomena              AS NapomenaPriprema,
        x.Neto, x.NHM, x.OMBroj
FROM    planiranje.OP_Jobs j
CROSS APPLY (SELECT 1 AS Pravac, j.Stanica1ID AS OdID, COALESCE(j.Stanica2ID, j.Stanica3ID) AS DoID,
                    j.Bruto1 AS Bruto, j.BrojKola1 AS BrojKola, j.Duzina1 AS Duzina, j.Neto1 AS Neto, j.NHM1 AS NHM, j.RIDBroj1 AS RIDBroj, j.OMBroj1 AS OMBroj
             UNION ALL
             SELECT 2, j.Stanica2ID, j.Stanica3ID, j.Bruto2, j.BrojKola2, j.Duzina2, j.Neto2, j.NHM2, j.RIDBroj2, j.OMBroj2
             WHERE  j.Stanica2ID IS NOT NULL) x
LEFT JOIN dbo.stanice s1 ON s1.ID = x.OdID
LEFT JOIN dbo.stanice s2 ON s2.ID = x.DoID@SPOJ@;';
SET @sql = REPLACE(@sql, N'@STATUS@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'CAST(NULL AS nvarchar(100))' ELSE N'RTRIM(st.Opis)' END);
SET @sql = REPLACE(@sql, N'@SPOJ@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'' ELSE N' LEFT JOIN dbo.StatusVoza st ON st.ID = j.Status' END);
EXEC sys.sp_executesql @sql;
GO

-- Isti stupci kao ranije (posao kao celina: stanica 1 → stanica 3), da postojeći upiti po NajavaID ostanu ispravni.
DECLARE @sql nvarchar(max);
SET @sql = N'ALTER VIEW planiranje.OP_vNajava
AS
SELECT  j.ID                    AS NajavaID,
        CAST(j.ID AS nvarchar(20)) AS BrojNajave,
        j.Stanica1ID            AS OtpravnaID,
        j.Stanica3ID            AS UputnaID,
        CAST(NULL AS int)       AS OtpravnaGrID,
        CAST(NULL AS int)       AS UputnaGrID,
        j.Stanica1ID            AS PlanOtpravnaID,
        j.Stanica3ID            AS PlanUputnaID,
        RTRIM(s1.Opis)          AS PlanOtpravna,
        RTRIM(s3.Opis)          AS PlanUputna,
        RTRIM(s1.Opis)          AS Otpravna,
        RTRIM(s3.Opis)          AS Uputna,
        CAST(NULL AS nvarchar(100)) AS OtpravnaGr,
        CAST(NULL AS nvarchar(100)) AS UputnaGr,
        j.PlatilacID,
        j.PreuzimaOdID          AS PrevoznikID,
        j.Bruto1                AS BrutoT,
        j.Duzina1               AS DuzinaM,
        j.BrojKola1             AS BrojKola,
        CAST(NULL AS nvarchar(50)) AS SerijaVagona,
        CAST(NULL AS int)       AS Tovareno,
        j.RIDBroj1              AS RID,
        CAST(NULL AS bit)       AS AdHoc,
        j.ETD                   AS PredvidjenoPrimanje,
        CAST(NULL AS datetime2(0)) AS StvarnoPrimanje,
        CAST(NULL AS datetime2(0)) AS PredvidjenaPredaja,
        CAST(NULL AS datetime2(0)) AS StvarnaPredaja,
        j.Status                AS StatusNajave,
        @STATUS@                AS StatusOpis,
        j.Napomena              AS NapomenaPriprema
FROM    planiranje.OP_Jobs j
LEFT JOIN dbo.stanice s1 ON s1.ID = j.Stanica1ID
LEFT JOIN dbo.stanice s3 ON s3.ID = j.Stanica3ID@SPOJ@;';
SET @sql = REPLACE(@sql, N'@STATUS@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'CAST(NULL AS nvarchar(100))' ELSE N'RTRIM(st.Opis)' END);
SET @sql = REPLACE(@sql, N'@SPOJ@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'' ELSE N' LEFT JOIN dbo.StatusVoza st ON st.ID = j.Status' END);
EXEC sys.sp_executesql @sql;
GO

-- Posao je pravac posla: RBDeonice 1 ili 2.
ALTER VIEW planiranje.OP_vPosao
AS
SELECT  p.ID, p.NajavaID, p.RBDeonice, p.Faza, p.Status, p.TrasaID,
        p.PrijemStanicaID, p.PredajaStanicaID,
        p.PlaniraniPrijem, p.PlaniraniPolazak, p.PlaniraniDolazak, p.PlaniranaPredaja,
        p.StvarniPolazak, p.StvarniDolazak,
        p.PotrebnoLokomotiva, p.Projekat, p.OperativniVozID, p.Napomena,
        n.BrojNajave, n.Otpravna AS NajavaOtpravna, n.Uputna AS NajavaUputna,
        n.OtpravnaGr AS NajavaOtpravnaGr, n.UputnaGr AS NajavaUputnaGr,
        n.PlanOtpravnaID, n.PlanUputnaID, n.PlanOtpravna, n.PlanUputna,
        n.StatusNajave, n.StatusOpis,
        n.BrutoT AS NajavaBrutoT, n.DuzinaM AS NajavaDuzinaM, n.BrojKola AS NajavaBrojKola,
        n.SerijaVagona AS NajavaSerijaVagona, n.Tovareno AS NajavaTovareno, n.RID AS NajavaRID,
        n.PredvidjenoPrimanje, n.PredvidjenaPredaja, n.StvarnoPrimanje, n.StvarnaPredaja,
        p.RowVer
FROM    planiranje.OP_Posao p
LEFT JOIN planiranje.OP_vJobPravac n ON n.NajavaID = p.NajavaID AND n.Pravac = p.RBDeonice;
GO

-- Redosled taskova po pravcu: deo rute i bruto delovi se broje u okviru posla (pravca).
ALTER VIEW planiranje.OP_vTaskNajava
AS
SELECT  tn.TaskID, tn.NajavaID, tn.PosaoID, tn.BrojNajave, tn.KmOd, tn.KmDo, tn.RBDela,
        tn.BrutoT AS NajavaBrutoT, tn.BrojKola AS NajavaBrojKola, t.Status,
        CASE WHEN t.Status = N'STORNIRAN' THEN NULL
             ELSE DENSE_RANK() OVER (PARTITION BY tn.NajavaID, ISNULL(tn.PosaoID, 0), CASE WHEN t.Status = N'STORNIRAN' THEN 1 ELSE 0 END ORDER BY tn.KmOd) END AS DeoRute,
        CASE WHEN t.Status = N'STORNIRAN' THEN NULL
             ELSE COUNT(*) OVER (PARTITION BY tn.NajavaID, ISNULL(tn.PosaoID, 0), tn.KmOd, CASE WHEN t.Status = N'STORNIRAN' THEN 1 ELSE 0 END) END AS DelovaBruto
FROM    planiranje.OP_TaskNajava tn
JOIN    planiranje.OP_Task t ON t.ID = tn.TaskID;
GO

SELECT (SELECT COUNT(*) FROM planiranje.OP_Jobs) AS Jobs, (SELECT COUNT(*) FROM planiranje.OP_vJobPravac) AS Pravaca,
       (SELECT COUNT(*) FROM planiranje.OP_JobsLog) AS Log;
PRINT N'OP_030 završena. Najave ulaze u plan tek posle prenosa u JOBS (dugme na ekranu JOBS).';
GO

/* Poništavanje (ručno; vraća planiranje na dbo.Najava):
   pokrenuti ponovo OP_018_NajavaDatumi.sql (pogled OP_vNajava) i OP_017 deo za OP_vPosao,
   OP_022 deo za OP_vTaskNajava, pa:
DROP VIEW planiranje.OP_vJobPravac;
DROP TABLE planiranje.OP_JobsLog;
DROP TABLE planiranje.OP_Jobs;
*/
