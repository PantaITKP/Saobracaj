using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Saobracaj.Agent.CimProvera
{
    internal enum Rezim
    {
        /// <summary>Svi izveštaji idu samo na IzvestajAdresa (testiranje).</summary>
        Test,
        /// <summary>Dolazni: odgovor pošiljaocu; odlazni: obaveštenje pošiljaocu (sebi).</summary>
        Live,
    }

    /// <summary>
    /// Podešavanja iz App.config (Saobracaj.Agent.CimProvera.exe.config). Prijemna/odlazna adresa i režim se menjaju
    /// samo ovde, bez izmene koda. Tajne su šifrovane DPAPI-jem (komanda --zastiti) ili u promenljivim okruženja.
    /// </summary>
    internal sealed class AgentConfig
    {
        public const string AgentNaziv = "CimProvera";

        // Mail
        public string Nalog;               // sanduče koje agent čita (dolazni = INBOX, odlazni = Sent)
        public string Lozinka;
        public string ImapHost; public int ImapPort; public bool ImapSsl;
        public string SmtpHost; public int SmtpPort; public bool SmtpSsl;
        public string DolazniFolder;
        public string OdlazniFolder;       // prazno = poseban folder "Sent" sa servera
        public bool CitajOdlazne;
        public string PosiljalacIzvestaja; // From izveštaja (podrazumevano = Nalog)
        public Rezim Rezim;
        public string IzvestajAdresa;      // u režimu Test svi izveštaji idu ovde
        public List<string> KopijaNa = new List<string>();
        public List<string> InterniDomeni = new List<string>();
        public bool OdgovoriNaIspravanDolazni;
        public bool ObavestiIspravanOdlazni;
        public DateTime ImportOd;
        public string UpozorenjaNa;

        // Putanje
        public string PravilaFolder;
        public string PravilaFajl;
        public string SablonFajl;
        public string StanjeFolder;
        public string LogFolder;
        public int LogDana;

        // Lokalno čitanje (bez spoljnih servisa i ključeva)
        public string TessdataFolder;
        public string OcrJezici;
        /// <summary>Jezici za drugo čitanje strane kada obrazac pokazuje zemlju (hun, ita, ron, bul, slv, hrv).</summary>
        public string OcrDodatniJezici;
        public double OcrMinPouzdanostReci;
        public double PragPouzdanosti;
        public int MaxPokusaja;
        public int MaxPrilogMB;

        // Obrasci (šabloni) i pragovi poravnanja / mastila
        public string ObrasciFolder;
        public Citanje.PragoviPoravnanja PragPoravnanja = new Citanje.PragoviPoravnanja();
        /// <summary>Mastilo (udeo površine) u rubrici iznad kog rubrika bez pročitanog teksta nije prazna nego nečitljiva.</summary>
        public double PragMastila;
        /// <summary>Gustina mastila u kvadratiću iznad koje je polje označeno (X).</summary>
        public double PragOznake;
        /// <summary>Gustina mastila u kvadratiću ispod koje je polje prazno; između = nesigurno.</summary>
        public double PragPraznogPolja;
        /// <summary>U režimu Test: JSON sa pročitanim rubrikama i slika okvira za svaki list (prazno = ne čuva se).</summary>
        public string RezultatiFolder;
        /// <summary>Kratka potvrda pošiljaocu kada je dolazni CIM ispravan („tovarni list je ispravan“).</summary>
        public bool PotvrdaIspravnogDolaznog;

        public string Bazni => AppDomain.CurrentDomain.BaseDirectory;

        public static AgentConfig Ucitaj()
        {
            var a = ConfigurationManager.AppSettings;
            string S(string k, string d) => string.IsNullOrWhiteSpace(a[k]) ? d : a[k].Trim();
            int I(string k, int d) => int.TryParse(a[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : d;
            bool B(string k, bool d) => string.IsNullOrWhiteSpace(a[k]) ? d : string.Equals(a[k].Trim(), "true", StringComparison.OrdinalIgnoreCase);
            double D(string k, double d) => double.TryParse(a[k], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : d;
            List<string> L(string k, string d) => S(k, d).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

            var cfg = new AgentConfig
            {
                Nalog = S("Nalog", "stefan.obradovic@kprevoz.co.rs"),
                ImapHost = S("ImapHost", "mail.kprevoz.co.rs"),
                ImapPort = I("ImapPort", 993),
                ImapSsl = B("ImapSsl", true),
                SmtpHost = S("SmtpHost", "mail.kprevoz.co.rs"),
                SmtpPort = I("SmtpPort", 465),
                SmtpSsl = B("SmtpSsl", true),
                DolazniFolder = S("DolazniFolder", "INBOX"),
                OdlazniFolder = S("OdlazniFolder", ""),
                CitajOdlazne = B("CitajOdlazne", true),
                Rezim = string.Equals(S("Rezim", "Test"), "Live", StringComparison.OrdinalIgnoreCase) ? Rezim.Live : Rezim.Test,
                InterniDomeni = L("InterniDomeni", "kprevoz.co.rs"),
                KopijaNa = L("KopijaNa", ""),
                OdgovoriNaIspravanDolazni = B("OdgovoriNaIspravanDolazni", true),
                ObavestiIspravanOdlazni = B("ObavestiIspravanOdlazni", false),
                ImportOd = DateTime.ParseExact(S("ImportOd", DateTime.Today.ToString("yyyy-MM-dd")), "yyyy-MM-dd", CultureInfo.InvariantCulture),

                PravilaFolder = S("PravilaFolder", "pravila"),
                PravilaFajl = S("PravilaFajl", "cim_validation_rules.json"),
                SablonFajl = S("SablonFajl", @"sablon\cim_sablon.json"),
                StanjeFolder = S("StanjeFolder", "stanje"),
                LogFolder = S("LogFolder", "logs"),
                LogDana = I("LogDana", 90),

                TessdataFolder = S("TessdataFolder", "tessdata"),
                OcrJezici = S("OcrJezici", "eng+deu+fra+srp_latn+srp"),
                OcrDodatniJezici = S("OcrDodatniJezici", "hun+ita+ron+bul+slv+hrv"),
                OcrMinPouzdanostReci = double.TryParse(a["OcrMinPouzdanostReci"], NumberStyles.Float, CultureInfo.InvariantCulture, out var mp) ? mp : 0.3,
                PragPouzdanosti = double.TryParse(a["PragPouzdanosti"], NumberStyles.Float, CultureInfo.InvariantCulture, out var prag) ? prag : 0.6,
                MaxPokusaja = I("MaxPokusaja", 5),
                MaxPrilogMB = I("MaxPrilogMB", 30),

                ObrasciFolder = S("ObrasciFolder", @"sablon\obrasci"),
                PragMastila = D("PragMastila", 0.004),
                PragOznake = D("PragOznake", 0.15),
                PragPraznogPolja = D("PragPraznogPolja", 0.05),
                RezultatiFolder = S("RezultatiFolder", @"testovi\rezultati"),
                PotvrdaIspravnogDolaznog = B("PotvrdaIspravnogDolaznog", false),
            };
            cfg.PragPoravnanja = new Citanje.PragoviPoravnanja
            {
                MinSidara = I("MinSidara", 12),
                MinPoTrecini = I("MinSidaraPoTrecini", 2),
                MaxOdstupanje = D("MaxOdstupanjePoravnanja", 0.006),
                MinUdeoTeksta = D("MinUdeoTekstualnihSidara", 0.8),
            };
            cfg.ObrasciFolder = Apsolutna(cfg.ObrasciFolder);
            if (cfg.RezultatiFolder.Length > 0) cfg.RezultatiFolder = Apsolutna(cfg.RezultatiFolder);
            cfg.PosiljalacIzvestaja = S("PosiljalacIzvestaja", cfg.Nalog);
            cfg.IzvestajAdresa = S("IzvestajAdresa", cfg.Nalog);
            cfg.UpozorenjaNa = S("UpozorenjaNa", cfg.IzvestajAdresa);

            cfg.PravilaFolder = Apsolutna(cfg.PravilaFolder);
            cfg.SablonFajl = Apsolutna(cfg.SablonFajl);
            cfg.TessdataFolder = Apsolutna(cfg.TessdataFolder);
            cfg.StanjeFolder = Apsolutna(cfg.StanjeFolder);
            cfg.LogFolder = Apsolutna(cfg.LogFolder);

            cfg.Lozinka = Tajna("Lozinka", "CIMPROVERA_LOZINKA");
            return cfg;
        }

        /// <summary>Relativna putanja se računa od foldera exe-a.</summary>
        public static string Apsolutna(string p) =>
            Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p));

        public bool JeInterni(string adresa)
        {
            if (string.IsNullOrEmpty(adresa)) return false;
            var at = adresa.LastIndexOf('@');
            var domen = at >= 0 ? adresa.Substring(at + 1) : adresa;
            return InterniDomeni.Any(d => string.Equals(d, domen.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tajna iz appSettings "&lt;kljuc&gt;.dpapi" (šifrovano pod nalogom koji pokreće agent) ili iz promenljive okruženja.
        /// Vrednost u otvorenom tekstu u config-u se ne čita.
        /// </summary>
        public static string Tajna(string kljuc, string promenljiva)
        {
            var sifrovano = ConfigurationManager.AppSettings[kljuc + ".dpapi"];
            var izOkruzenja = Environment.GetEnvironmentVariable(promenljiva);
            if (!string.IsNullOrWhiteSpace(sifrovano))
            {
                try
                {
                    var bytes = ProtectedData.Unprotect(Convert.FromBase64String(sifrovano.Trim()), Entropija, Opseg);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch (CryptographicException)
                {
                    // Šifrovano pod drugim nalogom, ili je servisnom nalogu u međuvremenu postavljena nova lozinka
                    // (Instaliraj.ps1) pa stari DPAPI ključ više ne važi: vrednost se tretira kao nepodešena.
                    NedesifrovaneTajne.Add(kljuc);
                }
            }
            return izOkruzenja;
        }

        /// <summary>Tajne iz config-a koje postoje, ali ne mogu da se dešifruju pod trenutnim nalogom.</summary>
        public static readonly List<string> NedesifrovaneTajne = new List<string>();

        public static string Zastiti(string vrednost) =>
            Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(vrednost), Entropija, Opseg));

        private static readonly byte[] Entropija = Encoding.UTF8.GetBytes("Saobracaj.Agent.CimProvera");

        private static DataProtectionScope Opseg =>
            string.Equals(ConfigurationManager.AppSettings["DpapiOpseg"], "LocalMachine", StringComparison.OrdinalIgnoreCase)
                ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser;
    }
}
