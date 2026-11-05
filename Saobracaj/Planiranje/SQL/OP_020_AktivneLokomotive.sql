/* =====================================================================
   OP_020_AktivneLokomotive.sql
   Aktivne lokomotive po spisku korisnika (28 lokomotiva) i serije 186 i 193.

   - Aktivne su lokomotive sa spiska i sve lokomotive serija 186 i 193.
     Ostale postaju neaktivne: ne brišu se, a njihova istorija i kvalifikacije ostaju.
   - Serije su prve tri cifre broja. Dodaju se serije 186 i 383, a lokomotive
     383 216 - 383 220 dobijaju seriju 383.
   - Lokomotiva serije 186 još nema u bazi, pa se dodaje samo serija.
   - Broj lokomotive se ne menja. Spisak ga poredi sa brojem iz baze:
     na primer 646 001 je 6460010, jer je poslednja cifra kontrolna.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_019.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
DECLARE @Spisak TABLE (Oznaka nvarchar(10) NOT NULL, Broj nvarchar(20) NOT NULL PRIMARY KEY, Serija nvarchar(20) NOT NULL);
INSERT @Spisak VALUES (N'185 681', N'185 681', N'185');
INSERT @Spisak VALUES (N'185 691', N'185.691', N'185');
INSERT @Spisak VALUES (N'185 717', N'185.717', N'185');
INSERT @Spisak VALUES (N'383 216', N'383216', N'383');
INSERT @Spisak VALUES (N'383 217', N'383217', N'383');
INSERT @Spisak VALUES (N'383 218', N'383218', N'383');
INSERT @Spisak VALUES (N'383 219', N'383219', N'383');
INSERT @Spisak VALUES (N'383 220', N'383220', N'383');
INSERT @Spisak VALUES (N'441 513', N'4415130', N'441');
INSERT @Spisak VALUES (N'646 001', N'6460010', N'646');
INSERT @Spisak VALUES (N'646 002', N'6460024', N'646');
INSERT @Spisak VALUES (N'647 001', N'6470019', N'647');
INSERT @Spisak VALUES (N'647 004', N'6470043', N'647');
INSERT @Spisak VALUES (N'647 005', N'6470050', N'647');
INSERT @Spisak VALUES (N'647 008', N'647008', N'647');
INSERT @Spisak VALUES (N'647 009', N'647009', N'647');
INSERT @Spisak VALUES (N'647 101', N'647101', N'647');
INSERT @Spisak VALUES (N'647 102', N'647102', N'647');
INSERT @Spisak VALUES (N'647 103', N'647103', N'647');
INSERT @Spisak VALUES (N'647 104', N'647104', N'647');
INSERT @Spisak VALUES (N'648 100', N'648100', N'648');
INSERT @Spisak VALUES (N'648 110', N'648110', N'648');
INSERT @Spisak VALUES (N'648 111', N'648111', N'648');
INSERT @Spisak VALUES (N'648 200', N'648200', N'648');
INSERT @Spisak VALUES (N'648 201', N'648201', N'648');
INSERT @Spisak VALUES (N'667 001', N'6670014', N'667');
INSERT @Spisak VALUES (N'667 002', N'6670028', N'667');
INSERT @Spisak VALUES (N'667 100', N'6671000', N'667');

IF EXISTS (SELECT 1 FROM @Spisak s WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Lokomotive l WHERE l.Broj = s.Broj))
BEGIN
    SELECT s.Oznaka AS NijeNadjena, s.Broj FROM @Spisak s WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Lokomotive l WHERE l.Broj = s.Broj);
    RAISERROR(N'Lokomotiva sa spiska nije u bazi. Ništa nije promenjeno.', 16, 1);
    RETURN;
END

BEGIN TRAN;
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_SerijeLokomotiva WHERE Oznaka = N'186')
    INSERT planiranje.OP_SerijeLokomotiva (Oznaka, VrstaVuce, Aktivna) VALUES (N'186', 'E', 1);
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_SerijeLokomotiva WHERE Oznaka = N'383')
    INSERT planiranje.OP_SerijeLokomotiva (Oznaka, VrstaVuce, Aktivna) VALUES (N'383', 'E', 1);

UPDATE l SET SerijaID = z.ID
FROM   planiranje.OP_Lokomotive l
JOIN   @Spisak s ON s.Broj = l.Broj
JOIN   planiranje.OP_SerijeLokomotiva z ON z.Oznaka = s.Serija
WHERE  ISNULL(l.SerijaID, 0) <> z.ID;
PRINT N'Lokomotiva sa promenjenom serijom: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- Pre upisa: lokomotive kojima se menja stanje.
SELECT l.ID, l.Broj, l.Naziv, CASE WHEN l.Aktivna = 1 THEN N'postaje neaktivna' ELSE N'postaje aktivna' END AS Promena
FROM   planiranje.OP_Lokomotive l
LEFT JOIN planiranje.OP_SerijeLokomotiva z ON z.ID = l.SerijaID
WHERE  l.Aktivna <> CASE WHEN l.Broj IN (SELECT Broj FROM @Spisak) OR z.Oznaka IN (N'186', N'193') THEN 1 ELSE 0 END
ORDER BY l.Broj;

UPDATE l SET Aktivna = CASE WHEN l.Broj IN (SELECT Broj FROM @Spisak) OR z.Oznaka IN (N'186', N'193') THEN 1 ELSE 0 END
FROM   planiranje.OP_Lokomotive l
LEFT JOIN planiranje.OP_SerijeLokomotiva z ON z.ID = l.SerijaID
WHERE  l.Aktivna <> CASE WHEN l.Broj IN (SELECT Broj FROM @Spisak) OR z.Oznaka IN (N'186', N'193') THEN 1 ELSE 0 END;
PRINT N'Lokomotiva sa promenjenim stanjem: ' + CAST(@@ROWCOUNT AS nvarchar(10));
COMMIT;
GO
SELECT z.Oznaka AS Serija, z.VrstaVuce, COUNT(l.ID) AS Aktivnih
FROM   planiranje.OP_SerijeLokomotiva z
LEFT JOIN planiranje.OP_Lokomotive l ON l.SerijaID = z.ID AND l.Aktivna = 1
GROUP BY z.Oznaka, z.VrstaVuce ORDER BY z.Oznaka;
GO
-- Neaktivna lokomotiva sa zauzetošću od danas: planer treba da je pregleda.
SELECT l.Broj AS Neaktivna, z.Vrsta, z.[Od], z.[Do]
FROM   planiranje.OP_LokomotivaZauzetost z JOIN planiranje.OP_Lokomotive l ON l.ID = z.LokomotivaID
WHERE  l.Aktivna = 0 AND z.[Do] >= GETDATE();
GO
PRINT N'OP_020 završena.';
GO
