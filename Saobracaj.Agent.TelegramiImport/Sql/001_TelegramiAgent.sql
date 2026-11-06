/*
  Migracija 001: agent za telegrame (Saobracaj.Agent.TelegramiImport)
  Ciljna verzija: SQL Server 2008 R2 (KPSRV-NOVI\SQLEXPRESS2008K)

  - Samo dodaje: nove kolone u Telegrami (sve NULL), nove tabele, indekse.
  - Postojeće redove ne menja.
  - Idempotentna: sme da se pokrene više puta.
  - Ne sadrži ime baze: pokretati u kontekstu ciljne baze, npr.
      sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech2 -i 001_TelegramiAgent.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ---------------------------------------------------------------------------
   1. AgentRunLog: jedan red po pokretanju bilo kog agenta na serveru
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.AgentRunLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentRunLog
    (
        ID         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgentRunLog PRIMARY KEY,
        Agent      nvarchar(100)  NOT NULL,
        Racunar    nvarchar(100)  NULL,
        Start      datetime       NOT NULL CONSTRAINT DF_AgentRunLog_Start DEFAULT (GETDATE()),
        Kraj       datetime       NULL,
        Status     nvarchar(20)   NOT NULL CONSTRAINT DF_AgentRunLog_Status DEFAULT (N'Radi'),  -- Radi / Uspeh / Greska / Preskoceno
        DryRun     bit            NOT NULL CONSTRAINT DF_AgentRunLog_DryRun DEFAULT (0),
        Obradjeno  int            NULL,
        Poruka     nvarchar(max)  NULL
    );
    CREATE INDEX IX_AgentRunLog_Agent_Start ON dbo.AgentRunLog (Agent, Start DESC);
END
GO

/* ---------------------------------------------------------------------------
   2. TelegramiMailLog: svaka primljena poruka iz sandučeta telegrami@
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TelegramiMailLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelegramiMailLog
    (
        ID              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TelegramiMailLog PRIMARY KEY,
        Folder          nvarchar(100)  NOT NULL CONSTRAINT DF_TelegramiMailLog_Folder DEFAULT (N'INBOX'),
        UidValidity     bigint         NOT NULL,
        Uid             bigint         NOT NULL,
        MessageId       nvarchar(255)  NULL,
        Primljeno       datetime       NULL,
        Posiljalac      nvarchar(255)  NULL,
        Naslov          nvarchar(500)  NULL,
        Vrsta           nvarchar(100)  NULL,   -- jedna ili više vrsta, razdvojeno zarezom (npr. 'VanSnage,Zatvor')
        BrojTelegrama   int            NULL,
        OznakaTelegrama nvarchar(20)   NULL,   -- kako piše u telegramu, npr. '288F'
        DatumTelegrama  date           NULL,
        Status          nvarchar(20)   NOT NULL, -- Upisano / Otkazano / Arhivirano / NeedsReview / Potvrdjeno / Greska / DryRun
        Poruka          nvarchar(max)  NULL,   -- upozorenja i greške
        BrojRedova      int            NULL,   -- koliko je redova upisano/izmenjeno u Telegrami
        TekstTelegrama  nvarchar(max)  NULL,   -- očišćen tekst PDF-a (bez zaglavlja primalaca)
        TekstMaila      nvarchar(max)  NULL,
        DryRun          bit            NOT NULL CONSTRAINT DF_TelegramiMailLog_DryRun DEFAULT (0),
        Obradjeno       datetime       NOT NULL CONSTRAINT DF_TelegramiMailLog_Obradjeno DEFAULT (GETDATE()),
        Pregledao       nvarchar(100)  NULL,   -- korisnik koji je potvrdio NeedsReview
        Pregledano      datetime       NULL
    );
    CREATE UNIQUE INDEX UX_TelegramiMailLog_Uid ON dbo.TelegramiMailLog (Folder, UidValidity, Uid);
    CREATE INDEX IX_TelegramiMailLog_MessageId ON dbo.TelegramiMailLog (MessageId);
    CREATE INDEX IX_TelegramiMailLog_Telegram ON dbo.TelegramiMailLog (BrojTelegrama, DatumTelegrama);
    CREATE INDEX IX_TelegramiMailLog_Status ON dbo.TelegramiMailLog (Status, ID);
END
GO

/* ---------------------------------------------------------------------------
   3. TelegramiPrilozi: originalni prilozi (PDF, .doc, .xlsx)
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TelegramiPrilozi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelegramiPrilozi
    (
        ID          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TelegramiPrilozi PRIMARY KEY,
        MailLogID   int            NOT NULL CONSTRAINT FK_TelegramiPrilozi_MailLog REFERENCES dbo.TelegramiMailLog (ID),
        NazivFajla  nvarchar(260)  NOT NULL,
        ContentType nvarchar(100)  NULL,
        Velicina    int            NOT NULL,
        Sha256      char(64)       NULL,
        Sadrzaj     varbinary(max) NOT NULL
    );
    CREATE INDEX IX_TelegramiPrilozi_MailLog ON dbo.TelegramiPrilozi (MailLogID);
END
GO

/* ---------------------------------------------------------------------------
   4. TelegramiMailStavke: svaki isparsirani red (tačka × dnevni interval)
      - Status 'Upisano'      → upisan u Telegrami (TelegramiID)
      - Status 'NeedsReview'  → nepoznata pruga/stanica ili dvosmislen tekst;
                                čeka da korisnik dopuni PrugaID/OdStanice/DoStanice i potvrdi
      - Status 'Potvrdjeno'   → korisnik potvrdio, upisan u Telegrami
      - Status 'Odbaceno'     → korisnik odbacio
      - Status 'Duplikat'     → isti red već postoji u Telegrami (npr. ručni unos)
      - Status 'DryRun'       → rezultat probnog pokretanja, ništa nije upisano
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TelegramiMailStavke', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelegramiMailStavke
    (
        ID               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TelegramiMailStavke PRIMARY KEY,
        MailLogID        int            NOT NULL CONSTRAINT FK_TelegramiMailStavke_MailLog REFERENCES dbo.TelegramiMailLog (ID),
        Tacka            nvarchar(10)   NULL,
        Vrsta            nvarchar(30)   NULL,
        BrojTelegrama    int            NULL,
        DatumTelegrama   date           NULL,
        PrugaTekst       nvarchar(300)  NULL,
        PrugaID          int            NULL,
        OdStaniceTekst   nvarchar(100)  NULL,
        OdStanice        int            NULL,
        DoStaniceTekst   nvarchar(100)  NULL,
        DoStanice        int            NULL,
        Kolosek          nvarchar(50)   NULL,
        VaziOd           datetime       NULL,
        VaziDo           datetime       NULL,
        Napomena         nvarchar(250)  NULL,
        NarocitaPosiljka bit            NOT NULL CONSTRAINT DF_TelegramiMailStavke_NP DEFAULT (0),
        Status           nvarchar(20)   NOT NULL,
        Upozorenje       nvarchar(1000) NULL,
        TelegramiID      int            NULL,   -- Telegrami.ID posle upisa
        Potvrdio         nvarchar(100)  NULL,
        Potvrdjeno       datetime       NULL
    );
    CREATE INDEX IX_TelegramiMailStavke_MailLog ON dbo.TelegramiMailStavke (MailLogID);
    CREATE INDEX IX_TelegramiMailStavke_Status ON dbo.TelegramiMailStavke (Status);
END
GO

/* ---------------------------------------------------------------------------
   5. Nove kolone u Telegrami (sve NULL; postojeći kod ih ne vidi)
   --------------------------------------------------------------------------- */
IF COL_LENGTH(N'dbo.Telegrami', N'DatumTelegrama') IS NULL
    ALTER TABLE dbo.Telegrami ADD DatumTelegrama date NULL;
IF COL_LENGTH(N'dbo.Telegrami', N'Vrsta') IS NULL
    ALTER TABLE dbo.Telegrami ADD Vrsta nvarchar(30) NULL;
IF COL_LENGTH(N'dbo.Telegrami', N'MailLogID') IS NULL
    ALTER TABLE dbo.Telegrami ADD MailLogID int NULL;
IF COL_LENGTH(N'dbo.Telegrami', N'UnosAutomatski') IS NULL
    ALTER TABLE dbo.Telegrami ADD UnosAutomatski bit NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Telegrami_TelegramiMailLog')
    ALTER TABLE dbo.Telegrami WITH CHECK
        ADD CONSTRAINT FK_Telegrami_TelegramiMailLog FOREIGN KEY (MailLogID) REFERENCES dbo.TelegramiMailLog (ID);
GO

/* ---------------------------------------------------------------------------
   6. Indeksi na Telegrami (tabela je danas heap bez ijednog indeksa)
      - UX_Telegrami_ID: upit notifier-a "ID > @x" na svakih 30 s sa svakog računara,
        i zaštita od dupliranog ID-a (procedura InsertTelegrami dodeljuje MAX(ID)+1 bez zaključavanja).
        Pre kreiranja proverava da nema NULL ni dupliranih ID-jeva.
      - IX_Telegrami_Broj: provera duplikata i traženje telegrama za otkazivanje.
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Telegrami') AND name = N'UX_Telegrami_ID')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Telegrami WHERE ID IS NULL)
       OR EXISTS (SELECT ID FROM dbo.Telegrami GROUP BY ID HAVING COUNT(*) > 1)
        RAISERROR(N'Telegrami ima NULL ili duplirane ID-jeve; UX_Telegrami_ID nije kreiran. Prvo ispraviti podatke.', 10, 1);
    ELSE
        CREATE UNIQUE INDEX UX_Telegrami_ID ON dbo.Telegrami (ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Telegrami') AND name = N'IX_Telegrami_Broj')
    CREATE INDEX IX_Telegrami_Broj ON dbo.Telegrami (BrojTelegrama, DatumTelegrama) INCLUDE (VaziOd, VaziDo, Aktivan);
GO

PRINT N'Migracija 001_TelegramiAgent završena.';
GO
