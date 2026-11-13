using System;
using System.Collections.Generic;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Ishod obrade jedne poruke.</summary>
    internal enum Korak
    {
        /// <summary>Poruka je završena bez izveštaja (preskočena, bez priloga, nema CIM-a, obaveštenje…).</summary>
        Gotova,
        /// <summary>Poruka je proverena i izveštaj je poslat.</summary>
        Obradjena,
        /// <summary>Privremena greška: poruka se ponovo pokušava u sledećem pokretanju (najviše MaxPokusaja puta).</summary>
        PrivremenaGreska,
    }

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
        }

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
            bool zadrzi = false;   // posle poruke koja čeka ponovni pokušaj poslednji UID se više ne pomera
            foreach (var s in red)
            {
                stanje.Poruke.TryGetValue(s.Kljuc, out var ps);
                if (ps != null && ps.Status != StatusPrivremena)
                {
                    log?.Info("  UID " + s.Uid + ": već obrađeno (" + ps.Status + ", " + ps.Folder + ")");
                    if (!zadrzi) fs.PoslednjiUid = Math.Max(fs.PoslednjiUid, s.Uid);
                    continue;
                }
                ps = ps ?? new Stanje.PorukaStanje();
                ps.Folder = s.Folder; ps.Uid = s.Uid; ps.Vreme = DateTime.Now;
                log?.Info("  UID " + s.Uid + " – obrada: Message-ID " + (s.MessageId.Length > 0 ? s.MessageId : "(nema)") + ", od " + s.Od + ", naslov „" + s.Naslov + "“, prilozi: "
                          + (s.Prilozi.Count == 0 ? "nema" : string.Join(", ", s.Prilozi)) + (ps.Pokusaja > 0 ? " (pokušaj " + (ps.Pokusaja + 1) + "/" + max + ")" : ""));

                Korak k;
                try { k = obradi(s, ps); }
                catch (Exception ex)
                {
                    log?.Greska("  UID " + s.Uid + ": obrada poruke nije uspela", ex);
                    ps.PoslednjaGreska = ex.GetType().Name + ": " + ex.Message;
                    k = Korak.PrivremenaGreska;
                }

                if (k == Korak.PrivremenaGreska)
                {
                    ps.Pokusaja++;
                    if (ps.Pokusaja < max)
                    {
                        ps.Status = StatusPrivremena;
                        stanje.Poruke[s.Kljuc] = ps; Sacuvaj();
                        zadrzi = true;
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
                if (!zadrzi) fs.PoslednjiUid = Math.Max(fs.PoslednjiUid, s.Uid);
                Sacuvaj();
            }
            return n;
        }

        private void Sacuvaj() { if (!dryRun && stanje.Putanja != null) stanje.Sacuvaj(); }
    }
}
