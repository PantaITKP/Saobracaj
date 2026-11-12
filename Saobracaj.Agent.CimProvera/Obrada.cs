using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Rezultat obrade jednog maila / foldera.</summary>
    internal sealed class Ishod
    {
        public RezultatProvere Provera;
        public List<string> Preskoceno = new List<string>();
        public List<string> NisuProcitani = new List<string>();
        public bool PrivremenaGreska;
        public bool Engleski;
    }

    /// <summary>
    /// Zajedničko jezgro za dolazni i odlazni mail i za CLI: svaki prilog se otvara; PDF i slike čita Claude,
    /// Excel se čita direktno; zatim rules engine nad svim dokumentima iz maila.
    /// </summary>
    internal sealed class Obrada
    {
        private readonly PravilaSkup pravila;
        private readonly ICitacDokumenata citac;
        private readonly Log log;
        private readonly double prag;

        public Obrada(PravilaSkup pravila, ICitacDokumenata citac, Log log, double prag)
        {
            this.pravila = pravila; this.citac = citac; this.log = log; this.prag = prag;
        }

        public Ishod Proveri(IEnumerable<(string ime, byte[] sadrzaj)> prilozi, string posiljalac, bool interni, DateTime referentniDatum,
            bool engleski, string rezimKp = "auto")
        {
            var ishod = new Ishod { Engleski = engleski };
            var dokumenti = new List<Dokument>();
            var fajlovi = new List<Prilog>();
            foreach (var (ime, b) in prilozi)
                fajlovi.AddRange(Raspakivanje.Raspakuj(ime, b, ishod.Preskoceno));
            foreach (var x in ishod.Preskoceno) log?.Info("  prilog " + x + " – preskočen");

            // Isti fajl dvaput u mailu (npr. i u ZIP-u i kao prilog) čita se jednom.
            foreach (var f in fajlovi.GroupBy(x => x.Hes).Select(g => g.First()))
            {
                try
                {
                    if (f.Vrsta == VrstaPriloga.Excel)
                    {
                        var ex = ExcelCitac.Procitaj(f, pravila.Kp);
                        dokumenti.AddRange(ex);
                        log?.Info("  prilog " + f.Naziv + " (Excel): " + VrstePoStranama(ex));
                    }
                    else
                    {
                        var r = citac.Procitaj(f);
                        dokumenti.AddRange(r.Dokumenti);
                        log?.Info("  prilog " + f.Naziv + " (" + f.Vrsta + "): " + VrstePoStranama(r.Dokumenti) + (r.NeprocitaneStrane > 0 ? ", nepročitano str.: " + r.NeprocitaneStrane : ""));
                        if (r.StraneOpis.Count > 0) log?.Info("    strane: " + string.Join(" | ", r.StraneOpis));
                        if (r.NeprocitaneStrane > 0 && !r.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)))
                            ishod.NisuProcitani.Add(f.Naziv + ": " + r.NeprocitaneStrane + " str. nije pročitano (" + r.Napomena + ")");
                    }
                }
                catch (PrivremenaGreskaCitanja ex)
                {
                    ishod.PrivremenaGreska = true;
                    ishod.NisuProcitani.Add(f.Naziv + ": " + ex.Message);
                    log?.Upozorenje("  " + f + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    ishod.NisuProcitani.Add(f.Naziv + ": " + ex.Message);
                    log?.Upozorenje("  " + f + ": nije pročitan – " + ex.Message + (ex is TrajnaGreskaCitanja ? "" : Environment.NewLine + ex.StackTrace));
                }
            }

            var u = new UlazProvere
            {
                Dokumenti = dokumenti, Posiljalac = posiljalac, InterniPosiljalac = interni,
                ReferentniDatum = referentniDatum, Engleski = engleski, RezimKp = rezimKp, Prag = prag,
            };
            ishod.Provera = new Motor(pravila, log).Proveri(u);
            return ishod;
        }

        /// <summary>Vrsta dokumenta po stranama za log, npr. "str.1 izvozna_deklaracija, str.5-6 tranzitna_deklaracija, str.7 cim, str.8 spisak_kola".</summary>
        internal static string VrstePoStranama(IEnumerable<Dokument> docs)
        {
            var d = docs.ToList();
            if (d.Count == 0) return "bez prepoznatih dokumenata";
            return string.Join(", ", d.Select(x => (x.Strane.Count == 0 ? "" : "str." + (x.Strane.Count == 1 ? x.Strane[0].ToString() : x.Strane.Min() + "-" + x.Strane.Max()) + " ")
                + x.Tip + (x.Tip == TipDokumenta.Cim || x.Tip == TipDokumenta.Cuv ? "(" + (x.Cim.FirstOrDefault()?.Sablon is string s && s.Length > 0 ? s : "?") + ")" : "")));
        }

        /// <summary>Engleski odgovor ako je mail pošiljaoca na nemačkom ili engleskom (prosta procena po rečima).</summary>
        public static bool JeEngleskiIliNemacki(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            var t = " " + PraviloKp.Normalizuj(tekst) + " ";
            int Broj(params string[] reci) => reci.Sum(r => Regex.Matches(t, " " + r + " ").Count);
            var sr = Broj("postovani", "pozdrav", "pozdravom", "molim", "molimo", "tovarni", "prilogu", "hvala", "srdacan", "srdacno", "lp", "dostavljam", "saljem", "voz", "kola")
                     + Regex.Matches(tekst, "[čćšžđČĆŠŽĐ]").Count / 3;
            var en = Broj("dear", "please", "attached", "regards", "kind", "thank", "thanks", "find", "the", "and", "best",
                          "sehr", "geehrte", "anbei", "freundlichen", "grussen", "bitte", "danke", "und", "die", "der");
            return en > sr && en >= 2;
        }
    }
}
