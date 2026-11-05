/* =====================================================================
   OP_019_Elektrifikacija.sql
   Elektrifikacija pruga po zvaničnom spisku: Prilog 3.4 Elektrificirane pruge.

   - Pruga sa spiska je elektrificirana cela, a pruga van spiska nije.
   - Delimično elektrificirane su M5, M6, R5, R11 i R15: elektrificirane su samo
     navedene deonice, a na samoj pruzi je NULL, jer o njima odlučuje deonica.
   - Stavka 2 obuhvata i M11 (Beograd Centar - Rasputnica G - Rakovica).
   - Stavka 4 (Jagodina - Ćuprija - Paraćin) je u važećem Registru deo pruge M2.
   - Spisak važi i preko OSM podatka na deonici. Deonica posle ove skripte više
     nema praznu elektrifikaciju, pa se vrednost sa pruge više ne koristi.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_018.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
DECLARE @Cele TABLE (Sifra nvarchar(6) PRIMARY KEY);
INSERT @Cele (Sifra)
SELECT N'M1' UNION ALL SELECT N'M2' UNION ALL SELECT N'M3' UNION ALL SELECT N'M4' UNION ALL SELECT N'M7' UNION ALL SELECT N'M11' UNION ALL SELECT N'M12' UNION ALL SELECT N'M13' UNION ALL SELECT N'M14' UNION ALL SELECT N'M15' UNION ALL SELECT N'M16' UNION ALL SELECT N'M17' UNION ALL SELECT N'M18' UNION ALL SELECT N'M19' UNION ALL SELECT N'M20' UNION ALL SELECT N'M24' UNION ALL SELECT N'M25' UNION ALL SELECT N'M26' UNION ALL SELECT N'M27' UNION ALL SELECT N'M28' UNION ALL SELECT N'M29' UNION ALL SELECT N'M30' UNION ALL SELECT N'M31' UNION ALL SELECT N'R1' UNION ALL SELECT N'R13' UNION ALL SELECT N'R14' UNION ALL SELECT N'L18';

-- Do = NULL znači do kraja pruge; deonica ulazi od stanice Od do stanice Do.
DECLARE @Delovi TABLE (Sifra nvarchar(6) PRIMARY KEY, OdNaziv nvarchar(100) NOT NULL, DoNaziv nvarchar(100) NULL, OcekivanoDeonica int NOT NULL);
INSERT @Delovi VALUES (N'M5', N'Dimitrovgrad', NULL, 1);
INSERT @Delovi VALUES (N'M6', N'Beograd Centar', N'Pančevo varoš', 9);
INSERT @Delovi VALUES (N'R5', N'Novi Sad', N'Sajlovo', 2);
INSERT @Delovi VALUES (N'R11', N'Kraljevo', NULL, 19);
INSERT @Delovi VALUES (N'R15', N'Mala Krsna', N'Požarevac', 2);

DECLARE @Vazece TABLE (ID int PRIMARY KEY, Sifra nvarchar(6) NOT NULL);
INSERT @Vazece SELECT p.ID, p.Sifra FROM planiranje.OP_Pruga p JOIN planiranje.OP_RegistarVerzija r ON r.ID = p.RegistarVerzijaID WHERE r.Status = N'VAZECA' AND p.Sifra IS NOT NULL;

IF EXISTS (SELECT Sifra FROM @Cele WHERE Sifra NOT IN (SELECT Sifra FROM @Vazece) UNION ALL SELECT Sifra FROM @Delovi WHERE Sifra NOT IN (SELECT Sifra FROM @Vazece))
BEGIN
    RAISERROR(N'Pruga sa spiska nije u važećem Registru. Ništa nije promenjeno.', 16, 1);
    RETURN;
END

-- Redni broj deonice je prvo mesto na pruzi; granice su mesta navedenih stanica.
DECLARE @Deonice TABLE (VezaID int PRIMARY KEY, Sifra nvarchar(6) NOT NULL);
INSERT @Deonice
SELECT v.ID, d.Sifra
FROM   planiranje.OP_MrezaVeza v
JOIN   @Vazece p ON p.ID = v.PrugaID
JOIN   @Delovi d ON d.Sifra = p.Sifra
WHERE  (SELECT MIN(m.RB) FROM planiranje.OP_PrugaMesto m WHERE m.PrugaID = v.PrugaID AND m.StanicaID IN (v.StanicaAID, v.StanicaBID))
       >= (SELECT MIN(m.RB) FROM planiranje.OP_PrugaMesto m JOIN planiranje.OP_Stanice s ON s.ID = m.StanicaID WHERE m.PrugaID = v.PrugaID AND s.Naziv = d.OdNaziv)
  AND  (d.DoNaziv IS NULL OR (SELECT MIN(m.RB) FROM planiranje.OP_PrugaMesto m WHERE m.PrugaID = v.PrugaID AND m.StanicaID IN (v.StanicaAID, v.StanicaBID))
       < (SELECT MIN(m.RB) FROM planiranje.OP_PrugaMesto m JOIN planiranje.OP_Stanice s ON s.ID = m.StanicaID WHERE m.PrugaID = v.PrugaID AND s.Naziv = d.DoNaziv));

IF EXISTS (SELECT 1 FROM @Delovi d WHERE d.OcekivanoDeonica <> (SELECT COUNT(*) FROM @Deonice x WHERE x.Sifra = d.Sifra))
BEGIN
    SELECT d.Sifra, d.OcekivanoDeonica, (SELECT COUNT(*) FROM @Deonice x WHERE x.Sifra = d.Sifra) AS Nadjeno FROM @Delovi d;
    RAISERROR(N'Broj deonica delimično elektrificirane pruge nije očekivan. Ništa nije promenjeno.', 16, 1);
    RETURN;
END

BEGIN TRAN;
-- Šta se menja, pre upisa: ostaje kao zapis u izlazu skripte.
SELECT p.Sifra, COUNT(*) AS Promenjenih, CAST(SUM(v.DuzinaKm) AS decimal(9,1)) AS Km,
       SUM(CASE WHEN n.Nova = 1 THEN 1 ELSE 0 END) AS PostajeDa, SUM(CASE WHEN n.Nova = 0 THEN 1 ELSE 0 END) AS PostajeNe
FROM   planiranje.OP_MrezaVeza v
JOIN   @Vazece p ON p.ID = v.PrugaID
JOIN   (SELECT v2.ID, CASE WHEN c.Sifra IS NOT NULL OR x.VezaID IS NOT NULL THEN 1 ELSE 0 END AS Nova
        FROM planiranje.OP_MrezaVeza v2 JOIN @Vazece p2 ON p2.ID = v2.PrugaID LEFT JOIN @Cele c ON c.Sifra = p2.Sifra LEFT JOIN @Deonice x ON x.VezaID = v2.ID) n ON n.ID = v.ID
WHERE  v.Elektrificirana IS NULL OR CASE WHEN v.Elektrificirana = 1 THEN 1 ELSE 0 END <> n.Nova
GROUP BY p.Sifra ORDER BY p.Sifra;

UPDATE v
SET    Elektrificirana = CASE WHEN c.Sifra IS NOT NULL OR x.VezaID IS NOT NULL THEN 1 ELSE 0 END
FROM   planiranje.OP_MrezaVeza v
JOIN   @Vazece p ON p.ID = v.PrugaID
LEFT JOIN @Cele c ON c.Sifra = p.Sifra
LEFT JOIN @Deonice x ON x.VezaID = v.ID;
PRINT N'Deonica upisano: ' + CAST(@@ROWCOUNT AS nvarchar(10));

UPDATE p
SET    Elektrificirana = CASE WHEN d.Sifra IS NOT NULL THEN NULL WHEN c.Sifra IS NOT NULL THEN 1 ELSE 0 END
FROM   planiranje.OP_Pruga p
JOIN   @Vazece z ON z.ID = p.ID
LEFT JOIN @Cele c ON c.Sifra = z.Sifra
LEFT JOIN @Delovi d ON d.Sifra = z.Sifra;
PRINT N'Pruga upisano: ' + CAST(@@ROWCOUNT AS nvarchar(10));
COMMIT;
GO
SELECT v.Elektrificirana, COUNT(*) AS Deonica, CAST(SUM(v.DuzinaKm) AS decimal(9,1)) AS Km FROM planiranje.OP_MrezaVeza v WHERE v.Aktivna = 1 GROUP BY v.Elektrificirana;
GO
PRINT N'OP_019 završena.';
GO
