/* =====================================================================
   OP_918_tmp_Dogadjaji.sql
   v3.0 (PROMPT_v3.0, tačka 2.10 – faza 1): događaji iz teksta maila (ETA, ETD, dolazak, polazak, kašnjenje, otkačena kola,
   broj kola, ID najave, broj voza, MRN, ostalo važno) za privremene poslove agenta CimProvera / alata CimRadionica.

   - Pravi SAMO planiranje.tmpJobsDogadjaj i proširuje proveru razloga u planiranje.tmpJobsIstorija (novi razlog DOGADJAJ).
     dbo.* i planiranje.OP_Jobs se ne diraju. Nema FK prema dbo tabelama (MestoID = dbo.stanice.ID, bez veze).
   - U fazi 1 događaji se samo beleže: polja posla se iz teksta ne menjaju, ništa se ne šalje; u Napomenu posla ide jedan red,
     u istoriju zapis sa razlogom DOGADJAJ.
   - SQL Server 2008 R2 (compat 80): bez JSON, STRING_AGG, TRY_CONVERT, IIF.
   - Može da se pokrene više puta. Pokreće se ručno (sysadmin), posle OP_910 i OP_916, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_918_tmp_Dogadjaji.sql
   - Posle ove skripte pokrenuti OP_913_tmp_Prava.sql (prava naloga svc_cimprovera na tmpJobsDogadjaj).
   - Brisanje: OP_911_tmp_Brisanje.sql (briše i ovu tabelu); podaci: OP_915_tmp_BrisanjePodataka.sql.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'planiranje.tmpJobs', N'U') IS NULL OR OBJECT_ID(N'planiranje.tmpDokument', N'U') IS NULL
BEGIN
    RAISERROR(N'Prvo pokrenuti OP_910_tmp_Kreiranje.sql. Ništa nije urađeno.', 16, 1);
    SET NOEXEC ON;
END
GO
IF OBJECT_ID(N'planiranje.tmpJobsDogadjaj', N'U') IS NULL
BEGIN
    CREATE TABLE planiranje.tmpJobsDogadjaj
    (
        ID              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpJobsDogadjaj PRIMARY KEY,
        JobID           int            NULL,          -- posao maila (NULL kada mail još nema posao)
        DokumentID      int            NULL,          -- tmpDokument (mail iz kog je događaj)
        Tip             varchar(20)    NOT NULL,
        MestoID         int            NULL,          -- dbo.stanice.ID (kada je mesto prepoznato; bez FK)
        MestoTekst      nvarchar(200)  NULL,
        Vreme           datetime2(0)   NULL,          -- lokalno vreme u zoni VremenskaZona
        VremeTekst      nvarchar(100)  NULL,          -- kako piše u mailu („18/morning“, „17.11. 12:00“)
        VremenskaZona   varchar(10)    NULL,          -- CET, EET, TRT, UTC
        Vrednost        nvarchar(200)  NULL,          -- broj kola, ID najave, broj voza, MRN, trajanje kašnjenja…
        Recenica        nvarchar(1000) NULL,          -- izvorna rečenica (novi deo poruke, bez citata i potpisa)
        Jezik           varchar(5)     NULL,          -- sr, en, de, hu, ro, bg, tr
        Pouzdanost      tinyint        NULL,          -- 0–100
        Kreirano        datetime2(0)   NOT NULL CONSTRAINT DF_tmpJobsDogadjaj_Kreirano DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_tmpJobsDogadjaj_Job      FOREIGN KEY (JobID)      REFERENCES planiranje.tmpJobs (ID),
        CONSTRAINT FK_tmpJobsDogadjaj_Dokument FOREIGN KEY (DokumentID) REFERENCES planiranje.tmpDokument (ID),
        CONSTRAINT CK_tmpJobsDogadjaj_Tip CHECK (Tip IN ('ETA','ETD','DOLAZAK','POLAZAK','KASNJENJE','OTKACENA_KOLA','BROJ_KOLA','ID_NAJAVE','BROJ_VOZA','MRN','OSTALO_VAZNO'))
    );
    PRINT N'planiranje.tmpJobsDogadjaj napravljena.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsDogadjaj') AND name = N'IX_tmpJobsDogadjaj_Job')
    CREATE INDEX IX_tmpJobsDogadjaj_Job ON planiranje.tmpJobsDogadjaj (JobID, Tip);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsDogadjaj') AND name = N'IX_tmpJobsDogadjaj_Dokument')
    CREATE INDEX IX_tmpJobsDogadjaj_Dokument ON planiranje.tmpJobsDogadjaj (DokumentID);
GO
-- istorija: novi razlog DOGADJAJ (događaj iz teksta maila – samo zapis, polja posla se ne menjaju)
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog' AND definition NOT LIKE N'%DOGADJAJ%')
    ALTER TABLE planiranje.tmpJobsIstorija DROP CONSTRAINT CK_tmpJobsIstorija_Razlog;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tmpJobsIstorija_Razlog')
    ALTER TABLE planiranje.tmpJobsIstorija ADD CONSTRAINT CK_tmpJobsIstorija_Razlog
        CHECK (Razlog IN ('NOVI_POSAO','NOVA_VERZIJA_CIM','SPISAK_KOLA','CIT7','POVRATAK_PRAZNIH','PONOVLJEN_MAIL','DOGADJAJ'));
GO
PRINT N'OP_918_tmp_Dogadjaji: gotovo. Pokrenuti OP_913_tmp_Prava.sql.';
GO
SET NOEXEC OFF;
GO
