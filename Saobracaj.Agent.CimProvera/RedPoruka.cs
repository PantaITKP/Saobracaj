using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// Red novih poruka jednog foldera. Svaka poruka se obrađuje posebno: greška (i izuzetak) u jednoj poruci ne zaustavlja
    /// ostale. Pre obrade se u log upisuju UID, Message-ID, pošiljalac, naslov i imena priloga. Posle <c>max</c> neuspelih
    /// pokušaja poruka dobija status „greška – ručna provera“, jednom se šalje interno obaveštenje i više se ne pokušava.
    /// Poslednji obrađeni UID foldera se ne pomera preko poruke koja čeka ponovni pokušaj.
    /// </summary>
    internal sealed class RedPoruka
    {
        public const string StatusPrivremena = "PrivremenaGreska";
        public const string StatusRucna = "GreskaRucnaProvera";

        public sealed class Stavka
        {
            public long Uid;
            public string Folder = "";
            public string Kljuc = "";
            public string MessageId = "";
            public string Od = "";
            public string Naslov = "";
            public List<string> Prilozi = new List<string>();
            /// <summary>v2.8.3: broj PDF/slika priloga i njihova ukupna veličina (iz BodyStructure) – procena trajanja OCR-a.</summary>
            public int PdfSlika;
            public long Bajtova;
            /// <summary>Mail sa mnogo skeniranih strana (npr. 8 PDF priloga, ~60 strana OCR-a) – obrađuje se posle ostalih u krugu.</summary>
            public bool Tezak => PdfSlika >= TezakPdf || Bajtova >= TezakBajtova;
        }

        /// <summary>v2.8.3: prag „teškog“ maila – obrađuje se posle ostalih da ne blokira mailove iza sebe (mail > 10 min).</summary>
        public const int TezakPdf = 6;
        public const long TezakBajtova = 8L * 1024 * 1024;

        /// <summary>
        /// v2.8.3: redosled obrade – laki mailovi po UID-u, pa teški (po UID-u). Teški mail ne zadržava ostale iz istog kruga.
        /// </summary>
        public static List<Stavka> Redosled(IEnumerable<Stavka> red) =>
            red.Select((s, i) => (s, i)).OrderBy(x => x.s.Tezak ? 1 : 0).ThenBy(x => x.i).Select(x => x.s).ToList();

        internal static string Trajanje(TimeSpan t) =>
            t.TotalMinutes >= 1 ? (int)t.TotalMinutes + " min " + t.Seconds + " s" : Math.Max(0, (int)Math.Round(t.TotalSeconds)) + " s";

        private readonly Stanje stanje;
        private readonly Stanje.FolderStanje fs;
        private readonly int max;
        private readonly Log log;
        private readonly bool dryRun;

        /// <summary>Posle poslednjeg neuspelog pokušaja (jednom po poruci): interno obaveštenje, nikad pošiljaocu.</summary>
        public Action<Stavka, Stanje.PorukaStanje> PosleZadnjegPokusaja;

        public RedPoruka(Stanje stanje, Stanje.FolderStanje fs, int maxPokusaja, Log log, bool dryRun)
        {
            this.stanje = stanje; this.fs = fs; max = Math.Max(1, maxPokusaja); this.log = log; this.dryRun = dryRun;
        }

        /// <summary>Obrađuje red; vraća broj poruka za koje je poslat izveštaj.</summary>
        public int Obradi(IEnumerable<Stavka> red, Func<Stavka, Stanje.PorukaStanje, Korak> obradi)
        {
            int n = 0;
            var lista = red.ToList();
            // v2.8.3: poslednji UID se pomera samo preko neprekinutog niza ZAVRŠENIH poruka (po UID-u): teški mail koji se obrađuje
            // posle ostalih, ili poruka koja čeka ponovni pokušaj, nikad se ne preskače u sledećem krugu.
            var uidovi = lista.Select(x => x.Uid).Distinct().OrderBy(x => x).ToList();
            var zavrsene = new HashSet<long>();
            void Pomeri()
            {
                foreach (var u in uidovi) { if (!zavrsene.Contains(u)) break; fs.PoslednjiUid = Math.Max(fs.PoslednjiUid, u); }
            }
            var odlozeni = lista.Where(x => x.Tezak).ToList();
            if (odlozeni.Count > 0 && odlozeni.Count < lista.Count)
                foreach (var t in odlozeni)
                    log?.Info("  UID " + t.Uid + ": veliki mail (" + t.PdfSlika + " PDF/slika, " + (t.Bajtova / 1048576.0).ToString("0.0") + " MB) – obrađuje se posle ostalih u ovom krugu.");
            foreach (var s in Redosled(lista))
            {
                stanje.Poruke.TryGetValue(s.Kljuc, out var ps);
                if (ps != null && ps.Status != StatusPrivremena)
                {
                    log?.Info("  UID " + s.Uid + ": već obrađeno (" + ps.Status + ", " + ps.Folder + ")");
                    zavrsene.Add(s.Uid); Pomeri();
                    continue;
                }
                ps = ps ?? new Stanje.PorukaStanje();
                ps.Folder = s.Folder; ps.Uid = s.Uid; ps.Vreme = DateTime.Now;
                // v3.0: prethodno pokretanje je počelo obradu ove poruke i nije je završilo – proces je pao (blokiran DLL, prekid zadatka…).
                // Pokušaj je već uračunat; posle MaxPokusaja poruka ide na ručnu proveru uz interno obaveštenje (nikad tiho ne ispada).
                if (ps.UToku)
                {
                    ps.UToku = false;
                    ps.Pokusaja++;
                    ps.PoslednjaGreska = "obrada prekinuta – proces agenta je pao tokom obrade (greška okruženja, npr. blokiran DLL, ili prekid zadatka)"
                                         + (string.IsNullOrEmpty(ps.PoslednjaGreska) ? "" : "; ranije: " + ps.PoslednjaGreska);
                    log?.Upozorenje("  UID " + s.Uid + ": prethodna obrada nije završena (pokušaj " + ps.Pokusaja + "/" + max + ") – " + ps.PoslednjaGreska);
                    if (ps.Pokusaja >= max)
                    {
                        ps.Status = StatusRucna;
                        stanje.Poruke[s.Kljuc] = ps;
                        log?.Upozorenje("  UID " + s.Uid + ": posle " + ps.Pokusaja + " pokušaja – ručna provera (više se ne pokušava).");
                        try { PosleZadnjegPokusaja?.Invoke(s, ps); }
                        catch (Exception ex) { log?.Greska("  UID " + s.Uid + ": interno obaveštenje nije poslato: " + ex.Message); }
                        zavrsene.Add(s.Uid); Pomeri();
                        Sacuvaj();
                        continue;
                    }
                }
                // v3.0: „u toku“ se upisuje PRE obrade (i ostaje dok obrada traje) – pad procesa ostavlja trag (vidi gore). Nova poruka dobija
                // status privremene greške, da je posle pada sledeće pokretanje ne bi smatralo obrađenom.
                ps.UToku = true;
                if (string.IsNullOrEmpty(ps.Status)) ps.Status = StatusPrivremena;
                stanje.Poruke[s.Kljuc] = ps; Sacuvaj();
                log?.Info("  UID " + s.Uid + " – obrada: Message-ID " + (s.MessageId.Length > 0 ? s.MessageId : "(nema)") + ", od " + s.Od + ", naslov „" + s.Naslov + "“, prilozi: "
                          + (s.Prilozi.Count == 0 ? "nema" : string.Join(", ", s.Prilozi)) + (ps.Pokusaja > 0 ? " (pokušaj " + (ps.Pokusaja + 1) + "/" + max + ")" : ""));

                Korak k;
                var sat = Stopwatch.StartNew();
                try { k = obradi(s, ps); }
                catch (Exception ex)
                {
                    log?.Greska("  UID " + s.Uid + ": obrada poruke nije uspela", ex);
                    ps.PoslednjaGreska = Stanje.GreskaOkruzenja(ex) ?? ex.GetType().Name + ": " + ex.Message;
                    k = Korak.PrivremenaGreska;
                }
                sat.Stop();
                ps.UToku = false;
                // v2.8.3: trajanje obrade svakog maila
                log?.Info("  UID " + s.Uid + ": trajanje obrade " + Trajanje(sat.Elapsed) + (sat.Elapsed.TotalMinutes >= 10 ? " (duže od 10 min)" : ""));

                if (k == Korak.PrivremenaGreska)
                {
                    ps.Pokusaja++;
                    if (ps.Pokusaja < max)
                    {
                        ps.Status = StatusPrivremena;
                        stanje.Poruke[s.Kljuc] = ps; Sacuvaj();
                        log?.Upozorenje("  UID " + s.Uid + ": greška (pokušaj " + ps.Pokusaja + "/" + max + "), ponovo u sledećem pokretanju; ostale poruke se obrađuju.");
                        continue;
                    }
                    ps.Status = StatusRucna;
                    stanje.Poruke[s.Kljuc] = ps;
                    log?.Upozorenje("  UID " + s.Uid + ": posle " + ps.Pokusaja + " pokušaja – greška, ručna provera (više se ne pokušava). Greška: " + ps.PoslednjaGreska);
                    try { PosleZadnjegPokusaja?.Invoke(s, ps); }
                    catch (Exception ex) { log?.Greska("  UID " + s.Uid + ": interno obaveštenje nije poslato: " + ex.Message); }
                }
                else if (k == Korak.Obradjena) n++;

                stanje.Poruke[s.Kljuc] = ps;
                zavrsene.Add(s.Uid); Pomeri();
                Sacuvaj();
            }
            return n;
        }

        private void Sacuvaj() { if (!dryRun && stanje.Putanja != null) stanje.Sacuvaj(); }
    }
}
