using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimRadionica
{
    /// <summary>
    /// Obrada radnog prostora „Testiranje“ (kartica Testiranje i komanda --testiranje): nove .msg/.eml iz foldera Testiranje → uvoz i
    /// hronološka obrada → _Analiza u folderu Testiranje → Zbirni Excel. v3.0.5: posle svake obrade paket za Claude sa mailovima ove obrade:
    /// &lt;Testiranje&gt;\_Paket\Claude\Claude_Testiranje_&lt;yyyy-MM-dd&gt;.zip (drugi paket istog dana: _2, _3 …; preko 100 MB: delovi).
    /// </summary>
    internal static class TestiranjeObrada
    {
        public const string Prostor = "Testiranje";

        internal sealed class Ishod
        {
            public int Fajlova, Novih, ZaObradu;
            public List<StavkaProstora> Neuspeli = new List<StavkaProstora>();
            public List<string> Paketi = new List<string>();
            public int Izostavljeno;
        }

        internal static Ishod Obradi(Podesavanja p, bool iNeuspele, Action<string> zapis)
        {
            var ishod = new Ishod();
            if (string.IsNullOrWhiteSpace(p.TestiranjeFolder) || !Directory.Exists(p.TestiranjeFolder)) { zapis("nema foldera Testiranje: " + p.TestiranjeFolder); return ishod; }
            var fajlovi = Directory.GetFiles(p.TestiranjeFolder).Where(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"(?i)\.(msg|eml)$")).ToList();
            ishod.Fajlova = fajlovi.Count;
            if (fajlovi.Count == 0) return ishod;
            var test = RadniProstor.Otvori(Path.Combine(p.Radni, Prostor));
            var nove = test.Uvezi(fajlovi);
            ishod.Novih = nove.Count;
            // v3.0: „Obradi sada“ obrađuje ponovo i poruke koje nisu obrađene zbog greške okruženja
            var neuspeleRanije = iNeuspele ? new HashSet<string>(test.Neuspeli(p.Verzija).Select(x => x.Kljuc)) : new HashSet<string>();
            var neobradjene = test.Hronoloski().Where(s => !s.Rezultati.ContainsKey(p.Verzija) || neuspeleRanije.Contains(s.Kljuc)).ToList();
            ishod.ZaObradu = neobradjene.Count;
            if (neobradjene.Count == 0) return ishod;
            zapis("novih: " + nove.Count + ", za obradu: " + neobradjene.Count + " (verzija " + p.Verzija + ")");
            var konekcija = p.Online ? p.Konekcija() : null;
            foreach (var deo in Enumerable.Range(0, (neobradjene.Count + p.VelicinaDela - 1) / p.VelicinaDela).Select(i => neobradjene.Skip(i * p.VelicinaDela).Take(p.VelicinaDela).ToList()))
            {
                var posao = new PosaoRadnika { Koren = test.Koren, Kljucevi = deo.Select(x => x.Kljuc).ToList(), Online = p.Online, Verzija = p.Verzija, Kes = p.Kes, Analiza = p.TestiranjeFolder };
                var javljeni = new HashSet<string>();
                var neuspeh = GlavnaForma.PokreniRadnika(posao, konekcija, red =>
                {
                    var m = System.Text.RegularExpressions.Regex.Match(red, @"^(?:OK|GRESKA) (.+?) \| ");
                    if (m.Success) lock (javljeni) javljeni.Add(m.Groups[1].Value);
                    zapis(red);
                });
                // v3.0: pad radnika – poruke bez ishoda dobijaju „nije obrađen – greška okruženja …“ (i ANALIZA.md u folderu Testiranje)
                List<StavkaProstora> bez;
                lock (javljeni) bez = RadniProstor.ZabeleziNeobradjene(test.Koren, posao.Kljucevi, javljeni, p.Verzija, neuspeh, p.Online, false);
                foreach (var x in bez) { Analiza.NapisiNeuspeh(p.TestiranjeFolder, p.Verzija, RadniProstor.Otvori(test.Koren).Rb(x), x, Path.Combine(test.Koren, x.Rezultati[p.Verzija])); zapis("NIJE OBRAĐEN: " + x.Naslov + " – " + x.Odluka); }
            }
            Analiza.ZbirnoIzFoldera(p.TestiranjeFolder, p.Verzija);
            test = RadniProstor.Otvori(test.Koren);
            Zbirno.Sacuvaj(test, p.Verzija);
            zapis("gotovo – _Analiza\\" + p.Verzija + " u " + p.TestiranjeFolder);
            ishod.Neuspeli = test.Neuspeli(p.Verzija);
            if (ishod.Neuspeli.Count > 0) zapis("NISU OBRAĐENI (" + ishod.Neuspeli.Count + "): " + string.Join("; ", ishod.Neuspeli.Select(x => x.Naslov)) + " – „Obradi sada (i neuspele)“ / --testiranje --neuspele ih obrađuje ponovo.");
            // v3.0.5: paket za Claude – samo mailovi obrađeni u ovoj obradi
            try
            {
                var rb = new HashSet<string>(neobradjene.Select(s => test.Rb(s).ToString("000", CultureInfo.InvariantCulture) + "_"));
                var izlaz = Path.Combine(p.TestiranjeFolder, "_Paket", "Claude");
                var osnova = SlobodnoIme(izlaz, "Claude_" + Prostor + "_" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                ishod.Paketi = Zbirno.PaketZaClaude(test, p.Verzija, izlaz, null, out var izost, analizaKoren: p.TestiranjeFolder, osnovaImena: osnova,
                    ukljuciMail: ime => rb.Any(x => ime.StartsWith(x, StringComparison.Ordinal)));
                ishod.Izostavljeno = izost.Count;
                foreach (var z in ishod.Paketi) zapis("paket za Claude: " + z + " (" + (new FileInfo(z).Length / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB)");
                if (izost.Count > 0) zapis("izostavljeno iz paketa (tajne): " + izost.Count);
            }
            catch (Exception ex) { zapis("paket za Claude nije napravljen: " + ex.Message); }
            return ishod;
        }

        /// <summary>„Claude_Testiranje_2026-11-20“, a ako paket tog imena (ili njegovi delovi) već postoji: „…_2“, „…_3“ …</summary>
        internal static string SlobodnoIme(string folder, string osnova)
        {
            bool Zauzeto(string o) => Directory.Exists(folder) && Directory.GetFiles(folder, o + "*.zip").Any(f =>
            {
                var n = Path.GetFileNameWithoutExtension(f);
                return n == o || n.StartsWith(o + "_deo", StringComparison.Ordinal);
            });
            if (!Zauzeto(osnova)) return osnova;
            for (int i = 2; ; i++) if (!Zauzeto(osnova + "_" + i)) return osnova + "_" + i;
        }
    }
}
