using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Agenti_jobs
{
    public partial class Planer
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Transport / Tabla", "Planer", null);

            // GRAF: Mreža i stanice (tab Mapa); sloj „Stanje na mreži“ se čita iz baze, pa nesačuvane izmene nisu na mapi.
            var graf = new ToolStripMenuItem("GRAF") { Font = sacuvajPlanToolStripMenuItem.Font, BackColor = sacuvajPlanToolStripMenuItem.BackColor };
            graf.Click += (s, e) => OtvoriGraf();
            menuStrip1.Items.Insert(menuStrip1.Items.IndexOf(sacuvajPlanToolStripMenuItem) + 1, graf);
        }

        private void OtvoriGraf()
        {
            var forma = Saobracaj.Planiranje.Forme.frmOPStanice.NaMapi();
            if (!forma.Pravo) { forma.Dispose(); return; }
            forma.Show();
        }
    }
}
