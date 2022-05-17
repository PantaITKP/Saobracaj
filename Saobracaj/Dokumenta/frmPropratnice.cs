using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    public partial class frmPropratnice : Form
    {
        public static string code = "frmPropratnice";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        bool status = false;
        string niz = "";
        MailMessage mailMessage;
        public string IdGrupe()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            //Sifarnici.frmLogovanje frm = new Sifarnici.frmLogovanje();         
            string query = "Select IdGrupe from KorisnikGrupa Where Korisnik = " + "'" + Kor.TrimEnd() + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int count = 0;

            while (dr.Read())
            {
                if (count == 0)
                {
                    niz = dr["IdGrupe"].ToString();
                    count++;
                }
                else
                {
                    niz = niz + "," + dr["IdGrupe"].ToString();
                    count++;
                }

            }
            conn.Close();
            return niz;
        }
        private int IdForme()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select IdForme from Forme where Rtrim(Code)=" + "'" + code + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                idForme = Convert.ToInt32(dr["IdForme"].ToString());
            }
            conn.Close();
            return idForme;
        }

        private void PravoPristupa()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select * From GrupeForme Where IdGrupe in (" + niz + ") and IdForme=" + idForme;
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows == false)
            {
                MessageBox.Show("Nemate prava za pristup ovoj formi", code);
                Pravo = false;
            }
            else
            {
                Pravo = true;
                while (reader.Read())
                {
                    insert = Convert.ToBoolean(reader["Upis"]);
                    if (insert == false)
                    {
                        tsNew.Enabled = false;
                    }
                    update = Convert.ToBoolean(reader["Izmena"]);
                    if (update == false)
                    {
                        tsSave.Enabled = false;
                    }
                    delete = Convert.ToBoolean(reader["Brisanje"]);
                    if (delete == false)
                    {
                        tsDelete.Enabled = false;
                    }
                }
            }

            conn.Close();
        }
        public frmPropratnice()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
            FillCombo();

            txt_ID.Enabled = false;
            //txt_putanjaZ.Visible = false;
            //txt_putanjaR.Visible = false;
        }
        private void FillCombo()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var query = "Select PaSifra,PaNaziv From Partnerji Where Posiljalac=1 order by PaSifra";
            SqlConnection conn = new SqlConnection(s_connection);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            combo_Firma.DataSource = ds.Tables[0];
            combo_Firma.DisplayMember = "PaNaziv";
            combo_Firma.ValueMember = "PaSifra";

            var query2 = "Select DeSifra,RTrim(DeIme)+' '+RTrim(DePriimek) as Zaposleni From Delavci order by Zaposleni";
            SqlDataAdapter da2 = new SqlDataAdapter(query2, conn);
            DataSet ds2 = new DataSet();
            da2.Fill(ds2);
            combo_Zaduzen.DataSource = ds2.Tables[0];
            combo_Zaduzen.DisplayMember = "Zaposleni";
            combo_Zaduzen.ValueMember = "DeSifra";
        }
        private void frmPropratnice_Load(object sender, EventArgs e)
        {
            GVPropratince();
            GVRazduzivanje();
            GVZaduzivanje();
        }
        private void GVPropratince()
        {
            var select = "select ID,IDNajave,Zaduzen,RTrim(Delavci.DeIme)+' '+RTrim(Delavci.DePriimek) as Zaposleni,ZaduzenaFirma,RTrim(Partnerji.PaNaziv),Napomena " +
                "from Propratnica " +
                "Inner Join Delavci on Propratnica.Zaduzen = Delavci.DeSifra " +
                "Inner Join Partnerji on Propratnica.ZaduzenaFirma = Partnerji.PaSifra " +
                "Order by ID desc";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView3.ReadOnly = true;
            dataGridView3.DataSource = ds.Tables[0];
            dataGridView3.Columns[0].HeaderText = "ID";
            dataGridView3.Columns[0].Width = 40;
            dataGridView3.Columns[1].HeaderText = "ID Najave";
            dataGridView3.Columns[1].Width = 50;
            dataGridView3.Columns[2].Visible = false; //Zaduzen
            dataGridView3.Columns[3].HeaderText = "Zaduzen";
            dataGridView3.Columns[3].Width = 130;
            dataGridView3.Columns[4].Visible = false;
            dataGridView3.Columns[5].HeaderText = "Zaduzena Firma";
            dataGridView3.Columns[5].Width = 180;
            dataGridView3.Columns[6].Width = 150;
        }
        private void GVZaduzivanje()
        {
            var select = "select PropratnicaZaduzenje.ID,PropratnicaZaduzenje.IdNajave,Propratnica.Napomena as NapomenaPropratnica,IdPropratnica,ZaposleniId," +
                "RTrim(Korisnici.Korisnik) as Zaposleni,PropratnicaZaduzenje.Napomena as NapomenaZaduzenje,VremeZaduzenja,PropratniceZaduzivanjeSlike.Slika," +
                "PropratniceZaduzivanjeSlike.Ime " +
                "From PropratnicaZaduzenje " +
                "Inner join Propratnica on PropratnicaZaduzenje.IdNajave = Propratnica.IDNajave " +
                "inner join Korisnici on PropratnicaZaduzenje.ZaposleniId = Korisnici.DeSifra " +
                "Inner join PropratniceZaduzivanjeSlike on PropratnicaZaduzenje.ID = PropratniceZaduzivanjeSlike.PropratnicaZaduzivanjeId " +
                "order by IdPropratnica desc";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[0].Width = 40;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 70;
            dataGridView1.Columns[5].Width = 60;
            dataGridView1.Columns[6].Width = 120;
            dataGridView1.Columns[8].Width = 120;
        }
        public void filterZaduzivanje(int najava)
        {
            var select = "select PropratnicaZaduzenje.ID,PropratnicaZaduzenje.IdNajave,Propratnica.Napomena as NapomenaPropratnica,IdPropratnica,ZaposleniId," +
                "RTrim(Korisnici.Korisnik) as Zaposleni,PropratnicaZaduzenje.Napomena as NapomenaZaduzenje,VremeZaduzenja,PropratniceZaduzivanjeSlike.Slika," +
                "PropratniceZaduzivanjeSlike.Ime " +
                "From PropratnicaZaduzenje " +
                "Inner join Propratnica on PropratnicaZaduzenje.IdNajave = Propratnica.IDNajave " +
                "inner join Korisnici on PropratnicaZaduzenje.ZaposleniId = Korisnici.DeSifra " +
                "Inner join PropratniceZaduzivanjeSlike on PropratnicaZaduzenje.ID = PropratniceZaduzivanjeSlike.PropratnicaZaduzivanjeId Where Propratnica.IdNajave="+najava +
                "order by IdPropratnica desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[0].Width = 40;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 70;
            dataGridView1.Columns[5].Width = 60;
            dataGridView1.Columns[6].Width = 120;
            dataGridView1.Columns[8].Width = 120;
        }

        private void GVRazduzivanje()
        {
            var select = "select PropratnicaRazduzenje.ID,PropratnicaRazduzenje.IdNajave,Propratnica.Napomena as NapomenaPropratnica,IdPropratnica,ZaposleniId," +
                "RTrim(Korisnici.Korisnik) as Zaposleni,PropratnicaRazduzenje.Napomena as NapomenaRazduzivanje,VremeRazduzenja,PropratniceRazduzivanjeSlike.Slika," +
                "PropratniceRazduzivanjeSlike.Ime " +
                "From PropratnicaRazduzenje " +
                "Inner join Propratnica on PropratnicaRazduzenje.IdNajave = Propratnica.IDNajave " +
                "inner join Korisnici on PropratnicaRazduzenje.ZaposleniId = Korisnici.DeSifra " +
                "Inner join PropratniceRazduzivanjeSlike on PropratnicaRazduzenje.ID = PropratniceRazduzivanjeSlike.PropratnicaRazduzivanjeId " +
                "order by IdPropratnica desc";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;
            dataGridView2.Columns[1].Width = 60;
            dataGridView2.Columns[3].Width = 60;
            dataGridView2.Columns[4].Width = 70;
            dataGridView2.Columns[5].Width = 60;
            dataGridView2.Columns[6].Width = 120;
            dataGridView2.Columns[8].Width = 120;
        }
        private void filterRazduzivanje(int najava)
        {
            var select = "select PropratnicaRazduzenje.ID,PropratnicaRazduzenje.IdNajave,Propratnica.Napomena as NapomenaPropratnica,IdPropratnica,ZaposleniId," +
                 "RTrim(Korisnici.Korisnik) as Zaposleni,PropratnicaRazduzenje.Napomena as NapomenaRazduzivanje,VremeRazduzenja,PropratniceRazduzivanjeSlike.Slika," +
                 "PropratniceRazduzivanjeSlike.Ime " +
                 "From PropratnicaRazduzenje " +
                 "Inner join Propratnica on PropratnicaRazduzenje.IdNajave = Propratnica.IDNajave " +
                 "inner join Korisnici on PropratnicaRazduzenje.ZaposleniId = Korisnici.DeSifra " +
                 "Inner join PropratniceRazduzivanjeSlike on PropratnicaRazduzenje.ID = PropratniceRazduzivanjeSlike.PropratnicaRazduzivanjeId " +
                 "Where Propratnica.IDNajave="+najava +
                 "order by IdPropratnica desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;
            dataGridView2.Columns[1].Width = 60;
            dataGridView2.Columns[3].Width = 60;
            dataGridView2.Columns[4].Width = 70;
            dataGridView2.Columns[5].Width = 60;
            dataGridView2.Columns[6].Width = 120;
            dataGridView2.Columns[8].Width = 120;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            GVPropratince();
            GVRazduzivanje();
            GVZaduzivanje();
        }

        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView3.Rows)
                {
                    if (row.Selected)
                    {
                        txt_ID.Text = row.Cells[0].Value.ToString();
                        txt_IdNajave.Text = row.Cells[1].Value.ToString();
                        txt_Napomena.Text = row.Cells[6].Value.ToString();
                        combo_Zaduzen.SelectedValue = row.Cells[2].Value.ToString();
                        combo_Firma.SelectedValue = row.Cells[4].Value.ToString();
                        filterZaduzivanje(Convert.ToInt32(txt_IdNajave.Text));
                        filterRazduzivanje(Convert.ToInt32(txt_IdNajave.Text));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }
        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            if (status == true)
            {
                InsertPropratnica ins = new InsertPropratnica();
                ins.InsPropratnica(Convert.ToInt32(txt_IdNajave.Text.ToString().TrimEnd()), txt_Napomena.Text.ToString().TrimEnd(),Convert.ToInt32(combo_Zaduzen.SelectedValue),Convert.ToInt32(combo_Firma.SelectedValue));
                status = false;
                GVPropratince();
            }
            else
            {
                InsertPropratnica upd = new InsertPropratnica();
                upd.UpdPropratnica(Convert.ToInt32(txt_ID.Text.ToString().TrimEnd()), Convert.ToInt32(txt_IdNajave.Text.ToString().TrimEnd()), txt_Napomena.Text.ToString().TrimEnd(), Convert.ToInt32(combo_Zaduzen.SelectedValue), Convert.ToInt32(combo_Firma.SelectedValue));
                GVPropratince();
            }
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertPropratnica del = new InsertPropratnica();
            del.DeletePropratnica(Convert.ToInt32(txt_ID.Text.ToString().TrimEnd()));
            status = false;
            GVPropratince();
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected)
                {
                    TextBox pom = new TextBox();
                    pom.Text = row.Cells[3].Value.ToString().TrimEnd();

                    string query = "Select Slika from PropratniceZaduzivanjeSlike Where PropratnicaZaduzivanjeId=" + Convert.ToInt32(pom.Text);
                    var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection conn = new SqlConnection(connect);
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        txt_putanjaZ.Text = dr["Slika"].ToString();
                    }
                    conn.Close();
                }
            }
        }
        private void btn_prikaziZ_Click(object sender, EventArgs e)
        {
            if (txt_putanjaZ.Text.Equals(""))
            {
                MessageBox.Show("Mora se selektovati propratnica");
            }
            else
            {
                System.Diagnostics.Process.Start(txt_putanjaZ.Text);
            }
        }


        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Selected)
                {
                    TextBox pom = new TextBox();
                    pom.Text = row.Cells[3].Value.ToString().TrimEnd();
                    string query = "Select Slika from PropratniceRazduzivanjeSlike Where PropratnicaRazduzivanjeId=" + Convert.ToInt32(pom.Text);
                    var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection conn = new SqlConnection(connect);
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                       txt_putanjaR.Text = dr["Slika"].ToString().TrimEnd();
                    }
                    conn.Close();
                }
            }
        }

        private void btn_prikaziR_Click(object sender, EventArgs e)
        {
            if (txt_putanjaR.Text.Equals(""))
            {
                MessageBox.Show("Mora se selektovati propratnica");
            }
            else
            {
                System.Diagnostics.Process.Start(txt_putanjaR.Text);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            DialogResult dr = MessageBox.Show("Da li želite da dodate prilog uz mail?", "Attachment", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.Yes)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Title = "Izaberite datoteku";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string[] file = dialog.FileNames;
                    string query = "Select DeEmail From Delavci Where DeSifra= " + Convert.ToInt32(combo_Zaduzen.SelectedValue);
                    SqlConnection conn = new SqlConnection(connect);
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader dRead = cmd.ExecuteReader();
                    int count = 0;
                    string nizMail = "";
                    while (dRead.Read())
                    {
                        if (count == 0)
                        {
                            nizMail = dRead["DeEmail"].ToString();
                            count++;
                        }
                        else
                        {
                            nizMail = nizMail + "," + dRead["DeEmail"].ToString();
                            count++;
                        }

                    }
                    if (nizMail == "")
                    {
                        MessageBox.Show("Za ovog korisnika nije uneta mail adresa");
                        return;
                    }
                    conn.Close();
                    try
                    {
                        string cuvaj = "disp@kprevoz.co.rs";
                        mailMessage = new MailMessage("disp@kprevoz.co.rs", nizMail);
                        //mailMessage.CC.Add(cuvaj);
                        mailMessage.Subject = "Zaduženje";
                        string body = "";
                        body = body + "Zaduženje broj: " + txt_ID.Text.ToString().TrimEnd() + "<br/>Za: " + combo_Zaduzen.Text.ToString().TrimEnd() + "<br/>";
                        body = body + "Najava: " + txt_IdNajave.Text.ToString().TrimEnd() + "<br/>";
                        body = body + "Zadužena firma: " + combo_Firma.Text.ToString().TrimEnd() + "<br/>";
                        body = body + "Napomena: " + txt_Napomena.Text.ToString().TrimEnd() + "<br/><br/>";
                        body = body + "Srdačan pozdrav, <br/>" + "Dispečerska služba, Kombinovani prevoz";

                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = true;
                        SmtpClient smtpClient = new SmtpClient();
                        smtpClient.Host = "mail.kprevoz.co.rs";
                        for (int i = 0; i < file.Length; i++)
                        {
                            Attachment data = new Attachment(file[i], MediaTypeNames.Application.Octet);

                            // Add time stamp information for the file.
                            ContentDisposition disposition = data.ContentDisposition;
                            disposition.CreationDate = System.IO.File.GetCreationTime(file[i]);
                            disposition.ModificationDate = System.IO.File.GetLastWriteTime(file[i]);
                            disposition.ReadDate = System.IO.File.GetLastAccessTime(file[i]);

                            // Add the file attachment to this e-mail message.
                            mailMessage.Attachments.Add(data);
                        }
                       

                        smtpClient.Port = 25;
                        smtpClient.UseDefaultCredentials = true;
                        smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "pele1122.disp");

                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mailMessage);
                        MessageBox.Show("Uspesno poslato");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
            else if (dr == DialogResult.No)
            {
                string query = "Select DeEmail From Delavci Where DeSifra= " + Convert.ToInt32(combo_Zaduzen.SelectedValue);
                SqlConnection conn = new SqlConnection(connect);
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader dRead = cmd.ExecuteReader();
                int count = 0;
                string nizMail = "";
                while (dRead.Read())
                {
                    if (count == 0)
                    {
                        nizMail = dRead["DeEmail"].ToString();
                        count++;
                    }
                    else
                    {
                        nizMail = nizMail + "," + dRead["DeEmail"].ToString();
                        count++;
                    }
                }
                if (nizMail == "")
                {
                    MessageBox.Show("Za ovog korisnika nije uneta mail adresa");
                    return;
                }
                conn.Close();
                try
                {
                    string cuvaj = "disp@kprevoz.co.rs";
                    mailMessage = new MailMessage("disp@kprevoz.co.rs", nizMail);
                    mailMessage.CC.Add(cuvaj);
                    mailMessage.Subject = "Zaduženje";
                    string body = "";
                    body = body + "Zaduženje broj: " + txt_ID.Text.ToString().TrimEnd() + "<br/>Za: " + combo_Zaduzen.Text.ToString().TrimEnd() + "<br/>";
                    body = body + "Najava: " + txt_IdNajave.Text.ToString().TrimEnd() + "<br/>";
                    body = body + "Zadužena firma: " + combo_Firma.Text.ToString().TrimEnd() + "<br/>";
                    body = body + "Napomena: " + txt_Napomena.Text.ToString().TrimEnd() + "<br/><br/>";
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
                    MessageBox.Show("Uspesno poslato");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
            else if (dr == DialogResult.Cancel)
            {
                return;
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }

}
