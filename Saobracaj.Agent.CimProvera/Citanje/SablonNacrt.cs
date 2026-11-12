using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Šablon novog obrasca iz jednog uzorka (PDF sa tekstom ili sken), u dva koraka:
    ///  1) nacrt: linije okvira, zatvorene ćelije (c1, c2…), prazni kvadratići (k1, k2…), krugovi i tekstualna sidra,
    ///     sa slikom na kojoj su ćelije i kvadratići numerisani;
    ///  2) sa mapom (--mapa): rubrika = jedna ili više ćelija (ili pravougaonik), polje = kvadratić; broj rubrike u krugu
    ///     se nalazi sam (krug uz gornji levi ugao rubrike). Rezultat se proverava na slici i kopira u ObrasciFolder.
    /// </summary>
    internal static class SablonNacrt
    {
        public static JObject Napravi(Strana s, CimSablon recnik, string id, string naziv, string uzorak, JObject mapa, List<string> napomene)
        {
            var r = s.Raster ?? throw new InvalidOperationException("strana nema sliku (raster)");
            var vod = r.Vodoravne.Where(l => l.Duzina >= 0.03).ToList();
            var usp = r.Uspravne.Where(l => l.Duzina >= 0.02).ToList();
            var celije = r.Celije();
            var kvadratici = r.Kvadratici();
            var krugovi = r.Krugovi();

            double[] Oblast(JToken t, bool kvadratic)
            {
                if (t is JArray a && a.Count == 4 && a.All(x => x.Type == JTokenType.Float || x.Type == JTokenType.Integer)) return a.Select(x => (double)x).ToArray();
                var delovi = (t is JArray aa ? aa.Select(x => (string)x) : new[] { (string)t }).ToList();
                var oblici = new List<Oblik>();
                foreach (var d in delovi)
                {
                    var m = Regex.Match(d ?? "", @"^([ck])(\d+)$");
                    if (!m.Success) throw new ArgumentException("nepoznata oznaka u mapi: " + d);
                    var lista = m.Groups[1].Value == "c" ? celije : kvadratici;
                    var i = int.Parse(m.Groups[2].Value) - 1;
                    if (i < 0 || i >= lista.Count) throw new ArgumentException("nema " + d);
                    oblici.Add(lista[i]);
                }
                return new[] { R(oblici.Min(o => o.X0)), R(oblici.Min(o => o.Y0)), R(oblici.Max(o => o.X1)), R(oblici.Max(o => o.Y1)) };
            }

            var rubrike = new JObject(); var polja = new JObject(); var kucice = new JObject(); var brojevi = new JObject();
            if (mapa != null)
            {
                foreach (var p in ((JObject)mapa["rubrike"] ?? new JObject()).Properties()) rubrike[p.Name] = new JArray(Oblast(p.Value, false));
                foreach (var p in ((JObject)mapa["polja"] ?? new JObject()).Properties()) polja[p.Name] = new JArray(Oblast(p.Value, true));
                foreach (var p in ((JObject)mapa["kucice"] ?? new JObject()).Properties()) kucice[p.Name] = new JArray(Oblast(p.Value, false));
                // broj rubrike: krug uz gornji levi ugao rubrike
                foreach (var p in rubrike.Properties().Where(x => Regex.IsMatch(x.Name, @"^\d+[ab]?$")))
                {
                    var b = p.Value.Select(x => (double)x).ToArray();
                    var k = krugovi.Where(o => o.Cx > b[0] - 0.005 && o.Cx < b[0] + 0.05 && o.Cy > b[1] - 0.005 && o.Cy < b[1] + 0.035)
                        .OrderBy(o => Math.Abs(o.Cx - b[0] - 0.012) + Math.Abs(o.Cy - b[1] - 0.012)).FirstOrDefault();
                    if (k != null) brojevi[p.Name] = new JArray(R(k.Cx), R(k.Cy));
                    else napomene.Add("rubrika " + p.Name + ": krug sa brojem nije nađen uz gornji levi ugao");
                }
                if (mapa["brojevi"] is JObject rb) foreach (var p in rb.Properties()) brojevi[p.Name] = p.Value;
            }

            // Osnova mastila prazne rubrike/kućice (odštampani natpisi koje OCR ne pročita, sitni okviri): meri se na uzorku
            // za rubrike u kojima na uzorku nema podataka; u radu se rubrika smatra popunjenom/nečitljivom tek iznad te osnove.
            var praznoMastilo = new JObject();
            if (mapa != null)
            {
                var privremeni = new JObject { ["id"] = id, ["rubrike"] = rubrike, ["kucice"] = kucice, ["visina_natpisa"] = mapa["visina_natpisa"] };
                var obrazac = ObrazacSablon.IzJson(privremeni);
                var potpisiN = recnik.PotpisiNaziva(s.Reci);
                bool Natpis(Rec w) => recnik.JeNaziv(w, potpisiN) || (obrazac.VisinaNatpisa > 0 && w.Velicina == 0 && w.Y1 - w.Y0 < obrazac.VisinaNatpisa && Regex.IsMatch(w.T, @"[A-ZА-ЯĐŠČĆŽbdfhkltđćčšžбдфђћ]"));
                var bez = new HashSet<string>(((JArray)mapa["bez_osnove"] ?? new JArray()).Select(x => (string)x));
                var rez = new CimSablon.Rezultat { Raster = r, Obrazac = obrazac, Poravnanje = new Poravnanje { Obrazac = id } };
                foreach (var w in s.Reci) rez.Dodela.Add((w, obrazac.RubrikaZa(w.Cx, w.Cy), Natpis(w)));
                bool ImaPodataka(Func<Rec, bool> u) => rez.Dodela.Any(d => !d.naziv && u(d.rec) && Regex.IsMatch(d.rec.T, @"[\p{L}\p{N}]{2,}|\d"));
                foreach (var p in rubrike.Properties().Where(x => !bez.Contains(x.Name)))
                    if (!ImaPodataka(w => rez.Dodela.Any(d => ReferenceEquals(d.rec, w) && d.rubrika == p.Name)) && rez.Mastilo(p.Name) is double m && m > 0)
                        praznoMastilo[p.Name] = Math.Round(m, 4);
                foreach (var p in kucice.Properties().Where(x => !bez.Contains("#" + x.Name)))
                {
                    var o = p.Value.Select(x => (double)x).ToArray();
                    if (!ImaPodataka(w => w.Cx >= o[0] && w.Cx <= o[2] && w.Cy >= o[1] && w.Cy <= o[3] && Regex.IsMatch(w.T, @"\d")) && rez.MastiloKucica(p.Name) is double m && m > 0)
                        praznoMastilo["#" + p.Name] = Math.Round(m, 4);
                }
            }

            // tekstualna sidra: odštampani nazivi rubrika (font naziva ili rečnik), reči od 4+ znaka koje se javljaju jednom
            var potpisi = recnik.PotpisiNaziva(s.Reci);
            var pojave = s.Reci.GroupBy(w => CimSablon.Norm(w.T)).ToDictionary(g => g.Key, g => g.Count());
            var sidra = new JArray();
            foreach (var w in s.Reci.Where(w => recnik.JeNaziv(w, potpisi)).OrderBy(w => w.Cy).ThenBy(w => w.Cx))
            {
                var n = CimSablon.Norm(w.T);
                if (n.Length < 4 || pojave[n] != 1 || Regex.IsMatch(n, @"^\d+$")) continue;
                sidra.Add(new JArray(w.T, R(w.Cx), R(w.Cy)));
            }

            napomene.Add("ćelija " + celije.Count + ", kvadratića " + kvadratici.Count + ", krugova " + krugovi.Count + ", sidara " + sidra.Count
                         + ", linija " + vod.Count + " vodoravnih / " + usp.Count + " uspravnih" + (mapa != null ? ", rubrika " + rubrike.Count + ", brojeva " + brojevi.Count : ""));
            var j = new JObject
            {
                ["id"] = (string)mapa?["id"] ?? id,
                ["naziv"] = (string)mapa?["naziv"] ?? naziv ?? id,
                ["verzija"] = (int?)mapa?["verzija"] ?? 1,
                ["uzorak"] = uzorak,
                ["rubrike"] = rubrike,
                ["polja"] = polja,
                ["kucice"] = kucice,
                ["brojevi"] = brojevi,
                ["sidra"] = sidra,
                ["vodoravne"] = new JArray(vod.Select(l => new JArray(R(l.A), R(l.B0), R(l.B1)))),
                ["uspravne"] = new JArray(usp.Select(l => new JArray(R(l.A), R(l.B0), R(l.B1)))),
                ["poledjina"] = mapa?["poledjina"] ?? new JArray(),
                ["visina_natpisa"] = mapa?["visina_natpisa"] ?? 0,
                ["prazno_mastilo"] = praznoMastilo,
            };
            if (mapa == null)
            {
                j["napomena"] = "NACRT: na slici su ćelije (c…) i kvadratići (k…). Napraviti mapu {\"id\":…,\"rubrike\":{\"1\":[\"c5\"],…},\"polja\":{\"30cim\":\"k2\",…},\"kucice\":{\"16\":[x0,y0,x1,y1]}} i ponoviti sa --mapa.";
                j["celije"] = new JObject(celije.Select((o, i) => new JProperty("c" + (i + 1), new JArray(R(o.X0), R(o.Y0), R(o.X1), R(o.Y1)))));
                j["kvadratici"] = new JObject(kvadratici.Select((o, i) => new JProperty("k" + (i + 1), new JArray(R(o.X0), R(o.Y0), R(o.X1), R(o.Y1)))));
            }
            return j;
        }

        private static double R(double v) => Math.Round(v, 4);
    }
}
