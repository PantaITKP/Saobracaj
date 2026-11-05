/* =====================================================================
   OP_014_NajavaStanice.sql
   Merodavne stanice najave za planiranje:
     Otpravna: stanica iz najave ako je u Srbiji, inače OtpravnaGr;
     Uputna:   stanica iz najave ako je u Srbiji, inače Uputnagr.
   Ranije je granica imala prednost čim je upisana.

   - Stanica je u Srbiji ako je tako vodi OP_Stanice (preko IzvorniID, i za
     ugašeni duplikat preko glavne stanice) ili dbo.stanice.
   - Granica „Nema“ (dbo.stanice, ID 424 na radnoj bazi) znači da granica nije upisana.
   - Strana stanica bez upisane granice ostaje kakva jeste.
   - Menja poglede OP_vNajava i OP_vPosao i dodaje kolone PlanOtpravna i PlanUputna.
   - Posao u fazi 1 dobija nove stanice prijema i predaje. Ostali poslovi se ne menjaju.
   - U dbo se ništa ne menja. Može da se pokrene više puta.
   - Pokreće se ručno, posle OP_010.
   ===================================================================== */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
ALTER VIEW planiranje.OP_vNajava
AS
SELECT  n.ID                    AS NajavaID,
        n.BrojNajave,
        n.Otpravna              AS OtpravnaID,
        n.Uputna                AS UputnaID,
        n.OtpravnaGr            AS OtpravnaGrID,
        n.Uputnagr              AS UputnaGrID,
        CASE WHEN m1.Drzava = N'SRB' OR s1.Drzava IN (N'SRB', N'RS') OR NULLIF(n.OtpravnaGr, 0) IS NULL OR RTRIM(g1.Opis) = N'Nema' THEN n.Otpravna ELSE n.OtpravnaGr END AS PlanOtpravnaID,
        CASE WHEN m2.Drzava = N'SRB' OR s2.Drzava IN (N'SRB', N'RS') OR NULLIF(n.Uputnagr, 0) IS NULL OR RTRIM(g2.Opis) = N'Nema' THEN n.Uputna ELSE n.Uputnagr END AS PlanUputnaID,
        CASE WHEN m1.Drzava = N'SRB' OR s1.Drzava IN (N'SRB', N'RS') OR NULLIF(n.OtpravnaGr, 0) IS NULL OR RTRIM(g1.Opis) = N'Nema' THEN RTRIM(s1.Opis) ELSE RTRIM(g1.Opis) END AS PlanOtpravna,
        CASE WHEN m2.Drzava = N'SRB' OR s2.Drzava IN (N'SRB', N'RS') OR NULLIF(n.Uputnagr, 0) IS NULL OR RTRIM(g2.Opis) = N'Nema' THEN RTRIM(s2.Opis) ELSE RTRIM(g2.Opis) END AS PlanUputna,
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
LEFT JOIN dbo.stanice g2 ON g2.ID = n.Uputnagr
LEFT JOIN planiranje.OP_Stanice o1 ON o1.IzvorniID = n.Otpravna
LEFT JOIN planiranje.OP_Stanice m1 ON m1.ID = COALESCE(o1.DuplikatOd, o1.ID)
LEFT JOIN planiranje.OP_Stanice o2 ON o2.IzvorniID = n.Uputna
LEFT JOIN planiranje.OP_Stanice m2 ON m2.ID = COALESCE(o2.DuplikatOd, o2.ID);
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
        n.BrutoT AS NajavaBrutoT, n.DuzinaM AS NajavaDuzinaM, n.BrojKola AS NajavaBrojKola,
        n.SerijaVagona AS NajavaSerijaVagona, n.Tovareno AS NajavaTovareno, n.RID AS NajavaRID,
        n.PredvidjenoPrimanje, n.PredvidjenaPredaja, n.StvarnoPrimanje, n.StvarnaPredaja,
        p.RowVer
FROM    planiranje.OP_Posao p
LEFT JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID;
GO
UPDATE p
SET    PrijemStanicaID = (SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanOtpravnaID ORDER BY s.ID),
       PredajaStanicaID = (SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanUputnaID ORDER BY s.ID)
FROM   planiranje.OP_Posao p
JOIN   planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID
WHERE  p.Status = N'AKTIVAN' AND p.Faza = 1;
PRINT N'Poslova u fazi 1 usklađeno: ' + CAST(@@ROWCOUNT AS nvarchar(10));
SELECT p.ID AS Posao_posle_faze_1_nije_menjan, p.Faza, p.BrojNajave, a.Naziv AS Prijem, n.PlanOtpravna AS TrebaPrijem, b.Naziv AS Predaja, n.PlanUputna AS TrebaPredaja
FROM   planiranje.OP_vPosao p
JOIN   planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID
LEFT JOIN planiranje.OP_Stanice a ON a.ID = p.PrijemStanicaID
LEFT JOIN planiranje.OP_Stanice b ON b.ID = p.PredajaStanicaID
WHERE  p.Status = N'AKTIVAN' AND p.Faza > 1
  AND  (ISNULL(p.PrijemStanicaID, 0) <> ISNULL((SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanOtpravnaID ORDER BY s.ID), 0)
    OR  ISNULL(p.PredajaStanicaID, 0) <> ISNULL((SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = n.PlanUputnaID ORDER BY s.ID), 0));
GO
PRINT N'OP_014 završena.';
GO
