/*
  Migracija 002: naučeni nazivi pruga i stanica (SQL Server 2008 R2)

  Kada korisnik potvrdi/dopuni stavku sa statusom NeedsReview (izabere prugu ili stanicu iz šifarnika),
  naziv iz telegrama se pamti ovde, pa ga agent sledeći put prepoznaje sam.
  Idempotentna; pokretati u kontekstu ciljne baze (sqlcmd -d <baza> -f 65001 -i 002_TelegramiAlias.sql).
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.TelegramiAlias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelegramiAlias
    (
        ID        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TelegramiAlias PRIMARY KEY,
        Tip       nvarchar(10)   NOT NULL,   -- 'Stanica' / 'Pruga'
        Tekst     nvarchar(300)  NOT NULL,   -- ključ: bez dijakritika, mala slova (kao u agentu)
        PrugaID   int            NULL,       -- za stanicu: važi samo na toj pruzi (NULL = svuda)
        RefID     int            NOT NULL,   -- stanice.ID ili Pruga.ID
        Uneo      nvarchar(100)  NULL,
        Uneto     datetime       NOT NULL CONSTRAINT DF_TelegramiAlias_Uneto DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_TelegramiAlias ON dbo.TelegramiAlias (Tip, Tekst, PrugaID);
END
GO

-- Poznate razlike između telegrama i šifarnika (iz ručnog unosa 20.10-02.11.2026)
IF NOT EXISTS (SELECT 1 FROM dbo.TelegramiAlias WHERE Tip = N'Stanica' AND Tekst = N'kosjeric')
   AND EXISTS (SELECT 1 FROM dbo.stanice WHERE Opis = N'Kosijerić')
    INSERT dbo.TelegramiAlias (Tip, Tekst, RefID, Uneo)
    SELECT TOP 1 N'Stanica', N'kosjeric', ID, N'migracija 002' FROM dbo.stanice WHERE Opis = N'Kosijerić' ORDER BY ID;
GO

PRINT N'Migracija 002_TelegramiAlias završena.';
GO
