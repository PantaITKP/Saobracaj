using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Saobracaj.Agent.TelegramiImport.Data;
using Saobracaj.Agent.TelegramiImport.Mail;
using Saobracaj.Agent.TelegramiImport.Parsing;
using Saobracaj.Agent.TelegramiImport.Pdf;
using Saobracaj.Common.Data;

namespace Saobracaj.Agent.TelegramiImport
{
    /// <summary>
    /// Saobracaj.Agent.TelegramiImport.exe [--once] [--dry-run] [--import-folder &lt;izvoz&gt;]
    ///   bez argumenata / --once : jedno pokretanje (Task Scheduler ga pokreće na 2 min)
    ///   --dry-run               : sve uradi u transakciji i vrati (rollback) — ništa se ne upisuje u Telegrami
    ///   --import-folder &lt;dir&gt;   : umesto IMAP-a čita izvoz sandučeta (test / prvo poređenje)
    ///   --zastiti &lt;vrednost&gt;    : ispiše DPAPI-šifrovanu vrednost za App.config (pokrenuti pod servisnim nalogom)
    ///   --parse-folder &lt;dir&gt; [--tsv] : dijagnostika parsera nad PDF-ovima, bez baze
    /// Exit code: 0 = uspeh (i kada drugi proces već radi), 1 = greška.
    /// </summary>
    internal static class Program
    {
        private const string LockResurs = "Agent.TelegramiImport";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                var z = Arg(args, "--zastiti");
                if (z != null) { Console.WriteLine(AgentConfig.Zastiti(z)); return 0; }

                var pf = Arg(args, "--parse-folder");
                if (pf != null) return ParseFolder(pf, args.Contains("--tsv"));

                var cfg = AgentConfig.Ucitaj();
                var pr = Arg(args, "--preuzmi");
                if (pr != null) return Preuzmi(cfg, pr);
                if (args.Contains("--upis")) cfg.DryRun = false;     // samo za ručni test; u radu važi DryRun iz App.config
                if (args.Contains("--dry-run")) { cfg.DryRun = true; cfg.BezIzmena = true; }
                return Pokreni(cfg, Arg(args, "--import-folder"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static int Pokreni(AgentConfig cfg, string importFolder)
        {
            var log = new Log(cfg.LogFolder);
            log.Info("Start" + (cfg.DryRun ? " (DryRun)" : "") + (importFolder != null ? ", izvor: folder " + importFolder : ", izvor: IMAP " + cfg.ImapKorisnik));

            using (var c = SqlDb.Open(cfg.ConnectionString))
            {
                // Jedna instanca u celoj mreži: zaključavanje u bazi, drugi proces odmah izlazi
                if (!SqlDb.TryLockSession(c, LockResurs, 0))
                {
                    log.Info("Drugi proces već radi (sp_getapplock " + LockResurs + "); izlaz.");
                    UpisiRun(c, "Preskoceno", cfg.DryRun, 0, "Drugi proces već radi.");
                    return 0;
                }

                var runId = UpisiRun(c, "Radi", cfg.DryRun, null, null);
                int obradjeno = 0, greske = 0, upisano = 0, ugaseno = 0;
                string poruka = null;

                // Isteklo važenje → Aktivan = 0. Prvo i nezavisno od IMAP-a (ako mail server ne radi, gašenje i dalje radi)
                // i od DryRun-a iz App.config (prvo puštanje); preskače se samo uz ručni --dry-run.
                if (cfg.GasiIstekle && !cfg.BezIzmena)
                {
                    try
                    {
                        var ids = TelegramStore.UgasiIstekle(c);
                        if (ids.Count > 0)
                            log.Info("Isteklo važenje, Aktivan = 0 za " + ids.Count + " redova: ID " + string.Join(", ", ids));
                    }
                    catch (Exception ex)
                    {
                        greske++;
                        log.Greska("Gašenje isteklih telegrama nije uspelo", ex);
                    }
                }
                try
                {
                    var sifarnik = Sifarnik.Ucitaj(c, null);
                    var obrada = new Obrada(cfg, log, sifarnik);

                    IEnumerable<Func<Poruka>> poruke;
                    ImapIzvor imap = null;
                    if (importFolder != null)
                        poruke = FolderIzvor.Citaj(importFolder).Select(p => (Func<Poruka>)(() => p));
                    else
                    {
                        imap = new ImapIzvor();
                        imap.Otvori(cfg);
                        var poslednji = TelegramStore.PoslednjiUid(c, cfg.ImapFolder, imap.UidValidity);
                        var od = cfg.ImportOd;
                        if (poslednji == 0)
                        {
                            // Prvo pokretanje ili promenjen UIDVALIDITY: od poslednje obrađene poruke (Message-ID sprečava duplo)
                            var posl = SqlDb.Scalar(c, null, "SELECT MAX(Primljeno) FROM dbo.TelegramiMailLog WHERE Folder = @f", "@f", cfg.ImapFolder);
                            if (posl != null) { od = Convert.ToDateTime(posl).AddDays(-2); log.Info("UIDVALIDITY " + imap.UidValidity + " nema obrađenih poruka; čitam od " + od.ToString("dd.MM.yyyy")); }
                        }
                        var uids = imap.Nove(poslednji, od);
                        log.Info("Novih poruka: " + uids.Count + " (poslednji UID " + poslednji + ", UIDVALIDITY " + imap.UidValidity + ")");
                        poruke = uids.Select(u => (Func<Poruka>)(() => imap.Preuzmi(u)));
                    }

                    using (imap)
                    {
                        foreach (var dohvati in poruke)
                        {
                            Poruka p = null;
                            try
                            {
                                p = dohvati();
                                var r = obrada.Obradi(c, p);
                                if (r.Preskoceno) continue;
                                obradjeno++;
                                upisano += r.Upisano;
                                ugaseno += r.Ugaseno;
                            }
                            catch (Exception ex)
                            {
                                greske++;
                                log.Greska("Greška pri obradi poruke UID " + p?.Uid + " \"" + p?.Naslov + "\"", ex);
                                if (p != null && !cfg.DryRun) UpisiGresku(c, p, ex, log);
                            }
                        }
                    }

                    poruka = string.Format("Obrađeno poruka: {0}, upisano redova: {1}, ugašeno: {2}, grešaka: {3}", obradjeno, upisano, ugaseno, greske);
                    log.Info(poruka);
                    ZavrsiRun(c, runId, greske > 0 ? "Greska" : "Uspeh", obradjeno, poruka);
                    return greske > 0 ? 1 : 0;
                }
                catch (Exception ex)
                {
                    log.Greska("Agent prekinut", ex);
                    ZavrsiRun(c, runId, "Greska", obradjeno, ex.Message);
                    return 1;
                }
            }
        }

        private static int UpisiRun(SqlConnection c, string status, bool dryRun, int? obradjeno, string poruka)
        {
            return Convert.ToInt32(SqlDb.Scalar(c, null, @"INSERT dbo.AgentRunLog (Agent, Racunar, Status, DryRun, Obradjeno, Poruka, Kraj)
                VALUES (@a, @r, @s, @d, @o, @p, CASE WHEN @s = N'Radi' THEN NULL ELSE GETDATE() END); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "@a", AgentConfig.AgentNaziv, "@r", Environment.MachineName, "@s", status, "@d", dryRun, "@o", obradjeno, "@p", poruka));
        }

        private static void ZavrsiRun(SqlConnection c, int id, string status, int obradjeno, string poruka)
        {
            try
            {
                SqlDb.Execute(c, null, "UPDATE dbo.AgentRunLog SET Kraj = GETDATE(), Status = @s, Obradjeno = @o, Poruka = @p WHERE ID = @id",
                    "@s", status, "@o", obradjeno, "@p", poruka, "@id", id);
            }
            catch { /* log run-a ne sme da sakrije pravu grešku */ }
        }

        /// <summary>Poruka čija obrada je pukla se upisuje sa statusom Greska (bez redova), da se ne bi obrađivala u krug.</summary>
        private static void UpisiGresku(SqlConnection c, Poruka p, Exception ex, Log log)
        {
            try
            {
                using (var tx = c.BeginTransaction())
                {
                    var store = new TelegramStore(c, tx);
                    if (!store.VecObradjena(p))
                    {
                        var id = store.UpisiMailLog(p, false);
                        store.ZavrsiMailLog(id, "Greska", null, 0, new[] { ex.GetType().Name + ": " + ex.Message });
                    }
                    tx.Commit();
                }
            }
            catch (Exception ex2)
            {
                log.Greska("Ni status Greska nije upisan za UID " + p.Uid, ex2);
            }
        }

        /// <summary>
        /// Snima poruke iz sandučeta (od ImportOd) u folder, u formatu izvoza (UID_naslov\poruka.txt + prilozi),
        /// za testove i dijagnostiku. Samo čitanje; baza se ne koristi.
        /// </summary>
        private static int Preuzmi(AgentConfig cfg, string folder)
        {
            using (var imap = new ImapIzvor())
            {
                imap.Otvori(cfg);
                var uids = imap.Nove(0, cfg.ImportOd);
                Console.WriteLine("Poruka: " + uids.Count);
                foreach (var u in uids)
                {
                    var p = imap.Preuzmi(u);
                    var ime = string.Concat((p.Uid.ToString("000000") + "_" + (p.Naslov ?? "")).Take(40)
                        .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).TrimEnd(' ', '.');
                    var dir = Path.Combine(folder, ime);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "poruka.txt"),
                        "Datum: " + (p.Primljeno?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "") + "\n" +
                        "Od: " + p.Posiljalac + "\nNaslov: " + p.Naslov + "\n" + new string('-', 60) + "\n" + p.TekstMaila, Encoding.UTF8);
                    foreach (var a in p.Prilozi)
                        {
                        var n = string.Concat((a.Naziv ?? "prilog").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
                        var ext = Path.GetExtension(n);
                        n = string.Concat(Path.GetFileNameWithoutExtension(n).Take(40)).Trim() + ext;
                        File.WriteAllBytes(Path.Combine(dir, n), a.Sadrzaj);
                    }
                    Console.WriteLine(ime);
                }
            }
            return 0;
        }

        private static string Arg(string[] args, string ime)
        {
            int i = Array.IndexOf(args, ime);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        /// <summary>Dijagnostika: parsira sve PDF-ove u folderu (rekurzivno) i ispisuje rezultat.</summary>
        private static int ParseFolder(string folder, bool tsv)
        {
            foreach (var f in Directory.GetFiles(folder, "*.pdf", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            {
                var tekst = PdfTekst.Izvuci(File.ReadAllBytes(f));
                string naslov = Path.GetFileNameWithoutExtension(f);
                DateTime? datumMaila = null;
                var por = Path.Combine(Path.GetDirectoryName(f), "poruka.txt");
                if (File.Exists(por))
                    foreach (var l in File.ReadLines(por).Take(10))
                    {
                        if (l.StartsWith("Naslov: ")) naslov = l.Substring(8);
                        if (l.StartsWith("Datum: ") && DateTimeOffset.TryParse(l.Substring(7), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) datumMaila = d.LocalDateTime;
                    }
                var t = TelegramParser.Parsiraj(tekst, naslov, datumMaila);
                var dir = Path.GetFileName(Path.GetDirectoryName(f));
                var rb = dir.Substring(0, Math.Min(4, dir.Length));
                if (tsv)
                {
                    foreach (var s in t.Stavke)
                        Console.WriteLine(string.Join("\t", t.Broj, t.Datum?.ToString("yyyy-MM-dd"), t.GlavnaVrsta, s.Tacka, s.OdTekst, s.DoTekst, s.Kolosek,
                            s.VaziOd.ToString("yyyy-MM-dd HH:mm"), s.VaziDo.ToString("yyyy-MM-dd HH:mm"), rb));
                    continue;
                }
                Console.WriteLine("=== " + rb + " " + t.Oznaka + " " + t.Datum?.ToString("dd.MM.yyyy") + " [" + t.VrsteTekst + "] " + t.Izdavac + " pruga: " + t.PrugaTekst);
                foreach (var c in t.Ciljevi) Console.WriteLine("   CILJ " + c);
                foreach (var s in t.Stavke)
                {
                    Console.WriteLine("   " + s);
                    Console.WriteLine("      N: " + s.Napomena);
                    foreach (var u in s.Upozorenja) Console.WriteLine("      ! " + u);
                }
                foreach (var u in t.Upozorenja) Console.WriteLine("   ! " + u);
            }
            return 0;
        }
    }
}
