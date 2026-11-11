using System;
using System.Collections.Generic;

namespace Saobracaj.Agent.CimProvera.Model
{
    /// <summary>Opis polja koji ide u JSON šemu za Claude (description).</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    internal sealed class OpisAttribute : Attribute
    {
        public string Tekst { get; }
        public string[] Vrednosti { get; set; }
        public OpisAttribute(string tekst) { Tekst = tekst; }
    }

    /// <summary>Polje koje Claude ne popunjava (popunjava ga kod posle čitanja).</summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class InterniAttribute : Attribute { }

    // ------------------------------------------------------------------------------------------------
    // Pravilo modela: SVA polja su obavezna i nijedno nije null (prazan string, false, prazna lista).
    // Tako JSON šema nema unija tipova. Objekti koji mogu da ne postoje su liste sa 0 ili 1 elementom.
    // ------------------------------------------------------------------------------------------------

    internal static class StatusRubrike
    {
        public const string Popunjeno = "popunjeno";
        public const string Prazno = "prazno";
        public const string Necitljivo = "necitljivo";
        /// <summary>Upisane su samo oznake obrasca/šablona bez vrednosti („Tara: … Netto: … Bruto: …“, „+- - -“).</summary>
        public const string Oznake = "oznake";
    }

    /// <summary>Zajednički deo svake rubrike: da li je popunjena, sirovi tekst i pouzdanost čitanja.</summary>
    internal abstract class Rubrika
    {
        [Opis("popunjeno = nešto je upisano (i ako je pogrešno); prazno = polje je prazno; necitljivo = nešto je upisano, ali ne može pouzdano da se pročita (bledo, odsečeno, rukopis).",
            Vrednosti = new[] { StatusRubrike.Popunjeno, StatusRubrike.Prazno, StatusRubrike.Necitljivo })]
        public string Status { get; set; } = StatusRubrike.Prazno;

        [Opis("Tačno prepisan tekst rubrike, onako kako piše (bez ispravljanja i tumačenja). Prazan string ako je rubrika prazna.")]
        public string Tekst { get; set; } = "";

        [Opis("Pouzdanost čitanja ove rubrike, 0 do 1.")]
        public double Pouzdanost { get; set; } = 1;

        [Newtonsoft.Json.JsonIgnore]
        public bool Popunjena => Status == StatusRubrike.Popunjeno || Status == StatusRubrike.Necitljivo;
    }

    internal sealed class TekstRubrika : Rubrika { }

    internal sealed class Strana : Rubrika
    {
        [Opis("Naziv / ime.")] public string Naziv { get; set; } = "";
        [Opis("Poštanska adresa (ulica, broj, poštanski broj, mesto).")] public string Adresa { get; set; } = "";
        [Opis("Šifra zemlje ISO 3166 (dva slova, npr. RS, HU, DE) ako je UPISANA u adresi; prazno ako nije upisana šifra (i ako je upisan pun naziv zemlje).")] public string SifraZemlje { get; set; } = "";
        [Opis("Pun naziv zemlje ako je upisan.")] public string NazivZemlje { get; set; } = "";
        [Opis("Da li postoji potpis ili pečat sa potpisom.")] public bool Potpis { get; set; }
        [Opis("Telefon ili faks ako je upisan.")] public string Telefon { get; set; } = "";
        [Opis("E-mail ako je upisan.")] public string Email { get; set; } = "";
        [Opis("PDV (VAT) broj ako je upisan.")] public string Pdv { get; set; } = "";
    }

    internal sealed class SifraTekst
    {
        [Opis("Šifra (broj) stavke kako je upisana, npr. '10' za šifru 7.10 ili '830' za UN/EDIFACT 1001. Prazno ako stavka nema šifru.")] public string Sifra { get; set; } = "";
        [Opis("Tekst stavke (bez šifre).")] public string Tekst { get; set; } = "";
    }

    internal sealed class ListaRubrika : Rubrika
    {
        [Opis("Pojedinačne stavke rubrike, svaka sa šifrom i tekstom.")] public List<SifraTekst> Stavke { get; set; } = new List<SifraTekst>();
    }

    internal sealed class MestoIsporuke : Rubrika
    {
        [Opis("Naziv uputne stanice.")] public string Stanica { get; set; } = "";
        [Opis("Šifra stanice (DIUM) ako je upisana.")] public string SifraStanice { get; set; } = "";
        [Opis("Zemlja (šifra UIC 920-14, npr. 72, ili naziv).")] public string Zemlja { get; set; } = "";
    }

    internal sealed class Preuzimanje : Rubrika
    {
        [Opis("Mesto / stanica preuzimanja (naziv).")] public string Mesto { get; set; } = "";
        [Opis("Šifra stanice (DIUM) ako je upisana.")] public string SifraStanice { get; set; } = "";
        [Opis("Zemlja (šifra UIC 920-14 ili naziv).")] public string Zemlja { get; set; } = "";
        [Opis("Datum preuzimanja u obliku YYYY-MM-DD ako je upisan (godinu dodaj samo ako je upisana; ako nije, YYYY = 0000). Prazno ako datum NIJE upisan.")] public string Datum { get; set; } = "";
        [Opis("Vreme (čas) preuzimanja, npr. '14' ili '14:30'. Prazno ako nije upisano.")] public string Cas { get; set; } = "";
        /// <summary>Kućice mesec–dan–čas kada datum nije pročitan: prazno (bez mastila) ili necitljivo (ima mastila); prazno = nije provereno.</summary>
        [Interni] public string KuciceStanje { get; set; } = "";
    }

    internal sealed class Mrn
    {
        [Opis("Prefiks tačno kako piše ispred broja: 'E MRN', 'T MRN', 'TS MRN', 'EXS MRN', 'ENS MRN', 'MRN' ili prazno ako stoji samo broj.")] public string Prefiks { get; set; } = "";
        [Opis("MRN broj (18 znakova).")] public string Broj { get; set; } = "";
        [Opis("Kontejner (UTI) ili kola uz koja je MRN naveden, ako je naveden.")] public string Uz { get; set; } = "";
    }

    internal sealed class Uti
    {
        [Opis("Broj UTI/kontejnera, npr. MSCU 123456 7.")] public string Broj { get; set; } = "";
        [Opis("Šifra tipa UTI ako je upisana.")] public string Tip { get; set; } = "";
        [Opis("Dužina UTI ako je upisana.")] public string Duzina { get; set; } = "";
        [Opis("Neto masa sadržaja UTI ako je upisana.")] public string Neto { get; set; } = "";
        [Opis("Tara UTI ako je upisana.")] public string Tara { get; set; } = "";
    }

    internal sealed class NaznacenjeRobe : Rubrika
    {
        [Opis("Opis robe (naziv robe) kako piše.")] public string OpisRobe { get; set; } = "";
        [Opis("UN brojevi opasne robe (samo brojevi, npr. '1203').")] public List<string> UnBrojevi { get; set; } = new List<string>();
        [Opis("RID klase ako su upisane (npr. '3', '2.1').")] public List<string> RidKlase { get; set; } = new List<string>();
        [Opis("Svi MRN brojevi upisani u rubriku 21.")] public List<Mrn> Mrn { get; set; } = new List<Mrn>();
        [Opis("ARC brojevi (akcize) upisani u rubriku 21.")] public List<string> Arc { get; set; } = new List<string>();
        [Opis("Da li su upisane plombe (broj/oznake plombi).")] public bool Plombe { get; set; }
        [Opis("UTI/kontejneri upisani u rubriku 21.")] public List<Uti> Uti { get; set; } = new List<Uti>();
        [Opis("12-cifreni brojevi kola upisani u rubriku 21 (kada su kola predata kao roba).")] public List<string> BrojeviKola { get; set; } = new List<string>();
        [Opis("EWC šifre otpada ako su upisane.")] public List<string> Ewc { get; set; } = new List<string>();
        [Opis("Da li postoji napomena IZVOZ / EXPORT.")] public bool Izvoz { get; set; }
        [Opis("Da li postoji IMDG izjava (pomorski prevoz opasne robe, 5.4.1.6 IMDG).")] public bool ImdgIzjava { get; set; }
        [Opis("Da li postoji napomena da list CIM ima snagu kolskog lista CUV.")] public bool CuvNapomena { get; set; }
        [Opis("Da li rubrika sadrži zaostali tekst šablona / primera koji nije pravi podatak (npr. 'XXXX', 'upisati naziv robe', tekst primera iz obrasca, podaci očigledno iz drugog prevoza).")] public bool ZaostaliTekstSablona { get; set; }
        [Opis("Ako postoji zaostali tekst šablona, koji tačno tekst.")] public string ZaostaliTekst { get; set; } = "";
    }

    internal sealed class Masa : Rubrika
    {
        [Opis("Pojedinačne mase (bruto po NHM šifri / po UTI), kako su upisane, u kg, samo broj (npr. '21500').")] public List<string> Pojedinacne { get; set; } = new List<string>();
        [Opis("Tara (UTI, pribor), u kg, ako je upisana.")] public string Tara { get; set; } = "";
        [Opis("Ukupna masa pošiljke u kg, ako je upisana.")] public string Ukupno { get; set; } = "";
    }

    internal sealed class Iznos : Rubrika
    {
        [Opis("Iznos.")] public string Vrednost { get; set; } = "";
        [Opis("Šifra valute kako je upisana (npr. EUR).")] public string Valuta { get; set; } = "";
    }

    internal sealed class MestoDatum : Rubrika
    {
        [Opis("Mesto ispostavljanja.")] public string Mesto { get; set; } = "";
        [Opis("Datum ispostavljanja YYYY-MM-DD. Prazno ako datum NIJE upisan.")] public string Datum { get; set; } = "";
    }

    internal sealed class OznakaDokumenta : Rubrika
    {
        [Opis("Da li je u polju CIM upisan znak X.")] public bool Cim { get; set; }
        [Opis("Da li je u polju CUV upisan znak X.")] public bool Cuv { get; set; }
    }

    internal sealed class Prevoznik
    {
        [Opis("Šifra preduzeća (4 cifre, lista CIT), npr. 3212.")] public string Sifra { get; set; } = "";
        [Opis("Naziv prevoznika.")] public string Naziv { get; set; } = "";
        [Opis("Deonica (od - do, nazivi ili šifre stanica).")] public string Deonica { get; set; } = "";
        [Opis("Status: '1' uzastopni, '2' izvršni, prazno ako nije upisan.")] public string StatusPrevoznika { get; set; } = "";
    }

    internal sealed class PrevozniciRubrika : Rubrika
    {
        [Opis("Prevoznici upisani u rubriku.")] public List<Prevoznik> Prevoznici { get; set; } = new List<Prevoznik>();
    }

    internal sealed class UgovorniPrevoznik : Rubrika
    {
        [Opis("Šifra preduzeća.")] public string Sifra { get; set; } = "";
        [Opis("Naziv.")] public string Naziv { get; set; } = "";
        [Opis("Adresa.")] public string Adresa { get; set; } = "";
        [Opis("Da li postoji potpis (ne samo pečat).")] public bool Potpis { get; set; }
        [Opis("Da li postoji samo pečat bez potpisa.")] public bool SamoPecat { get; set; }
    }

    internal sealed class Identifikacija : Rubrika
    {
        [Opis("Šifra zemlje (UIC 920-14, npr. 72).")] public string Zemlja { get; set; } = "";
        [Opis("Šifra stanice (DIUM).")] public string Stanica { get; set; } = "";
        [Opis("Šifra preduzeća (4 cifre).")] public string Preduzece { get; set; } = "";
        [Opis("Broj otpravljanja (npr. 12345-6).")] public string Broj { get; set; } = "";
    }

    /// <summary>Jedan tovarni list CIM ili kolski list CUV (jedan list, ne ceo PDF).</summary>
    internal sealed class CimList
    {
        [Opis("Obrazac: CIM = tovarni list CIM; CUV = kolski list CUV; CIM_KOMBINOVANI = tovarni list CIM za kombinovani saobraćaj.",
            Vrednosti = new[] { "CIM", "CUV", "CIM_KOMBINOVANI" })]
        public string Obrazac { get; set; } = "CIM";

        [Opis("Redni broj lista ako PDF ima više listova (1, 2, …), inače 1.")] public int RedniBroj { get; set; } = 1;
        [Opis("Da li je ovo ispis elektronskog tovarnog lista (ne papirni obrazac).")] public bool ElektronskiIspis { get; set; }
        [Opis("Ako je ispis elektronskog lista: da li na vrhu postoji napomena 'Ispis elektronskog tovarnog lista - GGGG-MM-DD - izdat od strane ...'.")] public bool NapomenaIspisa { get; set; }
        [Opis("Jezici na kojima je list POPUNJEN (upisani podaci, ne odštampani nazivi rubrika): sr, de, fr, en, hu, ro, tr, bg, hr, … Prazna lista ako nema dovoljno teksta za procenu.")] public List<string> JeziciPopunjavanja { get; set; } = new List<string>();
        [Opis("Kvalitet skena: dobar, srednji ili los.", Vrednosti = new[] { "dobar", "srednji", "los" })] public string KvalitetSkena { get; set; } = "dobar";
        [Opis("Da li je na listu navedena pomorska deonica (luka, brod, IMDG).")] public bool PomorskaDeonica { get; set; }
        /// <summary>Šablon obrasca po kom su rubrike pročitane (K-504, K-505, ZS-EUROLOG…).</summary>
        [Interni] public string Sablon { get; set; } = "";
        /// <summary>Ako obrazac nije prepoznat ili poravnanje nije pouzdano: opis (rubrike se tada ne proveravaju – ručna provera).</summary>
        [Interni] public string NeprepoznatObrazac { get; set; } = "";
        /// <summary>Tekst zaglavlja strane (gornjih ~9 %): natpisi kao „Nem fuvarlevél / Kein Frachtbrief“ (G12).</summary>
        [Interni] public string Zaglavlje { get; set; } = "";

        [Opis("Rubrika 1 - Pošiljalac.")] public Strana R1 { get; set; } = new Strana();
        [Opis("Rubrika 4 - Primalac.")] public Strana R4 { get; set; } = new Strana();
        [Opis("Rubrika 7 - Izjave pošiljaoca (šifra 7.x + tekst).")] public ListaRubrika R7 { get; set; } = new ListaRubrika();
        [Opis("Rubrika 8 - Referenca pošiljaoca.")] public TekstRubrika R8 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 9 - Prilozi (šifra UN/EDIFACT 1001 + tekst).")] public ListaRubrika R9 { get; set; } = new ListaRubrika();
        [Opis("Rubrika 10 - Mesto isporuke.")] public MestoIsporuke R10 { get; set; } = new MestoIsporuke();
        [Opis("Rubrika 12 - Šifra stanice koja opslužuje mesto isporuke.")] public TekstRubrika R12 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 13 - Komercijalni uslovi (šifra 1-5 + tekst).")] public ListaRubrika R13 { get; set; } = new ListaRubrika();
        [Opis("Rubrika 14 - Broj korisničkog sporazuma ili tarife.")] public TekstRubrika R14 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 15 - Napomene primaocu (često broj voza).")] public TekstRubrika R15 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 16 - Preuzimanje na prevoz (mesto, datum, čas).")] public Preuzimanje R16 { get; set; } = new Preuzimanje();
        [Opis("Rubrika 18 - Broj kola. Stavke: svaki broj kola ili tekst (npr. 'po spisku kola').")] public ListaRubrika R18 { get; set; } = new ListaRubrika();
        [Opis("Rubrika 19 - Fakturisanje tranzita.")] public TekstRubrika R19 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 20 - Plaćanje troškova (Franko, Incoterms + mesto). Tekst tačno kako piše, uključujući oznaku polja ako je čekirano.")] public TekstRubrika R20 { get; set; } = new TekstRubrika();
        /// <summary>Polje „Franko prevoznina“ u r20: oznaceno / prazno / nesigurno (prazno = nije provereno).</summary>
        [Interni] public string R20Franko { get; set; } = "";
        /// <summary>Polje „Incoterms“ u r20: oznaceno / prazno / nesigurno.</summary>
        [Interni] public string R20Incoterms { get; set; } = "";
        /// <summary>Polje r52 (frankaturni račun) i r58b (pojednostavljeni postupak): oznaceno / prazno / nesigurno.</summary>
        [Interni] public string R52Polje { get; set; } = "";
        [Interni] public string R58bPolje { get; set; } = "";
        [Opis("Rubrika 21 - Naznačenje robe.")] public NaznacenjeRobe R21 { get; set; } = new NaznacenjeRobe();
        [Opis("Rubrika 22 - Naročita pošiljka (popunjeno ako je polje označeno).")] public TekstRubrika R22 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 23 - RID (tekst tačno kako piše, npr. 'X').")] public TekstRubrika R23 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 24 - NHM šifre. Stavke: svaka šifra (bez razmaka) u polju Sifra.")] public ListaRubrika R24 { get; set; } = new ListaRubrika();
        [Opis("Rubrika 25 - Masa.")] public Masa R25 { get; set; } = new Masa();
        [Opis("Rubrika 26 - Izjava o vrednosti.")] public Iznos R26 { get; set; } = new Iznos();
        [Opis("Rubrika 27 - Obezbeđenje uredne isporuke.")] public Iznos R27 { get; set; } = new Iznos();
        [Opis("Rubrika 28 - Pouzeće.")] public Iznos R28 { get; set; } = new Iznos();
        [Opis("Rubrika 29 - Mesto i datum ispostavljanja.")] public MestoDatum R29 { get; set; } = new MestoDatum();
        [Opis("Rubrika 30 - Oznaka dokumenta (polja CIM / CUV).")] public OznakaDokumenta R30 { get; set; } = new OznakaDokumenta();
        [Opis("Rubrika 49 - Šifra frankiranja.")] public TekstRubrika R49 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 50 - Stvarni prevozni put.")] public TekstRubrika R50 { get; set; } = new TekstRubrika();
        [Opis("Rubrika 57 - Ostali prevoznici.")] public PrevozniciRubrika R57 { get; set; } = new PrevozniciRubrika();
        [Opis("Rubrika 58a - Ugovorni prevoznik.")] public UgovorniPrevoznik R58a { get; set; } = new UgovorniPrevoznik();
        [Opis("Rubrika 62 - Identifikacija pošiljke.")] public Identifikacija R62 { get; set; } = new Identifikacija();
        [Opis("Rubrika 99 - Carinske napomene (tekst kako piše).")] public TekstRubrika R99 { get; set; } = new TekstRubrika();
    }

    internal sealed class KolaNaSpisku
    {
        [Opis("12-cifreni broj kola (samo cifre).")] public string Broj { get; set; } = "";
        [Opis("UTI/kontejneri na tim kolima.")] public List<string> Uti { get; set; } = new List<string>();
        [Opis("NHM šifra za ta kola ako je upisana.")] public string Nhm { get; set; } = "";
        [Opis("Da li je za ta kola označen RID.")] public bool Rid { get; set; }
        [Opis("MRN brojevi upisani uz ta kola/UTI.")] public List<string> Mrn { get; set; } = new List<string>();
        [Opis("Da li su kola prazna.")] public bool Prazna { get; set; }
    }

    /// <summary>Spisak kola (CIT 23 / Prilog 5).</summary>
    internal sealed class SpisakKola
    {
        [Opis("Identifikacija pošiljke na spisku (rubrika 62 na spisku).")] public Identifikacija R62 { get; set; } = new Identifikacija();
        [Opis("Da li je na spisku naveden Kombinovani prevoz / KP / UIC 3212 (zaglavlje ili polje prevoznika).")] public bool KpNaveden { get; set; }
        [Opis("Nazivi i šifre prevoznika upisani na spisku, kako pišu.")] public List<string> Prevoznici { get; set; } = new List<string>();
        [Opis("Da li je na spisku negde označen RID.")] public bool RidOznaka { get; set; }
        [Interni] public List<string> UnBrojevi { get; set; } = new List<string>();
        [Opis("Datum na spisku YYYY-MM-DD, ako postoji.")] public string Datum { get; set; } = "";
        [Opis("Kola na spisku.")] public List<KolaNaSpisku> Kola { get; set; } = new List<KolaNaSpisku>();
    }

    /// <summary>Carinski i prateći dokumenti: deklaracije, T1, tabele MRN.</summary>
    internal sealed class CarinskiPodaci
    {
        [Opis("MRN brojevi iz dokumenta.")] public List<string> Mrn { get; set; } = new List<string>();
        [Opis("ARC brojevi iz dokumenta.")] public List<string> Arc { get; set; } = new List<string>();
        [Opis("Parovi MRN - kontejner/kola ako ih dokument navodi.")] public List<Mrn> MrnPoUti { get; set; } = new List<Mrn>();
        /// <summary>Tarifne oznake robe (HS, prvih 6 cifara) iz deklaracije.</summary>
        [Interni] public List<string> Tarife { get; set; } = new List<string>();
        /// <summary>Broj izvozne deklaracije (JCI) i carinarnica, npr. "42072/C1/23309/2026", "CI S. Mitrovica".</summary>
        [Interni] public string BrojDeklaracije { get; set; } = "";
        [Interni] public string Carinarnica { get; set; } = "";
        /// <summary>EWC (indeksni) brojevi otpada iz dozvole, npr. "12 01 01".</summary>
        [Interni] public List<string> Ewc { get; set; } = new List<string>();
        /// <summary>Rok za podnošenje robe (tranzit), dd.MM.yyyy.</summary>
        [Interni] public string Rok { get; set; } = "";
        /// <summary>Polje „Sigurnost“ (bezbednosni podaci) iz tranzitne deklaracije: 0–4, prazno ako nije pročitano.</summary>
        [Interni] public string Sigurnost { get; set; } = "";
    }

    internal sealed class DodatniListPodaci
    {
        [Opis("Da li dodatni list sadrži broj pošiljke.")] public bool BrojPosiljke { get; set; }
        [Opis("Da li dodatni list sadrži datum preuzimanja.")] public bool DatumPreuzimanja { get; set; }
    }

    internal static class TipDokumenta
    {
        public const string Cim = "cim";
        public const string Cuv = "cuv";
        public const string SpisakKola = "spisak_kola";
        public const string DodatniList = "dodatni_list";
        public const string IzvoznaDeklaracija = "izvozna_deklaracija";
        public const string TranzitnaDeklaracija = "tranzitna_deklaracija";
        public const string DrugaCarinska = "druga_carinska";
        public const string Eur1 = "eur1";
        public const string UverenjeOPoreklu = "uverenje_o_poreklu";
        public const string Sertifikat = "sertifikat";
        public const string Faktura = "faktura";
        public const string TabelaMrn = "tabela_mrn";
        public const string Ugovor = "ugovor";
        public const string Ponuda = "ponuda";
        public const string Najava = "najava";
        public const string Interni = "interni_kp";
        public const string Poledjina = "poledjina";
        /// <summary>Rešenje o dozvoli za prekogranično kretanje (izvoz) otpada.</summary>
        public const string DozvolaOtpad = "dozvola_otpad";
        /// <summary>Dokument o kretanju otpada (Annex VII / Prilog VII).</summary>
        public const string AnnexVii = "annex_vii";
        public const string IzvestajRadioloski = "izvestaj_radioloski";
        public const string Ostalo = "ostalo";

        public static readonly string[] Sve =
        {
            Cim, Cuv, SpisakKola, DodatniList, IzvoznaDeklaracija, TranzitnaDeklaracija, DrugaCarinska, Eur1, UverenjeOPoreklu,
            Sertifikat, Faktura, TabelaMrn, Ugovor, Ponuda, Najava, Interni, Poledjina, DozvolaOtpad, AnnexVii, IzvestajRadioloski, Ostalo
        };

        public static bool JeTovarniList(string t) => t == Cim || t == Cuv;
        public static bool JeCarinski(string t) => t == IzvoznaDeklaracija || t == TranzitnaDeklaracija || t == DrugaCarinska;
    }

    /// <summary>Jedan prepoznat dokument u prilogu (može da zauzme više strana; jedan PDF može da ima više dokumenata).</summary>
    internal sealed class Dokument
    {
        [Opis("Vrsta dokumenta. cim = tovarni list CIM (i CIM za kombinovani saobraćaj); cuv = kolski list CUV; spisak_kola = spisak kola CIT 23 / wagon list; dodatni_list = dodatni list tovarnog lista; izvozna_deklaracija = izvozna deklaracija / EX / VDD / ECS; tranzitna_deklaracija = T1/T2/TS/NCTS tranzitni prateći dokument; druga_carinska = ostali carinski dokumenti (ENS, uvozna, JCI…); eur1 = EUR.1; uverenje_o_poreklu; sertifikat = fito/veterinarski/analiza/kontrola tovara; faktura; tabela_mrn = tabela MRN po kontejnerima/kolima; ugovor; ponuda; najava = najava prevoza/voza; interni_kp = interni obrasci Kombinovanog prevoza (K-200, KOL-65, teretnica, propratnica); ostalo.",
            Vrednosti = new[] {
                TipDokumenta.Cim, TipDokumenta.Cuv, TipDokumenta.SpisakKola, TipDokumenta.DodatniList, TipDokumenta.IzvoznaDeklaracija,
                TipDokumenta.TranzitnaDeklaracija, TipDokumenta.DrugaCarinska, TipDokumenta.Eur1, TipDokumenta.UverenjeOPoreklu,
                TipDokumenta.Sertifikat, TipDokumenta.Faktura, TipDokumenta.TabelaMrn, TipDokumenta.Ugovor, TipDokumenta.Ponuda,
                TipDokumenta.Najava, TipDokumenta.Interni, TipDokumenta.Ostalo })]
        public string Tip { get; set; } = TipDokumenta.Ostalo;

        [Opis("Strane priloga koje dokument zauzima (1 = prva strana). Za sliku: [1].")] public List<int> Strane { get; set; } = new List<int>();
        [Opis("Kratak opis dokumenta (npr. 'Izvozna deklaracija MRN 26RS…', 'CIM list 3 od 14').")] public string Opis { get; set; } = "";
        [Opis("Za cim/cuv: tačno jedan element sa svim rubrikama. Za ostale vrste: prazna lista.")] public List<CimList> Cim { get; set; } = new List<CimList>();
        [Opis("Za spisak_kola: tačno jedan element. Za ostale: prazna lista.")] public List<SpisakKola> Spisak { get; set; } = new List<SpisakKola>();
        [Opis("Za carinske dokumente i tabela_mrn: tačno jedan element sa MRN/ARC brojevima. Za ostale: prazna lista.")] public List<CarinskiPodaci> Carinski { get; set; } = new List<CarinskiPodaci>();
        [Opis("Za dodatni_list: tačno jedan element. Za ostale: prazna lista.")] public List<DodatniListPodaci> DodatniList { get; set; } = new List<DodatniListPodaci>();

        // --- popunjava kod ---
        [Interni] public string Fajl { get; set; } = "";
        /// <summary>Uslov isporuke iz fakture / deklaracije (npr. "DAP Alesd").</summary>
        [Interni] public string Incoterms { get; set; } = "";
        [Interni] public string Izvor { get; set; } = "claude"; // claude | excel
    }

    /// <summary>Odgovor Claude-a za jedan prilog.</summary>
    internal sealed class RezultatPriloga
    {
        [Opis("Svi dokumenti pronađeni u prilogu, redom po stranama. Svaki CIM/CUV list je poseban dokument.")]
        public List<Dokument> Dokumenti { get; set; } = new List<Dokument>();
        [Opis("Napomena čitača (npr. 'strana 3 je okrenuta naopako', 'strana 5 je prazna'). Prazno ako nema.")]
        public string Napomena { get; set; } = "";

        /// <summary>Strane koje nisu mogle da se pročitaju (sken bez OCR-a, ili liče na CIM obrazac, a nisu pouzdano pročitane).</summary>
        [Interni] public int NeprocitaneStrane { get; set; }
        /// <summary>Za log: vrsta svake strane, natpisi po kojima je prepoznata i pouzdanost, npr. "str.6 spisak_kola (spisak kola, CIT-23; 0,90)".</summary>
        [Interni] public List<string> StraneOpis { get; set; } = new List<string>();
    }
}
