using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTablicaMasa : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPTablicaMasa";
        public bool Pravo { get; }

        private readonly DataGridView tabela = EkranOP.NovaTabela("ID", "SerijaID", "PrugaID", "StanicaOdID", "StanicaDoID");
        private readonly UredjivacReda polja = new UredjivacReda("Red tablice");
        private DataTable serije, pruge, stanice;

        public frmOPTablicaMasa()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new
            {
                Tab = TablicaMasaRepo.Sve(),
                Ser = LokomotiveRepo.Serije(),
                Pru = RegistarRepo.PrugeZaIzbor(),
                Sta = StaniceRepo.ZaIzbor()
            }, p =>
            {
                serije = p.Ser; pruge = p.Pru; stanice = p.Sta;
                polja.IzvorIzbora("SerijaID", p.Ser, "Oznaka", "ID", false);
                polja.IzvorIzbora("PrugaID", p.Pru, "Naziv", "ID", true);
                polja.IzvorIzbora("StanicaOdID", p.Sta, "Naziv", "ID", true);
                polja.IzvorIzbora("StanicaDoID", p.Sta, "Naziv", "ID", true);
                tabela.DataSource = p.Tab;
            });
        }

        private void Sacuvaj()
        {
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                var red = new RedTabliceMasa
                {
                    SerijaID = polja.IzabranID("SerijaID") ?? throw new FormatException("Izaberite seriju."),
                    PrugaID = polja.IzabranID("PrugaID"),
                    StanicaOdID = polja.IzabranID("StanicaOdID"),
                    StanicaDoID = polja.IzabranID("StanicaDoID"),
                    Smer = polja.TekstIliNull("Smer"),
                    BrojLokomotiva = polja.CeoBroj("BrojLokomotiva"),
                    Sloj = polja.Tekst("Sloj").Length == 0 ? "RUCNA" : polja.Tekst("Sloj"),
                    MaxMasaT = polja.Decimal("MaxMasaT"),
                    VaziOd = polja.DatumIliNull("VaziOd"),
                    VaziDo = polja.DatumIliNull("VaziDo"),
                    Napomena = polja.TekstIliNull("Napomena")
                };
                Proveri(red);
                if (polja.ID.HasValue) TablicaMasaRepo.Sacuvaj(polja.ID.Value, polja.RowVer, red);
                else TablicaMasaRepo.Dodaj(red);
            });
            if (uspeh) { var _ = Osvezi(); }
        }

        private static void Proveri(RedTabliceMasa r)
        {
            if (r.MaxMasaT <= 0) throw new FormatException("Najveća masa mora biti veća od nule.");
            if (r.BrojLokomotiva < 1) throw new FormatException("Broj lokomotiva mora biti bar 1.");
            if (!r.PrugaID.HasValue && !(r.StanicaOdID.HasValue && r.StanicaDoID.HasValue))
                throw new FormatException("Izaberite prugu ili obe stanice relacije.");
        }

        private void Obrisi()
        {
            if (!polja.ID.HasValue || !EkranOP.Potvrdi(this, "Obrisati izabrani red?")) return;
            if (EkranOP.Izvrsi(this, () => TablicaMasaRepo.Obrisi(polja.ID.Value, polja.RowVer)))
            {
                polja.Popuni(null);
                var _ = Osvezi();
            }
        }

        // Kolone po nazivu iz prvog reda: Serija, Pruga ili Od/Do, Smer, Sloj, MaxMasa, BrojLok.
        private void Uvezi()
        {
            if (serije == null) return;
            string putanja;
            using (var d = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                putanja = d.FileName;
            }
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                List<RedTabliceMasa> redovi = Citaj(putanja);
                if (!EkranOP.Potvrdi(this, "Uvesti redova: " + redovi.Count + "?")) return;
                TablicaMasaRepo.UveziSve(redovi);
            });
            if (uspeh) { var _ = Osvezi(); }
        }

        private List<RedTabliceMasa> Citaj(string putanja)
        {
            XlsxList list;
            using (var s = new MemoryStream(XlsxCitac.ProcitajBajtove(putanja))) list = XlsxCitac.Procitaj(s);
            Dictionary<int, string> zaglavlje;
            if (!list.Redovi.TryGetValue(1, out zaglavlje)) throw new InvalidDataException("U prvom redu moraju biti nazivi kolona.");
            var kolone = zaglavlje.ToDictionary(k => NaziviStanica.Normalizuj(k.Value), k => k.Key);
            Func<int, string, string> v = (r, naziv) =>
            {
                int k;
                return kolone.TryGetValue(naziv, out k) ? list.Celija(r, k) : "";
            };
            var rezultat = new List<RedTabliceMasa>();
            var greske = new List<string>();
            for (int r = 2; r <= list.PoslednjiRed; r++)
            {
                if (!list.Redovi.ContainsKey(r)) continue;
                try
                {
                    var red = new RedTabliceMasa
                    {
                        SerijaID = NadjiID(serije, "Oznaka", v(r, "serija")) ?? throw new FormatException("nepoznata serija „" + v(r, "serija") + "“"),
                        PrugaID = NadjiID(pruge, "Naziv", v(r, "pruga")),
                        StanicaOdID = NadjiID(stanice, "Naziv", v(r, "od")),
                        StanicaDoID = NadjiID(stanice, "Naziv", v(r, "do")),
                        Smer = v(r, "smer").Length == 0 ? null : v(r, "smer").ToUpperInvariant(),
                        Sloj = v(r, "sloj").Length == 0 ? "RUCNA" : v(r, "sloj").ToUpperInvariant(),
                        BrojLokomotiva = v(r, "brojlok").Length == 0 ? 1 : int.Parse(v(r, "brojlok"), CultureInfo.InvariantCulture),
                        MaxMasaT = decimal.Parse(v(r, "maxmasa").Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture)
                    };
                    Proveri(red);
                    rezultat.Add(red);
                }
                catch (Exception ex)
                {
                    greske.Add("Red " + r + ": " + ex.Message);
                }
            }
            if (greske.Count > 0)
                throw new InvalidDataException("Uvoz nije urađen. Greške:\n" + string.Join("\n", greske.Take(15)));
            return rezultat;
        }

        private static int? NadjiID(DataTable t, string kolona, string vrednost)
        {
            if (string.IsNullOrWhiteSpace(vrednost)) return null;
            string trazi = NaziviStanica.Normalizuj(vrednost);
            DataRow r = t.Rows.Cast<DataRow>().FirstOrDefault(x => NaziviStanica.Normalizuj(Convert.ToString(x[kolona])) == trazi);
            if (r == null) throw new FormatException("nije pronađeno „" + vrednost + "“");
            return Convert.ToInt32(r["ID"]);
        }
    }
}
