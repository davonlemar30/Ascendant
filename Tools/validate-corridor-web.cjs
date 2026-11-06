// Goal 03: phone-density Corridor controls, Key evidence, room/restoration, journal and Practice.
const fs=require('fs'),path=require('path'),pw=require(process.env.PLAYWRIGHT_MODULE||'playwright');
(async()=>{
 const out=process.env.EVIDENCE_DIR||'Logs/CorridorEvidence';fs.mkdirSync(out,{recursive:true});fs.rmSync(path.join(out,'validation.txt'),{force:true});
 const browser=await pw.chromium.launch({headless:true,channel:'chrome'}),report=[];let pg;
 const check=(ok,line)=>{if(!ok)throw Error(line);report.push('PASS: Corridor: '+line);};
 try{
 const ctx=await browser.newContext({viewport:{width:360,height:800},deviceScaleFactor:2,isMobile:true,hasTouch:true});pg=await ctx.newPage();const errors=[];pg.on('pageerror',e=>{errors.push(e.message);fs.appendFileSync(path.join(out,'runtime-errors.txt'),e.stack+'\n');});
 await pg.goto(process.env.GREYBOX_URL||'http://127.0.0.1:8791');await pg.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});
 const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot()),act=async c=>{await pg.evaluate(x=>window.ascendantDial.act(x),c);await pg.waitForTimeout(100);};
 const wait=async f=>pg.waitForFunction(f,{},{timeout:30000});
 const idle=()=>wait(()=>!window.ascendantDial.snapshot().busy);
 const shot=async n=>{fs.writeFileSync(path.join(out,n+'.json'),JSON.stringify(await snap(),null,2));await pg.screenshot({path:path.join(out,n+'.png'),scale:'css'});};
 const tap=async id=>{await idle();const el=pg.locator('#control-'+id.replace(':','-'));await el.waitFor({state:'attached'});check(await el.evaluate(e=>!e.disabled),'native control enabled '+id);const r=await el.evaluate(e=>({x:parseFloat(e.style.left),y:parseFloat(e.style.top),w:parseFloat(e.style.width),h:parseFloat(e.style.height)}));check(r.w>=43.9&&r.h>=43.9,'44px target '+id);await pg.mouse.click(r.x+r.w/2,r.y+r.h/2);await pg.waitForTimeout(100);};
 const walk=async (poi,room)=>{await idle();await act('walk:'+poi);await wait(()=>!window.ascendantDial.snapshot().busy);check((await snap()).room===room,'walk '+poi+' reaches '+room);};
 const spend=async expected=>{await walk('observatory-door','observatory');await walk('atrium-door','atrium');await walk('chamber-door','chamber');await act('walk:books');await idle();await act('insert');await idle();check((await snap()).keysSpent===expected,'spend Key '+expected);await shot('lock-'+expected);await walk('atrium-door','atrium');};
 const enter=async()=>{await walk('sealed-left','observatory');await wait(()=>window.ascendantDial.snapshot().corridorOpen&&!window.ascendantDial.snapshot().busy);await shot('back-arch');await tap('cor-door');await idle();check((await snap()).room==='corridor','back arch reaches Corridor');};
 await act('jump:obswhole');await idle();check((await snap()).keysSpent===6,'starts from DEV obswhole, six spent Keys');await act('motion-on');await enter();await shot('dormant-room');await act('walk:rulership-wheel');await idle();check((await snap()).corridorPlaying,'walk to wheel opens instrument');await shot('traditional-intro');await tap('cor-next');
 const pairs=[[3],[4],[2,5],[1,6],[0,7],[8,11],[9,10]],signNames=['Aries','Taurus','Gemini','Cancer','Leo','Virgo','Libra','Scorpio','Sagittarius','Capricorn','Aquarius','Pisces'];
 // First pass: every placement revealed, proving the Key stays locked. Includes native drag.
 for(let planet=0;planet<7;planet++)for(const sign of pairs[planet]){
  const wrong=signNames.findIndex((_,s)=>!pairs[planet].includes(s));
  for(let miss=1;miss<=3;miss++){await tap('cor-sign:'+wrong);check((await snap()).corridorHint===miss,'placement hint '+miss+' for '+planet);if(planet===0)await shot('traditional-hint-'+miss);}
  await tap('cor-next');
 }
 check((await snap()).keys===6&&(await snap()).corridorPhase==='paused7'&&!(await snap()).corridorEvidence7,'all revealed placements pause Key 7');await shot('paused-key7');await tap('cor-next');await tap('cor-next');
 let dragDone=false;
 for(let planet=0;planet<7;planet++)for(const sign of pairs[planet]){
  check((await snap()).corridorPlanet===planet,'classical tray order '+planet);
  if(!dragDone){
   const r=await pg.locator('#control-cor-planet-'+planet).evaluate(e=>({x:parseFloat(e.style.left),y:parseFloat(e.style.top),w:parseFloat(e.style.width),h:parseFloat(e.style.height)}));const d=await pg.locator('#control-cor-sign-'+sign).evaluate(e=>({x:parseFloat(e.style.left),y:parseFloat(e.style.top),w:parseFloat(e.style.width),h:parseFloat(e.style.height)}));
   await pg.mouse.move(r.x+r.w/2,r.y+r.h/2);await pg.mouse.down();await pg.waitForTimeout(150);await pg.mouse.move(d.x+d.w/2,d.y+d.h/2,{steps:12});await pg.waitForTimeout(150);await pg.mouse.up();await pg.waitForTimeout(200);check((await snap()).corridorTraditional[sign],'native drag seats Moon in Cancer');dragDone=true;
  }else await tap('cor-sign:'+sign);
  check((await snap()).corridorTraditional[sign],'traditional place '+signNames[sign]);await shot('traditional-'+sign);await tap('cor-next');
 }
 await idle();let s=await snap();check(s.keys===7&&s.corridorEvidence7&&s.keyCeremony===7,'independent work earns Key 7 with shipped ceremony');await shot('key7');await tap('cor-back');await shot('restored-key7');await spend(7);await enter();await act('walk:rulership-wheel');await idle();await shot('modern-intro');await tap('cor-next');
 for(const sign of [10,11,7]){await shot('modern-'+sign);await tap('cor-sign:'+sign);await tap('cor-next');}
 await shot('detriment-intro');await tap('cor-next');
 // Every detriment helped first, then an independent replay. The correct opposite is computed from the named seed sign.
 const detriments=async assisted=>{for(let i=0;i<6;i++){
  s=await snap();check(s.corridorPlanet>=0&&s.corridorPlanet<=6&&s.corridorRuledSign>=0,'only classical detriment '+i);const answer=(s.corridorRuledSign+6)%12;
  if(assisted){await tap('cor-ask');check((await snap()).corridorHint===2,'Ask Caspar moves to level 2');if(i===0)await shot('detriment-rule');}
  for(let guard=0;(await snap()).corridorTurn!==answer&&guard<12;guard++)await tap('cor-right');
  check((await snap()).corridorTurn===answer,'turn to opposite of '+signNames[s.corridorRuledSign]);await shot((assisted?'assisted-':'')+'detriment-'+i);await tap('cor-seal');await tap('cor-next');
 }};
 const cards=async suffix=>{await shot('cards-intro'+suffix);await tap('cor-next');const correct=['Mercury: to think; Air; Mutable.','Venus: to value; Earth; Fixed.','Mars: to act; Water; Fixed.'];for(let i=0;i<3;i++){await shot('ruler-card-'+i+suffix);s=await snap();const ids=s.corridorControlIds,labels=s.corridorControlLabels,slot=labels.indexOf(correct[i]);check(slot>=0,'guided ruler card has correct factual choice '+i);await tap(ids[slot]);await shot('ruler-why-'+i+suffix);await tap('cor-next');}};
 await detriments(true);await cards('-assisted');s=await snap();check(s.keys===7&&s.corridorPhase==='paused8'&&!s.corridorEvidence8,'all helped detriments pause Key 8');await shot('paused-key8');await tap('cor-next');await tap('cor-next');await detriments(false);await cards('');await idle();s=await snap();check(s.keys===8&&s.corridorEvidence8&&s.keyCeremony===8,'independent detriment earns Key 8 with ceremony');await shot('key8');await tap('cor-back');await shot('restored-key8');
 await act('open-journal');await idle();if((await snap()).journalView==='title')await pg.waitForTimeout(2500);await act('journal-contents');await act('journal-chapter:wheel');await act('journal-seat:7');await act('journal-open');await shot('journal-rulers');s=await snap();check((s.caspar||'').includes('Mars')&&(s.caspar||'').includes('Pluto'),'Scorpio journal shows traditional and modern rulers');
 await act('journal-wheel');await act('journal-contents');await act('journal-landing');await act('journal-practice');s=await snap();check(s.journalPractice.includes('Rulers')&&s.journalPractice.includes('Detriment and modern rulers'),'both new Practice concepts unlocked');await shot('practice-list');
 for(const concept of [7,8]){await act('practice-row:'+concept);for(let i=0;i<6;i++){s=await snap();check(s.canQuizChoice&&s.quizChoices.length===4,'Practice '+concept+'/'+i);await shot('practice-'+concept+'-'+i);const r=s.quizChoiceBoxes;await pg.mouse.click(180+r[0],r[1]);await pg.waitForTimeout(100);s=await snap();check(s.canQuizNext&&s.quizFeedback.length>0,'Practice why '+concept+'/'+i);await act('quiz-next');}check((await snap()).quizOver,'six questions finish '+concept);await act('quiz-back');}
 await act('close-journal');await spend(8);s=await snap();check(s.keysSpent===8&&s.booksOpen===2&&s.observatoryLevel===4,'lock 8 and fully lit lower Observatory; third Book waits for Key 9');await shot('end-hub');
 check(errors.length===0,'no browser runtime exceptions: '+errors.join('|'));fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n')+'\n');process.stdout.write(report.join('\n')+'\n');await ctx.close();
 }catch(e){fs.writeFileSync(path.join(out,'partial-report.txt'),report.join('\n')+'\nFAIL: '+e.stack);if(pg){await pg.screenshot({path:path.join(out,'failure.png'),scale:'css'}).catch(()=>{});fs.writeFileSync(path.join(out,'failure-state.json'),JSON.stringify(await pg.evaluate(()=>window.ascendantDial?.snapshot()).catch(()=>null),null,2));}throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
