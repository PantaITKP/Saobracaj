using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Kontrole;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Gantogram zauzetosti sa ekrana Taskovi: lokomotive (taskovi, poslovi plana, servis i namenska)
    // ili osoblje (zadaci, smene, odsustva i nepopunjena radna mesta taskova). Samo prikaz; dvoklik na task ga bira na ekranu Taskovi.
    internal sealed class frmOPGanttTaskova : Form
    {
        internal enum Vrsta { Lokomotive, Osoblje }

        private readonly Vrsta vrsta;
        private readonly Action<int> izaberiTask;
        private readonly GanttPanel gantt = new GanttPanel { Dock = DockStyle.Fill };
        private readonly DateTimePicker dtOd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = Theme.Dp(110) };
        private readonly DateTimePicker dtDo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = Theme.Dp(110) };
        private readonly CheckBox chkSve = new CheckBox { AutoSize = true };
        private readonly CheckBox chkSati = new CheckBox { Text = "Po satima", AutoSize = true };
        private readonly Label lblStanje = new Label { AutoSize = true };
        private readonly Label lblLegenda = new Label { Dock = DockStyle.Bottom, Height = Theme.Dp(40) };

        private PlanKontekst kontekst;
        private List<TaskOP> taskovi;
        private OsobljeKontekst osoblje;

        internal frmOPGanttTaskova(Vrsta v, DateTime od, DateTime doDatuma, PlanKontekst k, List<TaskOP> sviTaskovi, Action<int> izaberi)
        {
            vrsta = v;
            izaberiTask = izaberi;
            kontekst = k;
            taskovi = sviTaskovi;
            Text = v == Vrsta.Lokomotive ? "Gantogram zauzetosti lokomotiva" : "Gantogram zauzetosti osoblja";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.Dp(1400), Theme.Dp(720));
            Font = Theme.Tekstualni(9.5f);
            BackColor = Theme.Pozadina;
            dtOd.Value = od.Date;
            dtDo.Value = doDatuma.Date;
            chkSve.Text = v == Vrsta.Lokomotive ? "Sve aktivne lokomotive" : "Sve osobe u planiranju";
            chkSati.Checked = (doDatuma - od).TotalDays <= 3;

            var osvezi = new Button { Text = "Osveži", AutoSize = true };
            osvezi.Click += (s, e) => { var _ = Ucitaj(); };
            chkSve.CheckedChanged += (s, e) => Prikazi();
            chkSati.CheckedChanged += (s, e) => Prikazi();
            dtOd.ValueChanged += (s, e) => Prikazi();
            dtDo.ValueChanged += (s, e) => Prikazi();
            var traka = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(Theme.Dp(8), Theme.Dp(6), Theme.Dp(8), Theme.Dp(4)) };
            foreach (Control c in new Control[] { new Label { Text = "Od", AutoSize = true }, dtOd, new Label { Text = "Do", AutoSize = true }, dtDo, osvezi, chkSve, chkSati, lblStanje })
            {
                c.Margin = new Padding(0, c is Label ? Theme.Dp(6) : Theme.Dp(2), Theme.Dp(8), 0);
                traka.Controls.Add(c);
            }
            ThemeApplier.StilDugmeta(osvezi, false);
            lblStanje.ForeColor = Theme.TekstSporedni;
            lblLegenda.ForeColor = Theme.TekstSporedni;
            lblLegenda.Padding = new Padding(Theme.Dp(8), Theme.Dp(4), Theme.Dp(8), 0);
            lblLegenda.Text = v == Vrsta.Lokomotive
                ? "Puna traka: task (vučna), tamna: task (potisak), isprekidana: posao sa nedeljnog plana bez taska, šrafirana: servis ili namenska. Žut okvir: preklapanje. Dvoklik na task ga bira na ekranu Taskovi."
                : "Zelena: smena, puna traka: zadatak (task ili posao), šrafirana: odsustvo, isprekidana (red „Nepopunjena radna mesta“): radno mesto taska bez osobe. Žut okvir: preklapanje zadataka. Dvoklik na task ga bira na ekranu Taskovi.";
            gantt.DvoklikTraka += t => { if (t.ZapisID.HasValue) izaberiTask?.Invoke(t.ZapisID.Value); };

            Controls.Add(gantt);
            Controls.Add(lblLegenda);
            Controls.Add(traka);
            Shown += (s, e) => { if (v == Vrsta.Osoblje) { var _ = Ucitaj(); } else Prikazi(); };
        }

        private DateTime Od => dtOd.Value.Date;
        private DateTime Do => dtDo.Value.Date.AddDays(1);

        // Osveži: taskovi i plan iznova, a za osoblje i zadaci, smene i odsustva.
        private async Task Ucitaj()
        {
            DateTime od = Od, doDatuma = Do;
            if (doDatuma <= od) { EkranOP.Poruka(this, "Datum Do mora biti posle datuma Od."); return; }
            bool saKontekstom = kontekst == null || osoblje != null || vrsta == Vrsta.Lokomotive;
            PlanKontekst stari = kontekst;
            lblStanje.Text = "Učitavam...";
            await EkranOP.Ucitaj(this, () =>
            {
                PlanKontekst k = saKontekstom ? PlanRepo.Ucitaj(od, doDatuma) : stari;
                return new
                {
                    K = k,
                    T = TaskRepo.ZaPeriod(od, doDatuma, false),
                    O = vrsta == Vrsta.Osoblje ? ZadatakRepo.Kontekst(k, od, doDatuma) : null
                };
            }, d =>
            {
                kontekst = d.K;
                taskovi = d.T;
                osoblje = d.O;
                Prikazi();
            });
        }

        private void Prikazi()
        {
            DateTime od = Od, doDatuma = Do;
            if (doDatuma <= od || kontekst == null || taskovi == null) return;
            var redovi = new List<GanttRed>();
            var trake = new List<GanttTraka>();
            if (vrsta == Vrsta.Lokomotive) Lokomotive(od, doDatuma, redovi, trake);
            else if (osoblje != null) Osoblje(od, doDatuma, redovi, trake);
            else return;
            // Preklapanje u istom redu (osim smene, koja je dostupnost, a ne zauzetost).
            foreach (var g in trake.Where(t => t.Vrsta != VrstaTrake.Dostupnost && t.Vrsta != VrstaTrake.BezLokomotive).GroupBy(t => t.RedKljuc))
            {
                List<GanttTraka> l = g.OrderBy(t => t.Od).ToList();
                for (int i = 0; i < l.Count; i++)
                    for (int j = i + 1; j < l.Count && l[j].Od < l[i].Do; j++)
                    {
                        l[i].Konflikt = true;
                        l[j].Konflikt = true;
                    }
            }
            gantt.Prikazi(od, doDatuma, chkSati.Checked, redovi, trake);
            int konflikti = trake.Count(t => t.Konflikt && redovi.Any(r => r.Kljuc == t.RedKljuc));
            lblStanje.Text = redovi.Count + (vrsta == Vrsta.Lokomotive ? " lokomotiva" : " redova") + " · " + trake.Count(t => t.Do > od && t.Od < doDatuma && redovi.Any(r => r.Kljuc == t.RedKljuc)) + " traka"
                + (konflikti > 0 ? " · preklapanja: " + konflikti : "");
        }

        private string Naziv(int id) { return kontekst.Naziv(id); }

        private static string Vreme(DateTime v) { return v.ToString("dd.MM. HH:mm"); }

        private void Lokomotive(DateTime od, DateTime doDatuma, List<GanttRed> redovi, List<GanttTraka> trake)
        {
            var naTasku = new HashSet<Tuple<int, int>>();
            foreach (TaskOP t in taskovi.Where(x => x.Status != StatusTaska.Storniran && x.Od.HasValue && x.Do.HasValue))
                foreach (TaskLokomotiva l in t.Lokomotive)
                {
                    bool vucna = l.Uloga == TaskLokomotiva.Vucna;
                    foreach (TaskNajavaDeo n in t.Najave.Where(n => n.PosaoID.HasValue)) naTasku.Add(Tuple.Create(l.LokomotivaID, n.PosaoID.Value));
                    trake.Add(new GanttTraka
                    {
                        RedKljuc = l.LokomotivaID, Od = t.Od.Value, Do = t.Do.Value, ZapisID = t.ID,
                        Vrsta = vucna ? VrstaTrake.Posao : VrstaTrake.DvostrukaVuca,
                        Tekst = "T" + t.ID + " " + Naziv(t.OdID) + " – " + Naziv(t.DoID) + (vucna ? "" : " · potisak"),
                        Opis = "Task " + t.ID + " · " + StatusTaska.Naziv(t.Status) + " · " + (vucna ? "vučna" : "potisak") + "\n" + Naziv(t.OdID) + " – " + Naziv(t.DoID)
                            + "\nZauzeta " + Vreme(t.Od.Value) + " – " + Vreme(t.Do.Value) + "\nNajave: " + t.OznakaNajava
                    });
                }
            // Kalendar plana: posao bez taska te lokomotive (nedeljni plan), servis, namenska i ostalo.
            foreach (ZauzetostLok z in kontekst.Kalendar.Where(x => x.Do > od && x.Od < doDatuma))
            {
                bool posao = z.Vrsta == "POSAO";
                if (posao && z.PosaoID.HasValue && naTasku.Contains(Tuple.Create(z.LokomotivaID, z.PosaoID.Value))) continue;
                string tekst = posao ? "Posao " + z.PosaoID + " (plan)" : z.Vrsta == "SERVIS" ? "Servis" : z.Vrsta == "OSTALO" ? z.Napomena ?? "Zauzeta" : "Namenska " + z.Projekat;
                trake.Add(new GanttTraka
                {
                    RedKljuc = z.LokomotivaID, Od = z.Od, Do = z.Do, Vrsta = posao ? VrstaTrake.Nacrt : VrstaTrake.Zauzetost, Tekst = tekst,
                    Opis = tekst + "\n" + Vreme(z.Od) + " – " + Vreme(z.Do) + (string.IsNullOrEmpty(z.Napomena) || !posao ? "" : "\n" + z.Napomena)
                });
            }
            var sa = new HashSet<int>(trake.Where(t => t.Do > od && t.Od < doDatuma).Select(t => t.RedKljuc.Value));
            foreach (LokomotivaOP l in kontekst.Lokomotive.Where(x => chkSve.Checked || sa.Contains(x.ID)).OrderBy(x => x.Vuca == "E" ? 0 : 1).ThenBy(x => x.Broj))
                redovi.Add(new GanttRed { Kljuc = l.ID, Naslov = l.Broj, Podnaslov = (string.IsNullOrEmpty(l.Serija) ? "" : l.Serija + " · ") + (l.Vuca == "E" ? "električna" : l.Vuca == "D" ? "dizel" : "") });
            // Lokomotiva sa taskom koja nije među aktivnim (npr. ugašena posle dodele) dobija red, da se task ne izgubi.
            foreach (int id in sa.Where(id => redovi.All(r => r.Kljuc != id)))
            {
                TaskLokomotiva l = taskovi.SelectMany(t => t.Lokomotive).FirstOrDefault(x => x.LokomotivaID == id);
                redovi.Add(new GanttRed { Kljuc = id, Naslov = l?.Broj ?? "#" + id, Podnaslov = "nije aktivna" });
            }
        }

        private void Osoblje(DateTime od, DateTime doDatuma, List<GanttRed> redovi, List<GanttTraka> trake)
        {
            var aktivniTaskovi = taskovi.Where(x => x.Status != StatusTaska.Storniran).ToDictionary(x => x.ID);
            foreach (ZadatakOsobe z in osoblje.Zadaci.Where(x => x.Status != "ODBIJEN" && x.Status != "OTKAZAN" && x.Do > od && x.Od < doDatuma))
            {
                TaskOP t = null;
                if (z.TaskID.HasValue) aktivniTaskovi.TryGetValue(z.TaskID.Value, out t);
                string gde = t != null ? Naziv(t.OdID) + " – " + Naziv(t.DoID) : z.StanicaID.HasValue ? Naziv(z.StanicaID.Value) : "";
                trake.Add(new GanttTraka
                {
                    RedKljuc = z.DeSifra, Od = z.Od, Do = z.Do, ZapisID = z.TaskID,
                    Vrsta = z.Aktivan ? VrstaTrake.Posao : VrstaTrake.DvostrukaVuca,
                    Tekst = (z.TaskID.HasValue ? "T" + z.TaskID : z.BrojNajave) + " " + TaskOsoba.NazivUloge(z.Uloga) + (z.Aktivan ? "" : " · " + z.Status.ToLowerInvariant()),
                    Opis = TaskOsoba.NazivUloge(z.Uloga) + " · " + (z.TaskID.HasValue ? "task " + z.TaskID : "posao " + z.BrojNajave) + " · zadatak " + z.Status.ToLowerInvariant()
                        + "\n" + gde + "\n" + Vreme(z.Od) + " – " + Vreme(z.Do)
                });
            }
            foreach (SmenaOsobe s in osoblje.Smene.Where(x => x.Od.HasValue && x.Do.HasValue && x.Do > od && x.Od < doDatuma))
                trake.Add(new GanttTraka
                {
                    RedKljuc = s.DeSifra, Od = s.Od.Value, Do = s.Do.Value, Vrsta = VrstaTrake.Dostupnost, Tekst = "Smena " + s.Vrsta.ToLowerInvariant(),
                    Opis = "Smena " + s.Vrsta.ToLowerInvariant() + "\n" + Vreme(s.Od.Value) + " – " + Vreme(s.Do.Value) + (s.LokacijaPosleID.HasValue ? "\nPosle smene: " + Naziv(s.LokacijaPosleID.Value) : "")
                });
            foreach (OdsustvoOsobe o in osoblje.Odsustva.Where(x => x.Do >= od && x.Od < doDatuma))
                trake.Add(new GanttTraka
                {
                    RedKljuc = o.DeSifra, Od = o.Od.Date, Do = o.Do.Date.AddDays(1), Vrsta = VrstaTrake.Zauzetost, Tekst = o.Vrsta,
                    Opis = "Odsustvo: " + o.Vrsta + "\n" + o.Od.ToString("dd.MM.yyyy") + " – " + o.Do.ToString("dd.MM.yyyy")
                });
            // Radna mesta taskova bez izabrane osobe.
            foreach (TaskOP t in aktivniTaskovi.Values)
                foreach (TaskOsoba m in t.Osoblje.Where(x => string.IsNullOrEmpty(x.Osoba) && x.Do > od && x.Od < doDatuma))
                    trake.Add(new GanttTraka
                    {
                        RedKljuc = null, Od = m.Od, Do = m.Do, ZapisID = t.ID, Vrsta = VrstaTrake.BezLokomotive,
                        Tekst = "T" + t.ID + " " + TaskOsoba.NazivUloge(m.Uloga) + " · " + Naziv(m.StanicaID),
                        Opis = "Task " + t.ID + ": " + TaskOsoba.NazivUloge(m.Uloga) + " bez osobe\n" + Naziv(m.StanicaID) + "\n" + Vreme(m.Od) + " – " + Vreme(m.Do)
                    });

            var saZauzetoscu = new HashSet<int>(trake.Where(t => t.RedKljuc.HasValue && t.Vrsta != VrstaTrake.Dostupnost && t.Do > od && t.Od < doDatuma).Select(t => t.RedKljuc.Value));
            var saSmenom = new HashSet<int>(trake.Where(t => t.RedKljuc.HasValue && t.Do > od && t.Od < doDatuma).Select(t => t.RedKljuc.Value));
            Func<OsobaPlan, int> redUloge = o => o.Masinovodja ? 0 : o.MasinovodjaManevra ? 1 : o.Pregledac ? 2 : o.Vozovodja ? 3 : o.Manevrista ? 4 : 5;
            foreach (OsobaPlan o in osoblje.Osobe.Where(x => chkSve.Checked ? x.Aktivan : saSmenom.Contains(x.DeSifra)).OrderBy(redUloge).ThenBy(x => x.Naziv))
                redovi.Add(new GanttRed { Kljuc = o.DeSifra, Naslov = o.Naziv, Podnaslov = Uloge(o) + (saZauzetoscu.Contains(o.DeSifra) ? "" : " · bez zadatka") });
            // Osoba sa zadatkom koja nije u planiranju i dalje dobija red; samo odsustvo ili smena van planiranja se ne prikazuje.
            var saZadatkom = new HashSet<int>(trake.Where(t => t.RedKljuc.HasValue && (t.Vrsta == VrstaTrake.Posao || t.Vrsta == VrstaTrake.DvostrukaVuca) && t.Do > od && t.Od < doDatuma).Select(t => t.RedKljuc.Value));
            foreach (int d in saZadatkom.Where(d => redovi.All(r => r.Kljuc != d)))
            {
                ZadatakOsobe z = osoblje.Zadaci.FirstOrDefault(x => x.DeSifra == d);
                redovi.Add(new GanttRed { Kljuc = d, Naslov = z?.Osoba ?? "#" + d, Podnaslov = "nije u planiranju" });
            }
            if (trake.Any(t => !t.RedKljuc.HasValue))
                redovi.Add(new GanttRed { Kljuc = null, Naslov = "Nepopunjena radna mesta", Podnaslov = "taskovi bez osobe" });
        }

        private static string Uloge(OsobaPlan o)
        {
            var l = new List<string>();
            if (o.Masinovodja) l.Add("mašinovođa");
            if (o.MasinovodjaManevra) l.Add("maš. manevre");
            if (o.Pregledac) l.Add("pregledač");
            if (o.Vozovodja) l.Add("vozovođa");
            if (o.Manevrista) l.Add("manevrista");
            return l.Count == 0 ? "bez uloge" : string.Join(", ", l);
        }
    }
}
