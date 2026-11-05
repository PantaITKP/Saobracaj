using System;
using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Kratki unos na ekranima osoblja: polja se slažu jedno ispod drugog.
    internal sealed class MaliDijalog : Form
    {
        private readonly TableLayoutPanel raspored = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12), AutoScroll = true };

        internal MaliDijalog(string naslov)
        {
            Text = naslov;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(Theme.Dp(380), Theme.Dp(120));
            Font = Theme.Tekstualni(9.5f);
            Controls.Add(raspored);
        }

        private void Red(string natpis, Control c)
        {
            raspored.Controls.Add(new Label { Text = natpis, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
            c.Dock = DockStyle.Top;
            raspored.Controls.Add(c);
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + Theme.Dp(52));
        }

        internal ComboBox Izbor(string natpis, params string[] stavke)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            c.Items.AddRange(stavke);
            if (stavke.Length > 0) c.SelectedIndex = 0;
            Red(natpis, c);
            return c;
        }

        internal DateTimePicker Datum(string natpis, DateTime vrednost, bool saVremenom = false)
        {
            var d = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = saVremenom ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy", Value = vrednost };
            Red(natpis, d);
            return d;
        }

        internal TextBox Tekst(string natpis)
        {
            var t = new TextBox();
            Red(natpis, t);
            return t;
        }

        internal bool Prikazi(IWin32Window vlasnik)
        {
            var dugmad = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Theme.Dp(44), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var ok = new Button { Text = "U redu", DialogResult = DialogResult.OK, AutoSize = true };
            var ne = new Button { Text = "Odustani", DialogResult = DialogResult.Cancel, AutoSize = true };
            dugmad.Controls.Add(ok);
            dugmad.Controls.Add(ne);
            Controls.Add(dugmad);
            AcceptButton = ok;
            CancelButton = ne;
            return ShowDialog(vlasnik) == DialogResult.OK;
        }
    }
}
