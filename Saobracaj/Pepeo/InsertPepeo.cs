using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Saobracaj.Pepeo
{
    public class InsertPepeo
    {
        private readonly string connection =
            ConfigurationManager.ConnectionStrings["Saobracaj.Properties.Settings.Perftech_BeogradConnectionString"].ConnectionString;

        private const string SqlSessionSettings =
            "SET ANSI_NULLS ON;" +
            "SET ANSI_PADDING ON;" +
            "SET ANSI_WARNINGS ON;" +
            "SET ARITHABORT ON;" +
            "SET CONCAT_NULL_YIELDS_NULL ON;" +
            "SET QUOTED_IDENTIFIER ON;" +
            "SET NUMERIC_ROUNDABORT OFF;";

        public DataTable UcitajTrase()
        {
            return VratiDataTable("dbo.Pepeo_UcitajTrase", null);
        }

        public DataTable UcitajNajave()
        {
            return VratiDataTable("dbo.Pepeo_UcitajNajave", null);
        }

        public DataTable UcitajSifarnikKasnjenja()
        {
            return VratiDataTable("dbo.Pepeo_UcitajSifarnikKasnjenja", null);
        }

        public List<PepeoOperacijaDefinicija> UcitajOperacijeZaTrasu(int trasaID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@TrasaID", SqlDbType.Int) { Value = trasaID }
            };

            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajOperacijeZaTrasu", p);
            var result = new List<PepeoOperacijaDefinicija>();

            foreach (DataRow r in dt.Rows)
            {
                result.Add(new PepeoOperacijaDefinicija
                {
                    OperacijaID = Int(r["OperacijaID"]),
                    Redosled = Int(r["Redosled"]),
                    Sifra = Str(r["Sifra"]),
                    Naziv = Str(r["Naziv"]),
                    Drzava = Str(r["Drzava"]),
                    TipPravila = Str(r["TipPravila"]),
                    TrajanjeMinuta = Int(r["TrajanjeMinuta"])
                });
            }

            return result;
        }

        public Dictionary<string, string> UcitajParametrePravila()
        {
            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajParametrePravila", null);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow r in dt.Rows)
            {
                string key = Str(r["ParametarKljuc"]);
                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = Str(r["ParametarVrednost"]);
            }

            return result;
        }

        public List<PepeoCiklusInfo> UcitajCikluse(bool zavrseni, int? trasaID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@Zavrseni", SqlDbType.Bit) { Value = zavrseni },
                new SqlParameter("@TrasaID", SqlDbType.Int) { Value = (object)trasaID ?? DBNull.Value }
            };

            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajCikluse", p);
            var list = new List<PepeoCiklusInfo>();

            foreach (DataRow r in dt.Rows)
            {
                list.Add(new PepeoCiklusInfo
                {
                    CiklusID = Int(r["CiklusID"]),
                    TrasaID = Int(r["TrasaID"]),
                    TrasaNaziv = Str(r["TrasaNaziv"]),
                    NajavaID = NullableInt(r["NajavaID"]),
                    PocetakCiklusa = Date(r["PocetakCiklusa"]),
                    KrajCiklusaPlan = Date(r["KrajCiklusaPlan"]),
                    KrajCiklusaStvarno = NullableDate(r["KrajCiklusaStvarno"]),
                    StatusCiklusa = Str(r["StatusCiklusa"])
                });
            }

            return list;
        }

        public List<PepeoKorakVremena> UcitajKorakeCiklusa(int ciklusID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@CiklusID", SqlDbType.Int) { Value = ciklusID }
            };

            return MapirajKorake(VratiDataTable("dbo.Pepeo_UcitajKorakeCiklusa", p));
        }

        public List<PepeoKorakVremena> UcitajOtvoreniLanacZaTrasu(int trasaID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@TrasaID", SqlDbType.Int) { Value = trasaID }
            };

            return MapirajKorake(VratiDataTable("dbo.Pepeo_UcitajOtvoreniLanacZaTrasu", p));
        }

        public int BrojOtvorenihCiklusaZaTrasu(int trasaID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@TrasaID", SqlDbType.Int) { Value = trasaID }
            };

            DataTable dt = VratiDataTable("dbo.Pepeo_BrojOtvorenihCiklusaZaTrasu", p);
            return dt.Rows.Count == 0 ? 0 : Int(dt.Rows[0]["Broj"]);
        }

        public void GenerisiPocetneCikluse(
            int trasaID,
            DateTime prviPocetak,
            int brojCiklusa,
            PepeoKalkulatorVremena kalkulator)
        {
            if (brojCiklusa <= 0)
                throw new Exception("Broj ciklusa mora biti veci od nule.");

            if (BrojOtvorenihCiklusaZaTrasu(trasaID) > 0)
                throw new Exception("Za izabranu trasu vec postoje aktivni/planirani ciklusi. Zavrsene cikluse mozete pregledati posebnim dugmetom.");

            using (SqlConnection conn = OtvoriKonekciju())
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    DateTime pocetak = prviPocetak;

                    for (int i = 0; i < brojCiklusa; i++)
                    {
                        List<PepeoKorakVremena> koraci = kalkulator.IzracunajCiklus(pocetak);
                        string status = i == 0 ? "TEKUCI" : "PLANIRAN";
                        int ciklusID = KreirajCiklus(conn, tran, trasaID, koraci, status);

                        foreach (PepeoKorakVremena k in koraci)
                        {
                            k.CiklusID = ciklusID;
                            k.TrasaID = trasaID;
                            UpisiKorak(conn, tran, k);
                        }

                        pocetak = koraci[koraci.Count - 1].PlaniraniKraj;
                    }

                    tran.Commit();
                }
                catch
                {
                    try { tran.Rollback(); } catch { }
                    throw;
                }
            }

            IzvrsiNonQuery("dbo.Pepeo_OsveziStatuse", null);
        }

        public int DodajSledeciCiklus(
            int trasaID,
            PepeoKalkulatorVremena kalkulator)
        {
            SqlParameter[] pp =
            {
                new SqlParameter("@TrasaID", SqlDbType.Int) { Value = trasaID }
            };
            DataTable last = VratiDataTable("dbo.Pepeo_PoslednjiCiklusZaTrasu", pp);
            if (last.Rows.Count == 0)
                throw new Exception("Nije pronadjen prethodni ciklus.");

            DateTime pocetak = NullableDate(last.Rows[0]["KrajCiklusaStvarno"])
                ?? Date(last.Rows[0]["KrajCiklusaPlan"]);

            List<PepeoKorakVremena> koraci = kalkulator.IzracunajCiklus(pocetak);
            int noviID;

            using (SqlConnection conn = OtvoriKonekciju())
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    noviID = KreirajCiklus(conn, tran, trasaID, koraci, "PLANIRAN");
                    foreach (PepeoKorakVremena k in koraci)
                    {
                        k.CiklusID = noviID;
                        k.TrasaID = trasaID;
                        UpisiKorak(conn, tran, k);
                    }
                    tran.Commit();
                }
                catch
                {
                    try { tran.Rollback(); } catch { }
                    throw;
                }
            }

            IzvrsiNonQuery("dbo.Pepeo_OsveziStatuse", null);
            return noviID;
        }

        public void SacuvajIzmenuILanac(
            List<PepeoKorakVremena> lanac,
            int indeksIzmenjenog,
            DateTime planiraniKrajPreIzmene,
            string korisnik)
        {
            if (lanac == null || indeksIzmenjenog < 0 || indeksIzmenjenog >= lanac.Count)
                throw new Exception("Neispravan lanac koraka.");

            PepeoKorakVremena selected = lanac[indeksIzmenjenog];
            if (!selected.StvarniKraj.HasValue || !selected.RazlogKasnjenjaID.HasValue)
                throw new Exception("Stvarno vreme i razlog odstupanja su obavezni.");

            using (SqlConnection conn = OtvoriKonekciju())
            using (SqlTransaction tran = conn.BeginTransaction())
            {
                try
                {
                    // Izmenjeni red: plan ostaje kao referenca, upisuje se stvarno vreme.
                    Pozovi(conn, tran, "dbo.Pepeo_AzurirajKorakStvarno", new[]
                    {
                        P("@KorakID", SqlDbType.Int, selected.ID),
                        P("@StvarniPocetak", SqlDbType.DateTime, selected.StvarniPocetak.Value),
                        P("@StvarniKraj", SqlDbType.DateTime, selected.StvarniKraj.Value),
                        P("@OdstupanjeMinuta", SqlDbType.Int, selected.OdstupanjeMinuta),
                        P("@RazlogKasnjenjaID", SqlDbType.Int, selected.RazlogKasnjenjaID.Value),
                        PNVarChar("@Napomena", 1000, selected.Napomena)
                    });

                    Pozovi(conn, tran, "dbo.Pepeo_UpisiIzmenuVremena", new[]
                    {
                        P("@CiklusID", SqlDbType.Int, selected.CiklusID),
                        P("@KorakID", SqlDbType.Int, selected.ID),
                        P("@OperacijaID", SqlDbType.Int, selected.OperacijaID),
                        P("@PlaniraniKrajPreIzmene", SqlDbType.DateTime, planiraniKrajPreIzmene),
                        P("@StvarniKraj", SqlDbType.DateTime, selected.StvarniKraj.Value),
                        P("@OdstupanjeMinuta", SqlDbType.Int, selected.OdstupanjeMinuta),
                        P("@RazlogKasnjenjaID", SqlDbType.Int, selected.RazlogKasnjenjaID.Value),
                        PNVarChar("@Napomena", 1000, selected.Napomena),
                        PNVarChar("@Kreirao", 100, korisnik)
                    });

                    var ciklusiZaRefresh = new HashSet<int>();
                    ciklusiZaRefresh.Add(selected.CiklusID);

                    for (int i = indeksIzmenjenog + 1; i < lanac.Count; i++)
                    {
                        PepeoKorakVremena k = lanac[i];
                        ciklusiZaRefresh.Add(k.CiklusID);

                        Pozovi(conn, tran, "dbo.Pepeo_AzurirajKorakPlanIOdstupanje", new[]
                        {
                            P("@KorakID", SqlDbType.Int, k.ID),
                            P("@PlaniraniPocetak", SqlDbType.DateTime, k.PlaniraniPocetak),
                            P("@PlaniraniKraj", SqlDbType.DateTime, k.PlaniraniKraj),
                            P("@OdstupanjeMinuta", SqlDbType.Int, k.OdstupanjeMinuta)
                        });
                    }

                    foreach (int ciklusID in ciklusiZaRefresh)
                    {
                        Pozovi(conn, tran, "dbo.Pepeo_OsveziZaglavljeCiklusa", new[]
                        {
                            P("@CiklusID", SqlDbType.Int, ciklusID)
                        });
                    }

                    tran.Commit();
                }
                catch
                {
                    try { tran.Rollback(); } catch { }
                    throw;
                }
            }
        }

        public void PoveziNajavuSaCiklusom(int ciklusID, int najavaID)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@CiklusID", SqlDbType.Int) { Value = ciklusID },
                new SqlParameter("@NajavaID", SqlDbType.Int) { Value = najavaID }
            };
            IzvrsiNonQuery("dbo.Pepeo_PoveziCiklusNajava", p);
        }

        public void ZavrsiCiklus(int ciklusID, DateTime stvarniKraj)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@CiklusID", SqlDbType.Int) { Value = ciklusID },
                new SqlParameter("@KrajCiklusaStvarno", SqlDbType.DateTime) { Value = stvarniKraj }
            };
            IzvrsiNonQuery("dbo.Pepeo_ZavrsiCiklus", p);
        }

        private int KreirajCiklus(
            SqlConnection conn,
            SqlTransaction tran,
            int trasaID,
            List<PepeoKorakVremena> koraci,
            string status)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_KreirajCiklus", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@TrasaID", SqlDbType.Int).Value = trasaID;
                cmd.Parameters.Add("@PocetakCiklusa", SqlDbType.DateTime).Value = koraci.Min(x => x.PlaniraniPocetak);
                cmd.Parameters.Add("@KrajCiklusaPlan", SqlDbType.DateTime).Value = koraci.Max(x => x.PlaniraniKraj);
                cmd.Parameters.Add("@StatusCiklusa", SqlDbType.NVarChar, 20).Value = status;
                object value = cmd.ExecuteScalar();
                return Convert.ToInt32(value);
            }
        }

        private void UpisiKorak(SqlConnection conn, SqlTransaction tran, PepeoKorakVremena k)
        {
            Pozovi(conn, tran, "dbo.Pepeo_UpisiKorak", new[]
            {
                P("@CiklusID", SqlDbType.Int, k.CiklusID),
                P("@OperacijaID", SqlDbType.Int, k.OperacijaID),
                P("@Redosled", SqlDbType.Int, k.Redosled),
                P("@PlaniraniPocetak", SqlDbType.DateTime, k.PlaniraniPocetak),
                P("@PlaniraniKraj", SqlDbType.DateTime, k.PlaniraniKraj)
            });
        }

        private List<PepeoKorakVremena> MapirajKorake(DataTable dt)
        {
            var list = new List<PepeoKorakVremena>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new PepeoKorakVremena
                {
                    ID = Int(r["KorakID"]),
                    CiklusID = Int(r["CiklusID"]),
                    TrasaID = Int(r["TrasaID"]),
                    TrasaNaziv = Str(r["TrasaNaziv"]),
                    NajavaID = NullableInt(r["NajavaID"]),
                    StatusCiklusa = Str(r["StatusCiklusa"]),
                    Redosled = Int(r["Redosled"]),
                    OperacijaID = Int(r["OperacijaID"]),
                    SifraOperacije = Str(r["SifraOperacije"]),
                    Operacija = Str(r["Operacija"]),
                    Drzava = Str(r["Drzava"]),
                    TipPravila = Str(r["TipPravila"]),
                    TrajanjeMinuta = Int(r["TrajanjeMinuta"]),
                    PlaniraniPocetak = Date(r["PlaniraniPocetak"]),
                    PlaniraniKraj = Date(r["PlaniraniKraj"]),
                    StvarniPocetak = NullableDate(r["StvarniPocetak"]),
                    StvarniKraj = NullableDate(r["StvarniKraj"]),
                    OdstupanjeMinuta = Int(r["OdstupanjeMinuta"]),
                    RazlogKasnjenjaID = NullableInt(r["RazlogKasnjenjaID"]),
                    RazlogOdstupanja = Str(r["RazlogOdstupanja"]),
                    Partner = Str(r["Partner"]),
                    Napomena = Str(r["Napomena"])
                });
            }
            return list;
        }

        private DataTable VratiDataTable(string procedure, SqlParameter[] parameters)
        {
            using (SqlConnection conn = OtvoriKonekciju())
            using (SqlCommand cmd = new SqlCommand(procedure, conn))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void IzvrsiNonQuery(string procedure, SqlParameter[] parameters)
        {
            using (SqlConnection conn = OtvoriKonekciju())
            using (SqlCommand cmd = new SqlCommand(procedure, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        private SqlConnection OtvoriKonekciju()
        {
            var conn = new SqlConnection(connection);
            conn.Open();
            using (SqlCommand set = new SqlCommand(SqlSessionSettings, conn))
            {
                set.CommandType = CommandType.Text;
                set.ExecuteNonQuery();
            }
            return conn;
        }

        private static void Pozovi(SqlConnection conn, SqlTransaction tran, string procedure, SqlParameter[] parameters)
        {
            using (SqlCommand cmd = new SqlCommand(procedure, conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        private static SqlParameter P(string name, SqlDbType type, object value)
        {
            return new SqlParameter(name, type) { Value = value ?? DBNull.Value };
        }

        private static SqlParameter PNVarChar(string name, int size, string value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim()
            };
        }

        private static int Int(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static int? NullableInt(object value)
        {
            return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static string Str(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value).Trim();
        }

        private static DateTime Date(object value)
        {
            return Convert.ToDateTime(value);
        }

        private static DateTime? NullableDate(object value)
        {
            return value == null || value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }
    }
}
