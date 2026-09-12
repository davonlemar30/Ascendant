// Run against the real locally served Unity build. Requires Playwright; no production dependency.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs=require('fs');
const path=require('path');
(async()=>{
  const out=process.env.EVIDENCE_DIR || 'Logs/WebEvidence';fs.mkdirSync(out,{recursive:true});
  const browser=await chromium.launch({headless:true,channel:'chrome'});
  const report=[];
  function check(value,text){if(!value)throw Error(text);report.push('PASS: '+text);}
  for(const viewport of [{width:390,height:844},{width:360,height:800}]){
    const context=await browser.newContext({viewport,deviceScaleFactor:1});
    const page=await context.newPage();const events=[],errors=[];
    page.on('pageerror',e=>errors.push(String(e)));
    page.on('console',msg=>{const text=msg.text(),mark=text.indexOf('[CelestialDial] ');if(mark>=0){try{events.push(JSON.parse(text.slice(mark+16)));}catch{}}});
    const state=()=>page.evaluate(()=>window.ascendantDial.snapshot());
    const ready=async()=>page.waitForFunction(()=>window.ascendantDial?.snapshot()?.canContinue,{},{timeout:120000});
    // Unity ignores pointer input in the first frame after a phase transition (the button is activated in
    // that same frame), so settle briefly after the state flips. A person cannot tap that fast.
    // A Level 2 problem shows its count beat one frame after it starts, so wait, let the beat begin, then wait for it to end.
    const waitActive=async(start)=>{const ok=s=>window.ascendantDial.snapshot()?.active && window.ascendantDial.snapshot().start===('Start: '+s);await page.waitForFunction(ok,start,{timeout:15000});await page.waitForTimeout(400);await page.waitForFunction(ok,start,{timeout:20000});await page.waitForTimeout(150);};
    const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await page.mouse.click(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await page.waitForTimeout(70);};
    const semantic=async(id)=>page.locator('#'+id).evaluate(b=>b.click());
    await page.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');
    await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});await page.locator("#loading").waitFor({state:"detached"});
    await page.screenshot({path:path.join(out,viewport.width+'-identity.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll on identity at '+viewport.width);
    await page.locator('#name').fill('Tester');await page.locator('#name').dispatchEvent('change');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
    await page.screenshot({path:path.join(out,viewport.width+'-birth.png')});
    check(await page.evaluate(()=>window.ascendantDial.snapshot().canSliceContinue===false),'birth prompt waits for a choice at '+viewport.width);
    await semantic('birth-known');await page.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);
    await semantic('sign-1');await page.waitForFunction(()=>window.ascendantDial.snapshot().sunSign==='Taurus');
    await semantic('next-screen');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
    await page.screenshot({path:path.join(out,viewport.width+'-atrium.png')});
    for(let n=0;n<8&&(await state()).screen==='atrium';n++){await semantic('next-screen');await page.waitForTimeout(150);}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing');await ready();
    check((await state()).dormant,'the Dial is dormant on arrival at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-encounter.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at '+viewport.width);
    // Seven intro beats, two of them automatic, then the teaching page and the guided problem.
    for(let n=0;n<16&&(await state()).start!=='Start: Taurus';n++){if((await state()).canContinue)await semantic('continue');await page.waitForTimeout(500);}
    await waitActive('Taurus');check(!(await state()).dormant,'the Dial has woken and the guided problem began at '+viewport.width);
    const boxes=await page.locator('#seats button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height,label:b.getAttribute('aria-label')})));
    check(boxes.length===12 && boxes.every(b=>b.width>=48 && b.height>=48 && b.label.includes('position')),'12 semantic seats and effective target floor at '+viewport.width);
    await tap(0,714); // Count during guided Level 2 must not downgrade evidence.
    const scale=Math.min(viewport.width/360,viewport.height/800),cx=viewport.width/2,cy=(viewport.height-800*scale)/2+270*scale;
    await page.mouse.move(cx-100*scale,cy);await page.mouse.down();
    // Four detents of travel: 4 x 55 logical px along a 100 px radius is a 2.2 rad sweep, so snapping lands on the fourth seat.
    for(let i=1;i<=40;i++){const a=Math.PI-2.2*i/40;await page.mouse.move(cx+Math.cos(a)*100*scale,cy-Math.sin(a)*100*scale);await page.waitForTimeout(12);}
    await page.mouse.up();await page.waitForTimeout(180);
    await page.screenshot({path:path.join(out,viewport.width+'-drag.png')});
    fs.writeFileSync(path.join(out,viewport.width+'-drag-state.json'),JSON.stringify({state:await state(),events},null,2));
    check((await state()).destination==='Selected: Virgo','actual pointer drag advances four detents at '+viewport.width);
    check(events.filter(e=>e.event_name==='answer_committed').length===0,'drag and count do not submit at '+viewport.width);
    await tap(0,654);await waitActive('Virgo');
    // Select a destination through the browser semantic path (assistive action simulation).
    await semantic('seat-9');check((await state()).destination==='Selected: Capricorn','semantic direct selection at '+viewport.width);
    await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().canContinue);
    await semantic('continue');await waitActive('Aries');
    for(let n=0;n<5;n++)await tap(122,654);await tap(-122,654);
    await page.screenshot({path:path.join(out,viewport.width+'-steps.png')});
    fs.writeFileSync(path.join(out,viewport.width+'-steps-state.json'),JSON.stringify({state:await state(),events},null,2));
    check((await state()).destination==='Selected: Leo','pointer step overshoot and correction at '+viewport.width);
    await tap(0,654);await waitActive('Leo');
    for(let n=0;n<4;n++)await page.keyboard.press('ArrowRight');
    check((await state()).destination==='Selected: Sagittarius','keyboard steps share selected destination at '+viewport.width);
    await page.locator('#seal').focus();await page.keyboard.press('Space');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyEarned && window.ascendantDial.snapshot().canOptional);
    let final=await state();check(final.seats.filter(s=>!s.includes('dormant')).length===6 && final.keyEarned,'six-seat completion and conditional Key at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyRevealed && window.ascendantDial.snapshot().canSliceContinue,{},{timeout:20000});
    check((await state()).message.startsWith('Aah'),'Dial reveals the Key with the locked reveal line at '+viewport.width);
    check(events.some(e=>e.event_name==='key_revealed'),'key_revealed event at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-complete.png')});
    check(events.filter(e=>e.event_name==='key1_earned').length===1,'one Key event at '+viewport.width);
    check(events.filter(e=>e.event_name==='answer_correct').slice(0,2).every(e=>!e.evidence_eligible),'guided answers stay ineligible at '+viewport.width);
    await semantic('optional');await waitActive('Gemini');
    await semantic('seat-6');await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().canOptional);
    check(events.some(e=>e.event_name==='optional_problem_offered') && events.some(e=>e.event_name==='optional_problem_accepted'),'optional probe events at '+viewport.width);
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atriumreturn');
    for(let n=0;n<4&&(await state()).screen==='atriumreturn';n++){await semantic('next-screen');await page.waitForTimeout(150);}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='chamber');
    check(!(await state()).canInsert,'the Key cannot be inserted before Caspar finishes at '+viewport.width);
    for(let n=0;n<4&&!(await state()).canInsert;n++){await semantic('next-screen');await page.waitForTimeout(150);}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canInsert);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber.png')});
    await semantic('insert');await page.waitForFunction(()=>window.ascendantDial.snapshot().ended,{},{timeout:40000});
    check((await state()).locksFilled===1 && (await state()).caspar.includes('Let us continue, shall we?'),'one Key fills one lock and the amended ending plays at '+viewport.width);
    check(events.some(e=>e.event_name==='key_inserted') && events.some(e=>e.event_name==='prototype_ended'),'chamber events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber-end.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the ending at '+viewport.width);
    // ---- v0.2: the return ----
    const SIGNS=['Aries','Taurus','Gemini','Cancer','Leo','Virgo','Libra','Scorpio','Sagittarius','Capricorn','Aquarius','Pisces'],ELEMENTS=['Fire','Earth','Air','Water'],ELEMENT_OF=i=>ELEMENTS[i%4];
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub');
    check((await state()).atriumStage===2 && (await state()).dueCount===0,'the Chamber leads to the Hub in Stage 2 with nothing due at '+viewport.width);
    // ---- v0.4: tap-to-move ----
    const atriumState=await state();
    check(atriumState.room==='atrium' && atriumState.avatarAt==='entry' && ['desk','wing-door','caspar','sealed-left','sealed-right'].every(id=>atriumState.pois.includes(id)),'the Atrium lists its points of interest with the marker where you came in at '+viewport.width);
    await semantic('poi-sealed-left');await page.waitForFunction(()=>window.ascendantDial.snapshot().note.startsWith('Sealed'));
    check(!(await state()).walking,'a sealed door only says it is sealed at '+viewport.width);
    await semantic('poi-caspar');await page.waitForFunction(()=>window.ascendantDial.snapshot().avatarAt==='caspar'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).note.includes('Caspar') && events.some(e=>e.event_name==='walk_started_caspar') && events.some(e=>e.event_name==='walk_arrived_caspar'),'tapping Caspar walks the marker to him at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the Hub at '+viewport.width);
    await semantic('enter-seals');await page.waitForFunction(()=>window.ascendantDial.snapshot().hubNote.includes('Nothing is due'));
    await semantic('advance-day');await page.waitForFunction(()=>window.ascendantDial.snapshot().dueCount===12);
    await semantic('enter-seals');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='review'&&window.ascendantDial.snapshot().reviewMode==='dial');
    for(let n=0;n<6;n++){
      try{await page.waitForFunction(i=>window.ascendantDial.snapshot().reviewIndex===i&&!window.ascendantDial.snapshot().busy&&(window.ascendantDial.snapshot().reviewMode!=='dial'||window.ascendantDial.snapshot().active),n,{timeout:20000});}
      catch(e){console.error('review loop stalled at item '+n+': '+JSON.stringify(await state()));console.error('last events: '+JSON.stringify(events.slice(-12).map(x=>x.event_name+'@'+x.input_method)));throw e;}
      const s=await state();const seat=SIGNS.indexOf(s.reviewSign);
      if(s.reviewMode==='dial'){await semantic('seat-'+((seat+4)%12));await semantic('seal');}
      else{if(n===1)await page.screenshot({path:path.join(out,viewport.width+'-review-tap.png')});await semantic('element-'+(seat%4));}
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().reviewMode==='done'&&window.ascendantDial.snapshot().canLeaveReview,{},{timeout:20000});
    check((await state()).reviewSummary.startsWith('6 of 6'),'six seals held through compressed Dial and direct tap at '+viewport.width);
    check(events.some(e=>e.event_name==='review_started') && events.some(e=>e.event_name==='review_finished'),'review events at '+viewport.width);
    await semantic('leave-review');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub');
    check((await state()).avatarAt==='desk','the review leaves the marker at the desk at '+viewport.width);
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().walking&&window.ascendantDial.snapshot().walkTarget==='wing-door',{},{timeout:5000});
    await page.screenshot({path:path.join(out,viewport.width+'-walk.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).room==='wing' && (await state()).avatarAt==='atrium-door' && (await state()).pois.join()==='atrium-door,dial' && (await state()).canEnterDial && (await state()).canLeaveWing,'the Wing doorway fades into the Wing room with the Dial and the doorway back at '+viewport.width);
    check(events.some(e=>e.event_name==='room_entered_wing'),'room events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room.png')});
    await semantic('enter-dial');await waitActive('Gemini');
    check((await state()).avatarAt==='dial','the Dial opens once the marker reaches it at '+viewport.width);
    check((await state()).phase.includes('Help level 0'),'Unit 1.1 continues on the player\'s own at '+viewport.width);
    for(const [from,to] of [[2,6],[6,10],[3,7],[7,11]]){await waitActive(SIGNS[from]);await semantic('seat-'+to);await semantic('seal');}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().wheelComplete&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).seats.every(x=>!x.includes('dormant')) && events.filter(e=>e.event_name==='key1_earned').length===1,'twelve seats lit with no second Key at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-lit.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===3&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).v02Complete && (await state()).avatarAt==='wing-door','one more return completes v0.2, the marker back at the Wing doorway at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-complete.png')});
    // ---- v0.3: glyphs and Key 2 ----
    await semantic('enter-wing');
    try{await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});}
    catch(e){console.error('wing button stalled: '+JSON.stringify(await state()));console.error('last events: '+JSON.stringify(events.slice(-14).map(x=>x.event_name+'@'+x.input_method)));throw e;}
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    check(!!(await state()).glyphChar && (await state()).glyphOptions.length===4,'Part A shows a mark and four names at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-a.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
      const s=await state();
      const target=SIGNS.findIndex(name=>s.glyphChar===['\u2648','\u2649','\u264A','\u264B','\u264C','\u264D','\u264E','\u264F','\u2650','\u2651','\u2652','\u2653'][SIGNS.indexOf(name)]);
      const slot=s.glyphOptions.indexOf(SIGNS[target]);
      await semantic('glyph-name-'+slot);await page.waitForTimeout(200);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active,{},{timeout:20000});
    check((await state()).namesHidden && (await state()).seats.every(x=>x.startsWith('Mark')),'Part B hides every name, labels included, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-b.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
      const target=SIGNS.indexOf((await state()).glyphTarget);
      await semantic('seat-'+target);await semantic('seal');await page.waitForTimeout(300);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key2&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).keys===2 && events.filter(e=>e.event_name==='key2_earned').length===1,'twelve marks placed earns Key 2 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-key2.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===4&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).v03Complete,'one more return completes v0.3 at '+viewport.width);
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).atriumStage===4 && (await state()).keys===2 && (await state()).avatarAt==='entry','a reload resumes at the Hub from the local save with Key 2 at '+viewport.width);
    await semantic('restart');await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity'&&!window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check(true,'Start over wipes the save at '+viewport.width);
    check(errors.length===0,'no browser runtime exceptions at '+viewport.width);
    fs.writeFileSync(path.join(out,viewport.width+'-events.json'),JSON.stringify(events,null,2));
    await context.close();
  }
  const recoveryContext=await browser.newContext({viewport:{width:390,height:844},reducedMotion:'reduce'});
  const recovery=await recoveryContext.newPage();
  await recovery.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');
  await recovery.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});
  const action=async(id)=>recovery.locator('#'+id).evaluate(b=>b.click());
  await action('next-screen');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
  await action('birth-unknown');check(await recovery.evaluate(()=>window.ascendantDial.snapshot().note.includes('Your sun sign is')&&window.ascendantDial.snapshot().sunSign!==''),'I don\'t know assigns a sun sign');
  await action('change-birth');await action('birth-chart');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthDate);
  await recovery.locator('#birthdate').fill('1990-05-01');await recovery.locator('#birthdate').dispatchEvent('change');
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().sunSign==='Taurus');check(true,'a birth date derives the sun sign in the browser');
  await action('next-screen');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
  for(let n=0;n<8&&(await recovery.evaluate(()=>window.ascendantDial.snapshot().screen))==='atrium';n++){await action('next-screen');await recovery.waitForTimeout(150);}
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canContinue);
  const active=async(sign)=>{const ok=s=>window.ascendantDial.snapshot().active && window.ascendantDial.snapshot().start==='Start: '+s;await recovery.waitForFunction(ok,sign,{timeout:15000});await recovery.waitForTimeout(400);await recovery.waitForFunction(ok,sign,{timeout:20000});await recovery.waitForTimeout(150);};
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().reducedMotion),'OS reduced-motion preference reaches Unity');
  for(let n=0;n<16&&(await recovery.evaluate(()=>window.ascendantDial.snapshot().start))!=='Start: Taurus';n++){if(await recovery.evaluate(()=>window.ascendantDial.snapshot().canContinue))await action('continue');await recovery.waitForTimeout(500);}
  await active('Taurus');
  await action('seat-5');await action('seal');await active('Virgo');
  await action('seat-9');await action('seal');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canContinue);
  await action('continue');await active('Aries');
  await action('seal');
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().destination==='Selected: Aries' && window.ascendantDial.snapshot().phase.includes('Help level 1')),'first browser rejection stays in place at Level 1');
  await action('seal');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().phase.includes('Help level 2'));
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});await action('count');
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().destination==='Selected: Aries' && window.ascendantDial.snapshot().phase.includes('Help level 2')),'second browser rejection shows the count once, ring returns, Count preserves Level 2');
  await recovery.screenshot({path:path.join(out,'390-rejected.png')});
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});await action('seal');await active('Leo');
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().phase.includes('Help level 0')),'Level 3 demo resets to a fresh Level 0 problem');
  const sealWrongThrice=async()=>{for(let n=0;n<3;n++){await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});await action('seal');await recovery.waitForTimeout(200);}};
  await sealWrongThrice();await active('Sagittarius');
  await sealWrongThrice();
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().phase.includes('Paused for now'),{},{timeout:30000});
  check(await recovery.evaluate(()=>!window.ascendantDial.snapshot().active && !window.ascendantDial.snapshot().keyEarned),'browser recovery cap pauses without awarding Key');
  await recovery.screenshot({path:path.join(out,'390-recovery-cap.png')});await recoveryContext.close();
  fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
