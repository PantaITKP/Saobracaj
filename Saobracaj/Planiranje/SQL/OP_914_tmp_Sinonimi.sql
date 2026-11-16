/* =====================================================================
   OP_914_tmp_Sinonimi.sql
   Ručno potvrđeni prevodi za agenta CimProvera (planiranje.tmpSinonimi, OP_910_tmp_Kreiranje.sql) – odluke od 14.11.2026:
     - PARTNER „Panšped“            → 1969 PANŠPED DOO
     - STANICA „Pančevo“            → 246 Pančevo Glavna
     - STANICA „Pančevo Glavna“     → 246 Pančevo Glavna
   v2.9.1 (odluke 16.11.2026, zadatak tačka 5.4) – granični prelazi, tip GRANICA (CiljniID = stanice.ID):
     - Kelebia / 0711 / HU               → 328 Subotica gr.
     - Tovarnik / HR                     → 381 Šid gr.
     - Stamora Moravita / RO             → 93 Vršac gr.
     - Dragoman, Kalotina, Kapikule, Svilengrad / BG → 118 Dimitrovgrad gr.
     - Tabanovci / MK                    → 273 Preševo gr.
     Oznaka zemlje (HU, HR, RO, BG, MK) se koristi samo kada prelaz nije naveden (pouzdanost 60).
     Prelazi za ostale zemlje se NE dodaju dok ih korisnik ne potvrdi.
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
-- granični prelazi (posle OP_916_tmp_Izmene.sql, koji dodaje tip GRANICA)
INSERT @s VALUES ('GRANICA', N'KELEBIA',          328, N'odluka 16.11.2026 (Kelebia → Subotica gr.)');
INSERT @s VALUES ('GRANICA', N'0711',             328, N'odluka 16.11.2026 (granični kod 0711 Subotica/Kelebia)');
INSERT @s VALUES ('GRANICA', N'HU',               328, N'odluka 16.11.2026 (HU → Subotica gr.)');
INSERT @s VALUES ('GRANICA', N'TOVARNIK',         381, N'odluka 16.11.2026 (Tovarnik → Šid gr.)');
INSERT @s VALUES ('GRANICA', N'HR',               381, N'odluka 16.11.2026 (HR → Šid gr.)');
INSERT @s VALUES ('GRANICA', N'STAMORA MORAVITA', 93,  N'odluka 16.11.2026 (Stamora Moravita → Vršac gr.)');
INSERT @s VALUES ('GRANICA', N'STAMORA',          93,  N'odluka 16.11.2026 (Stamora Moravita → Vršac gr.)');
INSERT @s VALUES ('GRANICA', N'RO',               93,  N'odluka 16.11.2026 (RO → Vršac gr.)');
INSERT @s VALUES ('GRANICA', N'DRAGOMAN',         118, N'odluka 16.11.2026 (Dragoman → Dimitrovgrad gr.)');
INSERT @s VALUES ('GRANICA', N'KALOTINA',         118, N'odluka 16.11.2026 (Kalotina → Dimitrovgrad gr.)');
INSERT @s VALUES ('GRANICA', N'KAPIKULE',         118, N'odluka 16.11.2026 (Kapikule → Dimitrovgrad gr.)');
INSERT @s VALUES ('GRANICA', N'SVILENGRAD',       118, N'odluka 16.11.2026 (Svilengrad → Dimitrovgrad gr.)');
INSERT @s VALUES ('GRANICA', N'BG',               118, N'odluka 16.11.2026 (BG → Dimitrovgrad gr.)');
INSERT @s VALUES ('GRANICA', N'TABANOVCI',        273, N'odluka 16.11.2026 (Tabanovci → Preševo gr.)');
INSERT @s VALUES ('GRANICA', N'MK',               273, N'odluka 16.11.2026 (MK → Preševo gr.)');

BEGIN TRANSACTION;
UPDATE x SET CiljniID = s.Cilj, Izvor = s.Izvor
FROM planiranje.tmpSinonimi x JOIN @s s ON s.Tip = x.Tip AND s.Tekst = x.TekstNormalizovan;
INSERT planiranje.tmpSinonimi (Tip, TekstNormalizovan, CiljniID, Izvor)
SELECT s.Tip, s.Tekst, s.Cilj, s.Izvor FROM @s s
WHERE NOT EXISTS (SELECT 1 FROM planiranje.tmpSinonimi x WHERE x.Tip = s.Tip AND x.TekstNormalizovan = s.Tekst);
COMMIT TRANSACTION;

SELECT Tip, TekstNormalizovan, CiljniID, Izvor FROM planiranje.tmpSinonimi ORDER BY Tip, TekstNormalizovan;
GO

/* ---------------------------------------------------------------------
   PREDLOG (v2.9.1) – granični prelazi za ostale zemlje, iz postojećih planiranje.OP_Jobs (najčešća granična stanica po
   zemlji prevoznika PreuzimaOd / PredajeOperateru, 16.11.2026). NE KORISTE SE dok ih korisnik ne potvrdi: posle potvrde
   skinuti komentar i ponovo pokrenuti skriptu.
     BA  → 417 Brasina gr.        (ulaz 4, izlaz 3)
     MNE → 1365 Prijepolje gr.    (izlaz 2)
     HR  → 51 Bogojevo gr.        (ulaz 6, izlaz 4 – Erdut/Dalj; HR je već 381 Šid gr. – dodati samo naziv prelaza)
   INSERT @s VALUES ('GRANICA', N'BA', 417, N'predlog 16.11.2026 (OP_Jobs: BA → Brasina gr.)');
   INSERT @s VALUES ('GRANICA', N'ZVORNIK', 417, N'predlog 16.11.2026 (Zvornik → Brasina gr.)');
   INSERT @s VALUES ('GRANICA', N'MNE', 1365, N'predlog 16.11.2026 (OP_Jobs: MNE → Prijepolje gr.)');
   INSERT @s VALUES ('GRANICA', N'BIJELO POLJE', 1365, N'predlog 16.11.2026 (Bijelo Polje → Prijepolje gr.)');
   INSERT @s VALUES ('GRANICA', N'ERDUT', 51, N'predlog 16.11.2026 (Erdut → Bogojevo gr.)');
   INSERT @s VALUES ('GRANICA', N'DALJ', 51, N'predlog 16.11.2026 (Dalj → Bogojevo gr.)');
   --------------------------------------------------------------------- */
