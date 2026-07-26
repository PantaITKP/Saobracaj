using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Pepeo
{
    public class InsertPepeo
    {
        private readonly string connection =
            ConfigurationManager.ConnectionStrings["Saobracaj.Properties.Settings.Perftech_BeogradConnectionString"].ConnectionString;

        public void InsTrasu(int IdTrase, int RB, int Stanica)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand("dbo.InsertPepeoTrase", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = IdTrase;
                cmd.Parameters.Add("@RB", SqlDbType.Int).Value = RB;
                cmd.Parameters.Add("@Stanica", SqlDbType.Int).Value = Stanica;
                conn.Open();
                PostaviObavezneSqlOpcije(conn);
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable UcitajTrase()
        {
            return VratiDataTable("dbo.Pepeo_UcitajTrase", null, CommandType.StoredProcedure);
        }

        public DataTable UcitajNajave()
        {
            return VratiDataTable("dbo.Pepeo_UcitajNajave", null, CommandType.StoredProcedure);
        }

        public DataTable UcitajSifarnikKasnjenja()
        {
            return VratiDataTable("dbo.Pepeo_UcitajSifarnikKasnjenja", null, CommandType.StoredProcedure);
        }

        public List<PepeoSablonOperacije> UcitajSabloneOperacija()
        {
            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajSabloneOperacija", null, CommandType.StoredProcedure);
            var lista = new List<PepeoSablonOperacije>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new PepeoSablonOperacije
                {
                    SablonOperacijeID = VratiInt(row["SablonOperacijeID"]),
                    RedosledKoraka = VratiInt(row["RedosledKoraka"]),
                    SifraKoraka = VratiString(row["SifraKoraka"]),
                    NazivKoraka = VratiString(row["NazivKoraka"]),
                    StanicaID = VratiNullableInt(row["StanicaID"]),
                    StanicaOpis = VratiString(row["StanicaOpis"]),
                    UlazakUStanicu = VratiBool(row["UlazakUStanicu"]),
                    TipPravila = VratiString(row["TipPravila"]),
                    TrajanjeMinuta = VratiInt(row["TrajanjeMinuta"]),
                    Napomena = VratiString(row["Napomena"])
                });
            }

            return lista;
        }

        public Dictionary<string, string> UcitajParametrePravila()
        {
            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajParametrePravila", null, CommandType.StoredProcedure);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in dt.Rows)
            {
                string kljuc = VratiString(row["ParametarKljuc"]);
                string vrednost = VratiString(row["ParametarVrednost"]);

                if (!string.IsNullOrWhiteSpace(kljuc))
                    dict[kljuc] = vrednost;
            }

            return dict;
        }

        public int UcitajAktivniPrevozZaTrasu(int idTrase)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@IDTrase", SqlDbType.Int) { Value = idTrase }
            };

            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajAktivniPrevozZaTrasu", p, CommandType.StoredProcedure);
            if (dt.Rows.Count == 0)
                return 0;

            return VratiInt(dt.Rows[0]["IDPrevoza"]);
        }

        public int KreirajNoviPrevozSaCiklusima(int idTrase, DateTime pocetakPrevoza, List<PepeoKorakVremena> koraci)
        {
            if (koraci == null || koraci.Count == 0)
                throw new Exception("Nema koraka za upis prevoza.");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                PostaviObavezneSqlOpcije(conn);
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int idPrevoza = KreirajPrevoz(conn, tran, idTrase, pocetakPrevoza, "Kreirano iz forme pepeo vremena.");

                        var grupe = koraci
                            .OrderBy(x => x.Ciklus)
                            .ThenBy(x => x.RB)
                            .GroupBy(x => x.Ciklus)
                            .ToList();

                        foreach (var grupa in grupe)
                        {
                            List<PepeoKorakVremena> koraciCiklusa = grupa.ToList();
                            int redniBroj = grupa.Key;
                            DateTime pocetakCiklusa = koraciCiklusa.Min(x => x.PlaniraniPocetak);
                            DateTime krajCiklusa = koraciCiklusa.Max(x => x.PlaniranoVreme);

                            int ciklusId = UpisiCiklus(conn, tran, idPrevoza, idTrase, redniBroj, pocetakCiklusa, krajCiklusa);

                            foreach (PepeoKorakVremena korak in koraciCiklusa)
                            {
                                korak.IDPrevoza = idPrevoza;
                                korak.CiklusID = ciklusId;
                                UpisiKorak(conn, tran, korak);
                            }
                        }

                        tran.Commit();
                        return idPrevoza;
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public void DodajCiklusPrevozu(int idPrevoza, int idTrase, int redniBrojCiklusa, List<PepeoKorakVremena> koraciCiklusa)
        {
            if (idPrevoza <= 0)
                throw new Exception("Nije izabran prevoz.");

            if (koraciCiklusa == null || koraciCiklusa.Count == 0)
                throw new Exception("Nema koraka za upis novog ciklusa.");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                PostaviObavezneSqlOpcije(conn);
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        DateTime pocetakCiklusa = koraciCiklusa.Min(x => x.PlaniraniPocetak);
                        DateTime krajCiklusa = koraciCiklusa.Max(x => x.PlaniranoVreme);
                        int ciklusId = UpisiCiklus(conn, tran, idPrevoza, idTrase, redniBrojCiklusa, pocetakCiklusa, krajCiklusa);

                        foreach (PepeoKorakVremena korak in koraciCiklusa.OrderBy(x => x.RB))
                        {
                            korak.IDPrevoza = idPrevoza;
                            korak.CiklusID = ciklusId;
                            korak.Ciklus = redniBrojCiklusa;
                            UpisiKorak(conn, tran, korak);
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
        }

        public List<PepeoKorakVremena> UcitajKorakePrevoza(int idPrevoza)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@IDPrevoza", SqlDbType.Int) { Value = idPrevoza }
            };

            DataTable dt = VratiDataTable("dbo.Pepeo_UcitajKorakePrevoza", p, CommandType.StoredProcedure);
            return MapirajKorake(dt);
        }

        public DataTable UcitajKorakePrevozaTabela(int idPrevoza)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@IDPrevoza", SqlDbType.Int) { Value = idPrevoza }
            };

            return VratiDataTable("dbo.Pepeo_UcitajKorakePrevoza", p, CommandType.StoredProcedure);
        }

        public DataTable UcitajCiklusePrevoza(int idPrevoza)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@IDPrevoza", SqlDbType.Int) { Value = idPrevoza }
            };

            return VratiDataTable("dbo.Pepeo_UcitajCiklusePrevoza", p, CommandType.StoredProcedure);
        }

        public void PoveziNajavuSaCiklusom(int ciklusId, int najavaId)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CiklusID", SqlDbType.Int) { Value = ciklusId },
                new SqlParameter("@NajavaID", SqlDbType.Int) { Value = najavaId }
            };

            IzvrsiNonQuery("dbo.Pepeo_PoveziCiklusNajava", p, CommandType.StoredProcedure);
        }

        public void ZavrsiCiklus(int ciklusId, DateTime stvarniKraj, int? razlogKasnjenjaId, string napomena)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@CiklusID", SqlDbType.Int) { Value = ciklusId },
                new SqlParameter("@KrajCiklusaStvarno", SqlDbType.DateTime) { Value = stvarniKraj },
                new SqlParameter("@RazlogKasnjenjaID", SqlDbType.Int) { Value = (object)razlogKasnjenjaId ?? DBNull.Value },
                new SqlParameter("@Napomena", SqlDbType.NVarChar, 1000) { Value = (object)napomena ?? DBNull.Value }
            };

            IzvrsiNonQuery("dbo.Pepeo_ZavrsiCiklus", p, CommandType.StoredProcedure);
        }

        public void AzurirajKorakePosleIzmene(List<PepeoKorakVremena> koraci, int indeksOdKogSeAzurira, int idIzmenjenogKoraka, DateTime novoStvarnoVreme, int? razlogKasnjenjaId, string napomena)
        {
            if (koraci == null || indeksOdKogSeAzurira < 0)
                return;

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                PostaviObavezneSqlOpcije(conn);
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand log = new SqlCommand("dbo.Pepeo_EvidentirajRucnuIzmenu", conn, tran))
                        {
                            log.CommandType = CommandType.StoredProcedure;
                            log.Parameters.Add("@VremePrevozaID", SqlDbType.Int).Value = idIzmenjenogKoraka;
                            log.Parameters.Add("@NovoStvarnoVreme", SqlDbType.DateTime).Value = novoStvarnoVreme;
                            log.Parameters.Add("@RazlogKasnjenjaID", SqlDbType.Int).Value = (object)razlogKasnjenjaId ?? DBNull.Value;
                            log.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)napomena ?? DBNull.Value;
                            log.Parameters.Add("@Kreirao", SqlDbType.NVarChar, 100).Value = Environment.UserName;
                            log.ExecuteNonQuery();
                        }

                        for (int i = indeksOdKogSeAzurira; i < koraci.Count; i++)
                        {
                            PepeoKorakVremena korak = koraci[i];
                            AzurirajJedanKorak(conn, tran, korak);
                        }

                        // Plan ciklusa se azurira na osnovu CELOG ciklusa, ne samo od izmenjenog reda.
                        var ciklusi = koraci
                            .Where(x => x.CiklusID > 0)
                            .GroupBy(x => x.CiklusID)
                            .ToList();

                        foreach (var grupa in ciklusi)
                        {
                            DateTime pocetak = grupa.Min(x => x.PlaniraniPocetak);
                            DateTime kraj = grupa.Max(x => x.PlaniranoVreme);
                            AzurirajCiklusPlan(conn, tran, grupa.Key, pocetak, kraj);
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
        }

        private int KreirajPrevoz(SqlConnection conn, SqlTransaction tran, int idTrase, DateTime pocetakPrevoza, string napomena)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_KreirajPrevoz", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = idTrase;
                cmd.Parameters.Add("@PocetakPrevoza", SqlDbType.DateTime).Value = pocetakPrevoza;
                cmd.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)napomena ?? DBNull.Value;

                SqlParameter outParam = new SqlParameter("@IDPrevoza", SqlDbType.Int);
                outParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outParam);

                cmd.ExecuteNonQuery();
                return VratiInt(outParam.Value);
            }
        }

        private int UpisiCiklus(SqlConnection conn, SqlTransaction tran, int idPrevoza, int idTrase, int redniBroj, DateTime pocetak, DateTime kraj)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_UpisiCiklus", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IDPrevoza", SqlDbType.Int).Value = idPrevoza;
                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = idTrase;
                cmd.Parameters.Add("@RedniBrojCiklusa", SqlDbType.Int).Value = redniBroj;
                cmd.Parameters.Add("@PocetakCiklusa", SqlDbType.DateTime).Value = pocetak;
                cmd.Parameters.Add("@KrajCiklusaPlan", SqlDbType.DateTime).Value = kraj;

                SqlParameter outParam = new SqlParameter("@CiklusID", SqlDbType.Int);
                outParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outParam);

                cmd.ExecuteNonQuery();
                return VratiInt(outParam.Value);
            }
        }

        private void UpisiKorak(SqlConnection conn, SqlTransaction tran, PepeoKorakVremena korak)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_UpisiVremePrevoza", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@IDPrevoza", SqlDbType.Int).Value = korak.IDPrevoza;
                cmd.Parameters.Add("@CiklusID", SqlDbType.Int).Value = korak.CiklusID;
                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = korak.IDTrase;
                cmd.Parameters.Add("@RB", SqlDbType.Int).Value = korak.RB;
                cmd.Parameters.Add("@SifraKoraka", SqlDbType.NVarChar, 50).Value = (object)korak.SifraKoraka ?? DBNull.Value;
                cmd.Parameters.Add("@Stanica", SqlDbType.Int).Value = (object)korak.Stanica ?? DBNull.Value;
                cmd.Parameters.Add("@StanicaOpis", SqlDbType.NVarChar, 100).Value = (object)korak.StanicaOpis ?? DBNull.Value;
                cmd.Parameters.Add("@Operacija", SqlDbType.NVarChar, 200).Value = (object)korak.Operacija ?? DBNull.Value;
                cmd.Parameters.Add("@UlazakUStanicu", SqlDbType.Bit).Value = korak.UlazakUStanicu;
                cmd.Parameters.Add("@PlaniraniPocetak", SqlDbType.DateTime).Value = korak.PlaniraniPocetak;
                cmd.Parameters.Add("@PlaniranoVreme", SqlDbType.DateTime).Value = korak.PlaniranoVreme;
                cmd.Parameters.Add("@StvarnoVreme", SqlDbType.DateTime).Value = (object)korak.StvarnoVreme ?? DBNull.Value;
                cmd.Parameters.Add("@RucnoIzmenjeno", SqlDbType.Bit).Value = korak.RucnoIzmenjeno;
                cmd.Parameters.Add("@KasnjenjeMin", SqlDbType.Int).Value = korak.KasnjenjeMin;
                cmd.Parameters.Add("@RazlogKasnjenjaID", SqlDbType.Int).Value = (object)korak.RazlogKasnjenjaID ?? DBNull.Value;
                cmd.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)korak.Napomena ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
        }

        private void AzurirajJedanKorak(SqlConnection conn, SqlTransaction tran, PepeoKorakVremena korak)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_AzurirajVremePrevoza", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = korak.ID;
                cmd.Parameters.Add("@PlaniraniPocetak", SqlDbType.DateTime).Value = korak.PlaniraniPocetak;
                cmd.Parameters.Add("@PlaniranoVreme", SqlDbType.DateTime).Value = korak.PlaniranoVreme;
                cmd.Parameters.Add("@StvarnoVreme", SqlDbType.DateTime).Value = (object)korak.StvarnoVreme ?? DBNull.Value;
                cmd.Parameters.Add("@RucnoIzmenjeno", SqlDbType.Bit).Value = korak.RucnoIzmenjeno;
                cmd.Parameters.Add("@KasnjenjeMin", SqlDbType.Int).Value = korak.KasnjenjeMin;
                cmd.Parameters.Add("@RazlogKasnjenjaID", SqlDbType.Int).Value = (object)korak.RazlogKasnjenjaID ?? DBNull.Value;
                cmd.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)korak.Napomena ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
        }

        private void AzurirajCiklusPlan(SqlConnection conn, SqlTransaction tran, int ciklusId, DateTime pocetak, DateTime kraj)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_AzurirajCiklusPlan", conn, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CiklusID", SqlDbType.Int).Value = ciklusId;
                cmd.Parameters.Add("@PocetakCiklusa", SqlDbType.DateTime).Value = pocetak;
                cmd.Parameters.Add("@KrajCiklusaPlan", SqlDbType.DateTime).Value = kraj;
                cmd.ExecuteNonQuery();
            }
        }

        private List<PepeoKorakVremena> MapirajKorake(DataTable dt)
        {
            var lista = new List<PepeoKorakVremena>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new PepeoKorakVremena
                {
                    ID = VratiInt(row["ID"]),
                    IDPrevoza = VratiInt(row["IDPrevoza"]),
                    CiklusID = VratiInt(row["CiklusID"]),
                    IDTrase = VratiInt(row["IDTrase"]),
                    Najava = VratiNullableInt(row["Najava"]),
                    Ciklus = VratiInt(row["Ciklus"]),
                    StatusCiklusa = VratiString(row["StatusCiklusa"]),
                    RB = VratiInt(row["RB"]),
                    SifraKoraka = VratiString(row["SifraKoraka"]),
                    Stanica = VratiNullableInt(row["Stanica"]),
                    StanicaOpis = VratiString(row["StanicaOpis"]),
                    Operacija = VratiString(row["Operacija"]),
                    UlazakUStanicu = VratiBool(row["UlazakUStanicu"]),
                    PlaniraniPocetak = VratiDatum(row["PlaniraniPocetak"]),
                    PlaniranoVreme = VratiDatum(row["PlaniranoVreme"]),
                    StvarnoVreme = VratiNullableDatum(row["StvarnoVreme"]),
                    RucnoIzmenjeno = VratiBool(row["RucnoIzmenjeno"]),
                    KasnjenjeMin = VratiInt(row["KasnjenjeMin"]),
                    RazlogKasnjenjaID = VratiNullableInt(VratiKolonu(row, "RazlogKasnjenjaID")),
                    RazlogKasnjenja = VratiString(VratiKolonu(row, "RazlogKasnjenja")),
                    Napomena = VratiString(row["Napomena"])
                });
            }

            return lista;
        }

        private DataTable VratiDataTable(string commandText, SqlParameter[] parameters, CommandType commandType)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                cmd.CommandTimeout = 120;

                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                PostaviObavezneSqlOpcije(conn);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }

        private void IzvrsiNonQuery(string commandText, SqlParameter[] parameters, CommandType commandType)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                PostaviObavezneSqlOpcije(conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void PostaviObavezneSqlOpcije(SqlConnection conn)
        {
            if (conn == null || conn.State != ConnectionState.Open)
                throw new InvalidOperationException("SQL konekcija mora biti otvorena pre postavljanja SET opcija.");

            const string sql =
                "SET ANSI_NULLS ON; " +
                "SET ANSI_PADDING ON; " +
                "SET ANSI_WARNINGS ON; " +
                "SET ARITHABORT ON; " +
                "SET CONCAT_NULL_YIELDS_NULL ON; " +
                "SET QUOTED_IDENTIFIER ON; " +
                "SET NUMERIC_ROUNDABORT OFF;";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private object VratiKolonu(DataRow row, string nazivKolone)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(nazivKolone))
                return DBNull.Value;

            return row[nazivKolone];
        }

        private int VratiInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return 0;
        }

        private int? VratiNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return null;
        }

        private DateTime VratiDatum(object value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;

            DateTime result;
            if (DateTime.TryParse(value.ToString(), out result))
                return result;

            return DateTime.MinValue;
        }

        private DateTime? VratiNullableDatum(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            DateTime result;
            if (DateTime.TryParse(value.ToString(), out result))
                return result;

            return null;
        }

        private bool VratiBool(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;

            bool b;
            if (bool.TryParse(value.ToString(), out b))
                return b;

            int i;
            if (int.TryParse(value.ToString(), out i))
                return i != 0;

            return false;
        }

        private string VratiString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            return value.ToString();
        }
    }
}
