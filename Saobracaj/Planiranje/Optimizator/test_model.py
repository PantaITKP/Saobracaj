"""Provera modela bez baze: python -m pytest test_model.py"""
from datetime import datetime, timedelta

from model import Lokomotiva, Osoba, Posao, Ulaz, Zauzetost, resi

D = datetime(2026, 11, 2)
A, B, C = 1, 2, 3


def minuta(a, b):
    if a is None or b is None or a == b:
        return 0
    return {frozenset((A, B)): 120, frozenset((B, C)): 120, frozenset((A, C)): 240}[frozenset((a, b))]


def posao(i, sat, sati=6, potrebno=None, od=A, do=B, **k):
    return Posao(i, D + timedelta(hours=sat), D + timedelta(hours=sat + sati), od, do, potrebno or {185: 1}, **k)


def ulaz(poslovi, lokomotive, zauzetosti=(), osobe=()):
    return Ulaz(list(poslovi), list(lokomotive), list(zauzetosti), list(osobe), minuta, vreme_resavanja_s=10)


def lok(i, serija=185, vuca="E", maticna=A):
    return Lokomotiva(i, serija, vuca, maticna)


def osoba(s, smena=True):
    o = Osoba(s, lokomotive=set(range(10, 13)) | set(range(100, 139)), pruge={7})
    if smena:
        o.smene.append((D, D + timedelta(days=7)))
    return o


def test_preklapanje_nije_moguce():
    r = resi(ulaz([posao(1, 8), posao(2, 9)], [lok(10)]))
    assert len(r.dodele) == 1 and len(r.nedodeljeni) == 1


def test_oba_posla_dobijaju_razlicite_lokomotive():
    r = resi(ulaz([posao(1, 8), posao(2, 9)], [lok(10), lok(11)]))
    assert not r.nedodeljeni
    assert {tuple(d.lokomotive) for d in r.dodele} == {(10,), (11,)}


def test_prazna_voznja_izmedju_poslova():
    # Posao 1 završava u B u 14:00, posao 2 kreće iz A: 2 h prazne vožnje ne staje pre 15:00.
    r = resi(ulaz([posao(1, 8), posao(2, 15, od=A, do=C)], [lok(10)]))
    assert len(r.dodele) == 1
    r = resi(ulaz([posao(1, 8), posao(2, 17, od=A, do=C)], [lok(10)]))
    assert len(r.dodele) == 2


def test_bira_blizu_lokomotivu():
    r = resi(ulaz([posao(1, 8)], [lok(10, maticna=C), lok(11, maticna=A)]))
    assert r.dodele[0].lokomotive == [11]
    assert r.dodele[0].prazna_voznja_min == 0


def test_dvostruka_vuca_iste_serije():
    r = resi(ulaz([posao(1, 8, potrebno={185: 2})], [lok(10), lok(11, serija=193), lok(12)]))
    assert sorted(r.dodele[0].lokomotive) == [10, 12]


def test_servis_iskljucuje():
    z = Zauzetost(10, "SERVIS", D, D + timedelta(days=1))
    r = resi(ulaz([posao(1, 8)], [lok(10)], [z]))
    assert r.nedodeljeni == [1]


def test_namenska_samo_za_svoj_projekat():
    z = Zauzetost(10, "NAMENSKA", D, D + timedelta(days=1), "X")
    assert resi(ulaz([posao(1, 8)], [lok(10)], [z])).nedodeljeni == [1]
    assert not resi(ulaz([posao(1, 8, projekat="X")], [lok(10)], [z])).nedodeljeni


def test_vrsta_vuce():
    r = resi(ulaz([posao(1, 8, samo_dizel=True)], [lok(10), lok(11, vuca="D")]))
    assert r.dodele[0].lokomotive == [11]


def test_masinovodja_sa_kvalifikacijom_i_smenom():
    o1, o2 = osoba(1, smena=False), osoba(2)
    r = resi(ulaz([posao(1, 8, pruge=[7])], [lok(10)], osobe=[o1, o2]))
    assert r.dodele[0].masinovodja == 2


def test_bez_kvalifikacije_za_prugu():
    o = osoba(1)
    r = resi(ulaz([posao(1, 8, pruge=[99])], [lok(10)], osobe=[o]))
    assert r.dodele[0].masinovodja is None


def test_odmor_masinovodje():
    r = resi(ulaz([posao(1, 8, pruge=[7]), posao(2, 16, pruge=[7])], [lok(10), lok(11)], osobe=[osoba(1)]))
    assert len(r.dodele) == 2
    assert sum(1 for d in r.dodele if d.masinovodja == 1) == 1


def test_veliki_plan_u_roku():
    poslovi = [posao(i, (i * 3) % 150) for i in range(1, 81)]
    lokomotive = [lok(100 + i, maticna=[A, B, C][i % 3]) for i in range(39)]
    osobe = [osoba(i) for i in range(1, 31)]
    u = ulaz(poslovi, lokomotive, osobe=osobe)
    u.vreme_resavanja_s = 25
    r = resi(u)
    assert r.status in ("OPTIMAL", "FEASIBLE")
    po_lok = {}
    for d in r.dodele:
        for l in d.lokomotive:
            po_lok.setdefault(l, []).append(d.posao)
    po_id = {p.id: p for p in poslovi}
    for l, lista in po_lok.items():
        izabrani = sorted((po_id[i] for i in lista), key=lambda p: p.prijem)
        for a, b in zip(izabrani, izabrani[1:]):
            assert a.predaja + timedelta(minutes=minuta(a.predaja_stanica, b.prijem_stanica)) <= b.prijem
