using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class ZadatakRepo
    {
        private static string Txt(DataRow r, string k)
        {
            return r.IsNull(k) ? null : Convert.ToString(r[k]).Trim();
        }

        internal static OsobljeKontekst Kontekst(PlanKontekst plan, DateTime od, DateTime doDatuma)
        {
            var k = new OsobljeKontekst { Plan = plan };
            foreach (string kljuc in new[] { "MAX_RAD_H", "ODMOR_POSLE_SMENE_H" }) plan.Parametri[kljuc] = ParametriRepo.ZaKljuc(kljuc);
            DateTime sireOd = od.AddDays(-14), sireDo = doDatuma.AddDays(14);
            using (var c = Db.Open())
            {
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT o.DeSifra, o.Naziv, o.Telefon, o.Email, o.Masinovodja, o.PregledacKola, o.Manevrista, o.Vozovodja, o.MasinovodjaManevra, o.Aktivan, o.UPlaniranju, o.MaticnaStanicaID, " +
                    "(SELECT p.Auto FROM planiranje.OP_OsobljePlan p WHERE p.DeSifra = o.DeSifra) AS Auto, (SELECT p.SamoDomicilna FROM planiranje.OP_OsobljePlan p WHERE p.DeSifra = o.DeSifra) AS SamoDomicilna, " +
                    "(SELECT TOP 1 t.Naziv FROM planiranje.OP_OsobljeTurnus d JOIN planiranje.OP_Turnus t ON t.ID = d.TurnusID WHERE d.DeSifra = o.DeSifra ORDER BY d.Od DESC) AS Turnus " +
                    "FROM planiranje.OP_vOsoblje o WHERE o.UPlaniranju = 1 ORDER BY o.Naziv").Rows)
                    k.Osobe.Add(new OsobaPlan
                    {
                        DeSifra = Convert.ToInt32(r["DeSifra"]),
                        Naziv = Txt(r, "Naziv"),
                        Telefon = Txt(r, "Telefon"),
                        Email = Txt(r, "Email"),
                        Masinovodja = Convert.ToBoolean(r["Masinovodja"]),
                        Pregledac = Convert.ToBoolean(r["PregledacKola"]),
                        Manevrista = Convert.ToBoolean(r["Manevrista"]),
                        Vozovodja = Convert.ToBoolean(r["Vozovodja"]),
                        MasinovodjaManevra = Convert.ToBoolean(r["MasinovodjaManevra"]),
                        Aktivan = Convert.ToBoolean(r["Aktivan"]),
                        UPlaniranju = Convert.ToBoolean(r["UPlaniranju"]),
                        MaticnaID = PlanRepo.Ceo(r, "MaticnaStanicaID"),
                        Turnus = Txt(r, "Turnus"),
                        Auto = !r.IsNull("Auto") && Convert.ToBoolean(r["Auto"]),
                        SamoDomicilna = !r.IsNull("SamoDomicilna") && Convert.ToBoolean(r["SamoDomicilna"])
                    });
                foreach (DataRow r in Db.Table(c, null, "SELECT DeSifra, PrugaID, VaziDo FROM planiranje.OP_OsobljePruga").Rows)
                    k.Kvalifikacije.Add(new KvalifikacijaOsobe { DeSifra = Convert.ToInt32(r["DeSifra"]), Vrsta = "PRUGA", PrugaID = PlanRepo.Ceo(r, "PrugaID"), VaziDo = PlanRepo.Dat(r, "VaziDo") });
                // Poznavanje serija (OP_029); poznavanje pojedinačnih lokomotiva se više ne koristi.
                foreach (DataRow r in Db.Table(c, null, "SELECT DeSifra, SerijaID, VaziDo FROM planiranje.OP_OsobljeSerija").Rows)
                    k.Kvalifikacije.Add(new KvalifikacijaOsobe { DeSifra = Convert.ToInt32(r["DeSifra"]), Vrsta = "SERIJA", SerijaID = PlanRepo.Ceo(r, "SerijaID"), VaziDo = PlanRepo.Dat(r, "VaziDo") });
                foreach (DataRow r in Db.Table(c, null, "SELECT DeSifra, Datum, Vrsta, Od, Do, LokacijaPosleStanicaID FROM planiranje.OP_Smena WHERE Datum >= @od AND Datum < @do", "@od", sireOd, "@do", sireDo).Rows)
                    k.Smene.Add(new SmenaOsobe { DeSifra = Convert.ToInt32(r["DeSifra"]), Datum = Convert.ToDateTime(r["Datum"]), Vrsta = Txt(r, "Vrsta"), Od = PlanRepo.Dat(r, "Od"), Do = PlanRepo.Dat(r, "Do"), LokacijaPosleID = PlanRepo.Ceo(r, "LokacijaPosleStanicaID") });
                foreach (DataRow r in Db.Table(c, null, "SELECT DeSifra, Vrsta, [Od], [Do] FROM planiranje.OP_vOdsustvo WHERE [Od] < @do AND [Do] >= @od AND [Od] IS NOT NULL AND [Do] IS NOT NULL", "@od", sireOd, "@do", sireDo).Rows)
                    k.Odsustva.Add(new OdsustvoOsobe { DeSifra = Convert.ToInt32(r["DeSifra"]), Vrsta = Txt(r, "Vrsta"), Od = Convert.ToDateTime(r["Od"]), Do = Convert.ToDateTime(r["Do"]) });
                k.Zadaci = Zadaci(c, null, "z.Od < @do AND z.Do > @od", "@od", sireOd, "@do", sireDo);
            }
            return k;
        }

        private static List<ZadatakOsobe> Zadaci(SqlConnection c, SqlTransaction tx, string uslov, params object[] args)
        {
            return Db.Table(c, tx,
                "SELECT z.ID, z.PosaoID, z.TaskID, z.TaskOsobljeID, z.DeSifra, z.Uloga, z.Status, z.StanicaID, z.Od, z.Do, z.RowVer, COALESCE(tk.StanicaDoID, p.PredajaStanicaID) AS PredajaStanicaID, n.BrojNajave, o.Naziv, o.Telefon, o.Email " +
                "FROM planiranje.OP_Zadatak z JOIN planiranje.OP_Posao p ON p.ID = z.PosaoID LEFT JOIN planiranje.OP_Task tk ON tk.ID = z.TaskID LEFT JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID " +
                "LEFT JOIN planiranje.OP_vOsoblje o ON o.DeSifra = z.DeSifra WHERE " + uslov + " ORDER BY z.Od", args)
                .Rows.Cast<DataRow>().Select(r => new ZadatakOsobe
                {
                    ID = Convert.ToInt32(r["ID"]),
                    PosaoID = Convert.ToInt32(r["PosaoID"]),
                    TaskID = PlanRepo.Ceo(r, "TaskID"),
                    TaskOsobljeID = PlanRepo.Ceo(r, "TaskOsobljeID"),
                    DeSifra = Convert.ToInt32(r["DeSifra"]),
                    Uloga = Txt(r, "Uloga"),
                    Status = Txt(r, "Status"),
                    StanicaID = PlanRepo.Ceo(r, "StanicaID"),
                    Od = Convert.ToDateTime(r["Od"]),
                    Do = Convert.ToDateTime(r["Do"]),
                    RowVer = (byte[])r["RowVer"],
                    PredajaStanicaID = PlanRepo.Ceo(r, "PredajaStanicaID"),
                    BrojNajave = Txt(r, "BrojNajave"),
                    Osoba = Txt(r, "Naziv"),
                    Telefon = Txt(r, "Telefon"),
                    Email = Txt(r, "Email")
                }).ToList();
        }

        internal static List<ZadatakOsobe> ZaPosao(int posaoId)
        {
            using (var c = Db.Open())
                return Zadaci(c, null, "z.PosaoID = @p", "@p", posaoId);
        }

        internal static List<ZadatakOsobe> ZaTask(int taskId)
        {
            using (var c = Db.Open())
                return Zadaci(c, null, "z.TaskID = @t", "@t", taskId);
        }

        internal sealed class NoviZadatak
        {
            internal int? TaskOsobljeID;
            internal int DeSifra;
            internal string Naziv;
            internal string Uloga;
            internal int? StanicaID;
            internal DateTime Od;
            internal DateTime Do;
            internal string Napomena;
        }

        // Svi zadaci jednog slanja idu u istoj transakciji, sa ponovnom proverom preklapanja.
        // Zadaci za task (taskId) vezuju se za task i radno mesto; isti „posao“ je tada task, a ne najava.
        internal static void Posalji(int posaoId, IList<NoviZadatak> novi, string napomena, int? taskId = null)
        {
            // Uslovi nad aliasom z: zadaci istog posla (task ili najava bez taska) i zadaci drugih poslova.
            string isti = taskId.HasValue ? "z.TaskID = @p" : "z.PosaoID = @p AND z.TaskID IS NULL";
            string drugiPosao = taskId.HasValue ? "ISNULL(z.TaskID, 0) <> @p" : "(z.PosaoID <> @p OR z.TaskID IS NOT NULL)";
            object kljuc = taskId.HasValue ? taskId.Value : posaoId;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (NoviZadatak z in novi.OrderBy(x => x.DeSifra))
                        Db.Lock(c, tx, "OP_Osoba_" + z.DeSifra);
                    foreach (NoviZadatak z in novi)
                    {
                        // Radno mesto taska ima svoj zadatak (može biti više manevrista); inače jedan zadatak po ulozi.
                        string mesto = taskId.HasValue && z.TaskOsobljeID.HasValue ? " AND z.TaskOsobljeID = @to" : " AND z.Uloga = @u";
                        using (var cmd = Db.Command(c, tx, "DELETE z FROM planiranje.OP_Zadatak z WHERE " + isti + mesto + " AND z.Status=N'PREDLOZEN'", "@p", kljuc, "@u", z.Uloga, "@to", (object)z.TaskOsobljeID))
                            cmd.ExecuteNonQuery();
                        if (Zadaci(c, tx, isti + mesto + " AND z.Status IN (N'POSLAT', N'PRIHVACEN')", "@p", kljuc, "@u", z.Uloga, "@to", (object)z.TaskOsobljeID).Count > 0)
                            throw new InvalidOperationException(z.Naziv + ": za ovo radno mesto zadatak je već poslat.");
                        // Druga zauzetost: zadatak drugog posla, a kod taska i drugo radno mesto istog taska.
                        string drugoMesto = taskId.HasValue && z.TaskOsobljeID.HasValue ? " OR (" + isti + " AND z.TaskOsobljeID IS NOT NULL AND z.TaskOsobljeID <> @to)" : "";
                        ZadatakOsobe drugi = Zadaci(c, tx, "z.DeSifra = @d AND (" + drugiPosao + drugoMesto + ") AND z.Status IN (N'POSLAT', N'PRIHVACEN') AND z.Od < @do AND z.Do > @od",
                            "@d", z.DeSifra, "@p", kljuc, "@to", (object)z.TaskOsobljeID, "@od", z.Od, "@do", z.Do).FirstOrDefault();
                        if (drugi != null)
                            throw new InvalidOperationException(z.Naziv + " već ima zadatak " + (drugi.TaskID.HasValue ? "za task " + drugi.TaskID : "za najavu " + drugi.BrojNajave) + ". Osvežite prikaz.");
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_Zadatak (PosaoID, TaskID, TaskOsobljeID, DeSifra, Uloga, StanicaID, Od, Do, Status, NapomenaZaOsobu, Poslao, Poslato) VALUES (@p, @t, @to, @d, @u, @s, @od, @do, N'POSLAT', @n, @k, SYSDATETIME())",
                            "@p", posaoId, "@t", (object)taskId, "@to", (object)z.TaskOsobljeID, "@d", z.DeSifra, "@u", z.Uloga, "@s", (object)z.StanicaID, "@od", z.Od, "@do", z.Do, "@n", (object)(z.Napomena ?? (z.Uloga == PonudaOsoblja.UlogaMasinovodja ? napomena : null)), "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    }
                    DogadjajRepo.Upisi(c, tx, posaoId, "ZADATAK_POSLAT", (taskId.HasValue ? "Task " + taskId + ": z" : "Z") + "adatak poslat: " + string.Join(", ", novi.Select(z => z.Naziv)) + ".");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Mašinovođa iz prihvaćenog predloga optimizatora; zadatak i dalje šalje mašinski dispečer.
        internal static void Predlozi(int posaoId, int deSifra, int? stanica, DateTime od, DateTime doVreme)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    using (var cmd = Db.Command(c, tx,
                        "DELETE planiranje.OP_Zadatak WHERE PosaoID=@p AND Uloga=@u AND Status=N'PREDLOZEN'; " +
                        "INSERT planiranje.OP_Zadatak (PosaoID, DeSifra, Uloga, StanicaID, Od, Do, Status) VALUES (@p, @d, @u, @s, @od, @do, N'PREDLOZEN')",
                        "@p", posaoId, "@d", deSifra, "@u", PonudaOsoblja.UlogaMasinovodja, "@s", (object)stanica, "@od", od, "@do", doVreme))
                        cmd.ExecuteNonQuery();
                    DogadjajRepo.Upisi(c, tx, posaoId, "ZADATAK_PREDLOZEN", "Optimizator je predložio mašinovođu.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Kad svi poslati zadaci budu prihvaćeni, a mašinovođa je među njima, posao prelazi u fazu 4.
        internal static void Odgovori(ZadatakOsobe z, bool prihvatio, string razlog)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Zadatak SET Status=@s, RazlogOdbijanja=@r, Odgovoreno=SYSDATETIME(), UneoOdgovor=@k WHERE ID=@id AND RowVer=@rv",
                        "@s", prihvatio ? "PRIHVACEN" : "ODBIJEN", "@r", (object)razlog, "@k", PlaniranjeDb.Korisnik, "@id", z.ID, "@rv", z.RowVer);
                    DogadjajRepo.Upisi(c, tx, z.PosaoID, prihvatio ? "ZADATAK_PRIHVACEN" : "ZADATAK_ODBIJEN", z.Osoba + (prihvatio ? " je prihvatio zadatak." : " je odbio zadatak: " + razlog));
                    // Ko je odbio ne ide nigde, pa vozaču prevoz više ne važi.
                    // Zadatak taska se odnosi samo na taj task; zadatak bez taska na posao najave.
                    string isti = z.TaskID.HasValue ? "TaskID = @p" : "PosaoID = @p AND TaskID IS NULL";
                    object kljuc = z.TaskID.HasValue ? z.TaskID.Value : z.PosaoID;
                    if (!prihvatio && PonudaOsoblja.ImaPrevoz(z.Uloga))
                        using (var cmd = Db.Command(c, tx,
                            "UPDATE planiranje.OP_Zadatak SET Status=N'OTKAZAN', Odgovoreno=SYSDATETIME(), UneoOdgovor=@k WHERE " + isti + " AND Uloga=@u AND Status IN (N'PREDLOZEN', N'POSLAT', N'PRIHVACEN')",
                            "@k", PlaniranjeDb.Korisnik, "@p", kljuc, "@u", PonudaOsoblja.UlogaPrevoza(z.Uloga)))
                            if (cmd.ExecuteNonQuery() > 0)
                                DogadjajRepo.Upisi(c, tx, z.PosaoID, "ZADATAK_OTKAZAN", "Prevoz je otkazan jer je " + z.Osoba + " odbio zadatak.");
                    List<ZadatakOsobe> svi = Zadaci(c, tx, "z." + isti.Replace(" AND TaskID", " AND z.TaskID") + " AND z.Status IN (N'POSLAT', N'PRIHVACEN')", "@p", kljuc);
                    // Faza posla se menja samo za zadatke najave; posao sa taskovima ima više taskova i ostaje kako jeste.
                    if (!z.TaskID.HasValue && prihvatio && svi.All(x => x.Status == "PRIHVACEN") && svi.Any(x => x.Uloga == PonudaOsoblja.UlogaMasinovodja))
                    {
                        int promena;
                        using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_Posao SET Faza=4, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND Faza=3", "@k", PlaniranjeDb.Korisnik, "@id", z.PosaoID))
                            promena = cmd.ExecuteNonQuery();
                        if (promena > 0) DogadjajRepo.Upisi(c, tx, z.PosaoID, "FAZA_4", "Svi su prihvatili zadatak; posao čeka prijem i pregled.");
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // SMTP je iz konfiguracije aplikacije (system.net/mailSettings). Bez nje poruka nije poslata, pa se javlja telefonom.
        internal static bool PosaljiMail(string email, string naslov, string tekst)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                using (var smtp = new SmtpClient())
                {
                    if (string.IsNullOrEmpty(smtp.Host)) return false;
                    using (var poruka = new MailMessage { Subject = naslov, Body = tekst })
                    {
                        poruka.To.Add(email.Trim());
                        if (poruka.From == null) return false;
                        smtp.Send(poruka);
                    }
                }
                return true;
            }
            catch (Exception ex) when (ex is SmtpException || ex is FormatException || ex is InvalidOperationException)
            {
                return false;
            }
        }

        // Dvoklik u kalendaru: ručna smena se više ne prepisuje iz turnusa (pravilo 8.8).
        internal static void SacuvajSmenuRucno(int deSifra, DateTime datum, string vrsta, DateTime? od, DateTime? doVreme)
        {
            OsobljeRepo.ObezbediPlan(deSifra);
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "IF EXISTS (SELECT 1 FROM planiranje.OP_Smena WHERE DeSifra=@d AND Datum=@dan) " +
                "UPDATE planiranje.OP_Smena SET Vrsta=@v, Od=@od, Do=@do, Izvor=N'RUCNO', Izmenio=@k WHERE DeSifra=@d AND Datum=@dan " +
                "ELSE INSERT planiranje.OP_Smena (DeSifra, Datum, Vrsta, Od, Do, Izvor, Izmenio) VALUES (@d, @dan, @v, @od, @do, N'RUCNO', @k)",
                "@d", deSifra, "@dan", datum.Date, "@v", vrsta, "@od", (object)od, "@do", (object)doVreme, "@k", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }
    }
}
