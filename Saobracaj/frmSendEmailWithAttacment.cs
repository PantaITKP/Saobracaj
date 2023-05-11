using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Mime;
using System.IO;

namespace Saobracaj
{
    public partial class frmSendEmailWithAttacment : Form
    {
        // ArrayList alAttachments;
        MailMessage mailMessage;
        int ID;
        public frmSendEmailWithAttacment()
        {
            InitializeComponent();
        }
        public frmSendEmailWithAttacment(int id)
        {
            ID = id;
            InitializeComponent();
            label6.Text = ID.ToString();
            MailPrimaoca();
        }
        private void MailPrimaoca()
        {
            string mail = "";
            string mailZa = "";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "select ID,Platilac,Partnerji.PaEMail From Najava inner join Partnerji on Najava.Platilac=Partnerji.PaSifra Where ID="+ID;
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int count = 0;

            while (dr.Read())
            {
                mail = dr[2].ToString().TrimEnd();
                mail.Replace(";", ",");
            }
            txtTo.Text = mail;
            conn.Close();
            string query2 = "Select ID,Platilac,Partnerji.PaEmail From Najava inner join Partnerji on Najava.PrevoznikZa=Partnerji.PaSifra Where ID=" + ID;
            conn.Open();
            SqlCommand cmd2 = new SqlCommand(query2, conn);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            while (dr2.Read())
            {
                mailZa = dr2[2].ToString().TrimEnd();
                mailZa.Replace(";", ",");
            }
            txt_CC.Text = mailZa;
            conn.Close();

        }
        private void frmSendEmailWithAttacment_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            DialogResult dr = MessageBox.Show("Da li želite da dodate prilog uz mail?", "Attachment", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.Yes)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                string dir = @"\\192.168.1.6\Dokumentacija vozova - MILICA";
                dir = dir.Replace("192.168.1.6", "WSS");
                dialog.InitialDirectory = dir;
                dialog.Title = "Izaberite datoteku";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string[] file = dialog.FileNames;
                    string cuvaj = "disp@kprevoz.co.rs";
                    mailMessage = new MailMessage(txtFrom.Text.ToString(), txtTo.Text.ToString());
                    mailMessage.CC.Add(txt_CC.Text.ToString().TrimEnd());
                    mailMessage.Subject = txtTema.Text;

                    var select = "SELECT Najava.ID as ID, Trase.Voz as Voz, Najava.Posiljalac as Posiljalac, Najava.Prevoznik as Prevoznik, Najava.Otpravna as Otpravna, " +
                        "Najava.Uputna as Uputna, Najava.Primalac as Primalac, Najava.RobaNHM as RobaNHM, " +
                        "Najava.PrevozniPut as PrevozniPut, Najava.Tezina as Tezina, Najava.Duzina as Duzina, Najava.BrojKola as BrojKola, Najava.RID as RID, " +
                        "Najava.PredvidjenoPrimanje as PredvidjenoPrimanje, Najava.StvarnoPrimanje as StvarnoPrimanje, " +
                        "Najava.PredvidjenaPredaja as PredvidjenaPredaja, Najava.StvarnaPredaja as StvarnaPredaja, Najava.[Status] as Status, Najava.OnBroj as OnBroj, " +
                        "Najava.Verzija as Verzija, Najava.Razlog as Razlog, Najava.DatumUnosa as DatumUnosa, " +
                        "stanice.Opis as Opis, stanice.Granicna as Granicna, stanice_1.Opis AS Expr1, stanice_1.Kod as Kod, NHM.Broj as Broj, NHM.Naziv as Naziv, " +
                        "Partnerji.PaNaziv as PaNaziv,  Partnerji_1.PaNaziv AS Expr2, " +
                        "Partnerji_2.PaNaziv AS Expr3, Najava.RIDBroj as RIDBroj, '1' as Dodaj, Partnerji_1.UIC as UIC, Najava.Komentar as Komentar,  " +
                        "StatusVoza.Opis as StatusVoza, Najava.BrojNajave as BrojNajave, " +
                        "t.Voz as Voz1, Partnerji_3.PaNaziv as PrevoznikZaO,Partnerji_3.UIC as PrevozZaUIC " +
                        "FROM Partnerji INNER JOIN " +
                        "Najava INNER JOIN " +
                        "stanice ON Najava.Otpravna = stanice.ID INNER JOIN " +
                        "stanice AS stanice_1 ON Najava.Uputna = stanice_1.ID left JOIN " +
                        "NHM ON Najava.RobaNHM = NHM.ID ON Partnerji.PaSifra = Najava.Posiljalac INNER JOIN " +
                        "Partnerji AS Partnerji_1 ON Najava.Prevoznik = Partnerji_1.PaSifra INNER JOIN " +
                        "Partnerji AS Partnerji_2 ON Najava.Primalac = Partnerji_2.PaSifra inner join " +
                        "Partnerji AS Partnerji_3 ON Najava.PrevoznikZa = Partnerji_3.PaSifra inner join " +
                        "Trase on Trase.ID = Najava.Voz inner join StatusVoza on StatusVoza.ID = Najava.[Status] " +
                        "inner join Trase t on t.ID = Najava.VozP " +
                        "where Najava.ID =" + ID;
                    var conn = new SqlConnection(connect);
                    conn.Open();
                    var da = new SqlDataAdapter(select, conn);
                    var ds = new DataSet();
                    da.Fill(ds);
                    string body = "";
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + row["ID"].ToString() + "<br/><hr>";
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:2px solid #dddddd; text-align:center; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><td>BROJ VOZA:</td><td>TRAIN NUMBER:</td><td>" + row["Voz"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>POŠILJALAC:</td><td>SENDER:</td><td>" + row["PaNaziv"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PRIMALAC:</td><td>CONSIGNEE:</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>OTPRAVNA STANICA:</td><td>FORWARDING STATION:</td><td>" + row["Opis"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>UPUTNA STANICA:</td><td>STATION OF DESTINATION:</td><td>" + row["Expr1"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNIK:</td><td>TRANSPORT OPERATOR:</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNI PUT:</td><td>TRAVELING ROUTE:</td><td>" + row["PrevozniPut"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>BROJ KOLA</td><td>NUMBER OF WAGON</td><td>" + row["BrojKola"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>TEŽINA VOZA</td><td>TRAIN BRUTOWEIGHT</td><td>" + row["Tezina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>DUŽINA VOZA</td><td>TRAINLENGTH</td><td>" + row["Duzina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ROBA:</td><td>GOODS-NHM:</td><td>" + row["Broj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>RID:</td><td>RID:</td><td>" + row["RIDBroj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ETA</td><td>ETA:</td><td>" + row["PredvidjenaPredaja"].ToString() + "</td></tr>");
                        sb.Append("</table>");
                        body = body + sb.ToString();
                        body = body + "<hr>";
                        System.Text.StringBuilder sb2 = new StringBuilder();
                        sb2.Append("<style>");
                        sb2.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                        sb2.Append("td,th { border:1px solid #dddddd; text-align:left; padding:1px;}");
                        sb2.Append("</style>");
                        sb2.Append("<table>");
                        sb2.Append("<tr><td></td><td>Odgovor železničkog prevoznika/Next carrier response:</td><td></td></tr>");
                        sb2.Append("<tr><td>Voz će biti prihvaćen:</td><td>Train will be accepted:</td><td></td></tr>");
                        sb2.Append("<tr><td>Datum i vreme:</td><td>Date and time:</td><td></td></tr>");
                        sb2.Append("<tr><td>Odgovorna osoba:</td><td>Responsible person:</td><td></td></tr>");
                        sb2.Append("<tr><td>Komentar:</td><td>Comment:</td><td></td></tr>");
                        sb2.Append("</table>");
                        body = body + sb2.ToString();

                    }
                    body = body + "<br/>" + txtBody.Text.ToString().TrimEnd();

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
                    smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "D1$p.pele1616");

                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mailMessage);
                    conn.Close();
                    MessageBox.Show("Uspešno poslato");
                }
            }
            if (dr == DialogResult.No)
            {
                //string[] file = dialog.FileNames;
                string cuvaj = "disp@kprevoz.co.rs";
                mailMessage = new MailMessage(txtFrom.Text.ToString(), txtTo.Text.ToString());
                mailMessage.CC.Add(txt_CC.Text.ToString().TrimEnd());
                mailMessage.Subject = txtTema.Text;

                var select = "SELECT Najava.ID as ID, Trase.Voz as Voz, Najava.Posiljalac as Posiljalac, Najava.Prevoznik as Prevoznik, Najava.Otpravna as Otpravna, " +
                    "Najava.Uputna as Uputna, Najava.Primalac as Primalac, Najava.RobaNHM as RobaNHM, " +
                    "Najava.PrevozniPut as PrevozniPut, Najava.Tezina as Tezina, Najava.Duzina as Duzina, Najava.BrojKola as BrojKola, Najava.RID as RID, " +
                    "Najava.PredvidjenoPrimanje as PredvidjenoPrimanje, Najava.StvarnoPrimanje as StvarnoPrimanje, " +
                    "Najava.PredvidjenaPredaja as PredvidjenaPredaja, Najava.StvarnaPredaja as StvarnaPredaja, Najava.[Status] as Status, Najava.OnBroj as OnBroj, " +
                    "Najava.Verzija as Verzija, Najava.Razlog as Razlog, Najava.DatumUnosa as DatumUnosa, " +
                    "stanice.Opis as Opis, stanice.Granicna as Granicna, stanice_1.Opis AS Expr1, stanice_1.Kod as Kod, NHM.Broj as Broj, NHM.Naziv as Naziv, " +
                    "Partnerji.PaNaziv as PaNaziv,  Partnerji_1.PaNaziv AS Expr2, " +
                    "Partnerji_2.PaNaziv AS Expr3, Najava.RIDBroj as RIDBroj, '1' as Dodaj, Partnerji_1.UIC as UIC, Najava.Komentar as Komentar,  " +
                    "StatusVoza.Opis as StatusVoza, Najava.BrojNajave as BrojNajave, " +
                    "t.Voz as Voz1, Partnerji_3.PaNaziv as PrevoznikZaO,Partnerji_3.UIC as PrevozZaUIC " +
                    "FROM Partnerji INNER JOIN " +
                    "Najava INNER JOIN " +
                    "stanice ON Najava.Otpravna = stanice.ID INNER JOIN " +
                    "stanice AS stanice_1 ON Najava.Uputna = stanice_1.ID left JOIN " +
                    "NHM ON Najava.RobaNHM = NHM.ID ON Partnerji.PaSifra = Najava.Posiljalac INNER JOIN " +
                    "Partnerji AS Partnerji_1 ON Najava.Prevoznik = Partnerji_1.PaSifra INNER JOIN " +
                    "Partnerji AS Partnerji_2 ON Najava.Primalac = Partnerji_2.PaSifra inner join " +
                    "Partnerji AS Partnerji_3 ON Najava.PrevoznikZa = Partnerji_3.PaSifra inner join " +
                    "Trase on Trase.ID = Najava.Voz inner join StatusVoza on StatusVoza.ID = Najava.[Status] " +
                    "inner join Trase t on t.ID = Najava.VozP " +
                    "where Najava.ID =" + ID;
                var conn = new SqlConnection(connect);
                conn.Open();
                var da = new SqlDataAdapter(select, conn);
                var ds = new DataSet();
                da.Fill(ds);
                string body = "";
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + row["ID"].ToString() + "<br/><hr>";
                    System.Text.StringBuilder sb = new StringBuilder();
                    sb.Append("<style>");
                    sb.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                    sb.Append("td,th { border:2px solid #dddddd; text-align:center; padding:5px;}");
                    sb.Append("</style>");
                    sb.Append("<table>");
                    sb.Append("<tr><td>BROJ VOZA:</td><td>TRAIN NUMBER:</td><td>" + row["Voz"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>POŠILJALAC:</td><td>SENDER:</td><td>" + row["PaNaziv"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>PRIMALAC:</td><td>CONSIGNEE:</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>OTPRAVNA STANICA:</td><td>FORWARDING STATION:</td><td>" + row["Opis"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>UPUTNA STANICA:</td><td>STATION OF DESTINATION:</td><td>" + row["Expr1"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>PREVOZNIK:</td><td>TRANSPORT OPERATOR:</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>PREVOZNI PUT:</td><td>TRAVELING ROUTE:</td><td>" + row["PrevozniPut"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>BROJ KOLA</td><td>NUMBER OF WAGON</td><td>" + row["BrojKola"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>TEŽINA VOZA</td><td>TRAIN BRUTOWEIGHT</td><td>" + row["Tezina"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>DUŽINA VOZA</td><td>TRAINLENGTH</td><td>" + row["Duzina"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>ROBA:</td><td>GOODS-NHM:</td><td>" + row["Broj"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>RID:</td><td>RID:</td><td>" + row["RIDBroj"].ToString() + "</td></tr>");
                    sb.Append("<tr><td>ETA</td><td>ETA:</td><td>" + row["PredvidjenaPredaja"].ToString() + "</td></tr>");
                    sb.Append("</table>");
                    body = body + sb.ToString();
                    body = body + "<hr>";
                    System.Text.StringBuilder sb2 = new StringBuilder();
                    sb2.Append("<style>");
                    sb2.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                    sb2.Append("td,th { border:1px solid #dddddd; text-align:left; padding:1px;}");
                    sb2.Append("</style>");
                    sb2.Append("<table>");
                    sb2.Append("<tr><td></td><td>Odgovor železničkog prevoznika/Next carrier response:</td><td></td></tr>");
                    sb2.Append("<tr><td>Voz će biti prihvaćen:</td><td>Train will be accepted:</td><td></td></tr>");
                    sb2.Append("<tr><td>Datum i vreme:</td><td>Date and time:</td><td></td></tr>");
                    sb2.Append("<tr><td>Odgovorna osoba:</td><td>Responsible person:</td><td></td></tr>");
                    sb2.Append("<tr><td>Komentar:</td><td>Comment:</td><td></td></tr>");
                    sb2.Append("</table>");
                    body = body + sb2.ToString();
                }
                body = body + "<br/>" + txtBody.Text.ToString().TrimEnd();

                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "mail.kprevoz.co.rs";

                smtpClient.Port = 25;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "D1$p.pele1616");

                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
                conn.Close();
                MessageBox.Show("Uspešno poslato");
            }
            if (dr == DialogResult.Cancel)
            {
                return;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string[] path = { Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "UF" };
            string save = Path.Combine(path);
            
            MessageBox.Show(save);
        }
    }
}
