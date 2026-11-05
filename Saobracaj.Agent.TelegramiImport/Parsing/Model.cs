using System;
using System.Collections.Generic;
using System.Linq;

namespace Saobracaj.Agent.TelegramiImport.Parsing
{
    public enum Vrsta
    {
        Nepoznato,
        Zatvor,
        Skretnica,
        NarocitaPosiljka,
        Otkazivanje,
        VanSnage,
        Izmena,
        Radovi,
        LaganaVoznja,
        Posedanje,
        PutniPrelaz,
        RedVoznje,
        TT,
        Ostalo
    }

    /// <summary>Akcija nad postojećim telegramom (otkazivanje, van snage, izmena).</summary>
    public sealed class CiljniTelegram
    {
        public int Broj { get; set; }
        public DateTime? Datum { get; set; }
        public Vrsta Akcija { get; set; }          // Otkazivanje / VanSnage / Izmena
        public string Tacka { get; set; }           // samo ta tačka, ako je navedena
        public List<DateTime> Dani { get; } = new List<DateTime>(); // samo ti dani, ako su navedeni

        /// <summary>Oznaka koja se dodaje na početak Napomena, kao u ručnom unosu.</summary>
        public static string Oznaka(Vrsta akcija, string oznakaNovog)
        {
            switch (akcija)
            {
                case Vrsta.Otkazivanje: return "OTKAZAN TEL. BR. " + oznakaNovog + " ";
                case Vrsta.Izmena: return "IZMENJEN TEL. BR. " + oznakaNovog + " ";
                default: return "VAN SNAGE TEL. BR. " + oznakaNovog + " ";
            }
        }

        public override string ToString()
        {
            return Akcija + " " + Broj + (Datum.HasValue ? "/" + Datum.Value.ToString("dd.MM.yyyy") : "") +
                   (Tacka != null ? " t." + Tacka : "") + (Dani.Count > 0 ? " dani " + string.Join(",", Dani.Select(d => d.ToString("dd.MM"))) : "");
        }
    }

    /// <summary>Jedan red za tabelu Telegrami (tačka × dnevni interval × niz uzastopnih dana).</summary>
    public sealed class Stavka
    {
        public string Tacka { get; set; }
        public Vrsta Vrsta { get; set; }
        public string PrugaTekst { get; set; }
        public string OdTekst { get; set; }
        public string DoTekst { get; set; }
        public string Kolosek { get; set; }
        public DateTime VaziOd { get; set; }
        public DateTime VaziDo { get; set; }
        public string Napomena { get; set; }
        public bool NarocitaPosiljka { get; set; }
        public List<string> Upozorenja { get; } = new List<string>();

        public override string ToString()
        {
            return (Tacka != null ? Tacka + (Tacka.EndsWith(")") ? " " : ") ") : "") + OdTekst + " - " + DoTekst + " [" + Kolosek + "] " +
                   VaziOd.ToString("dd.MM.yyyy HH:mm") + " -> " + VaziDo.ToString("dd.MM.yyyy HH:mm");
        }
    }

    public sealed class ParsiranTelegram
    {
        public int? Broj { get; set; }
        public string Oznaka { get; set; }            // npr. "288F"
        public DateTime? Datum { get; set; }
        public string Izdavac { get; set; }           // SP / RV
        public List<Vrsta> Vrste { get; } = new List<Vrsta>();
        public string Telo { get; set; }              // očišćen tekst (bez zaglavlja primalaca)
        public string PrugaTekst { get; set; }
        public List<Stavka> Stavke { get; } = new List<Stavka>();
        public List<CiljniTelegram> Ciljevi { get; } = new List<CiljniTelegram>();
        public List<string> Upozorenja { get; } = new List<string>();
        public bool BezTeksta { get; set; }

        /// <summary>Vrste koje se upisuju u Telegrami (kao do sada u ručnom unosu).</summary>
        public bool ZaUpis
        {
            get { return Vrste.Contains(Vrsta.Zatvor) || Vrste.Contains(Vrsta.Skretnica) || Vrste.Contains(Vrsta.NarocitaPosiljka); }
        }

        public Vrsta GlavnaVrsta
        {
            get
            {
                foreach (var v in new[] { Vrsta.NarocitaPosiljka, Vrsta.Skretnica, Vrsta.Zatvor })
                    if (Vrste.Contains(v)) return v;
                foreach (var v in new[] { Vrsta.VanSnage, Vrsta.Otkazivanje, Vrsta.Izmena })
                    if (Vrste.Contains(v)) return v;
                return Vrste.Count > 0 ? Vrste[0] : Vrsta.Nepoznato;
            }
        }

        public string VrsteTekst { get { return string.Join(",", Vrste); } }
    }
}
