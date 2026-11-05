# Optimizator planiranja

Servis koji pravi nacrt nedeljnog ili operativnog plana. Python 3.11, FastAPI, OR-Tools CP-SAT. Nije deo `Saobracaj.csproj`.

## Pokretanje

1. Administrator baze pokreće `SQL/OP_005_Prava.sql` i pravi nalog za servis. Taj nalog menja samo šemu `planiranje`, a `dbo` samo čita.
2. Na serveru:

   ```
   python -m venv .venv
   .venv\Scripts\pip install -r requirements.txt
   set OP_KONEKCIJA=DRIVER={SQL Server};SERVER=...;DATABASE=Perftech_Beograd;UID=...;PWD=...
   .venv\Scripts\uvicorn main:app --host 127.0.0.1 --port 8765
   ```

3. U konfiguraciji programa `Saobracaj` (`appSettings`) dodaje se adresa servisa:

   ```xml
   <add key="PlaniranjeOptimizatorUrl" value="http://server:8765" />
   ```

   Bez te stavke, ili kad servis ne odgovara, „Predloži plan“ radi jednostavnim algoritmom u programu.

Lozinka se ne upisuje u kod ni u repozitorijum.

## API

| Poziv | Telo | Period |
|---|---|---|
| `POST /plan/nedeljni` | `{"od": "2026-11-02"}` | nedelja kojoj datum pripada |
| `POST /plan/operativni` | `{"od": "2026-11-03"}` | taj dan i sledeći |
| `GET /zdravlje` | | provera rada |

Svaki poziv upisuje novu `OP_PlanVerzija` u statusu NACRT sa stavkama. Poslovi se ne menjaju: to radi „Prihvati predlog“ u programu, uz sve provere i događaje.

## Model

- **Poslovi:** aktivni poslovi perioda koji imaju izračunat termin, a nemaju lokomotivu.
- **Promenljive:** dodela jedne ili dve lokomotive iste serije i mašinovođe.
- **Ograničenja:** nepreklapanje uz praznu vožnju, zauzetost i namenske periode, vrsta vuče, masa po tablici (pravilo 8.5), kvalifikacije, smene, odsustva i odmor.
- **Cilj, po težini:** što manje nedodeljenih poslova, zatim poslova bez mašinovođe, zatim najmanja prazna vožnja i ravnomernost (`TEZINA_RAVNOMERNOST`, `TEZINA_SERVIS`).
- **Vreme rešavanja:** 25 s, uz učitavanje i upis ukupno do 30 s.

Posao bez kvalifikovanog mašinovođe i dalje dobija lokomotivu, a program ga prikazuje kao konflikt.

Prazna vožnja u cilju računa se od poslednjeg potvrđenog položaja lokomotive. Između dva posla iz istog nacrta ona se proverava kao ograničenje, ali ne ulazi u cilj.

## Provera

```
python -m pytest test_model.py
```

Testovi modela ne koriste bazu.

## Nije urađeno

Naučeni sloj tablice masa (noćni posao iz Teretnica) nije napravljen, jer Teretnica nema utvrđenu vezu sa serijom i prugom.
