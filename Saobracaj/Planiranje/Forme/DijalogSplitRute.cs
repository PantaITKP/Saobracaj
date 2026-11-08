using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Izbor stanica na kojima se ruta taska deli; ponuđene su samo stanice između početne i krajnje.
    // Ispod liste se odmah vidi koji delovi (taskovi) nastaju, da nijedna čekirana stanica ne ostane neprimećena.
    internal sealed class DijalogSplitRute : Form
    {
        private readonly CheckedListBox lista = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
        private readonly Label lblDelovi = new Label { Dock = DockStyle.Bottom, AutoSize = false };
        private readonly List<int> indeksi = new List<int>();
        private readonly TaskOP task;
        private readonly Func<int, string> naziv;
        private static readonly CultureInfo Kultura = CultureInfo.GetCultureInfo("sr-Latn-RS");

        internal List<int> Izabrana { get; private set; } = new List<int>();

        internal DijalogSplitRute(TaskOP t, Func<int, string> naziv, ICollection<int> promeneTrase)
        {
            task = t;
            this.naziv = naziv;
            Text = "Split rute · task " + t.ID;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(Theme.Dp(480), Theme.Dp(600));
            Font = Theme.Tekstualni(9.5f);

            for (int i = 1; i < t.Stanice.Count - 1; i++)
            {
                indeksi.Add(i);
                lista.Items.Add(naziv(t.Stanice[i].StanicaID) + "   ·   km " + t.Stanice[i].Km.ToString("#,##0.0", Kultura) + (promeneTrase.Contains(i) ? "   ← promena trase" : ""));
            }
            // Jedna stanica: split uvek daje dva taska, a svaki se posle deli dalje (po ruti ili po brutu).
            bool skidam = false;
            lista.ItemCheck += (s, e) =>
            {
                if (skidam || e.NewValue != CheckState.Checked) { BeginInvoke((Action)PrikaziDelove); return; }
                skidam = true;
                foreach (int i in lista.CheckedIndices.Cast<int>().ToList())
                    if (i != e.Index) lista.SetItemChecked(i, false);
                skidam = false;
                // CheckedIndices se menja tek posle ItemCheck, zato se pregled računa kad se događaj završi.
                BeginInvoke((Action)PrikaziDelove);
            };

            var opis = new Label
            {
                Dock = DockStyle.Top,
                Height = Theme.Dp(70),
                Padding = new Padding(Theme.Dp(8)),
                Text = naziv(t.OdID) + " – " + naziv(t.DoID) + ". Čekirajte stanicu na kojoj se ruta deli: nastaju dva taska (do te stanice i od nje), a task " + t.ID + " se stornira. Svaki od njih se posle može deliti dalje."
            };
            lblDelovi.Height = Theme.Dp(70);
            lblDelovi.Padding = new Padding(Theme.Dp(8), Theme.Dp(6), Theme.Dp(8), 0);
            lblDelovi.Font = Theme.Tekstualni(9.5f, true);

            var dugmad = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Theme.Dp(44), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var ok = new Button { Text = "Podeli", AutoSize = true };
            var ne = new Button { Text = "Odustani", DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += (s, e) =>
            {
                Izabrana = Cekirane();
                if (Izabrana.Count != 1) { MessageBox.Show(this, "Čekirajte stanicu na kojoj se ruta deli.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                DialogResult = DialogResult.OK;
            };
            dugmad.Controls.Add(ok);
            dugmad.Controls.Add(ne);
            var okvir = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(8), 0, Theme.Dp(8), 0) };
            okvir.Controls.Add(lista);
            Controls.Add(okvir);
            Controls.Add(opis);
            Controls.Add(lblDelovi);
            Controls.Add(dugmad);
            CancelButton = ne;
            PrikaziDelove();
        }

        private List<int> Cekirane()
        {
            return lista.CheckedIndices.Cast<int>().Select(x => indeksi[x]).OrderBy(x => x).ToList();
        }

        // „Nastaju 2 taska: 1. Vreoci – Pančevo glavna (66,6 km) 2. Pančevo glavna – Vršac gr. (81,2 km)“.
        private void PrikaziDelove()
        {
            if (IsDisposed) return;
            List<int> granice = Cekirane();
            if (granice.Count == 0) { lblDelovi.ForeColor = Theme.TekstSporedni; lblDelovi.Text = "Nijedna stanica nije čekirana."; return; }
            granice.Insert(0, 0);
            granice.Add(task.Stanice.Count - 1);
            var redovi = new List<string>();
            for (int g = 0; g + 1 < granice.Count; g++)
            {
                TaskStanica a = task.Stanice[granice[g]], b = task.Stanice[granice[g + 1]];
                redovi.Add((g + 1) + ". " + naziv(a.StanicaID) + " – " + naziv(b.StanicaID) + " (" + (b.Km - a.Km).ToString("#,##0.0", Kultura) + " km)");
            }
            lblDelovi.ForeColor = Theme.Tekst;
            lblDelovi.Text = "Nastaje " + redovi.Count + (redovi.Count < 5 ? " taska" : " taskova") + ":\n" + string.Join("\n", redovi);
        }
    }
}
