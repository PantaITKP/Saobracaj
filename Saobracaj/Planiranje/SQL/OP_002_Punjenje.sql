/* =====================================================================
   OP_002_Punjenje.sql
   Prvo punjenje OP_ tabela iz postojećih tabela programa.

   - Pokreće se posle OP_001_Sema.sql.
   - Postojeće tabele se SAMO ČITAJU.
   - Svaki korak je u svojoj transakciji i preskače se ako je već urađen
     (proverava se po kolonama Izvorni*), pa se skripta može ponoviti.
   - Kolone za koje nismo sigurni da postoje u svim bazama čitaju se
     dinamičkim SQL-om posle provere COL_LENGTH, da skripta ne padne.
   Redosled: 1 Stanice, 2 Serije, 3 Lokomotive, 4 Pruge (samo zaglavlja,
             za telegrame i kvalifikacije), 7 Trase, 8 Osoblje (samo dopuna),
             9 Kvalifikacije, 10 Parametri, 11 Turnusi.
   dbo.PrugaStavke se NE čita (menja se u starom programu).
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* 1. OP_Stanice  <-  dbo.stanice ---------------------------------------- */
BEGIN TRAN;
INSERT planiranje.OP_Stanice (Naziv, Kod, Drzava, Granicna, Latitude, Longitude, Izvor, IzvorniID, Kreirao)
SELECT  RTRIM(s.Opis),
        NULLIF(RTRIM(s.Kod), N''),
        NULLIF(RTRIM(s.Drzava), N''),
        CASE WHEN s.Granicna = 1 THEN 1 ELSE 0 END,
        s.Latitude, s.Longitude,
        N'STANICE', s.ID, N'OP_002'
FROM    dbo.stanice s
WHERE   NULLIF(RTRIM(s.Opis), N'') IS NOT NULL
  AND   NOT EXISTS (SELECT 1 FROM planiranje.OP_Stanice o WHERE o.IzvorniID = s.ID);
PRINT N'1. OP_Stanice: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';

-- Kod koji ima 5 cifara tretira se kao UIC šifra (proveriti u OP_004_Provera.sql).
UPDATE planiranje.OP_Stanice
SET    UIC = Kod
WHERE  UIC IS NULL AND Kod IS NOT NULL AND LEN(Kod) = 5 AND Kod NOT LIKE N'%[^0-9]%';
COMMIT;
GO

/* 2. OP_SerijeLokomotiva  <-  dbo.LokomotivaSerija ---------------------- */
IF OBJECT_ID(N'dbo.LokomotivaSerija', N'U') IS NOT NULL
BEGIN
    BEGIN TRAN;
    DECLARE @sql nvarchar(max) = N'
    INSERT planiranje.OP_SerijeLokomotiva (Oznaka, Opis, MasaT, SnagaKW, IzvorniID)
    SELECT  LEFT(RTRIM(ls.Oznaka), 20), NULLIF(RTRIM(ls.Opis), N''''),
            ' + CASE WHEN COL_LENGTH('dbo.LokomotivaSerija', 'Tezina') IS NOT NULL THEN N'ls.Tezina' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH('dbo.LokomotivaSerija', 'Snaga')  IS NOT NULL THEN N'ls.Snaga'  ELSE N'NULL' END + N',
            ls.ID
    FROM    dbo.LokomotivaSerija ls
    WHERE   NULLIF(RTRIM(ls.Oznaka), N'''') IS NOT NULL
      AND   NOT EXISTS (SELECT 1 FROM planiranje.OP_SerijeLokomotiva o WHERE o.Oznaka = LEFT(RTRIM(ls.Oznaka), 20));';
    EXEC sys.sp_executesql @sql;
    PRINT N'2. OP_SerijeLokomotiva: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';
    COMMIT;
END
ELSE PRINT N'2. dbo.LokomotivaSerija ne postoji, serije se unose ručno.';
GO

/* 3. OP_Lokomotive  <-  dbo.Mesta (Lokomotiva = 1) ---------------------- */
IF OBJECT_ID('tempdb..#lok') IS NOT NULL DROP TABLE #lok;
CREATE TABLE #lok (SmSifra nvarchar(20) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY, Naziv nvarchar(100) COLLATE DATABASE_DEFAULT NULL, Dizel int NULL, Masa decimal(10,2) NULL);

DECLARE @sql nvarchar(max) = N'
INSERT #lok (SmSifra, Naziv, Dizel, Masa)
SELECT  LEFT(RTRIM(m.SmSifra), 20), NULLIF(RTRIM(m.SmNaziv), N''''),
        ' + CASE WHEN COL_LENGTH('dbo.Mesta', 'Dizel') IS NOT NULL THEN N'CAST(m.Dizel AS int)' ELSE N'NULL' END + N',
        ' + CASE WHEN COL_LENGTH('dbo.Mesta', 'Masa')      IS NOT NULL THEN N'CAST(m.Masa AS decimal(10,2))'
                 -- forma Lokomotive prikazuje SmPogDela kao "Masa"
                 WHEN COL_LENGTH('dbo.Mesta', 'SmPogDela') IS NOT NULL THEN N'CAST(m.SmPogDela AS decimal(10,2))'
                 ELSE N'NULL' END + N'
FROM    dbo.Mesta m
WHERE   m.Lokomotiva = 1 AND NULLIF(RTRIM(m.SmSifra), N'''') IS NOT NULL;';
EXEC sys.sp_executesql @sql;

BEGIN TRAN;
INSERT planiranje.OP_Lokomotive (Broj, Naziv, SerijaID, MasaT, IzvorniSmSifra)
SELECT  l.SmSifra, l.Naziv, s.ID, l.Masa, l.SmSifra
FROM    #lok l
OUTER APPLY (
    -- serija čija se oznaka nalazi u broju lokomotive; prednost ima početak broja i duža oznaka
    SELECT TOP 1 sr.ID
    FROM   planiranje.OP_SerijeLokomotiva sr
    WHERE  REPLACE(l.SmSifra, N' ', N'') LIKE N'%' + REPLACE(sr.Oznaka, N' ', N'') + N'%'
    ORDER BY CASE WHEN REPLACE(l.SmSifra, N' ', N'') LIKE REPLACE(sr.Oznaka, N' ', N'') + N'%' THEN 0 ELSE 1 END,
             LEN(sr.Oznaka) DESC
) s
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Lokomotive o WHERE o.IzvorniSmSifra = l.SmSifra OR o.Broj = l.SmSifra);
PRINT N'3. OP_Lokomotive: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';

-- Vrsta vuče serije prema većini lokomotiva (Mesta.Dizel).
UPDATE sr
SET    VrstaVuce = CASE WHEN x.Dizel * 2 > x.Ukupno THEN 'D' ELSE 'E' END
FROM   planiranje.OP_SerijeLokomotiva sr
JOIN  (SELECT o.SerijaID, COUNT(*) AS Ukupno, SUM(CASE WHEN l.Dizel > 0 THEN 1 ELSE 0 END) AS Dizel
       FROM planiranje.OP_Lokomotive o JOIN #lok l ON l.SmSifra = o.IzvorniSmSifra
       WHERE o.SerijaID IS NOT NULL AND l.Dizel IS NOT NULL
       GROUP BY o.SerijaID) x ON x.SerijaID = sr.ID
WHERE  sr.VrstaVuce IS NULL;
COMMIT;

-- GPS: veza sa Technodis-om (dbo.LocoTechnodis).
IF OBJECT_ID(N'dbo.LocoTechnodis', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.LocoTechnodis', 'Lokomotiva') IS NOT NULL
   AND COL_LENGTH('dbo.LocoTechnodis', 'IDLokomotive') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
    UPDATE o
    SET    TechnodisID = LEFT(CONVERT(nvarchar(50), t.IDLokomotive), 50), ImaGPS = 1
    FROM   planiranje.OP_Lokomotive o
    JOIN   dbo.LocoTechnodis t ON REPLACE(RTRIM(CONVERT(nvarchar(50), t.Lokomotiva)), N'' '', N'''') = REPLACE(o.Broj, N'' '', N'''')
    WHERE  o.TechnodisID IS NULL;';
    PRINT N'3. GPS (Technodis) povezan: ' + CAST(@@ROWCOUNT AS nvarchar(10));
END
DROP TABLE #lok;
GO

/* 4. OP_Pruga  <-  dbo.Pruga, samo zaglavlja (verzija "Migracija") ------
   Potrebno za vezu sa dbo.Telegrami.PrugaID i dbo.DelavciPrugaMasin.
   Mesta na pruzi dolaze tek iz uvoza Registra. */
IF OBJECT_ID(N'dbo.Pruga', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Pruga WHERE IzvorniID IS NOT NULL)
BEGIN
    BEGIN TRAN;
    DECLARE @verzija int;
    INSERT planiranje.OP_RegistarVerzija (Naziv, Status, Izvor, Napomena, Uvezao, Potvrdio, Potvrdjeno)
    VALUES (N'Migracija iz dbo.Pruga (samo zaglavlja)',
            CASE WHEN EXISTS (SELECT 1 FROM planiranje.OP_RegistarVerzija WHERE Status = N'VAZECA') THEN N'ARHIVA' ELSE N'VAZECA' END,
            N'MIGRACIJA', N'Početno stanje pre prvog uvoza Registra.xlsx', N'OP_002', N'OP_002', SYSDATETIME());
    SET @verzija = SCOPE_IDENTITY();

    DECLARE @sql nvarchar(max) = N'
    INSERT planiranje.OP_Pruga (RegistarVerzijaID, IdentBroj, Naziv, Elektrificirana, IzvorniID)
    SELECT  @v, NULLIF(RTRIM(p.Oznaka), N''''), ISNULL(NULLIF(RTRIM(p.Opis), N''''), N''Pruga '' + CAST(p.ID AS nvarchar(10))),
            ' + CASE WHEN COL_LENGTH('dbo.Pruga', 'Elektrificirana') IS NOT NULL THEN N'CASE WHEN p.Elektrificirana > 0 THEN 1 ELSE 0 END' ELSE N'NULL' END + N',
            p.ID
    FROM    dbo.Pruga p;';
    EXEC sys.sp_executesql @sql, N'@v int', @v = @verzija;
    PRINT N'4. OP_Pruga: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';
    COMMIT;
END
ELSE PRINT N'4. Pruge preskočene (već uvezene ili dbo.Pruga ne postoji).';
GO

/* 5–6. Mesta na pruzi i veze iz pruga: NE PUNE SE iz dbo.PrugaStavke.
   PrugaStavke se trenutno menja u starom programu. Mesta na pruzi i veze
   dolaze iz uvoza Registra (forma Registar pruga) i iz OP_003_Mreza.sql. */

/* 7. OP_TraseVerzija / OP_Trase  <-  dbo.OperativniPlanTraseDokument / OperativniPlanTrasa
   Svaki uvezeni Excel postaje jedna verzija. Aktivni dokument
   (OperativniPlanPodesavanje.AktivniDokumentID) postaje VAZECA, ostali ARHIVA. */
IF OBJECT_ID(N'dbo.OperativniPlanTraseDokument', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OperativniPlanTrasa', N'U') IS NOT NULL
BEGIN
    DECLARE @aktivni bigint = NULL;
    IF OBJECT_ID(N'dbo.OperativniPlanPodesavanje', N'U') IS NOT NULL
        SELECT @aktivni = AktivniDokumentID FROM dbo.OperativniPlanPodesavanje WHERE ID = 1;

    DECLARE @sql nvarchar(max) = N'
    INSERT planiranje.OP_TraseVerzija (Naziv, Godina, VaziOd, VaziDo, Status, Izvor, OriginalniNaziv, Sha256, Sadrzaj, Uvezao, IzvorniDokumentID, Objavio, Objavljeno)
    SELECT  N''Trase '' + CAST(g.Godina AS nvarchar(4)) + N'' · '' + ISNULL(d.OriginalniNaziv, N''''),
            g.Godina,
            COALESCE(' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'VaziOd') IS NOT NULL THEN N'd.VaziOd' ELSE N'NULL' END + N', CAST(CAST(g.Godina AS char(4)) + ''0101'' AS date)),
            COALESCE(' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'VaziDo') IS NOT NULL THEN N'd.VaziDo' ELSE N'NULL' END + N', CAST(CAST(g.Godina AS char(4)) + ''1231'' AS date)),
            -- aktivni dokument postaje važeći, osim ako za tu godinu važeća verzija već postoji
            CASE WHEN d.ID = @aktivni AND NOT EXISTS (SELECT 1 FROM planiranje.OP_TraseVerzija v WHERE v.Godina = g.Godina AND v.Status = N''VAZECA'')
                 THEN N''VAZECA'' ELSE N''ARHIVA'' END,
            N''MIGRACIJA'', LEFT(d.OriginalniNaziv, 260),
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'Sha256') IS NOT NULL THEN N'LEFT(CONVERT(nvarchar(64), d.Sha256), 64)' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'Sadrzaj') IS NOT NULL THEN N'd.Sadrzaj' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'Uvezao') IS NOT NULL THEN N'ISNULL(LEFT(d.Uvezao, 50), N''OP_002'')' ELSE N'N''OP_002''' END + N',
            d.ID,
            CASE WHEN d.ID = @aktivni THEN N''OP_002'' END,
            CASE WHEN d.ID = @aktivni THEN SYSDATETIME() END
    FROM    dbo.OperativniPlanTraseDokument d
    CROSS APPLY (SELECT COALESCE(' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'Godina') IS NOT NULL THEN N'CAST(d.Godina AS int), ' ELSE N'' END
                 + CASE WHEN COL_LENGTH('dbo.OperativniPlanTraseDokument', 'VaziDo') IS NOT NULL THEN N'YEAR(d.VaziDo), ' ELSE N'' END + N'YEAR(SYSDATETIME())) AS Godina) g
    WHERE   NOT EXISTS (SELECT 1 FROM planiranje.OP_TraseVerzija v WHERE v.IzvorniDokumentID = d.ID);';

    BEGIN TRAN;
    EXEC sys.sp_executesql @sql, N'@aktivni bigint', @aktivni = @aktivni;

    SET @sql = N'
    INSERT planiranje.OP_Trase (VerzijaID, BrojVoza, PocetnaStanicaID, KrajnjaStanicaID, PocetnaNaziv, KrajnjaNaziv, Relacija,
            VremePolaska, VremeDolaska, DanPlus, DaniSaobracaja, MaxMasaT, MaxDuzinaM, ProcenatKocenja, Cena,
            SerijaLokomotive, Napomena, IzvorniRouteID, Kreirao)
    SELECT  v.ID, LEFT(RTRIM(t.Train), 20), s1.ID, s2.ID,
            LEFT(RTRIM(t.[From]), 100), LEFT(RTRIM(t.[To]), 100),
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTrasa', 'Relation') IS NOT NULL THEN N'LEFT(NULLIF(RTRIM(t.Relation), N''''), 300)' ELSE N'NULL' END + N',
            CASE WHEN ISDATE(t.Departure) = 1 THEN CONVERT(time(0), t.Departure) END,
            CASE WHEN ISDATE(t.Arrival) = 1 THEN CONVERT(time(0), t.Arrival) END,
            CASE WHEN ISDATE(t.Arrival) = 1 AND ISDATE(t.Departure) = 1 AND CONVERT(time(0), t.Arrival) < CONVERT(time(0), t.Departure) THEN 1 ELSE 0 END,
            LEFT(NULLIF(RTRIM(t.Days), N''''), 50), t.MaxWeight,
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTrasa', 'LengthValue') IS NOT NULL THEN N't.LengthValue' ELSE N'CASE WHEN ISNUMERIC(t.Length + ''''e0'''') = 1 THEN CAST(CAST(t.Length AS float) AS decimal(10,2)) END' END + N',
            ' + CASE WHEN COL_LENGTH('dbo.OperativniPlanTrasa', 'BrakePercent') IS NOT NULL THEN N't.BrakePercent' ELSE N'NULL' END + N',
            t.Price, LEFT(NULLIF(RTRIM(t.Locomotive), N''''), 50), LEFT(NULLIF(RTRIM(t.Note), N''''), 1000),
            t.ID, N''OP_002''
    FROM    dbo.OperativniPlanTrasa t
    JOIN    planiranje.OP_TraseVerzija v ON v.IzvorniDokumentID = t.DocumentID
    OUTER APPLY (SELECT TOP 1 ID FROM planiranje.OP_Stanice s WHERE s.Naziv = RTRIM(t.[From]) COLLATE Latin1_General_CI_AI ORDER BY CASE WHEN s.IzvorniID IS NULL THEN 1 ELSE 0 END, s.ID) s1
    OUTER APPLY (SELECT TOP 1 ID FROM planiranje.OP_Stanice s WHERE s.Naziv = RTRIM(t.[To])   COLLATE Latin1_General_CI_AI ORDER BY CASE WHEN s.IzvorniID IS NULL THEN 1 ELSE 0 END, s.ID) s2
    WHERE   NOT EXISTS (SELECT 1 FROM planiranje.OP_Trase o WHERE o.IzvorniRouteID = t.ID);';
    EXEC sys.sp_executesql @sql;
    PRINT N'7. OP_Trase: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';

    UPDATE planiranje.OP_Trase
    SET    Greska = CASE WHEN PocetnaStanicaID IS NULL THEN N'Nepoznata početna stanica. ' ELSE N'' END
                  + CASE WHEN KrajnjaStanicaID IS NULL THEN N'Nepoznata krajnja stanica. ' ELSE N'' END
                  + CASE WHEN VremePolaska IS NULL OR VremeDolaska IS NULL THEN N'Nedostaju vremena.' ELSE N'' END
    WHERE  Kreirao = N'OP_002' AND (PocetnaStanicaID IS NULL OR KrajnjaStanicaID IS NULL OR VremePolaska IS NULL OR VremeDolaska IS NULL);

    -- veza sa godišnjim šifarnikom dbo.Trase (za obračun)
    IF OBJECT_ID(N'dbo.Trase', N'U') IS NOT NULL
        EXEC sys.sp_executesql N'
        UPDATE o
        SET    IzvornaTrasaID = x.ID
        FROM   planiranje.OP_Trase o
        JOIN   planiranje.OP_TraseVerzija v ON v.ID = o.VerzijaID
        CROSS APPLY (SELECT MAX(t.ID) AS ID FROM dbo.Trase t
                     WHERE RTRIM(t.Voz) = o.BrojVoza AND RTRIM(CONVERT(nvarchar(10), t.Godina)) = CAST(v.Godina AS nvarchar(10))) x
        WHERE  o.IzvornaTrasaID IS NULL AND x.ID IS NOT NULL;';
    COMMIT;
END
ELSE PRINT N'7. Trase preskočene (nema OperativniPlanTrasa).';
GO

/* 8. OP_OsobljePlan  <-  dbo.Delavci ------------------------------------
   Osoblje se ne kopira: forme čitaju dbo.Delavci kroz planiranje.OP_vOsoblje.
   Ovde se upisuje samo dopuna za planiranje (DeSifra + predlog matične
   stanice iz mesta stanovanja) za aktivne radnike koji imaju ulogu. */
IF OBJECT_ID(N'dbo.Delavci', N'U') IS NOT NULL
BEGIN
    BEGIN TRAN;
    INSERT planiranje.OP_OsobljePlan (DeSifra, UPlaniranju, Izmenio)
    SELECT  v.DeSifra, 1, N'OP_002'
    FROM    planiranje.OP_vOsoblje v
    WHERE   v.Aktivan = 1
      AND   (v.Masinovodja = 1 OR v.PregledacKola = 1 OR v.Vozovodja = 1 OR v.Manevrista = 1)
      AND   NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePlan p WHERE p.DeSifra = v.DeSifra);
    PRINT N'8. OP_OsobljePlan: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';

    -- predlog matične stanice iz mesta stanovanja (proveriti u formi Osoblje)
    IF COL_LENGTH('dbo.Delavci', 'DeKrajBivS') IS NOT NULL
        EXEC sys.sp_executesql N'
        UPDATE p SET MaticnaStanicaID = x.ID
        FROM   planiranje.OP_OsobljePlan p
        JOIN   dbo.Delavci d ON d.DeSifra = p.DeSifra
        CROSS APPLY (SELECT TOP 1 s.ID FROM planiranje.OP_Stanice s
                     WHERE s.Naziv = RTRIM(CONVERT(nvarchar(100), d.DeKrajBivS)) COLLATE Latin1_General_CI_AI
                     ORDER BY s.ID) x
        WHERE  p.MaticnaStanicaID IS NULL;';
    COMMIT;
END
ELSE PRINT N'8. dbo.Delavci ne postoji.';
GO

/* 9. OP_OsobljeKvalifikacija  <-  dbo.DelavciPrugaMasin, dbo.DelavciLokMasin
   Kvalifikacije su podatak planiranja (menjaju se u novoj formi), pa se kopiraju. */
BEGIN TRAN;
IF OBJECT_ID(N'dbo.DelavciPrugaMasin', N'U') IS NOT NULL AND COL_LENGTH('dbo.DelavciPrugaMasin', 'Pruga') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
    INSERT planiranje.OP_OsobljeKvalifikacija (DeSifra, Vrsta, PrugaID, Napomena)
    SELECT DISTINCT o.DeSifra, N''PRUGA'', p.ID, N''Migracija iz DelavciPrugaMasin''
    FROM   dbo.DelavciPrugaMasin dp
    JOIN   planiranje.OP_OsobljePlan o ON o.DeSifra = dp.DeSifra
    JOIN   planiranje.OP_Pruga p ON p.IzvorniID = dp.Pruga
    WHERE  NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeKvalifikacija k WHERE k.DeSifra = o.DeSifra AND k.Vrsta = N''PRUGA'' AND k.PrugaID = p.ID);';
    PRINT N'9. Kvalifikacije za pruge: ' + CAST(@@ROWCOUNT AS nvarchar(10));
END

IF OBJECT_ID(N'dbo.DelavciLokMasin', N'U') IS NOT NULL
BEGIN
    -- kolona sa lokomotivom/serijom nije ista u svim verzijama baze
    DECLARE @kol sysname =
        CASE WHEN COL_LENGTH('dbo.DelavciLokMasin', 'SmSifra')    IS NOT NULL THEN N'SmSifra'
             WHEN COL_LENGTH('dbo.DelavciLokMasin', 'Lokomotiva') IS NOT NULL THEN N'Lokomotiva'
             WHEN COL_LENGTH('dbo.DelavciLokMasin', 'Serija')     IS NOT NULL THEN N'Serija' END;
    IF @kol IN (N'SmSifra', N'Lokomotiva')
    BEGIN
        DECLARE @sql nvarchar(max) = N'
        INSERT planiranje.OP_OsobljeKvalifikacija (DeSifra, Vrsta, SerijaID, Napomena)
        SELECT DISTINCT o.DeSifra, N''SERIJA'', l.SerijaID, N''Migracija iz DelavciLokMasin''
        FROM   dbo.DelavciLokMasin dl
        JOIN   planiranje.OP_OsobljePlan o ON o.DeSifra = dl.DeSifra
        JOIN   planiranje.OP_Lokomotive l ON l.IzvorniSmSifra = LEFT(RTRIM(CONVERT(nvarchar(50), dl.' + QUOTENAME(@kol) + N')), 20)
        WHERE  l.SerijaID IS NOT NULL
          AND  NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeKvalifikacija k WHERE k.DeSifra = o.DeSifra AND k.Vrsta = N''SERIJA'' AND k.SerijaID = l.SerijaID);';
        EXEC sys.sp_executesql @sql;
        PRINT N'9. Kvalifikacije za serije: ' + CAST(@@ROWCOUNT AS nvarchar(10));
    END
    ELSE IF @kol = N'Serija'
    BEGIN
        EXEC sys.sp_executesql N'
        INSERT planiranje.OP_OsobljeKvalifikacija (DeSifra, Vrsta, SerijaID, Napomena)
        SELECT DISTINCT o.DeSifra, N''SERIJA'', s.ID, N''Migracija iz DelavciLokMasin''
        FROM   dbo.DelavciLokMasin dl
        JOIN   planiranje.OP_OsobljePlan o ON o.DeSifra = dl.DeSifra
        JOIN   planiranje.OP_SerijeLokomotiva s ON s.Oznaka = RTRIM(CONVERT(nvarchar(50), dl.Serija))
        WHERE  NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljeKvalifikacija k WHERE k.DeSifra = o.DeSifra AND k.Vrsta = N''SERIJA'' AND k.SerijaID = s.ID);';
        PRINT N'9. Kvalifikacije za serije: ' + CAST(@@ROWCOUNT AS nvarchar(10));
    END
    ELSE PRINT N'9. DelavciLokMasin nema prepoznatu kolonu lokomotive; kvalifikacije za serije unose se ručno.';
END
COMMIT;
GO

/* 10. OP_Parametri (početne vrednosti, menjaju se u formi Parametri) ---- */
INSERT planiranje.OP_Parametri (Kljuc, Vrednost, Jedinica, Opis, Izmenio)
SELECT s.Kljuc, s.Vrednost, s.Jedinica, s.Opis, N'OP_002'
FROM (VALUES
    (N'OBRADA_PRIJEM_MIN',         60.0, N'min', N'Vreme obrade pri prijemu, ako za stanicu nije zadato drugačije'),
    (N'OBRADA_PREDAJA_MIN',        60.0, N'min', N'Vreme obrade pri predaji, ako za stanicu nije zadato drugačije'),
    (N'BRZINA_PRAZNE_VOZNJE_KMH',  50.0, N'km/h', N'Prosečna brzina prazne vožnje lokomotive za proračun'),
    (N'GPS_STAROST_MAX_MIN',       60.0, N'min', N'Starija GPS pozicija se ne koristi u proračunu'),
    (N'ODMOR_POSLE_SMENE_H',       12.0, N'h',   N'Najkraći odmor osoblja između dve smene'),
    (N'MAX_RAD_H',                 12.0, N'h',   N'Najduže trajanje rada mašinovođe u jednoj smeni'),
    (N'PLAN_HORIZONT_DANA',         7.0, N'dan', N'Dužina nedeljnog plana'),
    (N'REZERVA_PRE_POLASKA_MIN',   30.0, N'min', N'Upozorenje dispečeru ako osoblje nije potvrdilo zadatak pre polaska'),
    (N'TEZINA_RAVNOMERNOST',       30.0, N'min', N'Ocena ponude lokomotiva: dodatak po poslu iznad proseka nedelje'),
    (N'TEZINA_SERVIS',            120.0, N'min', N'Ocena ponude lokomotiva: dodatak ako je servis u roku od 48 h posle posla'),
    (N'MASA_LOK_U_VUCI',            0.0, N'0/1', N'1 = masa lokomotive se dodaje bruto masi voza pri proveri vuče (otvoreno pitanje)')
) AS s (Kljuc, Vrednost, Jedinica, Opis)
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Parametri t
                  WHERE t.Kljuc = s.Kljuc AND t.StanicaID IS NULL AND t.StanicaDoID IS NULL AND t.SerijaID IS NULL);
PRINT N'10. OP_Parametri: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';
GO

/* 11. OP_Turnus (primeri sa ekrana Dostupnost; menjaju se u formi) ----- */
INSERT planiranje.OP_Turnus (Naziv, Pocetak, TrajanjeMin, DanaRada, DanaOdmora, Nocna)
SELECT s.Naziv, s.Pocetak, s.TrajanjeMin, s.DanaRada, s.DanaOdmora, s.Nocna
FROM (VALUES
    (N'Dnevna 06–18, 2 rada / 2 odmor', CAST('06:00' AS time(0)), 720, 2, 2, 0),
    (N'Noćna 18–06, 2 rada / 2 odmor',  CAST('18:00' AS time(0)), 720, 2, 2, 1),
    (N'Dnevna 06–18, 5 rada / 2 odmor', CAST('06:00' AS time(0)), 720, 5, 2, 0)
) AS s (Naziv, Pocetak, TrajanjeMin, DanaRada, DanaOdmora, Nocna)
WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_Turnus t WHERE t.Naziv = s.Naziv);
PRINT N'11. OP_Turnus: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N' novih.';
GO

PRINT N'OP_002_Punjenje.sql završen. Pokrenite OP_003_Mreza.sql, pa OP_004_Provera.sql.';
GO
