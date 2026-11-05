/* =====================================================================
   OP_005_Prava.sql   (OPCIONO, za fazu 8: servis Optimizator)

   Forme planiranja rade u programu Saobracaj (meni Transport) sa
   postojećom konekcijom, pa im ova skripta NIJE potrebna.

   Skripta pravi ulogu za servis Optimizator (Python/FastAPI), koji se
   prijavljuje posebnim nalogom:
     - u šemi [planiranje] sme da čita i menja,
     - u šemi [dbo] sme SAMO DA ČITA navedene tabele,
     - ne može da menja nijednu tabelu operativnog programa.

   Pokreće administrator baze, tek u fazi 8. Login se pravi ručno (dole).
   ===================================================================== */
SET NOCOUNT ON;
GO
IF DATABASE_PRINCIPAL_ID(N'planiranje_app') IS NULL
    CREATE ROLE planiranje_app AUTHORIZATION dbo;
GO

-- 1. Sopstvena šema: puna prava nad podacima
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::planiranje TO planiranje_app;
GO

-- 2. Operativne tabele: samo čitanje (samo one koje planiranje koristi)
DECLARE @t TABLE (Naziv sysname);
INSERT @t VALUES
 (N'Najava'), (N'stanice'), (N'Partnerji'), (N'Mesta'), (N'LokomotivaSerija'), (N'LocoTechnodis'),
 (N'Pruga'), (N'Delavci'), (N'DelovnaMesta'), (N'Bolovanje'), (N'Dopust'), (N'DopustStavke'), (N'DelavciLokMasin'), (N'DelavciPrugaMasin'),
 (N'Telegrami'), (N'Teretnica'), (N'TeretnicaStavke'), (N'Trase'),
 (N'OperativniPlanTrasa'), (N'OperativniPlanTraseDokument'), (N'OperativniPlanPodesavanje'),
 (N'OperativniPlanVozovi'), (N'KorisnikGrupa'), (N'Forme'), (N'GrupeForme'), (N'Korisnici');
DECLARE @n sysname, @sql nvarchar(400);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Naziv FROM @t;
OPEN c; FETCH NEXT FROM c INTO @n;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @n, N'U') IS NOT NULL
    BEGIN
        SET @sql = N'GRANT SELECT ON dbo.' + QUOTENAME(@n) + N' TO planiranje_app;';
        EXEC sys.sp_executesql @sql;
        PRINT N'SELECT: dbo.' + @n;
    END
    ELSE PRINT N'Ne postoji (preskočeno): dbo.' + @n;
    FETCH NEXT FROM c INTO @n;
END
CLOSE c; DEALLOCATE c;
GO

-- 3. Izričita zabrana menjanja dbo (za svaki slučaj, i ako nalog dobije šira prava)
DENY INSERT, UPDATE, DELETE, ALTER ON SCHEMA::dbo TO planiranje_app;
GO

/* 4. Nalog aplikacije (pokrenuti ručno, sa pravom lozinkom):

USE [master];
CREATE LOGIN kp_optimizator WITH PASSWORD = N'<jaka lozinka>', CHECK_POLICY = ON;
USE [Perftech_Beograd];
CREATE USER kp_optimizator FOR LOGIN kp_optimizator WITH DEFAULT_SCHEMA = planiranje;
ALTER ROLE planiranje_app ADD MEMBER kp_optimizator;

   Konekcija servisa Optimizator (Planiranje/Optimizator/.env):
   Server=<server>;Database=Perftech_Beograd;UID=kp_optimizator;PWD=...;APP=KP.Optimizator
*/
