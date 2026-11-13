using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// reset-stanje: zaustavlja zadatak u Task Scheduler-u, čeka da se tekući prolaz završi, pravi rezervnu kopiju celog
    /// foldera stanja (stanje\arhiva\yyyy-MM-dd_HHmmss), briše spisak obrađenih mailova (UID po folderu, Message-ID, heš
    /// priloga) i keš (OCR/tekst strana i rezultati provere), postavlja nultu tačku i ponovo uključuje zadatak.
    /// Ne dira: pravila i istoriju pravila, šablone, config (i šifrovanu lozinku), logove i testovi\.
    /// </summary>
    internal static class ResetStanja
    {
        public const string PodrazumevaniZadatak = @"Saobracaj\Agent CimProvera";
        public const string ImeMuteksa = @"Global\Saobracaj.Agent.CimProvera";

        public static int Pokreni(AgentConfig cfg, Log log, string zadatak, bool bezZadatka)
        {
            var vreme = DateTimeOffset.Now;
            var stanjeF = cfg.StanjeFolder;
            var arhiva = Path.Combine(stanjeF, "arhiva", vreme.ToString("yyyy-MM-dd_HHmmss"));
            var izv = new StringBuilder();
            izv.AppendLine("Reset stanja agenta CimProvera – " + vreme.ToString("dd.MM.yyyy HH:mm:ss zzz") + ", " + Environment.MachineName + " (" + Environment.UserName + ")");
            izv.AppendLine("Folder stanja: " + stanjeF);
            log.Info("Reset stanja: početak (" + stanjeF + ").");
            bool iskljucen = false;
            int kod = 1;
            try
            {
                if (!bezZadatka)
                {
                    var bio = Zadatak(zadatak, "/Query", "/XML", out var xml) == 0 ? (xml.Contains("<Enabled>false</Enabled>") ? "isključen" : "uključen") : "nepoznato";
                    if (Zadatak(zadatak, "/Change", "/DISABLE", out var o) != 0) throw new InvalidOperationException("Zadatak „" + zadatak + "“ nije isključen: " + o.Trim());
                    iskljucen = true;
                    izv.AppendLine("Zadatak „" + zadatak + "“: bio " + bio + ", isključen pre brisanja.");
                    log.Info("Reset stanja: zadatak „" + zadatak + "“ isključen.");
                }

                // Tekući prolaz agenta drži muteks: reset čeka da se završi (najviše 15 minuta).
                using (var mutex = new Mutex(false, ImeMuteksa))
                {
                    bool imam;
                    try { imam = mutex.WaitOne(TimeSpan.FromMinutes(15)); } catch (AbandonedMutexException) { imam = true; }
                    if (!imam) throw new InvalidOperationException("Agent i posle 15 minuta obrađuje mail – reset nije urađen, ništa nije obrisano.");
                    try { Resetuj(stanjeF, arhiva, vreme, izv, log); }
                    finally { mutex.ReleaseMutex(); }
                }
                kod = 0;
            }
            catch (Exception ex)
            {
                izv.AppendLine("GREŠKA: " + ex.Message);
                log.Greska("Reset stanja nije uspeo", ex);
            }
            finally
            {
                if (iskljucen)
                {
                    var ok = Zadatak(zadatak, "/Change", "/ENABLE", out var o) == 0;
                    izv.AppendLine(ok ? "Zadatak „" + zadatak + "“ ponovo uključen." : "PAŽNJA: zadatak „" + zadatak + "“ NIJE ponovo uključen: " + o.Trim());
                    if (ok) log.Info("Reset stanja: zadatak „" + zadatak + "“ ponovo uključen.");
                    else { log.Greska("Reset stanja: zadatak „" + zadatak + "“ nije ponovo uključen: " + o.Trim()); kod = 1; }
                }
                var tekst = izv.ToString();
                Console.WriteLine(tekst);
                try
                {
                    Directory.CreateDirectory(arhiva);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(arhiva), Path.GetFileName(arhiva) + "_izvestaj.txt"), tekst, Encoding.UTF8);
                }
                catch (Exception ex) { log.Upozorenje("Izveštaj o resetu nije upisan: " + ex.Message); }
            }
            return kod;
        }

        private static void Resetuj(string stanjeF, string arhiva, DateTimeOffset vreme, StringBuilder izv, Log log)
        {
            // 1. Rezervna kopija celog foldera stanja (osim ranijih arhiva).
            var arhivaKoren = Path.Combine(stanjeF, "arhiva");
            var fajlovi = Directory.GetFiles(stanjeF, "*", SearchOption.AllDirectories)
                .Where(f => !f.StartsWith(arhivaKoren + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).ToList();
            long bajtova = 0;
            foreach (var f in fajlovi)
            {
                var cilj = Path.Combine(arhiva, f.Substring(stanjeF.TrimEnd('\\').Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(cilj));
                File.Copy(f, cilj, false);
                if (new FileInfo(cilj).Length != new FileInfo(f).Length) throw new IOException("Kopija nije ista: " + f);
                bajtova += new FileInfo(f).Length;
            }
            izv.AppendLine();
            izv.AppendLine("REZERVNA KOPIJA: " + arhiva + " (" + fajlovi.Count + " fajlova, " + Mb(bajtova) + ")");

            // 2. Spisak obrađenih mailova (stanje.json): poruke i UID po folderu; nulta tačka = sada.
            var s = Stanje.Ucitaj(stanjeF);
            var poStatusu = s.Poruke.Values.GroupBy(p => (p.Status ?? "?").Split('+')[0]).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count());
            var hesova = s.Poruke.Values.Count(p => !string.IsNullOrEmpty(p.HesPriloga));
            var folderi = s.Folderi.Select(kv => kv.Key + " (poslednji UID " + kv.Value.PoslednjiUid + ")").ToList();
            var upozorenja = s.Upozorenja.Count;
            izv.AppendLine();
            izv.AppendLine("OBRISANO:");
            izv.AppendLine("- spisak obrađenih mailova u stanje.json: " + s.Poruke.Count + " poruka (Message-ID, UID, heš priloga kod " + hesova + "; " + string.Join(", ", poStatusu) + ")");
            izv.AppendLine("- pozicija čitanja po folderu: " + (folderi.Count > 0 ? string.Join(", ", folderi) : "nema"));
            s.Poruke.Clear();
            s.Folderi.Clear();
            s.NultaTacka = vreme;
            s.ResetArhiva = arhiva;
            s.PrviIzvestajiIzgraditi = true;
            s.Sacuvaj();

            // 3. Keš: tekst/OCR strana (<heš>_strane_*.json) i rezultati čitanja i provere (<heš>_L*_*.json).
            var kes = Path.Combine(stanjeF, "kes");
            int ocr = 0, rez = 0, ost = 0; long kb = 0; var neObrisano = new List<string>();
            if (Directory.Exists(kes))
                foreach (var f in Directory.GetFiles(kes, "*", SearchOption.AllDirectories))
                {
                    var len = new FileInfo(f).Length;
                    try { File.Delete(f); kb += len; }
                    catch (Exception ex) { neObrisano.Add(Path.GetFileName(f) + ": " + ex.Message); continue; }
                    var ime = Path.GetFileName(f);
                    if (ime.Contains("_strane_")) ocr++; else if (System.Text.RegularExpressions.Regex.IsMatch(ime, @"_L\d+_")) rez++; else ost++;
                }
            izv.AppendLine("- OCR keš (tekst strana): " + ocr + " fajlova");
            izv.AppendLine("- keš rezultata provere: " + rez + " fajlova" + (ost > 0 ? " (+" + ost + " ostalih u kes\\)" : "") + "; ukupno " + Mb(kb));
            if (neObrisano.Count > 0) throw new IOException("Keš nije potpuno obrisan (" + neObrisano.Count + "): " + string.Join("; ", neObrisano.Take(5)));
            var prvi = Path.Combine(stanjeF, IndeksPrvih.Ime);
            if (File.Exists(prvi)) { File.Delete(prvi); izv.AppendLine("- indeks prvih izveštaja (" + IndeksPrvih.Ime + "), pravi se ponovo iz rezervne kopije"); }

            izv.AppendLine();
            izv.AppendLine("OSTALO:");
            izv.AppendLine("- stanje.json: vreme poslednjih upozorenja (" + upozorenja + "), nulta tačka " + vreme.ToString("dd.MM.yyyy HH:mm:ss zzz"));
            foreach (var d in Directory.GetDirectories(stanjeF).Where(d => !string.Equals(d, arhivaKoren, StringComparison.OrdinalIgnoreCase) && !string.Equals(d, kes, StringComparison.OrdinalIgnoreCase)))
                izv.AppendLine("- " + Path.GetFileName(d) + "\\ (" + Directory.GetFiles(d, "*", SearchOption.AllDirectories).Length + " fajlova)" + (Path.GetFileName(d) == "pravila_kes" ? " – lokalna kopija pravila" : ""));
            foreach (var f in Directory.GetFiles(stanjeF).Where(f => !f.EndsWith("stanje.json", StringComparison.OrdinalIgnoreCase)))
                izv.AppendLine("- " + Path.GetFileName(f));
            izv.AppendLine("- kes\\ (prazan folder)");
            izv.AppendLine("- arhiva\\ (" + Directory.GetDirectories(arhivaKoren).Length + " rezervnih kopija)");
            izv.AppendLine("- van foldera stanja ništa nije menjano: pravila i pravila\\istorija, sablon\\, config sa šifrovanom lozinkom, logs\\, testovi\\");
            izv.AppendLine();
            izv.AppendLine("Stare poruke u sandučetu se ne obrađuju ponovo; obrađuje se svaki mail koji stigne posle " + vreme.ToString("dd.MM.yyyy HH:mm:ss") + ".");

            log.Info("Stanje resetovano " + vreme.ToString("yyyy-MM-dd HH:mm:ss zzz") + ", rezervna kopija u " + arhiva);
        }

        private static string Mb(long b) => (b / 1048576.0).ToString("0.0") + " MB";

        private static int Zadatak(string ime, string komanda, string opcija, out string izlaz)
        {
            var psi = new ProcessStartInfo("schtasks.exe", komanda + " /TN \"" + ime + "\" " + opcija)
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var p = Process.Start(psi))
            {
                var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                izlaz = o;
                return p.ExitCode;
            }
        }
    }
}
