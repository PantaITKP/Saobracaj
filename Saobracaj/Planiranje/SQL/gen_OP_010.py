# Pravi OP_010_MrezaPodaci.sql iz mreza_srb.json (05_Mreza_pruga, final.py).
# Pokretanje: uv run --no-project python gen_OP_010.py <mreza_srb.json> OP_010_MrezaPodaci.sql
# Delovi skripte koji nisu podaci su u OP_010_zaglavlje.sqlt i OP_010_obrada.sqlt.
import json, math, os, sys

d = json.load(open(sys.argv[1], encoding='utf-8'))
S, E, ST = d['stanice'], d['grane'], d['stavke']
ovde = os.path.dirname(os.path.abspath(__file__))


def q(v):
    if v is None or v == '': return 'NULL'
    if isinstance(v, bool): return '1' if v else '0'
    if isinstance(v, (int, float)): return repr(round(v, 6)) if isinstance(v, float) else str(v)
    return "N'" + str(v).replace("'", "''") + "'"


def rdp(p, e):
    if len(p) < 3: return p
    a, b = p[0], p[-1]; dx, dy = b[0]-a[0], b[1]-a[1]; n = math.hypot(dx, dy); m = -1; mi = 0
    for i in range(1, len(p)-1):
        dd = abs(dy*p[i][0]-dx*p[i][1]+b[0]*a[1]-b[1]*a[0])/n if n else math.hypot(p[i][0]-a[0], p[i][1]-a[1])
        if dd > m: m = dd; mi = i
    if m > e: return rdp(p[:mi+1], e)[:-1] + rdp(p[mi:], e)
    return [a, b]


# U json-u je "kategorija" D4, a u OP_PrugaMesto se to zove Klasa; deo vrednosti je pisan ćirilicom.
CIR = str.maketrans('АВСЕ', 'ABCE')
OSOVINSKO = {'A': 16, 'B': 18, 'C': 20, 'D': 22.5}


def klasa(k):
    return k.translate(CIR) if k else None


def osovinsko(k):
    return OSOVINSKO.get(klasa(k)[0]) if k else None


# Legenda Registra (isto kao Pravila/RegistarSifre.cs); json za kodove 9-14 i 16 ima privremene nazive.
VRSTE = {0: None, 9: 'prelom brzine', 10: 'TO i stajalište', 11: 'STO', 12: 'tovarište', 13: 'TO', 14: 'odvojna skretnica', 16: 'odjavnica'}


# Veza je jedinstvena po paru stanica: ostaje deonica sa zvaničnom dužinom, zatim ona na pruzi višeg ranga.
RANG = {'M': 0, 'R': 1, 'L': 2}


def rang(e):
    return (e['km_izvor'] != 'REGISTAR', RANG.get(e['pruga'][0], 3))


def hav(a, b):
    r = math.pi / 180
    dl, dn = (b['latitude']-a['latitude'])*r, (b['longitude']-a['longitude'])*r
    h = math.sin(dl/2)**2 + math.cos(a['latitude']*r)*math.cos(b['latitude']*r)*math.sin(dn/2)**2
    return 2*6371*math.asin(math.sqrt(h))


# Deonica dodata iz naziva pruge dobija dužinu iz razlike stacionaža dve pruge, a to može biti slučajno mala vrednost.
# Važi isto pravilo kao za ostale deonice u final.py: kraća od prave linije znači da se dužina računa iz koordinata.
po_kljucu = {s['kljuc']: s for s in S}
for e in E:
    a, b = po_kljucu[e['a']], po_kljucu[e['b']]
    if e['km_izvor'] == 'REGISTAR_STAC' and a['latitude'] is not None and b['latitude'] is not None:
        prava = hav(a, b)
        if prava > e['km']*1.15 + 1.0:
            print('dužina iz koordinata:', e['pruga'], a['naziv'], '-', b['naziv'], e['km'], '->', round(prava*1.2, 3))
            e['km'], e['km_izvor'] = round(prava*1.2, 3), 'GEOMETRIJA'

parovi = {}
for e in E:
    k = tuple(sorted((e['a'], e['b'])))
    if k not in parovi or rang(e) < rang(parovi[k]): parovi[k] = e
G = list(parovi.values())

o = []
w = o.append
w(open(os.path.join(ovde, 'OP_010_zaglavlje.sqlt'), encoding='utf-8').read())

kol_reg = 'RegId,Kljuc,Naziv,NazivCir,NazivRegistar,AltNazivi,SifraIZS,Kod,UIC,VrstaKod,VrstaMesta,Granicna,GranicniPrelaz,AdministrativnaLinija,KiM,Lat,Lon,KoordIzvor,OsmId,Pruge,Cvor,Posednutost,OtvorenaZa,Vaga,Rampa,MaxDvAB,KolAB,MaxDvBA,KolBA,Regulisanje,Osiguranje,NagibSt,MinR,NV,Aktivna'
for i in range(0, len(S), 200):
    r = []
    for s in S[i:i+200]:
        r.append('(' + ','.join(q(v) for v in (s['id'], s['kljuc'], s['naziv'], s['naziv_cir'], s['naziv_registar'], s['alt_nazivi'], s['sifra_izs'], s['kod'], s['uic'],
            s['vrsta_kod'] or None, VRSTE.get(s['vrsta_kod'], s['vrsta_mesta']), s['granicna'], s['granicni_prelaz'], s['administrativna_linija'], s['kim_unmik'], s['latitude'], s['longitude'], s['koord_izvor'], int(s['osm_id'].split('/')[-1]) if s['osm_id'] else None,
            s['pruge'], s['cvor'], s['posednutost'], s['otvorena_za'], s['vaga'], s['rampa'], s['max_duzina_voza_ab'], s['koloseci_ab'], s['max_duzina_voza_ba'], s['koloseci_ba'],
            s['regulisanje'], s['osiguranje'], s['nagib_stanice'], s['min_radijus'], s['nadmorska_visina'], s['aktivna'])) + ')')
    w('INSERT #Reg (' + kol_reg + ') VALUES\n' + ',\n'.join(r) + ';')

# Stavke se vežu za OP_PrugaMesto po pruzi i šifri ili nazivu iz Registra.
po_id = {s['id']: s for s in S}
vid = set()
red = []
for st in ST:
    s = po_id[st['stanica_id']]
    nazivi = [s['naziv_registar']] + [a.strip() for a in (s['alt_nazivi'] or '').split(';') if a.strip()]
    for n in nazivi:
        k = (st['pruga'], s['kljuc'], n)
        if k in vid: continue
        vid.add(k)
        red.append('(' + ','.join(q(v) for v in (st['pruga'], s['kljuc'], s['sifra_izs'], st['vrsta_kod'] or None, n)) + ')')
for i in range(0, len(red), 500):
    w('INSERT #Stavka (Pruga,Kljuc,SifraIZS,VrstaKod,NazivRegistar) VALUES\n' + ',\n'.join(red[i:i+500]) + ';')

for i in range(0, len(G), 100):
    r = []
    for e in G[i:i+100]:
        wkt = None
        if e.get('geom'):
            wkt = 'LINESTRING(' + ', '.join(f'{x:.5f} {y:.5f}' for x, y in rdp(e['geom'], 0.0002)) + ')'
        r.append('(' + ','.join(q(v) for v in (e['pruga'], e['a'], e['b'], round(e['km'], 3), e['km_izvor'], klasa(e.get('kategorija')), osovinsko(e.get('kategorija')), e.get('vmax'),
            e['dvokolosecna'], e['el'], e.get('mer_uspon'), e.get('mer_pad'), e['kim'], e.get('geom_izvor'), wkt)) + ')')
    w('INSERT #Grana (Pruga,OdKljuc,DoKljuc,Km,KmIzvor,Klasa,OsovinskoT,Vmax,Dvokolosecna,Elektro,MerU,MerP,KiM,GeomIzvor,Geom) VALUES\n' + ',\n'.join(r) + ';')

w('GO\n')
w(open(os.path.join(ovde, 'OP_010_obrada.sqlt'), encoding='utf-8').read())
open(sys.argv[2], 'w', encoding='utf-8-sig', newline='\r\n').write('\n'.join(o))
print('stanica', len(S), 'grana', len(G), 'od', len(E), 'redova stavki', len(red))
