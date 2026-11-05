"""Čitanje šeme planiranje i upis nacrta. Veza je iz promenljive okruženja OP_KONEKCIJA (nalog iz OP_005_Prava.sql)."""
from __future__ import annotations

import heapq
import os
from datetime import datetime, time, timedelta
from typing import Optional

import pyodbc

from model import Lokomotiva, Osoba, Posao, Ulaz, Zauzetost

SLOJEVI = ("RUCNA", "FIZIKA", "NAUCENA")
NAJVECA_MASA_KOLA = 200


def otvori() -> pyodbc.Connection:
    veza = pyodbc.connect(os.environ["OP_KONEKCIJA"], autocommit=False)
    # Filtrirani indeksi: opcije idu u posebnoj komandi, pre upita.
    veza.execute("SET ARITHABORT ON; SET QUOTED_IDENTIFIER ON;")
    return veza


def _parametri(c: pyodbc.Connection, dan: datetime) -> dict[str, float]:
    """Globalne vrednosti parametara; opsezi po stanici i seriji ovde se ne koriste."""
    r = {}
    for k, v in c.execute(
        "SELECT Kljuc, Vrednost FROM planiranje.OP_Parametri WHERE StanicaID IS NULL AND StanicaDoID IS NULL AND SerijaID IS NULL "
        "AND VaziOd <= ? AND (VaziDo IS NULL OR VaziDo > ?) ORDER BY VaziOd", dan, dan):
        r[k] = float(v)
    return r


def _graf(c: pyodbc.Connection) -> dict[int, list[tuple[int, float]]]:
    g: dict[int, list[tuple[int, float]]] = {}
    for a, b, km in c.execute("SELECT StanicaAID, StanicaBID, DuzinaKm FROM planiranje.OP_MrezaVeza WHERE Aktivna = 1"):
        g.setdefault(a, []).append((b, float(km or 0)))
        g.setdefault(b, []).append((a, float(km or 0)))
    return g


def _rastojanja(g: dict[int, list[tuple[int, float]]], od: int) -> dict[int, float]:
    dist = {od: 0.0}
    red = [(0.0, od)]
    while red:
        d, s = heapq.heappop(red)
        if d > dist.get(s, float("inf")):
            continue
        for t, km in g.get(s, ()):
            n = d + km
            if n < dist.get(t, float("inf")):
                dist[t] = n
                heapq.heappush(red, (n, t))
    return dist


def _pocetak_dana(d) -> Optional[datetime]:
    """Stariji ODBC drajver vraća datetime2 i date kao tekst, pa se vrednost ovde svodi na datetime."""
    if d is None or isinstance(d, datetime):
        return d
    if isinstance(d, str):
        return datetime.fromisoformat(d.strip()[:19])
    return datetime.combine(d, time.min)


def _potrebno(bruto: float, serija: int, deonice: list[tuple], tablica: list, mesta: dict, dan: datetime) -> Optional[int]:
    """Pravilo 8.5. Vraća 1, 2 ili None kad ni dve lokomotive nisu dovoljne."""

    def max_masa(broj: int) -> Optional[float]:
        redovi = [r for r in tablica if r.SerijaID == serija and r.BrojLokomotiva == broj
                  and (r.VaziOd is None or _pocetak_dana(r.VaziOd) <= dan) and (r.VaziDo is None or dan < _pocetak_dana(r.VaziDo))]
        najmanja = None
        for i, (od, do, pruga) in enumerate(deonice):
            a, b = mesta.get((pruga, od)), mesta.get((pruga, do))
            smer = None if a is None or b is None or a == b else ("AB" if a < b else "BA")
            v = _masa_deonice(redovi, deonice, i, pruga, smer)
            if v is not None and (najmanja is None or v < najmanja):
                najmanja = v
        return najmanja

    jedna = max_masa(1)
    if jedna is None or bruto <= jedna:
        return 1
    dve = max_masa(2)
    return 2 if bruto <= (dve if dve is not None else 2 * jedna) else None


def _masa_deonice(redovi: list, deonice: list[tuple], i: int, pruga: Optional[int], smer: Optional[str]) -> Optional[float]:
    stanice = [d[0] for d in deonice] + ([deonice[-1][1]] if deonice else [])

    def relacija_pokriva(r) -> bool:
        if r.StanicaOdID not in stanice or r.StanicaDoID not in stanice:
            return False
        a, b = stanice.index(r.StanicaOdID), stanice.index(r.StanicaDoID)
        if r.Smer == "BA":
            return b <= i < a
        if r.Smer == "AB":
            return a <= i < b
        return min(a, b) <= i < max(a, b)

    for sloj in SLOJEVI:
        u_sloju = [r for r in redovi if r.Sloj == sloj]
        rel = [float(r.MaxMasaT) for r in u_sloju if r.StanicaOdID is not None and r.StanicaDoID is not None and relacija_pokriva(r)]
        if rel:
            return min(rel)
        if pruga is None:
            continue
        na_pruzi = [r for r in u_sloju if r.PrugaID == pruga and r.StanicaOdID is None]
        sa_smerom = [float(r.MaxMasaT) for r in na_pruzi if r.Smer is not None and r.Smer.strip() == smer]
        if sa_smerom:
            return min(sa_smerom)
        bez = [float(r.MaxMasaT) for r in na_pruzi if r.Smer is None]
        if bez:
            return min(bez)
    return None


def ucitaj(c: pyodbc.Connection, od: datetime, do: datetime, vreme_s: float) -> Ulaz:
    par = _parametri(c, od)
    g = _graf(c)
    brzina = par.get("BRZINA_PRAZNE_VOZNJE_KMH", 50.0)
    kes: dict[int, dict[int, float]] = {}

    def minuta(a: Optional[int], b: Optional[int]) -> int:
        if a is None or b is None or a == b or brzina <= 0:
            return 0
        if a not in kes:
            kes[a] = _rastojanja(g, a)
        return int(kes[a].get(b, 0) / brzina * 60 + 0.999)

    lokomotive = [Lokomotiva(r.ID, r.SerijaID, (r.VrstaVuce or "").strip() or None, r.MaticnaStanicaID) for r in c.execute(
        "SELECT l.ID, l.SerijaID, s.VrstaVuce, l.MaticnaStanicaID FROM planiranje.OP_Lokomotive l "
        "LEFT JOIN planiranje.OP_SerijeLokomotiva s ON s.ID = l.SerijaID WHERE l.Aktivna = 1 AND l.SerijaID IS NOT NULL")]
    serije = sorted({l.serija for l in lokomotive})
    tablica = c.execute("SELECT SerijaID, PrugaID, StanicaOdID, StanicaDoID, Smer, BrojLokomotiva, Sloj, MaxMasaT, VaziOd, VaziDo FROM planiranje.OP_TablicaMasa").fetchall()
    mesta = {(r.PrugaID, r.StanicaID): r.RB for r in c.execute(
        "SELECT m.PrugaID, m.StanicaID, MIN(m.RB) AS RB FROM planiranje.OP_PrugaMesto m JOIN planiranje.OP_Pruga p ON p.ID = m.PrugaID "
        "JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID WHERE v.Status = N'VAZECA' AND m.StanicaID IS NOT NULL GROUP BY m.PrugaID, m.StanicaID")}

    rute: dict[int, list[tuple]] = {}
    neelektrificirano: set[int] = set()
    for r in c.execute(
        "SELECT r.PosaoID, r.StanicaOdID, r.StanicaDoID, r.PrugaID, COALESCE(v.Elektrificirana, g.Elektrificirana) AS Elektrificirana FROM planiranje.OP_PosaoRuta r LEFT JOIN planiranje.OP_Pruga g ON g.ID = r.PrugaID "
        "LEFT JOIN planiranje.OP_MrezaVeza v ON (v.StanicaAID = r.StanicaOdID AND v.StanicaBID = r.StanicaDoID) OR (v.StanicaAID = r.StanicaDoID AND v.StanicaBID = r.StanicaOdID) "
        "ORDER BY r.PosaoID, r.RB"):
        rute.setdefault(r.PosaoID, []).append((r.StanicaOdID, r.StanicaDoID, r.PrugaID))
        if r.Elektrificirana is not None and not r.Elektrificirana:
            neelektrificirano.add(r.PosaoID)

    poslovi = []
    for r in c.execute(
        "SELECT p.ID, p.PlaniraniPrijem, p.PlaniraniPolazak, p.PlaniranaPredaja, p.PrijemStanicaID, p.PredajaStanicaID, p.Projekat, n.NajavaBrutoT, n.NajavaBrojKola, t.BrutoZbir "
        "FROM planiranje.OP_Posao p JOIN planiranje.OP_vPosao n ON n.ID = p.ID LEFT JOIN planiranje.OP_vTeretnicaNajava t ON t.NajavaID = p.NajavaID "
        "WHERE p.Status = N'AKTIVAN' AND p.PlaniraniPrijem >= ? AND p.PlaniraniPrijem < ? AND p.PlaniranaPredaja IS NOT NULL "
        "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_PosaoLokomotiva l WHERE l.PosaoID = p.ID)", od, do).fetchall():
        bruto = float(r.BrutoZbir or 0)
        if bruto > 0 and r.NajavaBrojKola and bruto / float(r.NajavaBrojKola) > NAJVECA_MASA_KOLA:
            bruto /= 1000
        if bruto <= 0:
            bruto = float(r.NajavaBrutoT or 0)
        deonice = rute.get(r.ID, [])
        potrebno = {}
        for s in serije:
            n = _potrebno(bruto, s, deonice, tablica, mesta, _pocetak_dana(r.PlaniraniPrijem))
            if n is not None:
                potrebno[s] = n
        poslovi.append(Posao(r.ID, _pocetak_dana(r.PlaniraniPrijem), _pocetak_dana(r.PlaniranaPredaja), r.PrijemStanicaID, r.PredajaStanicaID, potrebno, r.ID in neelektrificirano,
                             r.Projekat, sorted({d[2] for d in deonice if d[2] is not None}), _pocetak_dana(r.PlaniraniPolazak)))

    zauzetosti = [Zauzetost(r.LokomotivaID, r.Vrsta, _pocetak_dana(r.Od), _pocetak_dana(r.Do), r.Projekat, r.PredajaStanicaID) for r in c.execute(
        "SELECT k.LokomotivaID, k.Vrsta, k.Od, k.Do, k.Projekat, p.PredajaStanicaID FROM planiranje.OP_vLokomotivaKalendar k "
        "LEFT JOIN planiranje.OP_Posao p ON p.ID = k.PosaoID WHERE k.Od < ? AND k.Do > ?", do + timedelta(days=14), od - timedelta(days=30))]

    osobe = {r.DeSifra: Osoba(r.DeSifra) for r in c.execute("SELECT DeSifra FROM planiranje.OP_vOsoblje WHERE UPlaniranju = 1 AND Aktivan = 1 AND Masinovodja = 1")}
    for r in c.execute("SELECT DeSifra, PrugaID FROM planiranje.OP_OsobljePruga WHERE VaziDo IS NULL OR VaziDo >= ?", od):
        if r.DeSifra in osobe:
            osobe[r.DeSifra].pruge.add(r.PrugaID)
    for r in c.execute("SELECT DeSifra, LokomotivaID FROM planiranje.OP_OsobljeLokomotiva WHERE VaziDo IS NULL OR VaziDo >= ?", od):
        if r.DeSifra in osobe:
            osobe[r.DeSifra].lokomotive.add(r.LokomotivaID)
    for r in c.execute("SELECT DeSifra, Od, Do FROM planiranje.OP_Smena WHERE Vrsta <> N'SLOBODAN' AND Od IS NOT NULL AND Do IS NOT NULL AND Od < ? AND Do > ?", do, od - timedelta(days=1)):
        if r.DeSifra in osobe:
            osobe[r.DeSifra].smene.append((_pocetak_dana(r.Od), _pocetak_dana(r.Do)))
    for r in c.execute("SELECT DeSifra, [Od], [Do] FROM planiranje.OP_vOdsustvo WHERE [Od] < ? AND [Do] >= ?", do, od):
        if r.DeSifra in osobe and r[1] is not None and r[2] is not None:
            osobe[r.DeSifra].odsustva.append((_pocetak_dana(r[1]), _pocetak_dana(r[2])))
    for r in c.execute("SELECT DeSifra, Od, Do FROM planiranje.OP_Zadatak WHERE Status IN (N'POSLAT', N'PRIHVACEN') AND Od < ? AND Do > ?", do + timedelta(days=2), od - timedelta(days=2)):
        if r.DeSifra in osobe:
            osobe[r.DeSifra].zadaci.append((_pocetak_dana(r.Od), _pocetak_dana(r.Do)))

    return Ulaz(poslovi, lokomotive, zauzetosti, list(osobe.values()), minuta,
                int(par.get("TEZINA_RAVNOMERNOST", 30)), int(par.get("TEZINA_SERVIS", 120)),
                int(par.get("ODMOR_POSLE_SMENE_H", 12)), int(par.get("MAX_RAD_H", 12)), vreme_s)


def upisi_nacrt(c: pyodbc.Connection, vrsta: str, od: datetime, do: datetime, ulaz: Ulaz, dodele: list) -> tuple[int, int]:
    """Nova OP_PlanVerzija u statusu NACRT. Dodeljeni poslovi imaju red po lokomotivi, a ostali red bez nje."""
    try:
        c.execute("EXEC sp_getapplock @Resource = N'OP_PlanVerzija', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 15000")
        broj = c.execute("SELECT ISNULL(MAX(BrojVerzije), 0) + 1 FROM planiranje.OP_PlanVerzija WHERE Vrsta = ? AND PeriodOd = ?", vrsta, od).fetchval()
        verzija = c.execute(
            "INSERT planiranje.OP_PlanVerzija (Vrsta, PeriodOd, PeriodDo, BrojVerzije, Napomena, Kreirao) VALUES (?, ?, ?, ?, N'Optimizator', N'optimizator'); "
            "SELECT CAST(SCOPE_IDENTITY() AS int);", vrsta, od, do - timedelta(days=1), broj)
        verzija.nextset()
        verzija_id = verzija.fetchval()
        po_poslu = {p.id: p for p in ulaz.poslovi}
        dodeljeni = set()
        for d in dodele:
            dodeljeni.add(d.posao)
            p = po_poslu[d.posao]
            for rb, lok in enumerate(d.lokomotive, 1):
                c.execute("INSERT planiranje.OP_PlanVerzijaStavka (PlanVerzijaID, PosaoID, LokomotivaID, RB, Od, Do) VALUES (?, ?, ?, ?, ?, ?)", verzija_id, d.posao, lok, rb, p.prijem, p.predaja)
        for p in ulaz.poslovi:
            if p.id not in dodeljeni:
                c.execute("INSERT planiranje.OP_PlanVerzijaStavka (PlanVerzijaID, PosaoID, Od, Do) VALUES (?, ?, ?, ?)", verzija_id, p.id, p.prijem, p.predaja)
        c.commit()
        return verzija_id, broj
    except Exception:
        c.rollback()
        raise
