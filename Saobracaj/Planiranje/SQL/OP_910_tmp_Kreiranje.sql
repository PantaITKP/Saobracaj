/* =====================================================================
   OP_910_tmp_Kreiranje.sql
   Privremene tabele za test automatskog upisa iz agenta CimProvera (v2.9.0):
   tovarni list CIM/CUV → posao (tmpJobs), spisak kola CIT 23 → teretnica (tmpTeretnica, tmpTeretnicaStavke).

   - SQL Server 2008 R2: bez JSON, STRING_AGG, TRY_CONVERT, IIF.
   - Pravi samo nove tabele planiranje.tmp*. Postojeće tabele se ne menjaju, nema FK prema dbo tabelama.
   - Nema kolona vezanih za najavu (IzvornaNajavaID, IDNajave).
   - Može da se pokrene više puta: ono što već postoji se preskače.
   - Pokreće se ručno, na bazi Perftech_Beograd:
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_910_tmp_Kreiranje.sql
   - Brisanje: OP_911_tmp_Brisanje.sql.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Baza je na nivou kompatibilnosti 80: filtrirani indeksi traže ARITHABORT, ANSI_WARNINGS, ANSI_NULLS i QUOTED_IDENTIFIER ON,
-- i pri pravljenju i pri svakom upisu (agent ih postavlja posle otvaranja konekcije).
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF SCHEMA_ID(N'planiranje') IS NULL
BEGIN
    RAISERROR(N'Šema planiranje ne postoji. Ništa nije napravljeno.', 16, 1);
    SET NOEXEC ON;   -- ostatak skripte se ne izvršava
END
GO

/* ---------------------------------------------------------------------
   1. tmpDokument: svaki obrađen prilog (isti fajl = isti hash = jedan red)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpDokument', N'U') IS NULL
CREATE TABLE planiranje.tmpDokument
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpDokument PRIMARY KEY,
    MessageID           nvarchar(400)  NULL,
    UID                 bigint         NULL,          -- IMAP UID (uint32)
    Sanduce             nvarchar(200)  NULL,          -- npr. INBOX, Sent
    Posiljalac          nvarchar(320)  NULL,          -- From
    Naslov              nvarchar(500)  NULL,
    Primljeno           datetime2(0)   NULL,
    ImeFajla            nvarchar(400)  NULL,
    HashFajla           varchar(64)    NULL,          -- SHA-256, hex
    TipDokumenta        varchar(20)    NOT NULL CONSTRAINT DF_tmpDokument_Tip DEFAULT ('OSTALO'),
    Strane              nvarchar(100)  NULL,          -- npr. „2“, „6–7“
    KljucPosiljke       nvarchar(200)  NULL,
    JobID               int            NULL,          -- → tmpJobs (bez FK: tmpJobs pokazuje na tmpDokument)
    TeretnicaID         int            NULL,          -- → tmpTeretnica
    TekstMaila          nvarchar(max)  NULL,          -- ceo tekst maila (u tmpJobs.Napomena ide skraćen)
    BrojPrijema         int            NOT NULL CONSTRAINT DF_tmpDokument_BrojPrijema DEFAULT (1),   -- isti fajl primljen ponovo
    PoslednjePrimljeno  datetime2(0)   NULL,
    Obradjeno           datetime2(0)   NOT NULL CONSTRAINT DF_tmpDokument_Obradjeno DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_tmpDokument_Tip CHECK (TipDokumenta IN ('CIM','CUV','SPISAK_KOLA','LOADLIST','T1','JCI','CIT7','NAJAVA','SASTAV_VOZA','OSTALO'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'UX_tmpDokument_Hash')
    CREATE UNIQUE INDEX UX_tmpDokument_Hash ON planiranje.tmpDokument (HashFajla) WHERE HashFajla IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'IX_tmpDokument_Kljuc')
    CREATE INDEX IX_tmpDokument_Kljuc ON planiranje.tmpDokument (KljucPosiljke) WHERE KljucPosiljke IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'IX_tmpDokument_Job')
    CREATE INDEX IX_tmpDokument_Job ON planiranje.tmpDokument (JobID) WHERE JobID IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpDokument') AND name = N'IX_tmpDokument_Message')
    CREATE INDEX IX_tmpDokument_Message ON planiranje.tmpDokument (MessageID) WHERE MessageID IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   2. tmpJobs: posao iz tovarnog lista. Kolone kao planiranje.OP_Jobs,
      bez IzvornaNajavaID; Status, Stanica1ID i Stanica3ID dozvoljavaju NULL.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpJobs', N'U') IS NULL
CREATE TABLE planiranje.tmpJobs
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpJobs PRIMARY KEY,
    PlatilacID          int            NULL,          -- Partnerji.PaSifra, platilac za deonicu KP iz r7
    OrganizatorID       int            NULL,          -- NULL (dogovor kasnije)
    TipPrevozaID        int            NULL,          -- NULL (dogovor kasnije)
    OtpravnaID          int            NULL,          -- stanice.ID, r16/r17
    UputnaID            int            NULL,          -- stanice.ID, r10/r12
    Status              int            NULL,          -- NULL (u OP_Jobs NOT NULL)
    Napomena            nvarchar(2000) NULL,          -- tekst maila, najnoviji blok gore
    PreuzimaOdID        int            NULL,          -- Partnerji.PaSifra, prevoznik pre ulaska u Srbiju
    PredajeOperateruID  int            NULL,          -- Partnerji.PaSifra, prevoznik posle izlaska iz Srbije
    Stanica1ID          int            NULL,          -- gde KP preuzima voz (prednost „gr.“)
    Stanica2ID          int            NULL,          -- NULL (kasnije)
    Stanica3ID          int            NULL,          -- gde KP predaje voz (prednost „gr.“)
    ETD                 datetime2(0)   NULL,
    NHM1                int            NULL,          -- NHM.ID
    Bruto1              decimal(18,3)  NULL,          -- t
    BrojKola1           int            NULL,
    Duzina1             decimal(18,2)  NULL,          -- m
    Neto1               decimal(18,3)  NULL,          -- t
    RIDBroj1            nvarchar(100)  NULL,
    OMBroj1             nvarchar(100)  NULL,
    NHM2                int            NULL,
    Bruto2              decimal(18,3)  NULL,
    BrojKola2           int            NULL,
    Duzina2             decimal(18,2)  NULL,
    Neto2               decimal(18,3)  NULL,
    RIDBroj2            nvarchar(100)  NULL,
    OMBroj2             nvarchar(100)  NULL,
    Kreirao             nvarchar(100)  NOT NULL CONSTRAINT DF_tmpJobs_Kreirao  DEFAULT (N'CimProvera'),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_tmpJobs_Kreirano DEFAULT (SYSDATETIME()),
    Izmenio             nvarchar(100)  NULL,
    Izmenjeno           datetime2(0)   NULL,
    RowVer              rowversion     NOT NULL,
    SerijaKola1         nvarchar(40)   NULL,
    SerijaKola2         nvarchar(40)   NULL,
    -- dodatno u odnosu na OP_Jobs
    KljucPosiljke       nvarchar(200)  NULL,          -- ključ iz dosijea pošiljke (r62, ili pošiljalac + r8 + relacija)
    Referenca           nvarchar(100)  NULL,          -- r8
    BrojPonude          nvarchar(100)  NULL,          -- r14
    Verzija             int            NOT NULL CONSTRAINT DF_tmpJobs_Verzija      DEFAULT (1),
    PoslednjiDokumentID int            NULL,
    ImaGreske           bit            NOT NULL CONSTRAINT DF_tmpJobs_ImaGreske    DEFAULT (0),
    KPTovarniList       bit            NOT NULL CONSTRAINT DF_tmpJobs_KPTovarni    DEFAULT (0),   -- r62 = 3212
    CONSTRAINT FK_tmpJobs_Dokument FOREIGN KEY (PoslednjiDokumentID) REFERENCES planiranje.tmpDokument (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobs') AND name = N'UX_tmpJobs_Kljuc')
    CREATE UNIQUE INDEX UX_tmpJobs_Kljuc ON planiranje.tmpJobs (KljucPosiljke) WHERE KljucPosiljke IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobs') AND name = N'IX_tmpJobs_Kreirano')
    CREATE INDEX IX_tmpJobs_Kreirano ON planiranje.tmpJobs (Kreirano);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobs') AND name = N'IX_tmpJobs_Referenca')
    CREATE INDEX IX_tmpJobs_Referenca ON planiranje.tmpJobs (Referenca) WHERE Referenca IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   3. tmpTeretnica: zaglavlje spiska kola. Kolone kao dbo.Teretnica + JobID.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpTeretnica', N'U') IS NULL
CREATE TABLE planiranje.tmpTeretnica
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpTeretnica PRIMARY KEY,
    JobID               int            NULL,          -- NULL dok ne stigne tovarni list
    BrojTeretnice       nvarchar(100)  NULL,          -- identifikacija sa spiska (r62)
    StanicaOd           int            NULL,          -- = tmpJobs.Stanica1ID
    StanicaDo           int            NULL,          -- = tmpJobs.Stanica3ID
    StanicaPopisa       int            NULL,
    VremeOd             datetime       NULL,
    VremeDo             datetime       NULL,
    BrojLista           nvarchar(40)   NULL,          -- NULL (u dbo.Teretnica broj najave)
    Prijemna            tinyint        NULL,          -- 1
    Predajna            tinyint        NULL,          -- 0
    Korisnik            nvarchar(100)  NULL,          -- „CimProvera“
    Prevozna            tinyint        NULL,          -- 0
    RN                  int            NULL,
    Napomena            nvarchar(512)  NULL,          -- izvorni fajl i strane
    Carinska            int            NULL CONSTRAINT DF_tmpTeretnica_Carinska DEFAULT (0),
    rb                  int            NULL CONSTRAINT DF_tmpTeretnica_rb       DEFAULT (0),
    -- dodatno
    DokumentID          int            NULL,
    Verzija             int            NOT NULL CONSTRAINT DF_tmpTeretnica_Verzija DEFAULT (1),
    KljucSpiska         nvarchar(200)  NULL,          -- isti ključ = nova verzija istog spiska
    KljucPosiljke       nvarchar(200)  NULL,          -- za naknadno povezivanje sa poslom
    Referenca           nvarchar(100)  NULL,          -- r8 / broj voza iz naslova ili spiska
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_tmpTeretnica_Kreirano DEFAULT (SYSDATETIME()),
    Izmenjeno           datetime2(0)   NULL,
    CONSTRAINT FK_tmpTeretnica_Job      FOREIGN KEY (JobID)      REFERENCES planiranje.tmpJobs (ID),
    CONSTRAINT FK_tmpTeretnica_Dokument FOREIGN KEY (DokumentID) REFERENCES planiranje.tmpDokument (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND name = N'IX_tmpTeretnica_Job')
    CREATE INDEX IX_tmpTeretnica_Job ON planiranje.tmpTeretnica (JobID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND name = N'UX_tmpTeretnica_KljucSpiska')
    CREATE UNIQUE INDEX UX_tmpTeretnica_KljucSpiska ON planiranje.tmpTeretnica (KljucSpiska) WHERE KljucSpiska IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnica') AND name = N'IX_tmpTeretnica_KljucPosiljke')
    CREATE INDEX IX_tmpTeretnica_KljucPosiljke ON planiranje.tmpTeretnica (KljucPosiljke) WHERE KljucPosiljke IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   4. tmpTeretnicaStavke: jedna stavka = jedna kola. Kolone kao dbo.TeretnicaStavke,
      bez IDNajave. Jedinice kao u dbo.TeretnicaStavke: Duzina u m (14.800), Tara i Neto u t (19.990, 59.757).
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpTeretnicaStavke', N'U') IS NULL
CREATE TABLE planiranje.tmpTeretnicaStavke
(
    ID                        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpTeretnicaStavke PRIMARY KEY,
    RB                        int            NULL,
    BrojTeretnice             int            NOT NULL,        -- → tmpTeretnica.ID
    Uvrstena                  int            NULL,
    Otkacena                  int            NULL,
    BrojKola                  nvarchar(40)   NOT NULL,        -- „33854850210-7“
    Serija                    nvarchar(40)   NULL,
    BrojOsovina               decimal(18,2)  NULL,
    Duzina                    numeric(18,3)  NULL,            -- m
    Tara                      numeric(18,3)  NULL,            -- t
    Neto                      numeric(18,3)  NULL,            -- t
    G                         decimal(18,2)  NULL,
    P                         numeric(18,3)  NULL,
    R                         decimal(18,2)  NULL,
    RR                        decimal(18,2)  NULL,
    VRNP                      nvarchar(40)   NULL,            -- NHM, 4 cifre
    Otpravna                  int            NULL,
    Uputna                    int            NULL,
    Reon                      nvarchar(40)   NULL,
    Primedba                  nvarchar(100)  NULL,
    RucKoc                    decimal(18,2)  NULL,
    Uvozna                    int            NULL,
    Izvozna                   int            NULL,
    RID                       nvarchar(60)   NULL,
    Dokument                  nvarchar(40)   NULL,            -- broj otpravljanja iz r62
    SifraKvaraID              int            NULL,
    KontrolaBrojKola          nvarchar(40)   NULL,
    KontrolaDuznina           numeric(18,3)  NULL,
    KontrolaTara              numeric(18,3)  NULL,
    KontrolaP                 numeric(18,3)  NULL,
    KontrolaRucKoc            decimal(18,2)  NULL,
    IdPopisaneStavkeTeretnice int            NULL,
    NamernaGreska             bit            NOT NULL CONSTRAINT DF_tmpTeretnicaStavke_NamernaGreska DEFAULT (0),
    OpisNepravilnosti         nvarchar(1024) NULL,
    Popisana                  bit            NOT NULL CONSTRAINT DF_tmpTeretnicaStavke_Popisana DEFAULT (0),
    TrenutniRedniBroj         int            NULL,
    Obavezna                  int            NULL,
    PrivremenoPopisana        bit            NULL,
    LLPapuce                  int            NULL,
    DokumentID                int            NULL,
    CONSTRAINT FK_tmpTeretnicaStavke_Teretnica FOREIGN KEY (BrojTeretnice) REFERENCES planiranje.tmpTeretnica (ID) ON DELETE CASCADE,
    CONSTRAINT FK_tmpTeretnicaStavke_Dokument  FOREIGN KEY (DokumentID)    REFERENCES planiranje.tmpDokument (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnicaStavke') AND name = N'UX_tmpTeretnicaStavke_Kola')
    CREATE UNIQUE INDEX UX_tmpTeretnicaStavke_Kola ON planiranje.tmpTeretnicaStavke (BrojTeretnice, BrojKola);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpTeretnicaStavke') AND name = N'IX_tmpTeretnicaStavke_BrojKola')
    CREATE INDEX IX_tmpTeretnicaStavke_BrojKola ON planiranje.tmpTeretnicaStavke (BrojKola);   -- povezivanje spiska po brojevima kola
GO

/* ---------------------------------------------------------------------
   5. tmpJobsPolje: poreklo svake upisane vrednosti (posao ili teretnica)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpJobsPolje', N'U') IS NULL
CREATE TABLE planiranje.tmpJobsPolje
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpJobsPolje PRIMARY KEY,
    JobID               int            NULL,
    TeretnicaID         int            NULL,
    Kolona              nvarchar(128)  NOT NULL,
    Vrednost            nvarchar(400)  NULL,          -- upisana vrednost (NULL = nije pronađeno)
    TekstIzDokumenta    nvarchar(1000) NULL,          -- npr. „Payer for KP: Budamar South, s.r.o.“
    Rubrika             nvarchar(50)   NULL,          -- r7, r25, spisak…
    DokumentID          int            NULL,
    Pouzdanost          tinyint        NULL,          -- 0–100
    Kandidati           nvarchar(1000) NULL,          -- 3 najbliža partnera/stanice sa ocenom
    RucnoIzmenjeno      bit            NOT NULL CONSTRAINT DF_tmpJobsPolje_Rucno DEFAULT (0),
    Izmenjeno           datetime2(0)   NOT NULL CONSTRAINT DF_tmpJobsPolje_Izmenjeno DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_tmpJobsPolje_Job       FOREIGN KEY (JobID)       REFERENCES planiranje.tmpJobs (ID),
    CONSTRAINT FK_tmpJobsPolje_Teretnica FOREIGN KEY (TeretnicaID) REFERENCES planiranje.tmpTeretnica (ID) ON DELETE CASCADE,
    CONSTRAINT FK_tmpJobsPolje_Dokument  FOREIGN KEY (DokumentID)  REFERENCES planiranje.tmpDokument (ID),
    CONSTRAINT CK_tmpJobsPolje_Veza CHECK (JobID IS NOT NULL OR TeretnicaID IS NOT NULL),
    CONSTRAINT CK_tmpJobsPolje_Pouzdanost CHECK (Pouzdanost IS NULL OR Pouzdanost <= 100)
);
GO
-- jedno poreklo po koloni: za posao (TeretnicaID NULL) i za teretnicu
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsPolje') AND name = N'UX_tmpJobsPolje_Job')
    CREATE UNIQUE INDEX UX_tmpJobsPolje_Job ON planiranje.tmpJobsPolje (JobID, Kolona) WHERE TeretnicaID IS NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsPolje') AND name = N'UX_tmpJobsPolje_Teretnica')
    CREATE UNIQUE INDEX UX_tmpJobsPolje_Teretnica ON planiranje.tmpJobsPolje (TeretnicaID, Kolona) WHERE TeretnicaID IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   6. tmpJobsIstorija: svaka promena posla ili teretnice
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpJobsIstorija', N'U') IS NULL
CREATE TABLE planiranje.tmpJobsIstorija
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpJobsIstorija PRIMARY KEY,
    JobID               int            NULL,
    TeretnicaID         int            NULL,          -- bez FK: istorija ostaje i kad se stavka/teretnica obriše
    Kolona              nvarchar(128)  NULL,
    StaraVrednost       nvarchar(2000) NULL,
    NovaVrednost        nvarchar(2000) NULL,
    DokumentID          int            NULL,
    Razlog              varchar(30)    NOT NULL,
    Napomena            nvarchar(400)  NULL,          -- npr. „ručno izmenjeno – nije menjano“, „Broj kola u r21 (26) ne odgovara spisku (28)“
    Vreme               datetime2(0)   NOT NULL CONSTRAINT DF_tmpJobsIstorija_Vreme DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_tmpJobsIstorija_Job      FOREIGN KEY (JobID)      REFERENCES planiranje.tmpJobs (ID),
    CONSTRAINT FK_tmpJobsIstorija_Dokument FOREIGN KEY (DokumentID) REFERENCES planiranje.tmpDokument (ID),
    CONSTRAINT CK_tmpJobsIstorija_Razlog CHECK (Razlog IN ('NOVI_POSAO','NOVA_VERZIJA_CIM','SPISAK_KOLA','CIT7'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsIstorija') AND name = N'IX_tmpJobsIstorija_Job')
    CREATE INDEX IX_tmpJobsIstorija_Job ON planiranje.tmpJobsIstorija (JobID, Vreme);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpJobsIstorija') AND name = N'IX_tmpJobsIstorija_Teretnica')
    CREATE INDEX IX_tmpJobsIstorija_Teretnica ON planiranje.tmpJobsIstorija (TeretnicaID) WHERE TeretnicaID IS NOT NULL;
GO

/* ---------------------------------------------------------------------
   7. tmpSinonimi: ručno potvrđeni prevodi teksta iz dokumenta u šifru
      (agent sada samo čita; upis dolazi iz aplikacije u sledećoj fazi)
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'planiranje.tmpSinonimi', N'U') IS NULL
CREATE TABLE planiranje.tmpSinonimi
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_tmpSinonimi PRIMARY KEY,
    Tip                 varchar(20)    NOT NULL,      -- PARTNER (CiljniID = PaSifra), STANICA (stanice.ID), NHM (NHM.ID)
    TekstNormalizovan   nvarchar(200)  NOT NULL,
    CiljniID            int            NOT NULL,
    Izvor               nvarchar(100)  NULL,          -- ko je potvrdio / odakle
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_tmpSinonimi_Kreirano DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_tmpSinonimi_Tip CHECK (Tip IN ('PARTNER','STANICA','NHM'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'planiranje.tmpSinonimi') AND name = N'UX_tmpSinonimi')
    CREATE UNIQUE INDEX UX_tmpSinonimi ON planiranje.tmpSinonimi (Tip, TekstNormalizovan);
GO

PRINT N'OP_910_tmp_Kreiranje: gotovo.';
SELECT t.name AS Tabela, SUM(p.rows) AS Redova
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
WHERE t.schema_id = SCHEMA_ID(N'planiranje') AND t.name LIKE N'tmp%'
GROUP BY t.name ORDER BY t.name;
GO
SET NOEXEC OFF;
GO
