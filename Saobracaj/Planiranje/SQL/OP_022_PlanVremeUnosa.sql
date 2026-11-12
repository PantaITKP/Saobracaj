/* =====================================================================
   OP_022_PlanVremeUnosa.sql
   Vreme unosa reda u tabelama „Stanje na mreži“ (dbo.OperativniPlanMreza)
   i „Najave vozova“ (dbo.OperativniPlanNajave), tabovi u Planeru i dispMain-u.

   - CreatedUtc: kada je red unet. Novi red ga dobija sam (DEFAULT GETUTCDATE()),
     a čuvanje u Planeru i dispMain-u ga ne menja, jer upisuje samo kolone za unos.
   - IzmenjenoUtc (već postoji): poslednja izmena; čuvanje ga postavlja na svaku izmenu.
   - Postojeći redovi nemaju zapisano vreme unosa, pa dobijaju vreme poslednje izmene.
   - Naziv CreatedUtc je isti kao u dbo.OperativniPlanVozovi, a grid ga i u starim
     verzijama aplikacije (master) ne prikazuje.
   - Samo dodaje; može da se pokrene više puta. Poništavanje je na dnu.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.OperativniPlanMreza', N'CreatedUtc') IS NULL
    ALTER TABLE dbo.OperativniPlanMreza ADD CreatedUtc datetime NULL CONSTRAINT DF_OperativniPlanMreza_CreatedUtc DEFAULT (GETUTCDATE());
IF COL_LENGTH(N'dbo.OperativniPlanNajave', N'CreatedUtc') IS NULL
    ALTER TABLE dbo.OperativniPlanNajave ADD CreatedUtc datetime NULL CONSTRAINT DF_OperativniPlanNajave_CreatedUtc DEFAULT (GETUTCDATE());
GO

UPDATE dbo.OperativniPlanMreza SET CreatedUtc = IzmenjenoUtc WHERE CreatedUtc IS NULL;
PRINT N'Mreža, popunjeno vreme unosa: ' + CAST(@@ROWCOUNT AS nvarchar(10));
UPDATE dbo.OperativniPlanNajave SET CreatedUtc = IzmenjenoUtc WHERE CreatedUtc IS NULL;
PRINT N'Najave, popunjeno vreme unosa: ' + CAST(@@ROWCOUNT AS nvarchar(10));
GO

SELECT N'OperativniPlanMreza' AS Tabela, COUNT(*) AS Redova, MIN(CreatedUtc) AS NajranijiUnos, MAX(CreatedUtc) AS PoslednjiUnos FROM dbo.OperativniPlanMreza
UNION ALL
SELECT N'OperativniPlanNajave', COUNT(*), MIN(CreatedUtc), MAX(CreatedUtc) FROM dbo.OperativniPlanNajave;
GO

/* Poništavanje (ručno, po potrebi):

ALTER TABLE dbo.OperativniPlanMreza DROP CONSTRAINT DF_OperativniPlanMreza_CreatedUtc;
ALTER TABLE dbo.OperativniPlanMreza DROP COLUMN CreatedUtc;
ALTER TABLE dbo.OperativniPlanNajave DROP CONSTRAINT DF_OperativniPlanNajave_CreatedUtc;
ALTER TABLE dbo.OperativniPlanNajave DROP COLUMN CreatedUtc;
*/
