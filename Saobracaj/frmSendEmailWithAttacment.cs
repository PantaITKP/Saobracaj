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
        }
        private void frmSendEmailWithAttacment_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofdlg = new OpenFileDialog();

            if (ofdlg.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    txtAttacment.Text = ofdlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            DialogResult dr = MessageBox.Show("Da li želite da dodate prilog uz mail ? ", "Attachment", MessageBoxButtons.YesNoCancel);
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
                    mailMessage = new MailMessage(txtFrom.Text, txtTo.Text);
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
                        body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + row["ID"].ToString() + "<br/>";
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:1px solid #dddddd; text-align:left; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><td>Najava voza</td><td>Train announcement</td><td>Zuganmeldung</td><td></td></tr>");
                        sb.Append("<tr><td>Otkaz voza</td><td>Train cancellation</td><td>Zugkundigung</td><td></td></tr>");
                        sb.Append("<tr><td>BROJ VOZA</td><td>TRAIN NUMBER</td><td>ZUGNUMMER</td><td>" + row["Voz1"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>POŠILJALAC</td><td>SENDER</td><td>Absender</td><td>" + row["PaNaziv"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNIK</td><td>TRANSPORT OPERATOR</td><td>VERKEHRSUNTERNEHMER</td><td>" + row["UIC"].ToString() + "-" + row["Expr2"].ToString() + "<\n>3212 -Kombinovani prevoz</td></tr>");
                        sb.Append("<tr><td>OTPRAVNA STANICA</td><td>FORWARDING STATION</td><td>VERSANDBAHNHOF</td><td>" + row["Opis"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>UPUTNA STANICA</td><td>STATION OF DESTINATION</td><td>BESTIMMUNGSBAHNHOF</td><td>" + row["Expr1"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PRIMALAC</td><td>CONSIGNEE</td><td>EMPFÄNGER</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ROBA</td><td>GOODS-NHM</td><td>TRANSPORTGUT</td><td>" + row["Broj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNI PUT</td><td>TRAVELINGROUTE</td><td>LEITUNGSWEG</td><td>" + row["PrevozniPut"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>TEŽINA VOZA</td><td>TRAIN BRUTOWEIGHT</td><td>ZUG BRUTO GEWICHT</td><td>" + row["Tezina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>DUŽINA VOZA</td><td>TRAINLENGTH</td><td>ZUGLÄNGE</td><td>" + row["Duzina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>BROJ KOLA</td><td>NUMBER OF WAGON</td><td>ANZAHL DER WAGEN</td><td>" + row["BrojKola"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>RID Nr. UN</td><td>RID Nr. UN</td><td>RID Nr. UN</td><td>" + row["OnBroj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>STATUS</td><td>CANCELLED</td><td>GEKUNDIGT</td><td>" + row["StatusVoza"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ETA</td><td>" + row["Expr1"].ToString() + "</td><td></td><td></td></tr>");
                        sb.Append("<tr><td>Odgovor</td><td> Reply</td><td>Antworten</td><td></td></tr>");
                        sb.Append("<tr><td>Odgovor železničkog prevoznika</td><td>EVU Antwort</td><td>Railway undertakers reply</td><td></td></tr>");
                        sb.Append("<tr><td>Voz će biti primljen dana" + row["PredvidjenoPrimanje"].ToString() + " kao "
                            + row["Voz"].ToString() + "</td><td>Train will be overtaken on the day" + row["PredvidjenoPrimanje"].ToString() +
                            " o'clock as the train number " + row["Voz"].ToString() +
                            "</td><td>Der Zug  wird ubergenohmen werden am Tag" + row["PredvidjenoPrimanje"].ToString() + " Uhr" +" Wie ZugNr " + row["Voz"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>Komentar</td><td></td><td></td><td>" + row["Komentar"].ToString() + "</td></tr>");

                        sb.Append("</table>");

                        body = body + sb.ToString();
                        
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
                    smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "pele1122.disp");

                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mailMessage);
                    conn.Close();
                    MessageBox.Show("Uspešno poslato");
                }
                else if (dr == DialogResult.No)
                {
                    string cuvaj = "disp@kprevoz.co.rs";
                    mailMessage = new MailMessage(txtFrom.Text, txtTo.Text);
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
                        body = body + "<hr>Najava broj / Train designation in Kombinovani Prevoz: " + row["ID"].ToString() + "<br/>";
                        System.Text.StringBuilder sb = new StringBuilder();
                        sb.Append("<style>");
                        sb.Append("table { font-family:arial,sans-serif; font-size:12px; border-collapse:collapse; width:100%;}");
                        sb.Append("td,th { border:1px solid #dddddd; text-align:left; padding:5px;}");
                        sb.Append("</style>");
                        sb.Append("<table>");
                        sb.Append("<tr><td>Najava voza</td><td>Train announcement</td><td>Zuganmeldung</td><td></td></tr>");
                        sb.Append("<tr><td>Otkaz voza</td><td>Train cancellation</td><td>Zugkundigung</td><td></td></tr>");
                        sb.Append("<tr><td>BROJ VOZA</td><td>TRAIN NUMBER</td><td>ZUGNUMMER</td><td>" + row["Voz1"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>POŠILJALAC</td><td>SENDER</td><td>Absender</td><td>" + row["PaNaziv"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNIK</td><td>TRANSPORT OPERATOR</td><td>VERKEHRSUNTERNEHMER</td><td>" + row["UIC"].ToString() + "-" + row["Expr2"].ToString() + "<\n>3212 -Kombinovani prevoz</td></tr>");
                        sb.Append("<tr><td>OTPRAVNA STANICA</td><td>FORWARDING STATION</td><td>VERSANDBAHNHOF</td><td>" + row["Opis"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>UPUTNA STANICA</td><td>STATION OF DESTINATION</td><td>BESTIMMUNGSBAHNHOF</td><td>" + row["Expr1"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PRIMALAC</td><td>CONSIGNEE</td><td>EMPFÄNGER</td><td>" + row["Expr3"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ROBA</td><td>GOODS-NHM</td><td>TRANSPORTGUT</td><td>" + row["Broj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>PREVOZNI PUT</td><td>TRAVELINGROUTE</td><td>LEITUNGSWEG</td><td>" + row["PrevozniPut"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>TEŽINA VOZA</td><td>TRAIN BRUTOWEIGHT</td><td>ZUG BRUTO GEWICHT</td><td>" + row["Tezina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>DUŽINA VOZA</td><td>TRAINLENGTH</td><td>ZUGLÄNGE</td><td>" + row["Duzina"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>BROJ KOLA</td><td>NUMBER OF WAGON</td><td>ANZAHL DER WAGEN</td><td>" + row["BrojKola"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>RID Nr. UN</td><td>RID Nr. UN</td><td>RID Nr. UN</td><td>" + row["OnBroj"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>STATUS</td><td>CANCELLED</td><td>GEKUNDIGT</td><td>" + row["StatusVoza"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>ETA</td><td>" + row["Expr1"].ToString() + "</td><td></td><td></td></tr>");
                        sb.Append("<tr><td>Odgovor</td><td> Reply</td><td>Antworten</td><td></td></tr>");
                        sb.Append("<tr><td>Odgovor železničkog prevoznika</td><td>EVU Antwort</td><td>Railway undertakers reply</td><td></td></tr>");
                        sb.Append("<tr><td>Voz će biti primljen dana" + row["PredvidjenoPrimanje"].ToString() + " u sati" + row["Expr1"].ToString() + " kao " + row["Voz"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>Train will be overtaken on the day" + row["PredvidjenoPrimanje"].ToString() + " at the" + row["Expr1"].ToString() + " o'clock as the train number " + row["Voz"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>Der Zug  wird ubergenohmen werden am Tag" + row["PredvidjenoPrimanje"].ToString() + " Uhr" + row["Expr1"].ToString() + " Wie ZugNr " + row["Voz"].ToString() + "</td></tr>");
                        sb.Append("<tr><td>Komentar</td><td></td><td></td><td>" + row["Komentar"].ToString() + "</td></tr>");

                        sb.Append("</table>");

                        body = body + sb.ToString();

                    }
                    body = body + "<br/>" + txtBody.Text.ToString().TrimEnd();

                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = true;
                    SmtpClient smtpClient = new SmtpClient();
                    smtpClient.Host = "mail.kprevoz.co.rs";
                    smtpClient.Port = 25;
                    smtpClient.UseDefaultCredentials = true;
                    smtpClient.Credentials = new NetworkCredential("disp@kprevoz.co.rs", "pele1122.disp");

                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mailMessage);
                    conn.Close();
                    MessageBox.Show("Uspešno poslato");
                }
                else if (dr == DialogResult.Cancel)
                {
                    return;
                }
            }
        }
    }
}
