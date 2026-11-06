using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj
{
    // Novi glavni meni. Ribbon ostaje kao izvor stavki: svaka stavka poziva PerformClick postojećeg
    // dugmeta, pa otvaranje formi i provera prava u MainP.cs ostaju isti, a nova dugmad se sama pojave u meniju.
    public partial class MainP
    {
        private class ModulMenija
        {
            public string Naziv;
            public Ikona Ikona;
            public List<GrupaMenija> Grupe = new List<GrupaMenija>();
        }

        private class GrupaMenija
        {
            public string Naziv;
            public List<ToolStripButton> Stavke;
        }

        private const int PocetniModul = 2; // Transport

        private readonly List<ModulMenija> moduliMenija = new List<ModulMenija>();
        private SideMenu bocniMeni;
        private HeroMeni hero;
        private MrezaKartica grupeMenija;
        private int aktivniModul = PocetniModul;

        private void PrimeniNoviIzgled()
        {
            SuspendLayout();
            DodajMapuLokomotiva();
            UcitajStavkeIzRibona();
            // Ribbon se ne prikazuje, ali njegova dugmad i dalje nose događaje.
            Controls.Remove(ribbonControlAdv1);

            BackColor = Theme.Pozadina;
            KeyPreview = true;
            Rectangle radno = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(Theme.Dp(1440), radno.Width - Theme.Dp(16)), Math.Min(Theme.Dp(900), radno.Height - Theme.Dp(40)));
            MinimumSize = new Size(Math.Min(Theme.Dp(1000), radno.Width), Math.Min(Theme.Dp(640), radno.Height));
            StartPosition = FormStartPosition.CenterScreen;

            bocniMeni = new SideMenu(Korisnik);
            foreach (ModulMenija m in moduliMenija)
                bocniMeni.DodajModul(m.Naziv, m.Ikona);
            bocniMeni.ModulIzabran += (s, i) => PrikaziModul(i);

            hero = new HeroMeni();
            hero.DodajPrecicu("Novi radni nalog", Ikona.Plus, () => toolStripButton28.PerformClick());
            hero.DodajPrecicu("Centralna tabla", Ikona.Tabla, () => toolStripButton26.PerformClick());
            hero.DodajPrecicu("Najavljeni vozovi", Ikona.StrelicaDesno, () => toolStripButton129.PerformClick());
            hero.DodajPrecicu("Pronađi vagon", Ikona.Pretraga, () => toolStripButton34.PerformClick());
            hero.Pretraga.TextChanged += (s, e) => PrikaziSadrzaj();
            hero.Pretraga.KeyDown += PretragaTaster;

            grupeMenija = new MrezaKartica { Dock = DockStyle.Fill };

            // Redosled dodavanja određuje dokovanje: poslednja dodata kontrola se prva rasporedi.
            var glavniDeo = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Pozadina };
            glavniDeo.Controls.Add(grupeMenija);
            glavniDeo.Controls.Add(hero);
            Controls.Add(glavniDeo);
            Controls.Add(bocniMeni);

            PrikaziModul(PocetniModul);
            ResumeLayout(true);
        }

        // Transport / Tabla, odmah posle Planera i DispMain-a; meni stavke čita iz ribbon-a.
        private void DodajMapuLokomotiva()
        {
            var dugme = new ToolStripButton("Mapa lokomotiva") { DisplayStyle = ToolStripItemDisplayStyle.Text, Name = "tsbOPMapa" };
            dugme.Click += (s, e) =>
            {
                var forma = new Saobracaj.Planiranje.Forme.frmOPMapa();
                if (forma.Pravo) forma.Show(); else forma.Dispose();
            };
            toolStripEx10.Items.Insert(toolStripEx10.Items.IndexOf(toolStripButton139) + 1, dugme);
        }

        private void UcitajStavkeIzRibona()
        {
            var izvori = new[]
            {
                new { Tab = toolStripTabItem1, Ikona = Ikona.Knjiga },
                new { Tab = toolStripTabItem2, Ikona = Ikona.Klipbord },
                new { Tab = toolStripTabItem3, Ikona = Ikona.Voz },
                new { Tab = toolStripTabItem4, Ikona = Ikona.Sat },
                new { Tab = toolStripTabItem5, Ikona = Ikona.Alat },
                new { Tab = toolStripTabItem6, Ikona = Ikona.Aktovka },
                new { Tab = toolStripTabItem7, Ikona = Ikona.Stit },
                new { Tab = toolStripTabItem8, Ikona = Ikona.Kalkulator }
            };
            foreach (var izvor in izvori)
            {
                var modul = new ModulMenija { Naziv = izvor.Tab.Text, Ikona = izvor.Ikona };
                // Panel čuva grupe obrnutim redom u odnosu na prikaz u ribbon-u (dokovane su levo).
                foreach (var traka in izvor.Tab.Panel.Controls.OfType<ToolStrip>().OrderBy(t => t.Left))
                {
                    var stavke = traka.Items.OfType<ToolStripButton>().Where(b => b.Available).ToList();
                    if (stavke.Count > 0)
                        modul.Grupe.Add(new GrupaMenija { Naziv = string.IsNullOrWhiteSpace(traka.Text) ? "Ostalo" : traka.Text, Stavke = stavke });
                }
                moduliMenija.Add(modul);
            }
        }

        private void PrikaziModul(int indeks)
        {
            aktivniModul = indeks;
            bocniMeni.Izaberi(indeks);
            hero.PostaviModul(moduliMenija[indeks].Naziv);
            if (hero.Pretraga.TextLength > 0)
                hero.Pretraga.Clear();
            else
                PrikaziSadrzaj();
        }

        private void PrikaziSadrzaj()
        {
            string upit = hero.Pretraga.Text.Trim();
            grupeMenija.SuspendLayout();
            grupeMenija.Ocisti();
            if (upit.Length == 0)
            {
                foreach (GrupaMenija g in moduliMenija[aktivniModul].Grupe)
                    grupeMenija.Dodaj(NapraviKarticu(g.Naziv, g.Stavke));
            }
            else
            {
                string trazi = BezKvacica(upit);
                foreach (ModulMenija m in moduliMenija)
                    foreach (GrupaMenija g in m.Grupe)
                    {
                        var pogodak = g.Stavke.Where(b => BezKvacica(b.Text).Contains(trazi)).ToList();
                        if (pogodak.Count > 0)
                            grupeMenija.Dodaj(NapraviKarticu(m.Naziv + " / " + g.Naziv, pogodak));
                    }
            }
            grupeMenija.ResumeLayout(true);
            grupeMenija.Rasporedi();
        }

        private static KarticaGrupe NapraviKarticu(string naziv, IEnumerable<ToolStripButton> stavke)
        {
            var kartica = new KarticaGrupe(naziv);
            foreach (ToolStripButton dugme in stavke)
                kartica.Dodaj(dugme.Text, dugme.PerformClick);
            return kartica;
        }

        // Pretraga ne pravi razliku između "š" i "s", jer se forme često traže bez kvačica.
        private static string BezKvacica(string tekst)
        {
            return tekst.ToLowerInvariant().Replace('š', 's').Replace('đ', 'd').Replace('č', 'c').Replace('ć', 'c').Replace('ž', 'z');
        }

        private void PretragaTaster(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape) return;
            hero.Pretraga.Clear();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.K))
            {
                hero.Pretraga.Focus();
                hero.Pretraga.SelectAll();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
