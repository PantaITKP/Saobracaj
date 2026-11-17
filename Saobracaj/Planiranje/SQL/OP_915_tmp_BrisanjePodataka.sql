/* =====================================================================
   OP_915_tmp_BrisanjePodataka.sql
   Briše SAMO PODATKE koje je agent CimProvera upisao u planiranje.tmp* (DELETE); tabele ostaju.
   Ručno potvrđeni sinonimi (planiranje.tmpSinonimi, OP_914_tmp_Sinonimi.sql) ostaju.
   - Ne dira nijednu drugu tabelu. Može da se pokrene više puta.
       sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_915_tmp_BrisanjePodataka.sql
   ===================================================================== */
SET NOCOUNT ON; SET XACT_ABORT ON;
-- baza je na nivou kompatibilnosti 80: filtrirani indeksi traže ove opcije (u posebnom batch-u, pre DELETE)
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
GO
BEGIN TRANSACTION;
IF OBJECT_ID(N'planiranje.tmpJobsDogadjaj', N'U') IS NOT NULL DELETE FROM planiranje.tmpJobsDogadjaj;   -- v3.0 (OP_918)
DELETE FROM planiranje.tmpJobsIstorija;
DELETE FROM planiranje.tmpJobsPolje;
DELETE FROM planiranje.tmpTeretnicaStavke;
UPDATE planiranje.tmpDokument SET JobID = NULL, TeretnicaID = NULL;
DELETE FROM planiranje.tmpTeretnica;
DELETE FROM planiranje.tmpJobs;
DELETE FROM planiranje.tmpDokument;
COMMIT TRANSACTION;
SELECT (SELECT COUNT(*) FROM planiranje.tmpJobs) AS poslova, (SELECT COUNT(*) FROM planiranje.tmpTeretnica) AS teretnica,
       (SELECT COUNT(*) FROM planiranje.tmpDokument) AS dokumenata, (SELECT COUNT(*) FROM planiranje.tmpSinonimi) AS sinonima;
GO
