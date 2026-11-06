// Focused Stage 2 visual evidence: frozen Wing comparisons and Caspar's long new lines.
const fs=require('fs'),path=require('path'),pw=require(process.env.PLAYWRIGHT_MODULE||'playwright');
(async()=>{
 const out=process.env.EVIDENCE_DIR||'Logs/ObservatoryVisuals';fs.mkdirSync(out,{recursive:true});
 const browser=await pw.chromium.launch({headless:true,channel:'chrome'});const report=[];
 const check=(ok,line)=>{if(!ok)throw Error(line);report.push('PASS: '+line);};
 try{
 for(const source of ['baseline','current'])for(const width of [360,1280]){
  const base=source==='baseline'?process.env.BASELINE_URL:process.env.GREYBOX_URL,identity=source==='baseline'?process.env.BASELINE_IDENTITY:process.env.CURRENT_IDENTITY;
  const ctx=await browser.newContext({viewport:{width,height:800},deviceScaleFactor:width===360?2:1,isMobile:width===360,hasTouch:width===360}),pg=await ctx.newPage();
  const served=await ctx.request.get(base+'/validation-identity.txt');check(served.ok()&&(await served.text()).trim()===identity,'owned '+source+' artifact identity at '+width);
  await pg.goto(base);await pg.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});await pg.waitForTimeout(250);
  const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot()),act=async c=>{await pg.evaluate(c=>window.ascendantDial.act(c),c);await pg.waitForTimeout(100);};
  const wait=async f=>pg.waitForFunction(f,{},{timeout:30000});
  const shot=async n=>{fs.writeFileSync(path.join(out,source+'-'+width+'-'+n+'.json'),JSON.stringify(await snap(),null,2));await pg.screenshot({path:path.join(out,source+'-'+width+'-'+n+'.png')});};
  const checkpoint=async id=>{await act('motion-on');await act('jump:'+id);await wait(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);if(source==='current'&&['whole','obswhole'].includes(id))await wait(()=>window.ascendantDial.snapshot().observatoryOpen&&!window.ascendantDial.snapshot().busy);await act('motion-on');};
  for(const id of source==='baseline'?['key1','wheel','key2','key3','key4','whole']:['key1','wheel','key2','key3','key4','whole','obswhole']){await checkpoint(id);await act('walk:wing-door');await wait(()=>window.ascendantDial.snapshot().room==='wing'&&!window.ascendantDial.snapshot().busy);await pg.waitForTimeout(400);const s=await snap();check(s.reducedMotion&&s.kitLevel===Math.min(s.keys,4)&&(!['key4','whole','obswhole'].includes(id)||s.kitRestored===s.kitPieces),'frozen Wing kit '+source+' '+id+' at '+width);await shot('wing-'+id);}
  if(source==='current'&&width===360){
   await checkpoint('whole');await act('walk:sealed-left');await wait(()=>window.ascendantDial.snapshot().room==='observatory'&&!window.ascendantDial.snapshot().busy);await act('walk:orrery');await wait(()=>window.ascendantDial.snapshot().observatoryPlaying&&!window.ascendantDial.snapshot().busy);await shot('quote-namesintro');await act('obs-next');
   for(let i=0;i<10;i++){let s=await snap(),name=(i<2?'The ':'')+s.orreryNames[i],slot=s.orreryCardLabels.indexOf(name);check(slot>=0,'native name choice for quote preview '+i);await act('obs-card:'+slot);await act('obs-next');}await shot('quote-workintro');
   await checkpoint('key5');await act('walk:chamber-door');await wait(()=>window.ascendantDial.snapshot().room==='chamber'&&!window.ascendantDial.snapshot().busy);await act('walk:books');await wait(()=>window.ascendantDial.snapshot().atBooks&&!window.ascendantDial.snapshot().busy);await act('insert');await wait(()=>window.ascendantDial.snapshot().keysSpent===5&&!window.ascendantDial.snapshot().busy);await act('walk:atrium-door');await wait(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);await act('walk:sealed-left');await wait(()=>window.ascendantDial.snapshot().room==='observatory'&&!window.ascendantDial.snapshot().busy);await act('walk:orrery');await wait(()=>window.ascendantDial.snapshot().observatoryPlaying&&!window.ascendantDial.snapshot().busy);await shot('quote-ringsintro');await act('obs-next');
   const rings=[1,2,2,2,3,4,5,6,7,8];for(let i=0;i<10;i++){await act('obs-sphere:'+i);await act('obs-ring:'+rings[i]);await act('obs-next');}await shot('quote-retrograde');await act('obs-next');await shot('quote-bandsintro');
  }
  await ctx.close();
 }
 fs.writeFileSync(path.join(out,'visuals-complete.txt'),report.join('\n')+'\n');process.stdout.write(report.join('\n')+'\n');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
