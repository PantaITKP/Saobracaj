/* =====================================================================
   OP_016_SpajanjeStanica.sql
   Stanice van mreže spajaju se sa mestom iz Registra istog ili sličnog naziva.

   - Spisak je proveren par po par: naziv se poklapa i stanica je blizu, ili
     stanica nema koordinate, a naziv je jasno isti. Mesto mora biti u mreži.
   - Spojena stanica ostaje sa Aktivna = 0 i DuplikatOd, a reference prelaze na
     glavnu. Veza sa dbo.stanice (IzvorniID) ostaje.
   - Oznake iz Registra (KRAJ PRUGE, Km 2+330) nisu stanice, pa se gase.
   - Stanice van Srbije koje dbo.stanice vodi kao SRB dobijaju tačnu državu.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_015.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRAN;

/* ---------- 1. Spajanje ---------- */
DECLARE @s TABLE (Naziv nvarchar(100) COLLATE DATABASE_DEFAULT, Glavna nvarchar(100) COLLATE DATABASE_DEFAULT);
INSERT @s VALUES (N'Ćuprija', N'Rasputnica Ćuprija');
INSERT @s VALUES (N'Podbara', N'Rasputnica 3 Podbara');
INSERT @s VALUES (N'Zvornik Grad', N'Zvornik');
INSERT @s VALUES (N'Dražanj/Šepšin', N'Dražanj-Šepšin');
INSERT @s VALUES (N'Saraoci', N'Saraorci');
INSERT @s VALUES (N'Neradova', N'Neradovac');
INSERT @s VALUES (N'Ljuničevski most', N'Ljubičevski most');
INSERT @s VALUES (N'Rsavsko Jasenovo', N'Resavsko Jasenovo');
INSERT @s VALUES (N'Vasiljevac', N'Vasiljevac stajalište');
INSERT @s VALUES (N'Sikirica', N'Sikirica-Ratari');
INSERT @s VALUES (N'Bečmen', N'Jakovo-Bečmen');
INSERT @s VALUES (N'odv. skr. 1-3 Niš', N'Odv. skr. 1 - 3 Niš');
INSERT @s VALUES (N'Odb. Skr. 28 Mala Krsna', N'Odv. skr. 28 Mala Krsna');
INSERT @s VALUES (N'Rasputnica ''''G''''', N'Rasputnica G');
INSERT @s VALUES (N'Rasputnica ''''A''''', N'Rasputnica A');
INSERT @s VALUES (N'Brevnik', N'Brvenik');
INSERT @s VALUES (N'Kalenić', N'Kalenići');
INSERT @s VALUES (N'Trnjane', N'Trnjani');
INSERT @s VALUES (N'Jarinje', N'Donje Jarinje');
INSERT @s VALUES (N'Podrinjsko Novo Selo', N'Podrinsko Novo Selo');
INSERT @s VALUES (N'Bački Petrovac - Gložan', N'Petrovac-Gložan');
INSERT @s VALUES (N'Rasputnica Sajlovo', N'Sajlovo');
INSERT @s VALUES (N'Smederevo luka', N'Smederevo');
INSERT @s VALUES (N'Pančevo', N'Pančevo glavna');
INSERT @s VALUES (N'TPS Novi Sad', N'Novi Sad lokoteretna');
INSERT @s VALUES (N'Vrbas Nova', N'Vrbas');
INSERT @s VALUES (N'Lovćenac-Mali Iđoš', N'Lovćenac');
INSERT @s VALUES (N'Kosovska Mitrovica', N'Kosovska Mitrovica Sever');

-- Isti par može biti upisan samo ako glavna postoji jednom; ostali se ispisuju.
UPDATE d SET Aktivna = 0, DuplikatOd = g.ID, DatumAzuriranja = SYSDATETIME()
FROM planiranje.OP_Stanice d
JOIN @s x ON x.Naziv = LTRIM(RTRIM(d.Naziv))
JOIN planiranje.OP_Stanice g ON g.Naziv = x.Glavna AND g.RegKljuc IS NOT NULL AND g.DuplikatOd IS NULL
WHERE d.RegKljuc IS NULL AND d.DuplikatOd IS NULL AND d.ID <> g.ID
  AND (SELECT COUNT(*) FROM planiranje.OP_Stanice z WHERE z.Naziv = x.Glavna AND z.RegKljuc IS NOT NULL AND z.DuplikatOd IS NULL) = 1;
PRINT N'Spojene stanice: ' + CAST(@@ROWCOUNT AS nvarchar(10));

/* ---------- 2. Reference prelaze na glavnu stanicu (isto kao OP_010) ---------- */
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

/* ---------- 3. Oznake iz Registra nisu stanice ---------- */
UPDATE planiranje.OP_Stanice SET Aktivna = 0, DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Aktivna = 1 AND LTRIM(RTRIM(Naziv)) IN (N'KRAJ PRUGE', N'POČETAK PRUGE', N'Km 102+000 Pb', N'Km 35+187 Pb', N'Km 2+330');
PRINT N'Ugašene oznake: ' + CAST(@@ROWCOUNT AS nvarchar(10));

/* ---------- 4. Stanice van Srbije ---------- */
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Virpazar';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Golubovci';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Danilovgrad';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Mojkovac';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Trebaljevo';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Trebješnica';
UPDATE planiranje.OP_Stanice SET Drzava = N'MNE', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Kruševo';
UPDATE planiranje.OP_Stanice SET Drzava = N'RO', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Zombolj';
UPDATE planiranje.OP_Stanice SET Drzava = N'BA', DatumAzuriranja = SYSDATETIME() WHERE RegKljuc IS NULL AND Drzava = N'SRB' AND LTRIM(RTRIM(Naziv)) = N'Dvorovi';

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
-- Aktivne SRB stanice koje su i dalje van mreže: u izboru su, ali nemaju rutu.
SELECT s.ID, s.Naziv AS SRB_van_mreze, s.Latitude, s.Longitude, s.Izvor
FROM planiranje.OP_Stanice s
WHERE s.Aktivna = 1 AND s.Drzava = N'SRB' AND NOT EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza v WHERE v.StanicaAID = s.ID OR v.StanicaBID = s.ID)
ORDER BY s.Naziv;
GO
PRINT N'OP_016 završena.';
GO
