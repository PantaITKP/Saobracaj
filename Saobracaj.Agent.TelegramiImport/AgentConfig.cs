using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Saobracaj.Agent.TelegramiImport
{
    /// <summary>
    /// Podešavanja iz App.config (appSettings + connectionStrings["Telegrami"]).
    /// Tajne se čuvaju šifrovane DPAPI-jem pod nalogom koji pokreće agent (vidi README, "--zastiti").
    /// </summary>
    internal sealed class AgentConfig
    {
        public const string AgentNaziv = "TelegramiImport";

        public string ConnectionString;
        public string ImapHost;
        public int ImapPort;
        public bool ImapSsl;
        public string ImapKorisnik;
        public string ImapLozinka;
        public string ImapFolder;
        public DateTime ImportOd;
        public bool DryRun;
        public string LogFolder;
        public string PdfFolder;
        public bool GasiIstekle;
        /// <summary>Ručni --dry-run: ne menja ništa u bazi (ni gašenje isteklih). DryRun iz App.config ne utiče na gašenje.</summary>
        public bool BezIzmena;

        public static AgentConfig Ucitaj()
        {
            var a = ConfigurationManager.AppSettings;
            var cfg = new AgentConfig
            {
                ImapHost = a["ImapHost"] ?? "mail.kprevoz.co.rs",
                ImapPort = int.Parse(a["ImapPort"] ?? "993", CultureInfo.InvariantCulture),
                ImapSsl = !string.Equals(a["ImapSsl"], "false", StringComparison.OrdinalIgnoreCase),
                ImapKorisnik = a["ImapKorisnik"] ?? "telegrami@kprevoz.co.rs",
                ImapFolder = a["ImapFolder"] ?? "INBOX",
                ImportOd = DateTime.ParseExact(a["ImportOd"] ?? "2026-11-01", "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DryRun = !string.Equals(a["DryRun"], "false", StringComparison.OrdinalIgnoreCase), // podrazumevano UKLJUČEN
                LogFolder = a["LogFolder"],
                PdfFolder = a["PdfFolder"],
                GasiIstekle = !string.Equals(a["GasiIstekle"], "false", StringComparison.OrdinalIgnoreCase),
            };
            if (string.IsNullOrWhiteSpace(cfg.LogFolder))
                cfg.LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            cfg.ImapLozinka = Tajna("ImapLozinka");

            // Konekcija.dpapi / TELEGRAMI_KONEKCIJA ima prednost (ceo connection string šifrovan), inače connectionStrings["Telegrami"]
            var cs = Tajna("Konekcija");
            if (string.IsNullOrWhiteSpace(cs)) cs = ConfigurationManager.ConnectionStrings["Telegrami"]?.ConnectionString;
            if (cs != null && cs.Contains("{SqlLozinka}")) cs = cs.Replace("{SqlLozinka}", Tajna("SqlLozinka") ?? "");
            cfg.ConnectionString = cs;
            return cfg;
        }

        /// <summary>
        /// Vrednost iz appSettings: "&lt;kljuc&gt;.dpapi" (šifrovano, preporučeno) ili promenljive okruženja
        /// TELEGRAMI_&lt;KLJUC&gt; (za ručni test). Lozinka u otvorenom tekstu u App.config se ne čita.
        /// </summary>
        public static string Tajna(string kljuc)
        {
            var sifrovano = ConfigurationManager.AppSettings[kljuc + ".dpapi"];
            if (!string.IsNullOrWhiteSpace(sifrovano))
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(sifrovano), Entropija, Opseg);
                return Encoding.UTF8.GetString(bytes);
            }
            return Environment.GetEnvironmentVariable("TELEGRAMI_" + kljuc.ToUpperInvariant());
        }

        public static string Zastiti(string vrednost)
        {
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(vrednost), Entropija, Opseg));
        }

        private static readonly byte[] Entropija = Encoding.UTF8.GetBytes("Saobracaj.Agent.TelegramiImport");

        private static DataProtectionScope Opseg
        {
            get
            {
                return string.Equals(ConfigurationManager.AppSettings["DpapiOpseg"], "LocalMachine", StringComparison.OrdinalIgnoreCase)
                    ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser;
            }
        }
    }
}
