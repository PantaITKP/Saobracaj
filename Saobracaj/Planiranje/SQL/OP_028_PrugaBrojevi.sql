/* =====================================================================
   OP_028_PrugaBrojevi.sql
   Novi brojevi pruga: stotina po vrsti pruge + broj iz Registra.
        MAGISTRALNA    1xx  (M1  -> 101)
        REGIONALNA     2xx  (R5  -> 205)
        LOKALNA        3xx  (L18 -> 318)
        MANIPULATIVNA  4xx  (MP3 -> 403)

   - Menja planiranje.OP_Pruga.RedniBroj u svim verzijama Registra.
   - Sifra (M1, R5, L18, MP3) se ne menja: to je zvanična oznaka po kojoj se
     vezuju mreža (OP_010), elektrifikacija (OP_019) i nova verzija Registra.
   - Menja samo broj manji od 100, pa drugi prolaz ne menja ništa.
   - Uvoz Registra posle ove izmene programa sam daje nove brojeve.
   - U dbo se ništa ne menja. Pokreće se ručno, na bazi Perftech_Beograd.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
-- Broj od 100 ili više mora biti u stotini svoje vrste (već prenumerisan); inače se ništa ne menja.
IF EXISTS (SELECT 1 FROM planiranje.OP_Pruga WHERE RedniBroj >= 100
           AND RedniBroj / 100 <> CASE Kategorija WHEN N'MAGISTRALNA' THEN 1 WHEN N'REGIONALNA' THEN 2 WHEN N'LOKALNA' THEN 3 WHEN N'MANIPULATIVNA' THEN 4 ELSE -1 END)
BEGIN
    RAISERROR(N'Postoji broj pruge od 100 ili više koji nije u stotini svoje vrste. Ništa nije menjano.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

UPDATE planiranje.OP_Pruga
SET    RedniBroj = CASE Kategorija WHEN N'MAGISTRALNA' THEN 100 WHEN N'REGIONALNA' THEN 200 WHEN N'LOKALNA' THEN 300 WHEN N'MANIPULATIVNA' THEN 400 END + RedniBroj
WHERE  RedniBroj < 100
  AND  Kategorija IN (N'MAGISTRALNA', N'REGIONALNA', N'LOKALNA', N'MANIPULATIVNA');
PRINT N'Prenumerisanih pruga: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-- Važeća verzija: novi broj uz zvaničnu oznaku.
SELECT p.RedniBroj, p.Sifra, p.Kategorija, p.Naziv
FROM   planiranje.OP_Pruga p
JOIN   planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID
WHERE  v.Status = N'VAZECA'
ORDER BY p.RedniBroj;
GO

/* Poništavanje (ručno, po potrebi):

UPDATE planiranje.OP_Pruga SET RedniBroj = RedniBroj % 100
WHERE RedniBroj >= 100 AND Kategorija IN (N'MAGISTRALNA', N'REGIONALNA', N'LOKALNA', N'MANIPULATIVNA');
*/
