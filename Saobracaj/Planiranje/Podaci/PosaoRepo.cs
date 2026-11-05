using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class PosaoRepo
    {
        internal static DataTable ZaPeriod(DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT * FROM planiranje.OP_vPosao " +
                    "WHERE PlaniraniPrijem >= @od AND PlaniraniPrijem < @do AND Status <> N'OTKAZAN' ORDER BY PlaniraniPrijem",
                    "@od", od, "@do", doDatuma);
        }

        internal static DataTable UFazi(int faza)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT * FROM planiranje.OP_vPosao WHERE Faza = @f AND Status <> N'OTKAZAN' ORDER BY PlaniraniPrijem",
                    "@f", faza);
        }

        internal static DataRow Jedan(int id)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT * FROM planiranje.OP_vPosao WHERE ID = @id", "@id", id);
                return t.Rows.Count == 0 ? null : t.Rows[0];
            }
        }

        // Pravilo 8.1: posao za svaku najavu perioda koja ga još nema.
        internal static int UzmiNajave(DateTime od, DateTime doDatuma)
        {
            int novih = 0;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_UzmiNajave");
                    DataTable najave = Db.Table(c, tx,
                        "SELECT n.NajavaID, n.BrojNajave, n.PlanOtpravnaID, n.PlanUputnaID, n.PredvidjenoPrimanje FROM planiranje.OP_vNajava n " +
                        "WHERE n.PredvidjenoPrimanje >= @od AND n.PredvidjenoPrimanje < @do AND ISNULL(n.StatusNajave, 0) <> 7 AND n.StvarnaPredaja IS NULL " +
                        "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Posao p WHERE p.NajavaID = n.NajavaID AND p.Status <> N'OTKAZAN') ORDER BY n.PredvidjenoPrimanje",
                        "@od", od, "@do", doDatuma);
                    foreach (DataRow n in najave.Rows)
                    {
                        int posao;
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_Posao (NajavaID, PrijemStanicaID, PredajaStanicaID, PlaniraniPrijem, Faza, Kreirao) " +
                            "SELECT @n, (SELECT TOP 1 COALESCE(DuplikatOd, ID) FROM planiranje.OP_Stanice WHERE IzvorniID = @o ORDER BY ID), (SELECT TOP 1 COALESCE(DuplikatOd, ID) FROM planiranje.OP_Stanice WHERE IzvorniID = @u ORDER BY ID), @d, 1, @k; " +
                            "SELECT CAST(SCOPE_IDENTITY() AS int);",
                            "@n", n["NajavaID"], "@o", n["PlanOtpravnaID"], "@u", n["PlanUputnaID"], "@d", n["PredvidjenoPrimanje"], "@k", PlaniranjeDb.Korisnik))
                            posao = Convert.ToInt32(cmd.ExecuteScalar());
                        DogadjajRepo.Upisi(c, tx, posao, "NAJAVA", "Najava " + Convert.ToString(n["BrojNajave"]).Trim() + " je ušla u plan.");
                        novih++;
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            return novih;
        }

        private static void UpisiRutu(SqlConnection c, SqlTransaction tx, int posaoId, List<DeonicaRute> ruta)
        {
            using (var cmd = Db.Command(c, tx, "DELETE planiranje.OP_PosaoRuta WHERE PosaoID = @p", "@p", posaoId))
                cmd.ExecuteNonQuery();
            foreach (DeonicaRute d in ruta)
                using (var cmd = Db.Command(c, tx,
                    "INSERT planiranje.OP_PosaoRuta (PosaoID, RB, StanicaOdID, StanicaDoID, PrugaID, DuzinaKm, TrasaID, Stanje, Napomena) VALUES (@p, @rb, @o, @d, @pr, @km, @t, @s, @n)",
                    "@p", posaoId, "@rb", d.RB, "@o", d.OdID, "@d", d.DoID, "@pr", (object)d.PrugaID, "@km", d.Km, "@t", (object)d.TrasaID, "@s", d.Stanje, "@n", (object)d.Napomena))
                    cmd.ExecuteNonQuery();
        }

        // Ruta i vremena se upisuju posle „Uzmi najave“, da bi kalendari i tok posla imali termine.
        internal static void SacuvajRacun(RezultatPosla r)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    UpisiRutu(c, tx, r.Posao.ID, r.Ruta);
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Posao SET Faza = CASE WHEN Faza < 2 THEN 2 ELSE Faza END, PlaniraniPolazak=@po, PlaniraniDolazak=@dl, PlaniranaPredaja=@pr, TrasaID=@t, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                        "@po", r.Polazak, "@dl", r.Dolazak, "@pr", r.Predaja, "@t", (object)r.TrasaID, "@k", PlaniranjeDb.Korisnik, "@id", r.Posao.ID, "@rv", r.Posao.RowVer);
                    DogadjajRepo.Upisi(c, tx, r.Posao.ID, "RUTA_IZRACUNATA", "Ruta: " + r.Ruta.Count + " deonica, " + r.Km.ToString("0.#") + " km.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Obrazac 5.2: zaključavanje lokomotiva, ponovna provera unutar transakcije, upis i događaj.
        internal static void PotvrdiLokomotive(RezultatPosla r, KandidatLokomotiva izbor, int potrebno)
        {
            PosaoPlan p = r.Posao;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (LokomotivaOP l in izbor.Lokomotive.OrderBy(x => x.ID))
                        Db.Lock(c, tx, "OP_Lok_" + l.ID);
                    foreach (LokomotivaOP l in izbor.Lokomotive)
                    {
                        ZauzetostLok prepreka = PredlogLokomotiva.Prepreka(PlanRepo.Kalendar(c, tx, p.Prijem, r.Predaja, l.ID), l.ID, p, p.Prijem, r.Predaja);
                        if (prepreka != null)
                            throw new InvalidOperationException("Lokomotiva " + l.Broj + " " + PredlogLokomotiva.OpisPrepreke(prepreka) + " Izbor nije potvrđen.");
                    }
                    using (var cmd = Db.Command(c, tx, "DELETE planiranje.OP_PosaoLokomotiva WHERE PosaoID = @p", "@p", p.ID))
                        cmd.ExecuteNonQuery();
                    byte rb = 1;
                    foreach (LokomotivaOP l in izbor.Lokomotive)
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_PosaoLokomotiva (PosaoID, LokomotivaID, RB, Vucna, Od, Do, PraznaVoznjaMin, Potvrdio, Potvrdjeno) VALUES (@p, @l, @rb, 1, @od, @do, @pv, @k, SYSDATETIME())",
                            "@p", p.ID, "@l", l.ID, "@rb", rb++, "@od", p.Prijem, "@do", r.Predaja, "@pv", izbor.PraznaVoznjaMin, "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    UpisiRutu(c, tx, p.ID, r.Ruta);
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Posao SET Faza = CASE WHEN Faza < 3 THEN 3 ELSE Faza END, PlaniraniPolazak=@po, PlaniraniDolazak=@dl, PlaniranaPredaja=@pr, TrasaID=@t, " +
                        "PotrebnoLokomotiva=@n, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                        "@po", r.Polazak, "@dl", r.Dolazak, "@pr", r.Predaja, "@t", (object)r.TrasaID, "@n", potrebno, "@k", PlaniranjeDb.Korisnik, "@id", p.ID, "@rv", p.RowVer);
                    DogadjajRepo.Upisi(c, tx, p.ID, "LOKOMOTIVE_POTVRDJENE", "Lokomotive: " + izbor.Oznaka + ".", "{\"lokomotive\":[" + string.Join(",", izbor.Lokomotive.Select(l => l.ID)) + "]}");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static void ZatraziAdHoc(PosaoPlan p, DeonicaRute d, string pocetnaNaziv, string krajnjaNaziv)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    TraseRepo.ZatraziAdHoc(c, tx, p.NajavaID, pocetnaNaziv, krajnjaNaziv, d.OdID, d.DoID, "Zahtev iz posla za najavu " + p.BrojNajave);
                    DogadjajRepo.Upisi(c, tx, p.ID, "ZAHTEV_ADHOC", "Zatražena ad hoc trasa " + pocetnaNaziv + " – " + krajnjaNaziv + ".");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Snimak plana: BrojVerzije = max + 1, sa stavkama za sve poslove perioda.
        internal static int SacuvajVerziju(string vrsta, DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_PlanVerzija");
                    int broj, verzija;
                    using (var cmd = Db.Command(c, tx, "SELECT ISNULL(MAX(BrojVerzije), 0) + 1 FROM planiranje.OP_PlanVerzija WHERE Vrsta=@v AND PeriodOd=@od", "@v", vrsta, "@od", od.Date))
                        broj = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_PlanVerzija (Vrsta, PeriodOd, PeriodDo, BrojVerzije, Kreirao) VALUES (@v, @od, @do, @b, @k); SELECT CAST(SCOPE_IDENTITY() AS int);",
                        "@v", vrsta, "@od", od.Date, "@do", doDatuma.Date.AddDays(-1), "@b", broj, "@k", PlaniranjeDb.Korisnik))
                        verzija = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_PlanVerzijaStavka (PlanVerzijaID, PosaoID, LokomotivaID, RB, Od, Do) " +
                        "SELECT @ver, p.ID, l.LokomotivaID, l.RB, ISNULL(l.Od, p.PlaniraniPrijem), ISNULL(l.Do, p.PlaniranaPredaja) " +
                        "FROM planiranje.OP_Posao p LEFT JOIN planiranje.OP_PosaoLokomotiva l ON l.PosaoID = p.ID " +
                        "WHERE p.PlaniraniPrijem >= @od AND p.PlaniraniPrijem < @do AND p.Status <> N'OTKAZAN'",
                        "@ver", verzija, "@od", od, "@do", doDatuma))
                        cmd.ExecuteNonQuery();
                    tx.Commit();
                    return broj;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static string OznakaVerzije(string vrsta, DateTime od)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT TOP 1 BrojVerzije, Status FROM planiranje.OP_PlanVerzija WHERE Vrsta=@v AND PeriodOd=@od ORDER BY BrojVerzije DESC", "@v", vrsta, "@od", od.Date);
                if (t.Rows.Count == 0) return "Bez sačuvane verzije";
                return "Verzija " + t.Rows[0]["BrojVerzije"] + " · " + Convert.ToString(t.Rows[0]["Status"]).ToLowerInvariant();
            }
        }
    }
}
