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
            ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.Perftech2"].ConnectionString;

        public void InsTrasu(int IdTrase, int RB, int Stanica)
        {
            using (SqlConnection myConnection = new SqlConnection(connection))
            using (SqlCommand myCommand = new SqlCommand("InsertPepeoTrase", myConnection))
            {
                myCommand.CommandType = CommandType.StoredProcedure;
                myCommand.Parameters.Add("@IDTrase", SqlDbType.Int).Value = IdTrase;
                myCommand.Parameters.Add("@RB", SqlDbType.Int).Value = RB;
                myCommand.Parameters.Add("@Stanica", SqlDbType.Int).Value = Stanica;

                myConnection.Open();
                using (SqlTransaction myTransaction = myConnection.BeginTransaction())
                {
                    myCommand.Transaction = myTransaction;
                    try
                    {
                        myCommand.ExecuteNonQuery();
                        myTransaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        try { myTransaction.Rollback(); } catch { }
                        throw new Exception("Neuspešan upis trase u bazu: " + ex.Message, ex);
                    }
                }
            }
        }

        public DataTable UcitajTrase()
        {
            string trasaSql = @"
SET ARITHABORT ON;
SELECT 
    Glavni.IDTrase AS ID,
    CAST(Glavni.IDTrase AS varchar(10)) + ' - ' + 
    STUFF
    (
        (
            SELECT '/' + CAST(RTRIM(ISNULL(s.Opis, '')) AS varchar(100))
            FROM dbo.PepeoTrase pt
            LEFT JOIN dbo.Stanice s ON pt.Stanica = s.ID
            WHERE pt.IDTrase = Glavni.IDTrase
            ORDER BY pt.RB
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'),
        1,
        1,
        ''
    ) AS Stanice
FROM dbo.PepeoTrase AS Glavni
GROUP BY Glavni.IDTrase
ORDER BY Glavni.IDTrase;";

            return VratiDataTable(trasaSql, null, CommandType.Text);
        }

        public DataTable UcitajNajave()
        {
            string najavaSql = @"
SELECT TOP 100 
    ID,
    CAST(ID AS nvarchar(20)) + ' - ' + ISNULL(PrevozniPut, '') AS Put
FROM dbo.Najava
ORDER BY ID DESC;";

            return VratiDataTable(najavaSql, null, CommandType.Text);
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
                    // U nekim bazama/procedurama stara kolona se zvala Stanica, a u novom modelu StanicaID.
                    // Zbog toga citamo prvo StanicaID, a ako ne postoji onda Stanica.
                    StanicaID = VratiNullableInt(VratiPrvuVrednost(row, "StanicaID", "Stanica")),
                    StanicaOpis = VratiString(VratiPrvuVrednost(row, "StanicaOpis", "StanicaNaziv", "Opis")),
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

        public void SacuvajPlanVremena(int idTrase, int najava, List<PepeoKorakVremena> koraci, bool obrisiPostojeci)
        {
            if (koraci == null)
                koraci = new List<PepeoKorakVremena>();

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        if (obrisiPostojeci)
                        {
                            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_ObrisiPlanVremena", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = idTrase;
                                cmd.Parameters.Add("@Najava", SqlDbType.Int).Value = najava;
                                cmd.ExecuteNonQuery();
                            }
                        }

                        foreach (PepeoKorakVremena korak in koraci)
                        {
                            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_UpisiVremePrevoza", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@IDTrase", SqlDbType.Int).Value = idTrase;
                                cmd.Parameters.Add("@Najava", SqlDbType.Int).Value = najava;
                                cmd.Parameters.Add("@Ciklus", SqlDbType.Int).Value = korak.Ciklus;
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
                                cmd.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)korak.Napomena ?? DBNull.Value;
                                cmd.ExecuteNonQuery();
                            }
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

        public DataTable UcitajPlanVremena(int najava, int idTrase)
        {
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@Najava", SqlDbType.Int) { Value = najava },
                new SqlParameter("@IDTrase", SqlDbType.Int) { Value = idTrase }
            };

            return VratiDataTable("dbo.Pepeo_UcitajPlanVremena", p, CommandType.StoredProcedure);
        }

        public List<PepeoKorakVremena> UcitajKorakeVremena(int najava, int idTrase)
        {
            DataTable dt = UcitajPlanVremena(najava, idTrase);
            var lista = new List<PepeoKorakVremena>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new PepeoKorakVremena
                {
                    ID = VratiInt(row["ID"]),
                    IDTrase = VratiInt(row["IDTrase"]),
                    Najava = VratiInt(row["Najava"]),
                    Ciklus = VratiInt(row["Ciklus"]),
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
                    Napomena = VratiString(row["Napomena"])
                });
            }

            return lista;
        }

        public void AzurirajKorakePosleIzmene(List<PepeoKorakVremena> koraci, int indeksOdKogSeAzurira, int idIzmenjenogKoraka, DateTime novoStvarnoVreme, string napomena)
        {
            if (koraci == null || indeksOdKogSeAzurira < 0)
                return;

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand log = new SqlCommand("dbo.Pepeo_EvidentirajRucnuIzmenu", conn, tran))
                        {
                            log.CommandType = CommandType.StoredProcedure;
                            log.Parameters.Add("@VremePrevozaID", SqlDbType.Int).Value = idIzmenjenogKoraka;
                            log.Parameters.Add("@NovoStvarnoVreme", SqlDbType.DateTime).Value = novoStvarnoVreme;
                            log.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)napomena ?? DBNull.Value;
                            log.Parameters.Add("@Kreirao", SqlDbType.NVarChar, 100).Value = Environment.UserName;
                            log.ExecuteNonQuery();
                        }

                        for (int i = indeksOdKogSeAzurira; i < koraci.Count; i++)
                        {
                            PepeoKorakVremena korak = koraci[i];
                            using (SqlCommand cmd = new SqlCommand("dbo.Pepeo_AzurirajVremePrevoza", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = korak.ID;
                                cmd.Parameters.Add("@PlaniraniPocetak", SqlDbType.DateTime).Value = korak.PlaniraniPocetak;
                                cmd.Parameters.Add("@PlaniranoVreme", SqlDbType.DateTime).Value = korak.PlaniranoVreme;
                                cmd.Parameters.Add("@StvarnoVreme", SqlDbType.DateTime).Value = (object)korak.StvarnoVreme ?? DBNull.Value;
                                cmd.Parameters.Add("@RucnoIzmenjeno", SqlDbType.Bit).Value = korak.RucnoIzmenjeno;
                                cmd.Parameters.Add("@KasnjenjeMin", SqlDbType.Int).Value = korak.KasnjenjeMin;
                                cmd.Parameters.Add("@Napomena", SqlDbType.NVarChar, 1000).Value = (object)korak.Napomena ?? DBNull.Value;
                                cmd.ExecuteNonQuery();
                            }
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

        private DataTable VratiDataTable(string commandText, SqlParameter[] parameters, CommandType commandType)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(commandText, conn))
            {
                cmd.CommandType = commandType;
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }

        private object VratiPrvuVrednost(DataRow row, params string[] kolone)
        {
            if (row == null || row.Table == null || kolone == null)
                return DBNull.Value;

            foreach (string kolona in kolone)
            {
                if (!string.IsNullOrWhiteSpace(kolona) && row.Table.Columns.Contains(kolona))
                    return row[kolona];
            }

            return DBNull.Value;
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

        private string VratiString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;
            return value.ToString();
        }

        private bool VratiBool(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;
            bool result;
            if (bool.TryParse(value.ToString(), out result))
                return result;
            int intResult;
            if (int.TryParse(value.ToString(), out intResult))
                return intResult != 0;
            return false;
        }

        private DateTime VratiDatum(object value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;
            return Convert.ToDateTime(value);
        }

        private DateTime? VratiNullableDatum(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDateTime(value);
        }
    }
}
