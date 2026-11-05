# Specifikacija: Planiranje (meni Transport)

**Za:** Claude Code · **Projekat:** `Saobracaj` (WinForms) · **Verzija:** 3, 3.11.2026

Ovo je **jedini ulazni dokument** za izradu. Pročitaj ga ceo pre prvog koraka. Detaljnija objašnjenja su u `PLAN_RAZVOJA_OP.md` i `PLAN_MIGRACIJE_OP.md` (isti folder). Kada se razlikuju, važi ova specifikacija.

---

## 1. Cilj

U postojećem programu `Saobracaj`, u meniju **Transport**, napraviti deo **Planiranje**. On vodi jedan posao od najave do predaje:

1. Planer bira lokomotive (nedeljni i operativni plan).
2. Mašinski dispečer bira osoblje i šalje zadatak.
3. Faze posla se prate do predaje.

Princip je **„Ponudi mi“**: sistem nudi kandidate, a čovek potvrđuje.

**Nije u obimu ove faze:** tablet, automatsko slanje na uređaje, izmene postojećih formi, upis u postojeće tabele (vidi faza 9).

## 2. Okruženje

| Stavka | Vrednost |
|---|---|
| Rešenje | `Saobracaj.sln` → projekat `Saobracaj/Saobracaj.csproj` |
| Platforma | WinForms, .NET Framework **4.6.1**, C# **7.3** (bez `record`, `switch` izraza, `using var`, nullable referenci) |
| Baza | SQL Server, baza `Perftech_Beograd`, konekcija `WindowsFormsApplication1.Properties.Settings.NedraConnectionString` |
| Nova šema | `planiranje`, sve tabele sa prefiksom `OP_` |
| Korisnik | `Saobracaj.Sifarnici.frmLogovanje.user` |
| Postojeći pomoćni kod (koristi, **ne menjaj**) | `Agenti jobs/Db.cs` (`Db.Open/Command/Table/Lock/Text`), `Agenti jobs/GridSupport.cs` (`GridColumnFilters`, `GridTitles`), `Agenti jobs/ExcelRoutes.cs`, `Agenti jobs/RouteRules.cs` (`StationNames`, `TrainText`), `Agenti jobs/StationGeography.cs`, `Tema/` (`BaseForm`, `IzgledForme`, `KarticaPolja`), `MailRepository.cs` |
| Ulazni podaci | `Registar.xlsx`, `mreza_stanice.json`, `mreza_pruga.html`, dizajn ekrana (`*.dc.html`, `kp.css`, `canvas.json`) |

## 3. Obavezna pravila

1. **Postojeće ostaje kakvo jeste.** Ne menjaju se forme, klase ni tabele trasa, lokomotiva, stanica i pruga, kao ni `Planer`, `DispMain`, `IskoriscenjeTrasa`, `Sifarnici/*` i `Dokumenta/*`. Jedine izmene postojećih fajlova su:
   - `MainP.Designer.cs` / `MainP.cs`: nove grupe i dugmad u tabu Transport (odeljak 6);
   - `Saobracaj.csproj`: dodavanje novih fajlova.
2. **Svoji šifarnici.** Planiranje ima sopstvene tabele stanica, serija, lokomotiva, pruga i Registra, i trasa. One se jednom pune skriptom `OP_002` iz postojećih tabela, a posle se menjaju samo u novim formama.
3. **Tabela koja se samo čita koristi se direktno, bez kopije:**
   - `Najava`, `Delavci`, `Bolovanje`, `Dopust` / `DopustStavke`;
   - `Telegrami`, `Teretnica` / `TeretnicaStavke`, `Partnerji`;
   - `LocoTechnodis`, `OperativniPlanVozovi`.
   
   Čitaju se kroz poglede iz odeljka 4.3.
4. **Upis samo u šemu `planiranje`.** Jedini izuzetak (dorada KP-26-11-03-Osoblje): `OsobljeRepo` upisuje četiri kolone uloga u `dbo.Delavci` (`Masinovodja`, `PregledacKola`, `Vozovodja`, `Manevrista`). U proveri grep-om dozvoljen je samo taj red. Nijedan `INSERT/UPDATE/DELETE/MERGE` iz novog koda ne sme da ide na `dbo.`. Svaki repozitorijum piše samo u `planiranje.OP_*`. Pre svake predaje uradi proveru: `grep -rnE "(INSERT|UPDATE|DELETE|MERGE)[^;]*dbo\." Planiranje/` mora biti prazno.
5. **`dbo.PrugaStavke` se ne koristi** (ni čitanje ni upis), jer se menja u starom programu.
6. **Bez stranih ključeva prema `dbo`.** Veza ide preko kolona `Izvorni*` i `DeSifra` i proverava se u kodu.
7. **Bez SQL-a u formama.** Sav pristup bazi ide kroz klase u `Planiranje/Podaci/`, sa parametrima i bez lepljenja korisničkog unosa u upit.
8. **Konkurentnost:** svaki `UPDATE` tabele sa `RowVer` ima oblik `WHERE ID=@id AND RowVer=@rv`. Ako je promenjeno 0 redova, prikaži poruku „Podatke je u međuvremenu promenio drugi korisnik. Osvežite prikaz.“
9. **Evidencija:** svaka promena posla (faza, lokomotive, zadatak, odgovor, Teretnica) upisuje `OP_PosaoDogadjaj` u **istoj transakciji**.
10. **Interfejs se ne blokira:** čitanje ide kroz `Task.Run`, a forma se ne zatvara dok traje čuvanje (uzor je `DispMain.Start`).
11. **Izgled:**
    - forma nasleđuje `Saobracaj.Tema.BaseForm`;
    - ima partial `*.NoviIzgled.cs` sa `PrimeniNoviIzgled()` (uzor je `Sifarnici/frmLokomotive.NoviIzgled.cs`);
    - tabele koriste `GridColumnFilters` i `GridTitles`;
    - naslov forme počinje sa **„Planiranje · “**.
12. **Prava pristupa:** kao postojeće forme (`KorisnikGrupa`, `Forme`, `GrupeForme`), sa `public static string code = "frmOP…"`. Kodove upisuje administrator kroz `Administracija/frmForme`.
13. **Tekst:** srpski, latinica. Ćirilica iz Registra se čuva u `NazivCir` / `NazivOriginal`.
14. **SQL:** postojeće skripte `OP_001`–`OP_005` i `OP_900` se ne menjaju posle upotrebe. Svaka izmena šeme ide u novu skriptu `OP_0NN_Opis.sql`, idempotentnu i sa BOM UTF-8.
15. **Tablet se ne radi.** Zadatak se šalje mailom ili telefonom, a odgovor unosi dispečer.

## 4. Model podataka

Sve je u šemi `planiranje`. Tačne kolone su u `SQL/OP_001_Sema.sql` i u dodatku A.

### 4.1 Šifarnici planiranja (kopija + dopuna)

| Tabela | Svrha | Ključne kolone |
|---|---|---|
| `OP_Stanice` | stanice i službena mesta | `Naziv`, `NazivCir`, `UIC`, `VrstaMesta`, `Latitude/Longitude`, `Izvor` (STANICE/MREZA/REGISTAR/RUCNO), `IzvorniID` → `dbo.stanice.ID` |
| `OP_SerijeLokomotiva` | serije | `Oznaka`, `VrstaVuce` (E/D), `MasaT`, `SnagaKW`, `VmaxKmh`, `IzvorniID` |
| `OP_Lokomotive` | lokomotive | `Broj`, `SerijaID` → serija, `Vlasnistvo`, `TechnodisID`, `MaticnaStanicaID`, `IzvorniSmSifra` → `dbo.Mesta.SmSifra` |
| `OP_RegistarVerzija` | uvoz Registra | `Status` NACRT/VAZECA/ARHIVA/ODBACENA (jedna VAZECA) |
| `OP_Pruga` | pruga u verziji | `RedniBroj`, `IdentBroj`, `Naziv`, `Kategorija`, `Elektrificirana`, `IzvorniID` → `dbo.Pruga.ID` |
| `OP_PrugaMesto` | mesto na pruzi | `RB`, `StanicaID`, `Naziv`, `UIC`, `StacionazaM`, `MaxDuzinaVozaAB/BA`, `VmaxDesni/Levi`, `Klasa`, `Kategorija`, `PovezanoPo` |
| `OP_MrezaVeza` | graf mreže | `StanicaAID`, `StanicaBID`, `DuzinaKm`, `PrugaID`, `Izvor` (MREZA/REGISTAR) |
| `OP_TraseVerzija` | verzija trasa | `Godina`, `VaziOd/Do`, `Status` (jedna VAZECA po godini), `IzvorniDokumentID` |
| `OP_Trase` | trasa | `BrojVoza`, `PocetnaStanicaID/KrajnjaStanicaID`, `VremePolaska/Dolaska`, `DanPlus`, `MaxMasaT`, `MaxDuzinaM`, `AdHoc`, `AdHocStatus`, `StanjeRazlike`, `Greska` |
| `OP_TrasaStanice` | stanice trase | `TrasaID`, `RB`, `StanicaID`, `PrugaID` |

### 4.2 Tabele planiranja

| Tabela | Svrha |
|---|---|
| `OP_Parametri` | ključ/vrednost po opsegu (stanica, relacija, serija) i periodu `VaziOd/VaziDo` |
| `OP_TablicaMasa` | serija × pruga ili relacija × smer × **sloj** (RUCNA/FIZIKA/NAUCENA) → `MaxMasaT`, `BrojLokomotiva` |
| `OP_LokomotivaZauzetost` | SERVIS / NAMENSKA / PROJEKAT / OSTALO, `Od–Do`, `Projekat` |
| `OP_Posao` | posao za najavu: `NajavaID`, `RBDeonice`, `TrasaID`, prijem/predaja, planirana i stvarna vremena, `Faza` 1–7, `Status`, `OperativniVozID` |
| `OP_PosaoRuta` | deonice rute: `StanicaOdID/DoID`, `PrugaID`, `DuzinaKm`, `TrasaID`, `Stanje` (IMA_TRASU/NEMAMO_TRASU/ZAHTEV_ADHOC/ZATVOR) |
| `OP_PosaoLokomotiva` | `LokomotivaID`, `RB`, `Vucna`, `Od–Do`, `PraznaVoznjaMin` |
| `OP_PlanVerzija`, `OP_PlanVerzijaStavka` | snimak plana (NEDELJNI/OPERATIVNI) |
| `OP_OsobljePlan` | dopuna za `Delavci`: `DeSifra` (PK), `MaticnaStanicaID`, `UPlaniranju`, `Auto` (zna da vozi auto), `SamoDomicilna` (radi samo u matičnoj stanici) |
| `OP_OsobljeKvalifikacija` | `DeSifra`, `Vrsta` SERIJA/PRUGA, `SerijaID`/`PrugaID`, `VaziDo` |
| `OP_Turnus`, `OP_OsobljeTurnus` | turnus i ko ga radi od kog datuma |
| `OP_Smena` | `DeSifra`, `Datum`, `Vrsta` DNEVNA/NOCNA/SLOBODAN, `Izvor` TURNUS/RUCNO |
| `OP_Odsustvo` | **samo** ručna odsustva kojih nema u Bolovanje/Dopust |
| `OP_Zadatak` | `PosaoID`, `DeSifra`, `Uloga` (MASINOVODJA/PREGLEDAC_PRIJEM/PREGLEDAC_PREDAJA), `Status` (PREDLOZEN/POSLAT/PRIHVACEN/ODBIJEN/OTKAZAN) |
| `OP_PosaoDogadjaj` | `PosaoID`, `Vreme`, `Tip`, `Opis`, `Korisnik`, `PodaciJson` |

### 4.3 Pogledi (čitanje postojećih tabela)

| Pogled | Izvor | Najvažnije kolone |
|---|---|---|
| `OP_vNajava` | `dbo.Najava` + `stanice` | `NajavaID`, `BrojNajave`, **`PredvidjenoPrimanje`** (predviđeno vreme prijema), `PredvidjenaPredaja`, `StvarnoPrimanje`, `StvarnaPredaja`, `PlanOtpravnaID`/`PlanUputnaID` (granični prelaz ako postoji), `BrutoT`, `DuzinaM`, `BrojKola`, `RID` |
| `OP_vPosao` | `OP_Posao` + `OP_vNajava` | posao sa podacima najave |
| `OP_vOsoblje` | `dbo.Delavci` + `OP_OsobljePlan` | `DeSifra`, `Naziv`, `Telefon`, `Email`, uloge, `Aktivan`, `MaticnaStanicaID`, `UPlaniranju` |
| `OP_vOdsustvo` | `Bolovanje` + `Dopust/DopustStavke` + `OP_Odsustvo` | `DeSifra`, `Vrsta`, `Od`, `Do`, `Izvor` |
| `OP_vTelegram` | `dbo.Telegrami` | `IzvornaPrugaID`, `IzvornaStanicaOdID/DoID`, `Vrsta` (ZATVOR ako `Kolosek` sadrži „pruga“, inače UPOZORENJE), `VaziOd/Do`, `TrajeOd/Do` |
| `OP_vTeretnicaNajava` | `dbo.TeretnicaStavke` | `NajavaID`, `BrutoZbir` (Tara + Neto; jedinicu proveriti), `BrojKola` |
| `OP_vLokomotivaKalendar` | `OP_PosaoLokomotiva` + `OP_LokomotivaZauzetost` | kalendar lokomotive |
| `OP_vOsobljeDan` | `OP_Smena` + `OP_vOdsustvo` + `OP_Zadatak` | dan osobe |

Pogledi `OP_vOsoblje`, `OP_vOdsustvo`, `OP_vTelegram` i `OP_vTeretnicaNajava` prave se dinamički, posle provere kolona. Pročitaj poruke `OP_001` i dopuni pogled ako neka veza nije napravljena.

### 4.4 Parametri (ključevi u `OP_Parametri`)

`OBRADA_PRIJEM_MIN` (60), `OBRADA_PREDAJA_MIN` (60), `VOZNJA_MIN` (po relaciji, `Izvor` KNJIZICA), `BRZINA_PRAZNE_VOZNJE_KMH` (50), `GPS_STAROST_MAX_MIN` (60), `ODMOR_POSLE_SMENE_H` (12), `MAX_RAD_H` (12), `PLAN_HORIZONT_DANA` (7), `REZERVA_PRE_POLASKA_MIN` (30), `TEZINA_RAVNOMERNOST` (30), `TEZINA_SERVIS` (120), `MASA_LOK_U_VUCI` (0).

Vrednost se traži od najužeg ka najširem opsegu, i na svakom nivou važi samo red za koji je `VaziOd ≤ datum < VaziDo`:
1. stanica + stanica do + serija
2. stanica + stanica do
3. stanica
4. globalno

## 5. Struktura koda

```
Saobracaj/Saobracaj/Planiranje/
  docs/          SPECIFIKACIJA_PLANIRANJE.md (ovaj fajl), PLAN_RAZVOJA_OP.md, PLAN_MIGRACIJE_OP.md, README_plan_2026-11-01.md
  SQL/           OP_001_Sema … OP_005_Prava, OP_900_Ukloni
  Podaci/        PlaniranjeDb.cs, StaniceRepo.cs, LokomotiveRepo.cs, TraseRepo.cs, RegistarRepo.cs,
                 ParametriRepo.cs, NajaveRepo.cs, PosaoRepo.cs, OsobljeRepo.cs, OdsustvaRepo.cs,
                 TelegramiRepo.cs, DogadjajRepo.cs
  Modeli/        jednostavne klase (StanicaOP, LokomotivaOP, Posao, DeonicaRute, KandidatLokomotiva, ...)
  Pravila/       Latinica.cs, NaziviStanica.cs, ParametriPravila.cs, Ruta.cs, BrojLokomotiva.cs,
                 ProverePosla.cs, PredlogLokomotiva.cs, Turnusi.cs, PonudaOsoblja.cs,
                 RegistarCitac.cs, RegistarSifre.cs, RazlikeTrasa.cs
  Kontrole/      GanttPanel.cs (crtanje na OnPaint, prevlačenje)
  Forme/         frmOP*.cs + .Designer.cs + .NoviIzgled.cs
  Optimizator/   (faza 8, Python; nije deo .csproj)
```

Namespace: `Saobracaj.Planiranje`, `Saobracaj.Planiranje.Podaci`, `.Pravila`, `.Forme`.

### 5.1 Obrazac repozitorijuma

```csharp
namespace Saobracaj.Planiranje.Podaci
{
    internal static class PlaniranjeDb
    {
        internal const string Sema = "planiranje";
        internal static string Korisnik => string.IsNullOrWhiteSpace(Saobracaj.Sifarnici.frmLogovanje.user)
            ? System.Environment.UserName : Saobracaj.Sifarnici.frmLogovanje.user.Trim();
    }

    internal static class StaniceRepo
    {
        internal static DataTable Sve(bool samoAktivne)
        {
            using (var c = Saobracaj.Agenti_jobs.Db.Open())
                return Saobracaj.Agenti_jobs.Db.Table(c, null,
                    "SELECT ID, Naziv, NazivCir, UIC, VrstaMesta, Latitude, Longitude, Aktivna, Izvor, IzvorniID, RowVer " +
                    "FROM planiranje.OP_Stanice WHERE (@sve = 1 OR Aktivna = 1) ORDER BY Naziv",
                    "@sve", samoAktivne ? 0 : 1);
        }

        internal static void Sacuvaj(int id, byte[] rowVer, string naziv, string uic, double? lat, double? lon, bool aktivna)
        {
            using (var c = Saobracaj.Agenti_jobs.Db.Open())
            using (var cmd = Saobracaj.Agenti_jobs.Db.Command(c, null,
                "UPDATE planiranje.OP_Stanice SET Naziv=@n, UIC=@u, Latitude=@lat, Longitude=@lon, Aktivna=@a " +
                "WHERE ID=@id AND RowVer=@rv",
                "@n", naziv, "@u", (object)uic, "@lat", (object)lat, "@lon", (object)lon, "@a", aktivna, "@id", id, "@rv", rowVer))
                if (cmd.ExecuteNonQuery() == 0)
                    throw new DBConcurrencyException("Podatke je u međuvremenu promenio drugi korisnik. Osvežite prikaz.");
        }
    }
}
```

### 5.2 Obrazac transakcije posla (sa događajem)

```csharp
using (var c = Db.Open())
using (var tx = c.BeginTransaction())
{
    try
    {
        foreach (var lokId in izabrane) Db.Lock(c, tx, "OP_Lok_" + lokId);
        // ponovna provera preklapanja unutar transakcije (OP_vLokomotivaKalendar)
        // INSERT planiranje.OP_PosaoLokomotiva ...
        // UPDATE planiranje.OP_Posao SET Faza = 3, Izmenio = @k, Izmenjeno = SYSDATETIME() WHERE ID=@id AND RowVer=@rv
        DogadjajRepo.Upisi(c, tx, posaoId, "LOKOMOTIVE_POTVRDJENE", opis, json);
        tx.Commit();
    }
    catch { tx.Rollback(); throw; }
}
```

## 6. Meni Transport

U `MainP.Designer.cs`, tab `toolStripTabItem3` (Text = „Transport“), dodaj **dve** `Syncfusion.Windows.Forms.Tools.ToolStripEx` grupe po uzoru na `toolStripEx10`. Svojstva su `AutoSize = false`, `LayoutStyle = VerticalStackWithOverflow` i `Dock = None`. `Location.X` mora biti veći od desne ivice `toolStripEx26`, jer `MainP.NoviIzgled.UcitajStavkeIzRibona` ređa grupe po `Left`, a naziv kartice uzima iz `Text`. Dodaj ih u `toolStripTabItem3.Panel.Controls`.

```csharp
// MainP.cs (primer jednog dugmeta)
private void tsbOPNedeljniPlan_Click(object sender, EventArgs e)
{
    new Saobracaj.Planiranje.Forme.frmOPNedeljniPlan(Saobracaj.Planiranje.Forme.VrstaPlana.Nedeljni).Show();
}
```

| Grupa (`Text`) | Dugme (`Text`) | Otvara |
|---|---|---|
| **Planiranje** | Nedeljni plan | `frmOPNedeljniPlan(Nedeljni)` |
| | Operativni plan | `frmOPNedeljniPlan(Operativni)` |
| | Najave bez plana | `frmOPNedeljniPlan(Nedeljni, samoBezLokomotive: true)` |
| | Dostupnost osoblja | `frmOPDostupnost` |
| | Ponuda osoblja | `frmOPPonudaOsoblja` (bez posla: izbor iz liste poslova u fazi 3) |
| | Tok posla | `frmOPTokPosla` (izbor posla) |
| | Zahtevi za ad hoc trasu | `frmOPTrase(tab: AdHoc)` |
| **Šifarnici planiranja** | Trase: uvoz i pregled | `frmOPTrase` |
| | Registar pruga | `frmOPRegistar` |
| | Mreža i stanice | `frmOPStanice` |
| | Lokomotive i serije | `frmOPLokomotive` |
| | Osoblje i kvalifikacije | `frmOPOsoblje` |
| | Turnusi | `frmOPTurnusi` |
| | Tablica masa | `frmOPTablicaMasa` |
| | Parametri | `frmOPParametri` |

Postojeća dugmad (Planer, DispMain, Iskorišćenje trasa) se ne diraju.

## 7. Ekrani

Uzor za raspored su `*.dc.html` iz dizajna (ekrani 1–7). Podaci na njima su primer. Sve tabele imaju filtere po kolonama, sve liste se osvežavaju dugmetom „Osveži“, a forme 1, 3 i 5 i same, na 60 s, ako nema nesačuvanih izmena.

### 7.1 `frmOPNedeljniPlan` (ekran 1, Planer)
- **Zaglavlje:**
  - izbor nedelje (pon–ned) i prekidač Nedeljni/Operativni. Operativni plan je danas + sutra, po satima.
  - dugmad: „Uzmi najave“, „Sačuvaj verziju“, „Predloži plan“.
  - oznaka verzije, npr. „Verzija 3 · nacrt“.
- **Brojači:** poslova u planu · sa lokomotivom · bez lokomotive (crveno) · konflikata (crveno) · dvostruka vuča.
- **Gantt „Lokomotive po danima“** (`GanttPanel`):
  - red je aktivna lokomotiva (`OP_Lokomotive`), sa brojem i podnaslovom (vrsta vuče, GPS aktivan / zakup / „Nema GPS 7 h“);
  - trake:
    - posao je crvena traka (broj najave + relacija);
    - dvostruka vuča je tamna traka („1 od 2“);
    - zauzetost je šrafirana (servis, namenska + projekat);
    - konflikt ima žuti okvir;
  - poslednji red je „Bez lokomotive“, sa isprekidanim trakama;
  - dvoklik na traku otvara `frmOPPosao`;
  - prevlačenje trake na drugu lokomotivu radi kao izbor iz ponude (sve provere i potvrda).
- **Desno, „Konflikti i upozorenja“:** lista sa tačkom crveno/žuto/plavo, naslovom i objašnjenjem (odeljak 8.7).
- **Desno, „Poslovi bez lokomotive“:** najava, relacija, prijem i dugme „Ponudi“, koje otvara `frmOPPosao`.
- **„Uzmi najave“:** pravilo 8.1.
- **„Sačuvaj verziju“:** nova `OP_PlanVerzija` za period (`BrojVerzije` = max + 1) sa stavkama za sve poslove perioda.
- **„Predloži plan“:** pohlepni algoritam (8.6) bez upisa. Prikaže nacrt u Gantt-u (trake sa isprekidanim okvirom), a upisuje se posle „Prihvati predlog“. U fazi 8 radi optimizator.

### 7.2 `frmOPPosao` (ekran 2, Planer)
- **Zaglavlje:** „Najava {broj} · {otpravna} – {uputna}“, čip faze, dugme „Nazad na plan“.
- **Levo, „Podaci najave“ (samo čitanje):** otpravna, uputna, bruto, neto, dužina, broj kola, RID, prevoznik, izvor mase (čip „Čeka Teretnicu“ dok je masa iz najave).
- **Levo, „Vremenska linija“:** prijem → obrada → polazak (vožnja) → dolazak → predaja.
- **Levo, „Ruta“:** deonice iz `OP_PosaoRuta` sa stanjem (IMA_TRASU zeleno, NEMAMO_TRASU crveno sa dugmetom „Zatraži ad hoc“, ZATVOR crveno sa brojem telegrama).
- **Sredina, „Potreban broj lokomotiva“:** traka bruto prema najvećoj masi za seriju, i tekst predloga („Jedna lokomotiva nije dovoljna…“).
- **Sredina, „Ponuda lokomotiva“:** tri najbolja kandidata (8.5) sa kolonama #, lokomotive, lokacija (+ starost GPS-a), prazna vožnja, zauzetost, provere (čipovi) i „Izaberi“.
- **Desno, „Provere“:** 7 stavki (8.4) i brojač „6 od 7 u redu“.
- **Dno:** „Izabrano: …“, „Odbaci izbor“, „Potvrdi lokomotive i pošalji dispečeru“ (transakcija 5.2, faza 3).

### 7.3 `frmOPPonudaOsoblja` (ekran 3, mašinski dispečer)
- **Zaglavlje:** „Ponuda osoblja · Najava {broj}“, lokomotive, polazak, stanica, predaja.
- **Tabela „Mašinovođa“:** ime, turnus, lokacija (+ „posle smene“), serija (Da/Ne), pruga (Da/Ne), zauzetost i ocena (Preporučen / Nije na lokaciji / Zauzet / Nedostupan / Bez serije X / Bez pruge Y). Izbor je klikom na red (pravilo 8.9).
- **Tabele „Pregledač: prijem, {stanica}“ i „Pregledač: predaja, {stanica}“.**
- **Desno, „Zadatak za slanje“:** izabrane osobe, napomena za mašinovođu, „Pošalji zadatak“ (upis POSLAT i mail na `OP_vOsoblje.Email`; bez maila samo upis i poruka „Javiti telefonom“).
- **Desno, „Odgovori osoblja“:** za svaki poslat zadatak dugmad „Prihvatio“ i „Odbio“ (razlog). Posle odbijanja sistem odmah predlaže sledećeg kandidata iste uloge. Kad svi prihvate, posao prelazi u fazu 4 i upisuje se događaj.

### 7.4 `frmOPDostupnost` (ekran 4, mašinski dispečer)
- **Zaglavlje:** nedelja, filter Svi / Mašinovođe / Pregledači, dugmad „Generiši smene iz turnusa“ i „Dodaj odsustvo“.
- **Kalendar** (red = osoba iz `OP_vOsoblje` gde je `UPlaniranju = 1`, kolona = dan):
  - ćelija prikazuje dnevnu ili noćnu smenu, slobodan dan, godišnji, bolovanje, ili broj posla (zadatak POSLAT/PRIHVACEN);
  - legenda boja je kao na ekranu;
  - dvoklik na ćeliju menja smenu ručno (`Izvor = RUCNO`).
- **Desno, panel osobe:** uloga, matična lokacija, turnus, serije, pruge (čipovi), kvalifikacije važe do, „Izmeni turnus“, „Odsustva“.
- **Desno, „Zauzetost ove nedelje“:** zadaci i lokacija posle smene (stanica predaje poslednjeg zadatka).

### 7.5 `frmOPTokPosla` (ekran 5)
- **Koraci 1–7** (završeno zeleno, trenutno crveno, sledeće sivo), sa ulogom koja vodi korak.
- **„Evidencija događaja“:** `OP_PosaoDogadjaj` sa vremenom, naslovom i opisom.
- **„Sledeći koraci“:** šta se čeka (prihvatanje zadatka, Teretnica, potvrda prijema).
- **„Vreme po parametrima“:** obrada pri prijemu, vožnja, obrada pri predaji, očekivana predaja.
- **Dugmad:** „Prijem i pregled završen“ (faza 4 → 5 ako Dispečer još nije pokrenuo voz; inače čeka) i „Predaja u toku“ (faza 6).
- Faze 5 i 7 se ažuriraju čitanjem (8.10).

### 7.6 `frmOPTrase` (ekran 6), tabovi Uvoz iz Excela · Sve trase · Ad hoc
- **Uvoz:**
  - izbor `.xlsx`, godina važenja, izvor, period (postojeći dijalog `PeriodCenovnika`);
  - koraci: Datoteka → Pregled razlika → Potvrda → Objava;
  - brojači: redova, novih, izmenjenih, ukinutih, grešaka;
  - tabela razlika (pravilo 8.11), panel „Greške za ispravku“ i panel „Verzija i istorija“;
  - dugmad „Odbaci“ i „Potvrdi uvoz“.
- **Sve trase:** filter po verziji i godini, pregled i ručna ispravka stanice (izbor iz `OP_Stanice`).
- **Ad hoc:**
  - lista zahteva (`AdHocStatus = ZAHTEV`) sa poslom i deonicom;
  - unos trase (broj, stanice, vremena, max masa, važi od–do, napomena) → `ODOBRENA`;
  - odbijanje → `ODBIJENA`.
- **Ne piše u `OperativniPlanTrasa`.**

### 7.7 `frmOPRegistar` (ekran 7), tabovi Uvoz · Pregled pruga · Istorija verzija
- **Uvoz:**
  - datoteka i broj redova, pruga i pismo; ključ povezivanja je **naziv**;
  - koraci: Datoteka → Čišćenje i mapiranje → Pregled razlika → Potvrda;
  - brojači: redova, pruga, mesta bez stanice, novih mesta, izmenjenih vrednosti;
  - tabela mesta izabrane pruge (u Registru, u aplikaciji, UIC, klasa, kategorija, stanje);
  - tabela „Povezivanje brojeva pruga“ (redni broj, naziv, `dbo.Pruga`, stanje);
  - panel „Problemi pri čišćenju“, panel „Razlike prema trenutnom Registru“ i dugmad „Odbaci“ / „Potvrdi uvoz“.
- **Pregled pruga:** stablo pruga → mesta, sa svim kolonama.
- **Istorija:** verzije, ko je i kada potvrdio.

### 7.8 Šifarnici
| Forma | Sadržaj |
|---|---|
| `frmOPStanice` | Tabovi Stanice, Registar i Mapa (`WebBrowser`, Leaflet, natpis „© OpenStreetMap contributors“). Filteri „Samo SRB“, „Samo stanice“, „Za proveru“. Dugmad: „Spoji duplikate“ (reference na drugu stanicu se prebacuju na prvu, druga dobija `Aktivna = 0` i `DuplikatOd`), „Uvezi Registar“ i „Dopuni iz starog šifarnika“. Mapa: rutiranje, prevlačenje tačke (`KoordIzvor = RUCNO`), zatvor deonice klikom. Detalji su u odeljku 11. |
| `frmOPLokomotive` | Tabovi Lokomotive (serija, vlasništvo, zakupodavac, Technodis, matična stanica, aktivna), Serije, Zauzetost (sa proverom preklapanja sa poslovima) i „Dopuni iz starog šifarnika“. |
| `frmOPOsoblje` | Lista iz `OP_vOsoblje`: ime, uloge i aktivnost su samo za čitanje (iz Delavci). Menjaju se matična stanica i „u planiranju“, kvalifikacije (serija/pruga, važi do), turnus i odsustva (prikaz `OP_vOdsustvo`, unos u `OP_Odsustvo`). |
| `frmOPTurnusi` | Turnusi i „Generiši smene“ (period, osobe; pravilo 8.8). |
| `frmOPTablicaMasa` | Serija × pruga ili relacija × smer × sloj → max masa, broj lokomotiva, važi od–do. Uvoz iz Excela sa kolonama Serija, Pruga ili Od/Do, Smer, Sloj, MaxMasa, BrojLok. |
| `frmOPParametri` | Ključ, opseg (stanica, stanica do, serija), vrednost, jedinica, važi od–do, izvor, opis. |

## 8. Pravila računanja (`Planiranje/Pravila/`)

**8.1 Najave → poslovi.** Za period čitaju se `OP_vNajava` gde je `PredvidjenoPrimanje` u periodu i ne postoji posao (`Status <> 'OTKAZAN'`). Za svaku najavu pravi se `OP_Posao`:
- `PrijemStanicaID` / `PredajaStanicaID` iz `PlanOtpravnaID` / `PlanUputnaID`, preko `OP_Stanice.IzvorniID`;
- `PlaniraniPrijem = PredvidjenoPrimanje`;
- `Faza = 1` i događaj NAJAVA.

**8.2 Ruta.** Dijkstra po `OP_MrezaVeza` (neusmeren graf, težina km), od prijema do predaje. Svaka deonica dobija stanje:
- IMA_TRASU: postoji važeća trasa (`OP_TraseVerzija` VAZECA za godinu, ili ad hoc ODOBRENA u periodu) čije su početna i krajnja stanica na ruti, u istom smeru, i pokrivaju deonicu;
- NEMAMO_TRASU: takve trase nema;
- ZATVOR: aktivan zapis u `OP_vTelegram` za prugu i stanice deonice, u vremenu posla (`VaziOd–VaziDo`; dnevni interval `TrajeOd–TrajeDo`, ako postoji). Tada se traži ruta bez te veze; ako je nema, nastaje konflikt.

Telegram sa `Vrsta = UPOZORENJE` samo ide na listu upozorenja.

**8.3 Vremena.** Ako trasa postoji, polazak i dolazak su iz trase (`DanPlus`). Inače:
- polazak = prijem + `OBRADA_PRIJEM_MIN`;
- dolazak = polazak + `VOZNJA_MIN`, a ako ga nema, km / 40 km/h uz upozorenje;
- predaja = dolazak + `OBRADA_PREDAJA_MIN`.

Ako je predaja kasnija od `PredvidjenaPredaja`, to je upozorenje.

**8.4 Provere posla (7):**
1. vuča: sve deonice elektrificirane, inače samo serije D;
2. masa;
3. dužina voza ≤ najmanja `MaxDuzinaVoza` po smeru na ruti;
4. osovinsko opterećenje u okviru klase pruge;
5. zauzetost lokomotiva (`OP_vLokomotivaKalendar`, namenska samo za isti `Projekat`);
6. trasa: sve deonice IMA_TRASU;
7. vreme obrade uračunato.

**8.5 Masa i broj lokomotiva.** Bruto je `OP_vTeretnicaNajava.BrutoZbir` (preračunat u t) ako postoji, inače `Najava.Tezina`. Ako je `MASA_LOK_U_VUCI = 1`, dodaje se masa lokomotiva.

`MaxMasa(serija, deonica)` se traži:
- po sloju: RUCNA → FIZIKA → NAUCENA;
- po opsegu: relacija + smer → pruga + smer → pruga.

Za rutu važi najmanja vrednost. Broj lokomotiva:
- 1, ako je bruto ≤ MaxMasa(1);
- 2, ako je bruto ≤ red sa `BrojLokomotiva = 2` (ako tog reda nema: 2 × MaxMasa(1) uz upozorenje „Nema tablice za dvostruku vuču“);
- inače konflikt.

**8.5a Ponuda lokomotiva.**
- **Kandidati:** aktivne lokomotive dozvoljene vrste vuče; za dve lokomotive, parovi iste serije.
- **Lokacija u trenutku prijema:** svež GPS → stanica predaje poslednjeg posla pre tog trenutka → matična stanica.
- **Prazna vožnja** = ruta po mreži / `BRZINA_PRAZNE_VOZNJE_KMH`.
- **Isključuje se kandidat koji:**
  - je zauzet ili na servisu;
  - je namenska za drugi projekat;
  - ne stiže do prijema.
- **Ocena** (manja je bolja) = prazna vožnja (min) + `TEZINA_RAVNOMERNOST` × (broj poslova u nedelji − prosek) + `TEZINA_SERVIS`, ako je servis ≤ 48 h posle posla.
- Prikazuju se prva tri, sa razlogom.

**8.6 Predlog plana (do faze 8).** Poslovi bez lokomotive se obrađuju po `PlaniraniPrijem`. Svaki dobija najboljeg kandidata iz 8.5a, uz privremenu zauzetost u memoriji. Rezultat je nacrt.

**8.7 Konflikti i upozorenja:**
- **Konflikti:** preklapanje lokomotive; nema mašinovođe sa kvalifikacijom u terminu; posao ulazi u namenski period; više od 2 lokomotive; zatvor bez alternativne rute.
- **Upozorenja:** masa iznad tablice za jednu lokomotivu (predložena dvostruka); GPS stariji od parametra; masa iz najave (čeka Teretnicu); NEMAMO_TRASU; predaja posle predviđene; zadatak nije prihvaćen `REZERVA_PRE_POLASKA_MIN` pre polaska.

**8.8 Smene iz turnusa.** Za osobu, turnus i period, dan d je radni ako je `(d − Od) mod (DanaRada + DanaOdmora) < DanaRada`. Radni dan dobija DNEVNA ili NOCNA (`Od` = d + `Pocetak`, `Do` = `Od` + `TrajanjeMin`), ostali dani SLOBODAN. `Izvor = RUCNO` se ne prepisuje.

**8.9 Ponuda osoblja.**
- **Mašinovođa** iz `OP_vOsoblje` prolazi ako ispunjava sve uslove:
  - `Masinovodja = 1`, `Aktivan = 1`, `UPlaniranju = 1`;
  - poznaje svaku lokomotivu posla i svaku prugu na ruti (`OP_OsobljeLokomotiva`, `OP_OsobljePruga`), a `VaziDo` je prazan ili ≥ datum;
  - ima smenu koja pokriva polazak–predaju;
  - trajanje je ≤ `MAX_RAD_H`;
  - nema odsustvo u `OP_vOdsustvo`;
  - nema preklapanje sa drugim zadatkom i ima odmor ≥ `ODMOR_POSLE_SMENE_H`.
- **Ocena mašinovođe:** Preporučen ako je na lokaciji polaska; inače „Nije na lokaciji (+ vreme)“.
- **Pregledač:** `PregledacKola = 1` ili `Manevrista = 1`, matična stanica = stanica prijema ili predaje, smena pokriva vreme ± obrada, nema odsustva ni preklapanja.

**8.10 Faze iz postojećih podataka (samo čitanje).** U `OperativniPlanVozovi` traži se red gde je `AnnouncementFullId = NajavaID`, i upisuje se `OP_Posao.OperativniVozID`:
- `Started = 1` → faza 5 i `StvarniPolazak`;
- `Completed = 1` → faza 7, `StvarniDolazak`, `Status = ZATVOREN`.

Ako voza nema, koriste se `Najava.StvarnoPrimanje` i `StvarnaPredaja`. Svaka promena faze upisuje događaj.

**8.11 Razlike trasa.** Ključ je normalizovan `BrojVoza` + početna + krajnja.
- NOVA: nema je u prethodnoj verziji.
- IZMENJENA: razlika u polasku, dolasku, max masi, dužini, rangu ili danima, sa tekstom „Polje staro → novo“.
- UKINUTA: postoji u prethodnoj, a nema je u novoj.
- GRESKA: 00:00/00:00 ili prazno vreme, nepoznata stanica (predlog iz `NaziviStanica.Pronadji`) ili duplikat.

**8.12 Registar pruga** (`RegistarCitac`, `Sheet1`; kolone po brojevima iz reda 4):
- **Kategorija:** red sa jednom vrednošću u B koja počinje sa `МАГИСТРАЛНЕ` / `РЕГИОНАЛНЕ` / `ЛОКАЛНЕ` / `МАНИПУЛАТИВНЕ`.
- **Pruga:** red `N. Naziv` u B.
- **Mesto:** red sa vrednošću u F.
- **Kraj:** legenda od reda `*) Пруге на територији Косова...`.
- **Spojene ćelije:** `<mergeCells>` u `xl/worksheets/sheet1.xml`; vrednost važi za ceo opseg.
- **Kolone:**
  - E = stacionaža (m);
  - F = naziv;
  - G = vrsta (šifre 1–16);
  - H = Ј/Д;
  - I = kategorija;
  - J = klasa;
  - K, L = Vmax desni/levi;
  - M, N = max dužina i koloseci A→B;
  - O, P = isto za B→A;
  - Q = način regulisanja;
  - R = način osiguranja (šifre 1–11);
  - S = UIC;
  - T–W = vaga, rampa, posednutost, otvoreno za;
  - X = poluprečnik;
  - Y = nagib stanice;
  - Z, AA = merodavni uspon/pad;
  - AB, AC = otpor ↓/↑;
  - AD = profil;
  - AE = nadmorska visina.
- `КРАЈ ПРУГЕ` i `87+000` imaju `VrstaMesta = 'oznaka'`.
- `ПРУГА ЗАТВОРЕНА ЗА САОБРАЋАЈ` daje `Aktivno = 0`.
- Decimalni zarez se pretvara u tačku.
- Za čitanje koristi `System.IO.Compression` + `System.Xml.Linq`, bez NuGet-a.

**8.13 Povezivanje naziva** (`NaziviStanica`):
- **Normalizacija:** latinica (`Latinica.IzCirilice`), mala slova, bez kvačica, bez „st.“ i „gr.“, crtica postaje razmak, jedan razmak.
- **Ocena kandidata:**
  - 1,0 za isti normalizovan naziv;
  - 0,8 kad jedan naziv sadrži drugi;
  - 0,6 kad je Levenshtein ≤ 2;
  - −0,3 kad je kandidat dalje od 5 km (ako su koordinate poznate).
- **Redosled kod Registra:** prvo naziv (kod više kandidata najbliža koordinata), zatim UIC (ako ga oba imaju), na kraju ručno. U `PovezanoPo` se upisuje kako je mesto povezano.

## 9. Faze rada i prihvatanje

| Faza | Sadržaj | Prihvatanje |
|---|---|---|
| **0** | Na kopiji baze: `OP_001`, `OP_002`, `OP_003`, `OP_004`. Poruke i rezultate upiši u `docs/PROVERA_REZULTAT.md` | Drugi prolaz skripti ne dodaje redove; postojeće forme rade isto |
| **1** | `Podaci/`, `Pravila/` (Latinica, NaziviStanica, ParametriPravila), meni Transport sa praznim formama i pravima | Projekat se gradi; grupe su vidljive u meniju Transport; grep iz pravila 3.4 je prazan |
| **2** | Šifarnici 7.8 | Svaka forma čita i čuva; poruka o konkurentnosti radi; „Dopuni iz starog šifarnika“ dodaje samo nove redove |
| **3** | Trase 7.6, pravilo 8.11 | Dva uzastopna uvoza daju tačne razlike; Planer i Dispečer rade isto |
| **4** | Registar 7.7, pravila 8.12–8.13 | Pruga 1 je tačna od Beograda do granice; po nazivu je povezano ≥ 525 mesta |
| **5** | Plan i posao 7.1–7.2, pravila 8.1–8.7 | Najava sa 2.150 t na seriji 185 (tablica 1.600 t) daje predlog dvostruke vuče; posao bez trase dobija NEMAMO_TRASU; preklapanje se ne može potvrditi |
| **6** | Osoblje 7.3–7.4, pravila 8.8–8.9 | Smene iz turnusa su tačne; bolovanje iz `Bolovanje` isključuje osobu; odbijanje nudi sledećeg |
| **7** | Tok posla 7.5, pravilo 8.10 | Jedna najava prolazi faze 1–7; faze 5 i 7 dolaze iz Dispečera bez ručnog unosa |
| **8** | Optimizator (`Planiranje/Optimizator/`, Python 3.11, FastAPI, OR-Tools CP-SAT, nalog iz `OP_005`); `POST /plan/nedeljni` i `/plan/operativni` upisuju NACRT u `OP_PlanVerzija` | „Predloži plan“ vraća nacrt za ≤ 30 s; nema preklapanja ni kršenja kvalifikacija |
| **9** | Integracija, svaka stavka uz odobrenje: objava trasa u `OperativniPlanTrasa*`, „Prebaci u spisak vozova“ (`OperationsRepository.AddTrain/LinkNajava`) | Posle probnog rada od bar mesec dana |

Na kraju svake faze u `docs/PLAN_RAZVOJA_OP.md` upiši „Urađeno“, datum i otvorena pitanja.

## 10. Otvorena pitanja (pitaj korisnika kad dođeš do njih)

1. Masa lokomotive: samo u ukupnoj masi ili i u vuči (`MASA_LOK_U_VUCI`)?
2. Prve relacije i serije za ručnu tablicu masa; relacije za vreme iz knjižica.
3. Primer Excela trasa (provera `ExcelRoutes.Read`).
4. Vrednosti kolone `Kolosek` u `Telegrami`.
5. Jedinica `Tara` / `Neto` u `TeretnicaStavke` (t ili kg).
6. Kolona radnika i ključ u `dbo.Dopust`; koja vrednost `StatusGodmora` znači odobren odmor.
7. `Bolovanje.ZaposleniID` = `Delavci.DeSifra`? (proveriti)
8. OSM mapa: jednokratni uvoz ili stalno osvežavanje.

---

## 11. Dorada KP-26-11-03-Mreza i stanice (3.11.2026)

Ulaz: `05_Mreza_pruga/RAZVOJ_Mreza_i_stanice.md`. Kada se razlikuju, važi ovaj odeljak, jer dokument ne poznaje postojeći model.

**Odluke korisnika od 3.11.2026:**
- Podaci idu u postojeće OP_ tabele: `RegPruga` u `OP_Pruga`, `RegPrugaStavka` u `OP_PrugaMesto`, `MrezaGrana` u `OP_MrezaVeza`. Nema drugog grafa.
- „Dopuni iz starog šifarnika“ ostaje pored „Uvezi Registar“, jer Registar pokriva samo SRB.

**Skripte (redosled):** `OP_009_MrezaSema.sql`, zatim `OP_010_MrezaPodaci.sql` prvo sa `Potvrdi = 0`, pa sa `Potvrdi = 1`. Važeća verzija Registra mora da postoji. `OP_010` pravi `gen_OP_010.py` iz `mreza_srb.json`. Skripta briše `RegPruga`, `RegPrugaStavka` i `MrezaGrana`, ako postoje. Kopije `OP_Stanice_bak_20261103` i `OP_MrezaVeza_bak_20261103` ostaju, a `OP_900` ih ne briše: pre njega ih treba obrisati ručno, inače šema ostaje.

**Ključ mesta i uparivanje.** `RegKljuc` je `U:šifra`, `N:naziv` ili `GR:pruga:rb`. Uparivanje: ključ, Kod, šifra i naziv, šifra, naziv latinicom, naziv ćirilicom, alternativni naziv. Isti naziv sa drugom šifrom ili dalje od 30 km je drugo mesto. Šifra 16201 (Beograd ranžirna A i B) ne određuje mesto bez naziva.

**Duplikati.** Red ostaje sa `Aktivna = 0` i `DuplikatOd`, a sve reference prelaze na glavnu stanicu automatski u `OP_010`, a ručno kroz „Spoji duplikate“. Svako čitanje preko `IzvorniID` koristi `COALESCE(DuplikatOd, ID)`, jer `UX_OP_Stanice_IzvorniID` ne dozvoljava da glavna stanica preuzme više veza sa `dbo.stanice`.

**Graf.** `OP_MrezaVeza` je jedini graf. `DuzinaKm` ima tri decimale. Veza je jedinstvena po paru stanica: za par na dve pruge ostaje pruga višeg ranga. `Klasa` je D4, C3..., kao u `OP_PrugaMesto` (u `mreza_srb.json` se ta vrednost zove „kategorija“). Kategorija za raspodelu kilometara je `OP_Pruga.Kategorija`. Dužina deonice dodate iz naziva pruge koja je kraća od prave linije računa se iz koordinata.

**Zastoji na ekranu Mreža i stanice.** Po odluci korisnika, ruta se na ovom ekranu računa uvek: osovinsko, vuča, zatvor iz telegrama i pruga na KiM samo se prijavljuju, sa deonicama i kilometrima. Ako postoji i ruta bez zastoja, navodi se i nudi među kombinacijama. Ručni zatvor sa mape i dalje isključuje deonicu. Planer i ekran Trase isključuju zatvorenu deonicu kao i ranije.

**Rutiranje** (`Pravila/Rutiranje.cs`):
- Dijkstra, a za alternative Yen. Planer (`Ruta`) i ekran koriste isti graf; planer zaobilazi KiM.
- Vuča i osovinsko isključuju deonicu samo kad je podatak poznat i ne odgovara; deonica bez podatka ide na listu upozorenja. Osovinsko iz klase: A 16, B 18, C 20, D 22,5 t.
- Dužina voza daje upozorenje. Telegram ZATVOR isključuje deonicu, a levi ili desni kolosek na dvokolosečnoj pruzi daje upozorenje o smanjenom kapacitetu.
- Servis `GET /api/ruta` nije rađen: program računa u istom procesu, a drugog korisnika servisa nema.

**Trase.** „Izračunaj rutu“ u `frmOPTrase` puni `RastojanjeKm`, `KmMagistralno`, `KmRegionalno`, `KmLokalno`, `KmManipulativno`, `KmDizel`, `KmElektro`, `NajmanjiVmax`, `NajveciUspon`, `RutaIzracunata` i `OP_TrasaStanice`. Račun je za jednu izabranu trasu. Uvoz trasa prepoznaje i `NazivStari` i naziv ugašenog duplikata.

**Uvoz Registra.** „Uvezi Registar“ otvara `frmOPRegistar`, pa uvoz ima jedan put. Potvrda usklađuje stanice po ključu, a novo mesto dobija se bez koordinata i vidi se u filteru „Za proveru“. Koordinate, trasa, elektrifikacija i dužina iz koordinata se ne menjaju. Koordinate za novo mesto iz OSM-a daju samo skripte u `05_Mreza_pruga/skripte_v2`.

**Pravilo 3.1.** `Tema/IzgledForme` se ne menja: na tabu Mapa forma sama širi okvir tabova.

## 12. Dorada: AUTO, Samo domicilna i prevoz (4.11.2026)

Na tabu Osobe ekrana Osoblje i kvalifikacije su čekiranja „AUTO“ i „Samo domicilna“. Kolona `UPlaniranju` se na ekranu zove „Uključi u plan“. Čuvaju se u `OP_OsobljePlan`, skripta je `OP_011_OsobljeAutoDomicilna.sql`, a program ne radi bez nje. „Samo domicilna“ traži matičnu stanicu. Za matičnu stanicu se nude samo aktivne stanice sa `Drzava = SRB`. Skripte su `OP_011`, zatim `OP_012`.

**Samo domicilna** važi za pregledače: nudi se samo u matičnoj stanici. Pregledač bez oznake može i na drugu stanicu, sa vremenom puta po mreži.

**AUTO i par.** Važi za mašinovođe i pregledače koji nisu na lokaciji. Ko ima AUTO ide sam. Mašinovođa i pregledač za prijem idu u istu stanicu, pa se prvo traži par: ako polaze sa iste lokacije, oboje su slobodni do vremena prijema i jedno ima AUTO, drugo ide s njim, bez vozača. Par je ispred ostalih u ponudi i bira se sam uz izabranu osobu, a može da se promeni. Zadatak para počinje kad vožnja počne.

**Vozač iz osoblja.** Ako u paru niko nema AUTO, isti vozač vozi oboje do vremena prijema. Ko je sam bez AUTO (bez para ili sa druge lokacije) dobija svog vozača, a to važi i za pregledača na predaji. Vozač je najbliži slobodan mašinovođa, pregledač, vozovođa ili manevrista sa AUTO: aktivan, u planiranju, sa smenom i bez odsustva i drugog zadatka u vreme vožnje (do putnika, pa do cilja, po mreži, brzinom `BRZINA_PRAZNE_VOZNJE_KMH`). Ko nema ni par ni vozača ne može da se izabere.

**Vozač dobija zadatak** u `OP_Zadatak` sa ulogom `PREVOZ_MASINOVODJA`, `PREVOZ_PRIJEM` ili `PREVOZ_PREDAJA`, od početka vožnje do dolaska. Šalje se zajedno sa zadatkom osobe koju vozi i mora biti prihvaćen da bi posao prešao u fazu 4. Ako osoba odbije, prevoz se otkazuje. Ako vozač ili osoba iz para odbije, predlaže se novo rešenje za onoga ko je ostao. Povratak vozača se ne računa, po odluci korisnika.

## 13. Dorada: vremena turnusa (4.11.2026)

Dnevna smena je 07–19, a noćna 19–07. Skripta `OP_013_TurnusVremena.sql` menja postojeće turnuse i smene iz turnusa; dijalog za ručnu smenu nudi ista vremena.

## 14. Dorada: merodavne stanice najave i podaci za planera (4.11.2026)

Ovo menja pravilo 8.1 i opis `PlanOtpravnaID` / `PlanUputnaID`. Skripta je `OP_014_NajavaStanice.sql`, a program ne radi bez nje.

**Stanice.** Otpravna je stanica iz najave ako je u Srbiji, inače `OtpravnaGr`. Uputna je stanica iz najave ako je u Srbiji, inače `Uputnagr`. Granica „Nema“ znači da granica nije upisana. Strana stanica bez upisane granice ostaje kakva jeste, pa se za nju javlja „Nema rute“. Stanica je u Srbiji ako je tako vodi `OP_Stanice` ili `dbo.stanice`. Stanica sa praznom državom u obe tabele računa se kao strana.

**Podaci.** Planer vidi kilometre, bruto, BTTO, broj kola i tip kola u tabeli „Bez lokomotive“ i na ekranu Posao.
- **Km:** dužina rute po `OP_MrezaVeza`, a dužine su iz stacionaža Registra. Po odluci korisnika koristi se mreža iz Registra; `dbo.Daljinar` nije upotrebljiva (jedan red iz 2007). Kad zatvor iz telegrama traži obilazak, računa se ruta sa obilaskom.
- **Bruto:** iz Teretnice, a dok je nema iz najave, kao u pravilu računanja mase.
- **BTTO:** bruto (t) × km, po odluci korisnika.
- **Tip kola:** `dbo.Najava.SerijaVagona`.

## 15. Dorada: granična mesta (4.11.2026)

Državne granice iz Registra nose nazive koje planeri koriste (`Bogojevo gr.`, `Šid gr.`), a naziv iz Registra je u `NazivStari`. Skripta je `OP_015_GranicneStanice.sql`; pokreće se ponovo posle svakog `OP_010`, jer `OP_010` vraća nazive iz Registra. Ona spaja i stanice koje su isto mesto kao stavka Registra, a stanicu „Nema“ gasi. Polja Od i Do na mapi nude samo aktivne stanice sa `Drzava = SRB` koje su u mreži. Kad ruta ne postoji zbog osovinskog, mapa to kaže i navodi dužinu rute bez te provere. Planer proverava osovinsko samo na ekranu rute na mapi, a ne pri računu posla.

**Spajanje van mreže.** `OP_016_SpajanjeStanica.sql` spaja stanice van mreže sa mestom iz Registra istog ili sličnog naziva, po proverenom spisku. Spaja se kad se naziv poklapa i stanica je blizu, ili kad nema koordinate, a naziv je jasno isti. Stanice na KiM i u inostranstvu bez takvog para se ne spajaju, jer bi dale pogrešnu rutu.

## 16. Dorada: kombinacije trasa na mapi (4.11.2026)

Uz rutu na mapi prikazuje se do 5 kombinacija trasa (`Pravila/KombinacijeTrasa.cs`): duž najkraće rute, duž dve alternativne i lanci trasa preko cele mreže, jer trase često idu preko čvorova koje najkraća ruta zaobilazi (Beograd ranžirna). Lanac može biti najviše 40% duži od najkraće rute. Izabrana kombinacija crta svoju rutu na mapi.
- Trasa ulazi ako važi na izabrani dan (važeća verzija ili odobrena ad hoc), ako su joj obe krajnje stanice na ruti u smeru vožnje i ako dozvoljava uneti bruto i dužinu voza. Podatak koji trasa nema ne isključuje je.
- Za isti deo rute ostaje trasa sa najvećom masom, a broj ostalih se ispisuje uz nju.
- Poredak: najmanje kilometara bez trase, najmanje trasa, najveća dozvoljena masa, najkraća ruta.
- Ekran Posao prikazuje istu ponudu za najavu, sa brutom i dužinom iz najave. Račun je zajednički (`KombinacijeTrasa.SaZastojima`).
- Trase su u bazi bez vremena polaska i dolaska, pa se presedanje i dani saobraćaja ne proveravaju. Knjižice reda vožnje se za sada ne uključuju, po odluci korisnika.
- Trasa bez povezane stanice vezuje se po nazivu, a strana krajnja stanica po graničnom mestu sa tim susedom (`Vršac – Stamora Moravita` ide do `Vršac gr.`). Iz naziva `(Stamora Moravita) - Vršac` uzima se granica, iz početnog naziva `Kraljevo - Stalać` prva stanica, a iz krajnjeg `Novi Sad ranžirna - Subotica teretna` poslednja. To važi samo pri računu i ne upisuje se u `OP_Trase`.

## 17. Dorada: pregled plana (4.11.2026)

Skripta je `OP_017_PosaoStanice.sql`, a program ne radi bez nje. Ona ispravlja poslove napravljene pre `OP_014` koji kao stanicu imaju „Nema“.
- **Red lokomotive** je u jednom redu: broj, `EL` ili `DZ`, i `GPS` ako ga ima. Podatak koji lokomotiva nema se ne piše.
- **Kolona Najava** je `ID iz Najave / status` (`dbo.StatusVoza`). Traka i poruke koriste `BrojNajave`, a ID kad broja nema.
- **Telegram.** Plava zastavica sa brojem znači da na rutu utiče telegram: aktivan zatvor koji se obilazi ili prolazi, ili upozorenje na ruti. Desni klik u tabeli ili na traci daje spisak telegrama, izbor prikazuje telegram, a odatle se otvara ekran Telegrami na tom redu.

## 18. Dorada: šta ulazi u plan (4.11.2026)

Skripta je `OP_018_NajavaDatumi.sql`.
- **Ne ulazi:** najava u statusu 7 (Predato) i najava sa stvarnom predajom. Ne pravi se posao pri „Uzmi najave“ i ne prikazuje se u planu. Posao koji već ima lokomotivu ostaje u kalendaru.
- **Prazan datum** u `dbo.Najava` je `1900-01-01`. `OP_vNajava` umesto datuma pre 1950. daje NULL. To ispravlja pogrešno zatvaranje poslova i poruku „Predaja posle predviđene“.

## 19. Dorada: tab Pruge na ekranu Mreža i stanice (4.11.2026)

Bez nove skripte. Račun je u `Pravila/PregledPruga.cs`.
- Tab **Pruge** daje za svaku prugu kilometre, elektrifikaciju (Da, Ne, Delimično, Nepoznato) i broj trasa i najava koje je koriste; ispod su deonice izabrane pruge. Podrazumevano su prikazane samo pruge koje se koriste.
- **Koristi se** znači da je pruga na ruti važeće trase ili najave iz poslednjih 90 dana. Ni trase ni najave nemaju upisan put, pa je to najkraća ruta po mreži.
- **Elektrifikacija** se čita samo sa deonice (`OP_MrezaVeza.Elektrificirana`), a prazno je „Nepoznato“.
- Izračunata ruta na mapi dodatno navodi pruge kojima prolazi.

## 20. Dorada: elektrifikacija po zvaničnom spisku (4.11.2026)

Skripta je `OP_019_Elektrifikacija.sql`, a izvor Prilog 3.4 Elektrificirane pruge. Spisak važi i preko OSM podatka.
- **Cele pruge:** M1–M4, M7, M11–M20, M24–M31, R1, R13, R14 i L18. Sve ostale pruge nisu elektrificirane. Jagodina – Ćuprija – Paraćin je u važećem Registru deo pruge M2.
- **Delimično:** M5 Dimitrovgrad – granica, M6 Beograd Centar – Pančevo varoš, R5 Novi Sad – Sajlovo, R11 Kraljevo – Požega, R15 Mala Krsna – Požarevac. Na ovim prugama je `OP_Pruga.Elektrificirana` NULL, a o svakoj deonici odlučuje `OP_MrezaVeza`.
- Posle novog uvoza Registra skripta se pokreće ponovo: nova deonica uzima vrednost sa pruge, a skripta prekida rad bez izmena ako se broj deonica delimičnih pruga promeni.

## 21. Dorada: aktivne lokomotive (4.11.2026)

Skripta je `OP_020_AktivneLokomotive.sql`. Aktivne su 28 lokomotiva sa spiska korisnika i sve lokomotive serija 186 i 193; ostale postaju neaktivne, bez brisanja. Dodate su serije 186 i 383. Lokomotiva serije 186 još nema u bazi.

## Dodatak A. Kolone tabela (iz `SQL/OP_001_Sema.sql`)

Izvučeno automatski iz skripte. Kad se razlikuje, važi skripta.

### `planiranje.OP_Stanice`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Naziv` | nvarchar(100) | NOT NULL | latinica, kako se prikazuje |
| `NazivCir` | nvarchar(100) | NULL | original iz Registra / mreže |
| `UIC` | nvarchar(10) | NULL | šifra službenog mesta (UIC) |
| `Kod` | nvarchar(20) | NULL | dbo.stanice.Kod |
| `Drzava` | nvarchar(10) | NULL |  |
| `VrstaMesta` | nvarchar(40) | NULL | stanica, stajalište, rasputnica... |
| `Granicna` | bit | NOT NULL |  |
| `Latitude` | float | NULL |  |
| `Longitude` | float | NULL |  |
| `Aktivna` | bit | NOT NULL |  |
| `Izvor` | nvarchar(20) | NOT NULL | STANICE / REGISTAR / MREZA / RUCNO |
| `IzvorniID` | int | NULL | dbo.stanice.ID |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `Kreirao` | nvarchar(50) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_SerijeLokomotiva`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Oznaka` | nvarchar(20) | NOT NULL | 185, 193, 187... |
| `Opis` | nvarchar(100) | NULL |  |
| `VrstaVuce` | char(1) | NULL | E električna, D dizel |
| `MasaT` | decimal(10,2) | NULL |  |
| `SnagaKW` | decimal(10,2) | NULL |  |
| `VmaxKmh` | int | NULL |  |
| `DuzinaM` | decimal(6,2) | NULL |  |
| `Aktivna` | bit | NOT NULL |  |
| `IzvorniID` | int | NULL | dbo.LokomotivaSerija.ID |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_Lokomotive`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Broj` | nvarchar(20) | NOT NULL | 185 691 |
| `Naziv` | nvarchar(100) | NULL |  |
| `SerijaID` | int | NULL | → OP_SerijeLokomotiva.ID |
| `Vlasnistvo` | nvarchar(10) | NOT NULL |  |
| `Zakupodavac` | nvarchar(100) | NULL |  |
| `ImaGPS` | bit | NOT NULL |  |
| `TechnodisID` | nvarchar(50) | NULL | dbo.LocoTechnodis.IDLokomotive |
| `MaticnaStanicaID` | int | NULL | → OP_Stanice.ID |
| `MasaT` | decimal(10,2) | NULL | ako se razlikuje od serije |
| `Aktivna` | bit | NOT NULL |  |
| `IzvorniSmSifra` | nvarchar(20) | NULL | dbo.Mesta.SmSifra |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_OsobljePlan`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `DeSifra` | int | NOT NULL | PK; dbo.Delavci.DeSifra |
| `MaticnaStanicaID` | int | NULL | → OP_Stanice.ID |
| `UPlaniranju` | bit | NOT NULL |  |
| `Napomena` | nvarchar(300) | NULL |  |
| `Izmenio` | nvarchar(50) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_RegistarVerzija`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Naziv` | nvarchar(200) | NOT NULL |  |
| `OriginalniNaziv` | nvarchar(260) | NULL |  |
| `Sha256` | char(64) | NULL |  |
| `Sadrzaj` | varbinary(max) | NULL | originalna datoteka |
| `Status` | nvarchar(10) | NOT NULL |  |
| `BrojRedova` | int | NULL |  |
| `Izvor` | nvarchar(20) | NOT NULL | EXCEL / MIGRACIJA |
| `Uvezao` | nvarchar(50) | NOT NULL |  |
| `Uvezeno` | datetime2(0) | NOT NULL |  |
| `Potvrdio` | nvarchar(50) | NULL |  |
| `Potvrdjeno` | datetime2(0) | NULL |  |
| `Napomena` | nvarchar(1000) | NULL |  |

### `planiranje.OP_Pruga`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `RegistarVerzijaID` | int | NOT NULL | → OP_RegistarVerzija.ID |
| `RedniBroj` | int | NULL | redni broj u Registru (1, 2, 29...) |
| `IdentBroj` | nvarchar(10) | NULL | identifikacioni broj iz knjižica (102, 125...) |
| `Naziv` | nvarchar(300) | NOT NULL |  |
| `NazivCir` | nvarchar(300) | NULL |  |
| `Kategorija` | nvarchar(20) | NULL | MAGISTRALNA / REGIONALNA / LOKALNA / MANIPULATIVNA |
| `Elektrificirana` | bit | NULL |  |
| `Aktivna` | bit | NOT NULL |  |
| `IzvorniID` | int | NULL | dbo.Pruga.ID |

### `planiranje.OP_PrugaMesto`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `PrugaID` | int | NOT NULL | → OP_Pruga.ID |
| `RB` | int | NOT NULL | redosled na pruzi |
| `StanicaID` | int | NULL | → OP_Stanice.ID |
| `NazivOriginal` | nvarchar(150) | NULL | kako piše u Registru (ćirilica) |
| `Naziv` | nvarchar(150) | NOT NULL | latinica |
| `UIC` | nvarchar(10) | NULL |  |
| `VrstaMesta` | nvarchar(40) | NULL |  |
| `StacionazaKM` | decimal(10,3) | NULL |  |
| `StacionazaM` | int | NULL |  |
| `RastojanjeDoSledecegM` | int | NULL | m do sledećeg mesta na pruzi |
| `Kolosecnost` | tinyint | NULL | 1 jednokolosečna, 2 dvokolosečna |
| `Klasa` | nvarchar(10) | NULL | D4, D3... |
| `Kategorija` | nvarchar(10) | NULL | M, R, L |
| `VmaxDesni` | int | NULL |  |
| `VmaxLevi` | int | NULL |  |
| `MaxDuzinaVozaAB` | int | NULL | m, smer A→B |
| `KoloseciAB` | nvarchar(30) | NULL |  |
| `MaxDuzinaVozaBA` | int | NULL | m, smer B→A |
| `KoloseciBA` | nvarchar(30) | NULL |  |
| `NacinRegulisanja` | nvarchar(50) | NULL |  |
| `NacinOsiguranja` | nvarchar(50) | NULL |  |
| `Vaga` | nvarchar(20) | NULL |  |
| `Rampa` | nvarchar(20) | NULL |  |
| `Posednutost` | nvarchar(10) | NULL |  |
| `OtvorenoZa` | nvarchar(10) | NULL | P / T / P/T |
| `MinPoluprecnikM` | int | NULL |  |
| `NagibStanice` | decimal(6,2) | NULL | ‰ |
| `MerodavniUspon` | decimal(6,2) | NULL |  |
| `MerodavniPad` | decimal(6,2) | NULL |  |
| `MerodavniOtporUspon` | decimal(8,2) | NULL | daN |
| `MerodavniOtporPad` | decimal(8,2) | NULL |  |
| `OsovinskoOpterecenjeT` | decimal(6,2) | NULL |  |
| `TovarniProfil` | nvarchar(20) | NULL |  |
| `NadmorskaVisina` | decimal(8,2) | NULL |  |
| `OsOtpor` | nvarchar(50) | NULL | kolona iz dbo.PrugaStavke, značenje proveriti |
| `Aktivno` | bit | NOT NULL |  |
| `Promena` | nvarchar(20) | NULL | NOVO / IZMENJENO / NEMA_U_DATOTECI (pregled razlika) |
| `OpisPromene` | nvarchar(400) | NULL |  |
| `PovezanoPo` | nvarchar(10) | NULL | NAZIV / KOORDINATA / RUCNO (veza sa OP_Stanice) |

### `planiranje.OP_MrezaVeza`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `StanicaAID` | int | NOT NULL | → OP_Stanice.ID |
| `StanicaBID` | int | NOT NULL | → OP_Stanice.ID |
| `DuzinaKm` | decimal(8,2) | NOT NULL |  |
| `PrugaID` | int | NULL | → OP_Pruga.ID |
| `Elektrificirana` | bit | NULL |  |
| `Izvor` | nvarchar(20) | NOT NULL | MREZA / REGISTAR / RUCNO |
| `Aktivna` | bit | NOT NULL |  |

### `planiranje.OP_TraseVerzija`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Naziv` | nvarchar(200) | NOT NULL | "Trase 2027" |
| `Godina` | int | NOT NULL |  |
| `VaziOd` | date | NOT NULL |  |
| `VaziDo` | date | NOT NULL |  |
| `Status` | nvarchar(10) | NOT NULL |  |
| `Izvor` | nvarchar(30) | NOT NULL | EXCEL / MIGRACIJA / IZMENA |
| `PrethodnaVerzijaID` | int | NULL | → OP_TraseVerzija.ID |
| `OriginalniNaziv` | nvarchar(260) | NULL |  |
| `Sha256` | char(64) | NULL |  |
| `Sadrzaj` | varbinary(max) | NULL |  |
| `Uvezao` | nvarchar(50) | NOT NULL |  |
| `Uvezeno` | datetime2(0) | NOT NULL |  |
| `Objavio` | nvarchar(50) | NULL |  |
| `Objavljeno` | datetime2(0) | NULL |  |
| `IzvorniDokumentID` | bigint | NULL | dbo.OperativniPlanTraseDokument.ID |

### `planiranje.OP_Trase`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `VerzijaID` | int | NULL | → OP_TraseVerzija.ID; NULL samo za ad hoc |
| `BrojVoza` | nvarchar(20) | NOT NULL |  |
| `PocetnaStanicaID` | int | NULL | → OP_Stanice.ID |
| `KrajnjaStanicaID` | int | NULL | → OP_Stanice.ID |
| `PocetnaNaziv` | nvarchar(100) | NOT NULL | tekst iz datoteke |
| `KrajnjaNaziv` | nvarchar(100) | NOT NULL |  |
| `Relacija` | nvarchar(300) | NULL |  |
| `Rang` | nvarchar(10) | NULL |  |
| `VremePolaska` | time(0) | NULL |  |
| `VremeDolaska` | time(0) | NULL |  |
| `DanPlus` | tinyint | NOT NULL | dolazak sledećeg dana |
| `TrajanjeMin` | int | NULL |  |
| `DaniSaobracaja` | nvarchar(50) | NULL |  |
| `MaxMasaT` | decimal(10,2) | NULL |  |
| `MaxDuzinaM` | decimal(10,2) | NULL |  |
| `ProcenatKocenja` | decimal(6,2) | NULL |  |
| `Cena` | decimal(18,2) | NULL |  |
| `RastojanjeKm` | decimal(10,2) | NULL |  |
| `SerijaLokomotive` | nvarchar(50) | NULL | tekst iz datoteke |
| `Napomena` | nvarchar(1000) | NULL |  |
| `AdHoc` | bit | NOT NULL |  |
| `AdHocNajavaID` | int | NULL | dbo.Najava.ID za ad hoc trasu |
| `AdHocVaziOd` | date | NULL |  |
| `AdHocVaziDo` | date | NULL |  |
| `AdHocStatus` | nvarchar(10) | NULL | "Nemamo trasu" → zahtev |
| `StanjeRazlike` | nvarchar(10) | NULL |  |
| `OpisPromene` | nvarchar(400) | NULL |  |
| `Greska` | nvarchar(400) | NULL |  |
| `IzvornaTrasaID` | int | NULL | dbo.Trase.ID (godišnji šifarnik za obračun) |
| `IzvorniRouteID` | bigint | NULL | dbo.OperativniPlanTrasa.ID |
| `Kreirao` | nvarchar(50) | NOT NULL |  |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_TrasaStanice`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `TrasaID` | int | NOT NULL | → OP_Trase.ID |
| `RB` | int | NOT NULL |  |
| `StanicaID` | int | NOT NULL | → OP_Stanice.ID |
| `PrugaID` | int | NULL | → OP_Pruga.ID |

### `planiranje.OP_Parametri`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Kljuc` | nvarchar(50) | NOT NULL | OBRADA_PRIJEM_MIN, VOZNJA_MIN... |
| `StanicaID` | int | NULL | → OP_Stanice.ID |
| `StanicaDoID` | int | NULL | → OP_Stanice.ID |
| `SerijaID` | int | NULL | → OP_SerijeLokomotiva.ID |
| `Vrednost` | decimal(18,4) | NOT NULL |  |
| `Jedinica` | nvarchar(10) | NULL |  |
| `Opis` | nvarchar(300) | NULL |  |
| `Izvor` | nvarchar(20) | NULL | RUCNO / KNJIZICA (vreme po relaciji iz knjižica reda vožnje) |
| `VaziOd` | date | NOT NULL | verzije parametra |
| `VaziDo` | date | NULL |  |
| `Izmenio` | nvarchar(50) | NOT NULL |  |
| `Izmenjeno` | datetime2(0) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_TablicaMasa`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `SerijaID` | int | NOT NULL | → OP_SerijeLokomotiva.ID |
| `PrugaID` | int | NULL | → OP_Pruga.ID |
| `StanicaOdID` | int | NULL | → OP_Stanice.ID |
| `StanicaDoID` | int | NULL | → OP_Stanice.ID |
| `Smer` | char(2) | NULL |  |
| `BrojLokomotiva` | tinyint | NOT NULL |  |
| `Sloj` | nvarchar(10) | NOT NULL |  |
| `MaxMasaT` | decimal(10,2) | NOT NULL |  |
| `VaziOd` | date | NULL |  |
| `VaziDo` | date | NULL |  |
| `Napomena` | nvarchar(300) | NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_LokomotivaZauzetost`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `LokomotivaID` | int | NOT NULL | → OP_Lokomotive.ID |
| `Vrsta` | nvarchar(10) | NOT NULL |  |
| `Od` | datetime2(0) | NOT NULL |  |
| `Do` | datetime2(0) | NOT NULL |  |
| `Projekat` | nvarchar(100) | NULL | za NAMENSKA/PROJEKAT: samo poslovi tog projekta |
| `Napomena` | nvarchar(300) | NULL |  |
| `Kreirao` | nvarchar(50) | NOT NULL |  |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_Posao`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `NajavaID` | int | NOT NULL | dbo.Najava.ID (samo čitanje) |
| `RBDeonice` | tinyint | NOT NULL | ako se najava vozi u više delova |
| `TrasaID` | int | NULL | → OP_Trase.ID |
| `PrijemStanicaID` | int | NULL | → OP_Stanice.ID |
| `PredajaStanicaID` | int | NULL | → OP_Stanice.ID |
| `PlaniraniPrijem` | datetime2(0) | NULL |  |
| `PlaniraniPolazak` | datetime2(0) | NULL |  |
| `PlaniraniDolazak` | datetime2(0) | NULL |  |
| `PlaniranaPredaja` | datetime2(0) | NULL |  |
| `StvarniPolazak` | datetime2(0) | NULL |  |
| `StvarniDolazak` | datetime2(0) | NULL |  |
| `Faza` | tinyint | NOT NULL |  |
| `Status` | nvarchar(10) | NOT NULL |  |
| `PotrebnoLokomotiva` | tinyint | NULL | predlog iz tablice masa |
| `Projekat` | nvarchar(100) | NULL | za namenske lokomotive |
| `OperativniVozID` | bigint | NULL | dbo.OperativniPlanVozovi.ID (veza sa Dispečerom) |
| `Napomena` | nvarchar(1000) | NULL |  |
| `Kreirao` | nvarchar(50) | NOT NULL |  |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `Izmenio` | nvarchar(50) | NULL |  |
| `Izmenjeno` | datetime2(0) | NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_PosaoLokomotiva`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `PosaoID` | int | NOT NULL | → OP_Posao.ID |
| `LokomotivaID` | int | NOT NULL | → OP_Lokomotive.ID |
| `RB` | tinyint | NOT NULL | "1 od 2" |
| `Vucna` | bit | NOT NULL |  |
| `Od` | datetime2(0) | NOT NULL | zauzetost sa obradom |
| `Do` | datetime2(0) | NOT NULL |  |
| `PraznaVoznjaMin` | int | NULL |  |
| `Potvrdio` | nvarchar(50) | NULL |  |
| `Potvrdjeno` | datetime2(0) | NULL |  |

### `planiranje.OP_PosaoRuta`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `PosaoID` | int | NOT NULL | → OP_Posao.ID |
| `RB` | int | NOT NULL |  |
| `StanicaOdID` | int | NOT NULL | → OP_Stanice.ID |
| `StanicaDoID` | int | NOT NULL | → OP_Stanice.ID |
| `PrugaID` | int | NULL | → OP_Pruga.ID |
| `DuzinaKm` | decimal(8,2) | NULL |  |
| `TrasaID` | int | NULL | → OP_Trase.ID |
| `Stanje` | nvarchar(15) | NOT NULL |  |
| `Napomena` | nvarchar(300) | NULL | npr. broj telegrama o zatvoru |

### `planiranje.OP_PlanVerzija`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Vrsta` | nvarchar(10) | NOT NULL |  |
| `PeriodOd` | date | NOT NULL |  |
| `PeriodDo` | date | NOT NULL |  |
| `BrojVerzije` | int | NOT NULL |  |
| `Status` | nvarchar(10) | NOT NULL |  |
| `Napomena` | nvarchar(1000) | NULL |  |
| `Kreirao` | nvarchar(50) | NOT NULL |  |
| `Kreirano` | datetime2(0) | NOT NULL |  |
| `Potvrdio` | nvarchar(50) | NULL |  |
| `Potvrdjeno` | datetime2(0) | NULL |  |

### `planiranje.OP_PlanVerzijaStavka`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `PlanVerzijaID` | int | NOT NULL | → OP_PlanVerzija.ID |
| `PosaoID` | int | NOT NULL | → OP_Posao.ID |
| `LokomotivaID` | int | NULL | → OP_Lokomotive.ID; NULL = bez lokomotive |
| `RB` | tinyint | NULL |  |
| `Od` | datetime2(0) | NULL |  |
| `Do` | datetime2(0) | NULL |  |

### `planiranje.OP_OsobljeKvalifikacija`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `DeSifra` | int | NOT NULL | → OP_OsobljePlan.DeSifra |
| `Vrsta` | nvarchar(10) | NOT NULL |  |
| `SerijaID` | int | NULL | → OP_SerijeLokomotiva.ID |
| `PrugaID` | int | NULL | → OP_Pruga.ID |
| `VaziDo` | date | NULL |  |
| `Napomena` | nvarchar(300) | NULL |  |

### `planiranje.OP_Turnus`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `Naziv` | nvarchar(100) | NOT NULL | "Dnevna 07–19, 2 rada / 2 odmor" |
| `Pocetak` | time(0) | NOT NULL |  |
| `TrajanjeMin` | int | NOT NULL |  |
| `DanaRada` | tinyint | NOT NULL |  |
| `DanaOdmora` | tinyint | NOT NULL |  |
| `Nocna` | bit | NOT NULL |  |
| `Aktivan` | bit | NOT NULL |  |

### `planiranje.OP_OsobljeTurnus`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `DeSifra` | int | NOT NULL | → OP_OsobljePlan.DeSifra |
| `TurnusID` | int | NOT NULL | → OP_Turnus.ID |
| `Od` | date | NOT NULL | prvi radni dan ciklusa |
| `Do` | date | NULL |  |

### `planiranje.OP_Smena`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `DeSifra` | int | NOT NULL | → OP_OsobljePlan.DeSifra |
| `Datum` | date | NOT NULL |  |
| `Vrsta` | nvarchar(10) | NOT NULL |  |
| `Od` | datetime2(0) | NULL |  |
| `Do` | datetime2(0) | NULL |  |
| `Izvor` | nvarchar(10) | NOT NULL |  |
| `LokacijaPosleStanicaID` | int | NULL | → OP_Stanice.ID |
| `Izmenio` | nvarchar(50) | NULL |  |

### `planiranje.OP_Odsustvo`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `DeSifra` | int | NOT NULL | → OP_OsobljePlan.DeSifra |
| `Vrsta` | nvarchar(10) | NOT NULL |  |
| `Od` | date | NOT NULL |  |
| `Do` | date | NOT NULL |  |
| `Napomena` | nvarchar(300) | NULL |  |
| `Kreirao` | nvarchar(50) | NOT NULL |  |

### `planiranje.OP_Zadatak`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | int | NOT NULL | PK; identity |
| `PosaoID` | int | NOT NULL | → OP_Posao.ID |
| `DeSifra` | int | NOT NULL | → OP_OsobljePlan.DeSifra |
| `Uloga` | nvarchar(20) | NOT NULL |  |
| `StanicaID` | int | NULL | → OP_Stanice.ID |
| `Od` | datetime2(0) | NOT NULL |  |
| `Do` | datetime2(0) | NOT NULL |  |
| `Status` | nvarchar(10) | NOT NULL |  |
| `RazlogOdbijanja` | nvarchar(200) | NULL |  |
| `NapomenaZaOsobu` | nvarchar(1000) | NULL |  |
| `Poslao` | nvarchar(50) | NULL |  |
| `Poslato` | datetime2(0) | NULL |  |
| `Odgovoreno` | datetime2(0) | NULL |  |
| `UneoOdgovor` | nvarchar(50) | NULL |  |
| `RowVer` | rowversion | NOT NULL |  |

### `planiranje.OP_PosaoDogadjaj`

| Kolona | Tip | Null | Napomena |
|---|---|---|---|
| `ID` | bigint | NOT NULL | PK; identity |
| `PosaoID` | int | NOT NULL | → OP_Posao.ID |
| `Vreme` | datetime2(0) | NOT NULL |  |
| `Tip` | nvarchar(30) | NOT NULL | NAJAVA, LOKOMOTIVE_POTVRDJENE, ZADATAK_POSLAT, ODGOVOR, FAZA, TERETNICA... |
| `Opis` | nvarchar(500) | NOT NULL |  |
| `Korisnik` | nvarchar(50) | NOT NULL |  |
| `PodaciJson` | nvarchar(max) | NULL |  |

