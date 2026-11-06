// The shipped Zodiac Wing and Observatory before the approved Corridor door threshold.
const fs=require('fs'),path=require('path'),pw=require(process.env.PLAYWRIGHT_MODULE||'playwright');
(async()=>{
 const out=process.env.EVIDENCE_DIR||'Logs/CorridorFrozen';fs.mkdirSync(out,{recursive:true});const report=[];
 const browser=await pw.chromium.launch({headless:true,channel:'chrome'});
 try{for(const source of ['baseline','current'])for(const width of [360,1280]){
  const url=source==='baseline'?process.env.BASELINE_URL:process.env.GREYBOX_URL;
  const ctx=await browser.newContext({viewport:{width,height:800},deviceScaleFactor:width===360?2:1,isMobile:width===360,hasTouch:width===360}),pg=await ctx.newPage();
  const identity=await ctx.request.get(url+'/validation-identity.txt');if(!identity.ok()||(await identity.text()).trim()!==(source==='baseline'?process.env.BASELINE_IDENTITY:process.env.CURRENT_IDENTITY))throw Error('wrong '+source+' artifact');
  const errors=[];pg.on('pageerror',e=>errors.push(e.message));
  await pg.goto(url);await pg.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});
  const state=()=>pg.evaluate(()=>window.ascendantDial.snapshot()),act=async c=>{await pg.evaluate(x=>window.ascendantDial.act(x),c);await pg.waitForTimeout(100);},idle=()=>pg.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:30000});
  const shot=async id=>{const s=await state();if(s.busy)throw Error('busy capture '+id);await pg.screenshot({path:path.join(out,source+'-'+width+'-'+id+'.png'),scale:'css'});fs.writeFileSync(path.join(out,source+'-'+width+'-'+id+'.json'),JSON.stringify(s,null,2));report.push('CAPTURE: '+source+' '+width+' '+id);};
  const jump=async id=>{await act('motion-on');await act('jump:'+id);await idle();await act('motion-on');if(id==='whole')await pg.waitForFunction(()=>window.ascendantDial.snapshot().observatoryOpen&&!window.ascendantDial.snapshot().busy,{},{timeout:30000});};
  const walk=async id=>{await idle();await act('walk:'+id);await idle();};
  for(const id of ['key1','wheel','key2','key3','key4','whole','obswhole']){await jump(id);await walk('wing-door');await pg.waitForTimeout(400);const s=await state();if(s.kitLevel!==Math.min(s.keys,4)||!s.reducedMotion)throw Error('Wing kit drift '+id);await shot('wing-'+id);}
  await jump('whole');await walk('sealed-left');await shot('obs-dormant');await walk('orrery');await shot('obs-namesintro');await act('obs-next');
  for(let i=0;i<10;i++){await shot('obs-name-'+i);const s=await state(),name=(i<2?'The ':'')+s.orreryNames[i],slot=s.orreryCardLabels.indexOf(name);if(slot<0)throw Error('native name missing');await act('obs-card:'+slot);await act('obs-next');}
  await shot('obs-workintro');await act('obs-next');
  for(let i=0;i<10;i++){await shot('obs-work-'+i);await act('obs-sphere:'+i);await act('obs-seal');await act('obs-next');}
  await idle();await shot('obs-key5');
  await jump('key5');await walk('sealed-left');await shot('obs-key5-room');await walk('atrium-door');await walk('chamber-door');await walk('books');await act('insert');await idle();await walk('atrium-door');await walk('sealed-left');await walk('orrery');await shot('obs-ringsintro');await act('obs-next');await shot('obs-rings');
  const rings=[1,2,2,2,3,4,5,6,7,8];for(let i=0;i<10;i++){await act('obs-sphere:'+i);await act('obs-ring:'+rings[i]);await act('obs-next');}
  await shot('obs-retro');await act('obs-next');await shot('obs-bandsintro');await act('obs-next');await shot('obs-bands');
  const bands=[0,0,1,1,1,2,2,3,3,3];for(let i=0;i<10;i++){await act('obs-sphere:'+i);await act('obs-band:'+bands[i]);await act('obs-next');}
  for(let i=0;i<2;i++){await shot('obs-card-'+i);const s=await state();await act('obs-card:'+s.orreryQuestionOrder.indexOf(0));await act('obs-next');}
  await idle();await shot('obs-key6');await jump('key6');await walk('sealed-left');await shot('obs-key6-room');
  if(errors.length)throw Error(errors.join('|'));await ctx.close();
 }
 fs.writeFileSync(path.join(out,'captures-complete.txt'),report.join('\n')+'\n');process.stdout.write(report.join('\n')+'\n');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
