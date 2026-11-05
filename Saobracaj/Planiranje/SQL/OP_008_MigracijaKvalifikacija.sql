/* =====================================================================
   OP_008_MigracijaKvalifikacija.sql
   Jednokratan prenos kvalifikacija u nove tabele (posle OP_007).

   Izvori:
     planiranje.OP_OsobljeKvalifikacija  PRUGA ide u OP_OsobljePruga,
                                         SERIJA se širi na sve aktivne lokomotive te serije;
     dbo.DelavciPrugaMasin               u OP_OsobljePruga, preko OP_Pruga.IzvorniID;
     dbo.DelavciLokMasin                 u OP_OsobljeLokomotiva, preko OP_Lokomotive.IzvorniSmSifra.

   - Izvorne tabele se ne menjaju i ne brišu.
   - Prenosi se i osoba koja nema čekiranu ulogu: videće se u matrici kad joj se uloga čekira.
   - Osoba bez reda u OP_OsobljePlan dobija red sa UPlaniranju = 0.
   - Postojeći redovi u novim tabelama se ne menjaju, pa se skripta može ponoviti.
   - Na kraju ispisuje stare redove za koje nije nađen par.
   - Pokreće se ručno, na bazi Perftech_Beograd.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'planiranje.OP_OsobljePruga', N'U') IS NULL OR OBJECT_ID(N'planiranje.OP_OsobljeLokomotiva', N'U') IS NULL
BEGIN
    RAISERROR(N'Prvo pokrenuti OP_007_Osoblje.sql. Ništa nije preneto.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

INSERT planiranje.OP_OsobljePlan (DeSifra, UPlaniranju, Izmenio)
SELECT x.DeSifra, 0, N'OP_008'
FROM (SELECT DeSifra FROM dbo.DelavciPrugaMasin UNION SELECT DeSifra FROM dbo.DelavciLokMasin) x
JOIN dbo.Delavci d ON d.DeSifra = x.DeSifra
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePlan p WHERE p.DeSifra = x.DeSifra);
PRINT N'Novih redova u OP_OsobljePlan: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- Prvo nova kvalifikacija, jer jedina ima rok važenja. Ako ima više redova, trajan je jači od roka.
INSERT planiranje.OP_OsobljePruga (DeSifra, PrugaID, VaziDo, Napomena)
SELECT k.DeSifra, k.PrugaID, CASE WHEN COUNT(k.VaziDo) = COUNT(*) THEN MAX(k.VaziDo) END, MAX(k.Napomena)
FROM planiranje.OP_OsobljeKvalifikacija k
WHERE k.Vrsta = N'PRUGA'
  AND NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePruga n WHERE n.DeSifra = k.DeSifra AND n.PrugaID = k.PrugaID)
GROUP BY k.DeSifra, k.PrugaID;
PRINT N'Pruge iz OP_OsobljeKvalifikacija: ' + CAST(@@ROWCOUNT AS nvarchar(10));

INSERT planiranje.OP_OsobljeLokomotiva (DeSifra, LokomotivaID, VaziDo, Napomena)
SELECT k.DeSifra, l.ID, CASE WHEN COUNT(k.VaziDo) = COUNT(*) THEN MAX(k.VaziDo) END, MAX(k.Napomena)
FROM planiranje.OP_OsobljeKvalifikacija k
JOIN planiranje.OP_Lokomotive l ON l.SerijaID = k.SerijaID AND l.Aktivna = 1
WHERE k.Vrsta = N'SERIJA'
  AND NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeLokomotiva n WHERE n.DeSifra = k.DeSifra AND n.LokomotivaID = l.ID)
GROUP BY k.DeSifra, l.ID;
PRINT N'Lokomotive iz serija u OP_OsobljeKvalifikacija: ' + CAST(@@ROWCOUNT AS nvarchar(10));

INSERT planiranje.OP_OsobljePruga (DeSifra, PrugaID, Napomena)
SELECT DISTINCT d.DeSifra, p.ID, N'Migracija iz DelavciPrugaMasin'
FROM dbo.DelavciPrugaMasin d
JOIN planiranje.OP_OsobljePlan o ON o.DeSifra = d.DeSifra
JOIN planiranje.OP_Pruga p ON p.IzvorniID = d.Pruga
JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID AND v.Status = N'VAZECA'
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePruga n WHERE n.DeSifra = d.DeSifra AND n.PrugaID = p.ID);
PRINT N'Pruge iz DelavciPrugaMasin: ' + CAST(@@ROWCOUNT AS nvarchar(10));

INSERT planiranje.OP_OsobljeLokomotiva (DeSifra, LokomotivaID, Napomena)
SELECT DISTINCT d.DeSifra, l.ID, N'Migracija iz DelavciLokMasin'
FROM dbo.DelavciLokMasin d
JOIN planiranje.OP_OsobljePlan o ON o.DeSifra = d.DeSifra
JOIN planiranje.OP_Lokomotive l ON RTRIM(l.IzvorniSmSifra) = RTRIM(d.SMSifra) COLLATE DATABASE_DEFAULT
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeLokomotiva n WHERE n.DeSifra = d.DeSifra AND n.LokomotivaID = l.ID);
PRINT N'Lokomotive iz DelavciLokMasin: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-- Stari redovi bez para u novim šifarnicima: treba ih uneti ručno u matrici.
SELECT N'Pruga bez para' AS Stavka, d.DeSifra, CAST(d.Pruga AS nvarchar(20)) AS Sifra
FROM dbo.DelavciPrugaMasin d
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Pruga p JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID AND v.Status = N'VAZECA' WHERE p.IzvorniID = d.Pruga)
UNION ALL
SELECT N'Lokomotiva bez para', d.DeSifra, CAST(RTRIM(d.SMSifra) AS nvarchar(20)) COLLATE DATABASE_DEFAULT
FROM dbo.DelavciLokMasin d
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Lokomotive l WHERE RTRIM(l.IzvorniSmSifra) = RTRIM(d.SMSifra) COLLATE DATABASE_DEFAULT);

SELECT (SELECT COUNT(*) FROM planiranje.OP_OsobljePruga) AS Pruge, (SELECT COUNT(*) FROM planiranje.OP_OsobljeLokomotiva) AS Lokomotive;
GO
