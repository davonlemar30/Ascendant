from pathlib import Path
import subprocess,json
root=Path(__file__).resolve().parents[4]
art=root/'Assets/CelestialDial/Resources/Art'
out=Path('/private/tmp/obs-fitted-spheres');out.mkdir(exist_ok=True)
palettes={'sun':['C99A42','62441E','EADCC3'],'mercury':['7E8996','38424E','B8C3CC'],'venus':['8EAF9E','38584E','C9D6B4'],'mars':['AC513C','512F2B','D99461'],'uranus':['8ABEC5','3B616F','D4E7DE'],'neptune':['3A5E9B','21334E','91B6D0'],'pluto':['776D87','3A3449','AAA0B9'],'dark':['34333B','1C1A1F','666573']}
run=lambda a:subprocess.run(['magick']+list(map(str,a)),check=True)
run([art/'obs-sphere-moon.png','-alpha','extract',out/'alpha.png'])
for name,colors in palettes.items():
 args=[art/'obs-sphere-moon.png','-alpha','off']
 for old,new in zip(['C4CFD8','465363','EADCC3'],colors): args+=['-fill','#'+new,'-opaque','#'+old]
 args += [out/'alpha.png','-alpha','off','-compose','CopyOpacity','-composite']
 if name=='dark':args+=['-resize','64x64']
 args+=[out/('obs-sphere-'+name+'.png')];run(args)
print('Prepared eight distinct fitted variants outside Assets; no running Unity asset edits.')
