/* =====================================================================
   OP_024_TaskTrajanje.sql
   Trajanje taska: vreme koje je potrebno da se task izvrši
   = obrada (TASK_OBRADA_MIN, podrazumevano 180 min = 3 h)
   + vožnja po pruzi (km rute taska / BRZINA_VOZA_KMH, podrazumevano 40 km/h).

   - Dodaje kolonu planiranje.OP_Task.TrajanjeMin. Program je računa pri
     svakom novom tasku (izbor rute, split, spajanje).
   - Postojećim taskovima upisuje trajanje po globalnim parametrima.
   - Dodaje parametre TASK_OBRADA_MIN i BRZINA_VOZA_KMH ako ih nema.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_022. Program ne radi bez nje.
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
IF OBJECT_ID(N'planiranje.OP_Task', N'U') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_022_Taskovi.sql.', 16, 1);
GO
IF COL_LENGTH('planiranje.OP_Task', 'TrajanjeMin') IS NULL
    ALTER TABLE planiranje.OP_Task ADD TrajanjeMin int NULL;
GO

-- Parametri (globalni, važe od 1.1.2000, kao ostali); postojeći red se ne menja.
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_Parametri WHERE Kljuc = N'TASK_OBRADA_MIN')
    INSERT planiranje.OP_Parametri (Kljuc, Vrednost, Jedinica, VaziOd, Izvor, Opis)
    VALUES (N'TASK_OBRADA_MIN', 180, N'min', '20000101', N'RUCNO', N'Trajanje taska: obrada uz vožnju (3 h)');
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_Parametri WHERE Kljuc = N'BRZINA_VOZA_KMH')
    INSERT planiranje.OP_Parametri (Kljuc, Vrednost, Jedinica, VaziOd, Izvor, Opis)
    VALUES (N'BRZINA_VOZA_KMH', 40, N'km/h', '20000101', N'RUCNO', N'Trajanje taska: brzina vožnje po pruzi');
GO

DECLARE @obrada decimal(18,4), @brzina decimal(18,4);
SELECT TOP 1 @obrada = Vrednost FROM planiranje.OP_Parametri
WHERE Kljuc = N'TASK_OBRADA_MIN' AND StanicaID IS NULL AND StanicaDoID IS NULL AND SerijaID IS NULL ORDER BY VaziOd DESC;
SELECT TOP 1 @brzina = Vrednost FROM planiranje.OP_Parametri
WHERE Kljuc = N'BRZINA_VOZA_KMH' AND StanicaID IS NULL AND StanicaDoID IS NULL AND SerijaID IS NULL ORDER BY VaziOd DESC;
SET @obrada = ISNULL(@obrada, 180);
SET @brzina = CASE WHEN ISNULL(@brzina, 0) > 0 THEN @brzina ELSE 40 END;

UPDATE planiranje.OP_Task
SET    TrajanjeMin = CAST(CEILING(@obrada + Km / @brzina * 60) AS int)
WHERE  TrajanjeMin IS NULL;
PRINT N'Taskova sa upisanim trajanjem: ' + CAST(@@ROWCOUNT AS nvarchar(10));
GO
PRINT N'OP_024 završena.';
GO

/* Poništavanje (ručno, po potrebi):

ALTER TABLE planiranje.OP_Task DROP COLUMN TrajanjeMin;
DELETE planiranje.OP_Parametri WHERE Kljuc IN (N'TASK_OBRADA_MIN', N'BRZINA_VOZA_KMH');
*/
