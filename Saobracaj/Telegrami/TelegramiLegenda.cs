using System.Drawing;
using System.Windows.Forms;

namespace Saobracaj.Telegrami
{
    /// <summary>Traka sa legendom boja ispod grida (kvadratić u boji + opis).</summary>
    internal sealed class TelegramiLegenda : FlowLayoutPanel
    {
        public TelegramiLegenda()
        {
            Dock = DockStyle.Bottom;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = true;
            Padding = new Padding(4, 2, 4, 2);
            Controls.Add(new Label { Text = "Legenda:", AutoSize = true, Font = new Font(Control.DefaultFont, FontStyle.Bold), Margin = new Padding(0, 4, 6, 0) });
        }

        /// <summary>Stavka sa bojom pozadine (uzorak kao red u gridu).</summary>
        public TelegramiLegenda Pozadina(Color boja, string opis, bool podebljano = false)
        {
            return Dodaj(boja, SystemColors.ControlText, podebljano ? "Abc" : "", opis, podebljano);
        }

        /// <summary>Stavka sa bojom slova.</summary>
        public TelegramiLegenda Slova(Color boja, string opis)
        {
            return Dodaj(SystemColors.Window, boja, "Abc", opis, false);
        }

        private TelegramiLegenda Dodaj(Color pozadina, Color slova, string uzorak, string opis, bool podebljano)
        {
            Controls.Add(new Label
            {
                Text = uzorak,
                Width = 34,
                Height = 16,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = pozadina,
                ForeColor = slova,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(Control.DefaultFont, podebljano ? FontStyle.Bold : FontStyle.Regular),
                Margin = new Padding(0, 3, 4, 0)
            });
            Controls.Add(new Label { Text = opis, AutoSize = true, Margin = new Padding(0, 4, 14, 0) });
            return this;
        }
    }
}
