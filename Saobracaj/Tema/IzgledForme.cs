using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Tema.Controls;
using Syncfusion.Windows.Forms.Grid.Grouping;

namespace Saobracaj.Tema
{
    // Zajednički kostur radne forme: zaglavlje sa akcijama, kartice polja i tabela ili tabovi ispod.
    // Postojeće kontrole se samo prerasporede, pa imena, događaji i logika forme ostaju isti.
    public class IzgledForme
    {
        private readonly Form forma;
        private readonly ToolStrip traka;
        private readonly GlavaForme glava;
        private Panel okvirDonji;
        private Splitter podela;
        private bool podelaPomerena;

        public MrezaKartica Mreza { get; }

        public IzgledForme(Form forma, string putanja, string naslov, ToolStrip traka,
            Func<string> oznakaZapisa = null, Func<string> status = null, ToolStripButton[] dodatneGlavne = null)
        {
            this.forma = forma;
            this.traka = traka;
            forma.SuspendLayout();
            forma.BackColor = Theme.Pozadina;
            // MetroForm je koristio padding za sopstveno zaglavlje.
            forma.Padding = Padding.Empty;

            glava = new GlavaForme(putanja, naslov, oznakaZapisa ?? (() => ""), status ?? (() => "")) { Dock = DockStyle.Top };
            if (traka != null)
                glava.PostaviAkcije(NapraviGlavneAkcije(traka, dodatneGlavne));
            Mreza = new MrezaKartica { Dock = DockStyle.Fill };
        }

        public void OsveziZaglavlje()
        {
            glava.Invalidate();
        }

        public static void Akcija(ToolStripButton dugme, string tekst, Ikona ikona, Color boja)
        {
            dugme.Text = tekst;
            dugme.ToolTipText = tekst;
            dugme.Image = Ikone.Slika(ikona, Theme.Dp(16), boja);
            dugme.ImageTransparentColor = Color.Empty;
            dugme.ImageScaling = ToolStripItemImageScaling.None;
            dugme.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            dugme.TextImageRelation = TextImageRelation.ImageBeforeText;
            dugme.AutoSize = true;
            dugme.Padding = new Padding(Theme.Dp(8), Theme.Dp(6), Theme.Dp(8), Theme.Dp(6));
            dugme.Margin = new Padding(0, 0, Theme.Dp(6), 0);
        }

        private static ToolStrip NapraviGlavneAkcije(ToolStrip izvor, ToolStripButton[] dodatne)
        {
            var nova = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new TemaRenderer(VrstaTrake.Glava),
                Font = Theme.Tekstualni(9f, true),
                Padding = Padding.Empty,
                CanOverflow = false,
                ImageScalingSize = new Size(Theme.Dp(16), Theme.Dp(16))
            };
            var stavke = new List<ToolStripItem>();

            foreach (var n in new[]
            {
                new { Ime = "tsPrvi", Ikona = Ikona.Prva, Opis = "Prvi" },
                new { Ime = "tsNazad", Ikona = Ikona.Levo, Opis = "Prethodni" },
                new { Ime = "tsNapred", Ikona = Ikona.Desno, Opis = "Sledeći" },
                new { Ime = "tsPoslednja", Ikona = Ikona.Poslednja, Opis = "Poslednji" }
            })
            {
                var dugme = izvor.Items[n.Ime] as ToolStripButton;
                if (dugme == null) continue;
                Akcija(dugme, "", n.Ikona, Color.White);
                dugme.DisplayStyle = ToolStripItemDisplayStyle.Image;
                dugme.ToolTipText = n.Opis;
                dugme.Padding = new Padding(Theme.Dp(5), Theme.Dp(7), Theme.Dp(5), Theme.Dp(7));
                dugme.Margin = new Padding(0, 0, Theme.Dp(4), 0);
                stavke.Add(dugme);
            }

            var separator = izvor.Items.OfType<ToolStripSeparator>().FirstOrDefault();
            if (separator != null && stavke.Count > 0)
            {
                separator.Margin = new Padding(Theme.Dp(4), 0, Theme.Dp(8), 0);
                stavke.Add(separator);
            }

            var novi = izvor.Items["tsNew"] as ToolStripButton;
            if (novi != null)
            {
                Akcija(novi, "Novi", Ikona.Plus, Color.White);
                stavke.Add(novi);
            }
            var obrisi = izvor.Items["tsDelete"] as ToolStripButton;
            if (obrisi != null)
            {
                Akcija(obrisi, "Obriši", Ikona.Kanta, Color.White);
                stavke.Add(obrisi);
            }
            if (dodatne != null)
                stavke.AddRange(dodatne);
            var sacuvaj = izvor.Items["tsSave"] as ToolStripButton;
            if (sacuvaj != null)
            {
                Akcija(sacuvaj, "Sačuvaj", Ikona.Cekiraj, Theme.Primarna);
                sacuvaj.Tag = "primarno";
                sacuvaj.Margin = Padding.Empty;
                stavke.Add(sacuvaj);
            }

            // ToolStrip stavke prelaze na novu traku sa svojim događajima i pravima pristupa.
            nova.Items.AddRange(stavke.ToArray());
            return nova;
        }

        private void StilSporednih()
        {
            traka.Renderer = new TemaRenderer(VrstaTrake.SporedneAkcije);
            traka.GripStyle = ToolStripGripStyle.Hidden;
            traka.Font = Theme.Tekstualni(9f);
            traka.AutoSize = false;
            traka.Height = Theme.Dp(44);
            traka.Padding = new Padding(Theme.Dp(16), Theme.Dp(4), Theme.Dp(16), Theme.Dp(4));
            traka.BackColor = Theme.Pozadina2;
            traka.ImageScalingSize = new Size(Theme.Dp(16), Theme.Dp(16));
            traka.Dock = DockStyle.Top;
        }

        // donji: tabela ili TabControl koji popunjava prostor ispod kartica (ili null ako ga nema).
        public void Zavrsi(Control donji, int sirina = 1100, int visina = 700)
        {
            bool imaSporednih = traka != null && traka.Items.OfType<ToolStripButton>().Any();
            if (traka != null)
            {
                forma.Controls.Remove(traka);
                if (imaSporednih) StilSporednih();
            }

            forma.Controls.Add(Mreza);
            if (donji != null)
            {
                okvirDonji = new Panel
                {
                    Dock = DockStyle.Bottom,
                    BackColor = Color.White,
                    Padding = new Padding(Theme.Dp(8), Theme.Dp(4), Theme.Dp(8), Theme.Dp(8))
                };
                Stilizuj(donji);
                donji.Parent = okvirDonji;
                donji.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                donji.Dock = DockStyle.Fill;

                podela = new Splitter { Dock = DockStyle.Bottom, Height = Theme.Dp(6), BackColor = Theme.Pozadina2, MinSize = Theme.Dp(160), MinExtra = Theme.Dp(120) };
                podela.SplitterMoved += (s, e) => podelaPomerena = true;
                // Redosled dodavanja određuje dokovanje: poslednja dodata kontrola se prva rasporedi.
                forma.Controls.Add(podela);
                forma.Controls.Add(okvirDonji);
            }
            if (imaSporednih) forma.Controls.Add(traka);
            forma.Controls.Add(glava);

            foreach (GroupBox prazna in forma.Controls.OfType<GroupBox>().Where(g => g.Controls.Count == 0).ToList())
                prazna.Visible = false;

            PodesiVelicinu(sirina, visina);
            forma.Resize += (s, e) => PodesiDonji();
            Mreza.VisinaPromenjena += (s, e) => PodesiDonji();
            PodesiDonji();
            forma.ResumeLayout(true);
        }

        private static void Stilizuj(Control donji)
        {
            var tabela = donji as DataGridView;
            if (tabela != null)
            {
                ThemeApplier.StilTabele(tabela);
                return;
            }
            var grupna = donji as GridGroupingControl;
            if (grupna != null)
            {
                ThemeApplier.StilGrupneTabele(grupna);
                return;
            }
            var tabovi = donji as TabControl;
            if (tabovi == null) return;

            ThemeApplier.Apply(tabovi);
            ThemeApplier.TabeviNovogIzgleda(tabovi);
            foreach (TabPage strana in tabovi.TabPages)
                PopuniStranu(strana);
        }

        // Najveća kontrola na strani taba (tabela ili izveštaj) zauzima prostor ispod ostalih kontrola.
        private static void PopuniStranu(TabPage strana)
        {
            Control najveca = strana.Controls.Cast<Control>().OrderByDescending(c => c.Width * c.Height).FirstOrDefault();
            if (najveca == null || najveca.Dock != DockStyle.None) return;
            // Kontrola usidrena sa sve četiri strane već prati veličinu strane.
            const AnchorStyles sveStrane = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            if (najveca.Anchor == sveStrane) return;
            if (strana.Controls.Count == 1)
            {
                najveca.Dock = DockStyle.Fill;
                return;
            }
            if (!(najveca is DataGridView || najveca is GridGroupingControl || najveca is Microsoft.Reporting.WinForms.ReportViewer)) return;
            bool ostaleIznad = strana.Controls.Cast<Control>().All(c => c == najveca || c.Bottom <= najveca.Top + 4);
            if (ostaleIznad)
                ThemeApplier.Popuni(strana, najveca, najveca.Top);
        }

        // Zaglavlje i tema, a kontrole ostaju tamo gde su u Designer-u (paneli, više tabela, logika koja pomera kontrole).
        public static IzgledForme ZadrziRaspored(Form forma, string putanja, string naslov, ToolStrip traka = null,
            Func<string> oznakaZapisa = null)
        {
            var izgled = new IzgledForme(forma, putanja, naslov, traka, oznakaZapisa);
            int sirina = forma.ClientSize.Width;
            int visina = forma.ClientSize.Height;
            if (traka != null) forma.Controls.Remove(traka);

            Control[] kontrole = forma.Controls.Cast<Control>().ToArray();
            // Prostor koji je ostao od stare trake ili zaglavlja MetroForm-a se uklanja.
            var slobodne = kontrole.Where(c => c.Dock == DockStyle.None).ToList();
            int odsecak = kontrole.Length > 0 && slobodne.Count == kontrole.Length ? Math.Max(0, slobodne.Min(c => c.Top) - Theme.Dp(12)) : 0;

            // Panel na početku ima veličinu forme, da bi usidrene kontrole zadržale odnose pri širenju.
            var povrsina = new Panel { BackColor = Theme.Pozadina, AutoScroll = true, Bounds = new Rectangle(0, 0, sirina, visina - odsecak) };
            forma.Controls.Clear();
            foreach (Control c in slobodne)
                c.Top -= odsecak;
            povrsina.Controls.AddRange(kontrole);
            ThemeApplier.Apply(povrsina);

            forma.Controls.Add(povrsina);
            bool imaSporednih = traka != null && traka.Items.OfType<ToolStripButton>().Any();
            int zaglavlje = izgled.glava.Height + (imaSporednih ? Theme.Dp(44) : 0);
            if (imaSporednih)
            {
                izgled.StilSporednih();
                forma.Controls.Add(traka);
            }
            forma.Controls.Add(izgled.glava);

            izgled.PodesiVelicinu(Math.Max(sirina, 900), visina - odsecak + zaglavlje);
            povrsina.Dock = DockStyle.Fill;
            forma.ResumeLayout(true);
            return izgled;
        }

        private void PodesiVelicinu(int sirina, int visina)
        {
            Rectangle radno = Screen.PrimaryScreen.WorkingArea;
            forma.MinimumSize = new Size(Math.Min(Theme.Dp(900), radno.Width), Math.Min(Theme.Dp(520), radno.Height));
            if (forma.WindowState != FormWindowState.Normal) return;
            forma.ClientSize = new Size(Math.Min(Theme.Dp(sirina), radno.Width - Theme.Dp(16)), Math.Min(Theme.Dp(visina), radno.Height - Theme.Dp(40)));
            if (forma.StartPosition == FormStartPosition.WindowsDefaultLocation)
                forma.StartPosition = FormStartPosition.CenterScreen;
        }

        // Kartice dobijaju koliko im treba, a tabela sve ostalo; povlačenje podele isključuje ovo pravilo.
        private void PodesiDonji()
        {
            if (okvirDonji == null || podelaPomerena || forma.ClientSize.Height <= 0) return;
            int gore = glava.Height + (forma.Controls.Contains(traka) ? traka.Height : 0) + podela.Height;
            okvirDonji.Height = Math.Max(Theme.Dp(200), forma.ClientSize.Height - gore - Mreza.PotrebnaVisina);
        }
    }
}
