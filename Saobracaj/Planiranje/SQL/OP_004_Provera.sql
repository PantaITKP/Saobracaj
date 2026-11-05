/* =====================================================================
   OP_004_Provera.sql
   Provera posle punjenja. Ništa ne menja, samo čita.
   Svaki upit ima naslov u prvoj koloni; prazan rezultat znači "u redu".
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
GO

-- 1. Broj redova: izvor prema OP tabeli
SELECT N'1. Brojevi' AS Provera, x.Tabela, x.Izvor, x.OP
FROM (
    SELECT N'Stanice' AS Tabela, (SELECT COUNT(*) FROM dbo.stanice) AS Izvor, (SELECT COUNT(*) FROM planiranje.OP_Stanice WHERE IzvorniID IS NOT NULL) AS OP
    UNION ALL SELECT N'Lokomotive (Mesta.Lokomotiva=1)', (SELECT COUNT(*) FROM dbo.Mesta WHERE Lokomotiva = 1), (SELECT COUNT(*) FROM planiranje.OP_Lokomotive WHERE IzvorniSmSifra IS NOT NULL)
    UNION ALL SELECT N'Pruge', (SELECT COUNT(*) FROM dbo.Pruga), (SELECT COUNT(*) FROM planiranje.OP_Pruga WHERE IzvorniID IS NOT NULL)
    UNION ALL SELECT N'Trase (OperativniPlanTrasa)', (SELECT COUNT(*) FROM dbo.OperativniPlanTrasa), (SELECT COUNT(*) FROM planiranje.OP_Trase WHERE IzvorniRouteID IS NOT NULL)
    UNION ALL SELECT N'Osoblje u planiranju (Delavci + OP_OsobljePlan)', NULL, (SELECT COUNT(*) FROM planiranje.OP_OsobljePlan WHERE UPlaniranju = 1)
    UNION ALL SELECT N'Mesta na pruzi (OP)', NULL, (SELECT COUNT(*) FROM planiranje.OP_PrugaMesto)
    UNION ALL SELECT N'Veze mreže (OP)', NULL, (SELECT COUNT(*) FROM planiranje.OP_MrezaVeza)
) x;

-- 2. Lokomotive bez serije (serija se dodeljuje u formi Lokomotive)
SELECT N'2. Lokomotiva bez serije' AS Provera, ID, Broj, Naziv FROM planiranje.OP_Lokomotive WHERE SerijaID IS NULL ORDER BY Broj;

-- 3. Serije bez vrste vuče
SELECT N'3. Serija bez vrste vuče' AS Provera, ID, Oznaka, Opis FROM planiranje.OP_SerijeLokomotiva WHERE VrstaVuce IS NULL;

-- 4. Trase sa greškom (nepoznata stanica, nema vremena)
SELECT N'4. Trasa sa greškom' AS Provera, t.ID, v.Naziv AS Verzija, t.BrojVoza, t.PocetnaNaziv, t.KrajnjaNaziv, t.Greska
FROM planiranje.OP_Trase t JOIN planiranje.OP_TraseVerzija v ON v.ID = t.VerzijaID
WHERE t.Greska IS NOT NULL AND v.Status = N'VAZECA'
ORDER BY t.BrojVoza;

-- 5. Nazivi stanica iz trasa koji nisu prepoznati (za ručno povezivanje)
SELECT N'5. Nepoznat naziv stanice u trasama' AS Provera, x.Naziv, COUNT(*) AS BrojTrasa
FROM (
    SELECT PocetnaNaziv AS Naziv FROM planiranje.OP_Trase WHERE PocetnaStanicaID IS NULL
    UNION ALL SELECT KrajnjaNaziv FROM planiranje.OP_Trase WHERE KrajnjaStanicaID IS NULL
) x GROUP BY x.Naziv ORDER BY COUNT(*) DESC;

-- 6. Duplikati trasa u važećoj verziji (isti broj voza i relacija)
SELECT N'6. Duplikat trase' AS Provera, t.VerzijaID, t.BrojVoza, t.PocetnaNaziv, t.KrajnjaNaziv, COUNT(*) AS Broj
FROM planiranje.OP_Trase t JOIN planiranje.OP_TraseVerzija v ON v.ID = t.VerzijaID AND v.Status = N'VAZECA'
GROUP BY t.VerzijaID, t.BrojVoza, t.PocetnaNaziv, t.KrajnjaNaziv HAVING COUNT(*) > 1;

-- 7. Više važećih verzija za istu godinu (ne bi smelo, štiti indeks)
SELECT N'7. Više važećih verzija' AS Provera, Godina, COUNT(*) FROM planiranje.OP_TraseVerzija WHERE Status = N'VAZECA' GROUP BY Godina HAVING COUNT(*) > 1;

-- 8. Mesta na pruzi bez stanice
SELECT N'8. Mesto na pruzi bez stanice' AS Provera, p.Naziv AS Pruga, pm.RB, pm.Naziv
FROM planiranje.OP_PrugaMesto pm JOIN planiranje.OP_Pruga p ON p.ID = pm.PrugaID
WHERE pm.StanicaID IS NULL ORDER BY p.Naziv, pm.RB;

-- 9. Stanice bez UIC šifre koje se nalaze na nekoj pruzi
SELECT DISTINCT N'9. Stanica na pruzi bez UIC' AS Provera, s.ID, s.Naziv
FROM planiranje.OP_Stanice s JOIN planiranje.OP_PrugaMesto pm ON pm.StanicaID = s.ID
WHERE s.UIC IS NULL ORDER BY s.Naziv;

-- 10. UIC šifra koja se ponavlja (moguća greška u Kod -> UIC)
SELECT N'10. Ponovljen UIC' AS Provera, s.UIC, COUNT(*) AS Broj,
       STUFF((SELECT N', ' + x.Naziv FROM planiranje.OP_Stanice x WHERE x.UIC = s.UIC FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS Stanice
FROM planiranje.OP_Stanice s WHERE s.UIC IS NOT NULL GROUP BY s.UIC HAVING COUNT(*) > 1;

-- 11. Stanice bez koordinata (prazna vožnja ne može da se izračuna)
SELECT N'11. Stanica bez koordinata' AS Provera, s.ID, s.Naziv, s.Izvor
FROM planiranje.OP_Stanice s
WHERE (s.Latitude IS NULL OR s.Longitude IS NULL)
  AND EXISTS (SELECT 1 FROM planiranje.OP_Trase t WHERE t.PocetnaStanicaID = s.ID OR t.KrajnjaStanicaID = s.ID)
ORDER BY s.Naziv;

-- 12. Mašinovođe bez kvalifikacije
SELECT N'12. Mašinovođa bez kvalifikacije' AS Provera, o.DeSifra, o.Naziv,
       (SELECT COUNT(*) FROM planiranje.OP_OsobljeLokomotiva k WHERE k.DeSifra = o.DeSifra) AS Lokomotiva,
       (SELECT COUNT(*) FROM planiranje.OP_OsobljePruga k WHERE k.DeSifra = o.DeSifra) AS Pruga
FROM planiranje.OP_vOsoblje o
WHERE o.Masinovodja = 1 AND o.Aktivan = 1 AND o.UPlaniranju = 1
  AND (NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeLokomotiva k WHERE k.DeSifra = o.DeSifra)
    OR NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePruga k WHERE k.DeSifra = o.DeSifra))
ORDER BY o.Naziv;

-- 13. Osoblje bez matične lokacije
SELECT N'13. Osoba bez matične lokacije' AS Provera, DeSifra, Naziv FROM planiranje.OP_vOsoblje WHERE MaticnaStanicaID IS NULL AND Aktivan = 1 AND UPlaniranju = 1 ORDER BY Naziv;

-- 14. Ostrva u mreži: stanice iz trasa bez ijedne veze
SELECT DISTINCT N'14. Stanica iz trase bez veze u mreži' AS Provera, s.ID, s.Naziv
FROM planiranje.OP_Stanice s
WHERE EXISTS (SELECT 1 FROM planiranje.OP_Trase t WHERE t.PocetnaStanicaID = s.ID OR t.KrajnjaStanicaID = s.ID)
  AND NOT EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza v WHERE v.StanicaAID = s.ID OR v.StanicaBID = s.ID)
ORDER BY s.Naziv;
GO
