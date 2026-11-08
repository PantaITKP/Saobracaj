/* =====================================================================
   OP_032_JobsSerijaKola.sql
   JOBS: serija kola po pravcu (Serija kola 1 i Serija kola 2), kao polje
   „serija vagona“ pored broja kola na formi Najava (dbo.Najava.SerijaVagona,
   izbor iz dbo.VagoniSerije).

   - planiranje.OP_Jobs dobija SerijaKola1 i SerijaKola2.
   - Posao prenet iz najave dobija SerijaKola1 iz dbo.Najava.SerijaVagona
     (samo gde je još prazno; dbo.Najava se samo čita).
   - OP_vJobPravac i OP_vNajava daju seriju pravca kao SerijaVagona, pa je
     plan, ekran Posao i Taskovi prikazuju kao „Tip kola“ (kao ranije iz najave).

   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_030 (i OP_031). Program ne radi bez nje.
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
IF OBJECT_ID(N'planiranje.OP_Jobs', N'U') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_030_Jobs.sql.', 16, 1);
GO

IF COL_LENGTH(N'planiranje.OP_Jobs', N'SerijaKola1') IS NULL
    ALTER TABLE planiranje.OP_Jobs ADD SerijaKola1 nvarchar(20) NULL;
IF COL_LENGTH(N'planiranje.OP_Jobs', N'SerijaKola2') IS NULL
    ALTER TABLE planiranje.OP_Jobs ADD SerijaKola2 nvarchar(20) NULL;
GO

-- Već preneti poslovi: serija iz najave.
UPDATE j SET SerijaKola1 = NULLIF(RTRIM(n.SerijaVagona), N'')
FROM   planiranje.OP_Jobs j
JOIN   dbo.Najava n ON n.ID = j.IzvornaNajavaID
WHERE  j.SerijaKola1 IS NULL AND NULLIF(RTRIM(n.SerijaVagona), N'') IS NOT NULL;
PRINT N'Serija kola iz najave upisana na ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' poslova.';
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
        CAST(x.SerijaKola AS nvarchar(50)) AS SerijaVagona,
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
                    j.Bruto1 AS Bruto, j.BrojKola1 AS BrojKola, j.SerijaKola1 AS SerijaKola, j.Duzina1 AS Duzina, j.Neto1 AS Neto, j.NHM1 AS NHM, j.RIDBroj1 AS RIDBroj, j.OMBroj1 AS OMBroj
             UNION ALL
             SELECT 2, j.Stanica2ID, j.Stanica3ID, j.Bruto2, j.BrojKola2, j.SerijaKola2, j.Duzina2, j.Neto2, j.NHM2, j.RIDBroj2, j.OMBroj2
             WHERE  j.Stanica2ID IS NOT NULL) x
LEFT JOIN dbo.stanice s1 ON s1.ID = x.OdID
LEFT JOIN dbo.stanice s2 ON s2.ID = x.DoID@SPOJ@;';
SET @sql = REPLACE(@sql, N'@STATUS@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'CAST(NULL AS nvarchar(100))' ELSE N'RTRIM(st.Opis)' END);
SET @sql = REPLACE(@sql, N'@SPOJ@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'' ELSE N' LEFT JOIN dbo.StatusVoza st ON st.ID = j.Status' END);
EXEC sys.sp_executesql @sql;
GO

-- Posao kao celina (stanica 1 → stanica 3); serija kola je serija prvog pravca.
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
        CAST(j.SerijaKola1 AS nvarchar(50)) AS SerijaVagona,
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

-- OP_vPosao čita OP_vJobPravac po imenu kolone; posle DROP/CREATE pogleda se osvežava njegova definicija.
EXEC sys.sp_refreshview N'planiranje.OP_vPosao';
GO

SELECT (SELECT COUNT(*) FROM planiranje.OP_Jobs WHERE SerijaKola1 IS NOT NULL OR SerijaKola2 IS NOT NULL) AS [Poslova sa serijom],
       (SELECT COUNT(*) FROM planiranje.OP_vPosao WHERE NajavaSerijaVagona IS NOT NULL) AS [Pravaca sa tipom kola];
PRINT N'OP_032 završena.';
GO

/* Poništavanje (ručno): pokrenuti ponovo OP_030_Jobs.sql (pogledi), pa:
ALTER TABLE planiranje.OP_Jobs DROP COLUMN SerijaKola2;
ALTER TABLE planiranje.OP_Jobs DROP COLUMN SerijaKola1;
*/
