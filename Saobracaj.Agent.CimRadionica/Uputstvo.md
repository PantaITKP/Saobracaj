# CimRadionica – uputstvo (v3.0)

CimRadionica je alat za testiranje agenta CimProvera. Koristi isto jezgro kao agent: ista pravila, isti PRIMER i isti upis poslova. **Nikad ne šalje mail.** Odgovor priprema kao u režimu Live (sa primaocima) i čuva ga u `odgovor.html`. Stanje agenta na serveru se ne dira.

**Pokretanje:** prečica **CimRadionica** na Desktopu (`C:\CimRadionica\Program\CimRadionica.exe`). Podaci su u `C:\CimRadionica\`:
- `Radni\<radni prostor>\` – poruke, stanje, rezultati, Zbirni Excel, `_Analiza`;
- `kes\` – keš čitanja;
- `sifarnici\` – kopija šifarnika za offline.

## 1. Uvoz (kartica Poruke)
- **Radni prostor**: izaberite postojeći (npr. `Osnovni`) ili dugme **Novi…**. Svaki radni prostor ima svoj dosije i ponavljanja.
- **Uvezi fajlove**: `.msg` (Outlook), `.eml`, `.zip` (sa porukama).
- **Uvezi folder**: folder sa `.msg`/`.eml` porukama, ili folder jednog maila (prilozi + `mail.txt`/`mail.docx` sa redovima `From:`, `Subject:`, `Date:` na početku).
- Poruke su grupisane u **niti** (isti naslov bez RE/FW i veze odgovora) i poređane po datumu slanja. Ista poruka se ne uvozi dvaput.

## 2. Obrada
- **Obradi izabrano** / **Obradi sve**. Opcija **samo izabrana nit** obrađuje celu nit izabrane poruke.
- Obrada ide hronološki, u delovima od po 20 poruka; svaki deo radi u posebnom procesu. Napredak se vidi u statusnoj traci.
- **Verzija**: oznaka rezultata (npr. `3.0.0`, `3.0.0-proba`). Rezultati idu u `rezultati\<verzija>\<datum>_<naslov>\`:
  - `PRIMER_*.pdf`;
  - `odgovor.html` (Live, nije poslat);
  - `rezultat.json`;
  - `log.txt`.
- **obriši tmp podatke pre obrade** (samo online): `DELETE` podataka iz `planiranje.tmp*`; `tmpSinonimi` i `tmpNHM` ostaju.
- **Kreni ispočetka**: briše dosije i ponavljanja radnog prostora; keš i rezultati ostaju. Novu verziju za poređenje obradite ispočetka.
- **napravi _Analiza**: uz rezultat pravi i `_Analiza\<verzija>\NNN_…\ANALIZA.md`, sa slikama strana i sa INDEKS/SAŽETAK.

## 3. Rezultat
Kartica **Rezultat** za izabranu poruku prikazuje odgovor, `rezultat.json` i log. Dugmad otvaraju PRIMER, folder rezultata i originalni `.eml`.

## 4. Excel i oznake (kartica Zbirno i poređenje)
- **Zbirni Excel** `Zbirno_<verzija>.xlsx` ima red po mailu i tovarnom listu:
  - po kolona za svaku rubriku (G = greška, U = upozorenje, P = preporuka, (int) = samo interno);
  - JobID i polja posla, INTERNO, događaje i trajanje.
- U kolone **Tačno?** (`da` / `ne` / `delimično`) i **Komentar** upišite svoju ocenu. Ocene ostaju i posle ponovne obrade (kolona **Ključ** se ne menja).
- **Uporedi verzije**: izaberite dve verzije. Nastaju `Poredjenje_<a>_<b>.xlsx` i `.html`. Promena reda koji je ranije bio „Tačno? = da“ označena je kao **REGRESIJA**.
- **Napravi paket…**: zip za analizu sa porukama, rezultatima, `_Analiza` i Zbirnim Excel-om, bez lozinki, DPAPI fajlova i connection string-a. Opcija „samo ne/delimično“ pakuje samo redove sa tom ocenom.

## 5. Pregled poslova i pravila
- **Poslovi** i **Teretnice**: samo čitanje.
  - Online prikazuje `planiranje.tmpJobs`/`tmpTeretnica` sa nazivima umesto ID-eva, poreklom polja i istorijom.
  - Offline prikazuje predlog posla iz `rezultat.json`.
- **Događaji**: događaji iz teksta maila (ETA, dolazak, broj kola, ID najave…). **Rečnik izraza u Excel** daje broj izraza po jeziku.
- **Pravila**: sva pravila iz JSON-a, sa izvozom u Excel.

## 6. Online / offline i šifarnici (kartica Podešavanja)
- **Online**: upis u `planiranje.tmp*`, kao agent. Treba konekcija: **Podesi konekciju (DPAPI)**; čuva se šifrovano, samo za vaš Windows nalog.
- **Offline**: ništa se ne upisuje u bazu. U `rezultat.json` ide predlog posla iz lokalne kopije šifarnika.
- **Osveži lokalnu kopiju šifarnika** (potrebna konekcija) kopira, samo čitanjem:
  - stanice;
  - Partnerji (korišćene kolone);
  - tmpNHM i tmpSinonimi;
  - iz Ponudba samo broj ponude i partnera.
- Režim i datum kopije šifarnika uvek su prikazani u statusnoj traci.

## 7. Testiranje (praćenje foldera)
Kartica **Testiranje** prati `Desktop\Testiranje\`:
- novi `.msg`/`.eml` se obrađuju po datumu slanja, u posebnom radnom prostoru **Testiranje**;
- `_Analiza` nastaje u istom folderu;
- beleške `X - napomena.txt/.docx` i `Zapazanja.docx/.txt` ulaze u ANALIZA.md i ne obrađuju se kao mail;
- **Kreni ispočetka** briše stanje i rezultate praćenja.
