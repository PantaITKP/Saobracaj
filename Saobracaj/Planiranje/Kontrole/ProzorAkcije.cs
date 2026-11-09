using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Planiranje.Forme;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Kontrole
{
    // Spisak stavki iz „Stanje na mreži“ i „Najave vozova“ koje stoje PlanMrezeRepo.DanaUpozorenja i više dana.
    // Ispravka se radi na istim tabovima u Planeru ili dispMain-u; ovde je samo pregled, a dvoklik prikazuje mesto na mapi.
    internal sealed class ProzorAkcije : Form
    {
        private readonly DataGridView tabela = EkranOP.NovaTabela("Lat", "Lon");
        private readonly Label lblOpis = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 8, 10, 8), MaximumSize = new System.Drawing.Size(1100, 0) };

        // Izabrano mesto (lat, lon), za centriranje mape.
        internal event Action<double, double> MestoIzabrano;

        internal ProzorAkcije()
        {
            Text = "Potrebna akcija";
            StartPosition = FormStartPosition.CenterParent;
            Size = new System.Drawing.Size(1150, 520);
            ShowInTaskbar = false;
            tabela.Dock = DockStyle.Fill;
            tabela.ReadOnly = true;
            tabela.MultiSelect = false;
            tabela.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var red = tabela.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (red != null) MestoIzabrano?.Invoke(Convert.ToDouble(red["Lat"]), Convert.ToDouble(red["Lon"]));
            };
            tabela.DataBindingComplete += (s, e) =>
            {
                tabela.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                foreach (string k in new[] { "Stavka", "Šta uraditi" })
                    if (tabela.Columns.Contains(k))
                    {
                        tabela.Columns[k].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        tabela.Columns[k].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                tabela.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            };
            Controls.Add(tabela);
            Controls.Add(lblOpis);
        }

        internal void Prikazi(List<Tuple<string, MestoPlana, StavkaPlana>> stavke, DateTime osvezeno)
        {
            var t = new DataTable();
            foreach (string k in new[] { "Sloj", "Mesto", "Stavka" }) t.Columns.Add(k);
            t.Columns.Add("Uneto", typeof(string));
            t.Columns.Add("Poslednja izmena", typeof(string));
            t.Columns.Add("Očekivano", typeof(string));
            // Stanje na mreži: dana od unosa; najava: dana od očekivanog vremena.
            t.Columns.Add("Kasni (dana)", typeof(int));
            t.Columns.Add("Šta uraditi");
            t.Columns.Add("Lat", typeof(double));
            t.Columns.Add("Lon", typeof(double));
            Func<DateTime?, string> vreme = d => !d.HasValue ? "" : d.Value.ToString(d.Value.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm");
            foreach (Tuple<string, MestoPlana, StavkaPlana> x in stavke)
                t.Rows.Add(x.Item1, x.Item2.Naziv, x.Item3.Opis,
                    x.Item3.Uneto.HasValue ? x.Item3.Uneto.Value.ToString("dd.MM.yyyy HH:mm") : "",
                    x.Item3.Izmenjeno.HasValue ? x.Item3.Izmenjeno.Value.ToString("dd.MM.yyyy HH:mm") : "",
                    vreme(x.Item3.Ocekivano), x.Item3.DanaKasni, x.Item3.Akcija, x.Item2.Lat, x.Item2.Lon);
            tabela.DataSource = t;
            lblOpis.Text = (stavke.Count == 0 ? "Nema stavki za proveru." : stavke.Count + " stavki za proveru.")
                + " Stanje na mreži: red stoji " + PlanMrezeRepo.DanaUpozorenja + " i više dana od unosa – organizujte prevoz pri sledećem planu ili ažurirajte stanje."
                + " Najave: očekivano vreme je prošlo pre " + PlanMrezeRepo.DanaUpozorenja + " i više dana – proverite da li je voz ušao, ažurirajte očekivano vreme ili uklonite najavu"
                + " (najava bez datuma u očekivanom vremenu, kao „?“, ne traži proveru). Ispravke se rade na tabovima „Stanje na mreži“ i „Najave vozova“ u Planeru ili dispMain-u."
                + " Dvoklik na red prikazuje mesto na mapi. Osveženo u " + osvezeno.ToString("HH:mm") + ".";
        }
    }
}
