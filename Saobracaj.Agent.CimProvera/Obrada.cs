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
        /// <summary>Prilozi koji nisu mogli da se pročitaju ili klasifikuju – nisu dokaz da tovarnog lista nema.</summary>
        public List<string> Neprepoznati = new List<string>();
        /// <summary>Svaki prilog i njegova klasifikacija („ime – tovarni list CIM“).</summary>
        public List<string> OpisPriloga = new List<string>();
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

            // Prilozi koji se ne čitaju: OpenOffice tabela, Pages, XPS, TIFF… nisu dokaz da tovarnog lista nema.
            foreach (var x in ishod.Preskoceno)
                if (Regex.IsMatch(x, @"\.(ods|pages|xps|tif|tiff|bmp)\b", RegexOptions.IgnoreCase)) ishod.Neprepoznati.Add(Regex.Replace(x, @"\s*\(.*$", ""));
            var lc = citac as LokalniCitac;

            // Isti fajl dvaput u mailu (npr. i u ZIP-u i kao prilog) čita se jednom.
            foreach (var f in fajlovi.GroupBy(x => x.Hes).Select(g => g.First()))
            {
                var pre = dokumenti.Count;
                try
                {
                    if (f.Vrsta == VrstaPriloga.Excel || f.Vrsta == VrstaPriloga.Word)
                    {
                        // Tovarni list CIM/CUV kao Excel/Word obrazac: čita se iz ćelija (bez OCR-a); inače Excel = spisak kola / tabela MRN.
                        var word = f.Vrsta == VrstaPriloga.Word;
                        // Word tabela (.docx/.docm su ZIP); .doc/.rtf nemaju tabelu u XML-u – idu direktno u PDF
                        List<List<Celija>> celije = null;
                        if (!word) celije = TabelarniCim.IzExcela(f.Sadrzaj);
                        else if (f.Sadrzaj.Length > 4 && f.Sadrzaj[0] == 'P' && f.Sadrzaj[1] == 'K') try { celije = TabelarniCim.IzWorda(f.Sadrzaj); } catch { celije = null; }
                        var tovarni = celije == null ? null : lc?.TovarniIzTabele(f, celije, word ? "word" : "excel");
                        if (tovarni != null) dokumenti.Add(tovarni);
                        else if (!word) dokumenti.AddRange(ExcelCitac.Procitaj(f, pravila.Kp));
                        else ProcitajWord(f, dokumenti, ishod);
                        if (tovarni != null || !word)
                            log?.Info("  prilog " + f.Naziv + " (" + f.Vrsta + "): " + VrstePoStranama(dokumenti.Skip(pre)) + (tovarni != null ? " – " + tovarni.Opis : ""));
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
                var novi = dokumenti.Skip(pre).ToList();
                if (novi.Count == 0 && ishod.NisuProcitani.Any(x => x.StartsWith(f.Naziv + ":"))) ishod.Neprepoznati.Add(f.Naziv);
                ishod.OpisPriloga.Add(System.IO.Path.GetFileName(f.Naziv.Split('/').Last()) + " – " + OpisPriloga(f, novi, ishod.Neprepoznati.Contains(f.Naziv)));
            }
            foreach (var n in ishod.Neprepoznati.Where(n => !fajlovi.Any(f => f.Naziv == n))) ishod.OpisPriloga.Add(System.IO.Path.GetFileName(n) + " – nije prepoznat");

            IstiListDvaput(dokumenti, log);

            var u = new UlazProvere
            {
                Dokumenti = dokumenti, Posiljalac = posiljalac, InterniPosiljalac = interni,
                ReferentniDatum = referentniDatum, Engleski = engleski, RezimKp = rezimKp, Prag = prag,
            };
            ishod.Provera = new Motor(pravila, log).Proveri(u);
            ishod.Provera.OpisPriloga = ishod.OpisPriloga;
            return ishod;
        }

        private static readonly Regex ImeTovarnog = new Regex(@"(^|[^a-z])(cim|cuv|smgs)([^a-z]|$)|frachtbrief|wagenbrief|tovarni|consignment|nakladni|lettre", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Word prilog bez tabele obrasca: pretvara se u PDF (Syncfusion) i čita istim putem kao PDF (klasifikacija strana, šablon,
        /// vrednosti iz tekstualnog sloja). Neuspelo pretvaranje, ili ime koje liči na tovarni list a obrazac nije prepoznat → ručna
        /// provera (nikad „nema tovarnog lista“ pošiljaocu). Log: pretvoren, trajanje, broj strana i vrsta svake strane.
        /// </summary>
        private void ProcitajWord(Prilog f, List<Dokument> dokumenti, Ishod ishod)
        {
            var pdf = WordUPdf.Pretvori(f, out var razlog, out var trajanje);
            if (pdf == null)
            {
                if (WordUPdf.NedostajaoKljuc && !WordUPdf.KljucUcitan) log?.Greska("  prilog " + f.Naziv + " (Word): " + razlog);
                else log?.Upozorenje("  prilog " + f.Naziv + " (Word): " + razlog);
                ishod.NisuProcitani.Add(f.Naziv + ": " + razlog);
                return;
            }
            var pp = new Prilog { Naziv = f.Naziv, Sadrzaj = pdf, Vrsta = VrstaPriloga.Pdf, MediaType = "application/pdf", IzWorda = true };
            var r = citac.Procitaj(pp);
            foreach (var d in r.Dokumenti) { d.Fajl = f.Naziv; d.Izvor = "word"; }
            dokumenti.AddRange(r.Dokumenti);
            var strana = r.StraneOpis.Count > 0 ? r.StraneOpis.Count : r.Dokumenti.SelectMany(d => d.Strane).DefaultIfEmpty(0).Max();
            log?.Info("  prilog " + f.Naziv + " (Word): pretvoren u PDF za " + (int)trajanje.TotalMilliseconds + " ms, strana " + strana + " – " + VrstePoStranama(r.Dokumenti)
                      + (r.NeprocitaneStrane > 0 ? ", nepročitano str.: " + r.NeprocitaneStrane : ""));
            if (r.StraneOpis.Count > 0) log?.Info("    strane: " + string.Join(" | ", r.StraneOpis));
            if (!r.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)) && (r.NeprocitaneStrane > 0 || ImeTovarnog.IsMatch(System.IO.Path.GetFileNameWithoutExtension(f.Naziv) ?? "")))
            {
                var zasto = "Word prilog je pretvoren u PDF, ali obrazac tovarnog lista nije prepoznat" + (r.NeprocitaneStrane > 0 ? " (nepročitano strana: " + r.NeprocitaneStrane + ")" : " (ime fajla liči na tovarni list)") + " – potrebna je ručna provera";
                log?.Upozorenje("  prilog " + f.Naziv + ": " + zasto);
                ishod.NisuProcitani.Add(f.Naziv + ": " + zasto);
                if (!ishod.Neprepoznati.Contains(f.Naziv)) ishod.Neprepoznati.Add(f.Naziv);
            }
        }

        /// <summary>
        /// Isti tovarni list i kao PDF i kao Excel/Word (ista identifikacija r62 ili isto mesto i datum r29): proverava se jednom.
        /// Prednost ima PDF ako je potpisan ili pečatiran (sken, potpis u r1, pročitana r58a), inače tabela (pouzdano čitanje).
        /// </summary>
        internal static void IstiListDvaput(List<Dokument> dokumenti, Log log)
        {
            string Kljuc(CimList c) => Regex.Replace((c.R62.Zemlja ?? "") + (c.R62.Stanica ?? "") + (c.R62.Preduzece ?? "") + (c.R62.Broj ?? ""), @"\W", "").ToUpperInvariant() is var k && k.Length >= 8
                ? k : Regex.Replace((c.R29.Mesto ?? "") + (c.R29.Datum ?? ""), @"\W", "").ToUpperInvariant();
            var tabele = dokumenti.Where(d => TipDokumenta.JeTovarniList(d.Tip) && (d.Izvor == "excel" || d.Izvor == "word") && d.Cim.Count == 1 && string.IsNullOrEmpty(d.Cim[0].NeprepoznatObrazac)).ToList();
            foreach (var t in tabele)
            {
                var kt = Kljuc(t.Cim[0]);
                if (kt.Length < 6) continue;
                var pdf = dokumenti.FirstOrDefault(d => d != t && TipDokumenta.JeTovarniList(d.Tip) && d.Izvor != "excel" && d.Izvor != "word" && d.Cim.Any(c => string.IsNullOrEmpty(c.NeprepoznatObrazac) && Kljuc(c) == kt));
                if (pdf == null) continue;
                var c0 = pdf.Cim.First(c => Kljuc(c) == kt);
                var potpisan = pdf.Izvor == "ocr" || c0.R1.Potpis || c0.R58a.Popunjena;
                var izbacen = potpisan ? t : pdf;
                var ostaje = potpisan ? pdf : t;
                ostaje.Opis += "; isti list je i u " + System.IO.Path.GetFileName(izbacen.Fajl) + " – proveren je " + System.IO.Path.GetFileName(ostaje.Fajl) + (potpisan ? " (potpisan/pečatiran PDF)" : " (čitanje iz ćelija)");
                if (izbacen.Cim.Count <= 1) { izbacen.Tip = TipDokumenta.Ostalo; izbacen.Opis = "isti tovarni list kao " + System.IO.Path.GetFileName(ostaje.Fajl) + " (proverava se jednom)"; izbacen.Cim.Clear(); }
                else izbacen.Cim.Remove(izbacen.Cim.First(c => Kljuc(c) == kt));
                log?.Info("  isti tovarni list u " + izbacen.Fajl + " i " + ostaje.Fajl + " – proverava se " + ostaje.Fajl);
            }
        }

        /// <summary>Kako je prilog klasifikovan, za izveštaj: „tovarni list CIM“, „spisak kola“, „izveštaj o sastavu voza (ne proverava se)“…</summary>
        internal static string OpisPriloga(Prilog f, List<Dokument> docs, bool neprepoznat)
        {
            if (neprepoznat || docs.Count == 0) return "nije prepoznat";
            var ime = (f.Naziv ?? "").ToLowerInvariant();
            string Naziv(Dokument d)
            {
                switch (d.Tip)
                {
                    case TipDokumenta.Cim: case TipDokumenta.Cuv:
                        if (d.Cim.Count == 0) return d.Opis;
                        var c = d.Cim[0];
                        if (!string.IsNullOrEmpty(c.NeprepoznatObrazac)) return "tovarni list " + (d.Tip == TipDokumenta.Cuv ? "CUV" : "CIM") + " (obrazac nije automatski prepoznat – ručna provera)";
                        return "tovarni list " + (d.Tip == TipDokumenta.Cuv ? "CUV" : "CIM") + (d.Cim.Count > 1 ? " (" + d.Cim.Count + " lista)" : "") + (d.Izvor == "excel" ? " (Excel)" : d.Izvor == "word" ? " (Word)" : "");
                    case TipDokumenta.SpisakKola:
                        return Regex.IsMatch(ime, @"train ?list|load ?list|ladeliste") || d.Spisak.Any(s => s.Kola.Count > 0 && s.Kola.All(k => k.Uti.Count > 0) && Regex.IsMatch(ime, "train|load"))
                            ? "spisak tovarnih jedinica" : "spisak kola";
                    case TipDokumenta.SastavVoza: return "izveštaj o sastavu voza (ne proverava se)";
                    case TipDokumenta.DodatniList: return "dodatni list";
                    case TipDokumenta.IzvoznaDeklaracija: return "izvozna deklaracija";
                    case TipDokumenta.TranzitnaDeklaracija: return "tranzitna deklaracija";
                    case TipDokumenta.DrugaCarinska: return "carinski dokument";
                    case TipDokumenta.Eur1: return "EUR.1";
                    case TipDokumenta.UverenjeOPoreklu: return "uverenje o poreklu";
                    case TipDokumenta.Sertifikat: return "sertifikat";
                    case TipDokumenta.Faktura: return "faktura";
                    case TipDokumenta.TabelaMrn: return "tabela MRN";
                    case TipDokumenta.DozvolaOtpad: return "dozvola za otpad";
                    case TipDokumenta.AnnexVii: return "dokument o kretanju otpada (Annex VII)";
                    case TipDokumenta.IzvestajRadioloski: return "izveštaj o radiološkoj kontroli";
                    case TipDokumenta.Poledjina: return "poleđina lista";
                    case TipDokumenta.Najava: return "najava";
                    case TipDokumenta.Interni: return "interni obrazac KP";
                    case TipDokumenta.Ugovor: return "ugovor";
                    case TipDokumenta.Ponuda: return "ponuda";
                    default: return string.IsNullOrEmpty(d.Opis) || !d.Opis.StartsWith("isti tovarni list") ? "ostalo (ne proverava se)" : d.Opis;
                }
            }
            var nazivi = docs.Select(Naziv).Distinct().ToList();
            // „ostalo“ uz prepoznate dokumente iz istog PDF-a se ne navodi posebno
            if (nazivi.Count > 1) nazivi.Remove("ostalo (ne proverava se)");
            return string.Join(", ", nazivi);
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
