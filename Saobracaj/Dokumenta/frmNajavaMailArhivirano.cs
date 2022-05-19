using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    public partial class frmNajavaMailArhivirano : Form
    {
        string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        int PaSifra;
        MailMessage mailMessage;
        bool upd = false;
        int najava=0;
        public frmNajavaMailArhivirano()
        {
            InitializeComponent();
        }

        private void frmNajavaMailArhivirano_Load(object sender, EventArgs e)
        {
            FillGV();
            FillCheck();
        }
        private void FillGV()
        {
            var query = "SELECT distinct Partnerji.PaSifra,RTrim(PaNaziv) as Partner,Partnerji.Primalac,Najava.ID,Najava.Otpravna,RTrim(stanice.Opis),Najava.Uputna," +
                "RTrim(stanice1.Opis),Najava.Granicna,RTrim(Stanice2.Opis),Najava.Status,Najava.BrojKola,Najava.PredvidjenoPrimanje,Najava.PredvidjenaPredaja,Najava.Zadatak," +
                "Najava.StvarnoPrimanje,Najava.StvarnaPredaja " +
                "FROM Partnerji " +
                "Inner Join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
                "Inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
                "Inner join stanice on Najava.Otpravna = stanice.ID " +
                "Inner Join stanice as stanice1 on najava.Uputna = stanice1.ID " +
                "inner join stanice as stanice2 on najava.Granicna = stanice2.ID " +
                "Where Partnerji.Primalac = 1 and(Najava.status = 7 or Najava.Status = 9) and Najava.ID in " +
                "(Select ID from NajavaLog where NajavaLog.ID = Najava.ID and PoslatMail = 0 and(Datum >= Dateadd(day, -2, getdate()) and Datum < DateAdd(Day, 1, GetDate())))";

            SqlConnection conn = new SqlConnection(connect);
            var dataAdapter = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[1].HeaderText = "Partner";
            dataGridView1.Columns[1].Width = 150;
            dataGridView1.Columns[2].Visible = false; //posiljalac
            dataGridView1.Columns[3].HeaderText = "Najava";
            dataGridView1.Columns[3].Width = 70;
            dataGridView1.Columns[4].Visible = false; //otpravna
            dataGridView1.Columns[5].HeaderText = "Otpravna";
            dataGridView1.Columns[5].Width = 110;
            dataGridView1.Columns[6].Visible = false; //uputna
            dataGridView1.Columns[7].HeaderText = "Uputna";
            dataGridView1.Columns[7].Width = 110;
            dataGridView1.Columns[8].Visible = false; //granicna
            dataGridView1.Columns[9].HeaderText = "Trenutna";
            dataGridView1.Columns[9].Width = 110;
            dataGridView1.Columns[10].HeaderText = "Status";
            dataGridView1.Columns[10].Width = 50;
            dataGridView1.Columns[11].Width = 60;
            dataGridView1.Columns[14].Width = 190;
        }
        private void FillCheck()
        {
            SqlConnection con = new SqlConnection(connect);
            string query = "SELECT distinct PaSifra,RTrim(PaNaziv) as Partner " +
                "FROM Partnerji " +
                "Inner Join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
                "Inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
                "Inner join stanice on Najava.Otpravna = stanice.ID " +
                "Inner Join stanice as stanice1 on najava.Uputna = stanice1.ID " +
                "inner join stanice as stanice2 on najava.Granicna = stanice2.ID " +
                "Where Partnerji.Primalac = 1 and(status = 7 or status = 9) and PaSifra in(select Platilac from NajavaLog Where NajavaLog.ID = Najava.ID and PoslatMail = 0 " +
                "and(Datum >= Dateadd(day, -2, getdate()) and Datum < DateAdd(Day, 1, GetDate())))";
            SqlCommand cmd = new SqlCommand(query, con);
            SqlDataAdapter da;
            DataSet ds = new DataSet();
            da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            //cbList_Partneri.DataSource = ds.Tables[0];
            cbList_Partneri.DataSource = ds.Tables[0];
            cbList_Partneri.DisplayMember = "Partner";
            cbList_Partneri.ValueMember = "PaSifra";
        }
        private void btn_Posalji_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < cbList_Partneri.Items.Count; i++)
            {
                if (cbList_Partneri.GetItemCheckState(i) == CheckState.Checked)
                {
                    cbList_Partneri.SetSelected(i, true);
                    PaSifra = Convert.ToInt32(cbList_Partneri.SelectedValue);

                    string query = "Select PaKOMail From PartnerjiKontOseba Where PaKoTip=1 and PaKOSifra= " + PaSifra;
                    SqlConnection conn = new SqlConnection(connect);
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    int count = 0;
                    string nizMail = "";
                    while (dr.Read())
                    {
                        if (count == 0)
                        {
                            nizMail = dr["PaKoMail"].ToString();
                            count++;
                        }
                        else
                        {
                            nizMail = nizMail + "," + dr["PaKoMail"].ToString();
                            count++;
                        }

                    }
                    if (nizMail == "")
                    {
                        MessageBox.Show("Za partnera: " + PaSifra + " nije uneta mail adresa");
                    }
                    conn.Close();
                    try
                    {
                        string cuvaj = "disp@kprevoz.co.rs";
                        mailMessage = new MailMessage("disp@kprevoz.co.rs", nizMail);
                        mailMessage.Subject = "Status najave";

                        var select = "SELECT distinct Partnerji.PaSifra,RTrim(PaNaziv) as Partner,Partnerji.Primalac,Najava.ID as Najava,Otpravna," +
                             "RTrim(stanice.Opis) as [OtpravnaStanica],Uputna,RTrim(stanice1.Opis) as [UputnaStanica],Najava.Granicna," +
                             "RTrim(stanice2.Opis) as [TrenutnaStanica],Najava.Status,BrojKola,PredvidjenoPrimanje,PredvidjenaPredaja,Zadatak,StvarnoPrimanje,StvarnaPredaja " +
                             "FROM Partnerji " +
                             "Inner join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
                             "inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
                             "inner join stanice on Najava.Otpravna = stanice.ID " +
                             "inner join stanice as stanice1 on najava.Uputna = stanice1.ID " +
                             "inner join stanice as stanice2 on Najava.Granicna = stanice2.ID " +
                             "Where Partnerji.Primalac = 1 and(Najava.status = 7 or Najava.Status = 9) and PaSifra = "+PaSifra+" and Najava.ID in " +
                "(Select ID from NajavaLog where NajavaLog.ID = Najava.ID and PoslatMail = 0 and(Datum >= Dateadd(day, -2, getdate()) and Datum < DateAdd(Day, 1, GetDate())))";
                        var dataAdapter = new SqlDataAdapter(select, conn);

                        var commandBuilder = new SqlCommandBuilder(dataAdapter);
                        var ds = new DataSet();
                        dataAdapter.Fill(ds);
                        string body = "";

                        body = body + "STATUS NAJAVE: <br/><br/>";
                        foreach (DataRow myRow in ds.Tables[0].Rows)
                        {
                            body = body + "<hr>Najava broj: " + myRow["Najava"].ToString() + "<br/>";
                            najava = Convert.ToInt32(myRow["Najava"].ToString());
                            body = body + "IZ: " + myRow["OtpravnaStanica"].ToString() + "<br/>";
                            body = body + "DO: " + myRow["UputnaStanica"].ToString() + "<br/>";
                            body = body + "Status: " + myRow["Status"].ToString() + "<br/>";
                            //body = body + "Trenutna stanica: " + myRow["TrenutnaStanica"].ToString() + "<br/>";
                            body = body + "Broj kola: " + myRow["BrojKola"].ToString() + "<br/>";
                            int status = Convert.ToInt32(myRow["Status"]);
                            if (cb_Eta.Checked == false && cb_pPrimanje.Checked == false && cb_sPredaja.Checked == false && cb_sPrimanje.Checked == false)
                            {
                                body = body + "Stvarna predaja: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_Eta.Checked == true)
                            {
                                body = body + "ETA: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_pPrimanje.Checked == true)
                            {
                                body = body + "Predviđeno primanje: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                            }
                            if (cb_sPredaja.Checked == true)
                            {
                                body = body + "Stvarna predaja: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_sPrimanje.Checked == true)
                            {
                                body = body + "Stvarno primanje: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                            }
                            body = body + "Zadatak: " + myRow["Zadatak"].ToString() + "<hr><br/><br/>";
                        }
                        body = body + "Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";

                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;
                        SmtpClient smtpClient = new SmtpClient();
                        smtpClient.Host = "mail.kprevoz.co.rs";

                        smtpClient.Port = 25;
                        smtpClient.UseDefaultCredentials = true;
                        smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "pele1122.disp");

                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mailMessage);
                        MessageBox.Show("Uspešno poslato");
                        upd = true;
                        if (upd == true)
                        {
                            conn.Open();
                            SqlCommand updCMD = new SqlCommand(@"Update NajavaLOG set PoslatMail=1 Where ID= "+najava,conn);
                            updCMD.ExecuteNonQuery();
                            conn.Close();
                        }


                        upd = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                        upd = false;
                    }
                }
            }
        }

        private void btn_PosaljiSvi_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < cbList_Partneri.Items.Count; i++)
            {
                if (cbList_Partneri.GetItemCheckState(i) == CheckState.Unchecked)
                {
                    cbList_Partneri.SetItemChecked(i, true);
                    cbList_Partneri.SetSelected(i, true);
                    PaSifra = Convert.ToInt32(cbList_Partneri.SelectedValue);

                    string query = "Select PaKOMail From PartnerjiKontOseba Where PaKoTip=1 and PaKOSifra= " + PaSifra;
                    SqlConnection conn = new SqlConnection(connect);
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    int count = 0;
                    string nizMail = "";
                    while (dr.Read())
                    {
                        if (count == 0)
                        {
                            nizMail = dr["PaKoMail"].ToString();
                            count++;
                        }
                        else
                        {
                            nizMail = nizMail + "," + dr["PaKoMail"].ToString();
                            count++;
                        }
                    }
                    if (nizMail == "")
                    {
                        MessageBox.Show("Za partnera: " + PaSifra.ToString() + " nije uneta mail adresa");
                        return;
                    }
                    conn.Close();


                    try
                    {
                        string cuvaj = "disp@kprevoz.co.rs";
                        mailMessage = new MailMessage("disp@kprevoz.co.rs", nizMail );
                        mailMessage.Subject = "Status najave";

                        var select = "SELECT distinct Partnerji.PaSifra,RTrim(PaNaziv) as Partner,Partnerji.Primalac,Najava.ID as Najava,Otpravna," +
                             "RTrim(stanice.Opis) as [OtpravnaStanica],Uputna,RTrim(stanice1.Opis) as [UputnaStanica],Najava.Granicna," +
                             "RTrim(stanice2.Opis) as [TrenutnaStanica],Najava.Status,BrojKola,PredvidjenoPrimanje,PredvidjenaPredaja,Zadatak,StvarnoPrimanje,StvarnaPredaja " +
                             "FROM Partnerji " +
                             "Inner join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
                             "inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
                             "inner join stanice on Najava.Otpravna = stanice.ID " +
                             "inner join stanice as stanice1 on najava.Uputna = stanice1.ID " +
                             "inner join stanice as stanice2 on Najava.Granicna = stanice2.ID " +
                             "Where Partnerji.Primalac = 1 and(Najava.status = 7 or Najava.Status = 9) and PaSifra= "+PaSifra+" and Najava.ID in " +
                "(Select ID from NajavaLog where NajavaLog.ID = Najava.ID and PoslatMail = 0 and(Datum >= Dateadd(day, -2, getdate()) and Datum < DateAdd(Day, 1, GetDate())))";
                        var dataAdapter = new SqlDataAdapter(select, conn);

                        var commandBuilder = new SqlCommandBuilder(dataAdapter);
                        var ds = new DataSet();
                        dataAdapter.Fill(ds);
                        string body = "";

                        body = body + "STATUS NAJAVE: <br/><br/>";
                        foreach (DataRow myRow in ds.Tables[0].Rows)
                        {
                            body = body + "<hr>Najava broj: " + myRow["Najava"].ToString() + "<br/>";
                            najava = Convert.ToInt32(myRow["Najava"].ToString());
                            body = body + "IZ: " + myRow["OtpravnaStanica"].ToString() + "<br/>";
                            body = body + "DO: " + myRow["UputnaStanica"].ToString() + "<br/>";
                            body = body + "Status: " + myRow["Status"].ToString() + "<br/>";
                            //body = body + "Trenutna stanica: " + myRow["TrenutnaStanica"].ToString() + "<br/>";
                            body = body + "Broj kola: " + myRow["BrojKola"].ToString() + "<br/>";
                            int status = Convert.ToInt32(myRow["Status"]);
                            if (cb_Eta.Checked == false && cb_pPrimanje.Checked == false && cb_sPredaja.Checked==false && cb_sPrimanje.Checked==false)
                            {
                                    body = body + "Stvarna predaja: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_Eta.Checked == true)
                            {
                                body = body + "ETA: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_pPrimanje.Checked == true)
                            {
                                body = body + "Predviđeno primanje: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                            }
                            if (cb_sPredaja.Checked == true)
                            {
                                body = body + "Stvarna predaja: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_sPrimanje.Checked == true)
                            {
                                body = body + "Stvarno primanje: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                            }
                            body = body + "Zadatak: " + myRow["Zadatak"].ToString() + "<hr><br/><br/>";
                        }
                        body = body + "Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";

                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;
                        SmtpClient smtpClient = new SmtpClient();
                        smtpClient.Host = "mail.kprevoz.co.rs";

                        smtpClient.Port = 25;
                        smtpClient.UseDefaultCredentials = true;
                        smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "pele1122.disp");

                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mailMessage);
                        MessageBox.Show("Uspešno poslato");
                        upd = true;
                        if (upd == true)
                        {
                            conn.Open();
                            SqlCommand updCMD = new SqlCommand(@"Update NajavaLOG set PoslatMail=1 Where ID= " + najava,conn);
                            updCMD.ExecuteNonQuery();
                            conn.Close();
                        }

                        upd = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
        }
    }
}
