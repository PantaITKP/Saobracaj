using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    // Privremene tabele agenta CimProvera (planiranje.tmp*, OP_910_tmp_Kreiranje.sql): samo čitanje.
    // Nazivi dolaze iz šifarnika (dbo.Partnerji po PaSifra, dbo.stanice po ID, v2.9.1: NHM iz planiranje.tmpNHM); kada agent nije pronašao
    // šifru (ID je NULL), umesto naziva se vraća tekst iz dokumenta iz porekla polja (tmpJobsPolje).
    internal static class TmpCimRepo
    {
        // Kolona sa nazivom → kolona sa ID-em. Forma po ovome boji naziv kada je ID prazan.
        internal static readonly string[,] NaziviIDevi =
        {
            { "Platilac", "PlatilacID" }, { "Preuzima od", "PreuzimaOdID" }, { "Predaje operateru", "PredajeOperateruID" }, { "Predaje partneru", "PredajePartneruID" },
            { "Otpravna", "OtpravnaID" }, { "Uputna", "UputnaID" }, { "Stanica 1", "Stanica1ID" }, { "Stanica 3", "Stanica3ID" },
            { "NHM 1", "NHM1" }, { "NHM 2", "NHM2" }
        };

        private static string Poreklo(string alias, string kolona)
        {
            return "LEFT JOIN planiranje.tmpJobsPolje " + alias + " ON " + alias + ".JobID = j.ID AND " + alias + ".TeretnicaID IS NULL AND " + alias + ".Kolona = N'" + kolona + "' ";
        }

        private static string Naziv(string sifarnik, string poreklo)
        {
            return "ISNULL(RTRIM(" + sifarnik + "), " + poreklo + ".TekstIzDokumenta)";
        }

        // v2.9.1: NHM kao „šifra – naziv“ iz planiranje.tmpNHM („2520 10 00 – Gips; anhidrid gipsa“)
        private static string Nhm(string alias, string poreklo)
        {
            return "ISNULL(" + alias + ".Kod4 + N' ' + SUBSTRING(" + alias + ".Kod8, 5, 2) + N' ' + SUBSTRING(" + alias + ".Kod8, 7, 2) + N' – ' + ISNULL(" + alias + ".NazivLat, N''), " + poreklo + ".TekstIzDokumenta)";
        }

        internal static DataTable Poslovi(DateTime od, DateTime doDana, string tekst, bool samoPrazni)
        {
            string sql =
                "SELECT j.ID, " +
                Naziv("pl.PaNaziv", "fpl") + " AS [Platilac], j.PlatilacID AS [PlatilacID], " +
                Naziv("pr.PaNaziv", "fpr") + " AS [Preuzima od], j.PreuzimaOdID AS [PreuzimaOdID], " +
                Naziv("pd.PaNaziv", "fpd") + " AS [Predaje operateru], j.PredajeOperateruID AS [PredajeOperateruID], " +
                Naziv("pp.PaNaziv", "fpp") + " AS [Predaje partneru], j.PredajePartneruID AS [PredajePartneruID], " +
                Naziv("so.Opis", "fso") + " AS [Otpravna], j.OtpravnaID AS [OtpravnaID], " +
                Naziv("su.Opis", "fsu") + " AS [Uputna], j.UputnaID AS [UputnaID], " +
                Naziv("s1.Opis", "fs1") + " AS [Stanica 1], j.Stanica1ID AS [Stanica1ID], " +
                "RTRIM(s2.Opis) AS [Stanica 2 (u.m.)], j.Stanica2ID AS [Stanica2ID], " +
                Naziv("s3.Opis", "fs3") + " AS [Stanica 3], j.Stanica3ID AS [Stanica3ID], " +
                Nhm("n1", "fn1") + " AS [NHM 1], j.NHM1 AS [NHM1], " +
                "j.Bruto1 AS [Bruto 1 (t)], j.Neto1 AS [Neto 1 (t)], j.Tara1 AS [Tara 1 (t)], j.BrojKola1 AS [Broj kola 1], j.Duzina1 AS [Dužina 1 (m)], j.SerijaKola1 AS [Serija 1], " +
                "j.RIDBroj1 AS [RID broj 1], j.OMBroj1 AS [OM broj 1], " +
                Nhm("n2", "fn2") + " AS [NHM 2], j.NHM2 AS [NHM2], " +
                "j.Bruto2 AS [Bruto 2 (t)], j.Neto2 AS [Neto 2 (t)], j.Tara2 AS [Tara 2 (t)], j.BrojKola2 AS [Broj kola 2], " +
                "j.Referenca AS [Referenca (r8)], j.BrojPonude AS [Broj ponude (r14)], j.Verzija, " +
                "j.KPTovarniList AS [KP tovarni list], j.ImaGreske AS [Ima greške], j.Kreirano, j.Izmenjeno, " +
                "j.KljucPosiljke AS [Ključ pošiljke] " +
                "FROM planiranje.tmpJobs j " +
                "LEFT JOIN dbo.Partnerji pl ON pl.PaSifra = j.PlatilacID " +
                "LEFT JOIN dbo.Partnerji pr ON pr.PaSifra = j.PreuzimaOdID " +
                "LEFT JOIN dbo.Partnerji pd ON pd.PaSifra = j.PredajeOperateruID " +
                "LEFT JOIN dbo.Partnerji pp ON pp.PaSifra = j.PredajePartneruID " +
                "LEFT JOIN dbo.stanice so ON so.ID = j.OtpravnaID " +
                "LEFT JOIN dbo.stanice su ON su.ID = j.UputnaID " +
                "LEFT JOIN dbo.stanice s1 ON s1.ID = j.Stanica1ID " +
                "LEFT JOIN dbo.stanice s2 ON s2.ID = j.Stanica2ID " +
                "LEFT JOIN dbo.stanice s3 ON s3.ID = j.Stanica3ID " +
                "LEFT JOIN planiranje.tmpNHM n1 ON n1.ID = j.NHM1 " +
                "LEFT JOIN planiranje.tmpNHM n2 ON n2.ID = j.NHM2 " +
                Poreklo("fpl", "PlatilacID") + Poreklo("fpr", "PreuzimaOdID") + Poreklo("fpd", "PredajeOperateruID") + Poreklo("fpp", "PredajePartneruID") +
                Poreklo("fso", "OtpravnaID") + Poreklo("fsu", "UputnaID") + Poreklo("fs1", "Stanica1ID") + Poreklo("fs3", "Stanica3ID") +
                Poreklo("fn1", "NHM1") + Poreklo("fn2", "NHM2") +
                "WHERE j.Kreirano >= @od AND j.Kreirano < @do " +
                "AND (@t IS NULL OR j.Referenca LIKE @t OR pl.PaNaziv LIKE @t OR fpl.TekstIzDokumenta LIKE @t " +
                "  OR so.Opis LIKE @t OR su.Opis LIKE @t OR s1.Opis LIKE @t OR s3.Opis LIKE @t) " +
                "AND (@prazni = 0 OR j.PlatilacID IS NULL OR j.PreuzimaOdID IS NULL OR j.PredajeOperateruID IS NULL " +
                "  OR j.OtpravnaID IS NULL OR j.UputnaID IS NULL OR j.Stanica1ID IS NULL OR j.Stanica3ID IS NULL) " +
                "ORDER BY j.Kreirano DESC, j.ID DESC";
            string t = string.IsNullOrWhiteSpace(tekst) ? null : "%" + tekst.Trim() + "%";
            using (var c = Db.Open())
                return Db.Table(c, null, sql, "@od", od.Date, "@do", doDana.Date.AddDays(1), "@t", t, "@prazni", samoPrazni ? 1 : 0);
        }

        internal static string Napomena(int jobId)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT Napomena FROM planiranje.tmpJobs WHERE ID = @id", "@id", jobId);
                return t.Rows.Count == 0 || t.Rows[0].IsNull(0) ? "" : Convert.ToString(t.Rows[0][0]);
            }
        }

        internal static DataTable Dokumenti(int jobId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT d.ID, d.ImeFajla AS [Fajl], d.TipDokumenta AS [Tip], d.Strane, d.Naslov AS [Naslov maila], d.Posiljalac AS [Pošiljalac], " +
                    "d.Primljeno, d.Sanduce AS [Sanduče], d.UID, d.BrojPrijema AS [Broj prijema], d.Obradjeno AS [Obrađeno] " +
                    "FROM planiranje.tmpDokument d WHERE d.JobID = @id " +
                    "OR d.TeretnicaID IN (SELECT t.ID FROM planiranje.tmpTeretnica t WHERE t.JobID = @id) " +
                    "ORDER BY d.Primljeno DESC, d.ID DESC", "@id", jobId);
        }

        internal static DataTable PorekloPolja(int jobId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT p.Kolona, p.Vrednost, p.Rubrika, p.TekstIzDokumenta AS [Tekst iz dokumenta], p.Pouzdanost, p.Kandidati, " +
                    "d.ImeFajla AS [Dokument], p.RucnoIzmenjeno AS [Ručno izmenjeno], p.Izmenjeno " +
                    "FROM planiranje.tmpJobsPolje p LEFT JOIN planiranje.tmpDokument d ON d.ID = p.DokumentID " +
                    "WHERE p.JobID = @id AND p.TeretnicaID IS NULL ORDER BY p.Kolona", "@id", jobId);
        }

        internal static DataTable Istorija(int jobId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT i.Vreme, i.Kolona, i.StaraVrednost AS [Stara vrednost], i.NovaVrednost AS [Nova vrednost], i.Razlog, i.Napomena, " +
                    "i.TeretnicaID AS [Teretnica], d.ImeFajla AS [Dokument] " +
                    "FROM planiranje.tmpJobsIstorija i LEFT JOIN planiranje.tmpDokument d ON d.ID = i.DokumentID " +
                    "WHERE i.JobID = @id OR i.TeretnicaID IN (SELECT t.ID FROM planiranje.tmpTeretnica t WHERE t.JobID = @id) " +
                    "ORDER BY i.Vreme DESC, i.ID DESC", "@id", jobId);
        }

        internal static int BrojTeretnica(int jobId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, "SELECT COUNT(*) FROM planiranje.tmpTeretnica WHERE JobID = @id", "@id", jobId))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Teretnice posla (jobId) ili, bez posla, sve teretnice poslednjih 30 dana – i one koje još nisu povezane sa poslom.
        internal static DataTable Teretnice(int? jobId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT t.ID, N'Teretnica ' + CAST(t.ID AS nvarchar(10)) + ISNULL(N' · ' + t.BrojTeretnice, N'') + N' · v' + CAST(t.Verzija AS nvarchar(10)) " +
                    "+ N' · ' + CONVERT(nvarchar(16), t.Kreirano, 104) + N' ' + CONVERT(nvarchar(5), t.Kreirano, 108) " +
                    "+ CASE WHEN t.JobID IS NULL THEN N' · bez posla' ELSE N' · posao ' + CAST(t.JobID AS nvarchar(10)) END " +
                    "+ ISNULL(N' · grupa ' + CAST(t.Grupa AS nvarchar(3)), N'') AS Naziv " +
                    "FROM planiranje.tmpTeretnica t " +
                    "WHERE (@job IS NULL AND t.Kreirano >= DATEADD(day, -30, GETDATE())) OR t.JobID = @job " +
                    "ORDER BY t.Kreirano DESC, t.ID DESC", "@job", (object)jobId);
        }

        internal static DataTable ZaglavljeTeretnice(int id)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT t.ID, t.JobID, t.BrojTeretnice, " +
                    "ISNULL(CAST(t.StanicaOd AS nvarchar(10)) + N' – ' + RTRIM(so.Opis), CAST(t.StanicaOd AS nvarchar(10))) AS StanicaOd, " +
                    "ISNULL(CAST(t.StanicaDo AS nvarchar(10)) + N' – ' + RTRIM(sd.Opis), CAST(t.StanicaDo AS nvarchar(10))) AS StanicaDo, " +
                    "CASE t.Prijemna WHEN 1 THEN N'da' WHEN 0 THEN N'ne' ELSE N'' END AS Prijemna, t.Korisnik, t.Napomena, " +
                    "ISNULL(d.ImeFajla, N'') + ISNULL(N' (' + d.TipDokumenta + N')', N'') AS Dokument, t.Verzija, " +
                    "CASE WHEN t.JobID IS NULL THEN N'nije povezana sa poslom' ELSE CAST(t.JobID AS nvarchar(10)) + ISNULL(N' – ' + j.Referenca, N'') END AS Posao, " +
                    "t.Referenca, t.Grupa, t.Kreirano, t.Izmenjeno " +
                    "FROM planiranje.tmpTeretnica t " +
                    "LEFT JOIN dbo.stanice so ON so.ID = t.StanicaOd " +
                    "LEFT JOIN dbo.stanice sd ON sd.ID = t.StanicaDo " +
                    "LEFT JOIN planiranje.tmpDokument d ON d.ID = t.DokumentID " +
                    "LEFT JOIN planiranje.tmpJobs j ON j.ID = t.JobID " +
                    "WHERE t.ID = @id", "@id", id);
        }

        internal static DataTable Stavke(int teretnicaId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT s.RB, s.BrojKola AS [Broj kola], s.Serija, s.BrojOsovina AS [Broj osovina], s.Duzina AS [Dužina (m)], " +
                    "s.Tara AS [Tara (t)], s.Neto AS [Neto (t)], s.VRNP AS [NHM (VRNP)], " +
                    "RTRIM(so.Opis) AS [Otpravna], s.Otpravna AS [OtpravnaID], RTRIM(su.Opis) AS [Uputna], s.Uputna AS [UputnaID], " +
                    "RTRIM(si.Opis) AS [Izvozna], s.Izvozna AS [IzvoznaID], RTRIM(sv.Opis) AS [Uvozna], s.Uvozna AS [UvoznaID], " +
                    "s.RID, s.Dokument AS [Dokument (br. otpravljanja)], s.Primedba, CASE WHEN s.Otkacena = 1 THEN N'da' ELSE N'' END AS [Otkačena], s.ID " +
                    "FROM planiranje.tmpTeretnicaStavke s " +
                    "LEFT JOIN dbo.stanice so ON so.ID = s.Otpravna " +
                    "LEFT JOIN dbo.stanice su ON su.ID = s.Uputna " +
                    "LEFT JOIN dbo.stanice si ON si.ID = s.Izvozna " +
                    "LEFT JOIN dbo.stanice sv ON sv.ID = s.Uvozna " +
                    "WHERE s.BrojTeretnice = @id ORDER BY s.RB, s.ID", "@id", teretnicaId);
        }
    }
}
