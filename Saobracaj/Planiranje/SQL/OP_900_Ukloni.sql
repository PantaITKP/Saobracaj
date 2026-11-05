/* =====================================================================
   OP_900_Ukloni.sql
   Briše SVE OP_ tabele i poglede u šemi [planiranje] (povratak na stanje pre OP_001).
   Postojeće tabele programa se ne diraju. Pre ovoga ukloniti/sakriti nove forme u meniju Transport.
   PAŽNJA: briše i podatke unete kroz nove forme. Pre pokretanja napraviti
   rezervnu kopiju baze. Pokreće se samo ručno.
   ===================================================================== */
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'planiranje.OP_vOsobljeDan', N'V')         IS NOT NULL DROP VIEW planiranje.OP_vOsobljeDan;
IF OBJECT_ID(N'planiranje.OP_vOdsustvoPregled', N'V')    IS NOT NULL DROP VIEW planiranje.OP_vOdsustvoPregled;
IF OBJECT_ID(N'planiranje.OP_vOdsustvo', N'V')           IS NOT NULL DROP VIEW planiranje.OP_vOdsustvo;
IF OBJECT_ID(N'planiranje.OP_vOsoblje', N'V')            IS NOT NULL DROP VIEW planiranje.OP_vOsoblje;
IF OBJECT_ID(N'planiranje.OP_vLokomotivaKalendar', N'V') IS NOT NULL DROP VIEW planiranje.OP_vLokomotivaKalendar;
IF OBJECT_ID(N'planiranje.OP_vPosao', N'V')              IS NOT NULL DROP VIEW planiranje.OP_vPosao;
IF OBJECT_ID(N'planiranje.OP_vNajava', N'V')             IS NOT NULL DROP VIEW planiranje.OP_vNajava;
IF OBJECT_ID(N'planiranje.OP_vTelegram', N'V')           IS NOT NULL DROP VIEW planiranje.OP_vTelegram;
IF OBJECT_ID(N'planiranje.OP_vTeretnicaNajava', N'V')    IS NOT NULL DROP VIEW planiranje.OP_vTeretnicaNajava;
GO
-- redosled: prvo tabele koje pokazuju na druge
DECLARE @t TABLE (RB int IDENTITY, Naziv sysname);
INSERT @t (Naziv) VALUES
 (N'OP_OsobljePruga'), (N'OP_OsobljeLokomotiva'), (N'OP_PosaoDogadjaj'), (N'OP_Zadatak'), (N'OP_Odsustvo'), (N'OP_RazlogOdsustva'), (N'OP_Smena'), (N'OP_OsobljeTurnus'), (N'OP_Turnus'),
 (N'OP_OsobljeKvalifikacija'), (N'OP_PlanVerzijaStavka'), (N'OP_PlanVerzija'), (N'OP_PosaoRuta'), (N'OP_PosaoLokomotiva'), (N'OP_Posao'),
 (N'OP_LokomotivaZauzetost'), (N'OP_TablicaMasa'), (N'OP_Parametri'), (N'OP_TrasaStanice'), (N'OP_Trase'),
 (N'OP_TraseVerzija'), (N'OP_MrezaVeza'), (N'OP_PrugaMesto'), (N'OP_Pruga'), (N'OP_RegistarVerzija'),
 (N'OP_OsobljePlan'), (N'OP_Lokomotive'), (N'OP_SerijeLokomotiva'), (N'OP_Stanice');
DECLARE @n sysname, @i int = 1, @max int = (SELECT MAX(RB) FROM @t), @sql nvarchar(400);
WHILE @i <= @max
BEGIN
    SELECT @n = Naziv FROM @t WHERE RB = @i;
    IF OBJECT_ID(N'planiranje.' + @n, N'U') IS NOT NULL
    BEGIN
        SET @sql = N'DROP TABLE planiranje.' + QUOTENAME(@n) + N';';
        EXEC sys.sp_executesql @sql;
        PRINT N'Obrisana ' + @n;
    END
    SET @i += 1;
END
GO
IF SCHEMA_ID(N'planiranje') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID(N'planiranje'))
    EXEC (N'DROP SCHEMA planiranje;');
GO
