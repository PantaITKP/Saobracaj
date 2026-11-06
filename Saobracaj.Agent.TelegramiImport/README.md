# Saobracaj.Agent.TelegramiImport

Agent na serveru čita sanduče `telegrami@kprevoz.co.rs` i upisuje telegrame IŽS-a u tabelu `Telegrami`.
Aplikacija Saobracaj samo čita bazu: tab **Telegrami** na Planeru prikazuje aktivne telegrame, a `TelegramNotifier`
prikazuje MessageBox korisnicima koji imaju otvoren Planer ili dispMain.

Ovo je prvi agent i postavlja obrazac za sledeće (`Saobracaj.Agent.<Naziv>`, folder `\\192.168.1.6\Cloude\Agenti\<Naziv>\`).

## Šta radi

| Telegram | Akcija |
| --- | --- |
| Zatvor pruge/koloseka/skretnice, zabrana saobraćaja preko skretnice, naročita pošiljka (SP) | upis u `Telegrami`, popunjeno kao ručni unos |
| Otkazivanje / van snage / izmena | ciljni redovi → `Aktivan = 0`, napomena dobija `OTKAZAN/VAN SNAGE/IZMENJEN TEL. BR. …` |
| Lagane vožnje, radovi bez zatvora, posedanje, putni prelazi, red vožnje (RV), TT, testiranje vozila | samo arhiva (`TelegramiMailLog` + `TelegramiPrilozi`) |
| Nepoznata pruga ili stanica, skenirani PDF, poruka bez PDF-a | `NeedsReview` (ništa se ne upisuje u `Telegrami`) |
| Excel najave (HŽ Cargo) | arhiva |

Svaka poruka se obrađuje u jednoj transakciji. Ista poruka se nikada ne obrađuje dvaput (`Folder + UIDVALIDITY + UID`,
pa `Message-ID`). Dve istovremene instance su nemoguće: `sp_getapplock 'Agent.TelegramiImport'`, druga odmah izlazi.
**Gašenje isteklih:** na početku svakog pokretanja (na 2 min) svi redovi u `Telegrami` sa `Aktivan = 1` i
`VaziDo < sada` dobijaju `Aktivan = 0` — i ručno uneti; redovi bez `VaziDo` se ne diraju. Radi i kada mail server nije
dostupan i dok je `DryRun = true` u App.config (prvo puštanje); isključuje se samo ručnim `--dry-run` ili `GasiIstekle = false`.
ID-jevi ugašenih redova su u logu. SQL Server Express nema SQL Agent, pa ovo radi isključivo agent.
Telegrami „do opoziva“ i NP imaju `VaziDo = 31.12.` (kao ručni unos), pa se gase na kraju godine.

Kako se popunjavaju kolone (izvedeno iz ručnog unosa, ID 8931–9142):

- jedan red = tačka × dnevni interval × niz uzastopnih dana; razdvojeni dani su posebni redovi;
- `VaziOd` = prvi dan + početak intervala, `VaziDo` = poslednji dan + kraj (noćni interval: dan posle);
- `TrajeOd/TrajeDo` = `00:00:00/23:59:59`; do opoziva i NP: `VaziDo = 31.12.` tekuće godine;
- `Kolosek` kao do sada: `pruga`, `kolosek`, `levi`, `desni`, `svi`, `1,2,5`, `1-5`, `skretnica br.14`;
- `PrugaID`/`OdStanice`/`DoStanice` iz šifarnika `Pruga` i `stanice` (bez dijakritika, tolerancija jednog slova,
  „državna granica“ → `<stanica> gr.` po pruzi, naučeni nazivi iz `TelegramiAlias`);
- `ID = MAX(ID)+1` kao procedura `InsertTelegrami`, ali pod zaključavanjem;
- nove kolone: `DatumTelegrama`, `Vrsta`, `MailLogID`, `UnosAutomatski = 1`.

## Build

```
dotnet build Saobracaj.Agent.TelegramiImport -c Release
dotnet test  Saobracaj.Agent.TelegramiImport.Tests
```

Cilj je **.NET Framework 4.6.2**, a ne 4.7+. SQL Server 2008 R2 SP2 (10.50.4000) podržava samo TLS 1.0;
programi koji ciljaju .NET 4.7+ koriste sistemske TLS verzije i konekcija puca u „pre-login handshake“.

Testovi nad pravim PDF-ovima čitaju izvoz sandučeta iz `TELEGRAMI_TESTDATA` (folder `telegrami_export`).
Izvoz sadrži lična imena i telefone i **ne ide u repo**; bez njega su ti testovi `Inconclusive`.

## Baza (jednom, pre prvog pokretanja)

```
sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d <baza> -f 65001 -i Sql\001_TelegramiAgent.sql
sqlcmd -S 192.168.1.6\SQLEXPRESS2008K -d <baza> -f 65001 -i Sql\002_TelegramiAlias.sql
```

Skripte samo dodaju (kolone `NULL`, nove tabele, indekse) i mogu da se pokrenu više puta. `-f 65001` je potreban
zbog ćirilice/latinice u porukama. Na Perftech2 su obe primenjene 3. 11. 2026.

Napomena: procedure `InsertTelegrami` i `UpdateTelegrami` imaju upisano ime baze `[Perftech_Beograd].[dbo].[Telegrami]`
— i u Perftech2. Poziv procedure iz test baze piše u produkciju. Agent ih zato ne koristi.

## Instalacija na serveru 192.168.1.6

1. Kopirati sadržaj `bin\Release\net462\` u `\\192.168.1.6\Cloude\Agenti\TelegramiImport\`.
2. Servisni Windows nalog sa minimalnim pravima: čitanje foldera agenta, pisanje u `logs\`,
   u bazi `db_datareader` + `db_datawriter` + `EXECUTE` na `sp_getapplock` (dovoljno je `public`).
3. Konekcija u `App.config` (`connectionStrings/Telegrami`): `Integrated Security=True` pod servisnim nalogom
   i `Initial Catalog=Perftech_Beograd` (produkcija) ili `Perftech2` (test).
   Na samom serveru 192.168.1.6: **`Data Source=.\SQLEXPRESS2008K`** (lokalno). Preko IP adrese Windows prijava lokalnog
   naloga ne prolazi (server nije u domenu): `SSPI handshake failed 0x8009030c … login is from an untrusted domain`.
   Skripta `Instaliraj.ps1` (u folderu agenta na serveru) radi korake 2–5 sama.
4. Lozinke (vidi dole).
5. Task Scheduler, zadatak `Saobracaj\Agent TelegramiImport`:

```
schtasks /Create /TN "Saobracaj\Agent TelegramiImport" /RU <DOMEN\servisni_nalog> /RP * ^
  /SC MINUTE /MO 2 /TR "D:\Cloude\Agenti\TelegramiImport\Saobracaj.Agent.TelegramiImport.exe --once"
```

   - „Run whether user is logged on or not“, „Do not start a new instance“ (Settings → If the task is already running).
   - Ako zadatak radi na samom 192.168.1.6, u `/TR` koristiti **lokalnu** putanju deljenog foldera `Cloude`, ne UNC.
   - Ako se ipak pokreće sa UNC putanje, `App.config` već ima `<loadFromRemoteSources enabled="true"/>`.

## Lozinke

Nijedna lozinka nije u repou ni u `App.config` u otvorenom tekstu.

```
:: pokrenuti POD SERVISNIM NALOGOM (runas /user:DOMEN\servisni_nalog cmd)
Saobracaj.Agent.TelegramiImport.exe --zastiti "<lozinka telegrami@kprevoz.co.rs>"
```

Izlaz (Base64) upisati u `App.config`:

```xml
<add key="ImapLozinka.dpapi" value="AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA..." />
```

DPAPI (`CurrentUser`) znači da vrednost može da dešifruje samo taj nalog na tom računaru. Ako se koristi SQL nalog
umesto Integrated Security: `...;User ID=<nalog>;Password={SqlLozinka}` i `SqlLozinka.dpapi` na isti način.
Za ručni test (ne na serveru) mogu se koristiti promenljive okruženja `TELEGRAMI_IMAPLOZINKA` i `TELEGRAMI_KONEKCIJA`.

## Prvo puštanje

1. `DryRun = true` (podrazumevano). Agent sve parsira, upiše i **vrati transakciju** — u bazi ostaje samo red u `AgentRunLog`.
   Rezultat je u `logs\TelegramiImport-yyyyMMdd.log`.
2. Ručna provera nad izvozom sandučeta, bez IMAP-a:
   ```
   Saobracaj.Agent.TelegramiImport.exe --import-folder <telegrami_export> --dry-run
   ```
   Na Perftech2 (3. 11. 2026): 127 poruka, 82 reda za upis, 0 grešaka; ponovno pokretanje 0 novih redova.
3. Poređenje sa ručnim unosom (ID ≥ 9043): svi zatvori iz izvoza su pronađeni; razlike su greške ručnog unosa
   (287F upisan kao 278, 223F t.2 datum, 306F kraj 12:30 umesto 13:20, 266F stanica, duplikati 9113/9115/9120)
   i 265F t.2 (jedna tačka zatvara dve pruge → `NeedsReview`).
4. `ImportOd` = datum od kog se čitaju poruke pri prvom pokretanju. Redovi koji su već ručno uneti se ne dupliraju
   (isti broj, relacija, `VaziOd`/`VaziDo` do na minut → status `Duplikat`).
5. Upis se uključuje sa `DryRun = false`.

## Komande

| Argument | Značenje |
| --- | --- |
| (bez) / `--once` | jedno pokretanje (Task Scheduler) |
| `--dry-run` | sve uradi, ništa ne upiše (rollback) |
| `--import-folder <dir>` | čita izvoz sandučeta umesto IMAP-a |
| `--upis` | isključuje DryRun za to pokretanje (ručni test na test bazi) |
| `--zastiti <vrednost>` | DPAPI šifrovanje za `App.config` |
| `--parse-folder <dir> [--tsv]` | dijagnostika parsera nad PDF-ovima, bez baze |

Exit code: `0` uspeh (i kada drugi proces već radi), `1` greška.

## Praćenje

```sql
SELECT TOP 20 * FROM dbo.AgentRunLog ORDER BY ID DESC;
SELECT Status, COUNT(*) FROM dbo.TelegramiMailLog GROUP BY Status;
SELECT * FROM dbo.TelegramiMailLog WHERE Status IN (N'NeedsReview', N'Greska') ORDER BY ID DESC;
SELECT * FROM dbo.TelegramiMailStavke WHERE Status = N'NeedsReview';
```

`NeedsReview` poruke se obrađuju u aplikaciji: **frmTelegrami → tab „Na pregled (N)“** (ili dugme „Na pregled“ na
tabu Telegrami u Planeru). Korisnik izabere prugu/stanice u gridu stavki i klikne „Potvrdi“: red se upisuje u `Telegrami`
(isto kao agent: `UnosAutomatski = 1`, `MailLogID`, `DatumTelegrama`), a izabrani nazivi se pamte u `TelegramiAlias`
(„Zapamti nazive“), pa ih agent sledeći put prepoznaje sam. Poruke koje nisu telegram (reklame, prijave) → „Arhiviraj poruku“.
Pravo na potvrdu = pravo „Upis“ na formi frmTelegrami.

## Ručni test IMAP-a

```
set TELEGRAMI_IMAPLOZINKA=...        (ili ImapLozinka.dpapi u App.config)
Saobracaj.Agent.TelegramiImport.exe --dry-run
Saobracaj.Agent.TelegramiImport.exe --preuzmi <folder>   (snima poruke u formatu izvoza, za testove; samo čitanje)
```

4. 11. 2026, Perftech2: 25 poruka iz sandučeta (od 31. 10), 10 novih redova (18F, 19F, 21F, 22F — isti kao ručni unos
ID 9133–9142), ostalo duplikati redova iz izvoza; drugo pokretanje 0 poruka.
