#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Uvoz šifarnika NHM iz tarife Srbija Kargo („Тарифа за превоз ствари, Део 2 – Класификација робе“, 01.05.2017.)
u planiranje.tmpNHM – pravi SQL skriptu OP_917_tmp_NHM_Podaci.sql.

Upotreba:
    python uvoz_nhm_tarife.py <Tarifa-Deo2-01.05.2017.pdf | tekst.txt> [izlaz.sql]

- Ulaz PDF: tekst se vadi sa `pdftotext -table -enc UTF-8` (xpdf ili poppler; mora biti u PATH-u).
  Ulaz .txt: već izvađen tekst istom komandom (`-table`, ne `-layout`: u -table režimu šifra i
  vertikalno centriran naziv ostaju u istim redovima, a kolone „Тарифски разред“ i „Напомене“ su poravnate).
- Samo standardna biblioteka. Ponovljivo: isti PDF → ista skripta.
- Nivo: 2 = glava („XX00 00 00“), 4 = „XXXX 00 00“, 6 = „XXXX XX 00“, 8 = ostalo.
- Duplikat Kod8 (ista šifra dva puta u tarifi): zadržava se poslednji, prijavljuje se na stderr.
"""
import os
import re
import subprocess
import sys
import tempfile
import unicodedata

IZVOR = 'Tarifa SK Deo 2, 01.05.2017.'
PODRAZUMEVANI_IZLAZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'OP_917_tmp_NHM_Podaci.sql')

CIR = {
    'А': 'A', 'Б': 'B', 'В': 'V', 'Г': 'G', 'Д': 'D', 'Ђ': 'Đ', 'Е': 'E', 'Ж': 'Ž', 'З': 'Z', 'И': 'I', 'Ј': 'J',
    'К': 'K', 'Л': 'L', 'Љ': 'Lj', 'М': 'M', 'Н': 'N', 'Њ': 'Nj', 'О': 'O', 'П': 'P', 'Р': 'R', 'С': 'S', 'Т': 'T',
    'Ћ': 'Ć', 'У': 'U', 'Ф': 'F', 'Х': 'H', 'Ц': 'C', 'Ч': 'Č', 'Џ': 'Dž', 'Ш': 'Š',
    'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd', 'ђ': 'đ', 'е': 'e', 'ж': 'ž', 'з': 'z', 'и': 'i', 'ј': 'j',
    'к': 'k', 'л': 'l', 'љ': 'lj', 'м': 'm', 'н': 'n', 'њ': 'nj', 'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't',
    'ћ': 'ć', 'у': 'u', 'ф': 'f', 'х': 'h', 'ц': 'c', 'ч': 'č', 'џ': 'dž', 'ш': 'š',
}

# prava šifra je u koloni 1 (početak reda); uvučena šifra je poziv na drugu šifru u nazivu („… из NHM-а 3920 59 10“)
SIFRA = re.compile(r'^(\d{4})\s+(\d{2})\s+(\d{2})(?=\s|$)')
GLAVA = re.compile(r'^\s*ГЛАВА\s+(\d{1,2})\s*$')
FUTER = re.compile(r'^\s*(\d{1,4}\s+)?\d{2}\.\d{2}\.\d{4}\.?(\s+\d{1,4})?\s*$|^\s*\d{1,4}\s*$|^\s*[IVXLC]+\s*$')
ZAGLAVLJE = ('Наименовање', 'Тарифски', 'разре д', 'Напомене')


def latinica(s):
    s = ''.join(CIR.get(ch, ch) for ch in s)
    # „ЉУБ“ → „LJUB“: dvoslovna slova u reči pisanoj velikim slovima
    return re.sub(r'(?<=[A-ZČĆŽŠĐ])(Lj|Nj|Dž)|(Lj|Nj|Dž)(?=[A-ZČĆŽŠĐ])', lambda m: m.group(0).upper(), s)


def normalizuj(s):
    s = latinica(s).replace('Đ', 'Dj').replace('đ', 'dj')
    s = unicodedata.normalize('NFKD', s)
    s = ''.join(ch for ch in s if not unicodedata.combining(ch))
    return re.sub(r'\s+', ' ', s).strip().lower()


def nivo(k8):
    if k8[2:] == '000000':
        return 2
    if k8[4:] == '0000':
        return 4
    if k8[6:] == '00':
        return 6
    return 8


def pocinje_velikim(t, prethodni):
    """Početak novog naziva = ćirilično veliko slovo (latinični nazivi vrsta i zagrade su nastavak).
    Red pisan samo velikim slovima posle reda pisanog velikim slovima je nastavak naslova glave."""
    t = t.lstrip('"„“«')
    if not t or not ('Ѐ' <= t[0] <= 'ӿ') or not t[0].isupper():
        return False
    if t == t.upper() and prethodni and prethodni == prethodni.upper():
        return False
    return True


def tekst_iz_pdf(putanja):
    if putanja.lower().endswith('.txt'):
        with open(putanja, encoding='utf-8') as f:
            return f.read()
    fd, tmp = tempfile.mkstemp(suffix='.txt')
    os.close(fd)
    try:
        subprocess.run(['pdftotext', '-table', '-enc', 'UTF-8', putanja, tmp], check=True)
        with open(tmp, encoding='utf-8') as f:
            return f.read()
    finally:
        os.remove(tmp)


def razdvoji(red, hp):
    """Red → (tekst naziva, razred, napomena). hp = početak kolone „Тарифски“ na strani."""
    razred = napomena = None
    tokeni = [(m.start(), m.group()) for m in re.finditer(r'\S+', red)]
    # s desna: napomena (oko hp+19), pa razred (oko hp)
    while tokeni:
        poc, tok = tokeni[-1]
        if napomena is None and razred is None and poc >= hp + 12 and re.fullmatch(r'\d{1,2}|\*', tok):
            napomena = tok
            tokeni.pop()
            continue
        if razred is None and hp - 3 <= poc <= hp + 6 and re.fullmatch(r'\d{1,2}|\*', tok):
            razred = tok
            tokeni.pop()
            continue
        break
    kraj = tokeni[-1][0] + len(tokeni[-1][1]) if tokeni else 0
    return red[:kraj].strip(), razred, napomena


def parsiraj(tekst):
    strane = tekst.split('\f')
    zapisi = []          # [kod8, [linije naziva], razred, napomena, strana]
    poceo = False
    hp_prethodna = 92
    upozorenja = []
    for br, strana in enumerate(strane, 1):
        linije = strana.split('\n')
        hp = next((l.index('Тарифски') for l in linije if 'Тарифски' in l), hp_prethodna)
        hp_prethodna = hp
        iznad = []           # linije iznad sledeće šifre (vertikalno centriran naziv)
        nastavak = True      # linije bez šifre posle šifre: nastavak naziva dok ne počne novi (veliko slovo)
        for l in linije:
            if not poceo:
                if GLAVA.match(l) and GLAVA.match(l).group(1) == '1':
                    poceo = True
                continue
            if not l.strip() or GLAVA.match(l) or FUTER.match(l) or any(z in l for z in ZAGLAVLJE):
                continue
            if re.fullmatch(r'\s*1\s+2(\s+3)?(\s+4)?\s*', l) or re.fullmatch(r'\s*3\s+4\s*', l):
                continue
            if l.strip().startswith('*'):
                continue      # fusnota („* NHM позиција се користи само на територији Републике Србије“)
            m = SIFRA.match(l)
            if m:
                k8 = m.group(1) + m.group(2) + m.group(3)
                naziv, razred, nap = razdvoji(' ' * m.end() + l[m.end():], hp)
                if not iznad and naziv and naziv[0].islower() and zapisi:
                    # naziv je počeo iznad šifre, a red je pogrešno pripisan prethodnoj šifri kao nastavak
                    # (npr. „Grana Padano, …“ latinicom): vrati poslednje redove od reda koji počinje velikim slovom
                    pret = zapisi[-1]
                    nast = pret[1][pret[5]:]
                    j = max((i for i, t in enumerate(nast) if t.lstrip('"„“«')[:1].isupper()), default=None)
                    if j is not None:
                        iznad = nast[j:]
                        del pret[1][pret[5] + j:]
                    else:
                        upozorenja.append(f'{k8} (str. {br}): naziv počinje malim slovom, a iznad nema teksta: „{naziv[:50]}“')
                delovi = iznad + ([naziv] if naziv else [])
                zapisi.append([k8, delovi, razred, nap, br, len(delovi)])
                iznad = []
                nastavak = True
                continue
            naziv, razred, nap = razdvoji(l, hp)
            if not naziv:
                if zapisi and razred and zapisi[-1][2] is None:
                    zapisi[-1][2] = razred
                continue
            if nastavak and zapisi and not iznad and not pocinje_velikim(naziv, zapisi[-1][1][-1] if zapisi[-1][1] else ''):
                zapisi[-1][1].append(naziv)
                if razred and zapisi[-1][2] is None:
                    zapisi[-1][2] = razred
                if nap and zapisi[-1][3] is None:
                    zapisi[-1][3] = nap
            else:
                nastavak = False
                iznad.append(naziv)
        if iznad and zapisi:
            # tekst bez šifre na kraju strane je nastavak poslednjeg naziva
            zapisi[-1][1].extend(iznad)
    return zapisi, upozorenja


def spoji(delovi):
    s = ' '.join(d.strip() for d in delovi if d.strip())
    s = re.sub(r'\s+', ' ', s)
    s = re.sub(r'(\w)- (\w)', lambda m: m.group(1) + '-' + m.group(2) if m.group(2).isupper() else m.group(1) + m.group(2), s)
    return s.strip()


def sql_str(s):
    return 'NULL' if s is None else "N'" + s.replace("'", "''") + "'"


def napravi_sql(redovi):
    out = []
    out.append('/* =====================================================================')
    out.append('   OP_917_tmp_NHM_Podaci.sql – GENERISANO alatom NHM/uvoz_nhm_tarife.py, ne menjati ručno.')
    out.append(f'   Izvor: {IZVOR} (srbcargo.rs, Tarifa-Deo2-01.05.2017..pdf; kopija u \\\\192.168.1.6\\Cloude\\Agenti\\CimProvera\\sifarnici\\).')
    out.append('   Briše i ponovo puni planiranje.tmpNHM (ponovljivo). Ne dira nijednu drugu tabelu.')
    out.append('   Pokreće se ručno (sysadmin), posle OP_916_tmp_Izmene.sql:')
    out.append('       sqlcmd -S 192.168.1.6\\SQLEXPRESS2008K -d Perftech_Beograd -f 65001 -i OP_917_tmp_NHM_Podaci.sql')
    out.append(f'   Redova: {len(redovi)}')
    out.append('   ===================================================================== */')
    out.append('SET NOCOUNT ON;\nSET XACT_ABORT ON;\nSET ANSI_NULLS ON;\nSET ANSI_PADDING ON;\nSET ANSI_WARNINGS ON;\nSET ARITHABORT ON;')
    out.append('SET CONCAT_NULL_YIELDS_NULL ON;\nSET QUOTED_IDENTIFIER ON;\nSET NUMERIC_ROUNDABORT OFF;\nGO')
    out.append("IF OBJECT_ID(N'planiranje.tmpNHM', N'U') IS NULL\nBEGIN\n    RAISERROR(N'Prvo pokrenuti OP_916_tmp_Izmene.sql. Ništa nije urađeno.', 16, 1);\n    SET NOEXEC ON;\nEND\nGO")
    out.append('DELETE FROM planiranje.tmpNHM;\nGO')
    for i in range(0, len(redovi), 500):
        out.append('INSERT INTO planiranje.tmpNHM (Kod8, Kod6, Kod4, Nivo, NazivCir, NazivLat, NazivNorm, TarifniRazred, Napomena, Izvor)')
        vr = []
        for k8, naziv, razred, nap in redovi[i:i + 500]:
            rz = razred if razred and razred.isdigit() else 'NULL'
            vr.append(f"SELECT '{k8}', '{k8[:6]}', '{k8[:4]}', {nivo(k8)}, {sql_str(naziv)}, {sql_str(latinica(naziv))}, "
                      f"{sql_str(normalizuj(naziv))}, {rz}, {sql_str(nap)}, {sql_str(IZVOR)}")
        out.append('\nUNION ALL '.join(vr) + ';\nGO')
    out.append("PRINT N'OP_917_tmp_NHM_Podaci: gotovo.';")
    out.append('SELECT Nivo, COUNT(*) AS Redova FROM planiranje.tmpNHM GROUP BY Nivo ORDER BY Nivo;')
    out.append('SELECT LEFT(Kod4, 2) AS Glava, COUNT(*) AS Redova FROM planiranje.tmpNHM GROUP BY LEFT(Kod4, 2) ORDER BY 1;')
    out.append("SELECT Kod8, Nivo, TarifniRazred, LEFT(NazivLat, 80) AS Naziv FROM planiranje.tmpNHM\n"
               "WHERE Kod4 IN ('2520','2702','1512','2833','4810','9922','9931') AND Nivo <= 6 ORDER BY Kod8;")
    out.append('GO\nSET NOEXEC OFF;\nGO\n')
    return '\n'.join(out)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    izlaz = sys.argv[2] if len(sys.argv) > 2 else PODRAZUMEVANI_IZLAZ
    zapisi, upozorenja = parsiraj(tekst_iz_pdf(sys.argv[1]))
    po_kodu = {}
    for k8, delovi, razred, nap, br, _ in zapisi:
        naziv = spoji(delovi)
        if k8 in po_kodu:
            upozorenja.append(f'duplikat {k8} (str. {br}): zadržan poslednji; ranije „{po_kodu[k8][1][:40]}“, sada „{naziv[:40]}“')
        po_kodu[k8] = (k8, naziv, razred, nap)
    redovi = sorted(po_kodu.values())
    for k8, naziv, razred, nap in redovi:
        if not naziv:
            upozorenja.append(f'{k8}: prazan naziv')
    with open(izlaz, 'w', encoding='utf-8-sig', newline='\r\n') as f:
        f.write(napravi_sql(redovi))
    for u in upozorenja:
        print('UPOZORENJE:', u, file=sys.stderr)
    print(f'{len(redovi)} redova → {os.path.abspath(izlaz)} ({len(upozorenja)} upozorenja)')


if __name__ == '__main__':
    main()
