/* =====================================================================
   OP_033_SerijePogon.sql
   Lokomotive i serije:
   - OP_SerijeLokomotiva dobija kolone MasaPoOsovini (t) i Vlasnistvo
     (SOPSTVENA ili ZAKUP, prazno = nije uneto). Pogon je postojeća kolona
     VrstaVuce (E električna, D dizel), pa se ne dodaje nova.
   - Masa se od sada vodi samo na seriji. Kolona OP_Lokomotive.MasaT ostaje
     u bazi, ali je program više ne čita i ne menja.
   - Na kraju je pregled lokomotiva čija masa nije ista kao masa serije,
     da se masa po potrebi prepiše na seriju ručno.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_032. Program ne radi bez nje.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('planiranje.OP_SerijeLokomotiva', 'MasaPoOsovini') IS NULL
    ALTER TABLE planiranje.OP_SerijeLokomotiva ADD MasaPoOsovini decimal(5,2) NULL;
IF COL_LENGTH('planiranje.OP_SerijeLokomotiva', 'Vlasnistvo') IS NULL
    ALTER TABLE planiranje.OP_SerijeLokomotiva ADD Vlasnistvo nvarchar(10) NULL
        CONSTRAINT CK_OP_SerijeLokomotiva_Vlasnistvo CHECK (Vlasnistvo IN (N'SOPSTVENA', N'ZAKUP'));
GO

-- Lokomotive sa masom različitom od mase serije (masa lokomotive se više ne koristi).
SELECT s.Oznaka AS Serija, s.MasaT AS MasaSerije, l.Broj, l.MasaT AS MasaLokomotive
FROM   planiranje.OP_Lokomotive l
JOIN   planiranje.OP_SerijeLokomotiva s ON s.ID = l.SerijaID
WHERE  l.MasaT IS NOT NULL AND l.MasaT > 0 AND (s.MasaT IS NULL OR s.MasaT <> l.MasaT)
ORDER BY s.Oznaka, l.Broj;
PRINT N'OP_033 završena.';
GO

/* Poništavanje (ručno, po potrebi):

ALTER TABLE planiranje.OP_SerijeLokomotiva DROP CONSTRAINT CK_OP_SerijeLokomotiva_Vlasnistvo;
ALTER TABLE planiranje.OP_SerijeLokomotiva DROP COLUMN Vlasnistvo;
ALTER TABLE planiranje.OP_SerijeLokomotiva DROP COLUMN MasaPoOsovini;
*/
