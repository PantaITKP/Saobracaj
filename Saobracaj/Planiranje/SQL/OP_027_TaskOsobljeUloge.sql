/* =====================================================================
   OP_027_TaskOsobljeUloge.sql
   Nove uloge za radna mesta taska i zadatke osoblja:
   MANEVRISTA, VOZOVODJA, MASINOVODJA_MANEVRA (mašinovođa na manevri) i OSTALO.

   - Proširuje ograničenja CK_OP_TaskOsoblje_Uloga i CK_OP_Zadatak_Uloga.
   - Pogled planiranje.OP_vOsoblje dobija kolonu MasinovodjaManevra iz
     dbo.Delavci (OP_026_DelavciUloge.sql); bez te kolone u Delavci je 0.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_025. Ako se OP_026 pusti kasnije, OP_027 se pokreće ponovo.
     Program ne radi bez nje.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'planiranje.OP_TaskOsoblje', N'U') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_025_TaskOsoblje.sql.', 16, 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OP_TaskOsoblje_Uloga' AND definition LIKE N'%MASINOVODJA_MANEVRA%')
BEGIN
    BEGIN TRAN;
    ALTER TABLE planiranje.OP_TaskOsoblje DROP CONSTRAINT CK_OP_TaskOsoblje_Uloga;
    ALTER TABLE planiranje.OP_TaskOsoblje ADD CONSTRAINT CK_OP_TaskOsoblje_Uloga
        CHECK (Uloga IN (N'MASINOVODJA', N'PREGLEDAC_PRIJEM', N'PREGLEDAC_PREDAJA', N'MANEVRISTA', N'VOZOVODJA', N'MASINOVODJA_MANEVRA', N'OSTALO'));
    COMMIT;
    PRINT N'Proširene uloge radnih mesta taska.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OP_Zadatak_Uloga' AND definition LIKE N'%MASINOVODJA_MANEVRA%')
BEGIN
    BEGIN TRAN;
    ALTER TABLE planiranje.OP_Zadatak DROP CONSTRAINT CK_OP_Zadatak_Uloga;
    ALTER TABLE planiranje.OP_Zadatak ADD CONSTRAINT CK_OP_Zadatak_Uloga
        CHECK (Uloga IN (N'MASINOVODJA', N'PREGLEDAC_PRIJEM', N'PREGLEDAC_PREDAJA', N'PREVOZ_MASINOVODJA', N'PREVOZ_PRIJEM', N'PREVOZ_PREDAJA',
                         N'MANEVRISTA', N'VOZOVODJA', N'MASINOVODJA_MANEVRA', N'OSTALO'));
    COMMIT;
    PRINT N'Proširene uloge zadataka.';
END
GO

-- Pogled se dopunjuje, a ne piše iznova: definicija sa baze ostaje, a dodaje se MasinovodjaManevra
-- pre FROM dbo.Delavci. Ako je ranije dodato 0 (bez OP_026), a kolona sada postoji, 0 se zamenjuje kolonom.
DECLARE @def nvarchar(max), @izraz nvarchar(200), @poz int;
SET @def = OBJECT_DEFINITION(OBJECT_ID(N'planiranje.OP_vOsoblje'));
SET @izraz = CASE WHEN COL_LENGTH('dbo.Delavci', 'MasinovodjaManevra') IS NOT NULL
                  THEN N'CAST(CASE WHEN d.MasinovodjaManevra > 0 THEN 1 ELSE 0 END AS bit)'
                  ELSE N'CAST(0 AS bit)' END;
IF @def IS NULL
    RAISERROR(N'Pogled planiranje.OP_vOsoblje ne postoji (OP_001/OP_007).', 16, 1);
ELSE IF @def LIKE N'%MasinovodjaManevra%' AND NOT (@def LIKE N'%CAST(0 AS bit) AS MasinovodjaManevra%' AND COL_LENGTH('dbo.Delavci', 'MasinovodjaManevra') IS NOT NULL)
    PRINT N'Pogled OP_vOsoblje već ima MasinovodjaManevra.';
ELSE
BEGIN
    SET @poz = CHARINDEX(N'CREATE VIEW', @def);
    SET @def = STUFF(@def, @poz, LEN(N'CREATE VIEW'), N'ALTER VIEW');
    IF @def LIKE N'%CAST(0 AS bit) AS MasinovodjaManevra%'
        SET @def = REPLACE(@def, N'CAST(0 AS bit) AS MasinovodjaManevra', @izraz + N' AS MasinovodjaManevra');
    ELSE
    BEGIN
        SET @poz = PATINDEX(N'%FROM%dbo.Delavci%', @def);
        IF @poz = 0 BEGIN RAISERROR(N'U pogledu OP_vOsoblje nije nađeno FROM dbo.Delavci. Pogled nije menjan.', 16, 1); RETURN; END
        SET @def = STUFF(@def, @poz, 0, N'        , ' + @izraz + N' AS MasinovodjaManevra' + NCHAR(13) + NCHAR(10));
    END
    EXEC (@def);
    PRINT N'Pogled OP_vOsoblje: MasinovodjaManevra ' + CASE WHEN COL_LENGTH('dbo.Delavci', 'MasinovodjaManevra') IS NOT NULL THEN N'iz dbo.Delavci.' ELSE N'= 0 (OP_026 nije puštena; posle nje pokrenuti OP_027 ponovo).' END;
END
GO
PRINT N'OP_027 završena.';
GO
