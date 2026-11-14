# CimProvera – izmene programa

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
