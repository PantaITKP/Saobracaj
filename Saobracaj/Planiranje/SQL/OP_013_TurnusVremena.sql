/* =====================================================================
   OP_013_TurnusVremena.sql
   Turnusi: dnevna smena 07-19 umesto 06-18, noćna 19-07 umesto 18-06.

   - Menja početak i naziv turnusa od 12 sati koji počinje u 06:00 ili 18:00.
     Turnusi sa drugim početkom ostaju.
   - Smene napravljene iz turnusa (Izvor = TURNUS) pomera za sat vremena.
     Ručno unete smene ostaju.
   - Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_012.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRAN;

UPDATE planiranje.OP_Turnus
SET    Pocetak = '07:00', Naziv = REPLACE(REPLACE(Naziv, N'06–18', N'07–19'), N'06-18', N'07-19')
WHERE  Pocetak = '06:00' AND TrajanjeMin = 720 AND Nocna = 0;
PRINT N'Dnevnih turnusa: ' + CAST(@@ROWCOUNT AS nvarchar(10));

UPDATE planiranje.OP_Turnus
SET    Pocetak = '19:00', Naziv = REPLACE(REPLACE(Naziv, N'18–06', N'19–07'), N'18-06', N'19-07')
WHERE  Pocetak = '18:00' AND TrajanjeMin = 720 AND Nocna = 1;
PRINT N'Noćnih turnusa: ' + CAST(@@ROWCOUNT AS nvarchar(10));

UPDATE planiranje.OP_Smena
SET    Od = DATEADD(hour, 1, Od), Do = DATEADD(hour, 1, Do)
WHERE  Izvor = N'TURNUS'
  AND  ((Vrsta = N'DNEVNA' AND CAST(Od AS time(0)) = '06:00' AND CAST(Do AS time(0)) = '18:00')
    OR  (Vrsta = N'NOCNA'  AND CAST(Od AS time(0)) = '18:00' AND CAST(Do AS time(0)) = '06:00'));
PRINT N'Smena iz turnusa: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT;
GO
PRINT N'OP_013 završena.';
GO
