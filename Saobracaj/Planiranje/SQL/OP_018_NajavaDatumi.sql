/* =====================================================================
   OP_018_NajavaDatumi.sql
   U dbo.Najava prazan datum je 1900-01-01, a ne NULL. Planiranje je to čitalo
   kao stvarnu predaju, pa je zatvaralo poslove čiji voz nije ni primljen.

   - OP_vNajava daje NULL umesto datuma pre 1950. godine.
   - Pogrešno zatvoreni poslovi se ponovo otvaraju: najava nije u statusu 7,
     nema stvarnu predaju, a Dispečer nije završio voz. Faza se vraća na 1, a
     na 2 ako posao ima lokomotivu; sledeće usklađivanje je diže prema najavi.
     Promena se upisuje u OP_PosaoDogadjaj.
   - Lažna stvarna vremena na poslu (1900-01-01) se brišu.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_017.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
DECLARE @sql nvarchar(max);
SET @sql = N'ALTER VIEW planiranje.OP_vNajava
AS
SELECT  n.ID                    AS NajavaID,
        n.BrojNajave,
        n.Otpravna              AS OtpravnaID,
        n.Uputna                AS UputnaID,
        n.OtpravnaGr            AS OtpravnaGrID,
        n.Uputnagr              AS UputnaGrID,
        CASE WHEN m1.Drzava = N''SRB'' OR s1.Drzava IN (N''SRB'', N''RS'') OR NULLIF(n.OtpravnaGr, 0) IS NULL OR RTRIM(g1.Opis) = N''Nema'' THEN n.Otpravna ELSE n.OtpravnaGr END AS PlanOtpravnaID,
        CASE WHEN m2.Drzava = N''SRB'' OR s2.Drzava IN (N''SRB'', N''RS'') OR NULLIF(n.Uputnagr, 0) IS NULL OR RTRIM(g2.Opis) = N''Nema'' THEN n.Uputna ELSE n.Uputnagr END AS PlanUputnaID,
        CASE WHEN m1.Drzava = N''SRB'' OR s1.Drzava IN (N''SRB'', N''RS'') OR NULLIF(n.OtpravnaGr, 0) IS NULL OR RTRIM(g1.Opis) = N''Nema'' THEN RTRIM(s1.Opis) ELSE RTRIM(g1.Opis) END AS PlanOtpravna,
        CASE WHEN m2.Drzava = N''SRB'' OR s2.Drzava IN (N''SRB'', N''RS'') OR NULLIF(n.Uputnagr, 0) IS NULL OR RTRIM(g2.Opis) = N''Nema'' THEN RTRIM(s2.Opis) ELSE RTRIM(g2.Opis) END AS PlanUputna,
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
        CASE WHEN n.PredvidjenoPrimanje < ''19500101'' THEN NULL ELSE n.PredvidjenoPrimanje END AS PredvidjenoPrimanje,
        CASE WHEN n.StvarnoPrimanje < ''19500101'' THEN NULL ELSE n.StvarnoPrimanje END AS StvarnoPrimanje,
        CASE WHEN n.PredvidjenaPredaja < ''19500101'' THEN NULL ELSE n.PredvidjenaPredaja END AS PredvidjenaPredaja,
        CASE WHEN n.StvarnaPredaja < ''19500101'' THEN NULL ELSE n.StvarnaPredaja END AS StvarnaPredaja,
        n.Status                AS StatusNajave,
        @STATUS@ AS StatusOpis,
        n.NapomenaPriprema
FROM    dbo.Najava n
LEFT JOIN dbo.stanice s1 ON s1.ID = n.Otpravna
LEFT JOIN dbo.stanice s2 ON s2.ID = n.Uputna
LEFT JOIN dbo.stanice g1 ON g1.ID = n.OtpravnaGr
LEFT JOIN dbo.stanice g2 ON g2.ID = n.Uputnagr
LEFT JOIN planiranje.OP_Stanice o1 ON o1.IzvorniID = n.Otpravna
LEFT JOIN planiranje.OP_Stanice m1 ON m1.ID = COALESCE(o1.DuplikatOd, o1.ID)
LEFT JOIN planiranje.OP_Stanice o2 ON o2.IzvorniID = n.Uputna
LEFT JOIN planiranje.OP_Stanice m2 ON m2.ID = COALESCE(o2.DuplikatOd, o2.ID)@SPOJ@;';
SET @sql = REPLACE(@sql, N'@STATUS@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'CAST(NULL AS nvarchar(100))' ELSE N'RTRIM(st.Opis)' END);
SET @sql = REPLACE(@sql, N'@SPOJ@', CASE WHEN OBJECT_ID(N'dbo.StatusVoza', N'U') IS NULL THEN N'' ELSE N' LEFT JOIN dbo.StatusVoza st ON st.ID = n.Status' END);
EXEC sys.sp_executesql @sql;
GO
BEGIN TRAN;
IF OBJECT_ID('tempdb..#Otvori') IS NOT NULL DROP TABLE #Otvori;
SELECT p.ID INTO #Otvori
FROM   planiranje.OP_Posao p
JOIN   planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID
WHERE  p.Status = N'ZATVOREN' AND ISNULL(n.StatusNajave, 0) <> 7 AND n.StvarnaPredaja IS NULL
  AND  NOT EXISTS (SELECT 1 FROM dbo.OperativniPlanVozovi v WHERE v.AnnouncementFullId = CAST(p.NajavaID AS nvarchar(30)) AND v.Completed = 1);

UPDATE p
SET    Status = N'AKTIVAN', Faza = CASE WHEN EXISTS (SELECT 1 FROM planiranje.OP_PosaoLokomotiva l WHERE l.PosaoID = p.ID) THEN 2 ELSE 1 END, Izmenio = N'OP_018', Izmenjeno = SYSDATETIME()
FROM   planiranje.OP_Posao p JOIN #Otvori o ON o.ID = p.ID;
PRINT N'Ponovo otvoreni poslovi: ' + CAST(@@ROWCOUNT AS nvarchar(10));

INSERT planiranje.OP_PosaoDogadjaj (PosaoID, Tip, Opis, Korisnik)
SELECT ID, N'FAZA_ISPRAVKA', N'Posao je ponovo otvoren: prazan datum predaje iz najave bio je pročitan kao stvarna predaja.', N'OP_018' FROM #Otvori;

UPDATE planiranje.OP_Posao SET StvarniPolazak = NULL WHERE StvarniPolazak < '19500101';
UPDATE planiranje.OP_Posao SET StvarniDolazak = NULL WHERE StvarniDolazak < '19500101';
COMMIT;
GO
SELECT p.ID, p.NajavaID, p.Status, p.Faza, n.StatusNajave, n.StatusOpis, n.StvarnaPredaja
FROM planiranje.OP_Posao p JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID ORDER BY p.ID;
GO
PRINT N'OP_018 završena.';
GO
