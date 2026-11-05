/* =====================================================================
   OP_009_MrezaSema.sql
   Dorada KP-26-11-03-Mreza i stanice: nove kolone za šifarnik stanica,
   pruge, mesta na pruzi, graf mreže i trase.

   - Koristi postojeće tabele: OP_Stanice, OP_Pruga, OP_PrugaMesto,
     OP_MrezaVeza i OP_Trase. Novih tabela nema.
   - Kolona koja već postoji se ne menja, pa skripta radi i na bazi na kojoj
     je ranije puštena stanice_mreza_SRB.sql. Može da se pokrene više puta.
   - Ne menja podatke. Podaci su u OP_010_MrezaPodaci.sql.
   - Pokreće se ručno, posle OP_001-OP_008.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @k TABLE (RB int IDENTITY(1,1), Tabela sysname, Kolona sysname, Tip nvarchar(100));
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'SifraIZS', N'int NULL');                  -- šifra službenog mesta iz Registra (5 cifara)
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'RegKljuc', N'nvarchar(80) NULL');         -- ključ mesta: U:šifra, N:naziv ili GR:pruga:rb
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'NazivRegistar', N'nvarchar(120) NULL');   -- kako piše u Registru
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'AltNazivi', N'nvarchar(300) NULL');       -- drugi nazivi iste šifre u Registru, odvojeni sa „; “
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'NazivStari', N'nvarchar(120) NULL');      -- naziv pre usklađivanja, za uparivanje trasa
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'VrstaKod', N'int NULL');                  -- kolona 6 Registra
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'GranicniPrelaz', N'bit NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'AdministrativnaLinija', N'bit NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'KiM', N'bit NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'KoordIzvor', N'varchar(20) NULL');        -- OSM, INTERPOL..., RUCNO
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'OsmId', N'bigint NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Pruge', N'nvarchar(200) NULL');           -- M1, R11...
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Cvor', N'bit NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Posednutost', N'nvarchar(10) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'OtvorenaZa', N'nvarchar(10) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Vaga', N'nvarchar(10) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Rampa', N'nvarchar(10) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'MaxDuzinaVozaAB', N'int NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'KolosekAB', N'nvarchar(30) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'MaxDuzinaVozaBA', N'int NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'KolosekBA', N'nvarchar(30) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Regulisanje', N'nvarchar(40) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'Osiguranje', N'nvarchar(10) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'NagibStanice', N'decimal(6,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'MinRadijus', N'int NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'NadmorskaVisina', N'decimal(7,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'DuplikatOd', N'int NULL');                -- glavna stanica; red ostaje zbog veze sa dbo.stanice
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Stanice', N'DatumAzuriranja', N'datetime2(0) NULL');

INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Pruga', N'Sifra', N'nvarchar(6) NULL');               -- M1, R11, L14, MP3
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Pruga', N'KiM', N'bit NOT NULL CONSTRAINT DF_OP_Pruga_KiM DEFAULT (0)');

INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_PrugaMesto', N'RegKljuc', N'nvarchar(80) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_PrugaMesto', N'VrstaKod', N'int NULL');

INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'KmIzvor', N'nvarchar(20) NULL');        -- REGISTAR, REGISTAR_STAC, GEOMETRIJA, NEPOZNATO
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'Klasa', N'nvarchar(10) NULL');          -- D4, C3..., isto kao OP_PrugaMesto.Klasa
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'OsovinskoT', N'decimal(4,1) NULL');     -- A 16, B 18, C 20, D 22,5 t
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'Vmax', N'int NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'Dvokolosecna', N'bit NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'MerodavniUspon', N'decimal(6,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'MerodavniPad', N'decimal(6,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'KiM', N'bit NOT NULL CONSTRAINT DF_OP_MrezaVeza_KiM DEFAULT (0)');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'GeomIzvor', N'nvarchar(20) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_MrezaVeza', N'GeomWKT', N'nvarchar(max) NULL');        -- LINESTRING(lon lat, ...), za mapu

INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmMagistralno', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmRegionalno', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmLokalno', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmManipulativno', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmDizel', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'KmElektro', N'decimal(10,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'NajmanjiVmax', N'int NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'NajveciUspon', N'decimal(6,2) NULL');
INSERT @k (Tabela, Kolona, Tip) VALUES (N'OP_Trase', N'RutaIzracunata', N'datetime2(0) NULL');

DECLARE @i int, @max int, @t sysname, @n sysname, @tip nvarchar(100), @sql nvarchar(500);
SELECT @i = 1, @max = MAX(RB) FROM @k;
WHILE @i <= @max
BEGIN
    SELECT @t = Tabela, @n = Kolona, @tip = Tip FROM @k WHERE RB = @i;
    IF COL_LENGTH(N'planiranje.' + @t, @n) IS NULL
    BEGIN
        SET @sql = N'ALTER TABLE planiranje.' + QUOTENAME(@t) + N' ADD ' + QUOTENAME(@n) + N' ' + @tip + N';';
        EXEC sys.sp_executesql @sql;
        PRINT N'Dodata ' + @t + N'.' + @n;
    END
    SET @i = @i + 1;
END
GO

-- Registar ima deonice kraće od 10 m, pa dužina dobija tri decimale.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'planiranje.OP_MrezaVeza') AND name = N'DuzinaKm' AND scale < 3)
BEGIN
    ALTER TABLE planiranje.OP_MrezaVeza DROP CONSTRAINT CK_OP_MrezaVeza_Duzina;
    ALTER TABLE planiranje.OP_MrezaVeza ALTER COLUMN DuzinaKm decimal(9,3) NOT NULL;
    ALTER TABLE planiranje.OP_MrezaVeza ADD CONSTRAINT CK_OP_MrezaVeza_Duzina CHECK (DuzinaKm > 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OP_Stanice_DuplikatOd')
    ALTER TABLE planiranje.OP_Stanice ADD CONSTRAINT FK_OP_Stanice_DuplikatOd FOREIGN KEY (DuplikatOd) REFERENCES planiranje.OP_Stanice(ID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Stanice_RegKljuc')
    CREATE INDEX IX_OP_Stanice_RegKljuc ON planiranje.OP_Stanice(RegKljuc) WHERE RegKljuc IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OP_Stanice_SifraIZS')
    CREATE INDEX IX_OP_Stanice_SifraIZS ON planiranje.OP_Stanice(SifraIZS) WHERE SifraIZS IS NOT NULL;
GO
PRINT N'OP_009 završena.';
GO
