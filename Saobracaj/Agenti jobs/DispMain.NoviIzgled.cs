using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Agenti_jobs
{
    public partial class DispMain
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Transport / Tabla", "DispMain", null);

            // Mapa: Mreža i stanice (tab Mapa), kao GRAF u Planeru.
            var traka = new Panel { Dock = DockStyle.Top, Height = Theme.Dp(40), Padding = new Padding(Theme.Dp(6)) };
            var mapa = new Button { Text = "Mapa – Mreža i stanice", Dock = DockStyle.Left, Width = Theme.Dp(200), UseVisualStyleBackColor = true };
            mapa.Click += (s, e) => OtvoriMapu();
            traka.Controls.Add(mapa);
            tabMreza.Controls.Add(traka);
            traka.SendToBack();
        }

        private void OtvoriMapu()
        {
            var forma = Saobracaj.Planiranje.Forme.frmOPStanice.NaMapi();
            if (!forma.Pravo) { forma.Dispose(); return; }
            forma.Show();
        }
    }
}
