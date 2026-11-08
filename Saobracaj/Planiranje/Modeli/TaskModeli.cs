using System;
using System.Collections.Generic;
using System.Linq;

namespace Saobracaj.Planiranje.Modeli
{
    internal static class StatusTaska
    {
        internal const string Otvoren = "OTVOREN";
        internal const string Lansiran = "LANSIRAN";
        internal const string Zavrsen = "ZAVRSEN";
        internal const string Storniran = "STORNIRAN";

        internal static string Naziv(string s)
        {
            switch (s)
            {
                case Otvoren: return "Otvoren";
                case Lansiran: return "Lansiran";
                case Zavrsen: return "Završen";
                case Storniran: return "Storniran";
                default: return s;
            }
        }
    }

    internal static class NastanakTaska
    {
        internal const string Ruta = "RUTA";
        internal const string SplitRute = "SPLIT_RUTE";
        internal const string SplitBruto = "SPLIT_BRUTO";
        internal const string Spajanje = "SPAJANJE";

        internal static string Naziv(string s)
        {
            switch (s)
            {
                case Ruta: return "Izbor rute";
                case SplitRute: return "Split rute";
                case SplitBruto: return "Split bruto";
                case Spajanje: return "Spajanje";
                default: return s;
            }
        }
    }

    // Stanica rute taska; Km je od početka taska, a pruga i trasa važe za deonicu koja se završava ovde.
    internal sealed class TaskStanica
    {
        internal int StanicaID;
        internal decimal Km;
        internal int? PrugaID;
        internal int? TrasaID;
    }

    // Deo najave koji task vozi i gde je task na ruti te najave.
    internal sealed class TaskNajavaDeo
    {
        internal int NajavaID;
        internal int? PosaoID;
        internal string BrojNajave;
        internal decimal KmOd;
        internal decimal KmDo;
        internal int RBDela = 1;
        internal decimal BrutoT;
        internal int BrojKola;
        // Iz OP_vTaskNajava, samo za prikaz redosleda.
        internal int? DeoRute;
        internal int? DelovaBruto;

        internal TaskNajavaDeo Kopija()
        {
            var k = (TaskNajavaDeo)MemberwiseClone();
            k.DeoRute = null;
            k.DelovaBruto = null;
            return k;
        }
    }

    internal sealed class TaskLokomotiva
    {
        internal const string Vucna = "VUCNA";
        internal const string Potisak = "POTISAK";

        internal int LokomotivaID;
        internal string Broj;
        internal string Uloga = Vucna;
    }

    // Radno mesto taska (OP_TaskOsoblje): ko treba, u kojoj stanici i kada; osoba se bira u Ponudi osoblja.
    internal sealed class TaskOsoba
    {
        internal int ID;
        internal int TaskID;
        internal string Uloga;
        internal int StanicaID;
        internal DateTime Od;
        internal DateTime Do;
        internal string Napomena;
        internal byte[] RowVer;
        // Aktivan zadatak (POSLAT/PRIHVACEN) ili poslednji, za prikaz.
        internal string Osoba;
        internal string StatusZadatka;

        // Redosled u izboru radnog mesta na ekranu Taskovi.
        internal static readonly string[] Uloge = { "MASINOVODJA", "PREGLEDAC_PRIJEM", "PREGLEDAC_PREDAJA", "MANEVRISTA", "VOZOVODJA", "MASINOVODJA_MANEVRA", "OSTALO" };

        internal static string NazivUloge(string u)
        {
            switch (u)
            {
                case "MASINOVODJA": return "Mašinovođa";
                case "PREGLEDAC_PRIJEM": return "Pregledač, prijem";
                case "PREGLEDAC_PREDAJA": return "Pregledač, predaja";
                case "MANEVRISTA": return "Manevrista";
                case "VOZOVODJA": return "Vozovođa";
                case "MASINOVODJA_MANEVRA": return "Mašinovođa na manevri";
                case "OSTALO": return "Ostali";
                default: return u;
            }
        }
    }

    internal sealed class TaskOP
    {
        internal int ID;
        internal decimal Km;
        internal decimal BrutoT;
        internal int BrojKola;
        internal decimal? DuzinaM;
        internal string TipKola;
        internal DateTime? ETD;
        internal DateTime? ATD;
        internal DateTime? ETA;
        internal DateTime? ATA;
        internal string Status = StatusTaska.Otvoren;
        internal string Nastanak = NastanakTaska.Ruta;
        internal string OpisRute;
        internal string Napomena;
        internal byte[] RowVer;
        internal List<TaskStanica> Stanice = new List<TaskStanica>();
        internal List<TaskNajavaDeo> Najave = new List<TaskNajavaDeo>();
        internal List<TaskLokomotiva> Lokomotive = new List<TaskLokomotiva>();
        internal List<TaskOsoba> Osoblje = new List<TaskOsoba>();

        internal int OdID { get { return Stanice[0].StanicaID; } }
        internal int DoID { get { return Stanice[Stanice.Count - 1].StanicaID; } }
        internal bool Otvoren { get { return Status == StatusTaska.Otvoren; } }

        // Vreme potrebno da se task izvrši: obrada (3 h) + vožnja po pruzi (OP_024).
        internal int? TrajanjeMin;

        // Zauzetost lokomotiva: od stvarnog polaska (inače planiranog) do stvarnog dolaska;
        // dok ga nema, do kasnijeg od ETA i polaska + trajanje taska.
        internal DateTime? Od { get { return ATD ?? ETD; } }
        internal DateTime? Do
        {
            get
            {
                if (ATA.HasValue) return ATA;
                DateTime? kraj = Od.HasValue && TrajanjeMin.HasValue ? Od.Value.AddMinutes(TrajanjeMin.Value) : (DateTime?)null;
                if (!ETA.HasValue) return kraj;
                return kraj.HasValue && kraj.Value > ETA.Value ? kraj : ETA;
            }
        }

        internal string OznakaNajava { get { return string.Join(", ", Najave.Select(n => n.BrojNajave)); } }
    }
}
