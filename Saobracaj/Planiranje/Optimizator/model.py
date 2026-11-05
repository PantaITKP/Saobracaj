"""Model plana (CP-SAT). Bez baze: ulaz su obične klase, pa se može proveravati testovima."""
from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timedelta
from typing import Callable, Optional

from ortools.sat.python import cp_model

CENA_NEDODELJENOG = 100_000
CENA_BEZ_MASINOVODJE = 10_000


@dataclass
class Posao:
    id: int
    prijem: datetime
    predaja: datetime
    prijem_stanica: Optional[int]
    predaja_stanica: Optional[int]
    potrebno_po_seriji: dict[int, int]  # serija -> 1 ili 2 lokomotive; serije koje ne mogu nisu u rečniku
    samo_dizel: bool = False
    projekat: Optional[str] = None
    pruge: list[int] = field(default_factory=list)
    polazak: Optional[datetime] = None


@dataclass
class Lokomotiva:
    id: int
    serija: Optional[int]
    vuca: Optional[str]
    maticna_stanica: Optional[int]


@dataclass
class Zauzetost:
    lokomotiva: int
    vrsta: str
    od: datetime
    do: datetime
    projekat: Optional[str] = None
    predaja_stanica: Optional[int] = None


@dataclass
class Osoba:
    sifra: int
    lokomotive: set[int] = field(default_factory=set)
    pruge: set[int] = field(default_factory=set)
    smene: list[tuple[datetime, datetime]] = field(default_factory=list)
    odsustva: list[tuple[datetime, datetime]] = field(default_factory=list)
    zadaci: list[tuple[datetime, datetime]] = field(default_factory=list)


@dataclass
class Ulaz:
    poslovi: list[Posao]
    lokomotive: list[Lokomotiva]
    zauzetosti: list[Zauzetost]
    osobe: list[Osoba]
    minuta_prazne_voznje: Callable[[Optional[int], Optional[int]], int]
    tezina_ravnomernost: int = 30
    tezina_servis: int = 120
    odmor_h: int = 12
    max_rad_h: int = 12
    vreme_resavanja_s: float = 30.0


@dataclass
class Dodela:
    posao: int
    lokomotive: list[int]
    masinovodja: Optional[int]
    prazna_voznja_min: int


@dataclass
class Rezultat:
    dodele: list[Dodela]
    nedodeljeni: list[int]
    status: str


def _namenska(z: Zauzetost) -> bool:
    return z.vrsta in ("NAMENSKA", "PROJEKAT")


def _prepreka(z: Zauzetost, p: Posao) -> bool:
    if z.do <= p.prijem or z.od >= p.predaja:
        return False
    if _namenska(z) and z.projekat and p.projekat and z.projekat.lower() == p.projekat.lower():
        return False
    return True


def _polazna_stanica(l: Lokomotiva, p: Posao, kalendar: list[Zauzetost]) -> tuple[Optional[int], Optional[datetime]]:
    """Stanica predaje poslednjeg potvrđenog posla pre prijema, zatim matična (pravilo 8.5a)."""
    pre = [z for z in kalendar if z.lokomotiva == l.id and z.vrsta == "POSAO" and z.do <= p.prijem]
    if pre:
        z = max(pre, key=lambda x: x.do)
        return z.predaja_stanica, z.do
    return l.maticna_stanica, None


def _moze_lokomotiva(ulaz: Ulaz, l: Lokomotiva, p: Posao, kalendar: list[Zauzetost]) -> Optional[int]:
    """Vraća minute prazne vožnje ako lokomotiva može da preuzme posao, inače None."""
    if l.serija is None or l.serija not in p.potrebno_po_seriji:
        return None
    if p.samo_dizel and l.vuca != "D":
        return None
    if any(_prepreka(z, p) for z in kalendar if z.lokomotiva == l.id):
        return None
    stanica, slobodna_od = _polazna_stanica(l, p, kalendar)
    prazna = ulaz.minuta_prazne_voznje(stanica, p.prijem_stanica)
    if slobodna_od is not None and slobodna_od + timedelta(minutes=prazna) > p.prijem:
        return None
    # Mora da stigne i do sledećeg već potvrđenog posla.
    posle = [z for z in kalendar if z.lokomotiva == l.id and z.vrsta == "POSAO" and z.od >= p.predaja]
    if posle:
        z = min(posle, key=lambda x: x.od)
        if p.predaja + timedelta(minutes=ulaz.minuta_prazne_voznje(p.predaja_stanica, z.predaja_stanica)) > z.do:
            return None
    return prazna


def _moze_osoba(ulaz: Ulaz, o: Osoba, p: Posao, lokomotive: set[int]) -> bool:
    od = p.polazak or p.prijem
    if (p.predaja - od) > timedelta(hours=ulaz.max_rad_h):
        return False
    if not (lokomotive & o.lokomotive):
        return False
    if any(x not in o.pruge for x in p.pruge):
        return False
    if not any(a <= od and b >= p.predaja for a, b in o.smene):
        return False
    if any(a.date() <= p.predaja.date() and b.date() >= od.date() for a, b in o.odsustva):
        return False
    odmor = timedelta(hours=ulaz.odmor_h)
    return not any(a < p.predaja + odmor and b > od - odmor for a, b in o.zadaci)


def resi(ulaz: Ulaz) -> Rezultat:
    model = cp_model.CpModel()
    x: dict[tuple[int, int], cp_model.IntVar] = {}
    prazna: dict[tuple[int, int], int] = {}
    dodeljen: dict[int, cp_model.IntVar] = {}
    trosak: list = []

    for p in ulaz.poslovi:
        dodeljen[p.id] = model.NewBoolVar(f"dodeljen_{p.id}")
        trosak.append(CENA_NEDODELJENOG * (1 - dodeljen[p.id]))
        serije: dict[int, list[cp_model.IntVar]] = {}
        for l in ulaz.lokomotive:
            m = _moze_lokomotiva(ulaz, l, p, ulaz.zauzetosti)
            if m is None:
                continue
            v = model.NewBoolVar(f"x_{p.id}_{l.id}")
            x[(p.id, l.id)] = v
            prazna[(p.id, l.id)] = m
            serije.setdefault(l.serija, []).append(v)
            servis = any(z.lokomotiva == l.id and z.vrsta == "SERVIS" and p.predaja <= z.od <= p.predaja + timedelta(hours=48) for z in ulaz.zauzetosti)
            trosak.append((m + (ulaz.tezina_servis if servis else 0)) * v)
        # Za dve lokomotive obe moraju biti iste serije.
        izbori = []
        for serija, lokomotive in serije.items():
            y = model.NewBoolVar(f"y_{p.id}_{serija}")
            model.Add(sum(lokomotive) == p.potrebno_po_seriji[serija] * y)
            izbori.append(y)
        model.Add(sum(izbori) == dodeljen[p.id])

    # Nepreklapanje sa praznom vožnjom između dva posla.
    poslovi = sorted(ulaz.poslovi, key=lambda a: a.prijem)
    for l in ulaz.lokomotive:
        for i, a in enumerate(poslovi):
            if (a.id, l.id) not in x:
                continue
            for b in poslovi[i + 1:]:
                if (b.id, l.id) not in x:
                    continue
                if a.predaja + timedelta(minutes=ulaz.minuta_prazne_voznje(a.predaja_stanica, b.prijem_stanica)) > b.prijem:
                    model.Add(x[(a.id, l.id)] + x[(b.id, l.id)] <= 1)

    # Mašinovođa: bar po jedan za dodeljen posao, ali posao bez njega ostaje dodeljen uz veću cenu.
    z: dict[tuple[int, int], cp_model.IntVar] = {}
    for p in ulaz.poslovi:
        kandidati = []
        for o in ulaz.osobe:
            moguce = {l.id for l in ulaz.lokomotive if (p.id, l.id) in x}
            if not _moze_osoba(ulaz, o, p, moguce):
                continue
            v = model.NewBoolVar(f"z_{p.id}_{o.sifra}")
            z[(p.id, o.sifra)] = v
            kandidati.append(v)
            # Mašinovođa mora da poznaje svaku izabranu lokomotivu.
            for l in ulaz.lokomotive:
                if (p.id, l.id) in x and l.id not in o.lokomotive:
                    model.Add(v + x[(p.id, l.id)] <= 1)
        bez = model.NewBoolVar(f"bez_masinovodje_{p.id}")
        model.Add(sum(kandidati) + bez == dodeljen[p.id])
        trosak.append(CENA_BEZ_MASINOVODJE * bez)

    odmor = timedelta(hours=ulaz.odmor_h)
    for o in ulaz.osobe:
        for i, a in enumerate(poslovi):
            if (a.id, o.sifra) not in z:
                continue
            for b in poslovi[i + 1:]:
                if (b.id, o.sifra) in z and a.predaja + odmor > (b.polazak or b.prijem):
                    model.Add(z[(a.id, o.sifra)] + z[(b.id, o.sifra)] <= 1)

    # Ravnomernost: ceo broj poslova po lokomotivi koji se penalizuje iznad proseka.
    if ulaz.lokomotive and ulaz.poslovi:
        prosek = max(1, sum(max(p.potrebno_po_seriji.values(), default=1) for p in ulaz.poslovi) // len(ulaz.lokomotive))
        for l in ulaz.lokomotive:
            uzete = [x[(p.id, l.id)] for p in ulaz.poslovi if (p.id, l.id) in x]
            if not uzete:
                continue
            visak = model.NewIntVar(0, len(uzete), f"visak_{l.id}")
            model.Add(visak >= sum(uzete) - prosek)
            trosak.append(ulaz.tezina_ravnomernost * visak)

    model.Minimize(sum(trosak))
    resavac = cp_model.CpSolver()
    resavac.parameters.max_time_in_seconds = ulaz.vreme_resavanja_s
    resavac.parameters.num_search_workers = 8
    status = resavac.Solve(model)
    if status not in (cp_model.OPTIMAL, cp_model.FEASIBLE):
        return Rezultat([], [p.id for p in ulaz.poslovi], resavac.StatusName(status))

    dodele, nedodeljeni = [], []
    for p in ulaz.poslovi:
        izabrane = [l.id for l in ulaz.lokomotive if (p.id, l.id) in x and resavac.Value(x[(p.id, l.id)])]
        if not izabrane:
            nedodeljeni.append(p.id)
            continue
        masinovodja = next((o.sifra for o in ulaz.osobe if (p.id, o.sifra) in z and resavac.Value(z[(p.id, o.sifra)])), None)
        dodele.append(Dodela(p.id, izabrane, masinovodja, max(prazna[(p.id, l)] for l in izabrane)))
    return Rezultat(dodele, nedodeljeni, resavac.StatusName(status))
