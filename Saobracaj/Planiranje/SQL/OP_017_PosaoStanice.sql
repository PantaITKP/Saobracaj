/* =====================================================================
   OP_017_PosaoStanice.sql
   Plan: poslovi sa stanicom „Nema“ dobijaju merodavne stanice najave, a
   pogledi daju naziv statusa najave.

   - Poslovi napravljeni pre OP_014 imaju kao prijem ili predaju stanicu „Nema“,
     pa za njih ruta ne postoji. Menja se samo taj podatak, u svim statusima.
   - OP_vNajava i OP_vPosao dobijaju StatusOpis iz dbo.StatusVoza, a OP_vPosao i StatusNajave.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_016.
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
        n.PredvidjenoPrimanje,
        n.StvarnoPrimanje,
        n.PredvidjenaPredaja,
        n.StvarnaPredaja,
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
ALTER VIEW planiranje.OP_vPosao
AS
SELECT  p.ID, p.NajavaID, p.RBDeonice, p.Faza, p.Status, p.TrasaID,
        p.PrijemStanicaID, p.PredajaStanicaID,
        p.PlaniraniPrijem, p.PlaniraniPolazak, p.PlaniraniDolazak, p.PlaniranaPredaja,
        p.StvarniPolazak, p.StvarniDolazak,
        p.PotrebnoLokomotiva, p.Projekat, p.OperativniVozID, p.Napomena,
        n.BrojNajave, n.Otpravna AS NajavaOtpravna, n.Uputna AS NajavaUputna,
        n.OtpravnaGr AS NajavaOtpravnaGr, n.UputnaGr AS NajavaUputnaGr,
        n.PlanOtpravnaID, n.PlanUputnaID, n.PlanOtpravna, n.PlanUputna,
        n.StatusNajave, n.StatusOpis,
        n.BrutoT AS NajavaBrutoT, n.DuzinaM AS NajavaDuzinaM, n.BrojKola AS NajavaBrojKola,
        n.SerijaVagona AS NajavaSerijaVagona, n.Tovareno AS NajavaTovareno, n.RID AS NajavaRID,
        n.PredvidjenoPrimanje, n.PredvidjenaPredaja, n.StvarnoPrimanje, n.StvarnaPredaja,
        p.RowVer
FROM    planiranje.OP_Posao p
LEFT JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID;
GO
BEGIN TRAN;
UPDATE p
SET    PrijemStanicaID = CASE WHEN a.Naziv = N'Nema' THEN (SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanOtpravnaID ORDER BY s.ID) ELSE p.PrijemStanicaID END,
       PredajaStanicaID = CASE WHEN b.Naziv = N'Nema' THEN (SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanUputnaID ORDER BY s.ID) ELSE p.PredajaStanicaID END
FROM   planiranje.OP_Posao p
JOIN   planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID
LEFT JOIN planiranje.OP_Stanice a ON a.ID = p.PrijemStanicaID
LEFT JOIN planiranje.OP_Stanice b ON b.ID = p.PredajaStanicaID
WHERE  a.Naziv = N'Nema' OR b.Naziv = N'Nema';
PRINT N'Poslova sa stanicom Nema ispravljeno: ' + CAST(@@ROWCOUNT AS nvarchar(10));
COMMIT;
GO
SELECT p.ID AS Posao_i_dalje_sa_Nema, p.NajavaID, p.PlanOtpravna, p.PlanUputna
FROM planiranje.OP_vPosao p
LEFT JOIN planiranje.OP_Stanice a ON a.ID = p.PrijemStanicaID
LEFT JOIN planiranje.OP_Stanice b ON b.ID = p.PredajaStanicaID
WHERE a.Naziv = N'Nema' OR b.Naziv = N'Nema';
GO
PRINT N'OP_017 završena.';
GO
