using System.Data;
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

            // GRAF: Mreža i stanice (tab Mapa) sa vagonima iz trenutnog stanja tabele „Stanje na mreži“, i nesačuvanih izmena.
            var graf = new ToolStripMenuItem("GRAF") { Font = sacuvajPlanToolStripMenuItem.Font, BackColor = sacuvajPlanToolStripMenuItem.BackColor };
            graf.Click += (s, e) => OtvoriGraf();
            menuStrip1.Items.Insert(menuStrip1.Items.IndexOf(sacuvajPlanToolStripMenuItem) + 1, graf);
        }

        private void OtvoriGraf()
        {
            DataTable izvor = gridMreza?.Data ?? dgvMreza.DataSource as DataTable;
            DataTable stanje = null;
            if (izvor != null)
            {
                dgvMreza.EndEdit();
                stanje = izvor.Clone();
                foreach (DataRow r in izvor.Rows)
                    if (r.RowState != DataRowState.Deleted) stanje.ImportRow(r);
            }
            var forma = new Saobracaj.Planiranje.Forme.frmOPStanice(stanje);
            if (!forma.Pravo) { forma.Dispose(); return; }
            forma.Show();
        }
    }
}
