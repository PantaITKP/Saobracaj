using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// Trajno stanje na disku (StanjeFolder\stanje.json): poslednji obrađeni UID po folderu, obrađene poruke
    /// (Message-ID + heš priloga, status, broj pokušaja) i vreme poslednjih upozorenja. Posle restarta agent nastavlja gde je stao.
    /// </summary>
    internal sealed class Stanje
    {
        public sealed class FolderStanje
        {
            public long UidValidity { get; set; }
            public long PoslednjiUid { get; set; }
            /// <summary>Poruke koje su u ovaj folder stigle pre ovog trenutka se ne obrađuju (novo sanduče ili reset).</summary>
            public DateTimeOffset? NultaTacka { get; set; }
        }

        public sealed class PorukaStanje
        {
            public string Status { get; set; }
            public int Pokusaja { get; set; }
            public DateTime Vreme { get; set; }
            public string Folder { get; set; }
            public long Uid { get; set; }
            public string HesPriloga { get; set; }
            public int Greske { get; set; }
            public int Upozorenja { get; set; }
            /// <summary>Tekst poslednje greške obrade (vrsta i poruka izuzetka, bez sadržaja dokumenta).</summary>
            public string PoslednjaGreska { get; set; }
        }

        public Dictionary<string, FolderStanje> Folderi { get; set; } = new Dictionary<string, FolderStanje>();
        public Dictionary<string, PorukaStanje> Poruke { get; set; } = new Dictionary<string, PorukaStanje>();
        public Dictionary<string, DateTime> Upozorenja { get; set; } = new Dictionary<string, DateTime>();

        /// <summary>
        /// Posle komande reset-stanje: obrađuju se samo poruke koje su u sanduče stigle od ovog trenutka; starije se
        /// preskaču bez preuzimanja. Null = bez nulte tačke (čita se od ImportOd).
        /// </summary>
        public DateTimeOffset? NultaTacka { get; set; }
        /// <summary>Rezervna kopija poslednjeg reseta (stanje\arhiva\…): spisak ranije obrađenih mailova za poređenje izveštaja.</summary>
        public string ResetArhiva { get; set; }
        /// <summary>Indeks prvih izveštaja (stanje\prvi_izvestaji.json) treba sastaviti u sledećem prolazu.</summary>
        public bool PrviIzvestajiIzgraditi { get; set; }

        [Newtonsoft.Json.JsonIgnore] public string Putanja { get; private set; }

        public static Stanje Ucitaj(string folder)
        {
            Directory.CreateDirectory(folder);
            var p = Path.Combine(folder, "stanje.json");
            Stanje s = null;
            if (File.Exists(p)) s = Json.Iz<Stanje>(File.ReadAllText(p, Encoding.UTF8));
            s = s ?? new Stanje();
            s.Putanja = p;
            return s;
        }

        public void Sacuvaj()
        {
            // Čuvaju se poruke iz poslednjih 180 dana.
            foreach (var k in Poruke.Where(x => x.Value.Vreme < DateTime.Now.AddDays(-180)).Select(x => x.Key).ToList()) Poruke.Remove(k);
            var tmp = Putanja + ".tmp";
            File.WriteAllText(tmp, Json.U(this), Encoding.UTF8);
            if (File.Exists(Putanja)) File.Replace(tmp, Putanja, null);
            else File.Move(tmp, Putanja);
        }

        public FolderStanje Folder(string ime)
        {
            if (!Folderi.TryGetValue(ime, out var f)) Folderi[ime] = f = new FolderStanje();
            return f;
        }

        /// <summary>Ključ stanja po sanduču (v2.7.1): „agenti@kprevoz.co.rs|INBOX“ – sanduča se ne mešaju.</summary>
        public static string KljucSanduceta(string nalog, string folder) => (nalog ?? "").Trim().ToLowerInvariant() + "|" + folder;

        /// <summary>Ključ obrađene poruke po sanduču: „agenti@kprevoz.co.rs|&lt;Message-ID&gt;“.</summary>
        public static string KljucPoruke(string nalog, string messageId) => (nalog ?? "").Trim().ToLowerInvariant() + "|" + messageId;

        /// <summary>
        /// Stanje foldera jednog sanduča. Sanduče koje agent do sada nije čitao dobija nultu tačku = sada (stare poruke u njemu
        /// se ne obrađuju); posle reseta (nema nijednog foldera, a nulta tačka postoji) važi nulta tačka reseta.
        /// </summary>
        public FolderStanje FolderSanduceta(string nalog, string folder, DateTimeOffset sada)
        {
            var k = KljucSanduceta(nalog, folder);
            if (Folderi.TryGetValue(k, out var f)) return f;
            f = new FolderStanje { NultaTacka = Folderi.Count == 0 && NultaTacka.HasValue ? NultaTacka : sada };
            Folderi[k] = f;
            return f;
        }

        /// <summary>
        /// Isto upozorenje ide mailom najviše jednom na <paramref name="sati"/> sati. Samo provera: vreme se upisuje tek
        /// posle pokušaja slanja (<see cref="ZapisiUpozorenje"/>), da neuspelo slanje ne „potroši“ upozorenje.
        /// </summary>
        public bool SmeUpozorenje(string kljuc, int sati = 6, DateTime? sada = null)
            => !(Upozorenja.TryGetValue(kljuc, out var t) && t > (sada ?? DateTime.Now).AddHours(-sati));

        /// <summary>Posle slanja: sledeće isto upozorenje najranije za <c>sati</c>; posle neuspelog slanja ponovni pokušaj za 10 minuta.</summary>
        public void ZapisiUpozorenje(string kljuc, bool poslato, int sati, DateTime? sada = null)
        {
            var t = sada ?? DateTime.Now;
            Upozorenja[kljuc] = poslato ? t : t.AddHours(-sati).AddMinutes(10);
        }
    }
}
