# CimProvera – izmene programa

## 2026-11-20 – v3.0.5: priprema Test režima na serveru (obrada pošte ostaje ISKLJUČENA)

Odluke su u `docs/ODLUKE.md`, odeljak 15.

- **Server:** 20.11. u 00:28 postavljen je `Jezgro.dll` 3.0.4 (merilo pečata, ODLUKE 14). Rezervna kopija je `backup-pre-3.0.4-20261120-0028`.
- **Prekidač** (`Mail\Prekidac.cs`) radi pri svakom slanju i na početku svakog prolaza:
  - Test → isključivo agenti@;
  - Live → samo uz `OdobreniPosiljaoci`;
  - sve ostalo → ništa se ne šalje (INTERNO: log, `stanje\neposlato`, dnevnik).

  `Rezim` se čita iz config-a pri svakom slanju. Fajl `SLANJE_ZAUSTAVLJENO` služi za hitno isključivanje.
- **Nulta tačka iz arhive:** komanda `nulta-tacka-arhiva [--suvo]` postavlja poslednji UID pokriven arhivom, bez IMAP-a i bez SMTP-a.
- **Dnevni sažetak** ide na agenti@ jednom dnevno (`DnevniSazetakSat`, podrazumevano 7), iz `stanje\dnevnik\*.jsonl`.
- **Skripte:**
  - `Ukljuci-TestRezim.ps1 [-Suvo]`: provera → arhiviranje → nulta tačka → uključivanje zadatka;
  - `Iskljuci-Hitno.ps1`;
  - `Instaliraj.ps1 -ObradaIskljucena -SaSyncfusion`: Syncfusion ključ bez uključivanja obrade.
- **CimRadionica:** `--testiranje [--neuspele]`, a kartica Testiranje posle svake obrade pravi `_Paket\Claude\Claude_Testiranje_<datum>.zip`.
- **Novi ključevi config-a** (svi neobavezni): `OdobreniPosiljaoci`, `DnevniSazetakSat`. Config servera se ne menja.

## 2026-11-19 – v3.0.3 (postavljeno na server; obrada pošte ostaje ISKLJUČENA)

Server prelazi sa 2.9.2 na 3.0.3. Sve odluke su u `docs/ODLUKE.md` (odeljci 10–13), a ovde je kratak pregled.

- **3.0** – CimRadionica (alat za ponovnu obradu), arhiva sandučeta, događaji iz teksta maila (OP_918).
- **3.0.1** – R16, R1/R4 i ISO zemlje; „nema dokaza – nema tvrdnje“ (mera čitljivosti polja); samoprovera pre slanja; upis poslova (kopije, spiskovi, provera smisla mase).
- **3.0.2** – kućice sa X sa slike, vrsta dokumenta CIM/CUV; KP3, KP57 i KP uz OCR toleranciju; R29/R62 kod radnih verzija; CIT 18; mase.
- **3.0.3** – kućica CIM (R30 samo INTERNO, kada su obe kućice pouzdano prazne); CUV uz prethodni tovar; CIT 18 (mase i original po ponudi); zamenjena teretnica; kolona `Izvor` u tmp tabelama (OP_919, već pokrenut); `arhiviraj` bez SMTP-a i sa izveštajem o sandučetu; `Instaliraj.ps1 -ObradaIskljucena` pravi zadatak arhiviranja (07:10, 15:10) pod svc_cimprovera.
- **Rezervna kopija** 2.9.2 je u `backup-pre-3.0.3-20261119-1357`.
- **Pravila** su sada iz 3.0.3 (do sada su bila 2.8.4).
- **Config servera nije menjan.** Novi ključevi imaju podrazumevane vrednosti.


## 2026-11-17 – v2.9.2: platilac preko broja ponude, KP u r57, ispravke posle testova 16.–17.11. i ponovne obrade od UID 191 (pravila ostaju 2.8.4)

- **Platilac preko broja ponude** (`Baza\Ponude.cs`, `Provere.PonudaZaList`): broj ponude iz r7, pa r13, pa r14 („Ponuda 8957“, „Ponuda8957“, „Ponuda br.“, „ponuda:“, „KP Ponuda“, „po ponudi 8926“, „prema ponudi“, „Offer“, „offer nr 5856“, „according offer nr“, „acc. to offer no.“, „Angebot (Nr.)“, „Ajánlat“; najbliži oznaci SRB / KP / 3212). Samo ceo broj (1–6 cifara) se traži parametrizovanim upitom nad `dbo.Ponudba` (samo `PonStPon`, `PonPart`) i `dbo.Partnerji`; važenje se ne proverava. Nađena ponuda: nema KP7-NIJE-PLATILAC / KP7-PLATILAC / KP7-NEJASNO, r7 se ne označava u PRIMER-u, `PlatilacID = PonPart`, `BrojPonude`, poreklo „ponuda 8957 → PonPart naziv“ (100). Nije nađena: greška kao do sada + „Ponuda 8957 nije pronađena u sistemu KP“. Firma X u r7 i ponuda drugog partnera Y: samo INTERNO „r7 navodi X, ponuda 8957 glasi na Y“, upisuje se Y. Baza nedostupna: pošiljaocu ništa, interno „ponuda nije proverena – baza nedostupna“, mail se ponavlja u sledećem prolazu (posle poslednjeg pokušaja – samo interno). `OP_913`: `GRANT SELECT ON dbo.Ponudba` (pokreće korisnik); ponovna obrada čeka to pravo.
- **KP u r57** (`Provere.KP57`, `CimSablon.PrelivanjeR57`): KP u bilo kom obliku („3212“, „3212 KP“, „KP 3212“, „RS: 3212 KP“, „SRB: KP UIC 3212“, „3212 Kombinovani Prevoz“, „Kombinovani prevoz d.o.o.“). Naziv i UIC – ispravno; samo naziv – upozorenje „dopisati UIC 3212“; samo UIC – upozorenje „dopisati naziv“; ni jedno – greška. Dopuna „3212 KP“ u PRIMER-u samo u novom redu, uz srpsku deonicu, nikad pored strane deonice. Redovi r57 koji na skenu prelaze donju liniju rubrike („CIM - overen.pdf“: „5059 Fortrack Rail Kft“, „3212 Kombinovani Prevoz – Subotica br. – Bor Teretna“) pripadaju r57. Sken bez KP u r57/r58a: isečak rubrike se ponovo čita (400 dpi; r57 i kolona prevozilaca velikim slovima i ciframa) i prihvata samo kada nađe KP; srpska deonica u r57 sa ostatkom šifre („32 … Ostružnica – Subotica br.“) ili KP 3212 u r13 → samo interna napomena „KP nije pouzdano pročitan – proveriti ručno“, ne greška; pečat preko r58 bez čitljive šifre i naziva prevoznika → upozorenje. Preporuka za r14 („upišite broj ponude“) se ne daje kada je broj ponude u r7/r13.
- **Klasifikacija** (`LokalniCitac.TipStraneJezgro`, `Raspakivanje.SopstveniPrimer`): „Wagen Liste / Wagenliste / Lista de vagoane / CIT 23“ u naslovu je spisak kola i kada naslov pominje Frachtbrief (CIT 23 Zmajevo-CR.pdf); bez naslova obrasca CIM se prepoznaje po mreži rubrika (poravnat šablon), ne po rečima (opšti uslovi Imerys); sopstveni PDF primer (Title „Primer ispravnog tovarnog lista“ / Producer CimProvera, i pod drugim imenom) se preskače – ne proverava se i ne upisuje. „Liči na obrazac“ (interna ručna provera) samo uz natpis obrasca, mrežu rubrika ili 40+ natpisa. Ime fajla „CIM …“ i sadržaj tovarnog lista (platilac, „CIM 6“, 13.3, CIT 23, tara) → tovarni list, ne faktura („CIM za voz br. 262 ….pdf“). Verzija čitača L21 (keš OCR-a se obnavlja).
- **Interna poruka „nije pročitano“** (`Obrada.MozdaCim`): samo kada strana liči na CIM/CUV ili naslov/tekst maila pominje CIM, CUV, tovarni list, teretnicu ili Frachtbrief; inače samo log i tmpDokument.
- **Isti CIM sa novim prilogom**: pošiljaocu ništa; nova verzija spiska ažurira posao (istorija); u režimu Test kratka interna poruka „[INTERNO] Ažuriran posao“ sa izmenama. Spisak kome fali tara za neka kola → napomena „nedostaje tara: kola …“.
- **Zemlja u r1/r4** iz adrese: poljski poštanski broj „41-800“, PDV broj sa oznakom zemlje („PL6292256576“), gradovi (Zabrze, Gliwice, Žilina…); ne iz r62 ni prevoznika.
- **Bez izmišljenih imena u PRIMER-u**: kontakt „XXXX“ umesto „Marko Marković“ / „John Smith“; kontrola pre slanja (`Dopune.NepoznateReci`) – svaka reč dopune mora biti iz dokumenta, X šablon ili iz rečnika primera; inače se dopuna ne upisuje, a izveštaj ide samo INTERNO (PRIMER-KONTROLA).
- **Duplikati istog voza** (`PosaoIzDokumenata.R62Ispravna`, `UpisUBazu.PosaoPoZnacima`): r62 je ključ samo kada je ispravna (važeći UIC kod zemlje, kontrolna cifra stanice – Luhn); inače posao po zbiru znakova: ID iz naslova ili r8 + relacija, ili kola ≥ 50 % (ispravna kontrolna cifra) + relacija, ili r62 ista osim oznake zemlje; ista prepiska + relacija + NHM + mase ± 1 % (sken bez kola i r8); datum ± 5 dana; različite r8 (Coal 16 / Coal 17) i dve ispravne različite r62 bez ID-a/r8 nisu isti voz. Relacija toleriše nepouzdano pročitanu otpravnu (OCR šum u r16).
- **Mase**: masa sa spiska jednaka drugoj masi iz tovarnog lista (Coal 16, Prahovo 05) ili dve kolone spiska sa istim zbirom (natovarena kola) se ne upisuju – „proveriti kolone spiska“; natovaren spisak se ne pripaja teretnici/poslu praznih istih kola (exLDC7 prazan → LDC8 natovaren); grupa bez kola sa masama grupe 1 se ne pravi (005NISBAR26).
- **Platilac – bliski kandidati**: pravna forma kao znak zemlje (s.r.o./a.s. → SK/CZ; d.o.o. → SRB/HR/SI/BA; Kft./Zrt. → HU; GmbH → DE/AT; Sp. z o.o./S.A. → PL; S.R.L. → RO/IT; A.Ş. → TR), pa zemlja iz dokumenta; sinonim partnera je ceo sadržan u nazivu (pravna forma ostaje), najduži ima prednost. `OP_914`: Budamar South s.r.o. → 3355, Budamar South → 3143, Budamar Logistics → 1580, Budamar → 3143.
- **Šifra stanice sa kosom crtom** („72/12601“) se normalizuje pre traženja po `Kod`.
- **tmpDokument**: ponovo primljen fajl ne prepisuje `JobID` (raniji posao ostaje vezan za dokument).
- **x64 build** (`bin\Release-x64`, na serveru `rezerva-x64-2.9.2`): radi i obrađuje 230 mailova u jednom procesu bez nedostatka memorije, ali 64-bitni Tesseract na nekim skenovima čita drugačije (Drenik UID 533: 15 umesto 201 natpisa, gube se otpravna i uputna) – glavni ostaje 32-bitni build sa obradom u delovima od po 20 mailova.
- **Posle probe**: kopije skena sa jednom masom (Prahovo 05) su isti list (mase se ne sabiraju); ključ spiska „kola:“ razlikuje prazna i natovarena ista kola; isti posao i po istoj ponudi + NHM + broju kola (Bor ID 1657); CUV bez kola sa masama CIM-a istog maila je isti voz (005NISBAR26); list bez obe tačke deonice ide u voz istog maila sa istom stanicom ili robom („12 е въглища.pdf“); ponovo pročitana r7 se ne gubi pri ponovnom čitanju r57/r58a. Masa „1.848,150“ (hiljade + tri decimale) su tone; natovaren spisak ne ide uz posao praznih kola ni iz dosijea; teretnica istog spiska prelazi na posao čiji je tovarni list u istom mailu (LDC8).

## 2026-11-16 – v2.9.1: ispravke upisa poslova (tmp tabele) posle testova 15. i 16.11. (pravila ostaju 2.8.4)

- **Šema** (`OP_916_tmp_Izmene.sql`; u zadatku „OP_914_tmp_Izmene“ – broj 914 je zauzet sinonimima): `tmpJobs.PredajePartneruID`, `Tara1`/`Tara2` (t, NOT NULL 0), `tmpTeretnica.Grupa` (1 = natovarena kola, 2 = prazna / druga roba), nova `planiranje.tmpNHM`, tip `GRANICA` u `tmpSinonimi`, razlozi istorije `POVRATAK_PRAZNIH` i `PONOVLJEN_MAIL`. `OP_911` briše i `tmpNHM`, `OP_913` daje SELECT na `tmpNHM` (pokreće korisnik).
- **NHM iz tarife** (`SQL\NHM\uvoz_nhm_tarife.py` → `OP_917_tmp_NHM_Podaci.sql`, 12 406 redova): NHM1/NHM2 čuvaju `tmpNHM.ID`. Šifra (r24, r21, spisak): 8 cifara → tačan Kod8; Kod6 sa tačno jednim redom → taj red; inače red grupe Kod4; bez šifre – naziv robe (NazivNorm, sinonimi tip NHM). Intermodalni voz: NHM1 = grupa najčešća po tovarnim jedinicama, ostale šifre u napomenu. VRNP stavki = 4 cifre.
- **Jedan voz = jedan posao** (`Baza\UpisVoza.cs`): kopije istog lista (ista r62; bez pouzdane r62 – ista kola (≥ 80 %) ili ista otpravna, uputna i mase) su jedan tovarni list, polje iz najbolje pročitane kopije; listovi istog maila sa istom deonicom KP (Stanica1, Stanica3) su jedan posao (ID 1602 + 1496 → dva posla po 12 kola); drugi list iz istog dosijea i iste deonice se dodaje poslu; grupe posla iz zapisa tovarnih listova (`tmpJobsPolje`, Kolona „TL:…“): grupa 1 = natovarena kola, grupa 2 = prazna kola (CUV / 9922) ili druga roba, više od dve → Napomena; povratak praznih kola (≥ 80 % istih kola, suprotan smer, 30 dana) → grupa 2 istog posla, zaglavlje se ne menja. Ključ posla bez pouzdane r62: `ref:<r8>|<OtpravnaID>|<UputnaID>` (jedinstven, „#2“ za drugi voz).
- **Provera jednom po tovarnom listu** (`Motor.Proveri`): kopije sa istom pouzdanom r62 (original, faktura, list prijema…) se proveravaju i prijavljuju jednom („WAGON 1 stampa.pdf (+4 kopije)“); kopije ostaju za upis (`RezultatProvere.KopijeListova`).
- **Mase** (`PosaoIzDokumenata.Mase`, `IzSpiska`): oznake Neto/Netto/Ntto/Ladung/Wg.Ladung/Masa incărcată/Rakomány tömege, Tara/Tare/Dara/Tara vg./Tara vagona, Bruto/Brutto/Btto/Gesamt/Ukupna težina/Masa brută/Elegytömeg; r25 bez oznaka = masa robe → neto; tara nikad u bruto; dve od tri → treća („izračunato“); nije navedeno → 0. Spisak kola ima prednost: zbir sa spiska (samo mase koje spisak ima), red „Ukupno“ (± 0,5 %, inače „Ukupno“ i ručna provera), prazna kola sa jednom masom → tara; razlika neto r25 ↔ spisak > 1 % → napomena u poreklu i istoriji.
- **Spiskovi** (`ExcelCitac`): kolone kola imaju prednost nad kolonama tovarnih jedinica (LKW „Specification“: Tara vagona / Wg.Ladung / Ukupna težina), zaglavlje u dva reda (CER „Súly (t) – Saját / Rakomány / Elegy“), CIT 23 po numerisanim kolonama (6 neto, 7 tara, 8 bruto) i po šablonu neto + tara = bruto (OCR pomera kolone – Šabac GIPS.pdf str. 7), red „Ukupno“, dužina voza, zbirovi i prelazni put iz zaglavlja (CER „Vonat hossza“, „Érkezési állomás“; PSP „Itinerariu“; CIT 23 „Prevozni put“), prevoznik sa spiska („CER Hungary Zrt. UIC: 3085“).
- **Partneri i prevoznici** (`Poklapanje.Partner`): tačan UIC među Prevoznik = 1, pa među svim partnerima (napomena), pa skraćenica iz r57/r13 kao posebna reč naziva (CRW → „CENTRAL RAILWAYS A.S. CRW“, pouzdanost 90; više kandidata → NULL), pa naziv; bliski kandidati – bira se samo kada jedan odgovara zemlji iz dokumenta ili oznaci uloge. Nazivi zemalja i „Joint stock company“/„J.P.“ ne ulaze u poređenje. KP nije u r57: polazi iz Srbije → PredajeOperateru = prvi prevoznik susedne deonice; završava se u Srbiji → PreuzimaOd = poslednji prevoznik susedne deonice, pa prevoznik sa spiska, pa pošiljalac maila (Prevoznik = 1), pouzdanost ≤ 60. `PredajePartneruID` = primalac iz r4 kada je KP poslednji. Dnevna interna lista „nije nađen u Partnerji / nema UIC / nema oznaku Prevoznik“ (stanje\za_sifarnik.txt, najviše jednom dnevno).
- **Platilac** (`Provere.AnalizirajR7`): „for KP firma“, „za KP firma“, „für KP firma“, „KP: firma“, „KP-CINKOTURK LOJISTIK“ → firma plaća deonicu KP (lažna greška KP7-NIJE-PLATILAC, Draft CIMa i CIT23, UID 531); novi red r7 je nov zapis (prevoznici „RS: KP 3212“ u drugom redu nisu platilac). „SRB: KP UIC 3212, KP“ ostaje greška. Broj ponude iz r7 („KP Ponuda 8957“).
- **Stanice**: šifra stanice iz r62 kada je r16 nema (LDC8 „72 165167“ → 380 Šid), ceo tekst r16 potvrđuje šifru (Kwidzyn); granični prelaz iz `tmpSinonimi` (GRANICA: Kelebia/0711/HU → 328, Tovarnik/HR → 381, Stamora/RO → 93, Kapikule/Svilengrad/Dragoman/Kalotina/BG → 118, Tabanovci/MK → 273; samo po zemlji – pouzdanost 60), iz spiska kada posao nema pouzdan prelaz; Stanica2 i iz zagrade („Beograd Ranžirna (Ostružnica)“ → 240). SRB kodovi bez šifre u šifarniku → `sifarnici\predlog_kodova_stanica.csv` (šifarnik se ne menja).
- **PDF** (`PdfStrane`): `ParsingOptions { UseLenientParsing, SkipMissingFonts }` – „CIM WAGON … stampa.pdf“ (font /Helvetica) se čitaju (strana bez teksta → OCR).
- **CIT 7** (`Cit7Citac`, `Cit7Provera`, `UpisUBazu.PrimeniCit7`): „Correction: Box 25: …“; potpis/pečat prevoznika ili ispravka → bez pravila o pravu raspolaganja; otkačivanje kola i „+ 17 wagons“ nisu deljenje pošiljke; posao po ključu lista, r62, kolima ili dosijeu: Box 25 → neto (spisak ima prednost – razlika u istoriju), „wagon … detached“ → `Otkacena = 1` samo za kola sa spiska sa ispravnom kontrolnom cifrom i samo kada CIT 7 nije sken (OCR može da pogreši cifru – tada napomena „proveriti“), Box 4/10/12 → primalac / uputna (PredajePartneru); istorija (izvor CIT 7, datum) i jedan red u Napomeni.
- **Ponovljeni mail**: odgovor se i dalje ne šalje, ali se prijem beleži (`tmpDokument.BrojPrijema`, `PoslednjePrimljeno`) i tekst maila ide u Napomenu posla (istorija `PONOVLJEN_MAIL`).
- **Napomena, Referenca, BrojPonude**: svaki mail tačno jednom (po zaglavlju „datum pošiljalac: naslov“), i kada se posao ne menja; bez potpisa (posle pozdrava ostaje ime), pravnih upozorenja, linkova i telefona. r8/r14 samo kada liče na oznaku („il 1 42-0003-26“ → „42-0003-26“; „©j 520 o 0“ → NULL).
- **Ponovna obrada** (`ponovna-obrada --od-uid N`): mailovi iz sandučeta (samo čitanje) od UID-a N kao `validate --upis` sa privremenim stanjem – ne šalje se nijedan mail, `stanje\posiljke.json` i ostalo stanje agenta se ne menjaju. Lokalne probe: `validate … --upis --prefiks planiranje.tmpProba` (kopije tmp tabela). Mailovi se obrađuju u delovima od po 20, svaki u posebnom procesu (32-bitni proces sa OCR-om je posle ~70 mailova ostajao bez memorije), sa zajedničkim stanjem dosijea.
- **Forme** (`frmTmpJobs`, `frmTmpTeretnica`): kolone Tara 1, Tara 2, „Predaje partneru“, NHM „šifra – naziv“ iz `tmpNHM`; grupa teretnice i otkačena kola.
- **Ispravke posle probe (odluke 16.11.)**: deonica KP iz šifara u r57 („3212 KP 72 0711 210013“ → Stanica1 328, Stanica3 404; KP poslednji i r10/r12 nečitljivi → i Uputna); Stanica1/Stanica3 se ne pogađaju – ista granična stanica na oba kraja → NULL i kandidati; ista šifra kod više stanica (Wlasna/Kwidzyn) → pravilo 6 (najviše korišćena), nijedna korišćena → NULL i kandidati; skraćenica prevoznika: kandidat sa drugim UIC-om nije pogodak, a kada UIC iz dokumenta ne postoji – samo kandidati bez UIC-a, pouzdanost ≤ 70 i „proveriti“ na dnevnoj listi; HR samo po zemlji → NULL i kandidati (Šid gr. / Bogojevo gr.); `OP_914`: sinonim „CINKOTURK LOJISTIK“ → 2531, prelazi BA/Zvornik → 417, MNE/Bijelo Polje → 1365, Erdut/Dalj → 51, red „HR“ obrisan.
- **Testovi**: 21 nov jedinični test (V291_*), kopije u proveri (`MotorTests`), integracioni test SAD-HER 79 + ponovljeni mail (rollback, `CIMPROVERA_TMP_PREFIKS`).

## 2026-11-14 – v2.9.0: upis poslova i teretnica u privremene tabele planiranje.tmp* (pravila ostaju 2.8.4)

- **Novi korak posle provere** (`Baza\UpisUBazu.cs`): kada je `UpisUBazu = true`, posle provere i izveštaja svaki obrađen mail se upisuje u `planiranje.tmpDokument`, `tmpJobs`, `tmpTeretnica`, `tmpTeretnicaStavke`, `tmpJobsPolje` (poreklo vrednosti) i `tmpJobsIstorija`. Jedan mail = jedna transakcija; greška → rollback, log i jedno interno obaveštenje (najviše jednom na 6 h). Provera i izveštaj se ne menjaju. Ništa vezano za najavu.
- **Posao iz tovarnog lista** (`Baza\PosaoIzDokumenata.cs`): ključ iz dosijea (r62, ili pošiljalac + r8 + relacija) – nov posao, nova verzija (samo promenjena polja, `Verzija + 1`, polja sa `RucnoIzmenjeno = 1` se ne menjaju) ili isti sadržaj (samo veza dokumenta). Platilac za deonicu KP iz r7 (`Provere.AnalizirajR7`, ciljani OCR r7 iz v2.8.4), otpravna r16/r17, uputna r10/r12, deonica KP iz r13 (13.3 redosled prevoznika, 13.4 granični prelazi) i r57: PreuzimaOd/PredajeOperateru = susedni prevoznici (UIC), a kada voz polazi / završava se u Srbiji – pošiljalac / primalac (odluka 14.11.); Stanica1/Stanica3 = tačke predaje u Srbiji sa prednošću „X gr.“ (po nazivu – `Granicna` samo za izbor), odnosno otpravna/uputna. Mase u tonama (bez masa 0; samo tara → bruto = tara), broj kola, NHM1/NHM2, RID (r7 šifra 11) i Kemler/UN samo kada postoje. Napomena posla = tekst maila (najnoviji blok gore, najviše 2000 znakova; ceo tekst u `tmpDokument.TekstMaila`).
- **Teretnica iz spiska kola**: stavka po kolima (broj kola „33839338201-9“; dužina u m, tara i neto u t kao `dbo.TeretnicaStavke` – 14.800 / 19.990; bez podatka 0), Otpravna/Uputna = deonica KP, Izvozna = otpravna, Uvozna = krajnja uputna, Dokument = broj otpravljanja iz r62, VRNP = NHM sa 4 cifre. Spisak se povezuje sa poslom: isti mail → r62 → broj voza (r8 / naslov / zaglavlje spiska) → ista kola → dosije pošiljke; spisak pre tovarnog lista ostaje bez posla i povezuje se kada list stigne. Nova verzija spiska (isti ključ ili ista kola u 10 dana): dodata/uklonjena/izmenjena kola u istoriju. Posle povezivanja posao dobija BrojKola, Bruto, Neto, Duzina, SerijaKola, NHM i OMBroj sa spiska (razlog `SPISAK_KOLA`); razlika u broju kola tovarnog lista i spiska ide u istoriju i poreklo.
- **Šifarnici** (`Baza\Sifarnici.cs`, `Baza\Slicnost.cs`): stanica po nazivu (normalizacija, OCR šum oko naziva, ćirilični natpisi obrasca), duplikati u šifarniku → stanica koja se najčešće koristi u `OP_Jobs`/`TeretnicaStavke` (Portogruaro Caorle → 1009), pa šifra stanice iz dokumenta. Partner po nazivu (`PaNaziv`, `PaDoNaziv`; bez pravnih oblika, zagrada i grada) – prag 0,85, značajna reč bez para drži ocenu ispod praga (pogrešan partner je gori od praznog); prevoznik prvo po tačnom UIC među `Prevoznik = 1` (7853 → 3440 GRAMPET RAIL), naziv za potvrdu; `tmpSinonimi` imaju prednost. NHM: tačno, pa po 4 cifre i podgrupi (252010 → 2520, 992200 → „9922 00“). Ispod praga: NULL, tekst i 3 kandidata u poreklu polja.
- **Serija kola** (`Baza\BrojKola.cs`): dvoslovna oznaka (Ea, Ua, Za, Sg…, kao `dbo.VagoniSerije`) i broj osovina iz 5. i 6. cifre broja kola po tabeli izvedenoj iz 519 378 postojećih stavki; nepouzdane kombinacije (K/O/R, L/S, R → Rg/Ri) ostaju NULL.
- **Spisak kola iz Excel-a** (`ExcelCitac.PodaciPoKolima`, `ZaglavljeSpiska`): tara/neto/bruto, broj osovina, dužina, serija, napomena, Kemler/UN i redni broj po kolima; broj voza, otpravna, uputna i NHM iz zaglavlja CIT 23.
- **Zaštite posle obrade pravih mailova 14.11.**: isti voz (broj iz r8) sa istom otpravnom i uputnom u 10 dana je isti posao i kada je ključ drugačiji (OCR pošiljaoca na drugom skenu); u novoj verziji nepročitana vrednost (NULL/0) ne briše već pronađenu; spisak iz PDF-a/OCR-a ne menja vrednosti spiska iz Excel-a (samo dodata/uklonjena kola), OCR ne menja postojeću vrednost; mase po kolima sa smislom (tara 8–40 t, neto 0 ili 1–100 t) – pomerene kolone skena se odbacuju; bruto sa spiska samo kada spisak ima taru (inače ostaje bruto lista); mase „2.234533“, „Btto: 2.090,52 to“, „Netta“, oba razdvajača; tačka predaje – prednost granična stanica.
- **Odluke 14.11. (posle tačke 8)**:
  - partner: dva kandidata bliža od 0,05 → NULL i kandidati (izuzetak `tmpSinonimi`); „Panšped“ → 1969 (`OP_914_tmp_Sinonimi.sql`);
  - platilac uvek iz r7, iz zapisa za srpsku deonicu (SRB, RS, IŽS, ŽS, KP, Kombinovani prevoz, 3212…) ili za ceo put – r15 se više ne koristi;
  - Pančevo → 246 (sinonimi „Pančevo“, „Pančevo Glavna“; sinonim važi i kada je izabrana stanica 1464 „Pančevo“);
  - PreuzimaOd / PredajeOperateru samo iz lanca prevoznika (r57/r13): prevoznik pre / posle KP-a, KP prvi → NULL, poslednji → NULL (zamenjuje „pošiljalac / primalac“);
  - Stanica2 = utovarno mesto („u.m. Kukujevci“) iz r16, naslova maila, imena fajla ili zaglavlja spiska, kada nije otpravna stanica;
  - dužina: sa spiska, inače poslednja dužina istih kola iz `dbo.TeretnicaStavke` (poreklo „istorija“), inače NULL (i u poslu, ne 0);
  - prioritet izvora spiska: novija verzija ima prednost nad starijom; u istom mailu Excel ima prednost nad skenom; OCR ne gazi popunjenu vrednost iz Excel-a.
- **Izveštaj** (`Obrada.NapomenaBrojaKola`): interna napomena „Broj kola u r21 (26) ne odgovara spisku kola (28).“ kada su list i spisak u istom mailu ili dosijeu.
- **Config / server**: `UpisUBazu` (podrazumevano false u kodu, true u App.config), konekcija samo `Konekcija.dpapi` (DPAPI pod servisnim nalogom; `Instaliraj.ps1` korak 3c – lokalno `.\SQLEXPRESS2008K`, Windows prijava `svc_cimprovera`, bez lozinke) ili `CIMPROVERA_KONEKCIJA` za ručni test; otvoreni tekst se ne koristi. `--test-veze` proverava prava. `validate --upis [--meta <json>]` za ručnu obradu foldera sa upisom.
- **SQL** (`Saobracaj\Planiranje\SQL`): `OP_910_tmp_Kreiranje.sql`, `OP_911_tmp_Brisanje.sql`, `OP_912_tmp_Forme.sql` (forme `frmTmpJobs`, `frmTmpTeretnica` u Priprema → Porudžbine), `OP_913_tmp_Prava.sql` (login `KPSRV-NOVI\svc_cimprovera`: pisanje samo u `planiranje.tmp*`, čitanje šifarnika i tabela za poređenje), `OP_914_tmp_Sinonimi.sql` (potvrđeni prevodi), `OP_915_tmp_BrisanjePodataka.sql` (samo podaci, tabele i sinonimi ostaju). Baza je na nivou kompatibilnosti 80 – filtrirani indeksi traže `ARITHABORT ON` (agent ga postavlja pri otvaranju konekcije).

## 2026-11-14 – v2.8.4: ispravke posle pravog saobraćaja 13–14.11. i priprema za produkciju

- **Strana 2/2 spiska kola** (`LokalniCitac.NastavakSpiska`): zaglavlje kolona CIT 23 ili red zbirova „Σ“ → nastavak spiska, nikad tovarni list. Natpisi koji samo liče na rubrike bez natpisa obrasca nisu „neprepoznat CIM“.
- **Bez odgovora samo kad je sigurno**: mail bez tovarnog lista (spisak kola, carinski dokumenti, najava, sastav voza) ne dobija nikakav odgovor – samo red u logu. Strana koja liči na obrazac CIM/CUV a nije pročitana (`LokalniCitac.LiciNaObrazac`) ide na internu ručnu proveru. JCI „Specifikacija prevoznih sredstava uz rubriku 18“ je carinski dokument.
- **Položeni i crno-beli skenovi** (`Ocr.Prepoznaj`, `PdfCrtac`): orijentacija (OSD, proba 90°/270°) ispod 0,75; ugao po strani važi za svako iscrtavanje i uzima u obzir /Rotate strane; 1-bit sken – sivi tonovi, uvećanje, nagib. Sken obrasca bez pročitanih natpisa prepoznaje se po okviru i brojevima rubrika uz šifru stanice (voz 258).
- **r7** (`Provere.AnalizirajR7`, `LokalniCitac.PonoviR7`): tolerancija jednog slova za veznike i reči za platioca, nove reči za platioca, naziv do kraja reda, „SRB: KP 3212“ → upozorenje „nejasno“, šifra 16 samo u propisanom obliku, ponovni OCR isečka r7.
- **Anotacije** (`PdfStrane.FreeTextAnotacije`): vidljive FreeText anotacije koje se štampaju su podatak (SAD HER 79 r58a „UIC: 3212“, GK30 r62 preduzeće 3212 → KP tovarni list).
- **Zemlja, r29, r30, r24**: zemlja posle mesta je zemlja; nepoznata reč na kraju adrese uz uputnu u inostranstvu → napomena „proverite zemlju“; datum „2026.10.26“; X po obliku (`CimSablon.ImaX`); K-505 kućica r24; sporno čitanje kućice ne menja cifre opšteg OCR-a.
- **X19**: neupisana identifikacija na spisku je napomena.
- **Ponavljanja** (`Ponavljanja.cs`, `stanje\ponavljanja.json`): isti prilog i isti tovarni list (ključ: r62 sa slovnim brojem otpravljanja, pošiljalac + r8 + relacija, sadržaj) se ne prijavljuju ponovo; jedna interna poruka po mailu.
- **Dosije pošiljke** (`Dosije.cs`, `stanje\posiljke.json`): povezivanje mailova (7 dana), priloženost na celom dosijeu, „nova verzija – ispravljeno / ostalo“, overena verzija digitalnog lista (samo r58a, r62, r29). `validate --stanje` i `.eml` kao ulaz; `preuzmi <uid> --eml`.
- **Ko popunjava** (`popunjava` i `popunjava_basis` u pravilima, odluka KP 2026-11-14): r57 i r58a pošiljalac (KP57, R57, R58A – greške pošiljaoca); r62 pošiljalac ako zna – prazan/nepotpun r62 je napomena za spoljnog pošiljaoca, kod KP tovarnog lista interni nalaz; r49 i r50 prevoznik – samo INTERNO; ostale rubrike 1–30 pošiljalac.
- **Nova verzija** navodi samo greške, bez napomena. **Naslov** sa nepouzdanom r62: „… Tovarni list CIM (<fajl>) – r62 nečitljiva“.
- **„Out of memory.“ u ciljanom OCR-u** (`LokalniCitac.CiljaniOcr`, `Slike.Unutar`): nije bio nedostatak memorije – kućica preslikana ispod ivice strane (ZIJIN C1-2298 str. 5) davala je isečak van slike, što GDI+ prijavljuje kao OutOfMemoryException. Takva kućica se preskače, isečak ostaje u slici, greška jedne kućice ne prekida ostale. Proces ostaje 32-bitni (x64 radi sa svim bibliotekama, ali menja OCR rezultate bez koristi za ovaj problem). Čitač L20.
- **Primaoci u Live režimu** (`Mail\PrimaociLive.cs`, samo priprema): odgovor svima iz prepiske, nit i naslov „RE: … – CIM provera: …“; Test zaglavlje `X-CimProvera-Live-Primaoci`.
- **Interni izveštaji**: „Primer ispravke je u prilogu.“; nepouzdano pročitane rubrike u jednom redu.
- **r62 nečitljiva**: rukom upisana r62 sa nepouzdanim ciframa se ne koristi za odluke.
- **Zaštita režima Test** (`Slanje.Posalji`): svaki primalac osim agenti@kprevoz.co.rs se uklanja pre slanja; u log „TEST: uklonjen primalac …“.

## 2026-11-14 – v2.8.3: ispravke posle testa sa 6 mailova (13–14.11.)

- **Bez izmišljenih naziva** (`Pravila\Dopune.cs`): naziv/adresa/platilac/primalac u dopuni su šablon („XXXXX, ulica i broj, poštanski broj mesto, XX“, „Platilac za deonicu SRB: XXXXX“). Izvor „Transport Partner d.o.o.“ (R1/R4 i KP7-PLATILAC) uklonjen. Na skenu nema dopune u rubrici koja nije pouzdano pročitana. Test `Dopune_nikad_ne_izmisljaju_naziv_firme` pada ako dopuna sadrži naziv firme koji nije pročitan iz priloga.
- **Zemlja r1/r4**: iz adrese (`Dopune.ZemljaIzAdrese` – poštanski broj sa oznakom zemlje, poznata mesta), pa iz stanice te strane; nikad druga strana ni r62; nepoznato „XX“. HERSAD79/80 r1 → DE.
- **Interni redovi**: odeljak „INTERNO (ne ide pošiljaocu)“ na dnu maila (Test); u Live nikad pošiljaocu (`Izvestaj.ZaPosiljaoca`). REC4 (r57) obrisan.
- **Kolona „Kako treba da izgleda“** iz iste dopune kao PDF primer; r58a = preduzeće iz r62; NHM „XXXXXX“; „posle „…““ samo iza reda sa bar 3 slova/cifre.
- **Najava voza** (`Citanje\NajavaVoza.cs`): poseban tip, ne proverava se; u log broj voza, stanice, broj kola, težina, dužina, ETA. „Натурен лист на влак“ → sastav voza.
- **TCDD 3370A** (`sablon\obrasci\tcdd-3370a.json`): raspored CIT, visina natpisa 0,0042 (podaci TCDD štampe su sitni i ranije su odbacivani kao natpisi), r16/r62 kućice i r29 na mestu TCDD štampe; bira se samo kada je na strani „Taşıma Belgesi“ (`natpisi` u šablonu, prednost pred nacionalnim šablonima). r7/r15 „FREIGHT PAYER FOR X AND KP3212 – Y“ → platilac za Srbiju Y.
- **CIT 2006 sa skena druge razmere** (`Obrasci.HipotezaOkvir`): početno poravnanje po okviru obrasca, potvrđeno sa ≥ 6 krugova brojeva rubrika (SET EXPORT str. 4: ranije 0 sidara, sada 91).
- **Interni izveštaj**: kratak uvod bez pisma (`Izvestaj.UvodInternogKratak`), listovi grupisani po fajlu.
- **Sitnice**: naslov bez „CIM CIM“; `DnoNatpisa` – viši pojas odmah ispod natpisa je upisan tekst (r29 „Wittenberg“ više ne ide na marginu); Excel sa oznakama UTI bez kola → „spisak tovarnih jedinica“; trajanje obrade svakog maila u log; mail sa ≥ 6 PDF/slika ili ≥ 8 MB obrađuje se posle ostalih u krugu, a poslednji UID se pomera samo preko neprekinutog niza završenih poruka.

## 2026-11-13 – v2.8.2: šabloni umesto izmišljenih brojeva, r25, CIT 7 samo pouzdana veza, A4

- **PDF primer – šabloni sa X** (`Pravila\Dopune.cs`, klasa `Sablon`)
  - Brojevi propisanog oblika se ne izmišljaju: broj otpravljanja „XXXXX-X“, šifra stanice „XX XXXXX-X“, broj kola „XX XX XXXX XXX-X“, MRN „26RSXXXXXXXXXXXXX“, preduzeće „XXXX“ (kada nije poznato iz r58a).
  - Datum se uzima iz dokumenta, a nepoznat sat je „XX“ („11-13-XX“).
  - Ime, telefon i mail ostaju realan primer.
- **r25**
  - Natpis, tačkice ili tekst umesto broja daju grešku „Rubrika 25: masa nije upisana kao broj“.
  - U primeru se „X kg“ upisuje u redove koje obrazac ima (T= / N= / G=, Netto / Tare / Brutto).
  - Mase koje se mogu izračunati iz Excel spiska kola (zbir kolona tara / neto / bruto) upisuju se kao prave vrednosti. COAL 16: tara 703 000 kg.
- **CIT 7**: veza samo po identifikaciji (r62), brojevima kola ili referenci (r8). Bez pouzdane veze CIT 7 je „NEPOVEZAN“ (CUV_257).
- **R1/R4/R1-ISO/R4-ISO**: zemlja se traži samo u redovima adrese, ne u nazivu firme. Za 55 16261 to sada daje grešku R1 „nedostaje zemlja“, sa dopunom HU.
- **PDF primer – izgled**: strana ostaje A4 (list se umanji); ono što ne staje ide na donju marginu, sa brojem u krugu. Dopuna ne ide u red štampanih natpisa na vrhu rubrike.
- **Završne ispravke primera**
  - r62 po poljima (kućice 62.1–62.4); mase sa razmakom („703 000 kg“).
  - Krug je uvek u svojoj rubrici, van okvira drugih rubrika.
  - Datum se uzima samo iz dokumenta; nepoznat je „XXXX-XX-XX“ / „MM-DD-XX“.
- **Testovi**: 450 prolazi, 1 preskočen.

## 2026-11-13 – v2.8.1 (program i pravila): dopune posle prihvatanja v2.8

Detalji su u `cim_validation_rules.json` → `meta.changelog` (v2.8.1). Program ima verziju 2.8.1 (csproj `<Version>`), a log svakog kruga počinje redom „Program 2.8.1, pravila 2.8.1“.

- **`.strane.json` samo u testovima.** U obradi maila takav prilog se ignoriše; u log ide upozorenje „nedozvoljen tip priloga“. `validate` ga prihvata samo uz `--test-strane`.
- **r62 na skenu (GIPS)**
  - Ciljani OCR kućica čita dva isečka: osnovni i očišćen (proširen za 30 % gore i dole, bez isprekidanih pregrada i tačaka). Glasa i sa ciframa opšteg OCR-a.
  - GIPS: 72 / 163501 / 3212 / 158900, umesto „jj2 1 6350.1 / 3212“.
  - Nepouzdano čitanje daje internu napomenu „r62 nečitljiva – proveriti ručno“ (sa onim što je agent pročitao), a ne grešku.
- **Jezik za opšte domene** (gmail.com, yahoo.com, outlook.com, hotmail.com, live.com, icloud.com…): određuje se po tekstu koji je pošiljalac sam napisao (naslov i telo, bez citata i potpisa). Primer: „IZVOZ“ daje srpski. `validate --naslov … --telo …`.
- **Excel spiskovi kola** (55 16261, CUV_257, CIM COAL 16) dodati su u test podatke; X26 više nije greška.
  - COAL 16: `wagon list 16.xlsx` nema mase robe (sva kola „Empty“, samo tara), pa R25-MASA ostaje greška. Poruka to sada kaže.
- **CIT 7 – naknadni zahtev**
  - Čitač obrasca: `Citanje\Cit7Citac.cs`.
  - Veza sa tovarnim listom iz istog maila ili iz istorije obrađenih listova (`stanje\listovi.json`, najviše 3000 zapisa i 180 dana; samo r62, strane, relacija, r7 i brojevi kola).
  - Poseban interni izveštaj „[INTERNO] CIT 7 – naknadni zahtev – …“: šta se menja (stara → nova vrednost), razlike između CIT 7 i lista i upozorenja (duplikat lista, r7 „Primalac nema pravo raspolaganja“ / 7.1, deljenje pošiljke, carinsko područje). Pošiljaocu ništa.
- **Testovi**: `TestniPrimeriTests` +17.
- **PDF primer – novi izgled** (`PrimerPdf.cs`, `Pravila\Dopune.cs`)
  - Nema strane sa legendom i tabelom; PDF ima samo strane lista (i spiska kola kada na njemu ima nalaza).
  - Upisan tekst ostaje vidljiv, a oko rubrike je samo tanak okvir. Crvenom se dopisuje samo ono što nedostaje: prvo ispod poslednjeg upisanog reda, pa desno od teksta, pa u drugi prazan prostor rubrike (pravougaonik bez mastila na slici strane), Arial 9 → 6 pt.
  - Pogrešan deo se precrtava, a ispravka ide pored. To se radi samo nad stvarnim rečima (tekstualni sloj, OCR, Word); kod polja PDF obrasca položaj reči je procena, pa se samo dopisuje.
  - Pomak tekstualnog sloja prema iscrtanoj strani (CropBox) određuje se po mastilu.
  - Krug sa brojem iz tabele maila (ispravke i napomene se broje posebno) stoji u gornjem desnom uglu i ne prekriva tekst.
  - Što ne stane ni sa 6 pt ide na marginu strane, pored rubrike, sa istim brojem.
  - Dopuna (`Nalaz.Dopuna`, `Pogresno`, `DopunaSigurna`) je stvarna vrednost iz dokumenata ili realan primer. Opis formata („naziv, ulica i broj“, „<šifra>“, „npr.“) se nikad ne upisuje.
- **Mail**: pasus o primeru je posle tabela, sa novim tekstom (brojevi u krugovima = brojevi u tabeli).
- **Testovi za primer**: nema legende; dopuna nije opis; krug sa brojem iz tabele za svaki nalaz koji nije interni; dopuna je u svojoj rubrici; pasus. Ukupno 446 prolazi, 1 preskočen.

## 2026-11-13 – pravila v2.8: ispravke posle testa („Testni primeri“, 9 dokumenata)

Detaljan spisak izmena pravila je u `cim_validation_rules.json` → `meta.changelog` (v2.8). Ukratko:

- **r7 – platilac**
  - Prepoznaje se i sa greškama u kucanju i OCR-a („Payer fo/on“, „Frachjzahjler“).
  - „Serbian path/section“ i „IŽS“ znače Srbiju.
  - „Payer for HU: A; SRB: B“ znači da je B platilac za SRB.
  - Naziv platioca se završava na kraju rečenice ili reda, pa „KP/26“ iz „CIM 6: …“ nije platilac.
  - G5 (oblik „šifra + tekst“) je samo upozorenje.
- **Tip strane**
  - Sadržaj ima prednost nad imenom fajla.
  - Natpis obrasca CIM/CUV čini stranu tovarnim listom; kopije i poleđine lista se pripajaju listu i nisu faktura.
  - CIT 7 (naknadni nalog) je poseban dokument.
  - Bez tovarnog lista, a sa skenom niske pouzdanosti: interna ručna provera, nikad „nema tovarnog lista“.
- **Interni izveštaj**
  - List koji je izdao KP (r62 = 3212) i list na ručnoj proveri dobijaju samo interni izveštaj, uvek na srpskom.
  - Nalazi „samo interno“ (R49, R50, REC1, REC8) filtriraju se iz izveštaja za pošiljaoca i iz PDF primera, i u režimu Test.
- **Nečitljiv sken** (OCR ispod 0,75): nema grešaka ni upozorenja o sadržaju, samo interne napomene „proveriti ručno“.
- **Jezik**: određuje se po pošiljaocu – interni i .rs/.ba/.me/.hr/.si dobijaju srpski, ostali engleski. Svako pravilo ima `primer_en`.
- **Čitanje**
  - r62 se deli po obliku.
  - Vrednosti polja PDF obrasca (AcroForm) nikad nisu natpis i nisu „nečitljive“.
  - U r9 su nazivi dokumenata podatak.
  - Adresa se proverava po sadržaju cele rubrike.
- **R62 i R58A** važe za svaki list. Kada je r62 = 3212, za r58a važi KP2.
- **Izveštaj**
  - Uklonjen je šum: REC3, KP6, REC6.
  - Nalazi su grupisani po tovarnom listu (podnaslov sa r62 i stranama), a dva lista iz istog fajla imaju posebne oznake.
  - Primer zemlje se uzima iz lista.
  - Spisak kola u primerima je bez šifre 271.
- **Test podaci**
  - `testovi\nezavisni\2026-11-13\` (*.strane.json = pročitane strane iz keša agenta); `validate` prihvata i `<ime>.strane.json`.
  - `validate` određuje jezik po `--posiljalac` (ili `--en` / `--sr`).
- **Testovi**: `TestniPrimeriTests` (26), dopune `MotorTests`, ažuriran snimak nalaza (REC3/REC6 uklonjeni, R58A → KP2 za r62 = 3212, R62 za prazne r62). Ukupno 417 prolazi, 1 preskočen.

## 2026-11-13 – slike iz potpisa i logo se ne proveravaju

- Slika-prilog se ne čita (nema OCR-a) i **ne vodi na ručnu proveru** kada je logo, slika iz potpisa ili ikona:
  - manja od 15 KB;
  - manja od 250 000 piksela (npr. 500×500);
  - traka ili baner (odnos stranica ≥ 3, kraća strana < 500 px);
  - ime logo/signature/potpis/ikona društvene mreže, uz manje od 1,5 Mpx.
- Sken ili fotografija tovarnog lista (velika slika) čita se kao i do sada.
- U logu piše „slika iz potpisa/logo, ne proverava se: <razlog>“, a u redu „Prilozi:“ „slika iz potpisa/logo (ne proverava se)“.
- Mail koji ima samo takve slike ne daje internu „ručnu proveru“.
- Testovi: `UkrasTests` (5).

## 2026-11-13 – tovarni list CIM/CUV kao Word dokument (CUV_exGK33)

Pravila ostaju v2.7.1. Težine pravila nisu menjane. Na server se ne postavlja dok se ne potvrdi da Syncfusion licenca dozvoljava upotrebu na serveru.

- **Word → PDF preko Syncfusion DocIO 18.4.0.34** (`Syncfusion.DocToPDFConverter.WinForms`, isti major kao aplikacija Saobracaj)
  - Formati: .docx, .docm, .doc, .dot, .dotx, .rtf.
  - Pretvaranje radi u memoriji: bez Office-a, bez prozora i bez privremenih fajlova. Ograničenje je 60 s po fajlu.
  - .odt DocIO ne čita, pa ide na ručnu proveru sa razlogom.
  - Word tabela obrasca (.docx) se i dalje čita direktno iz ćelija. Pretvaranje se koristi tek kada tabele nema.
- **Licencni ključ**
  - Čita se samo iz `Syncfusion.dpapi`: DPAPI pod nalogom agenta, upisuje ga `Instaliraj.ps1`, korak 3b. Ključ nije u kodu, config-u, logu ni git-u (`*.dpapi` je u `.gitignore`).
  - Bez ključa se ništa ne pretvara. Word prilog ide na ručnu proveru, greška se upisuje u log i šalje se jedno interno obaveštenje.
  - `--test-veze` prikazuje stanje ključa.
- **PDF dobijen iz Word-a**
  - Podaci se čitaju samo iz tekstualnog sloja, sa koordinatama.
  - Natpisi obrasca (slika iz zaglavlja) se prepoznaju OCR-om uvećane slike i služe samo kao sidra; nikad nisu podatak.
  - Šablon CIT 2006 se poravnava po linijama, brojevima rubrika i natpisima sa slike strane.
- **Dodela po bloku**
  - Reči jednog Word okvira čine blok. Ceo blok ide u rubriku u kojoj je veći deo njegovog teksta, npr. tekst r13 koji prelazi u r14 ostaje u r13.
  - Tekst okvira koji se na strani ne vidi (pokriven drugim okvirom) se ne čita.
- **Polja za čekiranje (r20, r22, r23, r30, r52, r58b):** označen je kvadratić u kome je centar vidljivog „X“ iz tekstualnog sloja.
- **r62 na Word strani:** delovi se čitaju po kućicama šablona (zemlja, stanica, preduzeće, broj).
- **Na Word strani nema „nečitljivih“ rubrika** (isto kao kod Excel tabele).
- **Prilozi:** Word prilog koji se ne pretvori, ili čije ime liči na tovarni list a obrazac nije prepoznat, ide na ručnu proveru. Pošiljaocu se nikad ne javlja „nema tovarnog lista“.
- **Log za svaki Word prilog:** da je pretvoren, trajanje, broj strana i vrsta svake strane.
- **PDF primer:** pravi se i za Word tovarni list, preko PDF-a dobijenog pretvaranjem.
- **PDF sa poljima obrasca (AcroForm, npr. CIM_draft_GK30):** vrednosti se čitaju iz polja preko PdfPig-a (postojalo je i ranije; sada je pokriveno testom).
- **Ispravka (svi formati):** „KP(3212)“ u r57 više ne daje lažno upozorenje „naziv ')'“ uz šifru 3212.
- **Testovi:** novi `WordTests` (13). Bez ključa se 9 testova koji traže pretvaranje preskače, uz razlog.

## 2026-11-13 – HITNO: obrada sanduča stajala od 08:49

- **Uzrok:** `TekstUtil.BezDijakritika` je normalizovao tekst znak po znak.
  - Znak van osnovne ravni (emoji, npr. 🙂 u naslovu ili potpisu maila) zapisan je kao par surogata, pa je polovina para izazivala `Invalid Unicode code point`.
  - Sada se par prepisuje ceo, a usamljeni surogat (zaista neispravan tekst) menja se znakom U+FFFD (`TekstUtil.Ocisti`).
  - `TekstUtil.Normalizuj` ne baca izuzetak: ako normalizacija ne uspe, vraća tekst bez nje i upisuje upozorenje u log. Isto važi za `CimSablon.Norm` (PDF, OCR, Excel i Word tekst).
- **Jedna poruka ne zaustavlja ostale** (`RedPoruka`)
  - Svaka poruka se obrađuje u svom `try/catch`.
  - Pre obrade se u log upisuju UID, Message-ID, pošiljalac, naslov i imena priloga.
  - Posle greške obrada nastavlja sa sledećom porukom. Poslednji UID se ne pomera preko poruke koja čeka ponovni pokušaj.
  - Posle 3 neuspela pokušaja (`MaxPokusaja` = 3) poruka dobija status `GreskaRucnaProvera` i više se ne pokušava. Tada se jednom šalje interno obaveštenje (UID, naslov, pošiljalac, greška); pošiljaocu ništa.
- **Upozorenja**
  - Vreme upozorenja se upisuje tek posle pokušaja slanja. Neuspelo slanje (npr. 535) ponavlja se za 10 minuta i više ne „troši“ upozorenje za 6 sati.
  - Greška celog kruga (prijava, mreža) šalje se najviše jednom na sat.
  - Svaki ishod upozorenja (poslato, nije poslato, preskočeno) vidi se u logu.
  - Slanje upozorenja koristi isti nalog i lozinku kao izveštaji.
- **Log**
  - Mail bez priloga sada ima red „mail nema priloga – preskočeno (BezPriloga)“. Ranije je bio potpuno nevidljiv.
  - Broj neispravnih Unicode znakova i znakovi van osnovne ravni (npr. U+1F642) upisuju se za svaki mail, bez ostalog sadržaja.
- **Testovi**: `RedPorukaTests` (7).

## 2026-11-13 – čitač L17: tovarni list kao Excel/Word (mail „HER-SAD 79“)

Pravila ostaju v2.7.1. Težine pravila nisu menjane.

- **Tovarni list CIM/CUV iz Excela (.xlsx/.xls/.xlsm) i Worda (.docx/.docm)**
  - Prepoznaje se po naslovu obrasca (na bilo kom jeziku) i po brojevima rubrika (najmanje 10 ćelija, od toga najmanje 8 osnovnih). Ime fajla nije dokaz.
  - Vrednosti se čitaju direktno iz ćelija, bez OCR-a. Svaka vrednost pripada najbližoj rubrici gore-levo. Natpisi iz rečnika služe kao putokazi i ne čitaju se kao vrednosti.
  - Šifre upisane po jednu cifru u ćeliji (r12, r17, r62) spajaju se, npr. „80 10128-7“. Polje „X“ se čita za r30 (CIM/CUV), r20 i ostale rubrike.
  - Rečnik natpisa dopunjen je češkim i nemačkim natpisima (+42 reči).
  - Ako obrazac ima naslov, a rubrike nisu pouzdano pročitane, sledi ručna provera sa razlogom, nikad „nema tovarnog lista“.
- **Isti list kao PDF i kao Excel** proverava se jednom:
  - PDF, ako je potpisan ili pečatiran (sken, r1 potpis ili popunjena r58a);
  - u suprotnom tabela.
  - U opisu dokumenta piše koji je fajl proveren.
- **Spisak priloga u svakom odgovoru**: red „Prilozi: …“ sa klasifikacijom svakog fajla.
- **Nove vrste priloga**
  - Izveštaj o sastavu voza (MÁV VTK / Vonatösszeállítás / Zugvorbereitung / Meldezettel): ne proverava se, nije potreban u r9 i ne izaziva grešku.
  - Loadlist/Trainlist: spisak tovarnih jedinica, izvor podataka kao spisak kola.
  - HSL Wagenliste (.xls): spisak kola. Broj kola raspoređen u pet ćelija (2|2|4|3|1) spaja se u jedan broj.
- **Tovarni list nije nađen, a neki prilog nije pročitan ili prepoznat**
  - Šalje se samo interna poruka „[INTERNO] Ručna provera“: „Tovarni list nije automatski prepoznat. Prilog <ime> nije mogao da se pročita – proverite ručno.“
  - Pošiljalac ne dobija ništa.
  - U test režimu poruka ide na adresu izveštaja, u live režimu na KopijaNa i UpozorenjaNa.
- **R22 (naročita pošiljka)** prikazuje se pod rubrikom 22. X5 (šifra 7.11) ostaje pod rubrikom 7.
- **PDF primer i za Excel/Word tovarni list**
  - Primer se uvek šalje kao PDF, bez obzira na to u kom je formatu poslat tovarni list.
  - List obrasca se crta iz ćelija: širine kolona, visine vrsta, spojene ćelije i sivi okvir svake rubrike od njenog broja do sledećeg broja rubrike.
  - Na nacrtan list se kao kod PDF-a ucrtavaju ispravke (crveno) i napomene (narandžasto), a dodaju se i legenda i žig.
  - Ime fajla nosi i alfanumerički broj otpravljanja, npr. `PRIMER_801012873127HER-SAD79.pdf`.
  - Nova biblioteka nije potrebna (GDI+ i PdfPig, kao do sada).
- **Dijagnostika**: `Saobracaj.Agent.CimProvera.exe tabela <fajl> [--celije]`.
- **Testovi**: novi `TabelarniTests` (12). Ukupno 362 prolazi, 1 preskočen (Kelebija 2006 – nema fajla).
