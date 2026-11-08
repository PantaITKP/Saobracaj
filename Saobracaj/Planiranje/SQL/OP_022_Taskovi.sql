/* =====================================================================
   OP_022_Taskovi.sql
   Taskovi: deo posla (najave) koji se vozi jednom rutom, jednim sastavom
   i jednim lokomotivama. Ekran je frmOPTaskovi (Transport / Planiranje).

   - OP_Task           task: broj (ID), stanice, km, bruto, broj kola (EA),
                       ETD, ATD, ETA, ATA i status OTVOREN/LANSIRAN/ZAVRSEN/STORNIRAN.
   - OP_TaskNajava     najave taska (posle spajanja ih može biti više): koliko
                       bruta i kola nosi, i gde je task na ruti najave (KmOd, KmDo).
                       Redosled taskova najave je KmOd, pa RBDela, a ne broj taska.
   - OP_TaskStanica    stanice rute taska, sa km od početka taska, prugom i trasom.
   - OP_TaskLokomotiva lokomotive taska: jedna VUCNA, ostale POTISAK.
   - OP_TaskVeza       iz kog taska je nastao novi (split rute, split bruta, spajanje).
   - OP_vTaskNajava    task po najavi, sa delom rute i brojem bruto delova.

   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_021. Poništavanje je na dnu.
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
IF OBJECT_ID(N'planiranje.OP_Posao', N'U') IS NULL
    RAISERROR(N'Šema planiranje ne postoji (OP_001_Sema.sql).', 16, 1);
GO

IF OBJECT_ID(N'planiranje.OP_Task', N'U') IS NULL
CREATE TABLE planiranje.OP_Task
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_Task PRIMARY KEY,   -- broj taska
    StanicaOdID         int            NOT NULL CONSTRAINT FK_OP_Task_Od REFERENCES planiranje.OP_Stanice(ID),
    StanicaDoID         int            NOT NULL CONSTRAINT FK_OP_Task_Do REFERENCES planiranje.OP_Stanice(ID),
    Km                  decimal(9,3)   NOT NULL CONSTRAINT DF_OP_Task_Km DEFAULT (0),
    BrutoT              decimal(12,3)  NOT NULL CONSTRAINT DF_OP_Task_Bruto DEFAULT (0),
    BrojKola            int            NOT NULL CONSTRAINT DF_OP_Task_BrojKola DEFAULT (0),  -- EA
    DuzinaM             decimal(9,1)   NULL,
    TipKola             nvarchar(100)  NULL,
    ETD                 datetime2(0)   NULL,          -- planirani polazak
    ATD                 datetime2(0)   NULL,          -- stvarni polazak
    ETA                 datetime2(0)   NULL,          -- planirani dolazak
    ATA                 datetime2(0)   NULL,          -- stvarni dolazak
    Status              nvarchar(10)   NOT NULL CONSTRAINT DF_OP_Task_Status DEFAULT (N'OTVOREN')
                        CONSTRAINT CK_OP_Task_Status CHECK (Status IN (N'OTVOREN', N'LANSIRAN', N'ZAVRSEN', N'STORNIRAN')),
    Nastanak            nvarchar(12)   NOT NULL CONSTRAINT DF_OP_Task_Nastanak DEFAULT (N'RUTA')
                        CONSTRAINT CK_OP_Task_Nastanak CHECK (Nastanak IN (N'RUTA', N'SPLIT_RUTE', N'SPLIT_BRUTO', N'SPAJANJE')),
    RazlogStorna        nvarchar(12)   NULL
                        CONSTRAINT CK_OP_Task_RazlogStorna CHECK (RazlogStorna IN (N'SPLIT_RUTE', N'SPLIT_BRUTO', N'SPAJANJE', N'RUCNO')),
    OpisRute            nvarchar(1000) NULL,          -- izabrana kombinacija trasa
    Napomena            nvarchar(1000) NULL,
    Kreirao             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_Task_Kreirao DEFAULT (SUSER_SNAME()),
    Kreirano            datetime2(0)   NOT NULL CONSTRAINT DF_OP_Task_Kreirano DEFAULT (SYSDATETIME()),
    Izmenio             nvarchar(50)   NULL,
    Izmenjeno           datetime2(0)   NULL,
    RowVer              rowversion     NOT NULL,
    CONSTRAINT CK_OP_Task_Stanice CHECK (StanicaOdID <> StanicaDoID),
    CONSTRAINT CK_OP_Task_Masa CHECK (BrutoT >= 0 AND BrojKola >= 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Task_Period')
    CREATE INDEX IX_OP_Task_Period ON planiranje.OP_Task(ETD, ETA) INCLUDE (Status);
GO

IF OBJECT_ID(N'planiranje.OP_TaskNajava', N'U') IS NULL
CREATE TABLE planiranje.OP_TaskNajava
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TaskNajava PRIMARY KEY,
    TaskID              int            NOT NULL CONSTRAINT FK_OP_TaskNajava_Task REFERENCES planiranje.OP_Task(ID) ON DELETE CASCADE,
    NajavaID            int            NOT NULL,      -- dbo.Najava.ID (samo čitanje)
    PosaoID             int            NULL CONSTRAINT FK_OP_TaskNajava_Posao REFERENCES planiranje.OP_Posao(ID),
    BrojNajave          nvarchar(50)   NULL,          -- prepisano iz najave
    KmOd                decimal(9,3)   NOT NULL,      -- početak taska na ruti najave
    KmDo                decimal(9,3)   NOT NULL,
    RBDela              int            NOT NULL CONSTRAINT DF_OP_TaskNajava_RB DEFAULT (1), -- redosled bruto delova na istom mestu rute
    BrutoT              decimal(12,3)  NOT NULL CONSTRAINT DF_OP_TaskNajava_Bruto DEFAULT (0),
    BrojKola            int            NOT NULL CONSTRAINT DF_OP_TaskNajava_BrojKola DEFAULT (0),
    CONSTRAINT UX_OP_TaskNajava UNIQUE (TaskID, NajavaID),
    CONSTRAINT CK_OP_TaskNajava_Km CHECK (KmDo >= KmOd)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_TaskNajava_Najava')
    CREATE INDEX IX_OP_TaskNajava_Najava ON planiranje.OP_TaskNajava(NajavaID, KmOd, RBDela) INCLUDE (TaskID);
GO

IF OBJECT_ID(N'planiranje.OP_TaskStanica', N'U') IS NULL
CREATE TABLE planiranje.OP_TaskStanica
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TaskStanica PRIMARY KEY,
    TaskID              int            NOT NULL CONSTRAINT FK_OP_TaskStanica_Task REFERENCES planiranje.OP_Task(ID) ON DELETE CASCADE,
    RB                  int            NOT NULL,
    StanicaID           int            NOT NULL CONSTRAINT FK_OP_TaskStanica_Stanica REFERENCES planiranje.OP_Stanice(ID),
    Km                  decimal(9,3)   NOT NULL,      -- od početka taska
    PrugaID             int            NULL,          -- pruga deonice koja se završava ovde
    TrasaID             int            NULL,          -- trasa te deonice (NULL = bez trase)
    CONSTRAINT UX_OP_TaskStanica UNIQUE (TaskID, RB)
);
GO

IF OBJECT_ID(N'planiranje.OP_TaskLokomotiva', N'U') IS NULL
CREATE TABLE planiranje.OP_TaskLokomotiva
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TaskLokomotiva PRIMARY KEY,
    TaskID              int            NOT NULL CONSTRAINT FK_OP_TaskLokomotiva_Task REFERENCES planiranje.OP_Task(ID) ON DELETE CASCADE,
    LokomotivaID        int            NOT NULL CONSTRAINT FK_OP_TaskLokomotiva_Lok REFERENCES planiranje.OP_Lokomotive(ID),
    Uloga               nvarchar(8)    NOT NULL CONSTRAINT DF_OP_TaskLokomotiva_Uloga DEFAULT (N'VUCNA')
                        CONSTRAINT CK_OP_TaskLokomotiva_Uloga CHECK (Uloga IN (N'VUCNA', N'POTISAK')),
    RB                  tinyint        NOT NULL CONSTRAINT DF_OP_TaskLokomotiva_RB DEFAULT (1),
    Dodelio             nvarchar(50)   NOT NULL CONSTRAINT DF_OP_TaskLokomotiva_Dodelio DEFAULT (SUSER_SNAME()),
    Dodeljeno           datetime2(0)   NOT NULL CONSTRAINT DF_OP_TaskLokomotiva_Dodeljeno DEFAULT (SYSDATETIME()),
    CONSTRAINT UX_OP_TaskLokomotiva UNIQUE (TaskID, LokomotivaID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OP_TaskLokomotiva_Vucna')
    CREATE UNIQUE INDEX UX_OP_TaskLokomotiva_Vucna ON planiranje.OP_TaskLokomotiva(TaskID) WHERE Uloga = N'VUCNA';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_TaskLokomotiva_Lok')
    CREATE INDEX IX_OP_TaskLokomotiva_Lok ON planiranje.OP_TaskLokomotiva(LokomotivaID) INCLUDE (TaskID);
GO

IF OBJECT_ID(N'planiranje.OP_TaskVeza', N'U') IS NULL
CREATE TABLE planiranje.OP_TaskVeza
(
    ID                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OP_TaskVeza PRIMARY KEY,
    TaskID              int            NOT NULL CONSTRAINT FK_OP_TaskVeza_Task REFERENCES planiranje.OP_Task(ID),
    IzvorniTaskID       int            NOT NULL CONSTRAINT FK_OP_TaskVeza_Izvorni REFERENCES planiranje.OP_Task(ID),
    Vrsta               nvarchar(12)   NOT NULL
                        CONSTRAINT CK_OP_TaskVeza_Vrsta CHECK (Vrsta IN (N'SPLIT_RUTE', N'SPLIT_BRUTO', N'SPAJANJE')),
    CONSTRAINT UX_OP_TaskVeza UNIQUE (TaskID, IzvorniTaskID)
);
GO

-- Task po najavi. DeoRute: koji je deo rute (po KmOd), DelovaBruto: koliko taskova vozi isti deo.
-- Stornirani taskovi se ne broje.
IF OBJECT_ID(N'planiranje.OP_vTaskNajava', N'V') IS NOT NULL DROP VIEW planiranje.OP_vTaskNajava;
GO
CREATE VIEW planiranje.OP_vTaskNajava
AS
SELECT  tn.TaskID, tn.NajavaID, tn.PosaoID, tn.BrojNajave, tn.KmOd, tn.KmDo, tn.RBDela,
        tn.BrutoT AS NajavaBrutoT, tn.BrojKola AS NajavaBrojKola, t.Status,
        CASE WHEN t.Status = N'STORNIRAN' THEN NULL
             ELSE DENSE_RANK() OVER (PARTITION BY tn.NajavaID, CASE WHEN t.Status = N'STORNIRAN' THEN 1 ELSE 0 END ORDER BY tn.KmOd) END AS DeoRute,
        CASE WHEN t.Status = N'STORNIRAN' THEN NULL
             ELSE COUNT(*) OVER (PARTITION BY tn.NajavaID, tn.KmOd, CASE WHEN t.Status = N'STORNIRAN' THEN 1 ELSE 0 END) END AS DelovaBruto
FROM    planiranje.OP_TaskNajava tn
JOIN    planiranje.OP_Task t ON t.ID = tn.TaskID;
GO

SELECT N'OP_Task' AS Tabela, COUNT(*) AS Redova FROM planiranje.OP_Task
UNION ALL SELECT N'OP_TaskNajava', COUNT(*) FROM planiranje.OP_TaskNajava
UNION ALL SELECT N'OP_TaskStanica', COUNT(*) FROM planiranje.OP_TaskStanica
UNION ALL SELECT N'OP_TaskLokomotiva', COUNT(*) FROM planiranje.OP_TaskLokomotiva
UNION ALL SELECT N'OP_TaskVeza', COUNT(*) FROM planiranje.OP_TaskVeza;
PRINT N'OP_022 završena.';
GO

/* Poništavanje (ručno, po potrebi; briše sve taskove):

DROP VIEW planiranje.OP_vTaskNajava;
DROP TABLE planiranje.OP_TaskVeza;
DROP TABLE planiranje.OP_TaskLokomotiva;
DROP TABLE planiranje.OP_TaskStanica;
DROP TABLE planiranje.OP_TaskNajava;
DROP TABLE planiranje.OP_Task;
*/
