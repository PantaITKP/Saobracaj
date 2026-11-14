using System;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmTmpTeretnica
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Porudžbine", jobId.HasValue ? "CIM teretnica (test) · posao " + jobId.Value : "CIM teretnice (test)", null,
                () => IzabranaTeretnica.HasValue ? "#" + IzabranaTeretnica.Value : "");

            var izbor = new KarticaPolja(jobId.HasValue ? "Teretnice posla" : "Teretnice poslednjih 30 dana");
            izbor.Polje(new Label { Text = "Teretnica" }, cboTeretnica, siroko: true);
            izbor.Dugmad(EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            cboTeretnica.SelectedIndexChanged += (s, e) =>
            {
                if (puni) return;
                izgled.OsveziZaglavlje();
                var _ = Prikazi();
            };

            zaglavlje.Prikaz("BrojTeretnice", "Broj teretnice")
                .Prikaz("Posao", "Posao")
                .Prikaz("StanicaOd", "Stanica od")
                .Prikaz("StanicaDo", "Stanica do")
                .Prikaz("Prijemna", "Prijemna")
                .Prikaz("Korisnik", "Korisnik")
                .Prikaz("Verzija", "Verzija")
                .Prikaz("Referenca", "Referenca")
                .Prikaz("Dokument", "Izvorni dokument")
                .Prikaz("Napomena", "Napomena");

            izgled.Mreza.MaxKolona = 2;
            izgled.Mreza.Dodaj(izbor);
            izgled.Mreza.Dodaj(zaglavlje.Kartica);

            tabela.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn k in tabela.Columns)
                    if (k.ValueType == typeof(decimal)) k.DefaultCellStyle.Format = "N3";   // tara i neto u t (19,990), dužina u m
                if (tabela.Columns.Contains("Broj osovina")) tabela.Columns["Broj osovina"].DefaultCellStyle.Format = "0";
            };
            tabela.Dock = DockStyle.Fill;
            lblZbir.Dock = DockStyle.Bottom;
            lblZbir.Height = Theme.Dp(26);
            lblZbir.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblZbir.Font = Theme.Tekstualni(9.5f, true);
            lblZbir.ForeColor = Theme.Tekst;
            lblZbir.BackColor = System.Drawing.Color.White;
            var okvir = new Panel();
            okvir.Controls.Add(tabela);
            okvir.Controls.Add(lblZbir);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(okvir, 1400, 820);
        }
    }
}
