/* =====================================================================
   OP_914_tmp_Sinonimi.sql
   Ručno potvrđeni prevodi za agenta CimProvera (planiranje.tmpSinonimi, OP_910_tmp_Kreiranje.sql) – odluke od 14.11.2026:
     - PARTNER „Panšped“            → 1969 PANŠPED DOO
     - STANICA „Pančevo“            → 246 Pančevo Glavna
     - STANICA „Pančevo Glavna“     → 246 Pančevo Glavna
   TekstNormalizovan je kao u agentu (velika slova, bez kvačica i interpunkcije; partner bez pravnog oblika i grada).
   - Menja samo planiranje.tmpSinonimi; može da se pokrene više puta (postojeći red se ažurira).
   - Briše se sa tmp tabelama (OP_911_tmp_Brisanje.sql).
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
GO
DECLARE @s TABLE (Tip varchar(20), Tekst nvarchar(200), Cilj int, Izvor nvarchar(100));
INSERT @s VALUES ('PARTNER', N'PANSPED',        1969, N'odluka 14.11.2026 (Panšped → PANŠPED DOO)');
INSERT @s VALUES ('STANICA', N'PANCEVO',        246,  N'odluka 14.11.2026 (Pančevo → Pančevo Glavna)');
INSERT @s VALUES ('STANICA', N'PANCEVO GLAVNA', 246,  N'odluka 14.11.2026 (Pančevo Glavna)');

BEGIN TRANSACTION;
UPDATE x SET CiljniID = s.Cilj, Izvor = s.Izvor
FROM planiranje.tmpSinonimi x JOIN @s s ON s.Tip = x.Tip AND s.Tekst = x.TekstNormalizovan;
INSERT planiranje.tmpSinonimi (Tip, TekstNormalizovan, CiljniID, Izvor)
SELECT s.Tip, s.Tekst, s.Cilj, s.Izvor FROM @s s
WHERE NOT EXISTS (SELECT 1 FROM planiranje.tmpSinonimi x WHERE x.Tip = s.Tip AND x.TekstNormalizovan = s.Tekst);
COMMIT TRANSACTION;

SELECT Tip, TekstNormalizovan, CiljniID, Izvor FROM planiranje.tmpSinonimi ORDER BY Tip, TekstNormalizovan;
GO
