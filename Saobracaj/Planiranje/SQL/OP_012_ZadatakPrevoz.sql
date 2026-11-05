/* =====================================================================
   OP_012_ZadatakPrevoz.sql
   Prevoz osobe bez AUTO do lokacije: vozač dobija zadatak u OP_Zadatak.

   - Dodaje uloge PREVOZ_MASINOVODJA, PREVOZ_PRIJEM i PREVOZ_PREDAJA
     u ograničenje CK_OP_Zadatak_Uloga. Podaci se ne menjaju.
   - Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_011.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'Auto') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_011_OsobljeAutoDomicilna.sql.', 16, 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OP_Zadatak_Uloga' AND definition LIKE N'%PREVOZ_PRIJEM%')
BEGIN
    BEGIN TRAN;
    ALTER TABLE planiranje.OP_Zadatak DROP CONSTRAINT CK_OP_Zadatak_Uloga;
    ALTER TABLE planiranje.OP_Zadatak ADD CONSTRAINT CK_OP_Zadatak_Uloga
        CHECK (Uloga IN (N'MASINOVODJA', N'PREGLEDAC_PRIJEM', N'PREGLEDAC_PREDAJA', N'PREVOZ_MASINOVODJA', N'PREVOZ_PRIJEM', N'PREVOZ_PREDAJA'));
    COMMIT;
END
GO
PRINT N'OP_012 završena.';
GO
