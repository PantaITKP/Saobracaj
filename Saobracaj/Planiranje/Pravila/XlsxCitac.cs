using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Saobracaj.Planiranje.Pravila
{
    // Čitanje prvog lista .xlsx datoteke bez spoljnih paketa; spojene ćelije dobijaju vrednost cele oblasti.
    internal sealed class XlsxList
    {
        internal readonly Dictionary<int, Dictionary<int, string>> Redovi = new Dictionary<int, Dictionary<int, string>>();
        internal int PoslednjiRed;

        internal string Celija(int red, int kolona)
        {
            Dictionary<int, string> r;
            string v;
            return Redovi.TryGetValue(red, out r) && r.TryGetValue(kolona, out v) ? v : "";
        }
    }

    internal static class XlsxCitac
    {
        // Uspeva i kada je datoteka otvorena u Excelu.
        internal static byte[] ProcitajBajtove(string putanja)
        {
            using (var s = new FileStream(putanja, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var m = new MemoryStream())
            {
                s.CopyTo(m);
                return m.ToArray();
            }
        }

        private static readonly XNamespace N = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";

        internal static XlsxList Procitaj(Stream izvor, string nazivLista = null)
        {
            using (var zip = new ZipArchive(izvor, ZipArchiveMode.Read, true))
            {
                string[] deljeni = Deljeni(zip);
                ZipArchiveEntry list = NadjiList(zip, nazivLista);
                if (list == null) throw new InvalidDataException("U datoteci nema lista za čitanje.");

                XDocument doc;
                using (Stream s = list.Open()) doc = XDocument.Load(s);
                var rezultat = new XlsxList();
                foreach (XElement c in doc.Descendants(N + "c"))
                {
                    int kol, red;
                    if (!Adresa((string)c.Attribute("r"), out kol, out red)) continue;
                    string v = Vrednost(c, deljeni);
                    if (v.Length == 0) continue;
                    Upisi(rezultat, red, kol, v);
                }
                foreach (XElement m in doc.Descendants(N + "mergeCell"))
                {
                    string[] opseg = ((string)m.Attribute("ref") ?? "").Split(':');
                    int k1, r1, k2, r2;
                    if (opseg.Length != 2 || !Adresa(opseg[0], out k1, out r1) || !Adresa(opseg[1], out k2, out r2)) continue;
                    string v = rezultat.Celija(r1, k1);
                    if (v.Length == 0) continue;
                    for (int r = r1; r <= r2; r++)
                        for (int k = k1; k <= k2; k++)
                            Upisi(rezultat, r, k, v);
                }
                return rezultat;
            }
        }

        private static void Upisi(XlsxList l, int red, int kol, string v)
        {
            Dictionary<int, string> r;
            if (!l.Redovi.TryGetValue(red, out r)) l.Redovi[red] = r = new Dictionary<int, string>();
            r[kol] = v;
            if (red > l.PoslednjiRed) l.PoslednjiRed = red;
        }

        private static string[] Deljeni(ZipArchive zip)
        {
            ZipArchiveEntry e = zip.GetEntry("xl/sharedStrings.xml");
            if (e == null) return new string[0];
            using (Stream s = e.Open())
                return XDocument.Load(s).Descendants(N + "si")
                    .Select(si => string.Concat(si.Descendants(N + "t").Select(t => t.Value)))
                    .ToArray();
        }

        private static ZipArchiveEntry NadjiList(ZipArchive zip, string naziv)
        {
            ZipArchiveEntry knjiga = zip.GetEntry("xl/workbook.xml"), veze = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (knjiga != null && veze != null)
            {
                XDocument k, v;
                using (Stream s = knjiga.Open()) k = XDocument.Load(s);
                using (Stream s = veze.Open()) v = XDocument.Load(s);
                XElement list = k.Descendants(N + "sheet").FirstOrDefault(x => naziv == null || string.Equals((string)x.Attribute("name"), naziv, StringComparison.OrdinalIgnoreCase));
                string id = list == null ? null : (string)list.Attribute(R + "id");
                XElement veza = v.Descendants(Rel + "Relationship").FirstOrDefault(x => (string)x.Attribute("Id") == id);
                if (veza != null)
                {
                    string putanja = ((string)veza.Attribute("Target")).TrimStart('/');
                    ZipArchiveEntry e = zip.GetEntry(putanja.StartsWith("xl/") ? putanja : "xl/" + putanja);
                    if (e != null) return e;
                }
            }
            return zip.GetEntry("xl/worksheets/sheet1.xml");
        }

        private static string Vrednost(XElement c, string[] deljeni)
        {
            string tip = (string)c.Attribute("t");
            if (tip == "inlineStr") return string.Concat(c.Descendants(N + "t").Select(t => t.Value)).Trim();
            string v = (string)c.Element(N + "v") ?? "";
            if (tip == "s")
            {
                int i;
                return int.TryParse(v, out i) && i >= 0 && i < deljeni.Length ? deljeni[i].Trim() : "";
            }
            return v.Trim();
        }

        // "AB12" -> kolona 28, red 12
        internal static bool Adresa(string a, out int kolona, out int red)
        {
            kolona = 0; red = 0;
            if (string.IsNullOrEmpty(a)) return false;
            int i = 0;
            while (i < a.Length && char.IsLetter(a[i]))
            {
                kolona = kolona * 26 + (char.ToUpperInvariant(a[i]) - 'A' + 1);
                i++;
            }
            return kolona > 0 && i < a.Length && int.TryParse(a.Substring(i), out red);
        }

        internal static int Kolona(string slova)
        {
            int k, r;
            return Adresa(slova + "1", out k, out r) ? k : 0;
        }
    }
}
