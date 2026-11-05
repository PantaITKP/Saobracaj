using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Kontrole;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPOsoblje : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPOsoblje";
        private static readonly string[] Uloge = { "Masinovodja", "PregledacKola", "Vozovodja", "Manevrista" };

        public bool Pravo { get; }

        private readonly DataGridView tabOsoblje = EkranOP.NovaTabela("MaticnaStanicaID", "Ime", "Prezime");
        private readonly MatricaPanel matricaPruga = new MatricaPanel("Pruga", false);
        private readonly MatricaPanel matricaLokomotiva = new MatricaPanel("Lokomotiva", true);
        private readonly DataGridView tabTurnusi = EkranOP.NovaTabela("ID", "DeSifra", "TurnusID");
        private readonly DataGridView tabOdsustva = EkranOP.NovaTabela();
        private readonly UredjivacReda poljaOsoba = new UredjivacReda("Dopuna za planiranje");
        private readonly UredjivacReda poljaTurnus = new UredjivacReda("Dodela turnusa");
        private readonly UredjivacReda poljaOdsustvo = new UredjivacReda("Odsustva");
        private readonly CheckBox cekMasinovodje = new CheckBox { Text = "Mašinovođe" };
        private readonly CheckBox cekPregledaci = new CheckBox { Text = "Pregledači" };
        private readonly CheckBox cekVozovodje = new CheckBox { Text = "Vozovođe" };
        private readonly CheckBox cekManevristi = new CheckBox { Text = "Manevristi" };
        private readonly DateTimePicker dtPregledOd = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy", Value = DateTime.Today.AddDays(-30) };
        private readonly DateTimePicker dtPregledDo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy", Value = DateTime.Today.AddDays(60) };
        private byte[] planRowVer;

        public frmOPOsoblje()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private int? IzabranaOsoba
        {
            get
            {
                DataRow r = EkranOP.Izabran(tabOsoblje);
                return r == null ? (int?)null : Convert.ToInt32(r["DeSifra"]);
            }
        }

        private async Task Osvezi()
        {
            int? izabrana = IzabranaOsoba;
            await EkranOP.Ucitaj(this, () => new
            {
                Osobe = OsobljeRepo.Svi(false),
                Stanice = StaniceRepo.ZaIzborSrb(),
                Turnusi = TurnusiRepo.Svi(),
                Razlozi = OdsustvaRepo.Razlozi(),
                Zaposleni = OdsustvaRepo.Zaposleni(),
                Pruge = KvalifikacijeRepo.Pruge(),
                Lokomotive = KvalifikacijeRepo.Lokomotive()
            }, p =>
            {
                poljaOsoba.IzvorIzbora("MaticnaStanicaID", p.Stanice, "Naziv", "ID", true);
                poljaTurnus.IzvorIzbora("TurnusID", p.Turnusi, "Naziv", "ID", false);
                poljaOdsustvo.IzvorIzbora("DeSifra", p.Zaposleni, "Naziv", "DeSifra", true);
                poljaOdsustvo.IzvorIzbora("RazlogID", p.Razlozi, "Naziv", "ID", true);
                poljaOdsustvo.IzvorIzbora("OdobrioDeSifra", p.Zaposleni.Copy(), "Naziv", "DeSifra", true);
                tabOsoblje.DataSource = p.Osobe;
                IzaberiOsobu(izabrana);
                // Neupisane izmene u matrici se ne gaze osvežavanjem drugog taba.
                if (!matricaPruga.ImaIzmena) matricaPruga.Postavi(p.Pruge.Radnici, p.Pruge.Redovi, p.Pruge.Stavke);
                if (!matricaLokomotiva.ImaIzmena) matricaLokomotiva.Postavi(p.Lokomotive.Radnici, p.Lokomotive.Redovi, p.Lokomotive.Stavke);
            });
            await OsveziOdsustva();
        }

        private void IzaberiOsobu(int? sifra)
        {
            if (!sifra.HasValue) return;
            foreach (DataGridViewRow r in tabOsoblje.Rows)
                if (r.Visible && Convert.ToInt32(r.Cells["DeSifra"].Value) == sifra.Value)
                {
                    tabOsoblje.CurrentCell = r.Cells["Naziv"];
                    return;
                }
        }

        private async Task PrikaziOsobu()
        {
            DataRow r = EkranOP.Izabran(tabOsoblje);
            poljaOsoba.Popuni(r);
            if (r == null) return;
            int d = Convert.ToInt32(r["DeSifra"]);
            await EkranOP.Ucitaj(this, () => new
            {
                Rv = OsobljeRepo.PlanRowVer(d),
                Tur = TurnusiRepo.DodeleOsobe(d)
            }, p =>
            {
                if (IzabranaOsoba != d) return;
                planRowVer = p.Rv;
                tabTurnusi.DataSource = p.Tur;
            });
        }

        private static bool Bit(DataRow r, string kolona, DataRowVersion verzija = DataRowVersion.Current)
        {
            object v = r[kolona, verzija];
            return v != DBNull.Value && Convert.ToBoolean(v);
        }

        private List<IzmenaUloga> IzmeneUloga()
        {
            tabOsoblje.EndEdit();
            var t = tabOsoblje.DataSource as DataTable;
            if (t == null) return new List<IzmenaUloga>();
            return t.Rows.Cast<DataRow>()
                .Where(r => r.RowState == DataRowState.Modified && Uloge.Any(u => Bit(r, u) != Bit(r, u, DataRowVersion.Original)))
                .Select(r => new IzmenaUloga
                {
                    DeSifra = Convert.ToInt32(r["DeSifra"]),
                    Naziv = Convert.ToString(r["Naziv"]),
                    Masinovodja = Bit(r, "Masinovodja"),
                    PregledacKola = Bit(r, "PregledacKola"),
                    Vozovodja = Bit(r, "Vozovodja"),
                    Manevrista = Bit(r, "Manevrista"),
                    StaroMasinovodja = Bit(r, "Masinovodja", DataRowVersion.Original),
                    StaroPregledacKola = Bit(r, "PregledacKola", DataRowVersion.Original),
                    StaroVozovodja = Bit(r, "Vozovodja", DataRowVersion.Original),
                    StaroManevrista = Bit(r, "Manevrista", DataRowVersion.Original)
                }).ToList();
        }

        private void SacuvajOsobe()
        {
            List<IzmenaUloga> uloge = IzmeneUloga();
            int? d = IzabranaOsoba;
            int? matica = poljaOsoba.IzabranID("MaticnaStanicaID");
            var t = (DataTable)tabOsoblje.DataSource;

            // Radnik sa ulogom mora imati matičnu stanicu; proveravaju se izmenjeni i izabrani radnik.
            var bezStanice = new List<string>();
            foreach (DataRow r in t.Rows.Cast<DataRow>().Where(x => uloge.Any(u => u.DeSifra == Convert.ToInt32(x["DeSifra"])) || Convert.ToInt32(x["DeSifra"]) == d))
            {
                bool izabran = Convert.ToInt32(r["DeSifra"]) == d;
                if (Uloge.Any(u => Bit(r, u)) && (izabran ? !matica.HasValue : r.IsNull("MaticnaStanicaID")))
                    bezStanice.Add(Convert.ToString(r["Naziv"]));
            }
            if (bezStanice.Count > 0)
            {
                EkranOP.Poruka(this, "Radnik sa ulogom mora imati matičnu stanicu. Izaberite radnika i upišite stanicu:\n\n" + string.Join("\n", bezStanice.Take(15)));
                return;
            }
            if (d.HasValue && poljaOsoba.Cek("SamoDomicilna") && !matica.HasValue)
            {
                EkranOP.Poruka(this, "Za „Samo domicilna“ izaberite matičnu stanicu.");
                return;
            }
            if (EkranOP.Izvrsi(this, () => OsobljeRepo.SacuvajOsobe(uloge, d, planRowVer, matica, poljaOsoba.Cek("UPlaniranju"), poljaOsoba.Cek("Auto"), poljaOsoba.Cek("SamoDomicilna"), poljaOsoba.TekstIliNull("Napomena"))))
            {
                var _ = Osvezi();
            }
        }

        private async Task SacuvajMatrice()
        {
            IzmeneMatrice pruge = matricaPruga.Izmene(), lokomotive = matricaLokomotiva.Izmene();
            if (!pruge.Ima && !lokomotive.Ima) { EkranOP.Poruka(this, "Nema izmena."); return; }
            bool upisano = false;
            await EkranOP.Ucitaj(this, () =>
            {
                KvalifikacijeRepo.SacuvajPruge(pruge);
                KvalifikacijeRepo.SacuvajLokomotive(lokomotive);
                return new { Pruge = KvalifikacijeRepo.Pruge(), Lokomotive = KvalifikacijeRepo.Lokomotive() };
            }, p =>
            {
                matricaPruga.Postavi(p.Pruge.Radnici, p.Pruge.Redovi, p.Pruge.Stavke);
                matricaLokomotiva.Postavi(p.Lokomotive.Radnici, p.Lokomotive.Redovi, p.Lokomotive.Stavke);
                upisano = true;
            });
            if (upisano) EkranOP.Poruka(this, "Kvalifikacije su sačuvane.");
        }

        private async Task OsveziMatrice()
        {
            if ((matricaPruga.ImaIzmena || matricaLokomotiva.ImaIzmena) && !EkranOP.Potvrdi(this, "Odbaciti nesačuvane izmene u matrici?")) return;
            await EkranOP.Ucitaj(this, () => new { Pruge = KvalifikacijeRepo.Pruge(), Lokomotive = KvalifikacijeRepo.Lokomotive() }, p =>
            {
                matricaPruga.Postavi(p.Pruge.Radnici, p.Pruge.Redovi, p.Pruge.Stavke);
                matricaLokomotiva.Postavi(p.Lokomotive.Radnici, p.Lokomotive.Redovi, p.Lokomotive.Stavke);
            });
        }

        private bool ImaNesacuvano()
        {
            return IzmeneUloga().Count > 0 || matricaPruga.ImaIzmena || matricaLokomotiva.ImaIzmena;
        }

        private void DodeliTurnus()
        {
            int? d = IzabranaOsoba, t = poljaTurnus.IzabranID("TurnusID");
            if (!d.HasValue || !t.HasValue) { EkranOP.Poruka(this, "Izaberite osobu i turnus."); return; }
            if (EkranOP.Izvrsi(this, () => TurnusiRepo.Dodeli(d.Value, t.Value, poljaTurnus.DatumVreme("Od").Date, poljaTurnus.DatumIliNull("Do"))))
            {
                var _ = PrikaziOsobu();
            }
        }

        private async Task OsveziOdsustva()
        {
            DateTime od = dtPregledOd.Value.Date, doDan = dtPregledDo.Value.Date;
            bool m = cekMasinovodje.Checked, p = cekPregledaci.Checked, v = cekVozovodje.Checked, a = cekManevristi.Checked;
            await EkranOP.Ucitaj(this, () => OdsustvaRepo.Pregled(od, doDan, m, p, v, a), t => tabOdsustva.DataSource = t);
        }

        private void DodajOdsustvo()
        {
            int? d = poljaOdsustvo.IzabranID("DeSifra"), razlog = poljaOdsustvo.IzabranID("RazlogID"), odobrio = poljaOdsustvo.IzabranID("OdobrioDeSifra");
            DateTime od = poljaOdsustvo.DatumVreme("Od").Date, doDan = poljaOdsustvo.DatumVreme("Do").Date;
            if (!d.HasValue || !razlog.HasValue || !odobrio.HasValue) { EkranOP.Poruka(this, "Izaberite zaposlenog, razlog i ko je odobrio."); return; }
            if (doDan < od) { EkranOP.Poruka(this, "Kraj odsustva ne može biti pre početka."); return; }
            string napomena = poljaOdsustvo.TekstIliNull("Napomena");
            if (EkranOP.Izvrsi(this, () => OdsustvaRepo.DodajRucno(d.Value, razlog.Value, odobrio.Value, od, doDan, napomena)))
            {
                var _ = OsveziOdsustva();
            }
        }
    }
}
