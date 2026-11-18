# CimProvera – odluke i izuzeci od GLV-CIM

Glavni izvor poslovnih odluka za agenta CimProvera i alat CimRadionica. Ažurira se u svakom zadatku.
Za svaku stavku su navedeni datum (ako je poznat) i izvor u zagradi. Važi poslednja odluka; zamenjene stavke su označene „~~zamenjeno~~“ ili „(zamenjuje …)“.

**Izvori:**
- promptovi u `\\192.168.1.6\Cloude\Agenti\CimProvera\prompt\` (skraćenice niže);
- `Pravila\cim_validation_rules.json` (`meta.changelog`, `basis`, `popunjava_basis`, `ne_proverava_se`);
- odluke korisnika iz razgovora (označene „korisnik, datum“).

**Testovi, pravila i sinonimi se ne brišu.**

**Skraćenice izvora:**

| Oznaka | Fajl | Datum |
|---|---|---|
| v1 | `PROMPT_ispravke_agenta_v1.md` (v2 je identičan) | test 10.11. |
| v3 | `PROMPT_ispravke_agenta_v3.md` | 10.11. |
| NEZ | `PROMPT_provera_na_novim_dokumentima.md` | — |
| Vo1569 | `PROMPT_ispravke_Vo1569.md` | 11.11. |
| v2.7 | `PROMPT_izmena_pravila_v2.7.md` / `-1` | 12.11. |
| NA | `PROMPT_nova_adresa_i_pdf_primer.md` | — |
| SVE | `PROMPT_v2.7.1_agenti_pdf_SVE.md` | 12.11. |
| 13-11 | `PROMPT_ispravke_13-11_testni_primeri.md` | 13.11. |
| HER-SAD79 | `PROMPT_ispravka_Excel_CIM_HER-SAD79.md` | 13.11. |
| Word-CUV | `PROMPT_ispravka_Word_CIM_CUV.md` | 13.11. |
| Syncfusion | `PROMPT_Word_preko_Syncfusion.md` | — |
| Hitno | `PROMPT_hitno_zaglavljeno_sanduce.md` | 13.11. |
| v2.8.1 / v2.8.2 / v2.8.3 | odgovarajući `PROMPT_v2.8.x_…` | 13.–14.11. |
| v2.8.4 | `PROMPT_v2.8.4.md` = `PROMPT_v2.8.4_pravi_saobracaj.md` | 14.11. |
| UPIS | `PROMPT_automatski_upis_tmpJobs.md` | v2.9.0, 14.11. |
| ANALIZA | `ANALIZA_automatski_upis_poslova.md` | 13.11.; predlog, zamenjen UPIS-om gde se razlikuje |
| IZV13-14 / IZV15-16 / IZV16-17 | izveštaji iz foldera prompt | 13.–17.11. |
| v2.9.1 / v2.9.2 / v2.9.3 / v3.0 | odgovarajući `PROMPT_v…` | 16.–17.11. |
| JSON x.y | `meta.changelog` u `cim_validation_rules.json` | — |

---

## 1. Opšta pravila rada i bezbednost

**Tajne i pristup**
- Tajne (lozinka sandučeta, konekcija baze, Syncfusion ključ) samo u DPAPI fajlovima. Nikad u kodu, config-u u otvorenom tekstu, logu, testovima, dokumentaciji ni git-u. Lozinku za agenti@ unosi korisnik (NA, SVE, UPIS, Syncfusion, v2.9.1).
- Connection string iz `app.config` aplikacije Saobracaj ne kopira se nigde. Na serveru je samo `Konekcija.dpapi` (UPIS, v2.9.1).
- Agent radi pod nalogom `KPSRV-NOVI\svc_cimprovera`. Prava daje `OP_913_tmp_Prava.sql`, koji pokreće korisnik (v2.9.0–v2.9.2).

**Git, postavljanje, alati**
- Commit samo lokalno, **nikad push** dok korisnik ne kaže. Delovi zadatka mogu u posebne commit-e (svi promptovi, v2.8.1, SVE, v3.0).
- Pre postavljanja na server: rezervna kopija (`backup-pre-<verzija>-<datum>`, ranije `istorija\<datum>\`). Verzija na serveru se ne menja dok svi testovi ne prođu (NA, SVE, v2.9.x, v3.0).
- Ništa se ne instalira bez potvrde. Za novu biblioteku prvo se pita.
  - Licenca mora dozvoljavati komercijalnu upotrebu: iText 5+ (AGPL) i EPPlus 5+ su zabranjeni.
  - Word → PDF ide preko Syncfusion DocIO (ključ iz `Syncfusion.dpapi`), LibreOffice se ne instalira.
  
  (NA, HER-SAD79, Syncfusion)
- Agent radi lokalno, bez API-ja i interneta (OCR Tesseract). Pravila su u JSON-u i menjaju se bez builda (v1).
- Fajl pravila: `cim_validation_rules.json`, verzija samo u fajlu; stare verzije u `pravila\istorija\`. Izmena težine ili pravila podiže verziju pravila (v1, v3, Vo1569, 13-11).
- Pravila i težine koje zadatak ne pominje se ne menjaju (v1, v3, DOP, SVE, v2.9.1).
- Originali na Desktopu se ne menjaju; testni fajlovi se kopiraju u `testovi\nezavisni\...` (13-11, v2.8.4).

**Testovi**
- Testovi posle svake tačke. Svi postojeći testovi moraju da prođu, a svaka promena očekivanja se obrazlaže (13-11, v2.8.4).
- Očekivani rezultati iz promptova su referenca: razlike se prijavljuju, agent se ne prilagođava na silu (NEZ, 13-11, HER-SAD79).
- Šablon obrasca se proverava na drugom dokumentu istog obrasca, ne na onom od kog je napravljen (NEZ).
- Prilog `<ime>.strane.json` važi samo u testovima; u obradi maila se ignoriše („nedozvoljen tip priloga“) (v2.8.1).
- Neispravan Unicode u naslovu i telu ne sme da obori obradu (zamena U+FFFD, upozorenje u logu) (Hitno).

**Odgovornost i stanje**
- Rubrike se proveravaju po pravilima bez obzira ko je napravio dokument (i KP) (v2.8.4).
- `popunjava` (pošiljalac/prevoznik) se upisuje po GLV-CIM, bez pogađanja (v2.8.4).
- **Ništa se ne briše iz sandučeta**: IMAP samo čita, ne briše i ne premešta. Brisanje radi korisnik, posle potvrde arhive (NA, SVE, v2.9.3, v3.0).
- `reset-stanje` i potpuni reset pokreće korisnik (skripta `Reset-CimProvera.ps1` kao administrator). Pre brisanja agent šalje spisak i čeka potvrdu (DOP, v2.8.2).
  - Reset briše: stanje, keš, rezultate, logove, kopije priloga.
  - Reset ne briše: config, DPAPI fajlove, pravila i istoriju, exe, zakazani zadatak, testove.

## 2. Test / Live režim, slanje i primaoci

**Test režim i sanduče**
- Režim ostaje **Test**: sva pošta ide samo na agenti@kprevoz.co.rs. Guard u `Slanje.Posalji` uklanja svakog drugog primaoca (To/Cc/Bcc) i beleži „TEST: uklonjen primalac …“ (v2.8.4, JSON 2.8.4, v2.9.x).
- Sanduče: agent čita agenti@kprevoz.co.rs; From, Reply-To i To su agenti@ (NA, SVE).
  - ~~stefan.obradovic@ kao test primalac~~ (v1, v3) – zamenjeno.
  - Adrese su podešavanja, ne konstante (NA).
- Iz razvojnog okruženja se ne šalje; probni mailovi se prave kao .eml/.html (SVE).

**Zaštita od petlje**
- Odgovor nosi `X-CimProvera` i `Auto-Submitted: auto-replied`.
- Sopstveni odgovori se preskaču („sopstveni odgovor – preskočeno“), i po naslovu „Tovarni list CIM … – potrebne ispravke / – napomene“ (NA, SVE, 13-11).

**Live režim (pripremljen, nije uključen)**
- Primaoci (v2.8.4): „Odgovori svima“ = From + To + Cc bez duplikata.
  - Uklanja se agenti@ i adrese samo zbog prosleđivanja.
  - KP adrese ostaju; Bcc se ne koristi.
  - Više od 25 primalaca → samo interno.
- Nit: naslov „RE: <orig.> – CIM provera: potrebne ispravke“ / „– CIM check: corrections required“, uz In-Reply-To i References (v2.8.4).
- Test mail u zaglavlju navodi „U Live režimu išlo bi na: To: …; Cc: …“ (v2.8.4).
- Interni izveštaji (ručna provera, KP tovarni list, odeljak INTERNO) nikad ne idu spoljnim adresama (v2.8.4).
- U Test režimu interni nalazi su u odeljku „INTERNO (ne ide pošiljaocu)“ na dnu maila. U Live režimu ih nema ni u mailu ni u PRIMER-u pošiljaocu (v2.8.3).
- Isti tovarni list (iste verzije) šalje se svima najviše jednom (v2.8.4).
- Uslov za Live: nijedna lažna greška na nezavisnim dokumentima, ≥ 90 % očekivanih grešaka pronađeno, ručna provera sa jasnim razlogom (NEZ, 13-11). Pre Live-a kratak test na stvarnom sandučetu: reply-all, zaglavlja, guard, više mailova (v3.0).
- Predlozi korisniku, **nisu odlučeni**:
  - odgovor pošiljaocu samo kada je izdavalac/špediter, a ARR/Aviso i tuđe niti samo interno (IZV13-14);
  - ~~blokada slanja preko SMTP filtera~~ (v1) – v3 je više ne pominje.

## 3. Sanduče, stanje, ponavljanja, dosije

**Obrada sandučeta**
- Svaka poruka ima svoj try/catch. Posle 3 neuspeha: „greška – ručna provera“ i jedno interno obaveštenje. Pošiljaocu ništa (Hitno).
- Pad celog kruga → interno obaveštenje najviše jednom na sat (Hitno).
- Nulta tačka: najveći UID u trenutku reseta (13.11.2026 23:41:27, UID 191). Poruke ≤ tog UID-a se ne obrađuju. Stanje se vodi po sandučetu (v2.8.2, NA).
- U log se upisuje trajanje svakog maila. Mail sa mnogo skenova ide posle ostalih; poslednji UID se ne pomera preko neobrađene poruke (v2.8.3).

**Ponavljanja i dosije**
- **Ponavljanja** (`stanje\ponavljanja.json`, v2.8.4):
  - isti prilog (heš) se ne proverava ponovo;
  - isti tovarni list (ista r62, ili pošiljalac + r8 + relacija) sa nepromenjenim rubrikama se ne prijavljuje ponovo;
  - najviše jedna interna poruka po mailu.
- **Ponovljeni mail** (isti prilozi): odgovor se ne šalje, ali se prijem beleži (`tmpDokument.BrojPrijema`, `PoslednjePrimljeno`) i tekst maila ide u Napomenu posla (v2.9.1 5.7).
- **Dosije pošiljke** (`stanje\posiljke.json`, 7 dana; v2.8.4):
  - veza po In-Reply-To/References, naslovu bez RE/FW/Fwd/Odg/AW, r62, r8, relaciji, kolima;
  - priloženost (X22, X26/X27, R18-SPISAK) se gleda na celom dosijeu;
  - nova verzija lista: „Ovo je nova verzija tovarnog lista od … Ispravljeno: … Ostalo neispravljeno: …“, navode se samo greške;
  - skenirana i overena verzija proverenog digitalnog lista: samo pečat, r58a, r62, r29.
- Isti CIM sa novim prilogom (nova verzija spiska): pošiljaocu ništa, posao se ažurira (istorija). U Test režimu ide kratka interna poruka šta je ažurirano (v2.9.2 3.3).
- **Arhiva sandučeta** (v2.9.3, v3.0):
  - pre brisanja sve poruke od UID 191 (INBOX i INBOX.Sent) idu u `\\…\CimProvera\arhiva\<folder>\<UID>.eml`, uz `indeks.csv`; arhiva je samo za čitanje;
  - ponovna obrada može iz arhive (`--izvor arhiva`);
  - brisanje poruka ni promena UIDVALIDITY ne smeju da pokvare poslednji UID, dosije, tmpDokument ni ponavljanja (Message-ID + heš priloga).
- **Ponovna obrada** (`ponovna-obrada --od-uid N --obrisi-podatke`): samo upis, bez slanja; stanje agenta se ne menja; delovi od po 20 mailova u posebnim procesima (v2.9.1, v2.9.2).

## 4. Klasifikacija dokumenata i strana

**Tovarni list**
- Klasifikacija ide strana po strana, po sadržaju (natpisi obrasca na svim jezicima, brojevi rubrika, mreža rubrika). **Ime fajla nikad ne određuje tip** kada sadržaj daje jasan rezultat (v1, 13-11).
- Strana je tovarni list CIM/CUV kada ima:
  - natpis obrasca i brojeve rubrika (v2.8.4); ili
  - bez natpisa, **mrežu rubrika** (poravnat šablon), a ne samo reči koje liče na natpise – opšti uslovi prodaje (Imerys) nisu CIM (v2.9.2 3.1b; „liči na obrazac“ samo uz natpis, mrežu ili 40+ natpisa).
- Ime fajla „CIM …“ + sadržaj tovarnog lista (platilac, „CIM 6“, 13.3, CIT 23, Tara) → tovarni list, ne faktura. Sken čiji natpisi nisu pročitani ide na ručnu proveru (v2.9.2 3a.8, voz 262).
- Ako je obrazac neprepoznat ili poravnanje nepouzdano: rubrike se ne proveravaju nasumično, ide ručna provera (v3).
- Sken strane ispod 0,75 pouzdanosti: nikad „nema tovarnog lista“, nego interna ručna provera (13-11).
- Orijentacija (OSD, 90/180/270°) i obrada crno-belog skena pre OCR-a (v2.8.4).

**Spisak kola i ostali dokumenti**
- Spisak kola (CIT 23, Wagenliste, Wagen Liste, Lista de vagoane, vykaz, Spisak kola, СПИСАК КОЛА) je spisak, i kada naslov pominje Frachtbrief (v2.9.2 3.1a).
- Strana 2/2 CIT 23 (zaglavlje kolona, red Σ) je nastavak spiska, nikad tovarni list (v2.8.4).
- Kopije istog lista (Original, Duplikat, list 2/3, Feuille…) i poleđina nisu novi list ni prilog u r9 (v1, v3, 13-11). Provera i izveštaj idu jednom po tovarnom listu (v2.9.1 4.1).
- CUV (X u r30) je CUV; za njega važe pravila za prazna kola (v1, v3, Word-CUV).
- Poseban tip, ne proverava se po rubrikama: CIT 7 (naknadni zahtev), najava/otkaz voza, sastav voza (MÁV VTK, Zugvorbereitung, „Натурен лист на влак“) (13-11, v2.8.3).
- Spisak tovarnih jedinica (loadlist) je izvor podataka kao spisak kola (HER-SAD79).
- T1/T2 i EUR.1 priznaju se samo uz natpis obrasca (+ MRN). Invoice/Total je faktura. Dodatni tipovi: `dozvola_otpad`, `izvestaj_radioloski` (Vo1569).
- Sopstveni PRIMER (naslov PDF-a „Primer ispravnog tovarnog lista“, proizvođač CimProvera, traka „Primer ispravnog popunjavanja – Kombinovani prevoz“) se preskače: ne proverava se i ne upisuje (v2.9.2 3.1c).

**Odgovor kada nema tovarnog lista**
- Mail bez tovarnog lista (samo spisak, carinski dokumenti, najava, sastav voza) **ne dobija nikakav odgovor, ni interni**. Samo red u logu „nema CIM/CUV – bez odgovora; dokumenti: …“ (v2.8.4, IZV13-14; zamenjuje ~~„spisak kola bez tovarnog lista“~~ iz NEZ i HER-SAD79).
- Nepročitan ili neprepoznat prilog: interna poruka „nije moglo da se pročita“ samo ako strana liči na CIM/CUV ili naslov/tekst maila pominje CIM, CUV, tovarni list, teretnicu ili Frachtbrief. Inače samo log i tmpDokument (v2.9.2 3.2; zamenjuje strože pravilo iz v1/HER-SAD79).

**Excel, Word, AcroForm**
- Excel je tovarni list ako ima naslov obrasca i desetak brojeva rubrika u ćelijama. Vrednosti se čitaju iz ćelija bez OCR-a; „X“ je označeno polje (HER-SAD79).
- Isti list u PDF-u i Excelu proverava se jednom: PDF ako je potpisan/pečatiran, inače Excel (HER-SAD79).
- Word (.docx/.doc/.rtf) → PDF (Syncfusion DocIO, najviše 60 s), pa kao PDF. Vrednosti iz tekstualnog sloja; blok teksta pripada rubrici sa većim delom površine. Neuspeh → ručna provera (Word-CUV, Syncfusion).
- AcroForm polja i FreeText anotacije (pisaća mašina u Acrobat-u) su podatak na svom položaju (13-11, v2.8.4).

## 5. Rubrike

**Nečitljivo i nepouzdano**
- Nečitljiv ili nepouzdan OCR nikad ne daje grešku ni upozorenje o sadržaju. Samo interno „rubrika nije pouzdano čitljiva – proveriti ručno“ (jedan red sa spiskom rubrika) i ručna provera (13-11, v2.8, v2.8.4; zamenjuje ~~upozorenje~~ iz Vo1569).
- Stanja rubrike: prazno → greška ako je obavezna; placeholder → „upisane su samo oznake“; nečitljivo → ručna provera.
- Polja za označavanje (r20, r22, r23, r30, r52, r58b) i kućice r16 čitaju se po gustini piksela (v1, v3).

### r1 / r4 – pošiljalac i primalac
- Zemlja nedostaje → greška R1/R4. Zemlja rečima umesto ISO šifre → upozorenje „Italy → IT“ (R1-ISO/R4-ISO) (v1, v3, JSON 2.3, v2.8.4).
- Zemlja se traži samo u adresi, ne u nazivu firme („Louis Dreyfus Company Hungary Kft.“ nije zemlja). Naziv zemlje **posle** mesta ili poštanskog broja je zemlja (JSON 2.8.2, v2.8.4).
- Zemlja za dopunu se određuje redom:
  1. iz adrese: naziv zemlje, poštanski broj sa oznakom („D-44653“, poljski „41-800“), PDV sa oznakom („PL6292…“), poznato mesto;
  2. iz stanice iste strane (r1 → r16/r17, r4 → r10/r12).

  Nikad iz r62, prevoznika ni druge strane; nepoznato = „XX“ (v2.8.3, v2.9.2 3.4).
- Adresa: „nedostaje adresa“ samo kada nema ni ulice ni mesta (13-11, JSON 2.8).
- Kontakt (telefon, e-mail) i korisničke šifre r2/r3/r5/r6 su samo interno (REC8) (13-11, JSON 2.8).

### r7 – izjave, platilac, ponuda
- ~~KP7 (KP mora biti u r7)~~ obrisano. Zamenjuju ga (v2.7, interna odluka KP 2026-11-12):
  - **KP7-PLATILAC** (greška): mora biti naveden platilac za deonicu kroz Srbiju (SRB, RS, Srbija, Serbia, Serbien, IŽS, ŽS, „Serbian part/section/route“, „auf/für KP“). Platilac za ceo put pokriva i Srbiju.
  - **KP7-NIJE-PLATILAC** (greška): KP je platilac za srpsku deonicu ili ceo put, i to samo kada je KP sa desne strane („… ist KP“). KP kao platilac van Srbije je dozvoljen; KP kao prevoznik nije greška.
  - **KP7-NEJASNO** (upozorenje): „SRB: KP 3212“ bez reči za platioca – nejasno da li je KP platilac ili prevoznik.
  - **KP7-INFO**: „Platilac za deonicu kroz Srbiju: <naziv>“.
- Prepoznavanje platioca:
  - reči za platioca: platilac, Frachtzahler, Zahler, payer (fo/on), freight payer, payeur, Költségviselő, plaćanje/plaća, Zahlung, payment, paid by, „platilac za prugu“ (v2.7, 13-11, v2.8.4);
  - tolerancija 1 slova za veznike und/ist/für (v2.8.4);
  - naziv do kraja reda ili rečenice (v2.8.4);
  - „FREIGHT PAYER FOR X AND KP3212 – Y“ (i u r15) → Y (v2.8.3);
  - „for KP firma“, „za KP firma“, „KP-firma“, „KP: firma“ → firma plaća deonicu KP (v2.9.1 5.3).
- G5 (oblik „šifra + tekst“) nikad nije greška, samo upozorenje sa primerom „16. Platilac za deonicu SRB: <naziv>“ (13-11, JSON 2.8).
- **Platilac preko broja ponude** (v2.9.2 deo 1, korisnik 17.11.):
  - broj ponude se traži u r7, pa r13, pa r14: „Ponuda 8957“, „Ponuda8957“, „Ponuda br.“, „KP Ponuda“, „po ponudi“, „prema ponudi“, „Offer“, „offer nr“, „according/acc. to offer no.“, „Angebot (Nr.)“, „Ajánlat“; najbliži oznaci SRB/KP/3212;
  - samo ceo broj (1–6 cifara) se traži u `dbo.Ponudba` (samo `PonStPon` → `PonPart`, parametrizovano, samo čitanje); važenje se ne proverava;
  - oznake sa crticama („42-0003-26“) idu u BrojPonude, ali se ne traže.

  | Slučaj | Provera | Upis |
  |---|---|---|
  | Ponuda nađena, za SRB KP / nejasno / ništa | bez KP7 grešaka, r7 se ne označava u PRIMER-u | PlatilacID = PonPart, pouzdanost 100 |
  | Ponuda nije nađena | greška kao do sada + „Ponuda N nije pronađena u sistemu KP“ | — |
  | Firma X u r7, ponuda glasi na istu firmu | ništa | — |
  | Firma X u r7, ponuda glasi na Y | samo INTERNO „r7 navodi X, ponuda N glasi na Y“ | Y |
  | Baza nedostupna | pošiljaocu ništa, interno, ponavlja se u sledećem prolazu | — |

  - Preporuka R14 („upišite broj ponude“) se ne daje kada je broj ponude u r7/r13.
- Naročita pošiljka: R22 i X5 (nema šifre 11 sa odobrenjem) su upozorenja (v2.7). Za prazne RID cisterne se preporučuje šifra 5 (hitni telefon) (v1, NEZ).

### r9 – prilozi
- Prilog je u mailu (ili dosijeu), a nije u r9 (izvozna/VDD, T1, spisak, ENS – X22, X26) → greška. Naveden u r9, a nije priložen (X27: faktura, EUR.1, Annex VII…) → upozorenje (v1, v3, Vo1569, JSON 2.3).
- r9 prazna, a r18 upućuje na spisak (G3) → greška (JSON 2.6).
- Prilozi rečima bez UN/EDIFACT 1001 šifre (G6) → upozorenje. Šifra 271 za spisak kola nije potvrđena u GLV-CIM, pa se piše „<šifra> Spisak kola“ (JSON 2.3, 13-11, JSON 2.8).
- „CIT 23“ u r9 = spisak kola; „Invoice“ = faktura; „JCI i specifikacija vagona uz JCI“ je ispravno (13-11, Vo1569).
- Kod otpada X27 kao greška: predlog, nije odobreno (Vo1569).

### r13 / r14
- r13: R13 se ne proverava (šifre i tačke nisu obavezne) (v2.7).
- **KP3** (greška): KP mora biti naveden kao prevoznik u r13, u bilo kom obliku i bilo kom delu (i dodatni list) (v2.7).
- r14: provera prefiksa 1/2 je obrisana. Prazna r14 → upozorenje „preporučujemo broj ponude KP“, osim kada je sporazum ili tarifa u r13.5 ili je broj ponude naveden (v2.7, Vo1569, v2.9.2).
- G2/G11 se ne proveravaju (v2.7).

### r16 / r29 – mesto i datum
- r16 bez datuma i časa → greška. Prazne kućice (bez mastila) su prazne, ne „nečitljivo“ (v1, v3).
- r29: mesto i datum ispostavljanja su obavezni. Mesto mora imati bar dva slova. „2026.10.26“, „13.11.2026.“ i kućice „mesec-dan-čas“ su datumi. Redosled r16/r29 samo kroz X14 (upozorenje) (JSON 2.1, 2.8, v2.8.3, v2.8.4).
- r50 popunjava prevoznik: samo interno (13-11, JSON 2.8).
- Rukom pisane vrednosti koje OCR ne pročita → interna napomena (13-11).

### r18 / r21 – roba i kola
- R18, R21-WAGON i X10 se ne proveravaju. Jedini nalaz je **R18-SPISAK** (upozorenje: r18 upućuje na spisak, a spisak nije priložen) (v2.7).
- R21-MRN → upozorenje (odluka KP, JSON 2.3), sa stvarnim MRN-om iz T1/deklaracije. Kod izvozne JCI bez MRN-a traži se broj JCI. MRN samo u r99 → R21-MRN99 (upozorenje) (v1, Vo1569, JSON 2.4, 2.5).
- Otpad (NHM 7204, 4707, 3915, 7602, 7404; reči otpad/scrap/Abfall/rifiuti/waste; Annex VII; dozvola u mailu) bez EWC šifre → greška R21-EWC/X6, sa predlogom iz dozvole (Vo1569, JSON 2.4).
- X30 (upozorenje): broj kola u r21 razlikuje se od broja u imenu fajla (JSON 2.6).
- Interno: broj kola u r21/r18 ≠ broj na spisku (v2.9.0).

### r20 – Incoterms
- Čita se cela r20, sa kolonom „jusqu'à / bis“. Na skenu je R20-INCO upozorenje (13-11, JSON 2.8).
- r20 ≠ uslov isporuke iz fakture/JCI (X28) → upozorenje (Vo1569, JSON 2.3).
- R20-RUTA i R20-INCO-ZASTARELO (DAF/DDU) su upozorenja (JSON 2.1, 2.5).

### r22 / r23 – naročita pošiljka i RID
- R22 i X5 su upozorenja (v2.7).
- **r23 (RID), od v2.9.3/v3.0** – RID podaci idu u r21; kolona RID na spisku je potrebna samo kada RID podataka nema u r21 ili kada kola nose različitu opasnu robu (v2.9.3 deo 1):

  | Slučaj | Ocena |
  |---|---|
  | X u r23 i UN broj u r21 ili na spisku | ispravno |
  | X u r23, UN broja nema nigde | greška „nedostaju RID podaci (UN broj, naziv, klasa)“ |
  | Nema X u r23, a UN broj postoji | greška „treba X u r23“ |
  | Ni X ni UN | ništa |
  | Različita opasna roba po kolima, a spisak bez RID podataka po kolima | greška |

  - UN broj = „UN“/„UN-Nr.“/„ООН“/„UN No.“ + 4 cifre.
  - Prazna RID kolona na spisku nije greška kada je roba ista.

### r24 – NHM
- Greška samo kada NHM ima manje od 4 cifre ili je prazan. 4–5 cifara → preporuka R24-NHM6, sa šifrom iz JCI/T1/spiska ako postoji. Isto važi za spisak (X29/X29-NHM6) i za prazna kola (9922) (v2.7, interna odluka KP 2026-11-12).
- „9922.00“ = 992200, bez preporuke (HER-SAD79). Nepoznat NHM u primeru = „XXXXXX“ (v2.8.3).

### r25 – mase (provera)
- **R25-MASA** (zamenjuje R25; v2.7-1, JSON 2.7.1):
  - tovarena kola bez mase → greška;
  - prazna kola (NHM 9921/9922, CUV, „prazna kola / empty / Leer / üres…“) → preporuka „poželjno je upisati taru“;
  - upućivanje na priloženi spisak sa masama → bez nalaza;
  - spisak naveden, a nije priložen → greška;
  - nečitljivo → upozorenje o čitljivosti;
  - samo oznake („T= ……“, „Netto:“) → „masa nije upisana kao broj“ (JSON 2.8.2).
- Priložen spisak sa masama, a r25 prazna → preporuka (13-11, JSON 2.8).

### r57 – ostali prevoznici
- R57 i R57-DEONICA su obrisani: status i deonica nisu obavezni (v2.7, v2.8.3: REC4 obrisana).
- **KP57**, ocena od 17.11. (v2.9.2 deo 2):
  - KP se prepoznaje u bilo kom obliku: „3212“, „3212 KP“, „KP 3212“, „RS: 3212 KP“, „SRB: KP UIC 3212“, „3212 Kombinovani Prevoz“, „Kombinovani prevoz d.o.o.“;
  - naziv i UIC → ispravno;
  - samo naziv → upozorenje „dopisati UIC 3212“;
  - samo UIC → upozorenje „dopisati naziv“;
  - ni jedno ni drugo → greška (osim kada je KP ugovorni prevoznik u r58a).
- KP vozi samo unutar Srbije: dopuna „3212 KP“ u PRIMER-u ide samo uz srpsku deonicu, u novom redu, nikad pored strane deonice (v2.9.2).
- Redovi koji na skenu prelaze donju liniju r57 pripadaju r57 (v2.9.2).
- **Sken**, odluka 17.11. (v2.9.2 napomena, v3.0 0a.6): kada OCR ne može da pročita gust red, a postoji srpska deonica sa delom šifre ili KP 3212 u r13 → samo INTERNO „proveriti ručno“, ne greška.
- KP5 (drugi naziv uz 3212) → upozorenje; oznake mreže (SRB:, HR:…) se uklanjaju pre provere (v1, Vo1569).
- Ne proverava se da li su navedeni svi ostali prevoznici (v2.7).

### r58a – ugovorni prevoznik
- Pošiljalac popunjava r57 i r58a (GLV-CIM Prilog 2, str. 25 – „ukoliko ne postoji poseban ugovor“; interna odluka KP 2026-11-14).
- R58A: dovoljna je UIC šifra ili prepoznatljiv naziv; naziv, adresa i potpis nisu obavezni (v2.7).
- „UIC: 3212“, „UIC 3212“, pečat „UIC CODE KP 3212“ i „KP(3212)“ znače popunjenu rubriku (v2.8.4).
- Nečitljiv pečat → upozorenje „OCR čitanje pečata nije pouzdano – proveriti ručno“ (v2.7, v3.0 0a.6).
- **Zavisno od izdavaoca** (od v2.9.3/v3.0):

  | r62 preduzeće | r58a prazna | r58a popunjena |
  |---|---|---|
  | 3212 (izdao KP) | **greška**, predlog „3212“ (KP2) | ispravno ako ima 3212 ili naziv KP |
  | drugi prevoznik (npr. 3154) | **upozorenje**, predlog = šifra i naziv iz r62 („3154 Unicom Tranzit“), **nikad 3212** | ispravno ako ima šifru ili naziv bilo kog prevoznika |
  | r62 nečitljiv/prazan | upozorenje „r58a prazna – proveriti“ | ispravno |

### r62 – identifikacija pošiljke
- r62 pošiljalac upisuje ako ga zna. Prazan ili nepotpun r62 je napomena za spoljnog pošiljaoca; kod KP tovarnog lista (r62 = 3212) je interni nalaz (GLV-CIM Prilog 2, str. 34; interna odluka KP 2026-11-14).
- **KP tovarni list** (r62 preduzeće 3212): ceo izveštaj je interni („[INTERNO] KP tovarni list – …“), na srpskom, nikad pošiljaocu (13-11, JSON 2.8).
- r62 na skenu sa nepouzdanim ciframa ne koristi se za odluke (KP list, ponavljanje, dosije, ključ) → „r62 nečitljiva – ručna provera“ (v2.8.1, v2.8.4).
- X19: greška samo kada je identifikacija upisana i na spisku i u r62 i razlikuju se. Ako je nema na spisku → napomena (v2.8.4).
- r62 kao ključ posla samo kada je ispravna: važeći UIC kod zemlje i kontrolna cifra stanice (v2.9.2 3a.1).

### r49 / r50
- Popunjava ih prevoznik: samo interno („popunjava prevoznik (KP pri prijemu)“), nikad greška pošiljaocu (JSON 2.4, 2.8, 2.8.4).

## 6. Izveštaj (mail) i jezik

**Jezik**
- Mail sa @kprevoz.co.rs uvek dobija srpski.
- Spoljni pošiljaoci: po domenu (.rs, .ba, .me, .hr, .si → srpski, ostalo engleski). Opšti domeni (gmail, yahoo, outlook, live…) se gledaju po tekstu koji je pošiljalac sam napisao.
- Engleski izveštaj je ceo na engleskom.

(13-11, v2.8.1)

**Sadržaj maila**
- Naslov pošiljaocu: „Tovarni list CIM <r62> (<referenca>) – potrebne ispravke / – napomene“ (v2.7).
- Format: multipart/alternative, UTF-8, HTML tabele sa inline stilovima (v2.7).
- Uvod: zahvalnica sa relacijom, brojem kola, robom i referencom, samo ono što je pouzdano pročitano (v2.7).
- Tabele:
  - „POTREBNE ISPRAVKE“ (greške);
  - „NAPOMENE (molimo proverite)“ (upozorenja i preporuke);
  - kolone Rb. | Rubrika | Šta nije u redu | Kako treba da izgleda (primer);
  - jedan nalaz = jedan red; nalazi različite težine nikad u istom redu (v2.7, JSON 2.7.1).
- Informacije (platilac) idu ispod tabela (v2.7).
- „Kako treba da izgleda“ dolazi iz iste dopune kao PRIMER („+ DE (posle „44653 Herne“)“). Opis formata samo kada dopune nema (v2.8.3).
- Nalazi se grupišu po tovarnom listu; isti nalaz na kopijama ide jednom (13-11, v2.8.3).
- Za spoljnog pošiljaoca samo rubrike koje popunjava pošiljalac; ostalo u INTERNO (v2.8.4).
- Potpis: „Priprema saobraćaja, Kombinovani prevoz d.o.o., Jug Bogdanova 28, 18400 Prokuplje, priprema@kprevoz.co.rs“ (v2.7).
- Iz izveštaja je uklonjen šum: REC3, KP6, REC6; r8 samo interno (13-11, JSON 2.8).

**Interni izveštaj**
- Bez „Poštovani“, zahvalnice i potpisa, sa kratkim uvodom („INTERNO – ručna provera. Mail … Uradite ručnu proveru pre odgovora pošiljaocu.“) (v2.8.3).
- KP tovarni list: „Primer ispravke je u prilogu.“ (v2.8.4).

## 7. PDF primer (PRIMER)

**Kada se pravi**
- Pravi se kada postoje greške ili upozorenja. Ne pravi se kada je list na ručnoj proveri ili obrazac nije prepoznat; tada u mailu piše „Primer … nije priložen jer obrazac nije automatski prepoznat“ (NA, SVE).
- Bez strane sa legendom (zamenjuje 13-11 i NA). Samo strane tovarnog lista, i spisak kada na njemu ima nalaza. Strana je A4 (v2.8.1, v2.8.2).

**Izgled**
- Podloga je originalna strana. Upisano ostaje vidljivo. Crvenom se dopisuje samo ono što nedostaje, u prvi slobodan red ispod ili desno, Arial 9 → 6 pt. Ono što ne staje ide na donju marginu, sa brojem. Dopuna nikad ne ide preko druge rubrike ni u red natpisa (v2.8.1, v2.8.2).
- Pogrešan deo se precrtava tankom linijom, a ispravka se piše pored (npr. NHM „2520“ → „252010“) (v2.8.1).
- Oznaka nalaza: krug sa brojem iz tabele (ispravke crveno, napomene narandžasto), u gornjem desnom uglu iste rubrike. Napomene imaju dopunu samo kada je predlog siguran (v2.8.1, v2.8.2).
- Vodeni žig „PRIMER – NIJE VAŽEĆI TOVARNI LIST“ / „SAMPLE – NOT A VALID CONSIGNMENT NOTE“, traka na vrhu, bez pečata i potpisa (NA, v2.8.1).
- r62 po poljima (62.1–62.4). Mase sa hiljadama odvojenim razmakom („703 000 kg“) (v2.8.2).

**Vrednosti u dopuni**
- **Vrednosti se ne izmišljaju.**
  - Brojevi propisanog oblika: šablon sa X („XXXXX-X“, „XX XXXXX-X“, „XX XX XXXX XXX-X“, „26RSXXXXXXXXXXXXX“, „XXXX“) (v2.8.2).
  - Naziv, adresa, platilac, primalac: „XXXXX, ulica i broj, poštanski broj mesto, XX“, „Platilac za deonicu SRB: XXXXX“ (v2.8.3).
  - Kontakt osoba: **„XXXX“**. ~~„Marko Marković“ / „John Smith“~~ je zamenjeno (v2.9.2 3.5).
  - Datum samo iz dokumenta, nikad datum maila („XXXX-XX-XX“, „MM-DD-XX“) (v2.8.2).
- **Kontrola pre slanja:** svaka reč koju PRIMER dodaje mora biti iz dokumenta, X šablon ili iz rečnika primera (RS, DE…, „Spisak kola“, kodovi dokumenata). Inače se dopuna ne upisuje, a izveštaj ide samo INTERNO (PRIMER-KONTROLA) (v2.9.2 3.5).
- Nepouzdano pročitana rubrika: bez dopune i bez sidra „posle …“. Dopuna ne sme da ima mešano pismo, „jj“ ni kratke nepoznate reči (v2.8.3, v2.8.4).

**Poravnanje i naziv fajla**
- **Poravnanje po strani** (v2.9.3 deo 2, v3.0):
  - obrazac se poravnava posebno za svaku stranu;
  - nijedna oznaka ni tekst ne sme da izađe van strane – ako izlazi, ponovi se poravnanje, a ako ni tada ne uspe, oznaka ide uz ivicu uz INTERNO „poravnanje strane N nepouzdano“;
  - markeri se numerišu od 1 na svakom tovarnom listu.
- Ime fajla: `PRIMER_<r62 bez razmaka>.pdf`, inače `PRIMER_<ime>.pdf`. Greška pri izradi ne zaustavlja slanje (NA).

## 8. Upis poslova (planiranje.tmp*)

**Baza i prava**
- Piše se **samo u `planiranje.tmp*`**.
  - Samo čitanje: šifarnici `dbo.stanice`, `dbo.Partnerji`, `dbo.NHM`, `planiranje.tmpNHM`, iz `dbo.Ponudba` samo `PonStPon` i `PonPart`.
  - Samo čitanje: `planiranje.OP_Jobs`, `dbo.Teretnica`, `dbo.TeretnicaStavke`, `dbo.Najava`.
  - **Ništa vezano za Najavu se ne upisuje** (UPIS, v2.9.1, v2.9.2).
- Izmene šeme tmp tabela smeju se izvršiti pod `sa`, ali samo nad `planiranje.tmp*`. Prava (`OP_913`) daje korisnik (v2.9.1).
- Tmp tabele služe samo za testiranje automatizacije. U `OP_Jobs` se ne piše dok tačnost nije dokazana i planer ne potvrdi (UPIS, ANALIZA).
- Upis je poseban korak posle provere. Greška upisa ne menja proveru ni izveštaj. Jedan mail = jedna transakcija (rollback, log, jedno interno obaveštenje na 6 h) (UPIS).
- Upis radi i kada CIM ima greške (ImaGreske) (ANALIZA, UPIS).
- SQL Server 2008 R2 (compat 80): bez JSON, STRING_AGG, TRY_CONVERT, IIF; `SET ARITHABORT ON` (UPIS).

**Posao = voz, grupe**
- **Posao = jedan voz.** Pun dolazak i prazan povratak su jedan posao (task 1 – prevoz punog, task 2 – predaja praznog; taskovi se sada ne prave) (v2.9.1, korisnik 16.11.).
- Grupe:
  - grupa 1 = natovarena kola (najviše kola);
  - grupa 2 = prazna kola (9922/CUV) ili druga roba;
  - više od dve → Napomena;
  - zaglavlje posla iz grupe 1 (v2.9.1 4.2).
- **NHM, mase i broj kola grupe samo iz dokumenata te grupe.** NHM grupe nije pročitan → NULL + kandidati, nikad iz druge grupe (v3.0 0a.3). Grupa bez kola sa masama grupe 1 se ne pravi (v2.9.2 3a.2).
- Prazna kola: grupa „prazna“ samo ako je NHM 9921/9922, dokument CUV ili piše prazna/leer/empty/üres. Inače su kola natovarena (v2.9.2 3a.2).
- Povratak praznih: prazna kola u suprotnom smeru postaju grupa 2 istog posla, zaglavlje se ne menja. Dva nezavisna načina vezivanja:
  1. ≥ 80 % istih kola u 30 dana (v2.9.1 4.3);
  2. **ista oznaka voza ili referenca (r8, naslov) + suprotan smer + prazna kola, i kada se kola ne poklapaju** – poreklo „povratak praznih po oznaci voza X, kola se ne poklapaju“ (v3.0 0a.2, Zmajevo SBGT26_SRB04).
- Listovi istog maila ili prepiske sa istom deonicom KP (Stanica1, Stanica3) su isti posao. Primer: ID 1602 i ID 1496 su dva posla (različita Stanica1) (v2.9.1 4.2).
- List bez obe tačke deonice ide u voz istog maila sa istom stanicom ili robom. CUV bez kola sa masama CIM-a istog maila je isti voz (v2.9.2).
- Konačni CIM ažurira posao iz drafta, ne pravi novi (LDC8, gips) (v2.9.2 3.6).

**Ključ posla i duplikati**
- `KljucPosiljke`:
  - r62 samo kada je ispravna;
  - inače `ref:<r8>|<OtpravnaID>|<UputnaID>`;
  - inače ključ lista – nikad OCR tekst (v2.9.1 5.8, v2.9.2 3a.1).
- Posao se, kada r62 nije pouzdana, traži po zbiru znakova (v2.9.2 3a.1):
  - znaci: „ID nnnn“ iz naslova ili ista prepiska, r8, ista Otpravna i Uputna, isti NHM, kola ≥ 50 % (kontrolna cifra UIC), mase ± 1 %, datum ± 5 dana;
  - dovoljno je: (ID ili r8) + relacija, ili kola ≥ 50 % + relacija;
  - dodatno (implementacija 2.9.2): r62 ista osim oznake zemlje; ista prepiska + NHM + mase; ista ponuda + NHM + broj kola;
  - različite r8 (Coal 16/17) nisu isti voz.
- Kopija istog lista (ista kola ≥ 80 % ili iste mase, i jedna masa uz istu grupu i broj kola) zamenjuje raniji zapis; mase se ne sabiraju (v2.9.1, v2.9.2).

**Mase u poslu**
- Tone sa 3 decimale; nije navedeno = 0. Sve što postoji se upisuje (neto, tara, bruto). Dve od tri → treća se računa („izračunato“) (v2.9.1 odluka 4, 5.1).
- **Zbirovi se računaju u kilogramima kao celi brojevi**, pa se tek na kraju dele sa 1000 (SAD-HER 80: 1 361,640 / 694,940) (v3.0 0a.4).
- r25 bez oznaka = masa robe = **Neto** (GLV-CIM). Vrednost označena kao Tara nikad ne ide u Bruto („Tare: 643.420“ → Tara 643,420, Neto 0, Bruto 0). Šablon „x.xxx“ = nije navedeno. „1.848,150“ (hiljade + 3 decimale) uz oznaku = tone (v2.9.1, v2.9.2).
- **Spisak kola ima prednost** nad CIM-om (zbir; red „Ukupno“ ± 0,5 %). Razlika r25 ↔ spisak > 1 % → napomena (v2.9.1 odluka 4).
  - **Izuzetak (v3.0 0a.1, LDC8)**: spisak sa **manje kola** od tovarnog lista, ili sa kolima kojih nema u tovarnom listu, ne menja broj kola ni mase posla. Stavke se čuvaju kao posebna verzija, uz INTERNO „spisak (N kola) ≠ tovarni list (M kola) – proveriti“ i oznaku u Excel-u/ANALIZA.md. Spisak sa istim skupom kola i dalje ima prednost.
- Spisak samo sa tarom (kola se tek tovare – Coal 17) → Tara = zbir, Neto 0, Bruto 0 (v2.9.1).
- Kolone spiska:
  - mapiranje po zaglavlju;
  - dve kolone sa istim zbirom kod natovarenih kola, ili masa spiska jednaka drugoj masi iz tovarnog lista → ta vrednost se ne upisuje + „proveriti kolone spiska“;
  - nelogične mase (bruto < neto, tara = neto) se ne upisuju (v2.9.1, v2.9.2 3a.2).
- Natovaren spisak ne ide uz posao ni teretnicu praznih istih kola (exLDC7 → LDC8) (v2.9.2).
- Spisak kome fali tara za neka kola → napomena „nedostaje tara: kola …“ (v2.9.2 3.3).

**NHM**
- NHM se čita iz `planiranje.tmpNHM` (tarifa Srbija Kargo, Deo 2), redom:
  1. 8 cifara → Kod8;
  2. jedan red sa istim Kod6 → taj red;
  3. inače grupa Kod4;
  4. inače NULL + kandidati.
- Bez šifre: po nazivu (NazivNorm, sinonimi tip NHM). Strani nazivi idu preko sinonima posle potvrde.
- Intermodal: NHM1 = najčešća grupa po jedinicama, ostali u Napomenu. VRNP stavke = 4 cifre.

(v2.9.1 odluka 2, 3.1; zamenjuje „najmanji ID“ iz UPIS-a)

**Stanice**
- Po nazivu (normalizacija, OCR tolerancija), pa po kodu iz r62/r12/r16/spiska. Šifra sa kosom crtom („72/12601“) se normalizuje (v2.9.1 5.4, v2.9.2 3a.5).
- Duplikati u šifarniku: stanica koja se najviše koristi u OP_Jobs/TeretnicaStavke. Ista šifra kod više stanica: pravilo upotrebe, a ako nijedna nije korišćena → NULL + kandidati (korisnik 16.11.).
- Stanica1 (ulaz u Srbiju) / Stanica3 (izlaz):
  - prednost ima „gr.“ stanica; voz koji polazi iz Srbije → Stanica1 = otpravna; voz koji se završava u Srbiji → Stanica3 = uputna (UPIS);
  - deonica KP iz šifara u r57 („3212 KP 72 0711 210013“) (korisnik 16.11.);
  - Stanica1 i Stanica3 se ne pogađaju: ista stanica na oba kraja → NULL + kandidati (korisnik 16.11.).
- Stanica2 = utovarno mesto („Beograd Ranžirna (Ostružnica)“ → 240) (odluka 14.11., v2.9.1).
- Granični prelazi (`tmpSinonimi` tip GRANICA, OP_914; samo po zemlji pouzdanost 60):

  | Prelaz / oznaka | Stanica | Izvor |
  |---|---|---|
  | Kelebia / 0711 / HU | 328 Subotica gr. | v2.9.1 |
  | Tovarnik | 381 Šid gr. | v2.9.1 |
  | Stamora Moravita / RO | 93 Vršac gr. | v2.9.1 |
  | Dragoman, Kalotina, Kapikule, Svilengrad / BG | 118 Dimitrovgrad gr. | v2.9.1 |
  | Tabanovci / MK | 273 Preševo gr. | v2.9.1 |
  | BA / Zvornik | 417 Brasina gr. | korisnik 16.11. |
  | MNE / Bijelo Polje | 1365 Prijepolje gr. | korisnik 16.11. |
  | Erdut / Dalj | 51 Bogojevo gr. | korisnik 16.11. |

  - **HR samo po zemlji se ne pogađa** → NULL + kandidati (korisnik 16.11.).
- Šifarnik stanica se ne menja. SRB kodovi bez šifre idu u `sifarnici\predlog_kodova_stanica.csv`. Stanica 404 Pančevo Varoš nema Kod 210013 i ostaje primer „stanica nije nađena“ (korisnik 16.11., Drenik).

**Partneri, prevoznici, platilac**
- Partner po nazivu (PaNaziv, PaDoNaziv; bez pravnih oblika, zagrada, grada; nazivi zemalja samo ako ostanu 2+ reči). Prag 0,85. Ispod praga → NULL + 3 kandidata. **Pogrešan partner je gori od praznog** (UPIS, v2.9.1).
- Prevoznik redom:
  1. tačan UIC među Prevoznik = 1;
  2. tačan UIC među svima (napomena);
  3. skraćenica (CRW, RCH…) kao posebna reč – samo kada je naziv samo skraćenica ili kao rezervni put; kandidat sa drugim UIC-om nije pogodak; bez UIC-a u šifarniku pouzdanost ≤ 70 + „proveriti“;
  4. naziv.

  (v2.9.1 5.3, korisnik 16.11.)
- Bliski kandidati (razlika < 0,05):
  - prvo pravna forma kao znak zemlje: s.r.o./a.s. → SK/CZ; d.o.o. → SRB/HR/SI/BA; Kft./Zrt. → HU; GmbH → DE/AT; Sp. z o.o./S.A. → PL; S.R.L. → RO/IT; A.Ş. → TR;
  - zatim zemlja iz adrese ili uloga;
  - inače NULL + kandidati.

  (v2.9.2 3a.3, korisnik 17.11.)
- **Sinonimi** (`tmpSinonimi`, tip PARTNER) imaju prednost; najduži sinonim ima prednost; pravna forma ostaje u sinonimu:

  | Naziv u dokumentu | PaSifra | Izvor |
  |---|---|---|
  | Panšped | 1969 | korisnik 14.11. |
  | CINKOTURK LOJISTIK | 2531 | korisnik 16.11. |
  | Budamar South s.r.o. | 3355 | korisnik 17.11. |
  | Budamar South (bez pravne forme) | 3143 | korisnik 17.11. |
  | Budamar Logistics (a.s.) | 1580 | korisnik 17.11. |
  | samo Budamar | 3143 | korisnik 17.11. |

- PlatilacID: platilac za deonicu KP iz r7 ili preko ponude (ponuda ima prednost). Nikad iz najave ili ugovora (odluka 14.11.).
- PreuzimaOd / PredajeOperateru = susedni prevoznici. Kada voz polazi ili se završava u Srbiji:
  - polazi iz Srbije, KP nije u r57 → PredajeOperateru = prvi prevoznik susedne deonice;
  - završava se u Srbiji → PreuzimaOd = poslednji susedni prevoznik, pa prevoznik sa spiska, pa pošiljalac maila (Prevoznik = 1), pouzdanost ≤ 60.

  (v2.9.1 5.3)
- `PredajePartneruID` = primalac iz r4 kada je KP poslednji (v2.9.1 odluka 5). Simetrična kolona se ne pravi.
- Dnevna interna lista „nije nađen u Partnerji / nema UIC / nema oznaku Prevoznik“ – šifarnik dopunjuje korisnik (v2.9.1).

**Teretnica i stavke**
- Iz spiska nastaju teretnica i stavke (jedna stavka = jedna kola) (UPIS):
  - broj kola „33839338201-9“;
  - tara i neto u t, dužina u m (kao `dbo.TeretnicaStavke`);
  - VRNP = NHM sa 4 cifre;
  - Dokument = broj otpravljanja iz r62.
- Spisak pre tovarnog lista: teretnica bez posla, vezuje se kada list stigne (UPIS).
- Nova verzija spiska (isti ključ ili ista kola): dodata, uklonjena i izmenjena kola u istoriju. Novija verzija ima prednost (osim izuzetka 0a.1). Isto stanje kola (prazna/natovarena) je uslov da se koristi ista teretnica (v2.9.2).
- CIT 7 povezan sa poslom (r62, kola, dosije) (v2.9.1 5.6):
  - Box 25 → neto (spisak ima prednost);
  - „wagon … detached“ → Otkacena = 1, samo za proverenu kontrolnu cifru i ne sa skena;
  - Box 4/10/12 → primalac i uputna;
  - istorija + jedan red u Napomeni.

**Napomena, istorija, poreklo**
- `tmpJobs.Napomena`:
  - tekst maila (zaglavlje „datum pošiljalac: naslov“ + telo bez citata, potpisa, odricanja, linkova i telefona);
  - svaki mail tačno jednom, najnoviji gore, najviše 2000 znakova.

  (UPIS, v2.9.1 5.8)
- Referenca (r8) i BrojPonude (r14) samo kada liče na oznaku (v2.9.1 5.8).
- `tmpJobsPolje`: poreklo svake vrednosti (rubrika, tekst, dokument, pouzdanost, kandidati). Polja sa RucnoIzmenjeno = 1 se ne menjaju. `tmpJobsIstorija`: svaka promena, ništa se ne briše (UPIS).
- `tmpDokument`:
  - svaki prilog, isti heš = isti red;
  - ponovni prijem **ne prepisuje JobID** (v2.9.2 3a.7);
  - sopstveni PRIMER se ne upisuje.
- Forme `frmTmpJobs` i `frmTmpTeretnica` su samo za prikaz: nazivi umesto ID-eva, poreklo, istorija (UPIS, v2.9.1).

**Tekst maila (od v3.0, faza 1)**
- Iz novog dela poruke (bez citata i potpisa) izvlače se događaji (ETA, ETD, DOLAZAK, POLAZAK, KASNJENJE, OTKACENA_KOLA, BROJ_KOLA, ID_NAJAVE, BROJ_VOZA, MRN, OSTALO_VAZNO) u `planiranje.tmpJobsDogadjaj`.
- Polja posla se iz teksta **ne menjaju** i ništa se ne šalje; ide samo jedan red u Napomenu i istorija. Ažuriranje posla iz događaja je faza 2, posle potvrde tačnosti (v3.0 2.10).

## 9. Platforma, keš, alati

**Platforma i keš**
- Platforma ostaje **32-bitna**. 64-bitni Tesseract lošije čita neke skenove (Drenik). x64 se uvodi tek kada na celom regresionom skupu da identične rezultate (korisnik 17.11., v3.0).
- Obrada u delovima od po 20 mailova u posebnim procesima, i za agenta i za alat (v2.9.1, v3.0).
- Keš strana i rezultata mora da sačuva sve što obrada koristi. Obrada iz keša i bez keša daje **identičan** rezultat (test na celom regresionom skupu) (v3.0 0a.5).
- Verzija čitača se podiže kada se promeni čitanje (keš se obnavlja).

**Agent i CimRadionica (od v3.0)**
- Jedno jezgro (`Saobracaj.Agent.CimProvera.Jezgro`) i dve ljuske: agent i CimRadionica. Nijedno pravilo, parser ni generator ne postoji u dve kopije.
- Agent se ne pokreće dok korisnik ne odluči; zakazani zadatak i prosleđivanje gasi korisnik.
- CimRadionica:
  - nikad ne šalje mail (bez SMTP-a u svim režimima);
  - odgovor priprema kao da je Live;
  - stanje drži po radnom prostoru i nikad ne dira `stanje\posiljke.json` agenta;
  - upisuje samo u tmp (online), ili u `rezultat.json` (offline);
  - paket za analizu ne sadrži lozinke, DPAPI ni connection string.

## 10. Tumačenja u implementaciji v3.0 (17.–18.11.)

Ovo su rešenja koja promptovi nisu izričito odredili. Svako se može promeniti odlukom korisnika.

**Upis poslova**
- „EMPTY“ uz kontejner ili prikolicu u redu kola opisuje praznu tovarnu jedinicu, ne prazna kola. Takva kola ostaju u grupi natovarenih (SAD-HER 80: 19 kola u jednoj grupi).
- Spisak sa „empty wagons“ ili šifrom 9921/9922 u zaglavlju, bez NHM-a i mase robe po kolima, je spisak praznih kola (NHM 9922) – Zmajevo – Kelebia.
- Spisak priložen uz natovaren tovarni list u istom mailu je spisak natovarenih kola i kada ima samo taru. Ne postaje nova verzija spiska praznih istih kola (Zmajevo soy, UID 660).
- Oznaka voza sa donjom crtom („SBGT26_SRB04“) je ispravna referenca (r8).
- Povratak praznih: poreklo grupe 2 je „povratak praznih po oznaci voza X, kola se ne poklapaju“, odnosno „…, N od M kola ista“.
- Spisak sa jednom grupom ne briše grupu 2 posla kada posao već ima teretnicu grupe 2 (povratak praznih). Primer: Zmajevo, UID 763, „Wagon list CIM.pdf“.
- NHM za kola spiska bez sopstvenog NHM-a uzima se iz grupe tovarnog lista sa istim kolima, a ne iz NHM1 posla. Kada se kola ne poklapaju ni sa jednom grupom, NHM ostaje NULL.
- Ako spisak ima samo zaglavlje „NHM KODU 392390000000“ (12 cifara), koristi se prvih 6 cifara.
- Spisak sa manje kola ili tuđim kolima (0a.1):
  - teretnica ostaje kao zapis;
  - posao se ne menja;
  - INTERNO napomena ide u `IshodUpisa.Interno`, u Test režimu u kratku internu poruku, u Excel i u ANALIZA.md.
- Turske kolone spiska: V.DARA = tara kola, NET TON = neto, TOPLAM = bruto, SIRA = redni broj.

**Pravila i PRIMER**
- r58a: naziv izdavaoca se uzima samo iz samog dokumenta (r57, r13, r7). Oznaka mreže („BDŽ:“, „SRB:“) nije naziv. Bez pouzdanog naziva predlog je samo šifra („3057“).
- RID: UN broj na spisku (kolona ili Kemler/UN po kolima) računa se i kada r21 ne upućuje na spisak. Različita opasna roba = više UN brojeva u r21 ili različit NHM natovarenih kola na spisku.
- PRIMER:
  - zajednički nalazi („Svi tovarni listovi“) dobijaju brojeve 1…s, a nalazi svakog lista nastavljaju od s+1; tabela maila i krugovi koriste iste brojeve;
  - obrazac čiji okviri izlaze više od 1,5 % van strane zamenjuje se sledećim pouzdanim obrascem;
  - ako takvog nema, okviri se uklapaju u stranu, uz INTERNO „poravnanje strane N nepouzdano“.

**Događaji (faza 1)**
- Kratka rečenica (manje od 4 reči) dobija jezik celog novog dela poruke.
- Vremenska zona redom: izričito u tekstu, pa domen pošiljaoca (.ro/.bg/.ua/.md/.gr = EET, .tr = TRT), pa jezik rečenice, inače CET.
- Samo dan i deo dana („18/morning“) znači mesec maila u 08:00.

**CimRadionica**
- Odgovor se priprema kao u Live režimu: KopijaNa = priprema@kprevoz.co.rs, UpozorenjaNa = agenti@kprevoz.co.rs, transport je prazan.
- Rezultati se čuvaju po verzijama. Nova verzija za poređenje obrađuje se „ispočetka“ (dosije i ponavljanja radnog prostora se brišu, keš ostaje).
- Keš čitanja je zajednički za sve radne prostore (`C:\CimRadionica\kes`). Kopija šifarnika je zajednička (`C:\CimRadionica\sifarnici`).

**Arhiva**
- Arhiva se puni samo dopisivanjem; fajlovi su samo za čitanje. Posle promene UIDVALIDITY nove poruke idu u `<folder>@<uidvalidity>`.
- Arhiviranje se pokreće fajlom `arhiviranje.zahtev` („--od-uid 191“) i zakazanim zadatkom pod svc nalogom. Posle arhiviranja agent izlazi bez obrade pošte.
- Agent u redovnom radu upisuje svaku novu poruku i u arhivu (`ArhivaFolder`, podrazumevano `arhiva` pored exe-a).
- Promena UIDVALIDITY šalje INTERNO upozorenje (najviše jednom dnevno).
