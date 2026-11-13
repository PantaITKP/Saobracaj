using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocToPDFConverter;

namespace Saobracaj.Agent.CimProvera.Prilozi
{
    /// <summary>
    /// Word (.docx, .docm, .doc, .dot, .dotx, .rtf) → PDF preko Syncfusion DocIO 18.4 (u memoriji, bez Office-a, bez prozora i bez
    /// privremenih fajlova), najviše 60 s po fajlu. Dobijeni PDF se dalje čita kao PDF prilog (šablon, tekstualni sloj, pravila).
    /// Licencni ključ se čita samo iz Syncfusion.dpapi (DPAPI pod nalogom agenta, upisuje ga Instaliraj.ps1); nikad iz koda, config-a
    /// ili loga. Bez ključa se ništa ne pretvara (Syncfusion bi u PDF upisao tekst probne licence) – prilog ide na ručnu proveru.
    /// .odt DocIO 18.4 ne otvara → ručna provera.
    /// </summary>
    internal static class WordUPdf
    {
        public const string ImeFajlaKljuca = "Syncfusion.dpapi";
        public const int MaxSekundi = 60;

        /// <summary>Ključ je učitan i registrovan (jednom po procesu).</summary>
        public static bool KljucUcitan { get; private set; }
        /// <summary>Stanje ključa za log i --test-veze (bez vrednosti ključa).</summary>
        public static string StanjeKljuca { get; private set; } = "nije učitavan";
        /// <summary>U ovom pokretanju je bar jedan Word prilog ostao nepretvoren jer ključ nije učitan (za jedno interno obaveštenje).</summary>
        public static bool NedostajaoKljuc;

        private static readonly Dictionary<string, byte[]> kes = new Dictionary<string, byte[]>();
        private static readonly object zakljucano = new object();

        /// <summary>Folderi u kojima se traži Syncfusion.dpapi: folder programa (server), pa %LOCALAPPDATA%\Saobracaj\CimProvera (razvoj).</summary>
        public static IEnumerable<string> PutanjeKljuca()
        {
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImeFajlaKljuca);
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Saobracaj", "CimProvera", ImeFajlaKljuca);
        }

        /// <summary>Učitava i registruje ključ (jednom). Vraća da li je ključ učitan; razlog je u <see cref="StanjeKljuca"/>.</summary>
        public static bool UcitajKljuc(Log log = null)
        {
            lock (zakljucano)
            {
                if (KljucUcitan) return true;
                foreach (var p in PutanjeKljuca())
                {
                    if (!File.Exists(p)) continue;
                    string kljuc;
                    try { kljuc = AgentConfig.Desifruj(File.ReadAllText(p)); }
                    catch (Exception ex) { kljuc = null; StanjeKljuca = "fajl " + p + " nije pročitan (" + ex.Message + ")"; }
                    if (string.IsNullOrWhiteSpace(kljuc))
                    {
                        if (!StanjeKljuca.StartsWith("fajl")) StanjeKljuca = "fajl " + p + " ne može da se dešifruje pod nalogom " + Environment.UserName + " – ponovo pokrenuti Instaliraj.ps1";
                        log?.Greska("Syncfusion ključ: " + StanjeKljuca + ". Word prilozi idu na ručnu proveru.");
                        return false;
                    }
                    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(kljuc.Trim());
                    kljuc = null;
                    KljucUcitan = true;
                    StanjeKljuca = "učitan (" + p + ")";
                    return true;
                }
                StanjeKljuca = "nije podešen (nema " + ImeFajlaKljuca + " – pokrenuti Instaliraj.ps1)";
                log?.Greska("Syncfusion ključ " + StanjeKljuca + ". Word prilozi idu na ručnu proveru.");
                return false;
            }
        }

        /// <summary>Samo za testove: ponaša se kao da ključ nije učitan.</summary>
        internal static bool ZaTestBezKljuca;

        /// <summary>
        /// PDF iz Word priloga (isti sadržaj → isti PDF iz memorije). null + razlog kada pretvaranje nije moguće ili ne uspe;
        /// razlog ide u log i u ručnu proveru, nikad pošiljaocu.
        /// </summary>
        public static byte[] Pretvori(Prilog p, out string razlog, out TimeSpan trajanje, int maxSekundi = MaxSekundi)
        {
            razlog = null; trajanje = TimeSpan.Zero;
            var ext = (Path.GetExtension(p.Naziv) ?? "").ToLowerInvariant();
            if (ext == ".odt" || ext == ".ott")
            {
                razlog = "OpenDocument (" + ext + ") se ne pretvara u PDF (Syncfusion DocIO ga ne čita) – potrebna je ručna provera";
                return null;
            }
            if (ZaTestBezKljuca || !KljucUcitan)
            {
                NedostajaoKljuc = true;
                razlog = "Word prilog nije pretvoren u PDF: Syncfusion licencni ključ nije učitan (" + (ZaTestBezKljuca ? "test" : StanjeKljuca) + ") – potrebna je ručna provera";
                return null;
            }
            lock (zakljucano) if (kes.TryGetValue(p.Hes, out var b)) return b;

            var sw = Stopwatch.StartNew();
            var sadrzaj = p.Sadrzaj;
            var zadatak = Task.Run(() =>
            {
                using (var ulaz = new MemoryStream(sadrzaj, false))
                using (var doc = new WordDocument(ulaz, FormatType.Automatic))
                using (var konv = new DocToPDFConverter())
                using (var pdf = konv.ConvertToPDF(doc))
                using (var izlaz = new MemoryStream())
                {
                    pdf.Save(izlaz);
                    return izlaz.ToArray();
                }
            });
            try
            {
                if (!zadatak.Wait(TimeSpan.FromSeconds(maxSekundi)))
                {
                    razlog = "pretvaranje Word → PDF je trajalo duže od " + maxSekundi + " s – potrebna je ručna provera";
                    return null;
                }
            }
            catch (AggregateException ex)
            {
                var e = ex.InnerException ?? ex;
                razlog = "Word prilog ne može da se pretvori u PDF (" + e.GetType().Name + ": " + e.Message + ") – potrebna je ručna provera";
                return null;
            }
            finally { trajanje = sw.Elapsed; }
            var rez = zadatak.Result;
            if (rez == null || rez.Length < 100) { razlog = "pretvaranje Word → PDF nije dalo PDF – potrebna je ručna provera"; return null; }
            lock (zakljucano) kes[p.Hes] = rez;
            return rez;
        }
    }
}
