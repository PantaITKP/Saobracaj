using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class MailPreview : Form
    {
        public MailPreview() : this("")
        {
        }

        public MailPreview(string tekstMaila)
        {
            InitializeComponent();
            txtMail.Text = tekstMaila ?? "";
        }
    }
}
