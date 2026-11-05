using System;
using System.Configuration;
using System.Net;

namespace Saobracaj
{
    /// <summary>
    /// Nalozi za slanje mail-a. Lozinke se ne drže u kodu: čitaju se iz appSettings, koji se dopunjuju
    /// fajlom Lozinke.config pored Saobracaj.exe (App.config: &lt;appSettings file="Lozinke.config"&gt;).
    /// Lozinke.config nije u repou; primer je Lozinke.config.example.
    /// </summary>
    internal static class MailNalog
    {
        public static NetworkCredential Disp()
        {
            var korisnik = ConfigurationManager.AppSettings["DispMailKorisnik"];
            if (string.IsNullOrWhiteSpace(korisnik)) korisnik = "disp@kprevoz.co.rs";
            var lozinka = ConfigurationManager.AppSettings["DispMailLozinka"];
            if (string.IsNullOrEmpty(lozinka))
                throw new InvalidOperationException("Lozinka za " + korisnik + " nije podešena. Dodajte DispMailLozinka u Lozinke.config pored Saobracaj.exe.");
            return new NetworkCredential(korisnik, lozinka);
        }
    }
}
