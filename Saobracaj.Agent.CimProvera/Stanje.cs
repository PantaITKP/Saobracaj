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
        }

        public Dictionary<string, FolderStanje> Folderi { get; set; } = new Dictionary<string, FolderStanje>();
        public Dictionary<string, PorukaStanje> Poruke { get; set; } = new Dictionary<string, PorukaStanje>();
        public Dictionary<string, DateTime> Upozorenja { get; set; } = new Dictionary<string, DateTime>();

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

        /// <summary>Isto upozorenje ide mailom najviše jednom na <paramref name="sati"/> sati.</summary>
        public bool SmeUpozorenje(string kljuc, int sati = 6)
        {
            if (Upozorenja.TryGetValue(kljuc, out var t) && t > DateTime.Now.AddHours(-sati)) return false;
            Upozorenja[kljuc] = DateTime.Now;
            return true;
        }
    }
}
