using System;

namespace Saobracaj.Pepeo
{
    public class PepeoOperacijaDefinicija
    {
        public int OperacijaID { get; set; }
        public int Redosled { get; set; }
        public string Sifra { get; set; }
        public string Naziv { get; set; }
        public string Drzava { get; set; }
        public string TipPravila { get; set; }
        public int TrajanjeMinuta { get; set; }
    }

    public class PepeoCiklusInfo
    {
        public int CiklusID { get; set; }
        public int TrasaID { get; set; }
        public string TrasaNaziv { get; set; }
        public int? NajavaID { get; set; }
        public DateTime PocetakCiklusa { get; set; }
        public DateTime KrajCiklusaPlan { get; set; }
        public DateTime? KrajCiklusaStvarno { get; set; }
        public string StatusCiklusa { get; set; }

        public string TekstZaCombo
        {
            get
            {
                string status = string.IsNullOrWhiteSpace(StatusCiklusa) ? "" : " - " + StatusCiklusa;
                return "C" + CiklusID.ToString() + status;
            }
        }
    }

    public class PepeoKorakVremena
    {
        public int ID { get; set; }
        public int CiklusID { get; set; }
        public int TrasaID { get; set; }
        public string TrasaNaziv { get; set; }
        public int? NajavaID { get; set; }
        public string StatusCiklusa { get; set; }

        public int Redosled { get; set; }
        public int OperacijaID { get; set; }
        public string SifraOperacije { get; set; }
        public string Operacija { get; set; }
        public string Drzava { get; set; }
        public string TipPravila { get; set; }
        public int TrajanjeMinuta { get; set; }

        public DateTime PlaniraniPocetak { get; set; }
        public DateTime PlaniraniKraj { get; set; }
        public DateTime? StvarniPocetak { get; set; }
        public DateTime? StvarniKraj { get; set; }
        public int OdstupanjeMinuta { get; set; }
        public int? RazlogKasnjenjaID { get; set; }
        public string RazlogOdstupanja { get; set; }
        public string Partner { get; set; }
        public string Napomena { get; set; }

        public bool ImaOdstupanje
        {
            get { return StvarniKraj.HasValue && OdstupanjeMinuta != 0; }
        }

        public bool Kasni
        {
            get { return ImaOdstupanje && OdstupanjeMinuta > 0; }
        }

        public bool Ranije
        {
            get { return ImaOdstupanje && OdstupanjeMinuta < 0; }
        }
    }
}
