# Plan razvoja: planiranje vozova i dispečerska služba (Kombinovani prevoz)

Stanje na 1.11.2026. Materijal za budući razvoj. Sve se gradi preko postojeće MS SQL baze (nova šema `planiranje`), postojeća aplikacija se ne menja.

## Veze (claude.ai, privatne)
- Specifikacija (dokument): https://claude.ai/artifact/TMgWZ5CSnE9BR8anUY6tCF
- Prezentacija (13 slajdova): https://claude.ai/artifact/B4QtZX3c53WsbQ161TkDRc
- Dizajn ekrana (7 tabli): https://claude.ai/artifact/Wb1nmq77NrKHtpB99gkp4f
- Mapa mreže pruga sa rutiranjem: https://claude.ai/artifact/5NsrpEAtjfZpJ8QayC4DYg

## Odluke do sada
- Osnova planiranja: relacija Otpravna -> Uputna iz Najave (u inostranstvu Otpravna gr / Uputna gr), km, bruto masa i broj kola iz Najave.
- Ruta se sklapa iz Registra pruga (graf stanica), bilo koja pruga; deonica bez naših trasa se označava "Nemamo trasu" i otvara zahtev za Ad hoc trasu.
- Telegrami o zatvoru: čitaju se iz postojeće forme Telegrami (Kolosek: "pruga" = zatvor deonice, levi/desni = upozorenje, kolosek u stanici/skretnica = upozorenje; dnevni interval "Traje od/do").
- Optimizator: Python + OR-Tools CP-SAT, FastAPI kao jedini ulaz, jedan server i jedna baza. Dva plana: nedeljni i operativni.
- Princip "Ponudi mi": sistem nudi kandidate (turnus, kvalifikacija serije i pruge, lokacija, zauzetost); mašinski dispečer potvrđuje.
- Tableti su zajednički, raspoređeni po lokomotivama; mašinovođa se prijavljuje, posle prijave GPS pripada lokomotivi. Poznavanje pruge se vodi po broju pruge.
- Masa: iz Najave (Bruto), kasnije Teretnica; tablica MaxMasa u 3 sloja (ručna, fizika, naučena).
- Vremena obrade (npr. Šid 60 min) u tabeli parametara sa verzijama. Knjižice samo za ukupno vreme po relaciji.

## Otvorena pitanja
- Težina lokomotive: samo za ukupnu masu ili i za vuču.
- Prve relacije i serije za ručnu tablicu masa; relacije za vreme iz knjižica.
- Format Excela trasa (jedan primer).
- Vrednosti kolone Kolosek u telegramima; da li je Pruga naziv iz Registra.
- Uvoz OSM mape: jednokratno ili stalno osvežavanje; uslovi ODbL (navesti OpenStreetMap contributors).
- Nazivi kolona tabela Najave, Mesta, Stanice, Trase, SerijeLokomotiva, Pruge (INFORMATION_SCHEMA.COLUMNS) za SQL šemu.

## Sadržaj foldera
- 02_Dizajn_mockup: 7 ekrana (HTML, kprevoz.co.rs boje); kp.css zajednički stilovi; canvas.json raspored.
- 03_Prezentacija_izvor: deck.json i slajdovi.
- 04_Ulazni_podaci: Registar.xlsx, Voz3.docx, snimci ekrana postojeće aplikacije (Lokomotive, Pruge, Serije, Trase, Telegrami). Knjižice reda vožnje (15 PDF) ostaju na originalnoj lokaciji.
- 05_Mreza_pruga: osm_pruge_stanice_srbija.geojson (izvor: OpenStreetMap, Overpass, 1.11.2026, 7.078 segmenata, 538 stanica), mreza_stanice.json (530 stanica, 739 deonica sa km), mreza_pruga.html (mapa sa rutiranjem), skripte/ (build.py gradi graf, sg.py graf stanica i rutiranje, mk.py izvoz za mapu).

## Ograničenja mreže
- Km su iz geometrije OSM-a, ne iz Registra; Registar treba priključiti.
- Samo 20 od 538 stanica ima UIC šifru: veza sa Registrom ide po nazivu i koordinati. Poklapanje po nazivu: 525 od 873 službenih mesta Registra; ostatak (rasputnice, odvojne skretnice, državna granica, "Beograd") traži ručnu potvrdu.
- Nisu u grafu kao stanice npr. Niš ranžirna; Bar je van Srbije.
