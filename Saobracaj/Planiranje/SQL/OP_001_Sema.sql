/* =====================================================================
   OP_001_Sema.sql
   Planiranje (meni Transport u programu Saobracaj): odvojeni šifarnici
   i tabele planiranja u posebnoj šemi [planiranje], prefiks OP_.

   - Skripta je idempotentna: može se pokrenuti više puta.
   - Postojeće tabele i forme ostaju kakve jesu. U šemi [dbo] se ništa
     ne pravi i ništa ne menja.
   - ODVOJENI ŠIFARNICI (kopija + dopuna, pune ih nove forme):
     OP_Stanice, OP_SerijeLokomotiva, OP_Lokomotive, OP_Trase*, OP_Pruga*.
   - SAMO ČITANJE, koristi se postojeća tabela (bez kopije): Najava,
     Delavci, Bolovanje, Dopust*, Telegrami, Teretnica*, Partnerji,
     LocoTechnodis, OperativniPlanVozovi, DelavciLokMasin, DelavciPrugaMasin.
   - Prema dbo NEMA stranih ključeva; veza je u kolonama Izvorni* / DeSifra.
   Baza: SQL Server 2008 R2 ili noviji, i na nivou kompatibilnosti 80.
   Filtrirani indeksi traže SET ARITHABORT ON i QUOTED_IDENTIFIER ON.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
GO
IF SCHEMA_ID(N'planiranje') IS NULL EXEC (N'CREATE SCHEMA planiranje AUTHORIZATION dbo;');
GO

/* ---------------------------------------------------------------------
   A. Šifarnici (privremene kopije)
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_Stanice', N'U') IS NULL
CREATE TABLE planiranje.OP_Stanice
(
    ID              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Stanice PRIMARY KEY,
    Naziv           nvarchar(100)  NOT NULL,          -- latinica, kako se prikazuje
    NazivCir        nvarchar(100)  NULL,              -- original iz Registra / mreže
    UIC             nvarchar(10)   NULL,              -- šifra službenog mesta (UIC)
    Kod             nvarchar(20)   NULL,              -- dbo.stanice.Kod
    Drzava          nvarchar(10)   NULL,
    VrstaMesta      nvarchar(40)   NULL,              -- stanica, stajalište, rasputnica...
    Granicna        bit            NOT NULL CONSTRAINT DF_OP_Stanice_Granicna DEFAULT (0),
    Latitude        float          NULL,
    Longitude       float          NULL,
    Aktivna         bit            NOT NULL CONSTRAINT DF_OP_Stanice_Aktivna DEFAULT (1),
    Izvor           nvarchar(20)   NOT NULL CONSTRAINT DF_OP_Stanice_Izvor DEFAULT (N'RUCNO'), -- STANICE / REGISTAR / MREZA / RUCNO
    IzvorniID       int            NULL,              -- dbo.stanice.ID
    Kreirano        datetime2(0)   NOT NULL CONSTRAINT DF_OP_Stanice_Kreirano DEFAULT (SYSDATETIME()),
    Kreirao         nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Stanice_Kreirao DEFAULT (SUSER_SNAME()),
    RowVer          rowversion     NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Stanice_IzvorniID')
    CREATE UNIQUE INDEX UX_OP_Stanice_IzvorniID ON planiranje.OP_Stanice(IzvorniID) WHERE IzvorniID IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Stanice_UIC')
    CREATE INDEX IX_OP_Stanice_UIC ON planiranje.OP_Stanice(UIC) WHERE UIC IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Stanice_Naziv')
    CREATE INDEX IX_OP_Stanice_Naziv ON planiranje.OP_Stanice(Naziv);
GO

IF OBJECT_ID(N'planiranje.OP_SerijeLokomotiva', N'U') IS NULL
CREATE TABLE planiranje.OP_SerijeLokomotiva
(
    ID              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_SerijeLokomotiva PRIMARY KEY,
    Oznaka          nvarchar(20)   NOT NULL,          -- 185, 193, 187...
    Opis            nvarchar(100)  NULL,
    VrstaVuce       char(1)        NULL CONSTRAINT CK_OP_SerijeLokomotiva_Vuca CHECK (VrstaVuce IN ('E','D')), -- E električna, D dizel
    MasaT           decimal(10,2)  NULL,
    SnagaKW         decimal(10,2)  NULL,
    VmaxKmh         int            NULL,
    DuzinaM         decimal(6,2)   NULL,
    Aktivna         bit            NOT NULL CONSTRAINT DF_OP_SerijeLokomotiva_Aktivna DEFAULT (1),
    IzvorniID       int            NULL,              -- dbo.LokomotivaSerija.ID
    RowVer          rowversion     NOT NULL,
    CONSTRAINT UX_OP_SerijeLokomotiva_Oznaka UNIQUE (Oznaka)
);
GO

IF OBJECT_ID(N'planiranje.OP_Lokomotive', N'U') IS NULL
CREATE TABLE planiranje.OP_Lokomotive
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Lokomotive PRIMARY KEY,
    Broj                nvarchar(20)   NOT NULL,      -- 185 691
    Naziv               nvarchar(100)  NULL,
    SerijaID            int            NULL CONSTRAINT FK_OP_Lokomotive_Serija REFERENCES planiranje.OP_SerijeLokomotiva(ID),
    Vlasnistvo          nvarchar(10)   NOT NULL CONSTRAINT DF_OP_Lokomotive_Vlasnistvo DEFAULT (N'SOPSTVENA')
                        CONSTRAINT CK_OP_Lokomotive_Vlasnistvo CHECK (Vlasnistvo IN (N'SOPSTVENA', N'ZAKUP')),
    Zakupodavac         nvarchar(100)  NULL,
    ImaGPS              bit            NOT NULL CONSTRAINT DF_OP_Lokomotive_ImaGPS DEFAULT (0),
    TechnodisID         nvarchar(50)   NULL,          -- dbo.LocoTechnodis.IDLokomotive
    MaticnaStanicaID    int            NULL CONSTRAINT FK_OP_Lokomotive_MaticnaStanica REFERENCES planiranje.OP_Stanice(ID),
    MasaT               decimal(10,2)  NULL,          -- ako se razlikuje od serije
    Aktivna             bit            NOT NULL CONSTRAINT DF_OP_Lokomotive_Aktivna DEFAULT (1),
    IzvorniSmSifra      nvarchar(20)   NULL,          -- dbo.Mesta.SmSifra
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_Lokomotive_Kreirano DEFAULT (SYSDATETIME()),
    RowVer              rowversion     NOT NULL,
    CONSTRAINT UX_OP_Lokomotive_Broj UNIQUE (Broj)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Lokomotive_SmSifra')
    CREATE UNIQUE INDEX UX_OP_Lokomotive_SmSifra ON planiranje.OP_Lokomotive(IzvorniSmSifra) WHERE IzvorniSmSifra IS NOT NULL;
GO

-- Osoblje se NE kopira: čita se direktno iz dbo.Delavci (samo čitanje).
-- Ovde su samo podaci koje Delavci nema: matična stanica za planiranje i učešće u planiranju.
IF OBJECT_ID(N'planiranje.OP_OsobljePlan', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljePlan
(
    DeSifra             int            NOT NULL CONSTRAINT PK_OP_OsobljePlan PRIMARY KEY, -- dbo.Delavci.DeSifra
    MaticnaStanicaID    int            NULL CONSTRAINT FK_OP_OsobljePlan_MaticnaStanica REFERENCES planiranje.OP_Stanice(ID),
    UPlaniranju         bit            NOT NULL CONSTRAINT DF_OP_OsobljePlan_UPlaniranju DEFAULT (1),
    Napomena            nvarchar(300)  NULL,
    Izmenio             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_OsobljePlan_Izmenio DEFAULT (SUSER_SNAME()),
    RowVer              rowversion     NOT NULL
);
GO

/* ---------------------------------------------------------------------
   B. Registar pruga i mreža
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_RegistarVerzija', N'U') IS NULL
CREATE TABLE planiranje.OP_RegistarVerzija
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_RegistarVerzija PRIMARY KEY,
    Naziv               nvarchar(200)  NOT NULL,
    OriginalniNaziv     nvarchar(260)  NULL,
    Sha256              char(64)       NULL,
    Sadrzaj             varbinary(max) NULL,          -- originalna datoteka
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_RegistarVerzija_Status DEFAULT (N'NACRT')
                        CONSTRAINT CK_OP_RegistarVerzija_Status CHECK (Status IN (N'NACRT', N'VAZECA', N'ARHIVA', N'ODBACENA')),
    BrojRedova          int            NULL,
    Izvor               nvarchar(20)   NOT NULL CONSTRAINT DF_OP_RegistarVerzija_Izvor DEFAULT (N'EXCEL'), -- EXCEL / MIGRACIJA
    Uvezao              nvarchar(50)   NOT NULL CONSTRAINT DF_OP_RegistarVerzija_Uvezao DEFAULT (SUSER_SNAME()),
    Uvezeno             datetime2(0)   NOT NULL CONSTRAINT DF_OP_RegistarVerzija_Uvezeno DEFAULT (SYSDATETIME()),
    Potvrdio            nvarchar(50)   NULL,
    Potvrdjeno          datetime2(0)   NULL,
    Napomena            nvarchar(1000) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_RegistarVerzija_Vazeca')
    CREATE UNIQUE INDEX UX_OP_RegistarVerzija_Vazeca ON planiranje.OP_RegistarVerzija(Status) WHERE Status = N'VAZECA';
GO

IF OBJECT_ID(N'planiranje.OP_Pruga', N'U') IS NULL
CREATE TABLE planiranje.OP_Pruga
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Pruga PRIMARY KEY,
    RegistarVerzijaID   int            NOT NULL CONSTRAINT FK_OP_Pruga_Verzija REFERENCES planiranje.OP_RegistarVerzija(ID),
    RedniBroj           int            NULL,          -- redni broj u Registru (1, 2, 29...)
    IdentBroj           nvarchar(10)   NULL,          -- identifikacioni broj iz knjižica (102, 125...)
    Naziv               nvarchar(300)  NOT NULL,
    NazivCir            nvarchar(300)  NULL,
    Kategorija          nvarchar(20)   NULL,          -- MAGISTRALNA / REGIONALNA / LOKALNA / MANIPULATIVNA
    Elektrificirana     bit            NULL,
    Aktivna             bit            NOT NULL CONSTRAINT DF_OP_Pruga_Aktivna DEFAULT (1),
    IzvorniID           int            NULL           -- dbo.Pruga.ID
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Pruga_Verzija')
    CREATE INDEX IX_OP_Pruga_Verzija ON planiranje.OP_Pruga(RegistarVerzijaID, RedniBroj);
GO

IF OBJECT_ID(N'planiranje.OP_PrugaMesto', N'U') IS NULL
CREATE TABLE planiranje.OP_PrugaMesto
(
    ID                      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PrugaMesto PRIMARY KEY,
    PrugaID                 int            NOT NULL CONSTRAINT FK_OP_PrugaMesto_Pruga REFERENCES planiranje.OP_Pruga(ID) ON DELETE CASCADE,
    RB                      int            NOT NULL,  -- redosled na pruzi
    StanicaID               int            NULL CONSTRAINT FK_OP_PrugaMesto_Stanica REFERENCES planiranje.OP_Stanice(ID),
    NazivOriginal           nvarchar(150)  NULL,      -- kako piše u Registru (ćirilica)
    Naziv                   nvarchar(150)  NOT NULL,  -- latinica
    UIC                     nvarchar(10)   NULL,
    VrstaMesta              nvarchar(40)   NULL,
    StacionazaKM            decimal(10,3)  NULL,
    StacionazaM             int            NULL,
    RastojanjeDoSledecegM   int            NULL,      -- m do sledećeg mesta na pruzi
    Kolosecnost             tinyint        NULL,      -- 1 jednokolosečna, 2 dvokolosečna
    Klasa                   nvarchar(10)   NULL,      -- D4, D3...
    Kategorija              nvarchar(10)   NULL,      -- M, R, L
    VmaxDesni               int            NULL,
    VmaxLevi                int            NULL,
    MaxDuzinaVozaAB         int            NULL,      -- m, smer A→B
    KoloseciAB              nvarchar(30)   NULL,
    MaxDuzinaVozaBA         int            NULL,      -- m, smer B→A
    KoloseciBA              nvarchar(30)   NULL,
    NacinRegulisanja        nvarchar(50)   NULL,
    NacinOsiguranja         nvarchar(50)   NULL,
    Vaga                    nvarchar(20)   NULL,
    Rampa                   nvarchar(20)   NULL,
    Posednutost             nvarchar(10)   NULL,
    OtvorenoZa              nvarchar(10)   NULL,      -- P / T / P/T
    MinPoluprecnikM         int            NULL,
    NagibStanice            decimal(6,2)   NULL,      -- ‰
    MerodavniUspon          decimal(6,2)   NULL,
    MerodavniPad            decimal(6,2)   NULL,
    MerodavniOtporUspon     decimal(8,2)   NULL,      -- daN
    MerodavniOtporPad       decimal(8,2)   NULL,
    OsovinskoOpterecenjeT   decimal(6,2)   NULL,
    TovarniProfil           nvarchar(20)   NULL,
    NadmorskaVisina         decimal(8,2)   NULL,
    OsOtpor                 nvarchar(50)   NULL,      -- kolona iz dbo.PrugaStavke, značenje proveriti
    Aktivno                 bit            NOT NULL CONSTRAINT DF_OP_PrugaMesto_Aktivno DEFAULT (1),
    Promena                 nvarchar(20)   NULL,      -- NOVO / IZMENJENO / NEMA_U_DATOTECI (pregled razlika)
    OpisPromene             nvarchar(400)  NULL,
    PovezanoPo              nvarchar(10)   NULL,      -- NAZIV / KOORDINATA / RUCNO (veza sa OP_Stanice)
    CONSTRAINT UX_OP_PrugaMesto_RB UNIQUE (PrugaID, RB)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_PrugaMesto_Stanica')
    CREATE INDEX IX_OP_PrugaMesto_Stanica ON planiranje.OP_PrugaMesto(StanicaID) INCLUDE (PrugaID, RB);
GO

IF OBJECT_ID(N'planiranje.OP_MrezaVeza', N'U') IS NULL
CREATE TABLE planiranje.OP_MrezaVeza
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_MrezaVeza PRIMARY KEY,
    StanicaAID          int            NOT NULL CONSTRAINT FK_OP_MrezaVeza_A REFERENCES planiranje.OP_Stanice(ID),
    StanicaBID          int            NOT NULL CONSTRAINT FK_OP_MrezaVeza_B REFERENCES planiranje.OP_Stanice(ID),
    DuzinaKm            decimal(8,2)   NOT NULL,
    PrugaID             int            NULL CONSTRAINT FK_OP_MrezaVeza_Pruga REFERENCES planiranje.OP_Pruga(ID),
    Elektrificirana     bit            NULL,
    Izvor               nvarchar(20)   NOT NULL CONSTRAINT DF_OP_MrezaVeza_Izvor DEFAULT (N'MREZA'), -- MREZA / REGISTAR / RUCNO
    Aktivna             bit            NOT NULL CONSTRAINT DF_OP_MrezaVeza_Aktivna DEFAULT (1),
    CONSTRAINT CK_OP_MrezaVeza_Razlicite CHECK (StanicaAID <> StanicaBID),
    CONSTRAINT CK_OP_MrezaVeza_Duzina CHECK (DuzinaKm > 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_MrezaVeza_Par')
    CREATE UNIQUE INDEX UX_OP_MrezaVeza_Par ON planiranje.OP_MrezaVeza(StanicaAID, StanicaBID);
GO

/* ---------------------------------------------------------------------
   C. Trase sa verzijama
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_TraseVerzija', N'U') IS NULL
CREATE TABLE planiranje.OP_TraseVerzija
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TraseVerzija PRIMARY KEY,
    Naziv               nvarchar(200)  NOT NULL,      -- "Trase 2027"
    Godina              int            NOT NULL,
    VaziOd              date           NOT NULL,
    VaziDo              date           NOT NULL,
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_TraseVerzija_Status DEFAULT (N'NACRT')
                        CONSTRAINT CK_OP_TraseVerzija_Status CHECK (Status IN (N'NACRT', N'VAZECA', N'ARHIVA', N'ODBACENA')),
    Izvor               nvarchar(30)   NOT NULL CONSTRAINT DF_OP_TraseVerzija_Izvor DEFAULT (N'EXCEL'), -- EXCEL / MIGRACIJA / IZMENA
    PrethodnaVerzijaID  int            NULL CONSTRAINT FK_OP_TraseVerzija_Prethodna REFERENCES planiranje.OP_TraseVerzija(ID),
    OriginalniNaziv     nvarchar(260)  NULL,
    Sha256              char(64)       NULL,
    Sadrzaj             varbinary(max) NULL,
    Uvezao              nvarchar(50)   NOT NULL CONSTRAINT DF_OP_TraseVerzija_Uvezao DEFAULT (SUSER_SNAME()),
    Uvezeno             datetime2(0)   NOT NULL CONSTRAINT DF_OP_TraseVerzija_Uvezeno DEFAULT (SYSDATETIME()),
    Objavio             nvarchar(50)   NULL,
    Objavljeno          datetime2(0)   NULL,
    IzvorniDokumentID   bigint         NULL,          -- dbo.OperativniPlanTraseDokument.ID
    CONSTRAINT CK_OP_TraseVerzija_Period CHECK (VaziDo >= VaziOd)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_TraseVerzija_VazecaGodina')
    CREATE UNIQUE INDEX UX_OP_TraseVerzija_VazecaGodina ON planiranje.OP_TraseVerzija(Godina) WHERE Status = N'VAZECA';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_TraseVerzija_Dokument')
    CREATE UNIQUE INDEX UX_OP_TraseVerzija_Dokument ON planiranje.OP_TraseVerzija(IzvorniDokumentID) WHERE IzvorniDokumentID IS NOT NULL;
GO

IF OBJECT_ID(N'planiranje.OP_Trase', N'U') IS NULL
CREATE TABLE planiranje.OP_Trase
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Trase PRIMARY KEY,
    VerzijaID           int            NULL CONSTRAINT FK_OP_Trase_Verzija REFERENCES planiranje.OP_TraseVerzija(ID) ON DELETE CASCADE, -- NULL samo za ad hoc
    BrojVoza            nvarchar(20)   NOT NULL,
    PocetnaStanicaID    int            NULL CONSTRAINT FK_OP_Trase_Pocetna REFERENCES planiranje.OP_Stanice(ID),
    KrajnjaStanicaID    int            NULL CONSTRAINT FK_OP_Trase_Krajnja REFERENCES planiranje.OP_Stanice(ID),
    PocetnaNaziv        nvarchar(100)  NOT NULL,      -- tekst iz datoteke
    KrajnjaNaziv        nvarchar(100)  NOT NULL,
    Relacija            nvarchar(300)  NULL,
    Rang                nvarchar(10)   NULL,
    VremePolaska        time(0)        NULL,
    VremeDolaska        time(0)        NULL,
    DanPlus             tinyint        NOT NULL CONSTRAINT DF_OP_Trase_DanPlus DEFAULT (0), -- dolazak sledećeg dana
    TrajanjeMin         int            NULL,
    DaniSaobracaja      nvarchar(50)   NULL,
    MaxMasaT            decimal(10,2)  NULL,
    MaxDuzinaM          decimal(10,2)  NULL,
    ProcenatKocenja     decimal(6,2)   NULL,
    Cena                decimal(18,2)  NULL,
    RastojanjeKm        decimal(10,2)  NULL,
    SerijaLokomotive    nvarchar(50)   NULL,          -- tekst iz datoteke
    Napomena            nvarchar(1000) NULL,
    AdHoc               bit            NOT NULL CONSTRAINT DF_OP_Trase_AdHoc DEFAULT (0),
    AdHocNajavaID       int            NULL,          -- dbo.Najava.ID za ad hoc trasu
    AdHocVaziOd         date           NULL,
    AdHocVaziDo         date           NULL,
    AdHocStatus         nvarchar(10)   NULL CONSTRAINT CK_OP_Trase_AdHocStatus CHECK (AdHocStatus IN (N'ZAHTEV', N'ODOBRENA', N'ODBIJENA')), -- "Nemamo trasu" → zahtev
    StanjeRazlike       nvarchar(10)   NULL CONSTRAINT CK_OP_Trase_Stanje CHECK (StanjeRazlike IN (N'NOVA', N'IZMENJENA', N'UKINUTA', N'ISTA', N'GRESKA')),
    OpisPromene         nvarchar(400)  NULL,
    Greska              nvarchar(400)  NULL,
    IzvornaTrasaID      int            NULL,          -- dbo.Trase.ID (godišnji šifarnik za obračun)
    IzvorniRouteID      bigint         NULL,          -- dbo.OperativniPlanTrasa.ID
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Trase_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_Trase_Kreirano DEFAULT (SYSDATETIME()),
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_Trase_AdHoc CHECK (AdHoc = 1 OR VerzijaID IS NOT NULL)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Trase_VerzijaBroj')
    CREATE INDEX IX_OP_Trase_VerzijaBroj ON planiranje.OP_Trase(VerzijaID, BrojVoza);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Trase_Relacija')
    CREATE INDEX IX_OP_Trase_Relacija ON planiranje.OP_Trase(PocetnaStanicaID, KrajnjaStanicaID) INCLUDE (VerzijaID, BrojVoza, MaxMasaT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Trase_Route')
    CREATE UNIQUE INDEX UX_OP_Trase_Route ON planiranje.OP_Trase(IzvorniRouteID) WHERE IzvorniRouteID IS NOT NULL;
GO

IF OBJECT_ID(N'planiranje.OP_TrasaStanice', N'U') IS NULL
CREATE TABLE planiranje.OP_TrasaStanice
(
    TrasaID             int            NOT NULL CONSTRAINT FK_OP_TrasaStanice_Trasa REFERENCES planiranje.OP_Trase(ID) ON DELETE CASCADE,
    RB                  int            NOT NULL,
    StanicaID           int            NOT NULL CONSTRAINT FK_OP_TrasaStanice_Stanica REFERENCES planiranje.OP_Stanice(ID),
    PrugaID             int            NULL CONSTRAINT FK_OP_TrasaStanice_Pruga REFERENCES planiranje.OP_Pruga(ID),
    CONSTRAINT PK_OP_TrasaStanice PRIMARY KEY (TrasaID, RB)
);
GO

/* ---------------------------------------------------------------------
   D. Planiranje lokomotiva
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_Parametri', N'U') IS NULL
CREATE TABLE planiranje.OP_Parametri
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Parametri PRIMARY KEY,
    Kljuc               nvarchar(50)   NOT NULL,      -- OBRADA_PRIJEM_MIN, VOZNJA_MIN...
    StanicaID           int            NULL CONSTRAINT FK_OP_Parametri_Stanica REFERENCES planiranje.OP_Stanice(ID),
    StanicaDoID         int            NULL CONSTRAINT FK_OP_Parametri_StanicaDo REFERENCES planiranje.OP_Stanice(ID),
    SerijaID            int            NULL CONSTRAINT FK_OP_Parametri_Serija REFERENCES planiranje.OP_SerijeLokomotiva(ID),
    Vrednost            decimal(18,4)  NOT NULL,
    Jedinica            nvarchar(10)   NULL,
    Opis                nvarchar(300)  NULL,
    Izvor               nvarchar(20)   NULL,          -- RUCNO / KNJIZICA (vreme po relaciji iz knjižica reda vožnje)
    VaziOd              date           NOT NULL CONSTRAINT DF_OP_Parametri_VaziOd DEFAULT ('2000-01-01'), -- verzije parametra
    VaziDo              date           NULL,
    Izmenio             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Parametri_Izmenio DEFAULT (SUSER_SNAME()),
    Izmenjeno           datetime2(0)   NOT NULL CONSTRAINT DF_OP_Parametri_Izmenjeno DEFAULT (SYSDATETIME()),
    RowVer              rowversion     NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Parametri_Kljuc')
    CREATE UNIQUE INDEX UX_OP_Parametri_Kljuc ON planiranje.OP_Parametri(Kljuc, StanicaID, StanicaDoID, SerijaID, VaziOd);
GO

IF OBJECT_ID(N'planiranje.OP_TablicaMasa', N'U') IS NULL
CREATE TABLE planiranje.OP_TablicaMasa
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TablicaMasa PRIMARY KEY,
    SerijaID            int            NOT NULL CONSTRAINT FK_OP_TablicaMasa_Serija REFERENCES planiranje.OP_SerijeLokomotiva(ID),
    PrugaID             int            NULL CONSTRAINT FK_OP_TablicaMasa_Pruga REFERENCES planiranje.OP_Pruga(ID),
    StanicaOdID         int            NULL CONSTRAINT FK_OP_TablicaMasa_Od REFERENCES planiranje.OP_Stanice(ID),
    StanicaDoID         int            NULL CONSTRAINT FK_OP_TablicaMasa_Do REFERENCES planiranje.OP_Stanice(ID),
    Smer                char(2)        NULL CONSTRAINT CK_OP_TablicaMasa_Smer CHECK (Smer IN ('AB','BA')),
    BrojLokomotiva      tinyint        NOT NULL CONSTRAINT DF_OP_TablicaMasa_Broj DEFAULT (1),
    Sloj                nvarchar(10)   NOT NULL CONSTRAINT DF_OP_TablicaMasa_Sloj DEFAULT (N'RUCNA')
                        CONSTRAINT CK_OP_TablicaMasa_Sloj CHECK (Sloj IN (N'RUCNA', N'FIZIKA', N'NAUCENA')), -- 3 sloja; ručna ima prednost
    MaxMasaT            decimal(10,2)  NOT NULL,
    VaziOd              date           NULL,
    VaziDo              date           NULL,
    Napomena            nvarchar(300)  NULL,
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_TablicaMasa_Opseg CHECK (PrugaID IS NOT NULL OR (StanicaOdID IS NOT NULL AND StanicaDoID IS NOT NULL)),
    CONSTRAINT CK_OP_TablicaMasa_Masa CHECK (MaxMasaT > 0)
);
GO

IF OBJECT_ID(N'planiranje.OP_LokomotivaZauzetost', N'U') IS NULL
CREATE TABLE planiranje.OP_LokomotivaZauzetost
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_LokomotivaZauzetost PRIMARY KEY,
    LokomotivaID        int            NOT NULL CONSTRAINT FK_OP_LokomotivaZauzetost_Lok REFERENCES planiranje.OP_Lokomotive(ID),
    Vrsta               nvarchar(10)   NOT NULL CONSTRAINT CK_OP_LokomotivaZauzetost_Vrsta CHECK (Vrsta IN (N'SERVIS', N'NAMENSKA', N'PROJEKAT', N'OSTALO')),
    Od                  datetime2(0)   NOT NULL,
    Do                  datetime2(0)   NOT NULL,
    Projekat            nvarchar(100)  NULL,          -- za NAMENSKA/PROJEKAT: samo poslovi tog projekta
    Napomena            nvarchar(300)  NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_LokomotivaZauzetost_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_LokomotivaZauzetost_Kreirano DEFAULT (SYSDATETIME()),
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_LokomotivaZauzetost_Period CHECK (Do > Od)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_LokomotivaZauzetost_Lok')
    CREATE INDEX IX_OP_LokomotivaZauzetost_Lok ON planiranje.OP_LokomotivaZauzetost(LokomotivaID, Od, Do);
GO

IF OBJECT_ID(N'planiranje.OP_Posao', N'U') IS NULL
CREATE TABLE planiranje.OP_Posao
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Posao PRIMARY KEY,
    NajavaID            int            NOT NULL,      -- dbo.Najava.ID (samo čitanje)
    RBDeonice           tinyint        NOT NULL CONSTRAINT DF_OP_Posao_RB DEFAULT (1), -- ako se najava vozi u više delova
    TrasaID             int            NULL CONSTRAINT FK_OP_Posao_Trasa REFERENCES planiranje.OP_Trase(ID),
    PrijemStanicaID     int            NULL CONSTRAINT FK_OP_Posao_Prijem REFERENCES planiranje.OP_Stanice(ID),
    PredajaStanicaID    int            NULL CONSTRAINT FK_OP_Posao_Predaja REFERENCES planiranje.OP_Stanice(ID),
    PlaniraniPrijem     datetime2(0)   NULL,
    PlaniraniPolazak    datetime2(0)   NULL,
    PlaniraniDolazak    datetime2(0)   NULL,
    PlaniranaPredaja    datetime2(0)   NULL,
    StvarniPolazak      datetime2(0)   NULL,
    StvarniDolazak      datetime2(0)   NULL,
    Faza                tinyint        NOT NULL CONSTRAINT DF_OP_Posao_Faza DEFAULT (1)
                        CONSTRAINT CK_OP_Posao_Faza CHECK (Faza BETWEEN 1 AND 7),
                        -- 1 Najava, 2 Plan lokomotive, 3 Osoblje i zadatak, 4 Prijem i pregled,
                        -- 5 U putu, 6 Predaja u toku, 7 Predato
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_Posao_Status DEFAULT (N'AKTIVAN')
                        CONSTRAINT CK_OP_Posao_Status CHECK (Status IN (N'AKTIVAN', N'OTKAZAN', N'ZATVOREN')),
    PotrebnoLokomotiva  tinyint        NULL,          -- predlog iz tablice masa
    Projekat            nvarchar(100)  NULL,          -- za namenske lokomotive
    OperativniVozID     bigint         NULL,          -- dbo.OperativniPlanVozovi.ID (veza sa Dispečerom)
    Napomena            nvarchar(1000) NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Posao_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_Posao_Kreirano DEFAULT (SYSDATETIME()),
    Izmenio             nvarchar(50)   NULL,
    Izmenjeno           datetime2(0)   NULL,
    RowVer              rowversion     NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Posao_NajavaDeonica')
    CREATE UNIQUE INDEX UX_OP_Posao_NajavaDeonica ON planiranje.OP_Posao(NajavaID, RBDeonice) WHERE Status <> N'OTKAZAN';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Posao_Period')
    CREATE INDEX IX_OP_Posao_Period ON planiranje.OP_Posao(PlaniraniPrijem, PlaniranaPredaja) INCLUDE (NajavaID, Faza, Status);
GO

IF OBJECT_ID(N'planiranje.OP_PosaoLokomotiva', N'U') IS NULL
CREATE TABLE planiranje.OP_PosaoLokomotiva
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PosaoLokomotiva PRIMARY KEY,
    PosaoID             int            NOT NULL CONSTRAINT FK_OP_PosaoLokomotiva_Posao REFERENCES planiranje.OP_Posao(ID) ON DELETE CASCADE,
    LokomotivaID        int            NOT NULL CONSTRAINT FK_OP_PosaoLokomotiva_Lok REFERENCES planiranje.OP_Lokomotive(ID),
    RB                  tinyint        NOT NULL CONSTRAINT DF_OP_PosaoLokomotiva_RB DEFAULT (1), -- "1 od 2"
    Vucna               bit            NOT NULL CONSTRAINT DF_OP_PosaoLokomotiva_Vucna DEFAULT (1),
    Od                  datetime2(0)   NOT NULL,      -- zauzetost sa obradom
    Do                  datetime2(0)   NOT NULL,
    PraznaVoznjaMin     int            NULL,
    Potvrdio            nvarchar(50)   NULL,
    Potvrdjeno          datetime2(0)   NULL,
    CONSTRAINT UX_OP_PosaoLokomotiva UNIQUE (PosaoID, LokomotivaID),
    CONSTRAINT CK_OP_PosaoLokomotiva_Period CHECK (Do > Od)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_PosaoLokomotiva_Lok')
    CREATE INDEX IX_OP_PosaoLokomotiva_Lok ON planiranje.OP_PosaoLokomotiva(LokomotivaID, Od, Do);
GO

-- Ruta posla sklopljena iz Registra (graf stanica). Deonica bez naše trase = "Nemamo trasu".
IF OBJECT_ID(N'planiranje.OP_PosaoRuta', N'U') IS NULL
CREATE TABLE planiranje.OP_PosaoRuta
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PosaoRuta PRIMARY KEY,
    PosaoID             int            NOT NULL CONSTRAINT FK_OP_PosaoRuta_Posao REFERENCES planiranje.OP_Posao(ID) ON DELETE CASCADE,
    RB                  int            NOT NULL,
    StanicaOdID         int            NOT NULL CONSTRAINT FK_OP_PosaoRuta_Od REFERENCES planiranje.OP_Stanice(ID),
    StanicaDoID         int            NOT NULL CONSTRAINT FK_OP_PosaoRuta_Do REFERENCES planiranje.OP_Stanice(ID),
    PrugaID             int            NULL CONSTRAINT FK_OP_PosaoRuta_Pruga REFERENCES planiranje.OP_Pruga(ID),
    DuzinaKm            decimal(8,2)   NULL,
    TrasaID             int            NULL CONSTRAINT FK_OP_PosaoRuta_Trasa REFERENCES planiranje.OP_Trase(ID),
    Stanje              nvarchar(15)   NOT NULL CONSTRAINT DF_OP_PosaoRuta_Stanje DEFAULT (N'IMA_TRASU')
                        CONSTRAINT CK_OP_PosaoRuta_Stanje CHECK (Stanje IN (N'IMA_TRASU', N'NEMAMO_TRASU', N'ZAHTEV_ADHOC', N'ZATVOR')),
    Napomena            nvarchar(300)  NULL,          -- npr. broj telegrama o zatvoru
    CONSTRAINT UX_OP_PosaoRuta UNIQUE (PosaoID, RB)
);
GO

IF OBJECT_ID(N'planiranje.OP_PlanVerzija', N'U') IS NULL
CREATE TABLE planiranje.OP_PlanVerzija
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PlanVerzija PRIMARY KEY,
    Vrsta               nvarchar(10)   NOT NULL CONSTRAINT CK_OP_PlanVerzija_Vrsta CHECK (Vrsta IN (N'NEDELJNI', N'OPERATIVNI')),
    PeriodOd            date           NOT NULL,
    PeriodDo            date           NOT NULL,
    BrojVerzije         int            NOT NULL,
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_PlanVerzija_Status DEFAULT (N'NACRT')
                        CONSTRAINT CK_OP_PlanVerzija_Status CHECK (Status IN (N'NACRT', N'POTVRDJEN', N'ARHIVA')),
    Napomena            nvarchar(1000) NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_PlanVerzija_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_PlanVerzija_Kreirano DEFAULT (SYSDATETIME()),
    Potvrdio            nvarchar(50)   NULL,
    Potvrdjeno          datetime2(0)   NULL,
    CONSTRAINT UX_OP_PlanVerzija UNIQUE (Vrsta, PeriodOd, BrojVerzije),
    CONSTRAINT CK_OP_PlanVerzija_Period CHECK (PeriodDo >= PeriodOd)
);
GO

-- Snimak dodela lokomotiva u trenutku "Sačuvaj verziju".
IF OBJECT_ID(N'planiranje.OP_PlanVerzijaStavka', N'U') IS NULL
CREATE TABLE planiranje.OP_PlanVerzijaStavka
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PlanVerzijaStavka PRIMARY KEY,
    PlanVerzijaID       int            NOT NULL CONSTRAINT FK_OP_PlanVerzijaStavka_Verzija REFERENCES planiranje.OP_PlanVerzija(ID) ON DELETE CASCADE,
    PosaoID             int            NOT NULL CONSTRAINT FK_OP_PlanVerzijaStavka_Posao REFERENCES planiranje.OP_Posao(ID),
    LokomotivaID        int            NULL CONSTRAINT FK_OP_PlanVerzijaStavka_Lok REFERENCES planiranje.OP_Lokomotive(ID), -- NULL = bez lokomotive
    RB                  tinyint        NULL,
    Od                  datetime2(0)   NULL,
    Do                  datetime2(0)   NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_PlanVerzijaStavka_Verzija')
    CREATE INDEX IX_OP_PlanVerzijaStavka_Verzija ON planiranje.OP_PlanVerzijaStavka(PlanVerzijaID);
GO

/* ---------------------------------------------------------------------
   E. Osoblje
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_OsobljeKvalifikacija', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljeKvalifikacija
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_OsobljeKvalifikacija PRIMARY KEY,
    DeSifra             int            NOT NULL CONSTRAINT FK_OP_OsobljeKvalifikacija_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra) ON DELETE CASCADE,
    Vrsta               nvarchar(10)   NOT NULL CONSTRAINT CK_OP_OsobljeKvalifikacija_Vrsta CHECK (Vrsta IN (N'SERIJA', N'PRUGA')),
    SerijaID            int            NULL CONSTRAINT FK_OP_OsobljeKvalifikacija_Serija REFERENCES planiranje.OP_SerijeLokomotiva(ID),
    PrugaID             int            NULL CONSTRAINT FK_OP_OsobljeKvalifikacija_Pruga REFERENCES planiranje.OP_Pruga(ID),
    VaziDo              date           NULL,
    Napomena            nvarchar(300)  NULL,
    CONSTRAINT CK_OP_OsobljeKvalifikacija_Jedno CHECK ((Vrsta = N'SERIJA' AND SerijaID IS NOT NULL AND PrugaID IS NULL)
                                                    OR (Vrsta = N'PRUGA' AND PrugaID IS NOT NULL AND SerijaID IS NULL))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_OsobljeKvalifikacija')
    CREATE UNIQUE INDEX UX_OP_OsobljeKvalifikacija ON planiranje.OP_OsobljeKvalifikacija(DeSifra, Vrsta, SerijaID, PrugaID);
GO

IF OBJECT_ID(N'planiranje.OP_Turnus', N'U') IS NULL
CREATE TABLE planiranje.OP_Turnus
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Turnus PRIMARY KEY,
    Naziv               nvarchar(100)  NOT NULL,      -- "Dnevna 06–18, 2 rada / 2 odmor"
    Pocetak             time(0)        NOT NULL,
    TrajanjeMin         int            NOT NULL CONSTRAINT CK_OP_Turnus_Trajanje CHECK (TrajanjeMin BETWEEN 1 AND 1440),
    DanaRada            tinyint        NOT NULL CONSTRAINT CK_OP_Turnus_Rad CHECK (DanaRada >= 1),
    DanaOdmora          tinyint        NOT NULL,
    Nocna               bit            NOT NULL CONSTRAINT DF_OP_Turnus_Nocna DEFAULT (0),
    Aktivan             bit            NOT NULL CONSTRAINT DF_OP_Turnus_Aktivan DEFAULT (1),
    CONSTRAINT UX_OP_Turnus_Naziv UNIQUE (Naziv)
);
GO

IF OBJECT_ID(N'planiranje.OP_OsobljeTurnus', N'U') IS NULL
CREATE TABLE planiranje.OP_OsobljeTurnus
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_OsobljeTurnus PRIMARY KEY,
    DeSifra             int            NOT NULL CONSTRAINT FK_OP_OsobljeTurnus_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra) ON DELETE CASCADE,
    TurnusID            int            NOT NULL CONSTRAINT FK_OP_OsobljeTurnus_Turnus REFERENCES planiranje.OP_Turnus(ID),
    Od                  date           NOT NULL,      -- prvi radni dan ciklusa
    Do                  date           NULL,
    CONSTRAINT CK_OP_OsobljeTurnus_Period CHECK (Do IS NULL OR Do >= Od)
);
GO

IF OBJECT_ID(N'planiranje.OP_Smena', N'U') IS NULL
CREATE TABLE planiranje.OP_Smena
(
    ID                      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Smena PRIMARY KEY,
    DeSifra                 int            NOT NULL CONSTRAINT FK_OP_Smena_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra) ON DELETE CASCADE,
    Datum                   date           NOT NULL,
    Vrsta                   nvarchar(10)   NOT NULL CONSTRAINT CK_OP_Smena_Vrsta CHECK (Vrsta IN (N'DNEVNA', N'NOCNA', N'SLOBODAN')),
    Od                      datetime2(0)   NULL,
    Do                      datetime2(0)   NULL,
    Izvor                   nvarchar(10)   NOT NULL CONSTRAINT DF_OP_Smena_Izvor DEFAULT (N'TURNUS')
                            CONSTRAINT CK_OP_Smena_Izvor CHECK (Izvor IN (N'TURNUS', N'RUCNO')),
    LokacijaPosleStanicaID  int            NULL CONSTRAINT FK_OP_Smena_Lokacija REFERENCES planiranje.OP_Stanice(ID),
    Izmenio                 nvarchar(50)   NULL,
    CONSTRAINT UX_OP_Smena UNIQUE (DeSifra, Datum)
);
GO

-- Samo odsustva kojih nema u Bolovanje / Dopust (slobodan dan, ostalo). Pregled svih: OP_vOdsustvo.
IF OBJECT_ID(N'planiranje.OP_Odsustvo', N'U') IS NULL
CREATE TABLE planiranje.OP_Odsustvo
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Odsustvo PRIMARY KEY,
    DeSifra             int            NOT NULL CONSTRAINT FK_OP_Odsustvo_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra) ON DELETE CASCADE,
    Vrsta               nvarchar(10)   NOT NULL CONSTRAINT CK_OP_Odsustvo_Vrsta CHECK (Vrsta IN (N'GODISNJI', N'BOLOVANJE', N'SLOBODAN', N'OSTALO')),
    Od                  date           NOT NULL,
    Do                  date           NOT NULL,
    Napomena            nvarchar(300)  NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Odsustvo_Kreirao DEFAULT (SUSER_SNAME()),
    CONSTRAINT CK_OP_Odsustvo_Period CHECK (Do >= Od)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Odsustvo_Osoba')
    CREATE INDEX IX_OP_Odsustvo_Osoba ON planiranje.OP_Odsustvo(DeSifra, Od, Do);
GO

-- Bez tableta: dispečer šalje zadatak (mail/telefon) i sam upisuje odgovor. DeSifra = dbo.Delavci.DeSifra.
IF OBJECT_ID(N'planiranje.OP_Zadatak', N'U') IS NULL
CREATE TABLE planiranje.OP_Zadatak
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Zadatak PRIMARY KEY,
    PosaoID             int            NOT NULL CONSTRAINT FK_OP_Zadatak_Posao REFERENCES planiranje.OP_Posao(ID) ON DELETE CASCADE,
    DeSifra             int            NOT NULL CONSTRAINT FK_OP_Zadatak_Osoba REFERENCES planiranje.OP_OsobljePlan(DeSifra),
    Uloga               nvarchar(20)   NOT NULL CONSTRAINT CK_OP_Zadatak_Uloga CHECK (Uloga IN (N'MASINOVODJA', N'PREGLEDAC_PRIJEM', N'PREGLEDAC_PREDAJA')),
    StanicaID           int            NULL CONSTRAINT FK_OP_Zadatak_Stanica REFERENCES planiranje.OP_Stanice(ID),
    Od                  datetime2(0)   NOT NULL,
    Do                  datetime2(0)   NOT NULL,
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_Zadatak_Status DEFAULT (N'PREDLOZEN')
                        CONSTRAINT CK_OP_Zadatak_Status CHECK (Status IN (N'PREDLOZEN', N'POSLAT', N'PRIHVACEN', N'ODBIJEN', N'OTKAZAN')),
    RazlogOdbijanja     nvarchar(200)  NULL,
    NapomenaZaOsobu     nvarchar(1000) NULL,
    Poslao              nvarchar(50)   NULL,
    Poslato             datetime2(0)   NULL,
    Odgovoreno          datetime2(0)   NULL,
    UneoOdgovor         nvarchar(50)   NULL,
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_Zadatak_Period CHECK (Do > Od)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Zadatak_Osoba')
    CREATE INDEX IX_OP_Zadatak_Osoba ON planiranje.OP_Zadatak(DeSifra, Od, Do) INCLUDE (Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_Zadatak_Aktivan')
    CREATE UNIQUE INDEX UX_OP_Zadatak_Aktivan ON planiranje.OP_Zadatak(PosaoID, Uloga, DeSifra) WHERE Status IN (N'PREDLOZEN', N'POSLAT', N'PRIHVACEN');
GO

/* ---------------------------------------------------------------------
   F. Tok posla
   --------------------------------------------------------------------- */

IF OBJECT_ID(N'planiranje.OP_PosaoDogadjaj', N'U') IS NULL
CREATE TABLE planiranje.OP_PosaoDogadjaj
(
    ID                  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_PosaoDogadjaj PRIMARY KEY,
    PosaoID             int            NOT NULL CONSTRAINT FK_OP_PosaoDogadjaj_Posao REFERENCES planiranje.OP_Posao(ID) ON DELETE CASCADE,
    Vreme               datetime2(0)   NOT NULL CONSTRAINT DF_OP_PosaoDogadjaj_Vreme DEFAULT (SYSDATETIME()),
    Tip                 nvarchar(30)   NOT NULL,      -- NAJAVA, LOKOMOTIVE_POTVRDJENE, ZADATAK_POSLAT, ODGOVOR, FAZA, TERETNICA...
    Opis                nvarchar(500)  NOT NULL,
    Korisnik            nvarchar(50)   NOT NULL CONSTRAINT DF_OP_PosaoDogadjaj_Korisnik DEFAULT (SUSER_SNAME()),
    PodaciJson          nvarchar(max)  NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_PosaoDogadjaj_Posao')
    CREATE INDEX IX_OP_PosaoDogadjaj_Posao ON planiranje.OP_PosaoDogadjaj(PosaoID, Vreme);
GO

/* ---------------------------------------------------------------------
   G. Pogledi
   --------------------------------------------------------------------- */

-- Najave za planiranje (samo čitanje dbo.Najava).
-- Predviđeno vreme prijema je dbo.Najava.PredvidjenoPrimanje (forma Najava: dtpPredvidjenoPrimanje).
-- U inostranstvu relacija ide preko OtpravnaGr / Uputnagr (granični prelaz).
IF OBJECT_ID(N'planiranje.OP_vNajava', N'V') IS NOT NULL DROP VIEW planiranje.OP_vNajava;
GO
CREATE VIEW planiranje.OP_vNajava
AS
SELECT  n.ID                    AS NajavaID,
        n.BrojNajave,
        n.Otpravna              AS OtpravnaID,
        n.Uputna                AS UputnaID,
        n.OtpravnaGr            AS OtpravnaGrID,
        n.Uputnagr              AS UputnaGrID,
        -- relacija za planiranje u Srbiji: granični prelaz ako postoji, inače stanica iz najave
        COALESCE(NULLIF(n.OtpravnaGr, 0), n.Otpravna) AS PlanOtpravnaID,
        COALESCE(NULLIF(n.Uputnagr, 0),   n.Uputna)   AS PlanUputnaID,
        RTRIM(s1.Opis)          AS Otpravna,
        RTRIM(s2.Opis)          AS Uputna,
        RTRIM(g1.Opis)          AS OtpravnaGr,
        RTRIM(g2.Opis)          AS UputnaGr,
        n.Platilac              AS PlatilacID,
        n.Prevoznik             AS PrevoznikID,
        n.Tezina                AS BrutoT,
        n.Duzina                AS DuzinaM,
        n.BrojKola,
        n.SerijaVagona,
        n.Tovareno,
        n.RID,
        n.AdHoc,
        n.PredvidjenoPrimanje,
        n.StvarnoPrimanje,
        n.PredvidjenaPredaja,
        n.StvarnaPredaja,
        n.Status                AS StatusNajave,
        n.NapomenaPriprema
FROM    dbo.Najava n
LEFT JOIN dbo.stanice s1 ON s1.ID = n.Otpravna
LEFT JOIN dbo.stanice s2 ON s2.ID = n.Uputna
LEFT JOIN dbo.stanice g1 ON g1.ID = n.OtpravnaGr
LEFT JOIN dbo.stanice g2 ON g2.ID = n.Uputnagr;
GO

-- Posao sa podacima najave.
IF OBJECT_ID(N'planiranje.OP_vPosao', N'V') IS NOT NULL DROP VIEW planiranje.OP_vPosao;
GO
CREATE VIEW planiranje.OP_vPosao
AS
SELECT  p.ID, p.NajavaID, p.RBDeonice, p.Faza, p.Status, p.TrasaID,
        p.PrijemStanicaID, p.PredajaStanicaID,
        p.PlaniraniPrijem, p.PlaniraniPolazak, p.PlaniraniDolazak, p.PlaniranaPredaja,
        p.StvarniPolazak, p.StvarniDolazak,
        p.PotrebnoLokomotiva, p.Projekat, p.OperativniVozID, p.Napomena,
        n.BrojNajave, n.Otpravna AS NajavaOtpravna, n.Uputna AS NajavaUputna,
        n.OtpravnaGr AS NajavaOtpravnaGr, n.UputnaGr AS NajavaUputnaGr,
        n.PlanOtpravnaID, n.PlanUputnaID,
        n.BrutoT AS NajavaBrutoT, n.DuzinaM AS NajavaDuzinaM, n.BrojKola AS NajavaBrojKola,
        n.SerijaVagona AS NajavaSerijaVagona, n.Tovareno AS NajavaTovareno, n.RID AS NajavaRID,
        n.PredvidjenoPrimanje, n.PredvidjenaPredaja, n.StvarnoPrimanje, n.StvarnaPredaja,
        p.RowVer
FROM    planiranje.OP_Posao p
LEFT JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID;
GO

-- Telegrami o zatvoru (dbo.Telegrami, samo čitanje). Pravila iz plana:
--   Kolosek = 'pruga'           -> ZATVOR deonice
--   levi / desni                -> UPOZORENJE
--   kolosek u stanici/skretnica -> UPOZORENJE
-- Dnevni interval: TrajeOd/TrajeDo. Imena kolona se proveravaju pre pravljenja pogleda.
IF OBJECT_ID(N'planiranje.OP_vTelegram', N'V') IS NOT NULL DROP VIEW planiranje.OP_vTelegram;
GO
IF OBJECT_ID(N'dbo.Telegrami', N'U') IS NOT NULL
BEGIN
    DECLARE @broj nvarchar(100) = CASE WHEN COL_LENGTH('dbo.Telegrami','BrojTelegrama') IS NOT NULL THEN N't.BrojTelegrama'
                                       WHEN COL_LENGTH('dbo.Telegrami','BrTelegrama')   IS NOT NULL THEN N't.BrTelegrama' ELSE N'NULL' END;
    DECLARE @od nvarchar(100) = CASE WHEN COL_LENGTH('dbo.Telegrami','TrajeOd') IS NOT NULL THEN N't.TrajeOd' ELSE N'NULL' END;
    DECLARE @do nvarchar(100) = CASE WHEN COL_LENGTH('dbo.Telegrami','TrajeDo') IS NOT NULL THEN N't.TrajeDo' ELSE N'NULL' END;
    DECLARE @sql nvarchar(max) = N'CREATE VIEW planiranje.OP_vTelegram
AS
SELECT  t.ID AS TelegramID, ' + @broj + N' AS BrojTelegrama, t.PrugaID AS IzvornaPrugaID,
        t.OdStanice AS IzvornaStanicaOdID, t.DoStanice AS IzvornaStanicaDoID,
        RTRIM(t.Kolosek) AS Kolosek,
        CASE WHEN LOWER(RTRIM(t.Kolosek)) LIKE N''%pruga%'' THEN N''ZATVOR'' ELSE N''UPOZORENJE'' END AS Vrsta,
        t.VaziOd, t.VaziDo, ' + @od + N' AS TrajeOd, ' + @do + N' AS TrajeDo,
        t.Napomena, t.Aktivan
FROM    dbo.Telegrami t;';
    EXEC sys.sp_executesql @sql;
END
ELSE PRINT N'dbo.Telegrami ne postoji, pogled OP_vTelegram nije napravljen.';
GO

-- Masa iz Teretnice po najavi (dbo.TeretnicaStavke, samo čitanje).
IF OBJECT_ID(N'planiranje.OP_vTeretnicaNajava', N'V') IS NOT NULL DROP VIEW planiranje.OP_vTeretnicaNajava;
GO
IF OBJECT_ID(N'dbo.TeretnicaStavke', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.TeretnicaStavke','IDNajave') IS NOT NULL
   AND COL_LENGTH('dbo.TeretnicaStavke','Tara') IS NOT NULL
   AND COL_LENGTH('dbo.TeretnicaStavke','Neto') IS NOT NULL
BEGIN
    DECLARE @duz nvarchar(100) = CASE WHEN COL_LENGTH('dbo.TeretnicaStavke','Duzina') IS NOT NULL THEN N'SUM(CAST(ts.Duzina AS decimal(10,2)))' ELSE N'NULL' END;
    DECLARE @sql nvarchar(max) = N'CREATE VIEW planiranje.OP_vTeretnicaNajava
AS
SELECT  ts.IDNajave AS NajavaID,
        SUM(ISNULL(CAST(ts.Tara AS decimal(12,3)), 0) + ISNULL(CAST(ts.Neto AS decimal(12,3)), 0)) AS BrutoZbir,   -- jedinica kao u Teretnici (proveriti: t ili kg)
        ' + @duz + N' AS DuzinaM,
        COUNT(*) AS BrojKola
FROM    dbo.TeretnicaStavke ts
WHERE   ts.IDNajave IS NOT NULL
GROUP BY ts.IDNajave;';
    EXEC sys.sp_executesql @sql;
END
ELSE PRINT N'dbo.TeretnicaStavke nema očekivane kolone, pogled OP_vTeretnicaNajava nije napravljen.';
GO

-- Osoblje: dbo.Delavci (samo čitanje) + dopuna za planiranje.
IF OBJECT_ID(N'planiranje.OP_vOsoblje', N'V') IS NOT NULL DROP VIEW planiranje.OP_vOsoblje;
GO
DECLARE @f TABLE (Kolona sysname PRIMARY KEY, Izraz nvarchar(200));
INSERT @f VALUES
    (N'Masinovodja',   N'CASE WHEN d.Masinovodja > 0 THEN 1 ELSE 0 END'),
    (N'PregledacKola', N'CASE WHEN d.PregledacKola > 0 THEN 1 ELSE 0 END'),
    (N'Pregledac',     N'CASE WHEN d.Pregledac > 0 THEN 1 ELSE 0 END'),
    (N'Vozovodja',     N'CASE WHEN d.Vozovodja > 0 THEN 1 ELSE 0 END'),
    (N'Manevrista',    N'CASE WHEN d.Manevrista > 0 THEN 1 ELSE 0 END'),
    (N'DeTelefon1',    N'RTRIM(d.DeTelefon1)'),
    (N'DeEMail',       N'RTRIM(d.DeEMail)'),
    (N'DeSifStat',     N'd.DeSifStat');
UPDATE @f SET Izraz = CASE WHEN Kolona IN (N'DeTelefon1', N'DeEMail', N'DeSifStat') THEN N'CAST(NULL AS nvarchar(100))' ELSE N'0' END
WHERE COL_LENGTH('dbo.Delavci', Kolona) IS NULL;
DECLARE @sql nvarchar(max) = N'CREATE VIEW planiranje.OP_vOsoblje
AS
SELECT  d.DeSifra,
        RTRIM(d.DeIme) AS Ime, RTRIM(d.DePriimek) AS Prezime,
        RTRIM(d.DePriimek) + N'' '' + RTRIM(d.DeIme) AS Naziv,
        ' + (SELECT Izraz FROM @f WHERE Kolona = N'DeTelefon1') + N' AS Telefon,
        ' + (SELECT Izraz FROM @f WHERE Kolona = N'DeEMail') + N' AS Email,
        CAST(' + (SELECT Izraz FROM @f WHERE Kolona = N'Masinovodja') + N' AS bit) AS Masinovodja,
        CAST(CASE WHEN (' + (SELECT Izraz FROM @f WHERE Kolona = N'PregledacKola') + N') = 1 OR (' + (SELECT Izraz FROM @f WHERE Kolona = N'Pregledac') + N') = 1 THEN 1 ELSE 0 END AS bit) AS PregledacKola,
        CAST(' + (SELECT Izraz FROM @f WHERE Kolona = N'Vozovodja') + N' AS bit) AS Vozovodja,
        CAST(' + (SELECT Izraz FROM @f WHERE Kolona = N'Manevrista') + N' AS bit) AS Manevrista,
        CAST(CASE WHEN ISNULL(' + (SELECT Izraz FROM @f WHERE Kolona = N'DeSifStat') + N', N'''') <> N''P'' THEN 1 ELSE 0 END AS bit) AS Aktivan,
        p.MaticnaStanicaID, CAST(ISNULL(p.UPlaniranju, 0) AS bit) AS UPlaniranju, p.Napomena
FROM    dbo.Delavci d
LEFT JOIN planiranje.OP_OsobljePlan p ON p.DeSifra = d.DeSifra;';
EXEC sys.sp_executesql @sql;
GO

-- Odsustva: postojeće evidencije (samo čitanje) + ručni unos u OP_Odsustvo (slobodan dan, ostalo).
--   dbo.Bolovanje    (ZaposleniID, DatumOd, DatumDo, TipBolovanja, Napomena)
--   dbo.DopustStavke (IDNadredjena -> dbo.Dopust.ID, VremeOd, VremeDo, StatusGodmora, Napomena)
-- Kolona radnika u dbo.Dopust se traži među uobičajenim imenima; ako je nema, godišnji se ne prikazuje.
IF OBJECT_ID(N'planiranje.OP_vOdsustvo', N'V') IS NOT NULL DROP VIEW planiranje.OP_vOdsustvo;
GO
DECLARE @sql nvarchar(max) = N'CREATE VIEW planiranje.OP_vOdsustvo
AS
SELECT  o.DeSifra, o.Vrsta, o.Od, o.Do, o.Napomena, N''PLANIRANJE'' AS Izvor
FROM    planiranje.OP_Odsustvo o';
IF OBJECT_ID(N'dbo.Bolovanje', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Bolovanje', 'ZaposleniID') IS NOT NULL
   AND COL_LENGTH('dbo.Bolovanje', 'DatumOd') IS NOT NULL
   AND COL_LENGTH('dbo.Bolovanje', 'DatumDo') IS NOT NULL
    SET @sql += N'
UNION ALL
SELECT  b.ZaposleniID, N''BOLOVANJE'', CAST(b.DatumOd AS date), CAST(b.DatumDo AS date), '
        + CASE WHEN COL_LENGTH('dbo.Bolovanje', 'Napomena') IS NOT NULL THEN N'CAST(b.Napomena AS nvarchar(300))' ELSE N'CAST(NULL AS nvarchar(300))' END
        + N', N''BOLOVANJE''
FROM    dbo.Bolovanje b';
DECLARE @rad sysname =
    CASE WHEN COL_LENGTH('dbo.Dopust', 'DeSifra')     IS NOT NULL THEN N'DeSifra'
         WHEN COL_LENGTH('dbo.Dopust', 'DoSifDel')    IS NOT NULL THEN N'DoSifDel'
         WHEN COL_LENGTH('dbo.Dopust', 'DoSifDe')     IS NOT NULL THEN N'DoSifDe'
         WHEN COL_LENGTH('dbo.Dopust', 'DoDelavec')   IS NOT NULL THEN N'DoDelavec'
         WHEN COL_LENGTH('dbo.Dopust', 'ZaposleniID') IS NOT NULL THEN N'ZaposleniID'
         WHEN COL_LENGTH('dbo.Dopust', 'Zaposleni')   IS NOT NULL THEN N'Zaposleni' END;
DECLARE @kljuc sysname =
    CASE WHEN COL_LENGTH('dbo.Dopust', 'ID')        IS NOT NULL THEN N'ID'
         WHEN COL_LENGTH('dbo.Dopust', 'DoStZapisa') IS NOT NULL THEN N'DoStZapisa' END;
IF @rad IS NOT NULL AND @kljuc IS NOT NULL AND OBJECT_ID(N'dbo.DopustStavke', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.DopustStavke', 'IDNadredjena') IS NOT NULL
   AND COL_LENGTH('dbo.DopustStavke', 'VremeOd') IS NOT NULL
   AND COL_LENGTH('dbo.DopustStavke', 'VremeDo') IS NOT NULL
    SET @sql += N'
UNION ALL
SELECT  CAST(dz.' + QUOTENAME(@rad) + N' AS int), N''GODISNJI'', CAST(ds.VremeOd AS date), CAST(ds.VremeDo AS date), '
        + CASE WHEN COL_LENGTH('dbo.DopustStavke', 'Napomena') IS NOT NULL THEN N'CAST(ds.Napomena AS nvarchar(300))' ELSE N'CAST(NULL AS nvarchar(300))' END
        + N', N''DOPUST''
FROM    dbo.DopustStavke ds
JOIN    dbo.Dopust dz ON dz.' + QUOTENAME(@kljuc) + N' = ds.IDNadredjena';
ELSE PRINT N'Godišnji odmori (dbo.Dopust) nisu povezani: kolona radnika nije prepoznata. Dopuniti OP_vOdsustvo.';
SET @sql += N';';
EXEC sys.sp_executesql @sql;
GO

-- Kalendar lokomotiva: poslovi i zauzetosti u jednom spisku (za nedeljni plan).
IF OBJECT_ID(N'planiranje.OP_vLokomotivaKalendar', N'V') IS NOT NULL DROP VIEW planiranje.OP_vLokomotivaKalendar;
GO
CREATE VIEW planiranje.OP_vLokomotivaKalendar
AS
SELECT  pl.LokomotivaID, N'POSAO' AS Vrsta, pl.PosaoID, pl.RB, pl.Vucna,
        pl.Od, pl.Do, CAST(NULL AS nvarchar(100)) AS Projekat, CAST(NULL AS nvarchar(300)) AS Napomena
FROM    planiranje.OP_PosaoLokomotiva pl
JOIN    planiranje.OP_Posao p ON p.ID = pl.PosaoID AND p.Status <> N'OTKAZAN'
UNION ALL
SELECT  z.LokomotivaID, z.Vrsta, NULL, NULL, NULL, z.Od, z.Do, z.Projekat, z.Napomena
FROM    planiranje.OP_LokomotivaZauzetost z;
GO

-- Dostupnost osoblja po danu: smena, odsustvo i zadaci.
IF OBJECT_ID(N'planiranje.OP_vOsobljeDan', N'V') IS NOT NULL DROP VIEW planiranje.OP_vOsobljeDan;
GO
CREATE VIEW planiranje.OP_vOsobljeDan
AS
SELECT  s.DeSifra, s.Datum, s.Vrsta AS Smena, s.Od, s.Do,
        o.Vrsta AS Odsustvo,
        (SELECT COUNT(*) FROM planiranje.OP_Zadatak z
          WHERE z.DeSifra = s.DeSifra AND z.Status IN (N'POSLAT', N'PRIHVACEN')
            AND z.Od < DATEADD(day, 1, CAST(s.Datum AS datetime2(0))) AND z.Do > CAST(s.Datum AS datetime2(0))) AS BrojZadataka
FROM    planiranje.OP_Smena s
OUTER APPLY (SELECT TOP 1 x.Vrsta FROM planiranje.OP_vOdsustvo x
             WHERE x.DeSifra = s.DeSifra AND s.Datum BETWEEN x.Od AND x.Do ORDER BY x.Od DESC) o;
GO

PRINT N'OP_001_Sema.sql završen.';
GO
