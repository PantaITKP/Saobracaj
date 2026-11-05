/* =====================================================================
   OP_007_Osoblje.sql
   Dorada KP-26-11-03-Osoblje: matrice kvalifikacija i razlozi odsustva.

   - Dodaje tabele OP_OsobljePruga, OP_OsobljeLokomotiva, OP_RazlogOdsustva.
   - Dodaje kolone RazlogID i OdobrioDeSifra u OP_Odsustvo.
   - Dodaje pogled OP_vOdsustvoPregled.
   - Ne briše ništa. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_001-OP_004. Prenos podataka je u OP_008.
   ===================================================================== */
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'planiranje.OP_OsobljePruga', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljePruga
(
    DeSifra     int            NOT NULL CONSTRAINT FK_OP_OsobljePruga_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra),
    PrugaID     int            NOT NULL CONSTRAINT FK_OP_OsobljePruga_Pruga REFERENCES planiranje.OP_Pruga(ID),
    VaziDo      date           NULL,          -- NULL = trajno
    Napomena    nvarchar(300)  NULL,
    CONSTRAINT PK_OP_OsobljePruga PRIMARY KEY (DeSifra, PrugaID)
);
GO

IF OBJECT_ID(N'planiranje.OP_OsobljeLokomotiva', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljeLokomotiva
(
    DeSifra       int            NOT NULL CONSTRAINT FK_OP_OsobljeLokomotiva_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra),
    LokomotivaID  int            NOT NULL CONSTRAINT FK_OP_OsobljeLokomotiva_Lok REFERENCES planiranje.OP_Lokomotive(ID),
    VaziDo        date           NULL,        -- NULL = trajno
    Napomena      nvarchar(300)  NULL,
    CONSTRAINT PK_OP_OsobljeLokomotiva PRIMARY KEY (DeSifra, LokomotivaID)
);
GO

IF OBJECT_ID(N'planiranje.OP_RazlogOdsustva', N'U') IS NULL
CREATE TABLE planiranje.OP_RazlogOdsustva
(
    ID       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_RazlogOdsustva PRIMARY KEY,
    Naziv    nvarchar(50)   NOT NULL CONSTRAINT UX_OP_RazlogOdsustva_Naziv UNIQUE,
    Vrsta    nvarchar(10)   NOT NULL CONSTRAINT CK_OP_RazlogOdsustva_Vrsta CHECK (Vrsta IN (N'GODISNJI', N'BOLOVANJE', N'SLOBODAN', N'OSTALO')),
    RB       int            NOT NULL,
    Aktivan  bit            NOT NULL CONSTRAINT DF_OP_RazlogOdsustva_Aktivan DEFAULT (1)
);
GO

IF NOT EXISTS (SELECT 1 FROM planiranje.OP_RazlogOdsustva WHERE Naziv = N'Godišnji odmor')
    INSERT planiranje.OP_RazlogOdsustva (Naziv, Vrsta, RB) VALUES (N'Godišnji odmor', N'GODISNJI', 1);
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_RazlogOdsustva WHERE Naziv = N'Bolovanje')
    INSERT planiranje.OP_RazlogOdsustva (Naziv, Vrsta, RB) VALUES (N'Bolovanje', N'BOLOVANJE', 2);
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_RazlogOdsustva WHERE Naziv = N'Slobodan dan')
    INSERT planiranje.OP_RazlogOdsustva (Naziv, Vrsta, RB) VALUES (N'Slobodan dan', N'SLOBODAN', 3);
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_RazlogOdsustva WHERE Naziv = N'Službeni put')
    INSERT planiranje.OP_RazlogOdsustva (Naziv, Vrsta, RB) VALUES (N'Službeni put', N'OSTALO', 4);
IF NOT EXISTS (SELECT 1 FROM planiranje.OP_RazlogOdsustva WHERE Naziv = N'Ostalo')
    INSERT planiranje.OP_RazlogOdsustva (Naziv, Vrsta, RB) VALUES (N'Ostalo', N'OSTALO', 5);
GO

-- Ranija ručna odsustva ostaju bez ovih podataka; obavezni su samo pri novom unosu.
IF COL_LENGTH('planiranje.OP_Odsustvo', 'RazlogID') IS NULL
    ALTER TABLE planiranje.OP_Odsustvo ADD RazlogID int NULL CONSTRAINT FK_OP_Odsustvo_Razlog REFERENCES planiranje.OP_RazlogOdsustva(ID);
GO
IF COL_LENGTH('planiranje.OP_Odsustvo', 'OdobrioDeSifra') IS NULL
    ALTER TABLE planiranje.OP_Odsustvo ADD OdobrioDeSifra int NULL; -- dbo.Delavci.DeSifra
GO

-- Pregled odsustava po ulogama. Bolovanje i Dopust nemaju podatak ko je odobrio.
IF OBJECT_ID(N'planiranje.OP_vOdsustvoPregled', N'V') IS NOT NULL DROP VIEW planiranje.OP_vOdsustvoPregled;
GO
CREATE VIEW planiranje.OP_vOdsustvoPregled
AS
SELECT  v.DeSifra, s.Naziv AS Zaposleni, v.Od, v.Do,
        CAST(CASE v.Vrsta WHEN N'BOLOVANJE' THEN N'Bolovanje' WHEN N'GODISNJI' THEN N'Godišnji odmor' ELSE v.Vrsta END AS nvarchar(50)) AS Razlog,
        CAST(NULL AS nvarchar(100)) AS Odobrio, CAST(v.Napomena AS nvarchar(300)) AS Napomena, v.Izvor,
        s.Masinovodja, s.PregledacKola, s.Vozovodja, s.Manevrista
FROM    planiranje.OP_vOdsustvo v
JOIN    planiranje.OP_vOsoblje s ON s.DeSifra = v.DeSifra
WHERE   v.Izvor <> N'PLANIRANJE'
UNION ALL
SELECT  o.DeSifra, s.Naziv, o.Od, o.Do,
        CAST(ISNULL(r.Naziv, o.Vrsta) AS nvarchar(50)),
        CAST(a.Naziv AS nvarchar(100)), o.Napomena, N'PLANIRANJE',
        s.Masinovodja, s.PregledacKola, s.Vozovodja, s.Manevrista
FROM    planiranje.OP_Odsustvo o
JOIN    planiranje.OP_vOsoblje s ON s.DeSifra = o.DeSifra
LEFT JOIN planiranje.OP_RazlogOdsustva r ON r.ID = o.RazlogID
LEFT JOIN planiranje.OP_vOsoblje a ON a.DeSifra = o.OdobrioDeSifra;
GO

PRINT N'OP_007_Osoblje.sql završen.';
GO
