// Stage 2 spot checks. Run against this branch's fresh shared build; never reports a partial run as green.
const fs=require('fs'),path=require('path');
const pw=require(process.env.PLAYWRIGHT_MODULE||'playwright');
(async()=>{
 const out=process.env.EVIDENCE_DIR||'Logs/ObservatoryEvidence';fs.mkdirSync(out,{recursive:true});fs.rmSync(path.join(out,'validation.txt'),{force:true});
 const browser=await pw.chromium.launch({headless:true,channel:'chrome'}),report=[];let pg;
 function check(ok,line){if(!ok)throw Error(line);report.push('PASS: Stage 2: '+line);}
 try{
 for(const view of (process.env.VIEWPORTS||'1280x800,360x800').split(',')){
 const [width,height=800]=view.split('x').map(Number),ctx=await browser.newContext({viewport:{width,height},deviceScaleFactor:+process.env.DEVICE_SCALE||1,isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});pg=await ctx.newPage();const errors=[];pg.on('pageerror',e=>errors.push(e.message));
 await pg.goto(process.env.GREYBOX_URL||'http://127.0.0.1:8791');await pg.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});
 const act=async c=>{await pg.evaluate(x=>window.ascendantDial.act(x),c);await pg.waitForTimeout(100);};
 const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot());
 const wait=async f=>pg.waitForFunction(f,{},{timeout:30000});
 const shot=async n=>pg.screenshot({path:path.join(out,view+'-'+n+'.png')});
 const tap=async id=>{const el=pg.locator('#control-'+id.replace(':','-'));await el.waitFor({state:'attached'});const r=await el.evaluate(e=>({x:parseFloat(e.style.left),y:parseFloat(e.style.top),w:parseFloat(e.style.width),h:parseFloat(e.style.height)}));check(r.w>=44*Math.min(width/360,height/800)-.1&&r.h>=44*Math.min(width/360,height/800)-.1,'44px native target '+id+' at '+view);await pg.mouse.click(r.x+r.w/2,r.y+r.h/2);await pg.waitForTimeout(150);};
 await act('jump:whole');await wait(()=>window.ascendantDial.snapshot().keys===4&&window.ascendantDial.snapshot().screen==='hub');
 await wait(()=>window.ascendantDial.snapshot().observatoryOpen&&!window.ascendantDial.snapshot().busy);let s=await snap();check(s.wingWhole&&s.keysSpent===4&&s.observatoryDoorSeen,'Wing whole door scene finishes and opens Observatory at '+view);await shot('door');
 await act('walk:sealed-left');await wait(()=>window.ascendantDial.snapshot().room==='observatory'&&!window.ascendantDial.snapshot().busy);s=await snap();check(s.observatoryLevel===0&&s.pois.includes('orrery')&&s.canOpenJournal,'walkable dormant room, orrery, journal at '+view);await shot('dormant');
 await act('walk:orrery');await wait(()=>window.ascendantDial.snapshot().observatoryPlaying&&!window.ascendantDial.snapshot().busy);await tap('obs-next');
 for(let i=0;i<10;i++){s=await snap();check(s.orreryTarget===i&&s.orreryPhase==='names','symbol seat order '+i+' at '+view);await shot('glyph-'+i);const cards=await pg.locator('[data-observatory]').evaluateAll(es=>es.filter(e=>!e.disabled&&e.id.startsWith('control-obs-card-')).map(e=>({id:e.id,txt:e.textContent})));check(cards.length===4,'four native name choices '+i+' at '+view); // Choose by the native semantic label supplied by the game, not a duplicated options algorithm.
 if(!(await snap()).orreryNamed[i]){const state=await snap();const labels=state.orreryCardLabels||[];if(!labels.length)throw Error('missing native card labels');await tap('obs-card:'+labels.indexOf((i===0?'The Moon':i===1?'The Sun':state.orreryNames[i])));}
 await tap('obs-next');}
 await tap('obs-next');
 for(let i=0;i<10;i++){s=await snap();check(s.orreryPhase==='work'&&s.orreryTarget===i,'work target '+i+' at '+view);await shot('spheres-'+i);await tap('obs-sphere:'+i);await tap('obs-seal');await tap('obs-next');}
 await wait(()=>window.ascendantDial.snapshot().keys===5&&!window.ascendantDial.snapshot().busy);s=await snap();check(s.keyCeremony===5&&s.orreryEvidence5,'Key 5 ceremony, evidence at '+view);await shot('key5');
 await tap('obs-back');await act('walk:atrium-door');await wait(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);await act('walk:chamber-door');await wait(()=>window.ascendantDial.snapshot().room==='chamber'&&!window.ascendantDial.snapshot().busy);await act('walk:books');await wait(()=>window.ascendantDial.snapshot().atBooks&&!window.ascendantDial.snapshot().busy);await act('insert');await wait(()=>window.ascendantDial.snapshot().keysSpent===5&&!window.ascendantDial.snapshot().busy);
 await act('walk:atrium-door');await wait(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);await act('walk:sealed-left');await wait(()=>window.ascendantDial.snapshot().room==='observatory'&&!window.ascendantDial.snapshot().busy);await act('walk:orrery');await wait(()=>window.ascendantDial.snapshot().observatoryPlaying&&!window.ascendantDial.snapshot().busy);await tap('obs-next');
 const rings=[1,2,2,2,3,4,5,6,7,8],bands=[0,0,1,1,1,2,2,3,3,3];
 for(let i=0;i<10;i++){await tap('obs-sphere:'+i);await tap('obs-ring:'+rings[i]);await tap('obs-next');}
 s=await snap();check(s.orreryPhase==='retro'&&s.planetRetro&&s.orreryPlaced.every(Boolean),'all ring placements and retrograde beat at '+view);await shot('retrograde');await tap('obs-next');await tap('obs-next');
 for(let i=0;i<10;i++){await tap('obs-sphere:'+i);await tap('obs-band:'+bands[i]);await tap('obs-next');}
 for(let i=0;i<2;i++){s=await snap();check(s.orreryPhase==='questions','individuality card '+i+' at '+view);await shot('individuality-'+i);await tap('obs-card:'+(s.orreryQuestionOrder||[]).indexOf(0));await tap('obs-next');}
 await wait(()=>window.ascendantDial.snapshot().keys===6&&!window.ascendantDial.snapshot().busy);s=await snap();check(s.keyCeremony===6&&s.orreryEvidence6,'Key 6 ceremony with ring evidence at '+view);await shot('key6');await tap('obs-back');await act('open-journal');await wait(()=>window.ascendantDial.snapshot().journal&&!window.ascendantDial.snapshot().busy);if((await snap()).journalView==='title')await pg.waitForTimeout(2500);
 await act('journal-contents');await act('journal-chapter:planets');await wait(()=>window.ascendantDial.snapshot().journalView==='planets');s=await snap();check(s.planetMet.every(Boolean)&&s.planetRetro,'planet index fills as introduced at '+view);await shot('planet-index');
 for(let i=0;i<10;i++){await tap('planet-page:'+i);s=await snap();check(s.planetPage===i&&s.planetDetails.includes('Work:')&&s.planetDetails.includes('Company:')&&s.planetDetails.includes('Pace:'),'planet journal '+i+' at '+view);await shot('planet-page-'+i);await tap('planet-index');}
 await act('journal-contents');await act('journal-landing');await act('journal-practice');s=await snap();check(s.journalPractice.includes('The Planets')&&s.journalPractice.includes('Pace and Company'),'both learned Practice concepts at '+view);await shot('practice-list');
 await act('close-journal');await act('jump:obswhole');await wait(()=>window.ascendantDial.snapshot().keysSpent===6&&!window.ascendantDial.snapshot().busy);s=await snap();check(s.booksOpen===2&&s.observatoryWhole,'DEV obswhole opens Book 2, other progression stops at '+view);await shot('whole');
 check(errors.length===0,'no browser runtime exceptions at '+view+' '+errors.join('|'));await ctx.close();}
 fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n')+'\n');process.stdout.write(report.join('\n')+'\n');
 }catch(e){fs.writeFileSync(path.join(out,'partial-report.txt'),report.join('\n')+'\nFAIL: '+e.stack);if(pg){await pg.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});fs.writeFileSync(path.join(out,'failure-state.json'),JSON.stringify(await pg.evaluate(()=>window.ascendantDial?.snapshot()).catch(()=>null),null,2));}throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
