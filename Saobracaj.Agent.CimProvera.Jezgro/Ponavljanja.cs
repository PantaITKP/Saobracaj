using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// v2.8.4 (6): ponavljanja – isti prilog (isti heš) i isti tovarni list (ista r62, ili isti pošiljalac + r8 + relacija, sa
    /// nepromenjenim sadržajem rubrika) se ne proveravaju i ne prijavljuju ponovo. Čuva se u StanjeFolder\ponavljanja.json
    /// (reset ga briše kao i ostalo stanje). Bez sadržaja dokumenata: samo heševi, oznaka maila (UID) i vreme.
    /// </summary>
    internal sealed class Ponavljanja
    {
        public const string Ime = "ponavljanja.json";
        /// <summary>Prilozi manji od ovoga (logo, slika iz potpisa) ne određuju da li je mail već obrađen.</summary>
        public const int MinBajtova = 10 * 1024;
        public const int Dana = 30;

        public sealed class Zapis
        {
            public string Oznaka { get; set; }
            public DateTime Vreme { get; set; }
            public string Ishod { get; set; }
        }

        public sealed class ZapisLista
        {
            public string Oznaka { get; set; }
            public DateTime Vreme { get; set; }
            /// <summary>Heš sadržaja rubrika (normalizovan tekst) – promena = nova verzija lista.</summary>
            public string Otisak { get; set; }
        }

        public Dictionary<string, Zapis> Prilozi { get; set; } = new Dictionary<string, Zapis>();
        public Dictionary<string, ZapisLista> Listovi { get; set; } = new Dictionary<string, ZapisLista>();

        [Newtonsoft.Json.JsonIgnore] private string putanja;

        public static Ponavljanja Ucitaj(string folder)
        {
            var p = Path.Combine(folder ?? ".", Ime);
            Ponavljanja s = null;
            try { if (File.Exists(p)) s = Json.Iz<Ponavljanja>(File.ReadAllText(p, Encoding.UTF8)); } catch { s = null; }
            s = s ?? new Ponavljanja();
            s.putanja = p;
            return s;
        }

        public void Sacuvaj()
        {
            var granica = DateTime.Now.AddDays(-Dana);
            foreach (var k in Prilozi.Where(x => x.Value.Vreme < granica).Select(x => x.Key).ToList()) Prilozi.Remove(k);
            foreach (var k in Listovi.Where(x => x.Value.Vreme < granica).Select(x => x.Key).ToList()) Listovi.Remove(k);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(putanja)));
            var tmp = putanja + ".tmp";
            File.WriteAllText(tmp, Json.U(this), Encoding.UTF8);
            if (File.Exists(putanja)) File.Replace(tmp, putanja, null); else File.Move(tmp, putanja);
        }

        private static string Hes(byte[] b) => Prilog.Sha256(b).Substring(0, 24);

        // ------------------------------------------------------------------------------------------- 6.1 isti prilog

        /// <summary>
        /// Raniji zapis kada su SVI značajni prilozi maila (≥ 10 KB) već provereni („1849_261114093806_001.pdf“ od trainhungary.hu
        /// pa od rss.sk); null kada je bar jedan prilog nov ili mail nema značajnih priloga.
        /// </summary>
        public Zapis IstiPrilozi(IEnumerable<(string ime, byte[] sadrzaj)> prilozi)
        {
            var znacajni = prilozi.Where(p => p.sadrzaj != null && p.sadrzaj.Length >= MinBajtova).ToList();
            if (znacajni.Count == 0) return null;
            Zapis prvi = null;
            foreach (var p in znacajni)
            {
                if (!Prilozi.TryGetValue(Hes(p.sadrzaj), out var z)) return null;
                if (prvi == null || z.Vreme < prvi.Vreme) prvi = z;
            }
            return prvi;
        }

        public void ZapamtiPriloge(IEnumerable<(string ime, byte[] sadrzaj)> prilozi, string oznaka, string ishod, DateTime vreme)
        {
            foreach (var p in prilozi.Where(x => x.sadrzaj != null && x.sadrzaj.Length >= MinBajtova))
            {
                var h = Hes(p.sadrzaj);
                if (!Prilozi.ContainsKey(h)) Prilozi[h] = new Zapis { Oznaka = oznaka, Vreme = vreme, Ishod = ishod };
            }
        }

        // ------------------------------------------------------------------------------------------- 6.2 isti tovarni list

        private static string N(string s) => Regex.Replace(PraviloKp.Normalizuj(s ?? ""), @"\s+", " ").Trim();
        private static string Cif(string s) => Regex.Replace(s ?? "", @"\D", "");

        /// <summary>
        /// Ključ lista: r62 (zemlja + stanica + preduzeće + broj otpravljanja) kada je cela upisana; inače pošiljalac (r1) + r8 +
        /// relacija (r16 → r10). null kada list nema dovoljno podataka (neprepoznat obrazac, prazne rubrike).
        /// </summary>
        public static string KljucLista(CimList c)
        {
            if (c == null || !string.IsNullOrEmpty(c.NeprepoznatObrazac)) return null;
            var r = c.R62;
            // v2.8.4: broj otpravljanja može biti i oznaka („HER-SAD79“ u „CIM Novi Sad via Kelebia (NEU).xlsx“) – slova i cifre
            var broj = Regex.Replace(PraviloKp.Normalizuj(r?.Broj ?? ""), @"[^a-z0-9]", "");
            // v2.8.4: nečitljiva r62 (rukom upisana na skenu) se ne koristi za ključ (ponavljanje, dosije)
            if (r != null && !r.NepouzdanaZaOdluke && (r.NesigurniDelovi?.Count ?? 0) == 0 && Cif(r.Zemlja).Length == 2 && Cif(r.Stanica).Length >= 5 && Cif(r.Preduzece).Length == 4 && (Cif(r.Broj).Length >= 5 || broj.Length >= 4))
                return "r62:" + Cif(r.Zemlja) + "-" + Cif(r.Stanica) + "-" + Cif(r.Preduzece) + "-" + (Cif(r.Broj).Length >= 5 ? Cif(r.Broj) : broj);
            var posiljalac = N(c.R1?.Naziv);
            var r8 = N(Motor.RubrikaLista(c, "8")?.Tekst);
            var od = N(Motor.RubrikaLista(c, "16")?.Tekst);
            var doo = N(Motor.RubrikaLista(c, "10")?.Tekst);
            if (posiljalac.Length >= 3 && r8.Length >= 3 && (od.Length >= 3 || doo.Length >= 3)) return "list:" + posiljalac + "|" + r8 + "|" + od + "|" + doo;
            // poslednje: pošiljalac + sadržaj rubrika (isti fajl → isti ključ; ime fajla, vreme i redosled priloga ne ulaze)
            var sadrzaj = N(Motor.RubrikaLista(c, "1")?.Tekst);
            return sadrzaj.Length >= 3 ? "sadrzaj:" + Otisak(c) : null;
        }

        private static readonly string[] RubrikeOtiska = { "1", "4", "7", "8", "9", "10", "12", "13", "14", "16", "17", "18", "20", "21", "24", "25", "29", "30", "57", "58a", "62" };

        /// <summary>Otisak sadržaja rubrika (normalizovan tekst) – isti fajl daje isti otisak; promena rubrike = nova verzija.</summary>
        public static string Otisak(CimList c) => Prilog.Sha256(Encoding.UTF8.GetBytes(SadrzajOtiska(c))).Substring(0, 24);

        /// <summary>Normalizovan tekst rubrika od kog se računa otisak (dijagnostika: CIMPROVERA_DBG).</summary>
        internal static string SadrzajOtiska(CimList c)
        {
            var sb = new StringBuilder();
            foreach (var id in RubrikeOtiska) sb.Append(id).Append('=').Append(N(Motor.RubrikaLista(c, id)?.Tekst)).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Raniji zapis kada su SVI pročitani listovi u rezultatu već prijavljeni sa istim sadržajem (HER-SAD 79 13.11. i „ARR – Aviso“
        /// 14.11.); null kada je bar jedan list nov ili promenjen, ili rezultat nema list sa ključem.
        /// </summary>
        public ZapisLista IstiListovi(RezultatProvere r)
        {
            var listovi = r.Listovi.Where(l => l.Cim != null).ToList();
            if (listovi.Count == 0) return null;
            ZapisLista prvi = null;
            foreach (var l in listovi)
            {
                var k = KljucLista(l.Cim);
                if (k == null || !Listovi.TryGetValue(k, out var z) || z.Otisak != Otisak(l.Cim)) return null;
                if (prvi == null || z.Vreme < prvi.Vreme) prvi = z;
            }
            return prvi;
        }

        public void ZapamtiListove(RezultatProvere r, string oznaka, DateTime vreme)
        {
            foreach (var l in r.Listovi.Where(x => x.Cim != null))
            {
                var k = KljucLista(l.Cim);
                if (k == null) continue;
                var o = Otisak(l.Cim);
                // zapis se menja samo kada se sadržaj promenio (nova verzija); isti sadržaj zadržava prvi mail
                if (!Listovi.TryGetValue(k, out var z) || z.Otisak != o) Listovi[k] = new ZapisLista { Oznaka = oznaka, Vreme = vreme, Otisak = o };
            }
        }
    }
}
