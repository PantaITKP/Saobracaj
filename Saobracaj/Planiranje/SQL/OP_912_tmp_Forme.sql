/* =====================================================================
   OP_912_tmp_Forme.sql
   Upisuje 2 forme za pregled privremenih tabela agenta CimProvera (OP_910_tmp_Kreiranje.sql)
   u dbo.Forme i daje prava grupi u dbo.GrupeForme. Meni: Priprema → Porudžbine.
   Isto kao da se unesu ručno kroz frmForme i dodele grupi.

   - Menja samo dbo.Forme i dbo.GrupeForme, i samo dodaje redove.
   - Može da se pokrene više puta: ono što već postoji se ne menja.
   - Grupa se bira promenljivom @IdGrupe (1 = Admin).
   - Pokreće se ručno, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_912_tmp_Forme.sql
   - Briše se zajedno sa tmp tabelama (OP_911_tmp_Brisanje.sql briše i ove redove). Poništavanje je i na dnu.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @IdGrupe int;
SET @IdGrupe = 1;

DECLARE @Forme TABLE (RB int IDENTITY(1,1), Code nvarchar(50) COLLATE DATABASE_DEFAULT, Naziv nvarchar(50) COLLATE DATABASE_DEFAULT);
INSERT @Forme (Code, Naziv) VALUES (N'frmTmpJobs',      N'Planiranje - CIM poslovi (test)');
INSERT @Forme (Code, Naziv) VALUES (N'frmTmpTeretnica', N'Planiranje - CIM teretnica (test)');

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

-- Samo pregled: forme ništa ne upisuju, ali pravo „Pristup“ u PravaRepo znači red u GrupeForme.
INSERT dbo.GrupeForme (IdGrupe, IdForme, Upis, Izmena, Brisanje)
SELECT @IdGrupe, x.IdForme, 0, 0, 0
FROM dbo.Forme x
JOIN @Forme f ON RTRIM(x.Code) = f.Code COLLATE DATABASE_DEFAULT
WHERE NOT EXISTS (SELECT 1 FROM dbo.GrupeForme g WHERE g.IdGrupe = @IdGrupe AND g.IdForme = x.IdForme);
PRINT N'Novih prava: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;

-- Mora da vrati 2 reda sa popunjenom grupom.
SELECT x.IdForme, RTRIM(x.Code) AS Code, RTRIM(x.Naziv) AS Naziv, g.IdGrupe, g.Upis, g.Izmena, g.Brisanje
FROM dbo.Forme x
JOIN @Forme f ON RTRIM(x.Code) = f.Code COLLATE DATABASE_DEFAULT
LEFT JOIN dbo.GrupeForme g ON g.IdForme = x.IdForme AND g.IdGrupe = @IdGrupe
ORDER BY f.RB;
GO

/* Poništavanje (ručno, po potrebi; isto radi i OP_911_tmp_Brisanje.sql):

DELETE g FROM dbo.GrupeForme g JOIN dbo.Forme f ON f.IdForme = g.IdForme WHERE RTRIM(f.Code) IN (N'frmTmpJobs', N'frmTmpTeretnica');
DELETE dbo.Forme WHERE RTRIM(Code) IN (N'frmTmpJobs', N'frmTmpTeretnica');
*/
