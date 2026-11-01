using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Saobracaj.Agenti_jobs
{
    public static class ExcelRoutes
    {
        private static readonly XNamespace N = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        public static List<RouteInfo> Read(string file)
        {
            using (var stream = File.OpenRead(file))
                return Read(stream);
        }

        public static List<RouteInfo> Read(Stream source)
        {
            using (var zip = new ZipArchive(source, ZipArchiveMode.Read, true))
            {
                Func<string, XDocument> xml = name =>
                {
                    var entry = zip.GetEntry(name);
                    if (entry == null)
                        throw new InvalidDataException("Neispravan XLSX: " + name);
                    using (var stream = entry.Open())
                        return XDocument.Load(stream);
                };
                var shared = zip.GetEntry("xl/sharedStrings.xml") == null ? new string[0] : xml("xl/sharedStrings.xml").Descendants(N + "si").Select(x => string.Concat(x.Descendants(N + "t").Select(t => t.Value))).ToArray();
                var rel = xml("xl/_rels/workbook.xml.rels").Root.Elements().ToDictionary(e => (string)e.Attribute("Id"), e => (string)e.Attribute("Target"));
                var result = new List<RouteInfo>();
                foreach (var sheet in xml("xl/workbook.xml").Descendants(N + "sheet"))
                {
                    var id = (string)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
                    var target = rel[id];
                    var name = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                    foreach (var row in xml(name).Descendants(N + "row"))
                    {
                        if ((int?)row.Attribute("r") < 7)
                            continue;
                        var values = new Dictionary<string, string>();
                        foreach (var cell in row.Elements(N + "c"))
                        {
                            var col = new string(((string)cell.Attribute("r")).TakeWhile(char.IsLetter).ToArray());
                            var type = (string)cell.Attribute("t");
                            var value = (string)cell.Element(N + "v") ?? "";
                            int index;
                            if (type == "s" && int.TryParse(value, out index))
                                value = shared[index];
                            if (type == "inlineStr")
                                value = string.Concat(cell.Descendants(N + "t").Select(t => t.Value));
                            values[col] = value.Trim();
                        }

                        Func<string, string> v = key => values.ContainsKey(key) ? values[key] : "";
                        if (v("C") == "" || v("D") == "" || v("E") == "")
                            continue;
                        result.Add(new RouteInfo { From = v("C"), To = v("D"), Train = v("E"), MaxWeight = Number(v("I")), Price = Number(v("M")), Departure = Time(v("F")), Arrival = Time(v("G")), Days = v("H"), Locomotive = v("J"), Length = v("K"), LengthValue = Number(v("K")), BrakePercent = Number(v("L")), Relation = v("C") + " - " + v("D"), Note = v("N") });
                    }
                }

                if (result.Count == 0)
                    throw new InvalidDataException("Nisu pronađene trase u kolonama C–N. Podržan je priloženi XLSX raspored reda vožnje.");
                return result;
            }
        }

        private static decimal? Number(string s)
        {
            decimal d;
            return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : (decimal?)null;
        }

        private static string Time(string s)
        {
            double d;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d) && d >= 0 && d < 1 ? TimeSpan.FromDays(d).ToString(@"hh\:mm") : s;
        }
    }
}
