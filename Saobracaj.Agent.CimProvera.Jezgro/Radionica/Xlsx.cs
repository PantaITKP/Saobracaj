using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>
    /// v3.0: minimalan .xlsx pisač (Office Open XML u zip-u, bez nove biblioteke): listovi sa redovima teksta/brojeva, podebljano
    /// zaglavlje, širine kolona, zamrznut prvi red i filter. Čitanje postojećeg fajla (ocene „Tačno?“/„Komentar“) ide preko ExcelDataReader-a.
    /// </summary>
    internal sealed class XlsxList
    {
        public string Ime = "List1";
        public List<string> Zaglavlje = new List<string>();
        public List<object[]> Redovi = new List<object[]>();
        /// <summary>Širina kolone (znakova); prazno = po sadržaju (do 60).</summary>
        public Dictionary<int, double> Sirine = new Dictionary<int, double>();
        /// <summary>Kolone sa prelamanjem teksta.</summary>
        public HashSet<int> Prelom = new HashSet<int>();
        /// <summary>Boja pozadine reda (ARGB „FFFFE0E0“) po indeksu reda (0 = prvi red podataka).</summary>
        public Dictionary<int, string> BojaReda = new Dictionary<int, string>();
    }

    internal static class Xlsx
    {
        public static void Sacuvaj(string putanja, params XlsxList[] listovi)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(putanja)));
            var tmp = putanja + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);
            var boje = listovi.SelectMany(l => l.BojaReda.Values).Distinct().ToList();
            using (var fs = new FileStream(tmp, FileMode.Create))
            using (var z = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                void Pisi(string ime, string sadrzaj)
                {
                    var e = z.CreateEntry(ime, CompressionLevel.Optimal);
                    using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(sadrzaj);
                }
                Pisi("[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>"
                    + string.Concat(listovi.Select((l, i) => @"<Override PartName=""/xl/worksheets/sheet" + (i + 1) + @".xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>")) + "</Types>");
                Pisi("_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>");
                Pisi("xl/workbook.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets>"
                    + string.Concat(listovi.Select((l, i) => @"<sheet name=""" + X(ImeLista(l.Ime)) + @""" sheetId=""" + (i + 1) + @""" r:id=""rId" + (i + 1) + @"""/>"))
                    + "</sheets>" + (listovi.Length > 0 ? "<definedNames>" + string.Concat(listovi.Select((l, i) => l.Redovi.Count > 0 && l.Zaglavlje.Count > 0
                        ? @"<definedName name=""_xlnm._FilterDatabase"" localSheetId=""" + i + @""" hidden=""1"">'" + X(ImeLista(l.Ime)).Replace("'", "''") + "'!$A$1:$" + Kolona(l.Zaglavlje.Count - 1) + "$" + (l.Redovi.Count + 1) + "</definedName>" : "")) + "</definedNames>" : "")
                    + "</workbook>");
                Pisi("xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">"
                    + string.Concat(listovi.Select((l, i) => @"<Relationship Id=""rId" + (i + 1) + @""" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet" + (i + 1) + @".xml""/>"))
                    + @"<Relationship Id=""rId" + (listovi.Length + 1) + @""" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/></Relationships>");
                // stilovi: 0 obično, 1 zaglavlje (podebljano, sivo), 2 prelom, 3+ boje reda (i 3+n prelom sa bojom)
                var sb = new StringBuilder();
                sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
                sb.Append(@"<fonts count=""2""><font><sz val=""10""/><name val=""Calibri""/></font><font><b/><sz val=""10""/><name val=""Calibri""/></font></fonts>");
                sb.Append(@"<fills count=""" + (3 + boje.Count) + @"""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FFD9D9D9""/><bgColor indexed=""64""/></patternFill></fill>"
                    + string.Concat(boje.Select(b => @"<fill><patternFill patternType=""solid""><fgColor rgb=""" + b + @"""/><bgColor indexed=""64""/></patternFill></fill>")) + "</fills>");
                sb.Append(@"<borders count=""1""><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>");
                sb.Append(@"<cellXfs count=""" + (3 + 2 * boje.Count) + @"""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyAlignment=""1""><alignment vertical=""top""/></xf>"
                    + @"<xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment vertical=""top"" wrapText=""1""/></xf>"
                    + @"<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyAlignment=""1""><alignment vertical=""top"" wrapText=""1""/></xf>"
                    + string.Concat(boje.Select((b, i) => @"<xf numFmtId=""0"" fontId=""0"" fillId=""" + (3 + i) + @""" borderId=""0"" xfId=""0"" applyFill=""1"" applyAlignment=""1""><alignment vertical=""top""/></xf>"
                        + @"<xf numFmtId=""0"" fontId=""0"" fillId=""" + (3 + i) + @""" borderId=""0"" xfId=""0"" applyFill=""1"" applyAlignment=""1""><alignment vertical=""top"" wrapText=""1""/></xf>")) + "</cellXfs>");
                sb.Append(@"<cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles></styleSheet>");
                Pisi("xl/styles.xml", sb.ToString());
                for (int i = 0; i < listovi.Length; i++) Pisi("xl/worksheets/sheet" + (i + 1) + ".xml", List(listovi[i], boje));
            }
            if (File.Exists(putanja)) File.Delete(putanja);
            File.Move(tmp, putanja);
        }

        private static string ImeLista(string ime)
        {
            var t = new string((ime ?? "List").Where(c => "[]:*?/\\".IndexOf(c) < 0).ToArray());
            return t.Length == 0 ? "List" : t.Length > 31 ? t.Substring(0, 31) : t;
        }

        private static string List(XlsxList l, List<string> boje)
        {
            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">");
            sb.Append(@"<sheetViews><sheetView workbookViewId=""0""><pane ySplit=""1"" topLeftCell=""A2"" activePane=""bottomLeft"" state=""frozen""/></sheetView></sheetViews>");
            var brojKolona = Math.Max(l.Zaglavlje.Count, l.Redovi.Select(r => r.Length).DefaultIfEmpty(0).Max());
            if (brojKolona > 0)
            {
                sb.Append("<cols>");
                for (int k = 0; k < brojKolona; k++)
                {
                    double w;
                    if (!l.Sirine.TryGetValue(k, out w))
                    {
                        var dz = Math.Max(k < l.Zaglavlje.Count ? (l.Zaglavlje[k] ?? "").Length : 0, l.Redovi.Take(500).Select(r => k < r.Length ? Tekst(r[k]).Split('\n').Select(x => x.Length).DefaultIfEmpty(0).Max() : 0).DefaultIfEmpty(0).Max());
                        w = Math.Min(60, Math.Max(8, dz + 2));
                    }
                    sb.Append(@"<col min=""" + (k + 1) + @""" max=""" + (k + 1) + @""" width=""" + w.ToString("0.0", CultureInfo.InvariantCulture) + @""" customWidth=""1""/>");
                }
                sb.Append("</cols>");
            }
            sb.Append("<sheetData>");
            int red = 1;
            if (l.Zaglavlje.Count > 0)
            {
                sb.Append(@"<row r=""1"">");
                for (int k = 0; k < l.Zaglavlje.Count; k++) sb.Append(Celija(k, 1, l.Zaglavlje[k], 1));
                sb.Append("</row>");
                red = 2;
            }
            for (int i = 0; i < l.Redovi.Count; i++, red++)
            {
                sb.Append(@"<row r=""" + red + @""">");
                var boja = l.BojaReda.TryGetValue(i, out var b) ? boje.IndexOf(b) : -1;
                for (int k = 0; k < l.Redovi[i].Length; k++)
                {
                    var prelom = l.Prelom.Contains(k);
                    var stil = boja >= 0 ? 3 + 2 * boja + (prelom ? 1 : 0) : prelom ? 2 : 0;
                    sb.Append(Celija(k, red, l.Redovi[i][k], stil));
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");
            if (l.Zaglavlje.Count > 0 && l.Redovi.Count > 0) sb.Append(@"<autoFilter ref=""A1:" + Kolona(l.Zaglavlje.Count - 1) + (l.Redovi.Count + 1) + @"""/>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static string Celija(int k, int red, object v, int stil)
        {
            var adr = Kolona(k) + red;
            var s = stil > 0 ? @" s=""" + stil + @"""" : "";
            if (v == null) return stil > 0 ? @"<c r=""" + adr + @"""" + s + "/>" : "";
            if (v is int || v is long || v is double || v is decimal || v is float)
                return @"<c r=""" + adr + @"""" + s + "><v>" + Convert.ToString(v, CultureInfo.InvariantCulture) + "</v></c>";
            var t = Tekst(v);
            if (t.Length > 32000) t = t.Substring(0, 32000) + "…";
            return @"<c r=""" + adr + @"""" + s + @" t=""inlineStr""><is><t xml:space=""preserve"">" + X(t) + "</t></is></c>";
        }

        private static string Tekst(object v) => v is DateTime d ? d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";

        /// <summary>XML tekst bez nedozvoljenih kontrolnih znakova.</summary>
        private static string X(string s) => SecurityElement.Escape(new string((s ?? "").Where(c => c == '\t' || c == '\n' || c == '\r' || c >= 0x20 && c != 0xFFFE && c != 0xFFFF).ToArray())) ?? "";

        internal static string Kolona(int k)
        {
            var s = "";
            k++;
            while (k > 0) { var r = (k - 1) % 26; s = (char)('A' + r) + s; k = (k - 1) / 26; }
            return s;
        }

        /// <summary>Čitanje lista (prvi red = zaglavlje) – za prenos ocena „Tačno?“ i „Komentar“ pri ponovnoj obradi.</summary>
        public static List<Dictionary<string, string>> Procitaj(string putanja, string imeLista = null)
        {
            var rez = new List<Dictionary<string, string>>();
            if (!File.Exists(putanja)) return rez;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var fs = new FileStream(putanja, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var r = ExcelDataReader.ExcelReaderFactory.CreateReader(fs))
            {
                var ds = ExcelDataReader.ExcelDataReaderExtensions.AsDataSet(r);
                var t = imeLista != null ? ds.Tables.Cast<DataTable>().FirstOrDefault(x => x.TableName == imeLista) : ds.Tables.Count > 0 ? ds.Tables[0] : null;
                if (t == null || t.Rows.Count == 0) return rez;
                var zag = t.Columns.Cast<DataColumn>().Select(c => Convert.ToString(t.Rows[0][c]) ?? "").ToList();
                for (int i = 1; i < t.Rows.Count; i++)
                {
                    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int k = 0; k < zag.Count; k++) if (zag[k].Length > 0 && !d.ContainsKey(zag[k])) d[zag[k]] = Convert.ToString(t.Rows[i][k], CultureInfo.InvariantCulture) ?? "";
                    rez.Add(d);
                }
            }
            return rez;
        }
    }
}
