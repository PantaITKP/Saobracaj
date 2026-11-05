using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTokPosla : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPTokPosla";
        public bool Pravo { get; }
        public int? PosaoId { get; private set; }

        private readonly ComboBox cboPosao = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Panel panelKoraka = new Panel();
        private readonly DataGridView tabDogadjaji = EkranOP.NovaTabela("ID", "PosaoID", "PodaciJson");
        private readonly Label lblSledeci = new Label();
        private readonly Label lblVreme = new Label();

        private PosaoPlan posao;
        private bool punjenje;

        public frmOPTokPosla(int? posaoId = null)
        {
            PosaoId = posaoId;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task UcitajPoslove()
        {
            await EkranOP.Ucitaj(this, () => TokPoslaRepo.Aktivni(), t =>
            {
                punjenje = true;
                cboPosao.DataSource = t;
                cboPosao.DisplayMember = "Naziv";
                cboPosao.ValueMember = "ID";
                if (PosaoId.HasValue) cboPosao.SelectedValue = PosaoId.Value;
                else if (t.Rows.Count > 0) PosaoId = Convert.ToInt32(cboPosao.SelectedValue);
                punjenje = false;
                var _ = Osvezi();
            });
        }

        private sealed class Podaci
        {
            internal PosaoPlan Posao;
            internal RezultatPosla Racun;
            internal DataTable Dogadjaji;
            internal List<ZadatakOsobe> Zadaci;
            internal PlanKontekst Plan;
        }

        // Pri svakom prikazu faze 5 i 7 se usklađuju čitanjem iz Dispečera (pravilo 8.10).
        private static Podaci Pripremi(int id)
        {
            TokPoslaRepo.Uskladi(id);
            PosaoPlan p = PlanRepo.Posao(id);
            if (p == null) throw new InvalidOperationException("Posao više ne postoji ili nema termin prijema.");
            PlanKontekst k = PlanRepo.Ucitaj(p.Prijem.Date, p.Prijem.Date.AddDays(7));
            return new Podaci { Posao = p, Plan = k, Racun = PlanRacun.Izracunaj(k, p), Dogadjaji = DogadjajRepo.ZaPosao(id), Zadaci = ZadatakRepo.ZaPosao(id) };
        }

        private async Task Osvezi()
        {
            if (!PosaoId.HasValue) return;
            int id = PosaoId.Value;
            await EkranOP.Ucitaj(this, () => Pripremi(id), Prikazi);
        }

        private void Prikazi(Podaci d)
        {
            posao = d.Posao;
            tabDogadjaji.DataSource = d.Dogadjaji;
            panelKoraka.Invalidate();
            var sledeci = new List<string>();
            if (posao.Faza <= 2) sledeci.Add("Potvrda lokomotiva (Planer).");
            int nePrihvaceni = d.Zadaci.Count(z => z.Status == "POSLAT");
            if (posao.Faza == 3 && d.Zadaci.Count(z => z.Aktivan) == 0) sledeci.Add("Slanje zadatka osoblju (mašinski dispečer).");
            if (nePrihvaceni > 0) sledeci.Add("Prihvatanje zadatka: čeka se " + nePrihvaceni + ".");
            if (d.Racun.MasaIzNajave) sledeci.Add("Teretnica: masa je još iz najave.");
            if (posao.Faza == 4) sledeci.Add("Potvrda prijema i pregleda.");
            if (posao.Faza == 5) sledeci.Add("Dolazak voza iz Dispečera.");
            if (posao.Faza == 6) sledeci.Add("Završetak voza u Dispečeru.");
            lblSledeci.Text = sledeci.Count == 0 ? "Nema otvorenih koraka." : string.Join("\n", sledeci);

            int prijem = (int)d.Plan.Parametar("OBRADA_PRIJEM_MIN", posao.Prijem, posao.PrijemID, null, null, 60);
            int predaja = (int)d.Plan.Parametar("OBRADA_PREDAJA_MIN", posao.Prijem, posao.PredajaID, null, null, 60);
            lblVreme.Text = "Obrada pri prijemu: " + prijem + " min\nVožnja: " + (int)(d.Racun.Dolazak - d.Racun.Polazak).TotalMinutes + " min\nObrada pri predaji: " + predaja
                + " min\nOčekivana predaja: " + d.Racun.Predaja.ToString("dd.MM. HH:mm");
        }

        private async Task PrijemZavrsen()
        {
            if (posao == null) return;
            if (posao.Faza != 4) { EkranOP.Poruka(this, "Ovaj korak se potvrđuje u fazi 4."); return; }
            PosaoPlan p = posao;
            await EkranOP.Ucitaj(this, () =>
            {
                if (TokPoslaRepo.VozPokrenut(p.NajavaID)) return false;
                TokPoslaRepo.PromeniFazu(p.ID, p.RowVer, 4, 5, "PRIJEM_ZAVRSEN", "Prijem i pregled su završeni.");
                return true;
            }, promenjeno =>
            {
                if (!promenjeno) EkranOP.Poruka(this, "Voz je već pokrenut u Dispečeru; faza dolazi odatle.");
                var _ = Osvezi();
            });
        }

        private async Task PredajaUToku()
        {
            if (posao == null) return;
            if (posao.Faza != 5) { EkranOP.Poruka(this, "Predaja počinje posle faze 5."); return; }
            PosaoPlan p = posao;
            await EkranOP.Ucitaj(this, () => { TokPoslaRepo.PromeniFazu(p.ID, p.RowVer, 5, 6, "PREDAJA_U_TOKU", "Predaja je u toku."); return true; }, _ => { var __ = Osvezi(); });
        }
    }
}
