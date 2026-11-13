# CimProvera – izmene programa

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
- **PDF primer** se ne pravi kad je tovarni list tabela (razlog „tabela“). Excel primer čeka odobrenje biblioteke za upis.
- **Dijagnostika**: `Saobracaj.Agent.CimProvera.exe tabela <fajl> [--celije]`.
- **Testovi**: novi `TabelarniTests` (10). Ukupno 360 prolazi, 1 preskočen (Kelebija 2006 – nema fajla).
