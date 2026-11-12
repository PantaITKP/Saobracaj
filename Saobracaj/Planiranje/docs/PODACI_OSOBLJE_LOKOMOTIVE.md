# Planiranje: šta je urađeno i koje podatke treba popuniti

**Stanje:** 8.11.2026 · **Detalji:** `SPECIFIKACIJA_PLANIRANJE.md` (brojevi odeljaka u zagradama)

Planiranje radi samo onoliko dobro koliko su popunjeni šifarnici. Ako osoblje i lokomotive nisu uneti, sistem neće imati koga da ponudi. Ovaj dokument kaže šta tačno treba uneti i kojim redom.

---

## 1. Sažetak urađenog

Planiranje je novi deo menija **Transport**. Vodi posao od najave do predaje po principu „Ponudi mi“: sistem nudi kandidate, a čovek potvrđuje.

| Oblast | Ekran | Šta radi |
|---|---|---|
| Plan lokomotiva | Nedeljni plan, Posao | Pravi poslove iz najava, računa rutu, masu i broj lokomotiva i predlaže lokomotive (8.1–8.7) |
| JOBS i taskovi | JOBS, Taskovi | Planiranje po pravcu umesto najave, split i spajanje taskova, gantogrami lokomotiva i osoblja (22, 26, 29, 30) |
| Osoblje | Osoblje i kvalifikacije, Dostupnost, Ponuda osoblja | Uloge, matična stanica, AUTO, poznavanje serija i pruga, turnusi, smene, odsustva; ponuda mašinovođa, pregledača i vozača (12, 23, 25, 28) |
| Lokomotive | Lokomotive | Lokomotive, serije, zauzetost (servis, namenska), aktivne lokomotive (21) |
| Mreža | Mreža i stanice, Registar | Stanice, pruge iz Registra, elektrifikacija, mapa sa GPS-om, zatvorima iz telegrama i vagonima (11, 19, 20, 27) |
| Trase | Trase | Uvoz iz Excela, razlike verzija, ad hoc trase |
| Ostalo | Tablica masa, Parametri, Turnusi | Granice mase po seriji i pruzi, vremena obrade, turnusi 07–19 i 19–07 (13) |

Na radnoj bazi skripte pokreće administrator baze, redom od `OP_001` do `OP_033`. Program ne radi bez skripti `OP_007`, `OP_009`, `OP_011`, `OP_014`, `OP_017`, `OP_022`–`OP_025`, `OP_027`, `OP_029`, `OP_032` i `OP_033`.

---

## 2. Osoblje

Ekran: **Transport → Osoblje i kvalifikacije**. Ime, telefon, e-mail i aktivnost dolaze iz kadrovske evidencije (`Delavci`) i ovde se ne menjaju.

### 2.1 Obavezno za svaku osobu koja ulazi u plan

| Podatak | Gde se unosi | Zašto je potreban | Šta se desi ako fali |
|---|---|---|---|
| **Aktivan** | kadrovska evidencija | Neaktivni se ne nude | Osoba se ne pojavljuje u ponudi |
| **Uloga** (Mašinovođa, Pregledač kola, Manevrista, Vozovođa, Mašinovođa manevre) | kolone uloga na tabu Osobe | Određuje za koje radno mesto se osoba nudi | Osoba se ne nudi ni za jedno mesto |
| **Uključi u plan** | čekiranje na tabu Osobe | Samo te osobe vide Dostupnost i Ponuda | Osoba je nevidljiva za planiranje |
| **Matična stanica** | tab Osobe (samo aktivne stanice u Srbiji) | Polazna lokacija kad osoba nema prethodni zadatak | Ocena „Lokacija nije poznata“; osoba se ne može predložiti |
| **Turnus** sa datumom „Od“ | tab Turnus | Iz turnusa se generišu smene | Bez smena osoba je „Nedostupan – Nema smenu u terminu“ |
| **Smene** za period plana | Dostupnost → „Generiši smene iz turnusa“ | Ponuda traži smenu koja pokriva ceo posao | Isto kao gore |

Uloge se mogu popuniti odjednom iz radnog mesta skriptom `OP_026` (7 manevrista, 8 vozovođa, 9 i 19 mašinovođa, 10 mašinovođa manevre, 22 pregledač). Posle toga ih treba proveriti na ekranu.

### 2.2 Dodatno za mašinovođe

| Podatak | Gde se unosi | Pravilo |
|---|---|---|
| **Poznavanje serija** | matrica „Poznavanje serija“, sa rokom „Važi do“ | Mašinovođa mora da poznaje seriju **svake** lokomotive posla (vučne i potiskivalice). Inače dobija ocenu „Bez serije 185, 193“. |
| **Poznavanje pruga** | tab Pruge, sa rokom „Važi do“ | Mora da poznaje **svaku** prugu na ruti. Inače dobija ocenu „Bez pruge …“. |
| Rok važenja | isto | Prazan rok znači da važi bez ograničenja. Istekao rok znači da osoba ne poznaje seriju ili prugu. |

Ako nijedan mašinovođa ne prolazi proveru, plan prijavljuje konflikt „Nema mašinovođe sa kvalifikacijom“.

### 2.3 Pregledači

- Nude se osobe sa ulogom **Pregledač kola** ili **Manevrista**.
- **Samo domicilna:** osoba se nudi samo u svojoj matičnoj stanici, pa matična stanica mora biti uneta. Bez ove oznake pregledač može i na drugu stanicu, uz vreme puta po mreži.

### 2.4 Prevoz do stanice (AUTO)

- **AUTO** označava osobe koje imaju auto i mogu same da stignu, ili da prevezu kolegu.
- Ako osoba nije na lokaciji, a nema AUTO i nema ni par ni vozača, ne može da se izabere („Nema AUTO, nema ko da ga preveze“).
- Preporuka: čekirati AUTO kod svih koji ga zaista imaju, kod mašinovođa, pregledača, vozovođa i manevrista.

### 2.5 Ostalo na ekranu Osoblje

| Podatak | Napomena |
|---|---|
| **Odsustva** | Bolovanje i godišnji se čitaju sami iz postojećih tabela. Ručno se unose samo odsustva koja tamo ne postoje. |
| E-mail | Bez e-maila zadatak se upisuje, ali se javlja telefonom. |
| Lekarski pregled, Periodični ispit, Školovanje | Unose se, ali ih ponuda **za sada ne proverava**. |
| Radno mesto | Menja se u kadrovskom delu programa, ne ovde. |

---

## 3. Lokomotive

Ekran: **Transport → Lokomotive**.

### 3.1 Serije (tab Serije)

| Podatak | Zašto je potreban |
|---|---|
| **Oznaka** (185, 193, 186…) | Po njoj se proverava poznavanje serija kod mašinovođa |
| **Pogon** (vrsta vuče) E ili D | Na pruzi bez kontaktne mreže nude se samo dizel lokomotive. Na elektrificiranoj ruti prednost imaju električne. |
| **Masa (t)** | Masa se vodi samo na seriji. Ulazi u bruto ako je uključen parametar `MASA_LOK_U_VUCI`. |
| Masa po osovini (t) | Za sada samo evidencija; kasnije će je koristiti rutiranje |
| Vlasništvo (Sopstvena / Zakup) | Evidencija na nivou serije; ponuda gleda vlasništvo lokomotive |
| **Aktivna** | Samo aktivne serije su u matrici poznavanja serija |

### 3.2 Lokomotive (tab Lokomotive)

| Podatak | Zašto je potreban | Šta se desi ako fali |
|---|---|---|
| **Broj** (npr. `193 891`) | GPS se povezuje po ciframa broja | Lokomotiva se ne vidi na mapi i nema GPS lokaciju |
| **Serija** | Vrsta vuče, tablica masa, poznavanje serije | Ne nudi se. U ponudi osoblja piše „Lokomotiva bez serije“. |
| **Aktivna** | Samo aktivne se nude | Ne nudi se |
| **Matična stanica** | Lokacija kad nema GPS-a ni prethodnog posla | „Lokacija nepoznata“: lokomotiva ide na kraj liste, a prazna vožnja se ne računa |
| **GPS** (čekirano) | Oznaka da lokomotiva šalje poziciju | Samo informacija na planu |
| Vlasništvo, Zakupodavac, Technodis ID | Evidencija (masa se unosi na seriji) | Ne utiče na ponudu |

### 3.3 Zauzetost (tab Zauzetost)

Servis, namenska zauzetost, projekat i ostalo, sa periodom Od–Do. Lokomotiva se u tom periodu ne nudi. Namenska se nudi samo za posao istog projekta. Servis u narednih 48 h kvari ocenu lokomotive.

### 3.4 Tablica masa (ekran Tablica masa)

Za svaku aktivnu seriju treba uneti najveću masu po pruzi ili relaciji i smeru, za jednu lokomotivu i, ako se koristi, za dve.

- Bez tablice ta serija se **isključuje** iz ponude: „Za seriju X nema tablice masa na ovoj ruti“.
- Bez reda za dve lokomotive koristi se dvostruka vrednost za jednu, uz upozorenje „Nema tablice za dvostruku vuču“.

---

## 4. Ostali podaci od kojih zavisi plan

| Podatak | Ekran | Napomena |
|---|---|---|
| Stanice i koordinate | Mreža i stanice | Stanice „Za proveru“ treba potvrditi ili prevući na mapi |
| Pruge i elektrifikacija | Mreža i stanice, Registar | Posle `OP_019` sve deonice imaju elektrifikaciju |
| Trase | Trase | Bez trase deonica dobija upozorenje „Nemamo trasu“ |
| Parametri | Parametri | Obrada prijema i predaje (60 min), brzina prazne vožnje (50 km/h), najduži rad (12 h), odmor (12 h) |
| Turnusi | Turnusi | Dnevna 07–19 i noćna 19–07 |

---

## 5. Preporučeni redosled popunjavanja

1. **Serije:** oznaka, vrsta vuče, masa, aktivna.
2. **Lokomotive:** serija, broj u obliku koji šalje GPS, matična stanica, aktivna.
3. **Tablica masa** za svaku aktivnu seriju na prugama kojima vozimo.
4. **Osoblje:** uloge (`OP_026` pa provera na ekranu), Uključi u plan, matična stanica, AUTO, Samo domicilna.
5. **Kvalifikacije mašinovođa:** serije i pruge, sa rokovima.
6. **Turnusi:** dodela turnusa osobama, pa „Generiši smene iz turnusa“ za period plana.
7. **Odsustva** kojih nema u bolovanju i godišnjem.
8. **Probni rad:** otvoriti Ponudu osoblja i Ponudu lokomotiva za jedan stvarni posao. Ocene iz tabela iznad pokazuju koji podatak još fali.

---

## 6. Otvorena pitanja

- Lokomotive serije 186: u bazi ih još nema, pa treba uneti brojeve. Da li je 187 310 zapravo 186?
- Da li je serija 648 zaista električna?
- Da li ponuda treba da proverava lekarski pregled, periodični ispit i školovanje?
- Tester traži mašinovođu sa matičnom stanicom, serijom 185 i prugama Šid – Ruma – Novi Sad za test celog toka osoblja.
