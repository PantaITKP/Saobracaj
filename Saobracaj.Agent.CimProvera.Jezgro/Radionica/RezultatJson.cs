using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>
    /// v3.0 (2.3): rezultat.json jedne poruke – klasifikacija po stranama, nalazi sa dokazom (pročitan tekst rubrike), polja posla sa
    /// poreklom, teretnice, odgovor (Live primaoci), PRIMER, događaji, trajanje faza, verzije, online/offline. Isti ulaz daje isti
    /// rezultat.json u alatu i agentu (osim polja vremena: „obradjeno“, „trajanje“).
    /// </summary>
    internal static class RezultatJson
    {
        public const int VerzijaSeme = 1;

        public static string VerzijaPrograma
        {
            get { var v = typeof(RezultatJson).Assembly.GetName().Version; return v == null ? "?" : v.Major + "." + v.Minor + "." + v.Build; }
        }

        /// <param name="poslovi">Polja posla po tovarnom listu (offline: predlog iz lokalnih šifarnika; online: iz upisa); može biti null.</param>
        public static JObject Napravi(UlaznaPoruka u, RezultatObrade r, PravilaSkup pravila, bool online, IEnumerable<Dogadjaj> dogadjaji,
            IEnumerable<(string list, PodaciPosla p)> poslovi = null, DateTime? obradjeno = null)
        {
            var m = u.Poruka;
            var o = new JObject
            {
                ["sema"] = VerzijaSeme,
                ["verzija"] = new JObject { ["program"] = VerzijaPrograma, ["pravila"] = pravila?.Verzija ?? "", ["citac"] = LokalniCitac.VerzijaCitaca },
                ["rezim"] = online ? "online" : "offline",
                ["obradjeno"] = (obradjeno ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["poruka"] = new JObject
                {
                    ["izvor"] = u.Izvor, ["uid"] = u.Uid, ["folder"] = u.Folder, ["message_id"] = (m.MessageId ?? "").Trim('<', '>'),
                    ["datum"] = m.Date == DateTimeOffset.MinValue ? "" : m.Date.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
                    ["od"] = PrimaociLive.Ocisti(m.From.Mailboxes.FirstOrDefault()?.Address), ["za"] = new JArray(PrimaociLive.Adrese(m.To)),
                    ["cc"] = new JArray(PrimaociLive.Adrese(m.Cc)), ["naslov"] = m.Subject ?? "",
                    // novi deo poruke (bez citata i potpisa) – osnova za događaje
                    ["telo_novo"] = Obrada.TekstPosiljaoca("", ObradaPoruke.TeloMaila(m)).Trim(),
                    ["prilozi"] = new JArray(r.Prilozi.Select(p => new JObject { ["ime"] = p.ime, ["bajtova"] = p.sadrzaj.Length, ["hes"] = Prilog.Sha256(p.sadrzaj).Substring(0, 16) })),
                },
                ["korak"] = r.Korak.ToString(), ["status"] = r.Status, ["odluka"] = Odluka(r),
            };
            var ishod = r.Ishod;
            if (ishod != null)
            {
                o["klasifikacija"] = new JObject
                {
                    ["prilozi"] = new JArray(ishod.OpisPriloga), ["preskoceno"] = new JArray(ishod.Preskoceno), ["nisu_procitani"] = new JArray(ishod.NisuProcitani),
                    ["neprepoznati"] = new JArray(ishod.Neprepoznati), ["niska_pouzdanost"] = new JArray(ishod.NisaPouzdanost),
                    ["dokumenti"] = new JArray((ishod.Provera?.Dokumenti ?? new List<Dokument>()).Select(d => new JObject
                    {
                        ["fajl"] = d.Fajl, ["tip"] = d.Tip, ["strane"] = new JArray(d.Strane), ["opis"] = d.Opis ?? "", ["izvor"] = d.Izvor ?? "", ["iz_dosijea"] = d.IzDosijea,
                    })),
                };
                var pr = ishod.Provera;
                if (pr != null)
                {
                    o["provera"] = new JObject
                    {
                        ["gresaka"] = pr.BrojGresaka, ["upozorenja"] = pr.BrojUpozorenja, ["rucna_provera"] = pr.ZaRucnuProveru,
                        ["interni_razlog"] = Izvestaj.RazlogInternog(pr, pravila) ?? "",
                        ["listovi"] = new JArray(pr.Listovi.Select(l => List(l, pravila))),
                        ["kopije_listova"] = new JArray(pr.KopijeListova.Select(l => l.Oznaka)),
                        ["cit7"] = pr.Cit7.Count,
                    };
                }
            }
            o["odgovor"] = new JArray(r.Poruke.Select(p => new JObject
            {
                ["vrsta"] = p.Vrsta, ["naslov"] = p.Naslov, ["za"] = new JArray(p.Za), ["cc"] = new JArray(p.Cc), ["live_primaoci"] = p.LivePrimaoci ?? "",
                ["telo"] = p.Telo, ["prilozi"] = new JArray(p.Prilozi),
            }));
            if (r.Primer != null)
                o["primer"] = new JObject
                {
                    ["ime"] = r.Primer.Ime, ["napravljen"] = r.Primer.Napravljen, ["razlog"] = r.Primer.Razlog ?? "", ["greska"] = r.Primer.Greska ?? "",
                    ["strana"] = r.Primer.Strane.Count, ["interno"] = new JArray(r.Primer.Interno), ["upisi"] = new JArray(r.Primer.Upisi.Select(x => x.ToString())),
                };
            if (r.Upis != null)
                o["upis"] = new JObject
                {
                    ["ishod"] = r.Upis.ToString(), ["promene"] = new JArray(r.Upis.Promene), ["napomene"] = new JArray(r.Upis.Napomene), ["interno"] = new JArray(r.Upis.Interno),
                    ["poslovi"] = new JArray(r.Upis.Poslovi.Select(p => new JObject(p.Where(kv => kv.Value != null && !(kv.Value is DBNull)).Select(kv => new JProperty(kv.Key, Vrednost(kv.Value)))))),
                };
            if (poslovi != null)
                o["poslovi"] = new JArray(poslovi.Select(x => new JObject
                {
                    ["list"] = x.list, ["kljuc"] = x.p.KljucPosiljke ?? "",
                    ["polja"] = new JObject(x.p.V.Where(kv => kv.Value != null).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new JProperty(kv.Key, Vrednost(kv.Value)))),
                    ["poreklo"] = new JArray(x.p.Poreklo.Select(po => new JObject
                    {
                        ["kolona"] = po.Kolona, ["vrednost"] = po.Vrednost == null ? null : Vrednost(po.Vrednost), ["rubrika"] = po.Rubrika, ["tekst"] = po.Tekst ?? "",
                        ["pouzdanost"] = po.Pouzdanost, ["kandidati"] = po.Kandidati ?? "", ["fajl"] = po.Fajl ?? "",
                    })),
                    ["napomene"] = new JArray(x.p.Napomene),
                }));
            o["dogadjaji"] = new JArray((dogadjaji ?? Enumerable.Empty<Dogadjaj>()).Select(d => new JObject
            {
                ["tip"] = d.Tip, ["mesto"] = d.MestoTekst, ["mesto_id"] = d.MestoID, ["vreme"] = d.Vreme?.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture) ?? "",
                ["vreme_tekst"] = d.VremeTekst, ["zona"] = d.VremenskaZona, ["vrednost"] = d.Vrednost, ["recenica"] = d.Recenica, ["jezik"] = d.Jezik,
                ["pouzdanost"] = d.Pouzdanost, ["izraz"] = d.Izraz, ["raspon"] = d.Raspon,
                ["posao"] = d.Posao ?? "", ["veza"] = d.Veza ?? "", ["kandidati"] = new JArray(d.Kandidati ?? new List<string>()),
            }));
            o["trajanje"] = new JObject(r.Trajanje.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new JProperty(kv.Key, Math.Round(kv.Value, 1))));
            if (!string.IsNullOrEmpty(r.Greska)) o["greska"] = r.Greska;
            return o;
        }

        private static JToken Vrednost(object v) => v is decimal d ? (JToken)d : v is int i ? i : v is DateTime t ? t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : (JToken)Convert.ToString(v, CultureInfo.InvariantCulture);

        /// <summary>Odluka o odgovoru jednom rečenicom (za Excel i ANALIZA.md).</summary>
        public static string Odluka(RezultatObrade r)
        {
            var vrste = r.Poruke.Select(p => p.Vrsta).ToList();
            if (r.Status == RadniProstor.StatusNeuspeh) return RadniProstor.OdlukaNeuspeha(r.Greska);
            if (r.Korak == Korak.PrivremenaGreska) return "privremena greška – ponovni pokušaj (" + r.Status + ")";
            switch (r.Status)
            {
                case "Preskoceno": return "preskočeno (sopstveni odgovor / automatska poruka)";
                case "BezPriloga": return "mail bez priloga – bez odgovora";
                case "SamoPotpis": return "nema dokumenata (samo slike iz potpisa) – bez odgovora";
                case "IstiPrilog": return "isti prilog kao ranije – bez odgovora (prijem zabeležen)";
                case "IstiList": return "isti tovarni list, nepromenjen – bez novog izveštaja";
                case "NemaCim": case "SamoSpisak": return "nema CIM/CUV – bez odgovora";
                case "NeprocitanoBezCim": return "prilozi nisu pročitani, mail ne pominje CIM – bez poruke";
                case "NeprocitanoRucnaProvera": return "prilozi nisu pročitani – interna ručna provera";
            }
            if (vrste.Contains("interni")) return "interni izveštaj (" + (r.Provera != null && r.Provera.Listovi.Any(l => l.KpRezim) ? "KP tovarni list" : "ručna provera") + ")";
            if (vrste.Contains("izvestaj")) return "izveštaj pošiljaocu" + (vrste.Contains("kopija") ? " + interna kopija" : "");
            if (vrste.Contains("kopija")) return "izveštaj samo interno (više od 25 primalaca)";
            if (r.Provera != null && r.Provera.ImaTovarniList && r.Provera.BrojGresaka + r.Provera.BrojUpozorenja == 0) return "ispravan tovarni list – bez odgovora";
            return r.Status;
        }

        private static JObject List(RezultatLista l, PravilaSkup pravila)
        {
            var c = l.Cim;
            var o = new JObject
            {
                ["oznaka"] = l.Oznaka ?? "", ["fajl"] = l.Dokument?.Fajl ?? "", ["strane"] = new JArray(l.Dokument?.Strane ?? new List<int>()),
                ["kp_rezim"] = l.KpRezim, ["rucna_provera"] = l.ZaRucnuProveru,
            };
            if (c != null)
            {
                o["obrazac"] = c.Obrazac ?? ""; o["sablon"] = c.Sablon ?? ""; o["neprepoznat"] = c.NeprepoznatObrazac ?? "";
                o["pouzdanost_strane"] = Math.Round(c.PouzdanostStrane, 2);
                o["r62"] = string.Join(" ", new[] { c.R62.Zemlja, c.R62.Stanica, c.R62.Preduzece, c.R62.Broj }.Where(x => !string.IsNullOrEmpty(x)));
                // v3.0 (e): nesigurni delovi r62 (niska pouzdanost cifara) i da li je r62 pouzdana za odluke (ključ, KP, ponavljanje)
                var nes = c.R62.NesigurniDelovi ?? new Dictionary<string, double>();
                var vrednostDela = new Dictionary<string, string> { ["zemlja"] = c.R62.Zemlja, ["stanica"] = c.R62.Stanica, ["preduzeće"] = c.R62.Preduzece, ["broj otpravljanja"] = c.R62.Broj };
                o["r62_nesigurno"] = new JArray(nes.Select(kv => new JObject { ["deo"] = kv.Key, ["vrednost"] = vrednostDela.TryGetValue(kv.Key, out var v) ? v ?? "" : "", ["pouzdanost"] = kv.Value }));
                o["r62_za_odluke"] = !c.R62.NepouzdanaZaOdluke && nes.Count == 0;
                o["rubrike"] = Rubrike(c, l.Dokument?.Izvor == "ocr");
                // v3.0.1 (2.3): samoprovera – nalazi za pošiljaoca bez pouzdanog dokaza (mora biti prazno)
                o["bez_dokaza"] = new JArray(Motor.BezDokaza(l).Select(n => n.Pravilo + " r" + n.Rubrika));
                o["pecat"] = l.Pecat ?? "";   // v3.0.4: pečat prevoznika i znak odluke
            }
            o["nalazi"] = new JArray(l.Nalazi.SelectMany(n => new[] { n }.Concat(n.Takodje)).Select(n => new JObject
            {
                ["pravilo"] = n.Pravilo, ["rubrika"] = n.Rubrika ?? "", ["tezina"] = n.Tezina, ["tekst"] = n.Tekst ?? "", ["primer"] = n.Primer ?? "",
                ["dopuna"] = n.Dopuna ?? "", ["samo_interno"] = n.SamoInterno, ["ublazeno"] = n.Ublazeno,
                // dokaz: pročitan tekst rubrike na koju se nalaz odnosi
                ["dokaz"] = n.Vrednost ?? "",
            }));
            return o;
        }

        /// <summary>Sve rubrike lista: r1 … r99 → status, pročitan tekst (sirov), pouzdanost.</summary>
        internal static JObject Rubrike(CimList c, bool sken = false)
        {
            var o = new JObject();
            foreach (var p in typeof(CimList).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => typeof(Rubrika).IsAssignableFrom(p.PropertyType)))
            {
                if (!(p.GetValue(c) is Rubrika r)) continue;
                var jo = new JObject { ["status"] = r.Status ?? "", ["tekst"] = r.Tekst ?? "", ["pouzdanost"] = Math.Round(r.Pouzdanost, 2) };
                // v3.0.1 (2.2): mera čitljivosti polja (sken, popunjena rubrika)
                var m = Citljivost.Izmeri(r, sken);
                if (m.Merena) jo["citljivost"] = new JObject { ["ukupno"] = Math.Round(m.Ukupno, 2), ["ocr"] = Math.Round(m.Ocr, 2), ["pokrivenost"] = Math.Round(m.Pokrivenost, 2), ["smisao"] = Math.Round(m.Smisao, 2), ["necitljivo"] = m.Necitljivo };
                o[p.Name.ToLowerInvariant()] = jo;
            }
            return o;
        }

        public static string U(JObject o) => o.ToString(Formatting.Indented);

        /// <summary>Isti rezultat bez polja koja zavise od vremena (za poređenje alat ↔ agent).</summary>
        public static JObject BezVremena(JObject o)
        {
            var k = (JObject)o.DeepClone();
            k.Remove("obradjeno"); k.Remove("trajanje");
            return k;
        }
    }
}
