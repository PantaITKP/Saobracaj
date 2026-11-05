"""Servis Optimizator: POST /plan/nedeljni i POST /plan/operativni upisuju nacrt plana."""
from __future__ import annotations

import time as sat
from datetime import date, datetime, timedelta

from fastapi import FastAPI
from pydantic import BaseModel

import podaci
from model import resi

VREME_RESAVANJA_S = 25.0

app = FastAPI(title="Planiranje Optimizator")


class Zahtev(BaseModel):
    od: date


class StavkaOdgovora(BaseModel):
    posao: int
    lokomotive: list[int]
    masinovodja: int | None = None
    prazna_voznja_min: int


class Odgovor(BaseModel):
    verzija_id: int
    broj_verzije: int
    status: str
    trajanje_s: float
    dodele: list[StavkaOdgovora]
    nedodeljeni: list[int]


def _plan(vrsta: str, od: datetime, do: datetime) -> Odgovor:
    pocetak = sat.monotonic()
    veza = podaci.otvori()
    try:
        ulaz = podaci.ucitaj(veza, od, do, VREME_RESAVANJA_S)
        veza.commit()
        rezultat = resi(ulaz)
        verzija, broj = podaci.upisi_nacrt(veza, vrsta, od, do, ulaz, rezultat.dodele)
    finally:
        veza.close()
    return Odgovor(
        verzija_id=verzija,
        broj_verzije=broj,
        status=rezultat.status,
        trajanje_s=round(sat.monotonic() - pocetak, 1),
        dodele=[StavkaOdgovora(posao=d.posao, lokomotive=d.lokomotive, masinovodja=d.masinovodja, prazna_voznja_min=d.prazna_voznja_min) for d in rezultat.dodele],
        nedodeljeni=rezultat.nedodeljeni,
    )


@app.post("/plan/nedeljni", response_model=Odgovor)
def nedeljni(z: Zahtev) -> Odgovor:
    ponedeljak = datetime.combine(z.od - timedelta(days=z.od.weekday()), datetime.min.time())
    return _plan("NEDELJNI", ponedeljak, ponedeljak + timedelta(days=7))


@app.post("/plan/operativni", response_model=Odgovor)
def operativni(z: Zahtev) -> Odgovor:
    dan = datetime.combine(z.od, datetime.min.time())
    return _plan("OPERATIVNI", dan, dan + timedelta(days=2))


@app.get("/zdravlje")
def zdravlje() -> dict:
    return {"stanje": "ok"}
