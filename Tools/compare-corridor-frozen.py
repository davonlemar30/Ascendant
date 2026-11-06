#!/usr/bin/env python3
"""Compare native captures as decoded RGB bytes, without image normalization."""
from pathlib import Path
from PIL import Image
import json,sys
folder=Path(sys.argv[1]);results=[]
baseline={p.name.removeprefix('baseline-') for p in folder.glob('baseline-*.png')}
current={p.name.removeprefix('current-') for p in folder.glob('current-*.png')}
if baseline!=current:raise SystemExit('baseline/current capture sets differ')
for p in sorted(folder.glob('baseline-*.png')):
 other=p.with_name(p.name.replace('baseline-','current-',1))
 if not other.exists():raise SystemExit('missing current capture: '+str(other))
 a=Image.open(p).convert('RGB');b=Image.open(other).convert('RGB')
 same=a.size==b.size and a.tobytes()==b.tobytes();results.append(dict(capture=p.name,size=a.size,identical=same));print(('PASS' if same else 'FAIL')+': '+p.name)
if not results:raise SystemExit('no baseline captures')
(folder/'pixel-comparison.json').write_text(json.dumps(results,indent=2)+'\n')
if not all(row['identical'] for row in results):raise SystemExit(1)
