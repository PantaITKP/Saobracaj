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
    public partial class frmMailNajava : Form
    {
        string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        int PaSifra;
        MailMessage mailMessage;
        public frmMailNajava()
        {
            InitializeComponent();
            FillGV();
            FillCheck();
            FillCombo();
        }
        private void FillGV()
        {
            var query = "SELECT Distinct Partnerji.PaSifra,RTrim(PaNaziv) as Partner,Partnerji.Primalac,Najava.ID,Otpravna,RTrim(stanice.Opis),Uputna,RTrim(stanice1.Opis),Najava.Granicna," +
                "RTrim(Stanice2.Opis),Najava.Status,BrojKola,PredvidjenoPrimanje,PredvidjenaPredaja,Zadatak,StvarnoPrimanje,StvarnaPredaja " +
                "FROM Partnerji " +
                "Inner Join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
                "Inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
                "Inner join stanice on Najava.Otpravna = stanice.ID " +
                "Inner Join stanice as stanice1 on najava.Uputna = stanice1.ID " +
                "inner join stanice as stanice2 on najava.Granicna = stanice2.ID " +
                "Where Partnerji.Primalac = 1 and(status = 1 or status = 2 or status = 4 or status = 5) " +
                "Order by PaSifra desc";

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
        private void FillCombo()
        {
            var query = "Select distinct Status From Najava where status <>0 and status <>3 and status <>6 and status <>7 and status <>8 and status <>9";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Status";
            comboBox1.ValueMember = "Status";
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
              "Where Partnerji.Primalac = 1 and(status = 1 or status = 2 or status = 4 or status = 5) order by PaSifra desc";
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
        private void frmMailNajava_Load(object sender, EventArgs e)
        {
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Left);
        }

        private void btn_Filter_Click(object sender, EventArgs e)
        {
             var query = "SELECT Distinct Partnerji.PaSifra,RTrim(PaNaziv) as Partner,Partnerji.Primalac,Najava.ID,Otpravna,RTrim(stanice.Opis),Uputna,RTrim(stanice1.Opis),Najava.Granicna," +
    "RTrim(Stanice2.Opis),Najava.Status,BrojKola,PredvidjenoPrimanje,PredvidjenaPredaja,Zadatak,StvarnoPrimanje,StvarnaPredaja " +
    "FROM Partnerji " +
    "Inner Join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
    "Inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
    "Inner join stanice on Najava.Otpravna = stanice.ID " +
    "Inner Join stanice as stanice1 on najava.Uputna = stanice1.ID " +
    "inner join stanice as stanice2 on najava.Granicna = stanice2.ID " +
    "Where Partnerji.Primalac = 1 and status = "+Convert.ToInt32(comboBox1.SelectedValue) +
    "Order by PaSifra desc";

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

            string query2 = "SELECT distinct PaSifra,RTrim(PaNaziv) as Partner " +
               "FROM Partnerji " +
               "Inner Join partnerjiKontOseba on Partnerji.PaSifra = partnerjiKontOseba.PaKOSifra " +
               "Inner join Najava on Partnerji.PaSifra = Najava.Platilac " +
             "Inner join stanice on Najava.Otpravna = stanice.ID " +
             "Inner Join stanice as stanice1 on najava.Uputna = stanice1.ID " +
             "inner join stanice as stanice2 on najava.Granicna = stanice2.ID " +
             "Where Partnerji.Primalac = 1 and status= "+Convert.ToInt32(comboBox1.SelectedValue)+" order by PaSifra desc";
            SqlCommand cmd2 = new SqlCommand(query2, conn);
            SqlDataAdapter da2;
            DataSet ds2 = new DataSet();
            da2 = new SqlDataAdapter(cmd2);
            da2.Fill(ds2);

            //cbList_Partneri.DataSource = ds.Tables[0];
            cbList_Partneri.DataSource = ds2.Tables[0];
            cbList_Partneri.DisplayMember = "Partner";
            cbList_Partneri.ValueMember = "PaSifra";
        }

        private void btn_Svi_Click(object sender, EventArgs e)
        {
            FillGV();
        }

        private void btn_PosaljiSvi_Click(object sender, EventArgs e)
        {
            for(int i= 0; i < cbList_Partneri.Items.Count; i++)
            {
                if (cbList_Partneri.GetItemCheckState(i) == CheckState.Unchecked)
                {
                    cbList_Partneri.SetItemChecked(i, true);
                    cbList_Partneri.SetSelected(i,true);
                    PaSifra = Convert.ToInt32(cbList_Partneri.SelectedValue);

                    string query = "Select PaKOMail From PartnerjiKontOseba Where PaKoTip=1 and PaKOSifra= "+PaSifra;
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
                        MessageBox.Show("Za partnera: "+PaSifra.ToString()+" nije uneta mail adresa");
                        return;
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
                            "WHERE Partnerji.Primalac = 1 and (status = 1 or status = 2 or status = 4 or status = 5) and PaSifra = "+PaSifra+" " +
                            "order by Najava.ID";
                        var dataAdapter = new SqlDataAdapter(select, conn);

                        var commandBuilder = new SqlCommandBuilder(dataAdapter);
                        var ds = new DataSet();
                        dataAdapter.Fill(ds);
                        string body = "";

                        body = body + "STATUS NAJAVE: <br/><br/>";
                        foreach (DataRow myRow in ds.Tables[0].Rows)
                        {
                            body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + myRow["Najava"].ToString() + "<br/>";
                            body = body + "IZ / Departure station of train: " + myRow["OtpravnaStanica"].ToString() + "<br/>";
                            body = body + "DO / Arrival station of train: " + myRow["UputnaStanica"].ToString() + "<br/>";
                            body = body + "Status / Status of train: " + myRow["Status"].ToString() + "<br/>";
                            body = body + "Trenutna stanica / Current position of train: " + myRow["TrenutnaStanica"].ToString() + "<br/>";
                            body = body + "Broj kola / Number of wagons: " + myRow["BrojKola"].ToString() + "<br/>";
                            int status = Convert.ToInt32(myRow["Status"]);
                            if (cb_Eta.Checked == false && cb_pPrimanje.Checked == false && cb_sPrimanje.Checked==false && cb_sPredaja.Checked==false)
                            {
                                if (status == 1 || status == 2)
                                {
                                    body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                                }
                                else
                                {
                                    body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                                    body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                                }
                            }
                            if (cb_Eta.Checked == true)
                            {
                                body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_pPrimanje.Checked == true)
                            {
                                body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                            }
                            if (cb_sPredaja.Checked == true)
                            {
                                body = body + "Stvarna predaja / Actual time of handed over train: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_sPrimanje.Checked == true)
                            {
                                body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                            }
                            body = body + "Zadatak(Dodatne informacije o vozu) / Additional information of train: " + myRow["Zadatak"].ToString() + "<hr><br/><br/>";
                        }
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:14px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:1px solid #dddddd; text-align:left; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><th>LEGENDA / LEGEND</th><th></th></tr>");
                        sb.Append("<tr><td>STATUS 1</td><td>NAJAVA OD KLIJENTA / ANNOUNCEMENT FROM CLIENT</td></tr>");
                        sb.Append("<tr><td>STATUS 2</td><td>NAJAVA OD PREVOZNIKA / ANNOUNCEMENT FROM RAILWAY UNDERTAKING</td></tr>");
                        sb.Append("<tr><td>STATUS 3</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 4</td><td>NIJE POKRENUTO / RASPUŠTENO / PARKED</td></tr>");
                        sb.Append("<tr><td>STATUS 5</td><td>U PUTU / ON THE ROAD</td></tr>");
                        sb.Append("<tr><td>STATUS 6</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 7</td><td>PREDATO / HANDED OVER</td></tr>");
                        sb.Append("<tr><td>STATUS 8</td><td>OTKAZANO / CANCELED</td></tr>");
                        sb.Append("<tr><td>STATUS 9</td><td>ČEKA POVRAT / WAITING FOR RETURN</td></tr>");
                        sb.Append("</table>");

                        body = body + sb.ToString() + "<hr><br/><br/>";


                        body = body + "<br/>Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";

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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
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
                        MessageBox.Show("Za partnera: "+PaSifra+" nije uneta mail adresa");
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
                            "WHERE Partnerji.Primalac = 1 and (status = 1 or status = 2 or status = 4 or status = 5) and PaSifra = " + PaSifra + " " +
                            "order by Najava.ID";
                        var dataAdapter = new SqlDataAdapter(select, conn);

                        var commandBuilder = new SqlCommandBuilder(dataAdapter);
                        var ds = new DataSet();
                        dataAdapter.Fill(ds);
                        string body = "";

                        body = body + "STATUS NAJAVE <br/><br/>";
                        foreach (DataRow myRow in ds.Tables[0].Rows)
                        {
                            body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + myRow["Najava"].ToString() + "<br/>";
                            body = body + "IZ / Departure station of train: " + myRow["OtpravnaStanica"].ToString() + "<br/>";
                            body = body + "DO / Arrival station of train: " + myRow["UputnaStanica"].ToString() + "<br/>";
                            body = body + "Status / Status of train: " + myRow["Status"].ToString() + "<br/>";
                            body = body + "Trenutna stanica / Current position of train: " + myRow["TrenutnaStanica"].ToString() + "<br/>";
                            body = body + "Broj kola / Number of wagons: " + myRow["BrojKola"].ToString() + "<br/>";
                            int status = Convert.ToInt32(myRow["Status"]);
                            if (cb_Eta.Checked == false && cb_pPrimanje.Checked == false && cb_sPrimanje.Checked == false && cb_sPredaja.Checked == false)
                            {
                                if (status == 1 || status == 2)
                                {
                                    body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                                }
                                else
                                {
                                    body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                                    body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                                }
                            }
                            if (cb_Eta.Checked == true)
                            {
                                body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_pPrimanje.Checked == true)
                            {
                                body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                            }
                            if (cb_sPredaja.Checked == true)
                            {
                                body = body + "Stvarna predaja / Actual time of handed over train: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_sPrimanje.Checked == true)
                            {
                                body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                            }
                            body = body + "Zadatak(Dodatne informacije o vozu) / Additional information of train: " + myRow["Zadatak"].ToString() + "<hr><br/><br/>";
                        }
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:14px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:1px solid #dddddd; text-align:left; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><th>LEGENDA / LEGEND</th><th></th></tr>");
                        sb.Append("<tr><td>STATUS 1</td><td>NAJAVA OD KLIJENTA / ANNOUNCEMENT FROM CLIENT</td></tr>");
                        sb.Append("<tr><td>STATUS 2</td><td>NAJAVA OD PREVOZNIKA / ANNOUNCEMENT FROM RAILWAY UNDERTAKING</td></tr>");
                        sb.Append("<tr><td>STATUS 3</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 4</td><td>NIJE POKRENUTO / RASPUŠTENO / PARKED</td></tr>");
                        sb.Append("<tr><td>STATUS 5</td><td>U PUTU / ON THE ROAD</td></tr>");
                        sb.Append("<tr><td>STATUS 6</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 7</td><td>PREDATO / HANDED OVER</td></tr>");
                        sb.Append("<tr><td>STATUS 8</td><td>OTKAZANO / CANCELED</td></tr>");
                        sb.Append("<tr><td>STATUS 9</td><td>ČEKA POVRAT / WAITING FOR RETURN</td></tr>");
                        sb.Append("</table>");

                        body = body + sb.ToString() + "<hr><br/><br/>";


                        body = body + "<br/>Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";
                        
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
        }

        private void btn_PosaljiFilter_Click(object sender, EventArgs e)
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
                            "WHERE Partnerji.Primalac = 1 and status= "+Convert.ToString(comboBox1.SelectedValue)+" and PaSifra = " + PaSifra + " " +
                            "order by Najava.ID";
                        var dataAdapter = new SqlDataAdapter(select, conn);

                        var commandBuilder = new SqlCommandBuilder(dataAdapter);
                        var ds = new DataSet();
                        dataAdapter.Fill(ds);
                        string body = "";

                        body = body + "                             STATUS NAJAVE <br/><br/>";
                        foreach (DataRow myRow in ds.Tables[0].Rows)
                        {
                            body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + myRow["Najava"].ToString() + "<br/>";
                            body = body + "IZ / Departure station of train: " + myRow["OtpravnaStanica"].ToString() + "<br/>";
                            body = body + "DO / Arrival station of train: " + myRow["UputnaStanica"].ToString() + "<br/>";
                            body = body + "Status / Status of train: " + myRow["Status"].ToString() + "<br/>";
                            body = body + "Trenutna stanica / Current position of train: " + myRow["TrenutnaStanica"].ToString() + "<br/>";
                            body = body + "Broj kola / Number of wagons: " + myRow["BrojKola"].ToString() + "<br/>";
                            int status = Convert.ToInt32(myRow["Status"]);
                            if (cb_Eta.Checked == false && cb_pPrimanje.Checked == false && cb_sPrimanje.Checked == false && cb_sPredaja.Checked == false)
                            {
                                if (status == 1 || status == 2)
                                {
                                    body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                                }
                                else
                                {
                                    body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                                    body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                                }
                            }
                            if (cb_Eta.Checked == true)
                            {
                                body = body + "ETA / Estimated time of hand over train: " + myRow["PredvidjenaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_pPrimanje.Checked == true)
                            {
                                body = body + "Predviđeno primanje / Estimated time of arrival of train: " + myRow["PredvidjenoPrimanje"].ToString() + "<br/>";
                            }
                            if (cb_sPredaja.Checked == true)
                            {
                                body = body + "Stvarna predaja / Actual time of handed over train: " + myRow["StvarnaPredaja"].ToString() + "<br/>";
                            }
                            if (cb_sPrimanje.Checked == true)
                            {
                                body = body + "Stvarno primanje / Actual time of taked over train: " + myRow["StvarnoPrimanje"].ToString() + "<br/>";
                            }
                            body = body + "Zadatak(Dodatne informacije o vozu) / Additional information of train: " + myRow["Zadatak"].ToString() + "<hr><br/><br/>";
                        }
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:14px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:1px solid #dddddd; text-align:left; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><th>LEGENDA / LEGEND</th><th></th></tr>");
                        sb.Append("<tr><td>STATUS 1</td><td>NAJAVA OD KLIJENTA / ANNOUNCEMENT FROM CLIENT</td></tr>");
                        sb.Append("<tr><td>STATUS 2</td><td>NAJAVA OD PREVOZNIKA / ANNOUNCEMENT FROM RAILWAY UNDERTAKING</td></tr>");
                        sb.Append("<tr><td>STATUS 3</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 4</td><td>NIJE POKRENUTO / RASPUŠTENO / PARKED</td></tr>");
                        sb.Append("<tr><td>STATUS 5</td><td>U PUTU / ON THE ROAD</td></tr>");
                        sb.Append("<tr><td>STATUS 6</td><td></td></tr>");
                        sb.Append("<tr><td>STATUS 7</td><td>PREDATO / HANDED OVER</td></tr>");
                        sb.Append("<tr><td>STATUS 8</td><td>OTKAZANO / CANCELED</td></tr>");
                        sb.Append("<tr><td>STATUS 9</td><td>ČEKA POVRAT / WAITING FOR RETURN</td></tr>");
                        sb.Append("</table>");

                        body = body + sb.ToString() + "<hr><br/><br/>";


                        body = body + "<br/>Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";

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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
        }

        private void btn_Arhivirano_Click(object sender, EventArgs e)
        {
            frmNajavaMailArhivirano arh = new frmNajavaMailArhivirano();
            arh.Show();
        }
    }
}
