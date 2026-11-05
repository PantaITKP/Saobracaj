# Plan razvoja: Planiranje u meniju Transport

Planiranje lokomotiva, osoblja i trasa za Kombinovani prevoz. Verzija 3, 3.11.2026.

Ovo je radni nalog za Claude Code. Pre svakog koraka pročitaj ceo fajl, zatim `PLAN_MIGRACIJE_OP.md` i SQL skripte u `SQL/`. Radi fazu po fazu i ne prelazi na sledeću dok kriterijumi prihvatanja prethodne nisu ispunjeni.

Izvori plana:
- `README_plan_2026-11-01.md`: specifikacija, prezentacija, dizajn, mapa mreže.
- Dizajn 7 ekrana: `canvas.json`, `*.dc.html`, `kp.css`.
- Pregled postojećeg koda: `Agenti jobs`, `Sifarnici`, `Dokumenta`, `MainP`.
- Odluke korisnika od 2. i 3.11.2026 (odeljak 2).

---

## 0. Pravila za rad (obavezno)

1. **Šta znači „odvojeno“.** Postojeće forme i tabele trasa, lokomotiva, stanica i pruga **ostaju kakve jesu**: `frmStanice`, `frmLokomotive`, `frmSerijeLokomotiva`, `frmTrase`, `frmPruge`, `Planer`, `DispMain`, `IskoriscenjeTrasa` i njihove tabele. Planiranje dobija **svoje šifarnike** (`OP_*` tabele) i svoje forme. Šifarnici se jednom napune iz postojećih tabela, a posle se održavaju samo kroz nove forme.
2. **Gde je kod.** Sve je u postojećem projektu `Saobracaj`, u folderu `Saobracaj/Planiranje/`, namespace `Saobracaj.Planiranje`. Ceo deo se otvara iz menija **Transport** (odeljak 4). Nema posebnog `.exe`.
3. **Baza:** ista baza i ista konekcija (`WindowsFormsApplication1.Properties.Settings.NedraConnectionString`). Nove tabele su u šemi **`planiranje`**, sa prefiksom **`OP_`** (npr. `planiranje.OP_Posao`).
4. **Pravilo čitanja.** Tabela koju planiranje **samo čita**, a ne puni i ne menja, koristi se direktno, bez kopije. To su:
   - Najava, Delavci, Bolovanje, Dopust / DopustStavke;
   - Telegrami, Teretnica / TeretnicaStavke, Partnerji;
   - LocoTechnodis, OperativniPlanVozovi;
   - DelavciLokMasin / DelavciPrugaMasin (samo za prvo punjenje kvalifikacija).
   
   Kopija postoji samo za šifarnike koje planiranje menja ili dopunjuje: stanice, serije, lokomotive, pruge i Registar, trase.
5. **Nova forma nikad ne piše u `dbo`.** Sav upis ide u šemu `planiranje`, kroz repozitorijume u `Planiranje/Podaci`. Svaki repozitorijum ima konstantu sa šemom, a pregled koda proverava da nijedan `INSERT/UPDATE/DELETE` ne ide na `dbo.`. Jedini izuzetak su stavke faze 9, posle probnog rada i uz odobrenje.
6. **Tehnologija:** WinForms, .NET Framework **4.6.1**, C# **7.3**, kao ceo projekat. Koristi postojeće: `Agenti jobs/Db.cs` (isti assembly, `internal` je dostupan), `GridSupport.cs`, `ExcelRoutes.cs`, `RouteRules.cs` (`StationNames`, `TrainText`), `StationGeography.cs`, `Tema` (`BaseForm`, `IzgledForme`, `KarticaPolja`). Postojeći fajlovi se **ne menjaju**. Ako je potrebna izmena, kopiraj klasu u `Planiranje/Zajednicko/` pod drugim imenom.
7. **`dbo.PrugaStavke` se ne koristi**, jer se trenutno menja u starom programu. Mesta na pruzi dolaze samo iz uvoza Registra, a mreža iz `OP_003_Mreza.sql`.
8. **Najava se ne kopira.** `OP_Posao` čuva samo `NajavaID`, a najava se čita kroz `planiranje.OP_vNajava`.
   - Predviđeno vreme prijema je **`Najava.PredvidjenoPrimanje`** (u formi Najava: `dtpPredvidjenoPrimanje`).
   - Predviđena predaja je `PredvidjenaPredaja`, a stvarna vremena su `StvarnoPrimanje` i `StvarnaPredaja`.
   - Relacija za planiranje je Otpravna → Uputna. Kod prevoza preko granice važe `OtpravnaGr` / `Uputnagr` (`PlanOtpravnaID` / `PlanUputnaID`).
9. **Osoblje se ne kopira.**
   - Forme čitaju `dbo.Delavci` kroz `planiranje.OP_vOsoblje`.
   - Podaci koje Delavci nema (matična stanica za planiranje, „u planiranju“) su u `OP_OsobljePlan`, sa ključem `DeSifra`.
   - Kvalifikacije, turnusi, smene, odsustva i zadaci koriste `DeSifra`.
   - Bolovanja i godišnji se čitaju iz `Bolovanje` i `Dopust/DopustStavke` (`OP_vOdsustvo`). U `OP_Odsustvo` se unosi samo ono čega tamo nema (slobodan dan, ostalo).
10. **Nema stranih ključeva prema `dbo`.** Veza je u kolonama `Izvorni*` i `DeSifra`, a proverava se u kodu.
11. **Stanice ↔ Registar ↔ mreža se povezuju po nazivu.** Koordinata pomaže kod istih naziva, a UIC samo kad postoji. Po README-u, po nazivu se poklapa 525 od 873 službena mesta; ostatak se potvrđuje ručno.
12. **Tablet se ne radi u ovoj fazi.** Zadatak se šalje mailom ili telefonom, a odgovor upisuje dispečer.
13. **GPS** se čita iz Technodis-a (`LocoTechnodis`, `OP_Lokomotive.TechnodisID`). Ako je pozicija starija od `GPS_STAROST_MAX_MIN`, uzima se stanica predaje poslednjeg posla.
14. **Konkurentnost:** `RowVer` u svakom `UPDATE`. Kad je promenjeno 0 redova, poruka je „Podatke je u međuvremenu promenio drugi korisnik. Osvežite prikaz.“
15. **Evidencija:** svaka promena posla upisuje red u `OP_PosaoDogadjaj` u istoj transakciji, sa korisnikom `Sifarnici.frmLogovanje.user`.
16. **Bez SQL-a u formama**, samo kroz repozitorijume. Tako konačna migracija menja samo imena tabela.
17. **Izgled i prava:**
    - Forme nasleđuju `Saobracaj.Tema.BaseForm` i imaju partial `*.NoviIzgled.cs` sa `PrimeniNoviIzgled()`.
    - Prava pristupa idu kao u postojećim formama (`KorisnikGrupa`, `Forme`, `GrupeForme`), kod forme je `frmOP...`.
    - Tekst je na srpskom, latinicom.
18. **SQL skripte** pokreće administrator ručno, prvo na kopiji baze. Izmena šeme ide u novu skriptu `OP_0NN_*.sql`, idempotentno.
19. Svaki novi `.cs` / `.Designer.cs` se dodaje u `Saobracaj.csproj`. Izmena `.csproj` (dodavanje fajlova) i `MainP` (nova dugmad) su jedine izmene postojećih fajlova.

---

## 1. Cilj i tok posla

| Faza | Naziv | Ko vodi | Gde |
|---|---|---|---|
| 1 | Najava prevoznika | Služba pripreme | postojeća forma Najava |
| 2 | Plan lokomotive | Planer | Nedeljni plan, Posao |
| 3 | Osoblje i zadatak | Mašinski dispečer | Ponuda osoblja, Dostupnost |
| 4 | Prijem i pregled | Pregledač, prijem | Tok posla (ručna potvrda) |
| 5 | U putu | Mašinovođa, operater | čita se iz `OperativniPlanVozovi.Started` |
| 6 | Predaja u toku | Pregledač, predaja | Tok posla (ručna potvrda) |
| 7 | Predato | zatvaranje | `Completed` ili `Najava.StvarnaPredaja` |

Princip je **„Ponudi mi“**: sistem nudi, čovek potvrđuje. Postoje dva plana: **nedeljni** i **operativni**.

## 2. Odluke

| Tema | Odluka |
|---|---|
| Gde | program Saobracaj, meni **Transport**, folder `Planiranje/` |
| Odvojeno | postojeće forme i tabele bez izmena; planiranje ima svoje šifarnike (`OP_*`) |
| Baza | ista baza i konekcija, šema `planiranje`, prefiks `OP_` |
| Šifarnici (kopija + dopuna) | `OP_Stanice`, `OP_SerijeLokomotiva` → `OP_Lokomotive`, `OP_TraseVerzija` / `OP_Trase`, `OP_Pruga` / `OP_PrugaMesto` (Registar) |
| Samo čitanje (bez kopije) | Najava, Delavci, Bolovanje, Dopust, Telegrami, Teretnica, Partnerji, LocoTechnodis, OperativniPlanVozovi |
| Posao | nova tabela `OP_Posao` + `OP_PosaoRuta` |
| Parametri | nova tabela `OP_Parametri` sa verzijama |
| PrugaStavke | ne koristi se |
| Stanice ↔ Registar | po nazivu |
| Masa | Najava, kasnije Teretnica; tablica MaxMasa u 3 sloja |
| Zatvori | `Telegrami` |
| Optimizator | Python + OR-Tools CP-SAT + FastAPI (faza 8) |
| Tablet | kasnije |

## 3. Tabele (šema `planiranje`)

Sve tabele i pogledi su u `SQL/OP_001_Sema.sql`: 26 tabela i 8 pogleda.

| Grupa | Tabela | Svrha |
|---|---|---|
| Šifarnici | `OP_Stanice` | stanice i službena mesta: naziv (lat/ćir), UIC, vrsta, koordinate, izvor |
| | `OP_SerijeLokomotiva` | serija: vrsta vuče E/D, masa, snaga, Vmax |
| | `OP_Lokomotive` | lokomotiva → serija, sopstvena/zakup, Technodis, matična stanica |
| | `OP_OsobljePlan` | dopuna za `Delavci`: matična stanica, „u planiranju“ (ključ `DeSifra`) |
| Registar | `OP_RegistarVerzija`, `OP_Pruga`, `OP_PrugaMesto` | uvoz Registra po verzijama, mesta na pruzi |
| | `OP_MrezaVeza` | graf mreže (A–B, km) |
| Trase | `OP_TraseVerzija`, `OP_Trase`, `OP_TrasaStanice` | trase po godini i verziji, ad hoc, zahtev za ad hoc |
| Plan | `OP_Parametri`, `OP_TablicaMasa`, `OP_LokomotivaZauzetost` | pravila i zauzetost |
| | `OP_Posao`, `OP_PosaoRuta`, `OP_PosaoLokomotiva` | posao, ruta, lokomotive |
| | `OP_PlanVerzija`, `OP_PlanVerzijaStavka` | verzije plana |
| Osoblje | `OP_OsobljeKvalifikacija`, `OP_Turnus`, `OP_OsobljeTurnus`, `OP_Smena`, `OP_Odsustvo`, `OP_Zadatak` | po `DeSifra` |
| Tok | `OP_PosaoDogadjaj` | evidencija |
| Pogledi | `OP_vNajava`, `OP_vPosao` | najava i posao |
| | `OP_vOsoblje` | Delavci + OP_OsobljePlan |
| | `OP_vOdsustvo` | Bolovanje + Dopust + OP_Odsustvo |
| | `OP_vTelegram` | zatvori i upozorenja |
| | `OP_vTeretnicaNajava` | masa iz Teretnice |
| | `OP_vLokomotivaKalendar`, `OP_vOsobljeDan` | kalendari |

**Parametri** (početne vrednosti su u `OP_002`): `OBRADA_PRIJEM_MIN`, `OBRADA_PREDAJA_MIN`, `BRZINA_PRAZNE_VOZNJE_KMH`, `GPS_STAROST_MAX_MIN`, `ODMOR_POSLE_SMENE_H`, `MAX_RAD_H`, `PLAN_HORIZONT_DANA`, `REZERVA_PRE_POLASKA_MIN`, `TEZINA_RAVNOMERNOST`, `TEZINA_SERVIS`, `MASA_LOK_U_VUCI`. Vreme vožnje po relaciji je `VOZNJA_MIN` (`StanicaID` + `StanicaDoID`, `Izvor = 'KNJIZICA'`).

Vrednost parametra se traži od najužeg ka najširem opsegu:
1. stanica + stanica do + serija,
2. stanica + stanica do,
3. stanica,
4. globalno.

Na svakom nivou važi samo red za koji je `VaziOd ≤ datum < VaziDo`.

## 4. Meni Transport i fajlovi

```
Saobracaj/Saobracaj/
  MainP.Designer.cs / MainP.cs   ← samo nove grupe i dugmad (izmena)
  Saobracaj.csproj               ← samo novi fajlovi (izmena)
  Agenti jobs/                   ← bez izmena (koristi se Db, GridSupport, ExcelRoutes...)
  Sifarnici/, Dokumenta/         ← bez izmena
  Planiranje/
    docs/      PLAN_RAZVOJA_OP.md, PLAN_MIGRACIJE_OP.md, README_plan_2026-11-01.md
    SQL/       OP_001 … OP_005, OP_900
    Podaci/    repozitorijumi (samo šema planiranje za upis)
    Pravila/   pravila bez formi (masa, ruta, ponuda, turnusi, registar)
    Zajednicko/ Latinica, NaziviStanica, pomoćne kontrole (Gantt)
    Forme/     frmOP*.cs + .Designer.cs + .NoviIzgled.cs
    Optimizator/  (faza 8, Python; nije deo .csproj)
```

U `MainP.Designer.cs`, tab `toolStripTabItem3` (**Transport**), dodaj **dve nove grupe** (`Syncfusion.Windows.Forms.Tools.ToolStripEx`) po uzoru na `toolStripEx10`. `Location.X` mora biti desno od `toolStripEx26`, jer `MainP.NoviIzgled.UcitajStavkeIzRibona` ređa grupe po `Left`. `Text` grupe je naziv kartice, a dugmad su `ToolStripButton` sa `Click` u `MainP.cs`. Postojeća dugmad (Planer, DispMain, Iskorišćenje trasa) ostaju.

| Grupa | Dugme | Forma |
|---|---|---|
| **Planiranje** | Nedeljni plan | `frmOPNedeljniPlan` |
| | Operativni plan | `frmOPNedeljniPlan` (operativni) |
| | Najave bez plana | `frmOPNedeljniPlan` (filter) |
| | Dostupnost osoblja | `frmOPDostupnost` |
| | Ponuda osoblja | `frmOPPonudaOsoblja` |
| | Tok posla | `frmOPTokPosla` |
| | Zahtevi za ad hoc trasu | `frmOPTrase` (tab Ad hoc) |
| **Šifarnici planiranja** | Trase: uvoz i pregled | `frmOPTrase` |
| | Registar pruga | `frmOPRegistar` |
| | Mreža i stanice | `frmOPStanice` |
| | Lokomotive i serije | `frmOPLokomotive` |
| | Osoblje i kvalifikacije | `frmOPOsoblje` |
| | Turnusi | `frmOPTurnusi` |
| | Tablica masa | `frmOPTablicaMasa` |
| | Parametri | `frmOPParametri` |

Naslov svake nove forme počinje sa **„Planiranje · “**, da se ne pomeša sa postojećim šifarnicima istog imena.

---

## 5. Faze

### Faza 0: baza
**Urađeno 3.11.2026.** Skripte `OP_001`–`OP_004` pokrenute su na `Perftech_Beograd` ručno, a rezultat je u `docs/PROVERA_REZULTAT.md`. Drugi prolaz skripti na radnoj bazi nije nezavisno proveren; na probnoj bazi `Planiranje_Proba` prolazi dvaput bez novih redova.

Radna baza: SQL Server 2008 R2 Express, **nivo kompatibilnosti 80**, kolacija baze `SQL_Croatian_CP1250_CI_AS`, kolacija servera `Latin1_General_CI_AI`. Zato su skripte izmenjene pre prve upotrebe:
- `TRY_CAST`, `TRY_CONVERT`, `CONCAT`, `DATEFROMPARTS` i `MERGE` su zamenjeni;
- privremene tabele imaju `COLLATE DATABASE_DEFAULT`;
- skripte uključuju `SET ARITHABORT ON` i `QUOTED_IDENTIFIER ON`.

Pravila za dalji rad:
- nivo kompatibilnosti radne baze se ne menja;
- svaka konekcija koja upisuje u `planiranje` mora da uključi `ARITHABORT` i `QUOTED_IDENTIFIER` u **posebnoj komandi** pre upita, zbog filtriranih indeksa (`Db.Open` to radi; važi i za optimizator u fazi 8);
- u novom SQL-u nema funkcija novijih od SQL Servera 2008 R2.

Nalazi u podacima radne baze:
- `OperativniPlanTrasa`: `Departure` i `Arrival` su prazni u svih 910 redova, pa će sve migrirane trase imati grešku „Nedostaju vremena“. Treba utvrditi gde se vremena čuvaju.
- `stanice`: 58 naziva se ponavlja. Veza po nazivu uzima prvu stanicu, pa to treba proveriti u `frmOPStanice`.

1. Na kopiji baze, kao administrator, pokreni redom: `OP_001_Sema`, `OP_002_Punjenje`, `OP_003_Mreza`, `OP_004_Provera`.
2. Proveri poruke `OP_001`. Ako `OP_vOdsustvo` nije povezao godišnje odmore (`dbo.Dopust`, kolona radnika), dopuni pogled novom skriptom.
3. Rezultate `OP_004` upiši u `docs/PROVERA_REZULTAT.md`.

**Prihvatanje:** skripte prolaze dvaput zaredom, a drugi put ne dodaju redove. Postojeće forme rade isto.

### Faza 1: zajednički sloj i meni
**Urađeno 3.11.2026, provereno na probnoj bazi.** Projekat se gradi, grupe su u meniju Transport, grep iz pravila 3.4 je prazan. Test: 26/26.

Otvoreno:
- administrator treba da upiše kodove formi (`frmOP…`) kroz `Administracija/frmForme`, inače forma javlja „Nemate prava“;
- u `PravaRepo` je prava više grupa spojeno (dovoljno je da ih jedna grupa ima), dok postojeće forme uzimaju poslednji red.

- Folder `Planiranje/` i repozitorijumi:
  - `StaniceRepo`, `LokomotiveRepo`, `TraseRepo`, `RegistarRepo`, `ParametriRepo`, `PosaoRepo`, `DogadjajRepo`;
  - `NajaveRepo` (samo `OP_vNajava`), `OsobljeRepo` (`OP_vOsoblje` + upis u `OP_OsobljePlan`), `OdsustvaRepo` (`OP_vOdsustvo` + upis u `OP_Odsustvo`), `TelegramiRepo` (samo čitanje).
- `Parametri.Vrednost(kljuc, datum, stanicaId, stanicaDoId, serijaId, podrazumevano)`.
- `Latinica.IzCirilice()`: Љ→Lj, Њ→Nj, Џ→Dž, Ђ→Đ, Ћ→Ć...; prvo slovo reči veliko.
- `NaziviStanica.Normalizuj` i `Pronadji(naziv, lat?, lon?)` sa ocenom:
  - 1,0 za isti naziv;
  - 0,8 kad jedan naziv sadrži drugi;
  - 0,6 kad je Levenshtein ≤ 2;
  - −0,3 kad je stanica dalje od 5 km.
- Meni Transport: dve nove grupe i prazne forme sa naslovom i proverom prava.

**Prihvatanje:** projekat se gradi, nove stavke se vide u meniju Transport, a stare forme rade isto.

### Faza 2: šifarnici planiranja
**Urađeno 3.11.2026, provereno na probnoj bazi.** Stanice sa mapom, lokomotive, osoblje, turnusi, parametri i tablica masa rade. Test: 27/27.

1. **`frmOPStanice`** („Mreža i stanice“):
   - tabela stanica (izvor STANICE / MREZA / REGISTAR) i mapa (`WebBrowser` sa HTML-om kao `mreza_pruga.html`, uz natpis „© OpenStreetMap contributors“);
   - izmena naziva, vrste, koordinata i aktivnosti;
   - „Spoji duplikate“.
2. **`frmOPLokomotive`**, tabovi Lokomotive / Serije / Zauzetost (servis, namenska, projekat), sa proverom preklapanja.
3. **`frmOPOsoblje`:**
   - spisak iz `OP_vOsoblje` (ime, uloge i aktivnost se ne menjaju, oni su iz Delavci);
   - menja se matična stanica i „u planiranju“ (`OP_OsobljePlan`);
   - kvalifikacije (serija, pruga po broju pruge, važi do);
   - turnus;
   - odsustva (prikaz `OP_vOdsustvo`, unos samo u `OP_Odsustvo`).
4. **`frmOPTurnusi`:** turnusi i „Generiši smene“.
5. **`frmOPTablicaMasa`:**
   - serija × pruga ili relacija × smer × sloj;
   - redosled slojeva je RUCNA → FIZIKA → NAUCENA;
   - uvoz iz Excela.
6. **`frmOPParametri`:** ključ, opseg, vrednost, važi od–do, izvor.
7. **„Dopuni iz starog šifarnika“** u formama 1 i 2 dodaje redove koji su u međuvremenu nastali u `stanice` / `Mesta` (isti upiti kao `OP_002`). Ne menja polja koja je planiranje već uredilo.

### Faza 3: trase (ekran 6)
**Urađeno 3.11.2026, provereno na probnoj bazi.** Uvoz, razlike, objava verzije i ad hoc rade. Test: 19/19.

Odstupanje od 8.11: stvarna datoteka nema vremena ni u jednom od 455 redova. Ako nema vremena u celoj datoteci, to je upozorenje, a ne greška po redu; vremena se tada računaju po pravilu 8.3. Treba potvrditi.

`frmOPTrase` ima tri taba: Uvoz iz Excela, Sve trase, Ad hoc.
1. **Uvoz:** `ExcelRoutes.Read` → nova `OP_TraseVerzija` (NACRT), sa datotekom, `Sha256` i prethodnom verzijom.
2. **Pregled razlika:** ključ je broj voza + početna + krajnja. Stanja su NOVA, IZMENJENA („Polazak 06:40 → 07:00“), UKINUTA i GRESKA (nema vremena, nepoznata stanica sa predlogom, duplikat). Brojači su kao na ekranu.
3. **Potvrda / objava:** nova verzija postaje VAZECA, a stara ARHIVA. **Ne piše u `OperativniPlanTrasa`**: Planer i Dispečer rade sa svojim katalogom kao i do sada.
4. **Ad hoc trasa** se unosi ručno. **Zahtev za ad hoc** dolazi iz rute posla (`AdHocStatus = 'ZAHTEV'`); kad trasa stigne, unosi se i postaje `ODOBRENA`.

**Prihvatanje:** dva uzastopna uvoza daju tačan pregled razlika, a Planer i Dispečer rade isto.

### Faza 4: Registar pruga (ekran 7)
**Urađeno 3.11.2026, provereno na stvarnoj datoteci i probnoj bazi.** Pruga 1 je tačna od Beograda do granice, po nazivu je povezano 539 mesta. Test: 27/27.

Odluke koje treba potvrditi:
- `ПРУГА ВАН ЕКСПЛОАТАЦИЈЕ` se tretira kao `ПРУГА ЗАТВОРЕНА ЗА САОБРАЋАЈ` (`Aktivno = 0`); zatvoren deo prekida graf;
- mesto bez stanice dobija novu stanicu pri potvrdi (`Izvor = REGISTAR`), jer bi ručno povezivanje oko 350 mesta bilo neizvodljivo;
- svaka državna granica je posebna stanica, inače bi jedan zajednički čvor spojio nepovezane pruge;
- redni brojevi pruga kreću iznova u svakoj kategoriji, pa je ključ kategorija + broj;
- kad se stacionaža ne slaže, koristi se kolona D; bez nje veza preko 50 km se ne pravi i ide u panel „Problemi pri čišćenju“.

`frmOPRegistar` ima tabove Uvoz, Pregled pruga, Istorija verzija.

**Struktura `Registar.xlsx`** (`Sheet1`, 1.040 redova; brojevi kolona iz reda 4):
- Redovi 2–4 su zaglavlje.
- Red sa jednom vrednošću u B koji počinje sa `МАГИСТРАЛНЕ` / `РЕГИОНАЛНЕ` / `ЛОКАЛНЕ` / `МАНИПУЛАТИВНЕ` je kategorija. Redni broj pruge se ponavlja po kategoriji.
- Red sa jednom vrednošću u B oblika `N. Naziv` je pruga.
- Red sa nazivom u F je službeno mesto. `КРАЈ ПРУГЕ` i oznake km (`87+000`) dobijaju `VrstaMesta = 'oznaka'`.
- Od reda `*) Пруге на територији Косова...` počinje legenda, koja se ne uvozi. Šifre kolona 6 (vrsta mesta 1–16) i 17 (način osiguranja 1–11) idu u `Pravila/RegistarSifre.cs`.

| Kol. | Excel | `OP_PrugaMesto` |
|---|---|---|
| 1, 2 | B, C | datum predaje (ne čuva se) |
| 3 | D | km udaljenost; računa se iz E |
| 4 | E | `StacionazaM`, `StacionazaKM` = E/1000 |
| 5 | F | `NazivOriginal`, `Naziv` (latinica) |
| 6 | G | `VrstaMesta` |
| 7 | H | `Kolosecnost` (Ј=1, Д=2) |
| 8 | I | `Kategorija` (u podacima: М, ман…) |
| 9 | J | `Klasa` (u podacima: D4, A, A'…) |
| 10, 11 | K, L | `VmaxDesni`, `VmaxLevi` |
| 12, 13 | M, N | `MaxDuzinaVozaAB`, `KoloseciAB` |
| 14, 15 | O, P | `MaxDuzinaVozaBA`, `KoloseciBA` |
| 16, 17 | Q, R | `NacinRegulisanja`, `NacinOsiguranja` |
| 18 | S | `UIC` |
| 19–22 | T–W | `Vaga`, `Rampa`, `Posednutost`, `OtvorenoZa` |
| 23, 24 | X, Y | `MinPoluprecnikM`, `NagibStanice` |
| 25, 26 | Z, AA | `MerodavniUspon`, `MerodavniPad` |
| 27, 28 | AB, AC | `MerodavniOtporUspon`, `MerodavniOtporPad` |
| 29, 30 | AD, AE | `TovarniProfil`, `NadmorskaVisina` |

Spojene ćelije (`<mergeCells>`) važe za ceo opseg. Tekst `ПРУГА ЗАТВОРЕНА ЗА САОБРАЋАЈ` znači `Aktivno = 0`. Datoteka se čita kroz `System.IO.Compression` + `System.Xml.Linq`.

Koraci uvoza:
1. Učitavanje u verziju NACRT i čišćenje (latinica; original se čuva).
2. **Mesta → `OP_Stanice` po nazivu:**
   - isti normalizovan naziv; kod više kandidata odlučuje najbliža koordinata;
   - zatim UIC, ako postoji kod oba;
   - ostatak se potvrđuje ručno, ili „Dodaj kao novu stanicu“ (`Izvor = 'REGISTAR'`);
   - u `PovezanoPo` se upisuje kako je mesto povezano.
3. **Pruga → `dbo.Pruga` po nazivu** (`OP_Pruga.IzvorniID`), zbog telegrama i kvalifikacija. Identifikacioni broj iz knjižica se unosi ručno.
4. **Razlike prema važećoj verziji.**
5. **Potvrda:** verzija postaje VAZECA, a `OP_MrezaVeza` (Registar) se osvežava. Km iz Registra ima prednost nad OSM.

**Prihvatanje:** pruga 1 je tačna od Beograda do granice, a po nazivu je povezano ≥ 525 mesta.

### Faza 5: planiranje lokomotiva (ekrani 1, 2)
**Urađeno 3.11.2026, provereno na probnoj bazi.** Najava od 2.150 t na seriji 185 daje dvostruku vuču, posao bez trase dobija NEMAMO_TRASU, preklapanje se ne može potvrditi. Test: 47/47.

Otvoreno:
- GPS nije povezan: pozicije dolaze samo iz spoljnog servisa, pa je lokacija poslednji posao, zatim matična stanica; upozorenje o starosti GPS-a se zato ne pojavljuje;
- osovinsko opterećenje računa 4 osovine po kolima i UIC granice po prvom slovu klase (A 16, B 18, C 20, D 22,5, E 25 t), što treba potvrditi;
- deonica bez podatka o elektrifikaciji ne blokira električnu lokomotivu.

**5.1 Najave → poslovi.** Za period po `OP_vNajava.PredvidjenoPrimanje` pravi se `OP_Posao`:
- prijem = `PlanOtpravnaID`, predaja = `PlanUputnaID` (preko `OP_Stanice.IzvorniID`);
- `PlaniraniPrijem = PredvidjenoPrimanje`;
- događaj NAJAVA.

**5.2 Ruta:** Dijkstra po `OP_MrezaVeza` daje deonice u `OP_PosaoRuta`. Stanje deonice je:
- IMA_TRASU;
- NEMAMO_TRASU, uz „Zatraži ad hoc trasu“, posle čega deonica dobija ZAHTEV_ADHOC;
- ZATVOR, iz `OP_vTelegram` (`VaziOd–VaziDo`, dnevno `TrajeOd–TrajeDo`): ruta se traži ponovo bez te deonice, a ako je nema, nastaje konflikt.

UPOZORENJE iz telegrama samo ide u listu.

**5.3 Vremena** se uzimaju iz trase. Kad trase nema:
- polazak = prijem + `OBRADA_PRIJEM_MIN`;
- dolazak = polazak + `VOZNJA_MIN`, ili km / brzina;
- predaja = dolazak + `OBRADA_PREDAJA_MIN`.

Ako je plan kasniji od `PredvidjenaPredaja`, to je upozorenje.

**5.4 Masa:**
- Bruto se uzima iz `OP_vTeretnicaNajava` ako postoji, inače iz `Najava.Tezina`.
- MaxMasa se traži po sloju i opsegu, a za rutu važi najmanja vrednost po deonicama.
- Broj lokomotiva je 1 ili 2.
- `MASA_LOK_U_VUCI` je parametar.

**5.5 Provere (7):** vuča, masa, dužina voza, osovinsko opterećenje, zauzetost, trasa, vreme obrade.

**5.6 Ponuda lokomotiva:**
- Lokacija je GPS, ili predaja poslednjeg posla, ili matična stanica.
- Prazna vožnja se računa po mreži.
- Isključuju se: zauzeta, na servisu, namenska za drugi projekat, ne stiže na vreme, pogrešna vrsta vuče.
- Ocena = prazna vožnja + `TEZINA_RAVNOMERNOST` × (poslovi − prosek) + `TEZINA_SERVIS`.
- Prikazuju se prve tri.

**5.7 Potvrda:** upis u `OP_PosaoLokomotiva`, faza 3 i događaj. Sve je u jednoj transakciji, sa ponovnom proverom i `sp_getapplock` po lokomotivi.

**5.8 Nedeljni / operativni plan:**
- brojači;
- Gantt na `Panel.OnPaint`, sa prevlačenjem;
- konflikti i upozorenja;
- poslovi bez lokomotive;
- „Sačuvaj verziju“;
- „Predloži plan“: do faze 8 jednostavan C# algoritam, posle toga optimizator.

### Faza 6: osoblje (ekrani 3, 4)
**Urađeno 3.11.2026, provereno na probnoj bazi.** Smene iz turnusa su tačne, bolovanje isključuje osobu, odbijanje nudi sledećeg. Test faza 6 i 7: 51/51.

Otvoreno: mail se šalje samo ako su u konfiguraciji podešeni `system.net/mailSettings`. Bez toga se zadatak upisuje, a poruka kaže „Javiti telefonom“. Šifra iz postojećih formi nije preuzeta. Stvarno slanje maila nije probano.

- **6.1 Smene iz turnusa:** dan je radni ako je `(d − Od) mod (DanaRada + DanaOdmora) < DanaRada`. Ručne smene se ne prepisuju.
- **6.2 Dostupnost:**
  - kalendar iz `OP_vOsobljeDan` (smena, odsustvo iz Bolovanje / Dopust / OP_Odsustvo, zadaci);
  - filter Svi / Mašinovođe / Pregledači;
  - panel izabrane osobe.
- **6.3 Ponuda osoblja:**
  - **Mašinovođa** iz `OP_vOsoblje` mora:
    - imati ulogu i biti aktivan i u planiranju;
    - imati kvalifikaciju za seriju svake lokomotive i za svaku prugu na ruti;
    - imati smenu koja pokriva posao, sa trajanjem ≤ `MAX_RAD_H`;
    - nemati odsustvo ni preklapanje;
    - imati odmor `ODMOR_POSLE_SMENE_H`.
  - **Pregledač** na prijemu i na predaji: matična stanica i smena.
  - **Slanje zadatka:** `OP_Zadatak` (POSLAT) i mail na `Delavci.DeEMail`, kroz postojeći `MailRepository` (koristi se, ne menja se).
  - **Odgovor** upisuje dispečer. Posle odbijanja sistem nudi sledećeg kandidata, a kad svi prihvate, posao prelazi u fazu 4.

### Faza 7: tok posla (ekran 5)
**Urađeno 3.11.2026, provereno na probnoj bazi.** Jedna najava prolazi faze 1–7, a faze 5 i 7 dolaze iz Dispečera bez ručnog unosa. Faza 2 počinje kad se izračuna ruta.

Otvoreno:
- tekst „faza 4 → 5 ako Dispečer još nije pokrenuo voz; inače čeka“ urađen je doslovno. Ako je voz već pokrenut, dugme samo javlja da faza dolazi iz Dispečera;
- tačka 4 ovde (događaj TERETNICA i konflikt kad se promeni broj lokomotiva) nije urađena; specifikacija je ne traži, a masa se računa iz Teretnice pri svakom prikazu.

1. **`frmOPTokPosla`:** koraci 1–7, događaji, sledeći koraci, vreme po parametrima.
2. Ručne potvrde faza 4 i 6.
3. Faze 5 i 7 se **čitaju** iz `OperativniPlanVozovi` (`AnnouncementFullId = NajavaID`, `Started`, `Completed`), ili iz `Najava.StvarnoPrimanje` / `StvarnaPredaja`. Upisuje se `OP_Posao.OperativniVozID`.
4. Kad stigne Teretnica, masa se računa ponovo i upisuje se događaj TERETNICA. Ako se promeni broj lokomotiva, to je konflikt.
5. Upozorenje ako mašinovođa nije prihvatio zadatak `REZERVA_PRE_POLASKA_MIN` pre polaska.

### Faza 8: optimizator (README)
**Urađeno 3.11.2026, provereno na probnoj bazi; servis nije postavljen na server.** Nacrt stiže u roku, bez preklapanja i kršenja kvalifikacija. Testovi: model 12/12, ceo tok kroz servis 14/14. Upis mašinovođe kroz servis u bazu pokriven je samo testom modela. Vidi `Optimizator/README.md`.

Nije urađeno: naučeni sloj tablice masa, jer Teretnica nema utvrđenu vezu sa serijom i prugom.

- **Smeštaj:** `Planiranje/Optimizator/` (Python 3.11, FastAPI, OR-Tools CP-SAT), servis na istom serveru, nalog `kp_optimizator` (`OP_005_Prava.sql`, `dbo` samo za čitanje).
- **Ulaz:** `POST /plan/nedeljni`, `POST /plan/operativni`. Servis čita šemu `planiranje` i upisuje nacrt kao `OP_PlanVerzija` + `OP_PlanVerzijaStavka`.
- **Model:**
  - promenljive: dodela 1–2 lokomotive i mašinovođe poslu;
  - ograničenja: nepreklapanje sa praznom vožnjom, zauzetost, vrsta vuče, kvalifikacije, smene i odmor;
  - cilj: najmanja prazna vožnja, nedodeljeni poslovi i neravnomernost;
  - vreme rešavanja je ograničeno na 30 s.
- „Predloži plan“ poziva servis, a potvrda kopira nacrt u `OP_PosaoLokomotiva` / `OP_Zadatak`.
- Naučeni sloj tablice masa: noćni posao pravi najveće ostvarene mase po seriji i pruzi iz Teretnica.

### Faza 9: integracija (posle probnog rada, uz odobrenje svake stavke)
**Nije započeto.** Čeka probni rad od bar mesec dana i odobrenje svake stavke.

1. Objava trasa iz `frmOPTrase` upisuje i u `OperativniPlanTraseDokument` / `OperativniPlanTrasa`, da Dispečer koristi iste trase.
2. „Prebaci u spisak vozova“ iz Toka posla koristi logiku `OperationsRepository.AddTrain` / `LinkNajava`.
3. Konačna migracija šifarnika prema `PLAN_MIGRACIJE_OP.md`, deo 3.

---

## 6. Otvorena pitanja
1. Masa lokomotive: računa se samo u ukupnu masu ili i u vuču (`MASA_LOK_U_VUCI`)?
2. Prve relacije i serije za ručnu tablicu masa; relacije za vreme iz knjižica.
3. Primer Excela trasa.
4. Vrednosti kolone `Kolosek` u telegramima: lista je u `PROVERA_REZULTAT.md`; treba odlučiti da li `zatvor pruge`, `Zatvor`, `svi` i `levi i desni` zatvaraju deonicu.
5. OSM mapa: uvoz jednom ili stalno osvežavanje (ODbL: navesti „© OpenStreetMap contributors“).
6. Jedinica mase u `TeretnicaStavke`: **odgovoreno**, tone; redovi preko 200 t po kolima dele se sa 1.000.
7. Kolona radnika u `dbo.Dopust`: **odgovoreno**, pogled je povezan (3.874 reda).
8. `StatusGodmora`: **odgovoreno**, kolona ne postoji u `dbo.Dopust`, pa se računaju svi redovi.

## 7. Dorada KP-26-11-03-Osoblje (3.11.2026)

Kvalifikacija po seriji zamenjena je poznavanjem konkretne lokomotive (`OP_OsobljeLokomotiva`), a pruge su u `OP_OsobljePruga`. `OP_OsobljeKvalifikacija` i stare tabele `dbo.DelavciPrugaMasin` i `dbo.DelavciLokMasin` planiranje više ne koristi; podaci se prenose jednom skriptom `OP_008`. Uloge se upisuju u `dbo.Delavci`, što je jedini izuzetak od pravila 3.4 specifikacije. Redosled na bazi: `OP_007`, zatim `OP_008`.

## 8. Dorada KP-26-11-03-Mreza i stanice (3.11.2026)

Urađeno u kodu, provereno na probnoj bazi i na lokalnoj kopiji podataka radne baze. Pravila su u odeljku 11 specifikacije, a rezultat u `PROVERA_REZULTAT.md`. Skripte `OP_009` i `OP_010` na radnoj bazi pokreće korisnik. Dok se ne pokrenu, program ne radi, jer `StaniceRepo` i `PlanRepo` čitaju nove kolone.
