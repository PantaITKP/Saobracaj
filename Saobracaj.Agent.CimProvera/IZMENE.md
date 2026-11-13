# CimProvera – izmene programa

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
