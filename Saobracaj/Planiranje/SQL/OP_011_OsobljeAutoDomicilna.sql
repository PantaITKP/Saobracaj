/* =====================================================================
   OP_011_OsobljeAutoDomicilna.sql
   Ekran Osoblje i kvalifikacije: čekiranja Auto i Samo domicilna.

   - Dodaje kolone Auto i SamoDomicilna u OP_OsobljePlan; podrazumevano 0.
   - U dbo se ništa ne menja. Pogled OP_vOsoblje ostaje isti.
   - Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_001-OP_010.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'Auto') IS NULL
    ALTER TABLE planiranje.OP_OsobljePlan ADD Auto bit NOT NULL CONSTRAINT DF_OP_OsobljePlan_Auto DEFAULT (0);          -- osoba zna da vozi auto
GO
IF COL_LENGTH('planiranje.OP_OsobljePlan', 'SamoDomicilna') IS NULL
    ALTER TABLE planiranje.OP_OsobljePlan ADD SamoDomicilna bit NOT NULL CONSTRAINT DF_OP_OsobljePlan_SamoDomicilna DEFAULT (0); -- osoba radi samo u matičnoj stanici
GO
PRINT N'OP_011 završena.';
GO
