/* =====================================================================
   OP_029_OsobljeSerijeProvere.sql
   Osoblje i kvalifikacije:
   - OP_OsobljePlan dobija čekiranja LekarskiPregled, PeriodicniIspit
     (položio periodični ispit) i Skolovanje (prošao školovanje).
   - Poznavanje lokomotiva se zamenjuje poznavanjem serija: nova tabela
     OP_OsobljeSerija (DeSifra, SerijaID, VaziDo, Napomena).
     Postojeća znanja iz OP_OsobljeLokomotiva prenose se na seriju lokomotive
     (rok je najkasniji rok među lokomotivama te serije, a bez roka ako je
     neka bez roka). OP_OsobljeLokomotiva ostaje, ali je program više ne menja.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_028. Program ne radi bez nje.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'LekarskiPregled') IS NULL
    ALTER TABLE planiranje.OP_OsobljePlan ADD LekarskiPregled bit NOT NULL CONSTRAINT DF_OP_OsobljePlan_Lekarski DEFAULT (0);
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'PeriodicniIspit') IS NULL
    ALTER TABLE planiranje.OP_OsobljePlan ADD PeriodicniIspit bit NOT NULL CONSTRAINT DF_OP_OsobljePlan_Ispit DEFAULT (0);
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'Skolovanje') IS NULL
    ALTER TABLE planiranje.OP_OsobljePlan ADD Skolovanje bit NOT NULL CONSTRAINT DF_OP_OsobljePlan_Skolovanje DEFAULT (0);
GO

IF OBJECT_ID(N'planiranje.OP_OsobljeSerija', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljeSerija
(
    DeSifra     int            NOT NULL CONSTRAINT FK_OP_OsobljeSerija_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra),
    SerijaID    int            NOT NULL CONSTRAINT FK_OP_OsobljeSerija_Serija REFERENCES planiranje.OP_SerijeLokomotiva(ID),
    VaziDo      date           NULL,          -- NULL = trajno
    Napomena    nvarchar(300)  NULL,
    CONSTRAINT PK_OP_OsobljeSerija PRIMARY KEY (DeSifra, SerijaID)
);
GO

-- Prenos: ko zna lokomotivu, zna i njenu seriju. Postojeći red serije se ne menja.
INSERT planiranje.OP_OsobljeSerija (DeSifra, SerijaID, VaziDo, Napomena)
SELECT k.DeSifra, l.SerijaID,
       CASE WHEN MAX(CASE WHEN k.VaziDo IS NULL THEN 1 ELSE 0 END) = 1 THEN NULL ELSE MAX(k.VaziDo) END,
       N'Preneto iz poznavanja lokomotiva'
FROM   planiranje.OP_OsobljeLokomotiva k
JOIN   planiranje.OP_Lokomotive l ON l.ID = k.LokomotivaID
WHERE  l.SerijaID IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeSerija s WHERE s.DeSifra = k.DeSifra AND s.SerijaID = l.SerijaID)
GROUP BY k.DeSifra, l.SerijaID;
PRINT N'Prenetih znanja serija: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- Lokomotive bez serije: njihovo znanje ne može da se prenese (upisati seriju lokomotive, pa pokrenuti ponovo).
SELECT l.Broj AS LokomotivaBezSerije, COUNT(*) AS Znanja
FROM   planiranje.OP_OsobljeLokomotiva k
JOIN   planiranje.OP_Lokomotive l ON l.ID = k.LokomotivaID
WHERE  l.SerijaID IS NULL
GROUP BY l.Broj;

SELECT (SELECT COUNT(*) FROM planiranje.OP_OsobljeLokomotiva) AS ZnanjaLokomotiva,
       (SELECT COUNT(*) FROM planiranje.OP_OsobljeSerija) AS ZnanjaSerija;
PRINT N'OP_029 završena.';
GO

/* Poništavanje (ručno, po potrebi):

DROP TABLE planiranje.OP_OsobljeSerija;
ALTER TABLE planiranje.OP_OsobljePlan DROP CONSTRAINT DF_OP_OsobljePlan_Lekarski;
ALTER TABLE planiranje.OP_OsobljePlan DROP COLUMN LekarskiPregled;
ALTER TABLE planiranje.OP_OsobljePlan DROP CONSTRAINT DF_OP_OsobljePlan_Ispit;
ALTER TABLE planiranje.OP_OsobljePlan DROP COLUMN PeriodicniIspit;
ALTER TABLE planiranje.OP_OsobljePlan DROP CONSTRAINT DF_OP_OsobljePlan_Skolovanje;
ALTER TABLE planiranje.OP_OsobljePlan DROP COLUMN Skolovanje;
*/
