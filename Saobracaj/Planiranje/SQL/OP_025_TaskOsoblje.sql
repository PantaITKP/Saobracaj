/* =====================================================================
   OP_025_TaskOsoblje.sql
   Osoblje za task: radna mesta koja task traži (ko, gde i kada) i veza
   zadatka osobe sa taskom.

   - OP_TaskOsoblje   radno mesto taska: uloga (MASINOVODJA, PREGLEDAC_PRIJEM,
                      PREGLEDAC_PREDAJA), stanica, od, do, napomena. Unosi se ručno
                      na ekranu Taskovi.
   - OP_Zadatak       dobija TaskID i TaskOsobljeID: zadatak poslat sa ekrana
                      Ponuda osoblja za task. PosaoID ostaje (posao najave taska).
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_024. Program ne radi bez nje.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF COL_LENGTH('planiranje.OP_Task', 'TrajanjeMin') IS NULL
    RAISERROR(N'Prvo pokrenuti OP_024_TaskTrajanje.sql.', 16, 1);
GO

IF OBJECT_ID(N'planiranje.OP_TaskOsoblje', N'U') IS NULL
CREATE TABLE planiranje.OP_TaskOsoblje
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TaskOsoblje PRIMARY KEY,
    TaskID              int            NOT NULL CONSTRAINT FK_OP_TaskOsoblje_Task REFERENCES planiranje.OP_Task(ID) ON DELETE CASCADE,
    Uloga               nvarchar(20)   NOT NULL
                        CONSTRAINT CK_OP_TaskOsoblje_Uloga CHECK (Uloga IN (N'MASINOVODJA', N'PREGLEDAC_PRIJEM', N'PREGLEDAC_PREDAJA')),
    StanicaID           int            NOT NULL CONSTRAINT FK_OP_TaskOsoblje_Stanica REFERENCES planiranje.OP_Stanice(ID),
    Od                  datetime2(0)   NOT NULL,
    Do                  datetime2(0)   NOT NULL,
    Napomena            nvarchar(300)  NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_TaskOsoblje_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_TaskOsoblje_Kreirano DEFAULT (SYSDATETIME()),
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_TaskOsoblje_Period CHECK (Do > Od)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_TaskOsoblje_Task')
    CREATE INDEX IX_OP_TaskOsoblje_Task ON planiranje.OP_TaskOsoblje(TaskID);
GO

-- Bez kaskadnog brisanja: zadatak se ne briše sa taskom ni sa radnim mestom (task se stornira, a ne briše).
IF COL_LENGTH('planiranje.OP_Zadatak', 'TaskID') IS NULL
    ALTER TABLE planiranje.OP_Zadatak ADD TaskID int NULL CONSTRAINT FK_OP_Zadatak_Task REFERENCES planiranje.OP_Task(ID);
GO
IF COL_LENGTH('planiranje.OP_Zadatak', 'TaskOsobljeID') IS NULL
    ALTER TABLE planiranje.OP_Zadatak ADD TaskOsobljeID int NULL CONSTRAINT FK_OP_Zadatak_TaskOsoblje REFERENCES planiranje.OP_TaskOsoblje(ID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Zadatak_Task')
    CREATE INDEX IX_OP_Zadatak_Task ON planiranje.OP_Zadatak(TaskID) INCLUDE (TaskOsobljeID, Status);
GO

SELECT N'OP_TaskOsoblje' AS Tabela, COUNT(*) AS Redova FROM planiranje.OP_TaskOsoblje
UNION ALL SELECT N'OP_Zadatak sa taskom', COUNT(*) FROM planiranje.OP_Zadatak WHERE TaskID IS NOT NULL;
PRINT N'OP_025 završena.';
GO

/* Poništavanje (ručno, po potrebi; zadaci ostaju, bez veze sa taskom):

DROP INDEX IX_OP_Zadatak_Task ON planiranje.OP_Zadatak;
ALTER TABLE planiranje.OP_Zadatak DROP CONSTRAINT FK_OP_Zadatak_TaskOsoblje;
ALTER TABLE planiranje.OP_Zadatak DROP CONSTRAINT FK_OP_Zadatak_Task;
ALTER TABLE planiranje.OP_Zadatak DROP COLUMN TaskOsobljeID;
ALTER TABLE planiranje.OP_Zadatak DROP COLUMN TaskID;
DROP TABLE planiranje.OP_TaskOsoblje;
*/
