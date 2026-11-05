# Plan migracije podataka: Planiranje (šema `planiranje`)

Verzija 3, 3.11.2026. Planiranje se otvara iz menija **Transport** u programu Saobracaj.

**Pravilo.** Postojeće forme i tabele ostaju kakve jesu. Planiranje ima svoje šifarnike (`OP_*`), koji se jednom napune iz postojećih tabela. Tabela koju planiranje samo čita koristi se direktno, bez kopije.

| Etapa | Kada | Smer |
|---|---|---|
| **1. Prvo punjenje** | sada (faza 0) | `dbo` (samo čitanje) → `planiranje` |
| **2. Probni rad** | dok traje razvoj | ko je vlasnik kog podatka |
| **3. Konačna migracija** | pre migracije programa | `planiranje` → glavne tabele |

---

## 1. Prvo punjenje

### 1.1 Redosled (administrator, prvo na kopiji baze, van radnog vremena)
1. Rezervna kopija baze (`BACKUP DATABASE ... COPY_ONLY`).
2. `OP_001_Sema.sql`: šema `planiranje`, 26 tabela i 8 pogleda.
3. `OP_002_Punjenje.sql`: šifarnici iz `dbo`.
4. `OP_003_Mreza.sql`: koordinate i veze iz `mreza_stanice.json` (538 mesta, 739 veza; © OpenStreetMap contributors, ODbL).
5. `OP_004_Provera.sql`: spisak onoga što treba ispraviti.
6. Ponovo 2–4: drugi prolaz ne sme da doda redove.

`OP_005_Prava.sql` je potreban tek u fazi 8 (nalog za servis Optimizator).

Registar pruga se uvozi kroz formu (faza 4), ne skriptom.

### 1.2 Šifarnici koji se kopiraju (planiranje ih posle menja)

| `planiranje.` | Izvor `dbo` | Pravilo | Veza nazad |
|---|---|---|---|
| `OP_Stanice` | `stanice` | `Naziv = RTRIM(Opis)`, `Kod`, `Drzava`, `Granicna`, koordinate. Petocifreni `Kod` ide u `UIC` (proveriti). | `IzvorniID` |
| `OP_Stanice` (dopuna) | `mreza_stanice.json` | po nazivu; dopunjuju se samo prazna polja; nova mesta dobijaju `Izvor = 'MREZA'` | |
| `OP_Stanice` (dopuna) | `Registar.xlsx` (forma) | po nazivu → koordinata → UIC → ručno; nova mesta dobijaju `Izvor = 'REGISTAR'` | `OP_PrugaMesto.PovezanoPo` |
| `OP_SerijeLokomotiva` | `LokomotivaSerija` | `Oznaka`, `Opis`, masa, snaga | `IzvorniID` |
| `OP_Lokomotive` | `Mesta WHERE Lokomotiva = 1` | broj, masa (`Masa` / `SmPogDela`), serija po oznaci u broju, vrsta vuče po `Dizel`, `TechnodisID` iz `LocoTechnodis` | `IzvorniSmSifra` |
| `OP_Pruga` (zaglavlja) | `Pruga` | oznaka, naziv, elektrificirana; mesta se ne kopiraju | `IzvorniID` (za `Telegrami.PrugaID`) |
| `OP_PrugaMesto` | **ništa** (`PrugaStavke` se ne koristi) | samo iz uvoza Registra | |
| `OP_TraseVerzija` / `OP_Trase` | `OperativniPlanTraseDokument` / `OperativniPlanTrasa` | dokument = verzija; aktivni dokument je VAZECA; stanice se vezuju po nazivu | `IzvorniDokumentID`, `IzvorniRouteID`, `IzvornaTrasaID` |
| `OP_OsobljePlan` | `Delavci` (aktivni sa ulogom) | samo `DeSifra` + predlog matične stanice iz `DeKrajBivS` | `DeSifra` |
| `OP_OsobljeKvalifikacija` | `DelavciPrugaMasin`, `DelavciLokMasin` | pruga → `OP_Pruga`; lokomotiva → njena serija | `DeSifra` |
| `OP_Parametri`, `OP_Turnus` | – | početne vrednosti | |

### 1.3 Tabele koje se samo čitaju (bez kopije)

| Tabela `dbo` | Pogled / upotreba |
|---|---|
| `Najava` | `OP_vNajava`: **`PredvidjenoPrimanje`** (predviđeno vreme prijema), `PredvidjenaPredaja`, `StvarnoPrimanje`, `StvarnaPredaja`, `Otpravna/Uputna`, `OtpravnaGr/Uputnagr`, `Tezina`, `Duzina`, `BrojKola`, `RID` |
| `Delavci` | `OP_vOsoblje`: ime, telefon, e-mail, uloge, aktivnost (+ `OP_OsobljePlan`) |
| `Bolovanje`, `Dopust` / `DopustStavke` | `OP_vOdsustvo` (+ ručni unos u `OP_Odsustvo`) |
| `Telegrami` | `OP_vTelegram`: zatvor (Kolosek „pruga“) ili upozorenje |
| `TeretnicaStavke` | `OP_vTeretnicaNajava`: Tara + Neto po najavi |
| `OperativniPlanVozovi` | faze 5 i 7 (`AnnouncementFullId`, `Started`, `Completed`) |
| `LocoTechnodis`, `Partnerji`, `stanice` | GPS, nazivi |

### 1.4 Pretpostavke za proveru

| # | Pretpostavka | Provera | Ako nije tačno |
|---|---|---|---|
| P1 | `stanice.Kod` sa 5 cifara je UIC | `OP_004` tačka 10 | `UPDATE planiranje.OP_Stanice SET UIC = NULL WHERE Izvor='STANICE'` |
| P2 | `Mesta.SmPogDela` je masa lokomotive | forma Lokomotive | `UPDATE planiranje.OP_Lokomotive SET MasaT = NULL` |
| P3 | oznaka serije se nalazi u broju lokomotive | `OP_004` tačka 2 | ručno |
| P4 | `DelavciLokMasin` sadrži lokomotivu ili seriju | `SELECT TOP 5 *` | ručno |
| P5 | `OperativniPlanTrasa.From/To` = `stanice.Opis` | `OP_004` tačka 5 | povezivanje u formi Trase |
| P6 | mesto stanovanja je dobar predlog matične stanice | `OP_004` tačka 13 | ručno |
| P7 | kolone `Telegrami` (`TrajeOd/TrajeDo`, `BrojTelegrama`) | poruke `OP_001` | dopuniti pogled |
| P8 | jedinica `Tara/Neto` (t ili kg) | forma Teretnica | podeliti sa 1000 u pogledu |
| P9 | `Dopust`: kolona radnika i ključ (`ID` / `DoStZapisa`) | poruke `OP_001` | dopuniti `OP_vOdsustvo` |
| P10 | `Bolovanje.ZaposleniID` = `Delavci.DeSifra` | forma Bolovanja | dopuniti pogled |

---

## 2. Probni rad

### 2.1 Ko je vlasnik podatka

| Podatak | Vlasnik | Napomena |
|---|---|---|
| Postojeće stanice, lokomotive, serije, pruge, trase | stari program | stare forme rade kao i do sada |
| Šifarnici planiranja (`OP_Stanice`, `OP_Lokomotive`, `OP_SerijeLokomotiva`, `OP_Pruga`, `OP_Trase`) | **planiranje** | menjaju se samo u novim formama |
| Nove stanice/lokomotive nastale u starom programu | stari program | dolaze u planiranje dugmetom „Dopuni iz starog šifarnika“ |
| Osoblje (ime, uloge, aktivnost) | stari program (`Delavci`) | samo čitanje |
| Matična stanica, kvalifikacije, turnusi, smene, ručna odsustva, zadaci | **planiranje** | |
| Bolovanja i godišnji | stari program | samo čitanje |
| Najava, Teretnica, Telegrami, vozovi Dispečera | stari program | samo čitanje |
| `PrugaStavke` | stari program (menja se sada) | ne koristi se |

### 2.2 Zaštita operativnog rada
- Nove forme pišu samo u šemu `planiranje` (pravilo 5 u planu razvoja, provera u pregledu koda).
- Nema stranih ključeva prema `dbo`, pa brisanje u starom programu nikad ne pada zbog planiranja.
- Postojeći fajlovi koda se ne menjaju, osim dodavanja dugmadi u `MainP` i fajlova u `Saobracaj.csproj`.

### 2.3 Povratak
- Sakriti nove grupe u meniju Transport (`Available = false`).
- Po potrebi pokrenuti `OP_900_Ukloni.sql`, koji briše šemu `planiranje` (pre toga rezervna kopija).

---

## 3. Konačna migracija

| Entitet | Predlog |
|---|---|
| Stanice | `OP_Stanice` postaje glavna. U `stanice` se dodaju `UIC`, `NazivCir`, `VrstaMesta`, a nove stanice se ubacuju. Mapa ključeva: `planiranje.OP_MapaStanica`. |
| Serije, lokomotive | `OP_SerijeLokomotiva` zamenjuje `LokomotivaSerija`; tabela lokomotiva nastaje iz `OP_Lokomotive` (`Mesta` ostaje za ostala vozila) |
| Osoblje | ostaje `Delavci`; `OP_OsobljePlan` ide u kolone `Delavci`; kvalifikacije zamenjuju `DelavciLokMasin` / `DelavciPrugaMasin` |
| Pruge | `OP_Pruga` / `OP_PrugaMesto` zamenjuju `Pruga` / `PrugaStavke` (stare kao pogled dok se ne prepišu `frmPruge`, `frmTrasaStanice`, `frmTelegrami`) |
| Trase | `OP_TraseVerzija` / `OP_Trase` postaju glavne; `OperativniPlanTrasa` postaje pogled; `Trase` (obračun) dobija `TrasaID` |
| Posao, plan, zadaci, događaji | ostaju u šemi `planiranje` |

**Koraci:**
1. Zamrzavanje.
2. Čist `OP_004`.
3. Mape ključeva.
4. Prenos po entitetu, uz dnevnik.
5. Prepis ključeva.
6. Pogledi kompatibilnosti.
7. Repozitorijumi menjaju imena tabela.
8. Brisanje `Izvorni*`.
9. Probni posao kroz svih 7 faza.

**Uslovi za početak:** bar mesec dana probnog rada, potvrđen Registar, objavljene trase važeće godine i odgovori na P1–P10.
