/* =====================================================================
   OP_006_Forme.sql
   Upisuje 13 formi planiranja u dbo.Forme i daje prava grupi u dbo.GrupeForme.
   Isto kao da se unesu ručno kroz frmForme i dodele grupi.

   - Menja samo dbo.Forme i dbo.GrupeForme, i samo dodaje redove.
   - Može da se pokrene više puta: ono što već postoji se ne menja.
   - Grupa se bira promenljivom @IdGrupe (1 = Admin).
   - Pokreće se ručno, na bazi Perftech_Beograd. Poništavanje je na dnu.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @IdGrupe int;
SET @IdGrupe = 1;

DECLARE @Forme TABLE (RB int IDENTITY(1,1), Code nvarchar(50) COLLATE DATABASE_DEFAULT, Naziv nvarchar(50) COLLATE DATABASE_DEFAULT);
INSERT @Forme (Code, Naziv) VALUES (N'frmOPNedeljniPlan',  N'Planiranje - Nedeljni plan');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPPosao',         N'Planiranje - Posao');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPPonudaOsoblja', N'Planiranje - Ponuda osoblja');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPDostupnost',    N'Planiranje - Dostupnost osoblja');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPTokPosla',      N'Planiranje - Tok posla');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPTrase',         N'Planiranje - Trase');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPRegistar',      N'Planiranje - Registar pruga');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPStanice',       N'Planiranje - Mreza i stanice');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPLokomotive',    N'Planiranje - Lokomotive i serije');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPOsoblje',       N'Planiranje - Osoblje i kvalifikacije');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPTurnusi',       N'Planiranje - Turnusi');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPTablicaMasa',   N'Planiranje - Tablica masa');
INSERT @Forme (Code, Naziv) VALUES (N'frmOPParametri',     N'Planiranje - Parametri');

IF NOT EXISTS (SELECT 1 FROM dbo.GrupeKorisnik WHERE IdGrupe = @IdGrupe)
BEGIN
    RAISERROR(N'Grupa ne postoji u dbo.GrupeKorisnik. Ništa nije upisano.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- Zaključavanje sprečava da dva upisa dobiju isti IdForme.
DECLARE @MaxId int;
SELECT @MaxId = ISNULL(MAX(IdForme), 0) FROM dbo.Forme WITH (TABLOCKX, HOLDLOCK);

DECLARE @Nove TABLE (RB int IDENTITY(1,1), Code nvarchar(50) COLLATE DATABASE_DEFAULT, Naziv nvarchar(50) COLLATE DATABASE_DEFAULT);
INSERT @Nove (Code, Naziv)
SELECT f.Code, f.Naziv FROM @Forme f
WHERE NOT EXISTS (SELECT 1 FROM dbo.Forme x WHERE RTRIM(x.Code) = f.Code COLLATE DATABASE_DEFAULT)
ORDER BY f.RB;

INSERT dbo.Forme (IdForme, Naziv, Code)
SELECT @MaxId + n.RB, n.Naziv, n.Code FROM @Nove n;
PRINT N'Novih formi: ' + CAST(@@ROWCOUNT AS nvarchar(10));

INSERT dbo.GrupeForme (IdGrupe, IdForme, Upis, Izmena, Brisanje)
SELECT @IdGrupe, x.IdForme, 1, 1, 1
FROM dbo.Forme x
JOIN @Forme f ON RTRIM(x.Code) = f.Code COLLATE DATABASE_DEFAULT
WHERE NOT EXISTS (SELECT 1 FROM dbo.GrupeForme g WHERE g.IdGrupe = @IdGrupe AND g.IdForme = x.IdForme);
PRINT N'Novih prava: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-- Mora da vrati 13 redova sa popunjenim pravima.
SELECT x.IdForme, RTRIM(x.Code) AS Code, RTRIM(x.Naziv) AS Naziv, g.IdGrupe, g.Upis, g.Izmena, g.Brisanje
FROM dbo.Forme x
JOIN @Forme f ON RTRIM(x.Code) = f.Code COLLATE DATABASE_DEFAULT
LEFT JOIN dbo.GrupeForme g ON g.IdForme = x.IdForme AND g.IdGrupe = @IdGrupe
ORDER BY f.RB;
GO

/* Poništavanje (ručno, po potrebi):

DELETE g FROM dbo.GrupeForme g JOIN dbo.Forme f ON f.IdForme = g.IdForme WHERE RTRIM(f.Code) LIKE N'frmOP%';
DELETE dbo.Forme WHERE RTRIM(Code) LIKE N'frmOP%';
*/
