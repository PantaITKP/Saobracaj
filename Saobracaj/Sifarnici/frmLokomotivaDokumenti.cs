using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace Saobracaj.Sifarnici
{
    public partial class frmLokomotivaDokumenti : Form
    {
        bool status = false;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        MailMessage mailMessage;
        public frmLokomotivaDokumenti()
        {
            InitializeComponent();
        }
        public frmLokomotivaDokumenti(string lokomotiva)
        {
            InitializeComponent();
            txt_Lokomotiva.Text = lokomotiva;
            txt_Kreirao.Text = Kor;
        }
        private void FillGV()
        {
            var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var query = "Select * From LokomotivaDokumenta Where Lokomotiva='"+txt_Lokomotiva.Text.ToString()+"'";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
        }
        private void frmLokomotivaDokumenti_Load(object sender, EventArgs e)
        {
            FillGV();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string PictureFolder = txt_putanja.Text;
            ofd1.InitialDirectory = PictureFolder;

            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                txt_putanja.Text = fbd1.SelectedPath.ToString() + ofd1.FileName;
            }
        }

        private void btn_Otvori_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txt_putanja.Text);
        }
        private void KopirajFajlPoTipu(string putanja, string FolderDestinacije)
        {
            string fileName = ofd1.FileName; //Ovde ce trebati promena
            fileName = fileName.Replace(" ", "_");
            string sourcePath = fbd1.SelectedPath.ToString();
            string result = Path.GetFileName(fileName);
            string targetPath = "";

            targetPath = @"\\192.168.1.6\LokomotiveDokumentacija\" + FolderDestinacije;

            string sourceFile = putanja;
            string destFile = System.IO.Path.Combine(targetPath, result);

            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            var remote = Path.Combine(targetPath, result);
            File.Copy(sourceFile, remote);
            txt_putanja.Text = remote;

            if (System.IO.Directory.Exists(sourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sourcePath);
                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = System.IO.Path.GetFileName(s);
                    destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
        }
        private void btn_Dodaj_Click(object sender, EventArgs e)
        {

        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            InsertLokomotivaDokumenti ins = new InsertLokomotivaDokumenti();
            if (status == true)
            {
                KopirajFajlPoTipu(txt_putanja.Text, txt_Lokomotiva.Text);
                ins.InsLokDok(txt_Lokomotiva.Text.ToString(), txt_Opis.Text.ToString().TrimEnd(), txt_Kreirao.Text.ToString().TrimEnd(), txt_putanja.Text.ToString());
                FillGV();
                status = false;
            }
            else
            {
                ins.UpdLokDok(Convert.ToInt32(txt_ID.Text.ToString()), txt_Lokomotiva.Text.ToString(), txt_Opis.Text.ToString().TrimEnd(),
                    txt_Kreirao.Text.ToString().TrimEnd(), txt_putanja.Text.ToString());
                FillGV();
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txt_ID.Text = row.Cells[0].Value.ToString();
                        txt_Lokomotiva.Text = row.Cells[1].Value.ToString();
                        txt_Opis.Text = row.Cells[2].Value.ToString();
                        txt_Kreirao.Text = row.Cells[3].Value.ToString();
                        txt_putanja.Text = row.Cells[4].Value.ToString();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void btn_Posalji_Click(object sender, EventArgs e)
        {
            var connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            DialogResult dr = MessageBox.Show("Posalji mail sa dokumentima", "Attachment", MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = @"\\192.168.1.6\LokomotiveDokumentacija\" + txt_Lokomotiva.Text.ToString().TrimEnd();
                dialog.Multiselect = true;
                dialog.Title = "Izaberite datoteke";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string[] file = dialog.FileNames;
                    try
                    {
                        //string cuvaj = "disp@kprevoz.co.rs";
                        mailMessage = new MailMessage("disp@kprevoz.co.rs", txt_Mail.Text.ToString().TrimEnd());
                        mailMessage.Subject = "Dokumentacija lokomotive:" + txt_Lokomotiva.Text.ToString().TrimEnd();
                        string body = "";
                        body = body + "Dokumentacija lokomotive: " + txt_Lokomotiva.Text.ToString().TrimEnd()+ "<br/><br/>";
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
            else if(dr == DialogResult.No)
            {
                return;
            }
        }
    }
}
