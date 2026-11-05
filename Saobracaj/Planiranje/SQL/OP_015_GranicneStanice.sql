/* =====================================================================
   OP_015_GranicneStanice.sql
   Granična mesta dobijaju nazive koje planeri koriste (Bogojevo gr.), a
   stanice koje su isto mesto kao stavka Registra spajaju se sa njom.

   - Mesto „Državna granica (Erdut)“ postaje „Bogojevo gr.“, a naziv iz
     Registra prelazi u NazivStari, pa ga uvoz trasa i dalje prepoznaje.
   - Spojene stanice ostaju sa Aktivna = 0 i DuplikatOd, a reference prelaze
     na glavnu stanicu. Veza sa dbo.stanice (IzvorniID) ostaje.
   - Stanica „Nema“ iz dbo.stanice je oznaka da granica nije upisana, pa se gasi.
   - Mesta se nalaze po šifri (Kod), jer se ID razlikuje od baze do baze.
   - Pokrenuti ponovo ako se pušta OP_010, jer ona vraća nazive iz Registra.
   - Na kraju ispisuje stanice iz najava koje su i dalje van mreže.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_014.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRAN;

/* ---------- 1. Nazivi graničnih mesta ---------- */
DECLARE @g TABLE (Kod nvarchar(20) COLLATE DATABASE_DEFAULT, Naziv nvarchar(100) COLLATE DATABASE_DEFAULT);
INSERT @g VALUES (N'254714', N'Bogojevo gr.');
INSERT @g VALUES (N'165175', N'Šid gr.');
INSERT @g VALUES (N'234997', N'Subotica gr.');
INSERT @g VALUES (N'210997', N'Vršac gr.');
INSERT @g VALUES (N'124982', N'Dimitrovgrad gr.');
INSERT @g VALUES (N'110288', N'Preševo gr.');
INSERT @g VALUES (N'231993', N'Horgoš gr.');
INSERT @g VALUES (N'157230', N'Vrbnica gr.');
INSERT @g VALUES (N'228999', N'Banatsko Veliko Selo gr.');
INSERT @g VALUES (N'163196', N'Brasina gr.');

UPDATE s SET NazivStari = s.Naziv, Naziv = g.Naziv, DatumAzuriranja = SYSDATETIME()
FROM planiranje.OP_Stanice s JOIN @g g ON g.Kod = s.Kod
WHERE s.GranicniPrelaz = 1 AND s.Naziv <> g.Naziv;
PRINT N'Granična mesta: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- Granica na pruzi L14 nema šifru u Registru.
UPDATE planiranje.OP_Stanice SET NazivStari = Naziv, Naziv = N'Sremska Rača Nova gr.', DatumAzuriranja = SYSDATETIME()
WHERE GranicniPrelaz = 1 AND RegKljuc LIKE N'GR:L14:%' AND Naziv <> N'Sremska Rača Nova gr.';

/* ---------- 2. Isto mesto kao stavka Registra ---------- */
DECLARE @s TABLE (Naziv nvarchar(100) COLLATE DATABASE_DEFAULT, Kod nvarchar(20) COLLATE DATABASE_DEFAULT);
INSERT @s VALUES (N'Brasina gr.', N'163196');
INSERT @s VALUES (N'Banatsko Veliko selo gr.', N'228999');
INSERT @s VALUES (N'Subotica Teretna', N'234500');
INSERT @s VALUES (N'Kukujevci', N'165134');
INSERT @s VALUES (N'Jakovo', N'162057');

UPDATE d SET Aktivna = 0, DuplikatOd = g.ID, DatumAzuriranja = SYSDATETIME()
FROM planiranje.OP_Stanice d
JOIN @s x ON x.Naziv COLLATE Latin1_General_CI_AI = LTRIM(RTRIM(d.Naziv)) COLLATE Latin1_General_CI_AI
JOIN planiranje.OP_Stanice g ON g.Kod = x.Kod AND g.RegKljuc IS NOT NULL
WHERE d.RegKljuc IS NULL AND d.DuplikatOd IS NULL AND d.ID <> g.ID;
PRINT N'Spojene stanice: ' + CAST(@@ROWCOUNT AS nvarchar(10));

/* ---------- 3. Reference prelaze na glavnu stanicu (isto kao OP_010) ---------- */
UPDATE x SET MaticnaStanicaID = d.DuplikatOd FROM planiranje.OP_Lokomotive x JOIN planiranje.OP_Stanice d ON d.ID = x.MaticnaStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET MaticnaStanicaID = d.DuplikatOd FROM planiranje.OP_OsobljePlan x JOIN planiranje.OP_Stanice d ON d.ID = x.MaticnaStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaID = d.DuplikatOd FROM planiranje.OP_PrugaMesto x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET PocetnaStanicaID = d.DuplikatOd FROM planiranje.OP_Trase x JOIN planiranje.OP_Stanice d ON d.ID = x.PocetnaStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET KrajnjaStanicaID = d.DuplikatOd FROM planiranje.OP_Trase x JOIN planiranje.OP_Stanice d ON d.ID = x.KrajnjaStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaID = d.DuplikatOd FROM planiranje.OP_TrasaStanice x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaOdID = d.DuplikatOd FROM planiranje.OP_TablicaMasa x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaOdID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaDoID = d.DuplikatOd FROM planiranje.OP_TablicaMasa x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaDoID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET PrijemStanicaID = d.DuplikatOd FROM planiranje.OP_Posao x JOIN planiranje.OP_Stanice d ON d.ID = x.PrijemStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET PredajaStanicaID = d.DuplikatOd FROM planiranje.OP_Posao x JOIN planiranje.OP_Stanice d ON d.ID = x.PredajaStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaOdID = d.DuplikatOd FROM planiranje.OP_PosaoRuta x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaOdID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaDoID = d.DuplikatOd FROM planiranje.OP_PosaoRuta x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaDoID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET LokacijaPosleStanicaID = d.DuplikatOd FROM planiranje.OP_Smena x JOIN planiranje.OP_Stanice d ON d.ID = x.LokacijaPosleStanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaID = d.DuplikatOd FROM planiranje.OP_Zadatak x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaID WHERE d.DuplikatOd IS NOT NULL;
DELETE x FROM planiranje.OP_Parametri x
JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaID AND d.DuplikatOd IS NOT NULL
JOIN planiranje.OP_Parametri p ON p.Kljuc = x.Kljuc AND p.VaziOd = x.VaziOd AND p.StanicaID = d.DuplikatOd AND ISNULL(p.StanicaDoID, 0) = ISNULL(x.StanicaDoID, 0) AND ISNULL(p.SerijaID, 0) = ISNULL(x.SerijaID, 0);
UPDATE x SET StanicaID = d.DuplikatOd FROM planiranje.OP_Parametri x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaID WHERE d.DuplikatOd IS NOT NULL;
UPDATE x SET StanicaDoID = d.DuplikatOd FROM planiranje.OP_Parametri x JOIN planiranje.OP_Stanice d ON d.ID = x.StanicaDoID WHERE d.DuplikatOd IS NOT NULL;
-- Ručna veza na spojenoj stanici: petlja ili duplikat se briše, ostale prelaze na glavnu.
DELETE v FROM planiranje.OP_MrezaVeza v
JOIN planiranje.OP_Stanice a ON a.ID = v.StanicaAID JOIN planiranje.OP_Stanice b ON b.ID = v.StanicaBID
WHERE (a.DuplikatOd IS NOT NULL OR b.DuplikatOd IS NOT NULL)
  AND (COALESCE(a.DuplikatOd, a.ID) = COALESCE(b.DuplikatOd, b.ID)
    OR EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza x WHERE x.ID <> v.ID
               AND ((x.StanicaAID = COALESCE(a.DuplikatOd, a.ID) AND x.StanicaBID = COALESCE(b.DuplikatOd, b.ID))
                 OR (x.StanicaBID = COALESCE(a.DuplikatOd, a.ID) AND x.StanicaAID = COALESCE(b.DuplikatOd, b.ID)))));
UPDATE v SET StanicaAID = a.DuplikatOd FROM planiranje.OP_MrezaVeza v JOIN planiranje.OP_Stanice a ON a.ID = v.StanicaAID WHERE a.DuplikatOd IS NOT NULL;
UPDATE v SET StanicaBID = b.DuplikatOd FROM planiranje.OP_MrezaVeza v JOIN planiranje.OP_Stanice b ON b.ID = v.StanicaBID WHERE b.DuplikatOd IS NOT NULL;

/* ---------- 4. Oznaka „Nema“ nije stanica ---------- */
UPDATE planiranje.OP_Stanice SET Aktivna = 0, DatumAzuriranja = SYSDATETIME() WHERE LTRIM(RTRIM(Naziv)) = N'Nema' AND RegKljuc IS NULL AND Aktivna = 1;

COMMIT;
GO
/* ---------- 5. Izveštaj: merodavne stanice iz najava koje su i dalje van mreže ---------- */
SELECT g.Naziv AS Stanica_van_mreze, g.Drzava, COUNT(*) AS Najava
FROM (SELECT PlanOtpravnaID AS S FROM planiranje.OP_vNajava WHERE PredvidjenoPrimanje >= '20250101'
      UNION ALL SELECT PlanUputnaID FROM planiranje.OP_vNajava WHERE PredvidjenoPrimanje >= '20250101') n
JOIN planiranje.OP_Stanice o ON o.IzvorniID = n.S
JOIN planiranje.OP_Stanice g ON g.ID = COALESCE(o.DuplikatOd, o.ID)
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza v WHERE v.StanicaAID = g.ID OR v.StanicaBID = g.ID)
GROUP BY g.Naziv, g.Drzava
ORDER BY COUNT(*) DESC;
GO
PRINT N'OP_015 završena.';
GO
