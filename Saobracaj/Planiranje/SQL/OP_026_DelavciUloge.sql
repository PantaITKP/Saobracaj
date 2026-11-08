/* =====================================================================
   OP_026_DelavciUloge.sql
   Uloge zaposlenih prema radnom mestu.

   - Dodaje kolonu dbo.Delavci.MasinovodjaManevra (tinyint, podrazumevano 0).
   - Za sve zaposlene (bez obzira na DeSifStat) upisuje ulogu
     prema radnom mestu. Radno mesto je Delavci.DeSifDelMes = DelovnaMesta.DmSifra:
          7  MANEVRISTA                  -> Manevrista = 1
          8  VOZOVOĐA                    -> Vozovodja = 1
          9  MAŠINOVOĐA OTVORENE PRUGE   -> Masinovodja = 1
         10  MAŠINOVOĐA NA MANEVRI       -> MasinovodjaManevra = 1
         19  MAŠINOVOĐA                  -> Masinovodja = 1
         22  PREGLEDAČ KOLA              -> Pregledac = 1 i PregledacKola = 1
     (Planiranje čita PregledacKola, pa se upisuju obe kolone pregledača.)

   - Promenljive su na početku DRUGOG dela (posle prvog GO):
       @Potvrdi = 0  samo prikazuje šta bi se promenilo (podrazumevano),
       @Potvrdi = 1  upisuje.
       @Prepisi = 0  samo postavlja 1 za ulogu radnog mesta; ostale uloge ostaju
                     kakve jesu (i one unete ručno na ekranu Osoblje),
       @Prepisi = 1  sve uloge se postavljaju samo prema radnom mestu (ostale 0).
   - Menja samo dbo.Delavci. Može da se pokrene više puta.
   - Pokreće se ručno, na bazi Perftech_Beograd. Pre upisa napraviti rezervnu kopiju.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('dbo.Delavci', 'MasinovodjaManevra') IS NULL
BEGIN
    ALTER TABLE dbo.Delavci ADD MasinovodjaManevra tinyint NULL
        CONSTRAINT DF_Delavci_MasinovodjaManevra DEFAULT (0) WITH VALUES;
    PRINT N'Dodata kolona dbo.Delavci.MasinovodjaManevra.';
END
GO

DECLARE @Potvrdi bit, @Prepisi bit;
SET @Potvrdi = 0;
SET @Prepisi = 0;

-- Radna mesta iz šifarnika, radi provere da šifre odgovaraju nazivima.
SELECT m.DmSifra, RTRIM(m.DmNaziv) AS RadnoMesto,
       CASE m.DmSifra WHEN 7 THEN N'Manevrista' WHEN 8 THEN N'Vozovodja' WHEN 9 THEN N'Masinovodja' WHEN 10 THEN N'MasinovodjaManevra'
                      WHEN 19 THEN N'Masinovodja' WHEN 22 THEN N'Pregledac, PregledacKola' END AS Uloga,
       (SELECT COUNT(*) FROM dbo.Delavci d WHERE d.DeSifDelMes = m.DmSifra) AS Zaposlenih
FROM   dbo.DelovnaMesta m
WHERE  m.DmSifra IN (7, 8, 9, 10, 19, 22)
ORDER BY m.DmSifra;

IF OBJECT_ID('tempdb..#Promene') IS NOT NULL DROP TABLE #Promene;
SELECT d.DeSifra,
       RTRIM(d.DePriimek) + ' ' + RTRIM(d.DeIme) AS Ime,
       d.DeSifDelMes AS RadnoMesto,
       d.Masinovodja AS MasinovodjaBilo, d.MasinovodjaManevra AS MasinovodjaManevraBilo, d.Vozovodja AS VozovodjaBilo,
       d.Manevrista AS ManevristaBilo, d.Pregledac AS PregledacBilo, d.PregledacKola AS PregledacKolaBilo,
       CAST(CASE WHEN d.DeSifDelMes IN (9, 19) THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.Masinovodja END AS tinyint) AS Masinovodja,
       CAST(CASE WHEN d.DeSifDelMes = 10 THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.MasinovodjaManevra END AS tinyint) AS MasinovodjaManevra,
       CAST(CASE WHEN d.DeSifDelMes = 8 THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.Vozovodja END AS tinyint) AS Vozovodja,
       CAST(CASE WHEN d.DeSifDelMes = 7 THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.Manevrista END AS tinyint) AS Manevrista,
       CAST(CASE WHEN d.DeSifDelMes = 22 THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.Pregledac END AS tinyint) AS Pregledac,
       CAST(CASE WHEN d.DeSifDelMes = 22 THEN 1 WHEN @Prepisi = 1 THEN 0 ELSE d.PregledacKola END AS tinyint) AS PregledacKola
INTO   #Promene
FROM   dbo.Delavci d
WHERE  d.DeSifDelMes IN (7, 8, 9, 10, 19, 22) OR @Prepisi = 1;

DELETE #Promene
WHERE  ISNULL(Masinovodja, 255) = ISNULL(MasinovodjaBilo, 255)
  AND  ISNULL(MasinovodjaManevra, 255) = ISNULL(MasinovodjaManevraBilo, 255)
  AND  ISNULL(Vozovodja, 255) = ISNULL(VozovodjaBilo, 255)
  AND  ISNULL(Manevrista, 255) = ISNULL(ManevristaBilo, 255)
  AND  ISNULL(Pregledac, 255) = ISNULL(PregledacBilo, 255)
  AND  ISNULL(PregledacKola, 255) = ISNULL(PregledacKolaBilo, 255);

-- Zaposleni kojima se uloga menja (staro → novo).
SELECT DeSifra, Ime, RadnoMesto,
       MasinovodjaBilo, Masinovodja, MasinovodjaManevraBilo, MasinovodjaManevra, VozovodjaBilo, Vozovodja,
       ManevristaBilo, Manevrista, PregledacBilo, Pregledac, PregledacKolaBilo, PregledacKola
FROM   #Promene ORDER BY RadnoMesto, Ime;

IF @Potvrdi = 1
BEGIN
    BEGIN TRANSACTION;
    UPDATE d
    SET    d.Masinovodja = p.Masinovodja, d.MasinovodjaManevra = p.MasinovodjaManevra, d.Vozovodja = p.Vozovodja,
           d.Manevrista = p.Manevrista, d.Pregledac = p.Pregledac, d.PregledacKola = p.PregledacKola
    FROM   dbo.Delavci d
    JOIN   #Promene p ON p.DeSifra = d.DeSifra;
    PRINT N'Ažurirano zaposlenih: ' + CAST(@@ROWCOUNT AS nvarchar(10));
    COMMIT TRANSACTION;
END
ELSE
BEGIN
    DECLARE @Broj int;
    SELECT @Broj = COUNT(*) FROM #Promene;
    PRINT N'Samo pregled: za upis postaviti @Potvrdi = 1. Zaposlenih za promenu: ' + CAST(@Broj AS nvarchar(10));
END

DROP TABLE #Promene;
GO

/* Poništavanje kolone (ručno, po potrebi; upisane uloge ostaju):

ALTER TABLE dbo.Delavci DROP CONSTRAINT DF_Delavci_MasinovodjaManevra;
ALTER TABLE dbo.Delavci DROP COLUMN MasinovodjaManevra;
*/
