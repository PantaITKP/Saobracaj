# Rezultat provere podataka (OP_004)

Stanje `Perftech_Beograd` na dan 3.11.2026. Baza je samo čitana. Skripte `OP_001`–`OP_004` pokrenute su ručno 3.11.2026.

## Koliko je podataka preneto

| Tabela | Redova |
|---|---|
| `OP_Stanice` | 1.571 |
| `OP_Lokomotive` | 39 |
| `OP_SerijeLokomotiva` | 7 |
| `OP_Pruga` | 98 (samo zaglavlja iz `dbo.Pruga`) |
| `OP_MrezaVeza` | 739 |
| `OP_Trase` | 910 |
| `OP_Parametri` | 11 |
| `OP_OsobljePlan` | 2 |
| `OP_OsobljeKvalifikacija` | 1 |

Pogledi rade: `OP_vNajava` 18.603, `OP_vOdsustvo` 4.410, `OP_vOsoblje` 483, `OP_vTelegram` 8.931, `OP_vTeretnicaNajava` 16.386 redova.

## Šta treba dopuniti pre rada

| Nalaz | Broj | Posledica | Ko rešava |
|---|---|---|---|
| Lokomotive bez serije | 10 | Ne ulaze u ponudu | `frmOPLokomotive` |
| Trase sa greškom | 455 | Nemaju obe stanice ili vremena; bez stanica ne pokrivaju rutu | `frmOPTrase` |
| Nepoznati nazivi stanica u trasama | 96 | Trasa ne može da se veže za rutu | `frmOPTrase` |
| Mašinovođe bez kvalifikacije | 2 | Nisu u ponudi osoblja | `frmOPOsoblje` |
| Osobe bez matične lokacije | 2 | Računaju se kao da nisu na lokaciji | `frmOPOsoblje` |
| Stanice bez veze u mreži | 13 | Ruta do njih ne postoji | `frmOPStanice` |
| Dupli nazivi u `dbo.stanice` | 58 | Povezivanje po nazivu uzima prvu stanicu | `frmOPStanice` |
| Osobe u planiranju | samo 2 | Većina poslova će imati konflikt „nema mašinovođe“ | `frmOPOsoblje` |

## Odgovori iz podataka na otvorena pitanja

- **Jedinica mase u `TeretnicaStavke`:** tone (prosek 50,6 po kolima na 16.321 najavi). Pojedini redovi su očigledno u kilogramima. Red sa više od 200 t po kolima deli se sa 1.000.
- **`Bolovanje.ZaposleniID` = `Delavci.DeSifra`:** da, svih 540 redova se povezuje.
- **`dbo.Dopust`:** pogled je povezan (3.874 reda godišnjeg). Kolona `StatusGodmora` ne postoji, pa se računaju svi redovi.
- **`AnnouncementFullId` u `OperativniPlanVozovi`:** čist broj najave, bez zareza. Od 8 redova, 5 je prazno, pa je veza sa Dispečerom za sada malo isprobana na stvarnim podacima.
- **Elektrifikacija:** nijedna od 739 veza nema podatak, a svih 98 pruga ima. Provera vuče zato čita prugu kad veza nema podatak. Veza bez pruge ostaje nepoznata i ne blokira električnu lokomotivu.

## Vrednosti `Kolosek` u telegramima (otvoreno)

| Vrednost | Redova | Trenutno se tretira kao |
|---|---|---|
| (prazno) | 2.733 | upozorenje |
| `pruga` | 2.721 | zatvor |
| `levi` | 724 | upozorenje |
| `desni` | 649 | upozorenje |
| `svi` | 204 | upozorenje |
| `zatvor pruge` | 135 | upozorenje |
| `Zatvor` | 117 | upozorenje |
| `levi i desni` | 117 | upozorenje |
| `deo pruge` | 95 | upozorenje |

Pravilo iz specifikacije traži reč „pruga“, pa `zatvor pruge`, `Zatvor`, `svi` i `levi i desni` ne zatvaraju deonicu. Potrebna je odluka; izmena je u pogledu `OP_vTelegram`.

## Registar u bazi

`Registar.xlsx` je uvezen u radnu bazu 3.11.2026 u 10:48 kroz `frmOPRegistar` (91 pruga, 919 mesta).

Provera datoteke na probnoj bazi: 1.014 redova, 91 pruga, 919 mesta, 539 mesta povezano po nazivu. Zatvoreno ili van eksploatacije je 72 mesta. Devet rastojanja je sumnjivo jer stacionaža kreće iznova; ona su u panelu „Problemi pri čišćenju“.

## Mreža i stanice (OP_009, OP_010), 3.11.2026

Provereno na `Planiranje_Proba` i na `Planiranje_RadnaKopija` (lokalna kopija `planiranje` tabela radne baze). Radna baza je samo čitana. Test: 67/67 na obe baze, a pravi ekran `frmOPStanice` sa mapom 16/16. Ponovljen `OP_010` ne menja ništa novo. Ponovljen uvoz iste datoteke ne dodaje stanice ni veze i ne menja nazive ni koordinate.

**Stanje radne baze pre skripti:** `stanice_mreza_SRB.sql` je puštena 3.11.2026 u 17:34. Nove kolone i 753 mesta su tu, ali planer i dalje računa po staroj `OP_MrezaVeza`, a 79 njenih veza ide na ugašene stanice. Još 28 duplikata ima `IzvorniID`, pa 154 najave vode na stanicu van nove mreže.

| Nalaz u isporučenim podacima | Šta je urađeno |
|---|---|
| 5 deonica dodatih iz naziva pruge je kraće od prave linije (`M7 Beograd Centar – Resnik` 0,425 km, stvarno oko 8,5 km) | Dužina se računa iz koordinata, kao u `final.py` za ostale deonice |
| Primeri iz odeljka 7.5 ne važe za 20 t: `Novi Sad – Bogojevo` 75,9 km ide preko pruge klase A (16 t), a `Kikinda – Subotica` i `Niš – Prahovo` nemaju rutu za 20 t | Pravilo 7.2 se primenjuje kako piše; ekran ima izbor „Bez provere“ |
| `Šid – Dimitrovgrad` je u zadatku 443,1 km zbog iste prečice | Sada 451,8 km |
| Šifra Rasputnice B (16201) spojila ju je sa Beograd ranžirnom A | `OP_010` vraća red na pravo mesto |
| Kodovi vrste 9–14 i 16 su „bez legende“, a legenda je u `Registar.xlsx` | Koriste se nazivi iz legende (`RegistarSifre`) |

| Primer (bez osovinskog) | U zadatku | Sada |
|---|---|---|
| Šid – Stara Pazova | 81,4 | 81,4 |
| Šid – Niš | 356,5 | 354,7 |
| Šid – Dimitrovgrad | 443,1 | 451,8 |
| Subotica – Niš ranžirna | 410,6 | 408,8 |
| Niš – Prahovo pristanište | 186,2 | 186,2 |
| Novi Sad – Bogojevo | 75,9 | 75,9 |
| Kikinda – Subotica | 102,1 | 99,7 |

**Za ručnu proveru:**
- 161 aktivan red bez države ili sa SRB nije u Registru; deo su strane stanice bez upisane države. `OP_010` ih ispisuje.
- 20 deonica nema OSM trasu, pa se na mapi crtaju kao prava linija. Deonica `Sombor – Rasputnica Strilić` ima privremenu dužinu od 1 km.
- 7 mesta iz Registra nema koordinate.
- Strane stanice sa zamenjenim koordinatama nisu menjane, po zadatku.
- Vrednosti `Kolosek` iz gornje tabele i dalje čekaju odluku: `zatvor pruge` i `Zatvor` ne zatvaraju deonicu.
