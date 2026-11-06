/* =====================================================================
   OP_021_FormaMapa.sql
   Upisuje formu frmOPMapa (Mapa lokomotiva, Transport / Tabla) u dbo.Forme
   i daje joj ista prava koja grupe imaju na frmOPStanice (Mreža i stanice).

   - Menja samo dbo.Forme i dbo.GrupeForme, i samo dodaje redove.
   - Može da se pokrene više puta: ono što već postoji se ne menja.
   - Pokreće se ručno, na bazi Perftech_Beograd. Poništavanje je na dnu.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Code nvarchar(50), @Naziv nvarchar(50), @Uzor nvarchar(50);
SET @Code = N'frmOPMapa';
SET @Naziv = N'Planiranje - Mapa lokomotiva';
SET @Uzor = N'frmOPStanice';

IF NOT EXISTS (SELECT 1 FROM dbo.Forme WHERE RTRIM(Code) = @Uzor)
BEGIN
    RAISERROR(N'Forma frmOPStanice nije upisana (OP_006_Forme.sql). Ništa nije upisano.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- Zaključavanje sprečava da dva upisa dobiju isti IdForme.
DECLARE @MaxId int;
SELECT @MaxId = ISNULL(MAX(IdForme), 0) FROM dbo.Forme WITH (TABLOCKX, HOLDLOCK);

IF NOT EXISTS (SELECT 1 FROM dbo.Forme WHERE RTRIM(Code) = @Code)
BEGIN
    INSERT dbo.Forme (IdForme, Naziv, Code) VALUES (@MaxId + 1, @Naziv, @Code);
    PRINT N'Nova forma: ' + @Code;
END

DECLARE @IdForme int, @IdUzora int;
SELECT @IdForme = IdForme FROM dbo.Forme WHERE RTRIM(Code) = @Code;
SELECT @IdUzora = IdForme FROM dbo.Forme WHERE RTRIM(Code) = @Uzor;

INSERT dbo.GrupeForme (IdGrupe, IdForme, Upis, Izmena, Brisanje)
SELECT u.IdGrupe, @IdForme, u.Upis, u.Izmena, u.Brisanje
FROM dbo.GrupeForme u
WHERE u.IdForme = @IdUzora
  AND NOT EXISTS (SELECT 1 FROM dbo.GrupeForme g WHERE g.IdGrupe = u.IdGrupe AND g.IdForme = @IdForme);
PRINT N'Novih prava: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-- Grupe sa pravom na Mapu lokomotiva (iste kao na Mreži i stanicama).
SELECT x.IdForme, RTRIM(x.Code) AS Code, g.IdGrupe, g.Upis, g.Izmena, g.Brisanje
FROM dbo.Forme x
JOIN dbo.GrupeForme g ON g.IdForme = x.IdForme
WHERE RTRIM(x.Code) = @Code;
GO

/* Poništavanje (ručno, po potrebi):

DELETE g FROM dbo.GrupeForme g JOIN dbo.Forme f ON f.IdForme = g.IdForme WHERE RTRIM(f.Code) = N'frmOPMapa';
DELETE dbo.Forme WHERE RTRIM(Code) = N'frmOPMapa';
*/
