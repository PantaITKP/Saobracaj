using System;

namespace Saobracaj.Pepeo
{
    public class PepeoSablonOperacije
    {
        public int SablonOperacijeID { get; set; }
        public int RedosledKoraka { get; set; }
        public string SifraKoraka { get; set; }
        public string NazivKoraka { get; set; }
        public int? StanicaID { get; set; }
        public string StanicaOpis { get; set; }
        public bool UlazakUStanicu { get; set; }
        public string TipPravila { get; set; }
        public int TrajanjeMinuta { get; set; }
        public string Napomena { get; set; }
    }

    public class PepeoKorakVremena
    {
        public int ID { get; set; }
        public int IDTrase { get; set; }
        public int Najava { get; set; }
        public int Ciklus { get; set; }
        public int RB { get; set; }
        public string SifraKoraka { get; set; }
        public int? Stanica { get; set; }
        public string StanicaOpis { get; set; }
        public string Operacija { get; set; }
        public bool UlazakUStanicu { get; set; }
        public DateTime PlaniraniPocetak { get; set; }
        public DateTime PlaniranoVreme { get; set; }
        public DateTime? StvarnoVreme { get; set; }
        public bool RucnoIzmenjeno { get; set; }
        public int KasnjenjeMin { get; set; }
        public string Napomena { get; set; }
    }
}
