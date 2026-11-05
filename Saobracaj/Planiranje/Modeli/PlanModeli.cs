using System;
using System.Collections.Generic;

namespace Saobracaj.Planiranje.Modeli
{
    internal sealed class VezaMreze
    {
        internal int ID;
        internal int A;
        internal int B;
        internal decimal Km;
        internal int? PrugaID;
        internal bool? Elektrificirana;
        internal string KategorijaPruge;
        internal decimal? OsovinskoT;
        internal int? Vmax;
        internal bool? Dvokolosecna;
        internal decimal? MerodavniUspon;
        internal decimal? MerodavniPad;
        internal bool KiM;
    }

    internal sealed class DeonicaRute
    {
        internal int RB;
        internal int VezaID;
        internal int OdID;
        internal int DoID;
        internal int? PrugaID;
        internal decimal Km;
        internal bool? Elektrificirana;
        internal int? TrasaID;
        internal string Stanje = "NEMAMO_TRASU";
        internal string Napomena;
    }

    internal sealed class TrasaOP
    {
        internal int ID;
        internal string BrojVoza;
        internal int? PocetnaID;
        internal int? KrajnjaID;
        internal TimeSpan? Polazak;
        internal TimeSpan? Dolazak;
        internal int DanPlus;
        internal decimal? MaxMasaT;
        internal decimal? MaxDuzinaM;
        internal bool AdHoc;
        internal string AdHocStatus;
        internal int? AdHocNajavaID;
        internal DateTime? VaziOd;
        internal DateTime? VaziDo;
    }

    internal sealed class TelegramOP
    {
        internal int ID;
        internal string Broj;
        internal int? PrugaID;
        internal int? OdID;
        internal int? DoID;
        internal string Vrsta;
        internal string Kolosek;
        internal DateTime VaziOd;
        internal DateTime? VaziDo;
        internal TimeSpan? TrajeOd;
        internal TimeSpan? TrajeDo;
        internal string Napomena;
    }

    internal sealed class MestoPruge
    {
        internal int PrugaID;
        internal int StanicaID;
        internal int RB;
        internal int? MaxDuzinaAB;
        internal int? MaxDuzinaBA;
        internal string Klasa;
    }

    internal sealed class RedMase
    {
        internal int SerijaID;
        internal int? PrugaID;
        internal int? StanicaOdID;
        internal int? StanicaDoID;
        internal string Smer;
        internal int BrojLokomotiva;
        internal string Sloj;
        internal decimal MaxMasaT;
        internal DateTime? VaziOd;
        internal DateTime? VaziDo;
    }

    internal sealed class LokomotivaOP
    {
        internal int ID;
        internal string Broj;
        internal int? SerijaID;
        internal string Serija;
        internal string Vuca;
        internal decimal? MasaT;
        internal bool ImaGPS;
        internal bool Zakup;
        internal int? MaticnaStanicaID;
    }

    internal sealed class ZauzetostLok
    {
        internal int LokomotivaID;
        internal string Vrsta;
        internal int? PosaoID;
        internal DateTime Od;
        internal DateTime Do;
        internal string Projekat;
        internal string Napomena;
        internal int? PredajaStanicaID;
        internal int RB = 1;
    }

    internal sealed class PosaoPlan
    {
        internal int ID;
        internal int NajavaID;
        internal string BrojNajave;
        internal string Otpravna;
        internal string Uputna;
        internal int? PrijemID;
        internal int? PredajaID;
        internal DateTime Prijem;
        internal DateTime? PredvidjenaPredaja;
        internal DateTime? PlaniraniPolazak;
        internal DateTime? PlaniraniDolazak;
        internal DateTime? PlaniranaPredaja;
        internal decimal? BrutoNajava;
        internal decimal? BrutoTeretnica;
        internal decimal? DuzinaM;
        internal int? BrojKola;
        internal string TipKola;
        internal string StatusNajave;
        internal string Projekat;
        internal int Faza;
        internal byte[] RowVer;
        internal string Relacija { get { return Otpravna + " – " + Uputna; } }
    }

    internal enum NivoNalaza { Konflikt, Upozorenje, Informacija }

    internal sealed class Nalaz
    {
        internal NivoNalaza Nivo;
        internal string Naslov;
        internal string Opis;
        internal int? PosaoID;

        internal Nalaz(NivoNalaza nivo, string naslov, string opis, int? posaoId = null)
        {
            Nivo = nivo;
            Naslov = naslov;
            Opis = opis;
            PosaoID = posaoId;
        }
    }

    internal sealed class ProveraStavka
    {
        internal int RB;
        internal string Naziv;
        internal bool Ok;
        internal string Tekst;
    }

    internal sealed class MasaZaSeriju
    {
        internal int? SerijaID;
        internal decimal Bruto;
        internal decimal? MaxJedna;
        internal decimal? MaxDve;
        internal int Broj;
        internal bool DveBezTablice;
        internal bool BezTablice;
        internal string Tekst;
    }

    internal sealed class KandidatLokomotiva
    {
        internal List<LokomotivaOP> Lokomotive = new List<LokomotivaOP>();
        internal string Lokacija;
        internal int? LokacijaID;
        internal string IzvorLokacije;
        internal int PraznaVoznjaMin;
        internal bool LokacijaNepoznata;
        internal decimal Ocena;
        internal string Razlog;
        internal string Oznaka
        {
            get
            {
                var brojevi = new List<string>();
                foreach (LokomotivaOP l in Lokomotive) brojevi.Add(l.Broj);
                return string.Join(" + ", brojevi);
            }
        }
    }

    // Izračunat posao: ruta, vremena, masa, provere i nalazi.
    internal sealed class RezultatPosla
    {
        internal PosaoPlan Posao;
        internal List<DeonicaRute> Ruta = new List<DeonicaRute>();
        internal bool ZatvorBezAlternative;
        internal DateTime Polazak;
        internal DateTime Dolazak;
        internal DateTime Predaja;
        internal int? TrasaID;
        internal decimal Bruto;
        internal bool MasaIzNajave;
        internal List<Nalaz> Nalazi = new List<Nalaz>();
        internal List<ProveraStavka> Provere = new List<ProveraStavka>();
        // Telegrami koji utiču na rutu: zatvor koji se obilazi ili prolazi, i upozorenje na ruti.
        internal List<TelegramOP> Telegrami = new List<TelegramOP>();
        // Bruto tonski kilometri.
        internal decimal Btto { get { return Km * Bruto; } }
        internal decimal Km
        {
            get
            {
                decimal z = 0;
                foreach (DeonicaRute d in Ruta) z += d.Km;
                return z;
            }
        }
    }
}
