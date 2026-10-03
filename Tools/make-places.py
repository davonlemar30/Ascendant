#!/usr/bin/env python3
"""The opening's place list (batch 2, owner Oct 2: "Download OK: cities15000.zip from download.geonames.org (CC BY 4.0) ... Use the
IANA tz data already on the Mac"). Writes Assets/CelestialDial/Resources/Places/places.txt (one line per place, biggest first) and
zones.txt (each zone's UTC offsets from 1900 to 2040), from GeoNames' cities15000.txt and the machine's IANA time zone database.
Run from the repository root:

    python3 Tools/make-places.py path/to/cities15000.txt

The Places/SOURCES.txt beside the files records where they came from and the GeoNames credit."""
import sys, os, unicodedata, datetime, zoneinfo, collections
SRC = sys.argv[1] if len(sys.argv) > 1 else 'cities15000.txt'
OUT = 'Assets/CelestialDial/Resources/Places'
countries = {}
for line in open('/usr/share/zoneinfo/iso3166.tab', encoding='utf-8'):
    if line.startswith('#') or not line.strip(): continue
    code, name = line.rstrip('\n').split('\t')[:2]; countries[code] = name
def fold(s):  # lowercase ascii for search: accents dropped
    s = unicodedata.normalize('NFKD', s); s = ''.join(c for c in s if not unicodedata.combining(c)); return s.lower()
rows = []
for line in open(SRC, encoding='utf-8'):
    f = line.rstrip('\n').split('\t')
    name, ascii_, lat, lon, cc, admin1, pop, tz = f[1], f[2], float(f[4]), float(f[5]), f[8], f[10], int(f[14] or 0), f[17]
    if not tz: continue
    rows.append(dict(name=name, ascii=ascii_, lat=lat, lon=lon, cc=cc, admin1=admin1, pop=pop, tz=tz))
rows.sort(key=lambda r: (-r['pop'], r['name']))
def label(r):
    country = countries.get(r['cc'], r['cc'])
    region = r['admin1'] if r['cc'] == 'US' and r['admin1'].isalpha() else ''
    return r['name'] + (', ' + region if region else '') + ', ' + country
labels = collections.Counter(label(r) for r in rows)
zones = sorted({r['tz'] for r in rows}); zi = {z: i for i, z in enumerate(zones)}
with open(OUT + '/places.txt', 'w', encoding='utf-8') as out:
    for r in rows:
        shown = label(r)
        if labels[shown] > 1:  # the same name twice in a country: where it is tells them apart
            shown += ' (%.1f°%s, %.1f°%s)' % (abs(r['lat']), 'N' if r['lat'] >= 0 else 'S', abs(r['lon']), 'E' if r['lon'] >= 0 else 'W')
        key = fold(r['ascii'] or r['name'])
        plain = key == r['name'].lower() and ',' not in r['name']  # the game reads a plain key off the name itself
        out.write('\t'.join([shown, '' if plain else key, '%.4f' % r['lat'], '%.4f' % r['lon'], str(zi[r['tz']])]) + '\n')
# the zones: the offset in force from 1900, then each change (minutes since 1900-01-01 00:00 UTC, the new offset in seconds), to 2040
EPOCH = datetime.datetime(1900, 1, 1, tzinfo=datetime.timezone.utc); END = datetime.datetime(2040, 1, 1, tzinfo=datetime.timezone.utc)
def off(z, t): return int(t.astimezone(z).utcoffset().total_seconds())
with open(OUT + '/zones.txt', 'w', encoding='utf-8') as out:
    total = 0
    for name in zones:
        z = zoneinfo.ZoneInfo(name); t = EPOCH; o = off(z, t); changes = []
        while t < END:
            n = t + datetime.timedelta(days=1); no = off(z, n)
            if no != o:
                lo, hi = t, n  # the minute it changes
                while (hi - lo) > datetime.timedelta(minutes=1):
                    mid = lo + (hi - lo) / 2; mid = mid.replace(second=0, microsecond=0)
                    if mid <= lo: mid = lo + datetime.timedelta(minutes=1)
                    if off(z, mid) == o: lo = mid
                    else: hi = mid
                changes.append('%d:%d' % (int((hi - EPOCH).total_seconds() // 60), no)); o = no
            t = n
        total += len(changes)
        out.write(name + '\t' + str(off(z, EPOCH)) + ('\t' + ' '.join(changes) if changes else '') + '\n')
print(len(rows), 'places;', len(zones), 'zones;', total, 'changes; duplicates disambiguated:', sum(1 for k, v in labels.items() if v > 1))
