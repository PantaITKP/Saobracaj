using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Mail
{
    /// <summary>Red arhive (indeks.csv).</summary>
    internal sealed class ZapisArhive
    {
        public string Folder = "", MessageId = "", Datum = "", Posiljalac = "", Naslov = "", Prilozi = "", HesPriloga = "";
        public long Uid, UidValidity;
        public long Bajtova;
        public bool Sopstveni;
        public string Fajl = "";
    }

    /// <summary>
    /// v3.0 (tačka 5, 2.9.3 tačka 4): arhiva sandučeta – svaka poruka kao .eml u &lt;koren&gt;\&lt;folder&gt;\&lt;UID&gt;.eml i red u indeks.csv
    /// (folder, UID, UIDVALIDITY, Message-ID, datum, pošiljalac, naslov, imena priloga, heš priloga, veličina, sopstveni odgovor).
    /// Arhiva je SAMO ZA DOPISIVANJE: postojeći fajl se nikad ne menja ni briše. Promena UIDVALIDITY → novi podfolder &lt;folder&gt;@&lt;uidvalidity&gt;.
    /// </summary>
    internal sealed class Arhiva
    {
        public readonly string Koren;
        public string Indeks => Path.Combine(Koren, "indeks.csv");

        public Arhiva(string koren) { Koren = koren; }

        private static string ImeFoldera(string folder, long uidValidity, long prvaUidValidity) =>
            Regex.Replace(folder ?? "INBOX", @"[\\/:*?""<>|]", "_") + (prvaUidValidity == 0 || uidValidity == prvaUidValidity ? "" : "@" + uidValidity);

        /// <summary>UIDVALIDITY zapisan pri prvom arhiviranju foldera (0 = folder još nije arhiviran).</summary>
        public long PrvaUidValidity(string folder)
        {
            var f = Path.Combine(Koren, Regex.Replace(folder ?? "INBOX", @"[\\/:*?""<>|]", "_"), "uidvalidity.txt");
            return File.Exists(f) && long.TryParse(File.ReadAllText(f).Trim(), out var v) ? v : 0;
        }

        public string Putanja(string folder, long uidValidity, long uid)
        {
            var prva = PrvaUidValidity(folder);
            return Path.Combine(Koren, ImeFoldera(folder, uidValidity, prva), uid + ".eml");
        }

        /// <summary>Upis poruke ako je još nema (isti folder/UIDVALIDITY/UID). Vraća true kada je upisana sada.</summary>
        public bool Sacuvaj(string folder, long uidValidity, long uid, MimeMessage m, Action<string> upozorenje = null)
        {
            Directory.CreateDirectory(Koren);
            var osnovni = Path.Combine(Koren, Regex.Replace(folder ?? "INBOX", @"[\\/:*?""<>|]", "_"));
            Directory.CreateDirectory(osnovni);
            var uv = Path.Combine(osnovni, "uidvalidity.txt");
            if (!File.Exists(uv)) File.WriteAllText(uv, uidValidity.ToString(CultureInfo.InvariantCulture));
            else if (PrvaUidValidity(folder) != uidValidity && !Directory.Exists(Path.Combine(Koren, ImeFoldera(folder, uidValidity, PrvaUidValidity(folder)))))
                upozorenje?.Invoke("Arhiva: UIDVALIDITY foldera " + folder + " se promenio (" + PrvaUidValidity(folder) + " → " + uidValidity + ") – nove poruke idu u " + ImeFoldera(folder, uidValidity, PrvaUidValidity(folder)) + "; postojeća arhiva se ne menja.");
            var put = Putanja(folder, uidValidity, uid);
            if (File.Exists(put)) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(put));
            var tmp = put + ".tmp";
            using (var f = File.Create(tmp)) m.WriteTo(f);
            File.Move(tmp, put);
            try { File.SetAttributes(put, FileAttributes.ReadOnly); } catch { }
            DodajUIndeks(Zapis(folder, uidValidity, uid, m, put));
            return true;
        }

        public static ZapisArhive Zapis(string folder, long uidValidity, long uid, MimeMessage m, string fajl)
        {
            var prilozi = Raspakivanje.PriloziMaila(m).ToList();
            return new ZapisArhive
            {
                Folder = folder, Uid = uid, UidValidity = uidValidity, MessageId = (m.MessageId ?? "").Trim('<', '>'),
                Datum = m.Date == DateTimeOffset.MinValue ? "" : m.Date.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
                Posiljalac = PrimaociLive.Ocisti(m.From.Mailboxes.FirstOrDefault()?.Address), Naslov = Regex.Replace(m.Subject ?? "", @"\s+", " ").Trim(),
                Prilozi = string.Join("; ", prilozi.Select(p => p.ime)),
                HesPriloga = prilozi.Count == 0 ? "" : Prilog.Sha256(prilozi.SelectMany(p => p.sadrzaj).ToArray()).Substring(0, 16),
                Bajtova = File.Exists(fajl) ? new FileInfo(fajl).Length : 0,
                Sopstveni = m.Headers.Contains(Slanje.ZaglavljeAgenta) || string.Equals((m.Headers[Slanje.ZaglavljeOdgovora] ?? "").Trim(), "odgovor", StringComparison.OrdinalIgnoreCase),
                Fajl = fajl,
            };
        }

        private static readonly string[] Kolone = { "folder", "uid", "uidvalidity", "message_id", "datum", "posiljalac", "naslov", "prilozi", "hes_priloga", "bajtova", "sopstveni", "fajl" };

        private static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";

        private void DodajUIndeks(ZapisArhive z)
        {
            var novo = !File.Exists(Indeks);
            var red = string.Join(";", new[] { Csv(z.Folder), z.Uid.ToString(CultureInfo.InvariantCulture), z.UidValidity.ToString(CultureInfo.InvariantCulture), Csv(z.MessageId), Csv(z.Datum), Csv(z.Posiljalac),
                Csv(z.Naslov), Csv(z.Prilozi), Csv(z.HesPriloga), z.Bajtova.ToString(CultureInfo.InvariantCulture), z.Sopstveni ? "1" : "0", Csv(z.Fajl.Substring(Math.Min(z.Fajl.Length, Koren.Length)).TrimStart('\\')) });
            File.AppendAllText(Indeks, (novo ? "﻿" + string.Join(";", Kolone) + "\r\n" : "") + red + "\r\n", new UTF8Encoding(false));
        }

        /// <summary>Redovi indeksa.</summary>
        public List<ZapisArhive> Procitaj()
        {
            var l = new List<ZapisArhive>();
            if (!File.Exists(Indeks)) return l;
            foreach (var red in File.ReadAllLines(Indeks, Encoding.UTF8).Skip(1))
            {
                var p = CsvRed(red);
                if (p.Count < 12) continue;
                l.Add(new ZapisArhive
                {
                    Folder = p[0], Uid = long.Parse(p[1], CultureInfo.InvariantCulture), UidValidity = long.Parse(p[2], CultureInfo.InvariantCulture), MessageId = p[3], Datum = p[4], Posiljalac = p[5],
                    Naslov = p[6], Prilozi = p[7], HesPriloga = p[8], Bajtova = long.Parse(p[9], CultureInfo.InvariantCulture), Sopstveni = p[10] == "1", Fajl = Path.Combine(Koren, p[11]),
                });
            }
            return l;
        }

        internal static List<string> CsvRed(string red)
        {
            var l = new List<string>(); var sb = new StringBuilder(); bool nav = false;
            for (int i = 0; i < red.Length; i++)
            {
                var c = red[i];
                if (nav) { if (c == '"') { if (i + 1 < red.Length && red[i + 1] == '"') { sb.Append('"'); i++; } else nav = false; } else sb.Append(c); }
                else if (c == '"') nav = true;
                else if (c == ';') { l.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            l.Add(sb.ToString());
            return l;
        }

        /// <summary>Poruke foldera arhive od UID-a N (bez sopstvenih odgovora agenta ako se traži), redom po UID-u.</summary>
        public List<ZapisArhive> Poruke(string folder, long odUid, long doUid = long.MaxValue, bool bezSopstvenih = true) =>
            Procitaj().Where(z => string.Equals(z.Folder, folder, StringComparison.OrdinalIgnoreCase) && z.Uid >= odUid && z.Uid <= doUid && !(bezSopstvenih && z.Sopstveni))
                .GroupBy(z => z.UidValidity + "|" + z.Uid).Select(g => g.First()).OrderBy(z => z.UidValidity == PrvaUidValidity(folder) ? 0 : 1).ThenBy(z => z.Uid).ToList();

        /// <summary>Izveštaj o arhivi: broj i veličina po folderu, od toga sopstveni odgovori agenta.</summary>
        public string Izvestaj()
        {
            var sb = new StringBuilder();
            string Mb(long b) => (b / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            foreach (var g in Procitaj().GroupBy(z => z.Folder))
            {
                var sop = g.Where(z => z.Sopstveni).ToList();
                sb.AppendLine(g.Key + ": " + g.Count() + " poruka, " + Mb(g.Sum(z => z.Bajtova)) + " (UID " + g.Min(z => z.Uid) + "–" + g.Max(z => z.Uid) + "); sopstveni odgovori agenta: " + sop.Count + ", " + Mb(sop.Sum(z => z.Bajtova)));
            }
            return sb.ToString();
        }
    }
}
