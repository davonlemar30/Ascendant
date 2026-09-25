// Run against the real locally served Unity build. Requires Playwright; no production dependency.
const playwright=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const engine=process.env.BROWSER==='webkit' ? playwright.webkit : playwright.chromium; // BROWSER=webkit approximates iPhone browsers, which all run WebKit
const fs=require('fs');
const path=require('path');
(async()=>{
  const out=process.env.EVIDENCE_DIR || 'Logs/WebEvidence';fs.mkdirSync(out,{recursive:true});
  const browser=await engine.launch(process.env.BROWSER==='webkit' ? {headless:true} : {headless:true,channel:'chrome'});
  const report=[];
  function check(value,text){if(!value)throw Error(text);report.push('PASS: '+text);}
  for(const viewport of [{width:390,height:844},{width:360,height:800}]){
    const context=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE}); // DEVICE_SCALE=2 MOBILE=1 approximates a phone
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
    check((await state()).artSet===''&&!(await state()).style&&(await state()).lastCue==='page','no query: the game plays on the Art folder, no style page; the page hook fired on Caspar\'s pages, file or not, at '+viewport.width); // Build E
    await page.screenshot({path:path.join(out,viewport.width+'-encounter.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at '+viewport.width);
    // Seven intro beats, two of them automatic, then the teaching page and the guided problem.
    for(let n=0;n<16&&(await state()).start!=='Start: Taurus';n++){if((await state()).canContinue)await semantic('continue');await page.waitForTimeout(500);}
    await waitActive('Taurus');check(!(await state()).dormant,'the Dial has woken and the guided problem began at '+viewport.width);
    const boxes=await page.locator('#seats button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height,label:b.getAttribute('aria-label')})));
    check(boxes.length===12 && boxes.every(b=>b.width>=48 && b.height>=48 && b.label.includes('position')),'12 semantic seats and effective target floor at '+viewport.width);
    check((await state()).challenge==='Next Earth after Taurus' && (await page.locator('#count').count())===0,'the wheel shows its own challenge and there is no Count button (Build I) at '+viewport.width);
    const scale=Math.min(viewport.width/360,viewport.height/800),cx=viewport.width/2,cy=(viewport.height-800*scale)/2+270*scale;
    await page.mouse.move(cx-100*scale,cy);await page.mouse.down();
    // Four detents of travel: 4 x 55 logical px along a 100 px radius is a 2.2 rad sweep, so snapping lands on the fourth seat.
    for(let i=1;i<=40;i++){const a=Math.PI-2.2*i/40;await page.mouse.move(cx+Math.cos(a)*100*scale,cy-Math.sin(a)*100*scale);await page.waitForTimeout(12);}
    await page.mouse.up();await page.waitForTimeout(180);
    await page.screenshot({path:path.join(out,viewport.width+'-drag.png')});
    fs.writeFileSync(path.join(out,viewport.width+'-drag-state.json'),JSON.stringify({state:await state(),events},null,2));
    check((await state()).destination==='Selected: Virgo','actual pointer drag advances four detents at '+viewport.width);
    check(events.filter(e=>e.event_name==='answer_committed').length===0,'a drag does not submit at '+viewport.width);
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
    for(let n=0;n<4&&!(await state()).canInsert;n++){await tap(0,654);await page.waitForTimeout(200);} // the visible Continue on the canvas, where a thumb lands
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canInsert,{},{timeout:5000});
    check(true,'a visible Continue turns Caspar\'s pages in the Chamber at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber.png')});
    await semantic('insert');await page.waitForFunction(()=>window.ascendantDial.snapshot().ended,{},{timeout:40000});
    check((await state()).locksFilled===1 && (await state()).caspar.includes('Let us continue, shall we?'),'one Key fills one lock and the amended ending plays at '+viewport.width);
    check(events.some(e=>e.event_name==='key_inserted') && events.some(e=>e.event_name==='prototype_ended'),'chamber events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber-end.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the ending at '+viewport.width);
    // ---- v0.2: the return ----
    const SIGNS=['Aries','Taurus','Gemini','Cancer','Leo','Virgo','Libra','Scorpio','Sagittarius','Capricorn','Aquarius','Pisces'],ELEMENTS=['Fire','Earth','Air','Water'],ELEMENT_OF=i=>ELEMENTS[i%4];
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub');
    check((await state()).atriumStage===2 && (await state()).dueCount>=6,'the Chamber leads to the Hub in Stage 2 with twelve items ready for practice at once, no in-game time, at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().lightAlpha===0.25,{},{timeout:5000}); // the fade settles, then publishes
    check((await state()).lightAlpha===0.25,'the light overlay follows the stage: a quarter at Stage 2 at '+viewport.width); // Build H
    // ---- v0.4: tap-to-move ----
    const atriumState=await state();
    check(atriumState.room==='atrium' && atriumState.avatarAt==='entry' && ['desk','wing-door','caspar','sealed-left','chamber-door'].every(id=>atriumState.pois.includes(id)) && atriumState.canEnterChamber && atriumState.keysInHand===0,'the Atrium lists its points of interest, the Chamber doorway among them, with the marker where you came in at '+viewport.width);
    await semantic('poi-sealed-left');await page.waitForFunction(()=>window.ascendantDial.snapshot().note.startsWith('Sealed'));
    check(!(await state()).walking,'a sealed door only says it is sealed at '+viewport.width);
    await semantic('poi-caspar');await page.waitForFunction(()=>window.ascendantDial.snapshot().avatarAt==='caspar'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).note.includes('Caspar') && events.some(e=>e.event_name==='walk_started_caspar') && events.some(e=>e.event_name==='walk_arrived_caspar'),'tapping Caspar walks the marker to him at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the Hub at '+viewport.width);
    await semantic('mute');await page.waitForFunction(()=>window.ascendantDial.snapshot().muted);check((await page.locator('#mute').getAttribute('aria-pressed'))==='true','the test sound toggle mutes from the Atrium and says so at '+viewport.width); // Build E
    await semantic('mute');await page.waitForFunction(()=>!window.ascendantDial.snapshot().muted);
    // ---- Build F: the desk is dressing, the journal is in the inventory, the fork is on the Dial, practice is the sitting ----
    check((await page.locator('#enter-seals').count())===0 && (await page.locator('#open-journal').count())===1,'Check the Seals is gone from the page; the journal has a control at '+viewport.width);
    await semantic('poi-desk');await page.waitForFunction(()=>window.ascendantDial.snapshot().avatarAt==='desk'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).note.includes('journal') && (await state()).canOpenJournal,'the desk only speaks; the journal is in hand at the Atrium at '+viewport.width);
    await semantic('open-journal');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='journal',{},{timeout:5000});
    { const j=await state(); check(j.journal && j.journalEntries.length===12 && j.journalEntries[0].startsWith('Aries — Fire') && !j.canJournalNext && !j.canJournalPrev && j.journalSection==='The elements' && j.caspar.includes('journal'),'the journal opens from the Atrium on twelve element entries, one section so far, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll in the journal at '+viewport.width);
    await tap(0,714); // Close the journal, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:5000});
    check((await state()).avatarAt==='desk','the journal closes back to the Atrium, the marker where it stood, on a canvas tap at '+viewport.width);
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().walking&&window.ascendantDial.snapshot().walkTarget==='wing-door',{},{timeout:5000});
    await page.screenshot({path:path.join(out,viewport.width+'-walk.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).room==='wing' && (await state()).avatarAt==='atrium-door' && (await state()).pois.join()==='atrium-door,grid,dial,shelf' && (await state()).canEnterDial && !(await state()).canEnterShelf && !(await state()).canEnterGrid && (await state()).canLeaveWing,'the Wing doorway fades into the Wing room with the Dial, the doorway back, a dark shelf, and a dark table at '+viewport.width);
    { const k=await state(); check(k.kitPieces===19 && k.kitLevel===k.keys && k.kitRestored<k.kitPieces && k.grime>0 && Math.abs(k.wingLight-[0,.25,.5,.75,1][k.keys])<.01,'Build M: the Wing kit shows the Keys earned so far, worn pieces and grime still in place, the light by Keys at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-wing-kit-early.png')}); }
    check(events.some(e=>e.event_name==='room_entered_wing'),'room events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room.png')});
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().caspar.includes('Dark and quiet'),{},{timeout:5000});
    check(!(await state()).kitUp.includes('shelf') && !(await state()).kitUp.includes('table'),'Build M: before they wake, the shelf and the table lie worn at '+viewport.width);
    check(!(await state()).walking,'before the wheel is lit the shelf only says it is dark at '+viewport.width);
    await semantic('poi-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().caspar.includes('dark and bare'),{},{timeout:5000});
    check(!(await state()).walking && !(await state()).gridOpen,'before the modality unit the table only says it is bare at '+viewport.width);
    await semantic('enter-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveWing,{},{timeout:15000}); // the wheel's Back button appears a frame after the screen
    check((await state()).avatarAt==='dial' && (await state()).canEnterPractice && (await state()).canContinueLesson && !(await state()).active && (await state()).canLeaveWing,'the Dial opens once the marker reaches it, on the fork: the lesson or practice, nothing started, Back still offered, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-fork.png')});
    await tap(78,714); // Practice what you know, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice'&&window.ascendantDial.snapshot().practiceMode==='dial',{},{timeout:10000});
    check((await state()).sitting===1 && (await state()).practiceCount===6 && (await state()).phase==='Practice · 1 of 6' && (await state()).canLeavePractice && !(await state()).canLeaveWing,'a canvas tap on the fork opens practice as the first sitting: six items, headed as practice, an exit on the item, no Back over the Seal at '+viewport.width);
    let reloadedMidPractice=false;
    const practiceItem=async(n,wrong)=>{
      try{await page.waitForFunction(i=>window.ascendantDial.snapshot().practiceIndex===i&&!window.ascendantDial.snapshot().busy&&(window.ascendantDial.snapshot().practiceMode!=='dial'||window.ascendantDial.snapshot().active),n,{timeout:20000});}
      catch(e){console.error('practice loop stalled at item '+n+': '+JSON.stringify(await state()));console.error('last events: '+JSON.stringify(events.slice(-12).map(x=>x.event_name+'@'+x.input_method)));throw e;}
      const s=await state();const seat=SIGNS.indexOf(s.practiceSign);
      if(s.practiceMode==='dial'){await semantic('seat-'+((seat+(s.step||4)+(wrong?1:0))%12));await semantic('seal');}
      else if(s.practiceMode==='modality'){await semantic('modality-'+((seat+(wrong?1:0))%3));}
      else if(s.practiceMode==='glyph'){const opts=s.glyphOptions;await semantic('glyph-name-'+(wrong?(opts.indexOf(SIGNS[seat])+1)%4:opts.indexOf(SIGNS[seat])));}
      else{await semantic('element-'+((seat+(wrong?1:0))%4));}
      await page.waitForTimeout(250);
    };
    for(let n=0;n<6;n++){
      await page.waitForFunction(i=>window.ascendantDial.snapshot().practiceIndex===i&&!window.ascendantDial.snapshot().busy&&(window.ascendantDial.snapshot().practiceMode!=='dial'||window.ascendantDial.snapshot().active),n,{timeout:20000});
      const s=await state();const seat=SIGNS.indexOf(s.practiceSign);
      if(s.practiceMode==='dial'){
        await semantic('seat-'+((seat+(s.step||4))%12));
        if(n===0){ // the owner's thumb path: the Seal on the canvas must not be covered by the Wing's Back button
          if(!reloadedMidPractice){reloadedMidPractice=true;const before=(await state()).dueCount;await page.screenshot({path:path.join(out,viewport.width+'-practice-dial.png')});await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
            check((await state()).dueCount===before && (await state()).sitting===1,'a reload during practice keeps the deck, its ready count, and the sitting at '+viewport.width);
            await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
            await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
            await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice'&&window.ascendantDial.snapshot().practiceMode==='dial',{},{timeout:15000});
            check((await state()).sitting===2,'practice after the reload is the second sitting at '+viewport.width);
            await page.waitForFunction(()=>window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});await semantic('seat-'+((seat+(s.step||4))%12));await page.waitForTimeout(150);}
          const committed=events.filter(e=>e.event_name==='answer_committed').length;await tap(0,654);await page.waitForTimeout(400);
          check(events.filter(e=>e.event_name==='answer_committed').length===committed+1,'the practice\'s Seal answers a canvas tap; nothing covers it at '+viewport.width);
        } else await semantic('seal');
      }
      else if(s.practiceMode==='modality'){await semantic('modality-'+(seat%3));}
      else{if(n===1)await page.screenshot({path:path.join(out,viewport.width+'-practice-tap.png')});await semantic('element-'+(seat%4));}
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().practiceMode==='done'&&window.ascendantDial.snapshot().canLeavePractice,{},{timeout:20000});
    check((await state()).practiceSummary.startsWith('6 of 6'),'six remembered through compressed Dial and direct tap at '+viewport.width);
    check(events.some(e=>e.event_name==='practice_started') && events.some(e=>e.event_name==='practice_finished') && events.some(e=>e.event_name==='sitting_2'),'practice and sitting events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-practice-done.png')});
    await semantic('leave-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:10000});
    // the other six at the third sitting; nothing due at the fourth still counts; three wrong answers at the fifth close the instrument; the journal from the room; re-entry with fresh strikes
    await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice',{},{timeout:10000});
    check((await state()).sitting===3 && (await state()).practiceCount===6,'the other six are due at the third sitting at '+viewport.width);
    for(let n=0;n<6;n++)await practiceItem(n,false);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().practiceMode==='done'&&window.ascendantDial.snapshot().canLeavePractice,{},{timeout:20000}); // the last answer's beat must settle before the exit is taken
    await semantic('leave-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:10000});
    await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().sitting===4&&!window.ascendantDial.snapshot().busy,{},{timeout:10000});
    check((await state()).screen==='wing' && (await state()).message.startsWith('Nothing is ready') && (await state()).fork==='both','with every item scheduled ahead the entry still counts a sitting; Caspar says nothing is due; the fork stays at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-nothing-due.png')});
    await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice'&&window.ascendantDial.snapshot().sitting===5,{},{timeout:10000});
    check((await state()).strikes===0 && (await state()).practiceCount===6,'one sitting on, six items are due again, fresh strikes at '+viewport.width);
    for(let n=0;n<3;n++){
      await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy&&!window.ascendantDial.snapshot().gated&&window.ascendantDial.snapshot().screen==='practice'&&(window.ascendantDial.snapshot().practiceMode!=='dial'||window.ascendantDial.snapshot().active),{},{timeout:20000});
      const s=await state();const seat=SIGNS.indexOf(s.practiceSign);
      if(s.practiceMode==='dial'){await semantic('seat-'+((seat+(s.step||4)+1)%12));await semantic('seal');}
      else if(s.practiceMode==='modality')await semantic('modality-'+((seat+1)%3));
      else await semantic('element-'+((seat+1)%4));
      await page.waitForTimeout(300);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&window.ascendantDial.snapshot().gated&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    check((await state()).strikes===3 && (await state()).caspar.startsWith('Three misses') && (await state()).canOpenJournal && events.some(e=>e.event_name==='practice_gated'),'the third wrong answer closes the instrument into the room, the journal offered, nobody locked out, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-gate.png')});
    await tap(0,774); // Your journal, on the canvas, from the Wing room
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='journal',{},{timeout:5000});
    await semantic('close-journal');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().gated&&!window.ascendantDial.snapshot().busy,{},{timeout:5000});
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice',{},{timeout:10000});
    check((await state()).strikes===0 && (await state()).sitting===6,'re-entry is immediate with fresh strikes and a new sitting at '+viewport.width);
    await semantic('leave-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canLeaveWing&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // one press from the Dial walks out through the room
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await waitActive('Gemini');
    check((await state()).avatarAt==='dial' && (await state()).fork==='none','the lesson continues from the fork at '+viewport.width);
    check((await state()).hintLevel===0 && !(await state()).phase.includes('Help level'),'Unit 1.1 continues on the player\'s own, no level numbers on screen, at '+viewport.width);
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
    check((await state()).canEnterShelf && (await state()).pois.includes('shelf'),'the lit wheel wakes the bookshelf as a point of interest at '+viewport.width);
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).glyphMode==='' && (await state()).message.includes('book upon the shelf'),'the Dial before the book only points at the shelf at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveWing,{},{timeout:15000}); // the wheel's Back button appears a frame after the screen
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).kitUp.includes('shelf'),'Build M: the shelf stands restored once the book of symbols wakes, before its lesson at '+viewport.width);
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='book'&&window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    check(!!(await state()).glyphChar && (await state()).glyphOptions.length===4,'the book opens Part A: a symbol and four names at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-a.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
      const s=await state();
      const target=SIGNS.findIndex(name=>s.glyphChar===['\u2648','\u2649','\u264A','\u264B','\u264C','\u264D','\u264E','\u264F','\u2650','\u2651','\u2652','\u2653'][SIGNS.indexOf(name)]);
      const slot=s.glyphOptions.indexOf(SIGNS[target]);
      await semantic('glyph-name-'+slot);await page.waitForTimeout(200);
      if(n===4){
        await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy);
        await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
        await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        check((await state()).glyphChar==='\u264D','mid-naming reload resumes at Virgo after five committed cards at '+viewport.width);
        await page.screenshot({path:path.join(out,viewport.width+'-symbol-naming-restored.png')});
      }
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    check((await state()).glyphWheel,'the twelfth name closes the book and returns to the room at '+viewport.width);
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active,{},{timeout:20000});
    check((await state()).namesHidden && (await state()).seats.every(x=>x.startsWith('Symbol')),'Part B hides every name, labels included, at '+viewport.width);
    await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // the Part A hold ends, then the wheel's labels refresh
    { const b=await state(); check(!SIGNS.some(n=>b.destination.includes(n)) && b.destination.startsWith('Selected: symbol ') && b.start==='' && b.count==='' && b.challenge===b.glyphTarget,'Part B readouts do not name the sign under the bracket; the center holds the target name (Build I) at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-b.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
      const target=SIGNS.indexOf((await state()).glyphTarget);
      await semantic('seat-'+target);await semantic('seal');await page.waitForTimeout(300);
      if(n===4){
        await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy);
        await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
        await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        check((await state()).glyphTarget==='Virgo','mid-placement reload resumes at Virgo after five committed placements at '+viewport.width);
        await page.screenshot({path:path.join(out,viewport.width+'-symbol-placement-restored.png')});
      }
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyCeremony===2,{},{timeout:20000});await page.waitForTimeout(950);await page.screenshot({path:path.join(out,viewport.width+'-key2-ceremony.png')}); // Build I: the Key rises
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key2&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).keys===2 && events.filter(e=>e.event_name==='key2_earned').length===1,'twelve symbols placed earns Key 2 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-key2.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    // ---- Build D: the finished loop. Keys earned are spent in the Chamber; the Atrium restores on the return. ----
    const spendAtBooks=async(tag)=>{
      await semantic('poi-chamber-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='chamberroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
      check((await state()).room==='chamber' && (await state()).avatarAt==='atrium-door' && (await state()).pois.join()==='atrium-door,books' && !(await state()).canInsert,'the Chamber doorway fades into the Chamber as a room ('+tag+') at '+viewport.width);
      await semantic('poi-books');await page.waitForFunction(()=>window.ascendantDial.snapshot().atBooks&&window.ascendantDial.snapshot().canInsert,{},{timeout:15000});
      const before=(await state()).keysSpent;await page.waitForTimeout(300);await tap(0,654); // the Insert button on the canvas, where a thumb lands
      await page.waitForFunction(k=>window.ascendantDial.snapshot().keysSpent===k+1,before,{},{timeout:5000});
      await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    };
    check((await state()).atriumStage===3 && (await state()).keysInHand===1 && (await state()).caspar.includes('carry a Key'),'one more return: Key 2 in hand, the Atrium waits for it to be spent at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key-in-hand.png')});
    await spendAtBooks('Key 2');
    check((await state()).keysSpent===2 && (await state()).keysInHand===0 && (await state()).booksOpen===0 && !(await state()).canInsert && events.filter(e=>e.event_name==='key_spent').length===1,'Key 2 fills the second lock; the Book stays shut; nothing more to spend at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber-lock2.png')});
    await semantic('leave-chamber');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===4&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).v03Complete && (await state()).avatarAt==='chamber-door' && (await state()).caspar.includes('Two locks filled'),'the return after spending takes the Atrium to Stage 4 at '+viewport.width);
    // ---- v0.3 revision, build 3: a clean replay hardens the symbols ----
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='book'&&window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    check((await state()).practice && !(await state()).hard,'after Key 2 the book tests again, in order the first time, at '+viewport.width);
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
      const s=await state();const target=['\u2648','\u2649','\u264A','\u264B','\u264C','\u264D','\u264E','\u264F','\u2650','\u2651','\u2652','\u2653'].indexOf(s.glyphChar);
      await semantic('glyph-name-'+s.glyphOptions.indexOf(SIGNS[target]));await page.waitForTimeout(200);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active,{},{timeout:20000});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
      const target=SIGNS.indexOf((await state()).glyphTarget);
      await semantic('seat-'+target);await semantic('seal');await page.waitForTimeout(300);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().cleanRuns===1&&!window.ascendantDial.snapshot().practice&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).hard && (await state()).keys===2 && events.filter(e=>e.event_name==='key2_earned').length===1 && events.some(e=>e.event_name==='symbol_practice_clean'),'a clean replay is recorded once and the next one is hard; Key 2 is not re-earned at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-practice-clean.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='book'&&window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    { const h=await state(); check(h.hard && h.glyphChar!=='\u2648' && new Set(h.glyphOptions).size===4,'the hard replay shuffles the order and keeps four distinct names at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-practice-hard.png')});
    await semantic('close-book');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    // ---- Build A: the modalities on the Dial ----
    check((await state()).dialGlow===1,'with Key 2 in hand the Dial glows in the Wing room: a unit waits there (Build I) at '+viewport.width);await page.screenshot({path:path.join(out,viewport.width+'-wing-room-dial-glow.png')});
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await page.waitForFunction(()=>window.ascendantDial.snapshot().unit==='modalities'&&window.ascendantDial.snapshot().active,{},{timeout:15000});
    check((await state()).step===3 && (await state()).start==='Start: Taurus' && (await state()).message.includes('second pattern'),'after Key 2 the Dial opens the modality unit at the sun sign, three forward, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-modalities.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:20000}); // let the last answer settle before asking whether the unit is done
      if((await state()).modalitiesComplete)break;
      // the guided problems show the three-count a frame after they start: wait, settle, wait again (the v0.2 count-beat lesson)
      try{await page.waitForFunction(()=>window.ascendantDial.snapshot().unit==='modalities'&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});}
      catch(e){console.error('modality loop stalled at '+n+': '+JSON.stringify(await state()).slice(0,900));console.error('last events: '+JSON.stringify(events.slice(-14).map(x=>x.event_name+'@'+x.input_method+(x.correctness?'✓':''))));throw e;}
      await page.waitForTimeout(300);
      await page.waitForFunction(()=>window.ascendantDial.snapshot().unit==='modalities'&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
      const m=await state();const from=SIGNS.indexOf(m.start.replace('Start: ',''));
      await semantic('seat-'+((from+3)%12));await semantic('seal');await page.waitForTimeout(400);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().modalitiesComplete&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).keys===2 && (await state()).litModCount===12 && events.filter(e=>e.event_name==='modality_family_completed').length===3,'three modality families of four complete with no new Key at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-modalities-complete.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    let sawModality=false;
    for(let round=0;round<6&&!sawModality;round++){ // three kinds, thirty-six items: older items come first
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canEnterPractice&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice'||window.ascendantDial.snapshot().message.startsWith('Nothing is ready'),{},{timeout:15000});
    if((await state()).screen==='practice'){
    for(let n=0;n<6;n++){
      await page.waitForFunction(i=>window.ascendantDial.snapshot().practiceIndex===i&&!window.ascendantDial.snapshot().busy&&(window.ascendantDial.snapshot().practiceMode!=='dial'||window.ascendantDial.snapshot().active),n,{timeout:20000});
      const r=await state();if(r.practiceMode==='done')break;const seat=SIGNS.indexOf(r.practiceSign);
      if(r.practiceMode==='dial'){if(r.step===3)sawModality=true;await semantic('seat-'+((seat+(r.step||4))%12));await semantic('seal');}
      else if(r.practiceMode==='modality'){sawModality=true;if(n===1)await page.screenshot({path:path.join(out,viewport.width+'-practice-modality.png')});await semantic('modality-'+(seat%3));}
      else if(r.practiceMode==='tap')await semantic('element-'+(seat%4));
      else{const opts=r.glyphOptions;await semantic('glyph-name-'+opts.indexOf(SIGNS[seat]));}
      await page.waitForTimeout(250);
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().practiceMode==='done'&&window.ascendantDial.snapshot().canLeavePractice,{},{timeout:20000});
    await semantic('leave-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    }
    if((await state()).screen==='wing'){await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveWing,{},{timeout:15000});}
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // one press from the Dial walks out through the room
    }
    check(sawModality,'a practice carries modality items within six sittings at '+viewport.width);
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).modalitiesComplete && (await state()).gridOpen && !(await state()).gridStarted,'a reload keeps the modality unit complete, and the table has woken at '+viewport.width);
    // ---- Build B: the table and Key 3 ----
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).canEnterGrid && (await state()).caspar.includes('table has woken'),'the finished modality unit wakes the table with a room button at '+viewport.width);
    check((await state()).kitUp.includes('table'),'Build M: the table stands restored the moment it wakes, before its lesson at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room-table.png')});
    await semantic('enter-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&window.ascendantDial.snapshot().canGridPick,{},{timeout:15000});
    { const g=await state(); check(g.avatarAt==='grid'&&g.gridStarted && g.gridTiles.length===12 && g.gridCells.length===12 && g.gridCells.every(c=>c.endsWith(': empty')) && g.gridTiles.every(t=>t.endsWith(', not placed')) && g.caspar.startsWith('Four elements and three modalities') && g.keys===2,'the room button walks to the table and opens it: twelve tiles, twelve empty cells, the intro line at '+viewport.width); }
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the table at '+viewport.width);
    const cellBoxes=await page.locator('#grid-cells button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height,label:b.getAttribute('aria-label')})));
    check(cellBoxes.length===12 && cellBoxes.every(b=>b.width>=48 && b.height>=48) && cellBoxes[1].label==='Fire, fixed: empty' && cellBoxes[11].label==='Water, mutable: empty','twelve semantic cells at the target floor, labeled by element and kind, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid.png')});
    const CELL=seat=>(seat%4)*3+seat%3;
    const settled=async()=>page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    const seatSign=async(seat)=>{await semantic('grid-sign-'+seat);await semantic('grid-cell-'+CELL(seat));await semantic('grid-seal');await settled();};
    await page.waitForTimeout(300);
    await tap(-129,344);await tap(-72,128);await tap(0,654); // the thumb path: the Aries tile, the Fire-cardinal cell, Seal on the canvas
    await settled();
    check((await state()).gridPlaced===1 && events.some(e=>e.event_name==='grid_placed'&&e.evidence_eligible&&e.start_sign==='Aries'),'canvas taps seat Aries at Level 0 with evidence at '+viewport.width);
    await semantic('grid-sign-1');await semantic('grid-cell-'+CELL(8));await semantic('grid-seal'); // Taurus to Fire mutable: the row is wrong
    await page.waitForFunction(()=>window.ascendantDial.snapshot().gridHintLevel===1,{},{timeout:5000});
    check((await state()).caspar==='Not that square, acolyte. Taurus is an Earth sign. Find the Earth row.' && (await state()).canGridAsk && (await state()).gridLocked && (await state()).gridCells[CELL(8)].endsWith('not that one'),'a wrong row nudges with the element, marks the cell, and offers Ask Caspar at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid-miss.png')});
    await semantic('grid-cell-'+CELL(1));await semantic('grid-seal');await settled();
    check((await state()).gridPlaced===2 && events.filter(e=>e.event_name==='grid_placed')[1].evidence_eligible,'the right cell after one nudge seats Taurus as Level 1 evidence at '+viewport.width);
    await semantic('grid-sign-2');await semantic('grid-cell-'+CELL(6));await semantic('grid-seal'); // Gemini to Air cardinal: the column is wrong
    await page.waitForFunction(()=>window.ascendantDial.snapshot().gridHintLevel===1,{},{timeout:5000});
    check((await state()).caspar==='Not that square, acolyte. Gemini is mutable. Find the mutable column.','a wrong column nudges with the kind at '+viewport.width);
    await semantic('grid-ask');await page.waitForFunction(()=>window.ascendantDial.snapshot().gridHintLevel===2,{},{timeout:5000});
    check((await state()).caspar.startsWith('Gemini is Air and it is mutable, acolyte.') && !(await state()).canGridAsk && events.some(e=>e.event_name==='hint_asked'&&e.requested_relationship==='cell_of_sign'),'Ask Caspar states the rule once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid-ask.png')});
    await semantic('grid-cell-'+CELL(2));await semantic('grid-seal');await settled();
    check((await state()).gridPlaced===3 && !events.filter(e=>e.event_name==='grid_placed')[2].evidence_eligible,'a seating after asking earns no evidence at '+viewport.width);
    for(const seat of [3,4])await seatSign(seat);
    check((await state()).gridPlaced===5,'five seated at '+viewport.width);
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).gridStarted && (await state()).gridPlaced===5 && (await state()).keys===2,'a reload mid-table keeps the five seated signs at '+viewport.width);
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('already placed'),'the room says the table waits, part seated, at '+viewport.width);
    await semantic('poi-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&window.ascendantDial.snapshot().canGridPick,{},{timeout:15000});
    check((await state()).avatarAt==='grid'&&(await state()).gridPlaced===5 && (await state()).caspar.includes('5 of twelve') && (await state()).gridCells[CELL(4)]==='Fire, fixed: Leo','tapping the table resumes it with its five seated signs at '+viewport.width);
    await semantic('grid-sign-5');for(const wrong of [6,7,8]){await semantic('grid-cell-'+CELL(wrong));await semantic('grid-seal');await page.waitForTimeout(150);} // Virgo: three wrong cells
    await page.waitForFunction(()=>window.ascendantDial.snapshot().gridHintLevel===3||window.ascendantDial.snapshot().gridPlaced===6,{},{timeout:10000});
    await settled();
    check((await state()).gridPlaced===6 && (await state()).caspar.includes('Virgo is placed') && !events.filter(e=>e.event_name==='grid_placed')[5].evidence_eligible,'three wrong cells hand the sign to Caspar, who seats it without evidence at '+viewport.width);
    for(let seat=6;seat<11;seat++)await seatSign(seat);
    await semantic('grid-sign-11');await semantic('grid-cell-'+CELL(11));await semantic('grid-seal'); // the last seat without settling, so the capture lands mid-ceremony
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyCeremony===3,{},{timeout:20000});await page.waitForTimeout(950);await page.screenshot({path:path.join(out,viewport.width+'-key3-ceremony.png')}); // Build I: the Key rises
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key3&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    check((await state()).keys===3 && (await state()).gridPlaced===12 && (await state()).gridComplete && !(await state()).canGridPick && events.filter(e=>e.event_name==='key3_earned').length===1 && (await state()).caspar.includes('Keeper Key 3 is yours'),'twelve seated earns Key 3 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid-key3.png')});
    await semantic('leave-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).atriumStage===4 && (await state()).keysInHand===1,'back in the Atrium with Key 3 in hand at '+viewport.width);
    await spendAtBooks('Key 3');
    check((await state()).keysSpent===3 && (await state()).booksOpen===1 && !(await state()).wingWhole && events.some(e=>e.event_name==='book_opened_1') && (await state()).caspar.includes('Book opens'),'Key 3 fills the third lock and Book 1 opens at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-book-opens.png')});
    await semantic('leave-chamber');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===5&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('Three locks') && (await state()).caspar.includes('Three lamps'),'the return takes the Atrium to Stage 5 with three Keys spent at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key3.png')});
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).atriumStage===5 && (await state()).keys===3 && (await state()).keysSpent===3 && (await state()).booksOpen===1 && (await state()).key3 && (await state()).gridComplete && (await state()).cleanRuns===1 && (await state()).avatarAt==='entry','a reload resumes at the Hub from the local save with three Keys spent, Book 1 open, the full table, and the clean run at '+viewport.width);
    // ---- Build C: polarity, the six opposite pairs, the builder, and Key 4 ----
    const OPP=seat=>(seat+6)%12;
    const unitActive=async()=>{await page.waitForFunction(()=>window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});await page.waitForTimeout(400);await page.waitForFunction(()=>window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});await page.waitForTimeout(150);}; // a guided problem shows its count a frame after it starts
    const startSeat=async()=>SIGNS.indexOf((await state()).start.replace('Start: ',''));
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('last pattern'),'with three Keys the room points at the wheel\'s last pattern at '+viewport.width);
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await page.waitForFunction(()=>window.ascendantDial.snapshot().unit==='opposites'&&window.ascendantDial.snapshot().canContinue,{},{timeout:15000});
    check(!(await state()).polarityShown && (await state()).message.startsWith('The wheel holds one last secret') && !(await state()).active && !(await state()).canLeaveWing,'after Key 3 the Dial opens the polarity beat; the Back button stays off its Continue at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-polarity.png')});
    await semantic('continue');await page.waitForFunction(()=>window.ascendantDial.snapshot().polarityShown,{},{timeout:5000});
    check((await state()).seats[0].includes(', Yang') && (await state()).seats[1].includes(', Yin') && (await state()).message.includes('day and night'),'the second line shows every seat\'s side, in words, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-polarity-shown.png')});
    await semantic('continue');await unitActive();
    check((await state()).step===6 && (await state()).start==='Start: Taurus' && (await state()).hintLevel===2 && (await state()).unit==='opposites','the first pair is guided from the sun sign with the six-count at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-opposites.png')});
    await semantic('seat-'+OPP(1));await semantic('seal');await unitActive();
    check((await state()).pairsKnown===1 && (await state()).hintLevel===0 && (await state()).start==='Start: Gemini','the guided pair is known; the next is on the player\'s own at '+viewport.width);
    await semantic('seat-'+((2+3)%12));await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().hintLevel===1&&window.ascendantDial.snapshot().canAsk,{},{timeout:5000});
    await semantic('ask-caspar');await page.waitForFunction(()=>window.ascendantDial.snapshot().hintLevel===2&&window.ascendantDial.snapshot().message.includes('six signs forward'),{},{timeout:5000});
    check(true,'a wrong turn nudges; Ask Caspar gives the six-seat rule at '+viewport.width);
    await unitActive();await semantic('seat-'+OPP(2));await semantic('seal');await unitActive();
    check((await state()).pairsKnown===2 && !events.filter(e=>e.event_name==='answer_correct'&&e.requested_relationship==='forward_offset_6').pop().evidence_eligible,'a pair found after asking counts, not as evidence, at '+viewport.width);
    for(let n=0;n<4;n++){const from=await startSeat();await semantic('seat-'+OPP(from));await semantic('seal');if(n<3)await unitActive();}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().oppositesComplete&&window.ascendantDial.snapshot().canContinue,{},{timeout:20000});
    check((await state()).pairsKnown===6 && events.filter(e=>e.event_name==='opposites_completed').length===1,'six pairs complete the last pattern at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-opposites-complete.png')});
    await semantic('continue');await page.waitForFunction(()=>window.ascendantDial.snapshot().builderStep==='name'&&window.ascendantDial.snapshot().canBuilderName,{},{timeout:5000});
    { const b=await state(); check(b.unit==='builder' && b.builderAsk==='Earth, fixed' && b.builderOptions.length===4 && b.builderOptions.includes('Taurus') && b.built===0 && !b.canLeaveWing,'Continue opens the builder on the sun sign\'s parts: four names, the Back button off them, at '+viewport.width); }
    const nameBoxes=await page.locator('#builder-names button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height})));
    check(nameBoxes.length===4 && nameBoxes.every(b=>b.width>=48 && b.height>=48),'four semantic names at the target floor at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-builder-name.png')});
    await page.waitForTimeout(300);await tap(-78+156*((await state()).builderOptions.indexOf('Taurus')%2),654+60*Math.floor((await state()).builderOptions.indexOf('Taurus')/2)); // the thumb path: the right name on the canvas
    await unitActive();check((await state()).builderStep==='opposite' && (await state()).step===6 && (await state()).start==='Start: Taurus','the right name hands to the wheel at '+viewport.width);
    await semantic('seat-'+OPP(1));await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().builderStep==='share'&&window.ascendantDial.snapshot().canBuilderShare,{},{timeout:20000});
    check((await state()).message.includes('Taurus and Scorpio'),'the opposite found, the share step asks what the two share at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-builder-share.png')});
    await semantic('builder-share-2');await page.waitForFunction(()=>window.ascendantDial.snapshot().message.includes('Not the element'),{},{timeout:5000});
    await semantic('builder-share-0');await page.waitForFunction(()=>window.ascendantDial.snapshot().shared.includes('Modality'),{},{timeout:5000});
    await semantic('builder-share-1');await page.waitForFunction(()=>window.ascendantDial.snapshot().built===1&&window.ascendantDial.snapshot().builderStep==='name',{},{timeout:20000});
    check(events.filter(e=>e.event_name==='builder_sign_built').pop().evidence_eligible && (await state()).builderAsk==='Air, cardinal','the first sign is built unassisted; the second begins at '+viewport.width);
    { const o=(await state()).builderOptions;const right=o.indexOf('Libra');await semantic('builder-name-'+((right+1)%4));await page.waitForFunction(()=>window.ascendantDial.snapshot().message.startsWith('Not that one'),{},{timeout:5000});await semantic('builder-name-'+right); }
    await unitActive();check((await state()).builderStep==='opposite' && (await state()).start==='Start: Libra','a wrong name is nudged; the right one hands to the wheel at '+viewport.width);
    await semantic('seat-'+((6+3)%12));await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().hintLevel===1&&window.ascendantDial.snapshot().canAsk,{},{timeout:5000});
    await semantic('ask-caspar');await page.waitForFunction(()=>window.ascendantDial.snapshot().hintLevel===2,{},{timeout:5000});await unitActive();
    await semantic('seat-'+OPP(6));await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().builderStep==='share'&&window.ascendantDial.snapshot().canBuilderShare,{},{timeout:20000});
    await semantic('builder-share-0');await page.waitForTimeout(150);await semantic('builder-share-1');await page.waitForFunction(()=>window.ascendantDial.snapshot().built===2&&window.ascendantDial.snapshot().builderStep==='name',{},{timeout:20000});
    check(!events.filter(e=>e.event_name==='builder_sign_built').pop().evidence_eligible && (await state()).builderAsk==='Fire, mutable','the second sign is built with help; the third begins at '+viewport.width);
    { const o=(await state()).builderOptions;const right=o.indexOf('Sagittarius');await semantic('builder-name-'+((right+1)%4));await page.waitForTimeout(150);await semantic('builder-name-'+((right+2)%4)); }
    await unitActive();check((await state()).message.startsWith('It is Sagittarius') && (await state()).builderStep==='opposite','two wrong names reveal the sign at '+viewport.width);
    await semantic('seat-'+OPP(8));await semantic('seal');await page.waitForFunction(()=>window.ascendantDial.snapshot().builderStep==='share'&&window.ascendantDial.snapshot().canBuilderShare,{},{timeout:20000});
    await semantic('builder-share-1');await page.waitForTimeout(150);await semantic('builder-share-0');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyCeremony===4,{},{timeout:20000});await page.waitForTimeout(950);await page.screenshot({path:path.join(out,viewport.width+'-key4-ceremony.png')}); // Build I: the Key rises
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key4&&window.ascendantDial.snapshot().canLeaveWing,{},{timeout:20000});
    check((await state()).keys===4 && (await state()).built===3 && events.filter(e=>e.event_name==='key4_earned').length===1 && (await state()).message.includes('Keeper Key 4 is yours'),'three signs built with one unassisted earns Key 4 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-key4.png')});
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).atriumStage===5 && (await state()).keysInHand===1,'back in the Atrium with Key 4 in hand at '+viewport.width);
    await spendAtBooks('Key 4');
    check((await state()).keysSpent===4 && (await state()).booksOpen===1 && (await state()).wingWhole && !(await state()).canInsert && events.some(e=>e.event_name==='wing_whole') && (await state()).caspar.includes('Wing is whole') && (await state()).caspar.includes('The Zodiac Wing is complete'),'Key 4 fills Book 2\'s first lock: the Wing is whole, with the closing line and the end card at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-whole.png')});
    await semantic('leave-chamber');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===6&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('Four Keys spent') && (await state()).caspar.includes('Four lamps'),'the return takes the Atrium to Stage 6 with four Keys spent at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().lightAlpha===1,{},{timeout:5000});
    check((await state()).lightAlpha===1,'the light overlay is full once the Wing is whole (Stage 6) at '+viewport.width); // Build H
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key4.png')});
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).atriumStage===6 && (await state()).keys===4 && (await state()).keysSpent===4 && (await state()).wingWhole && (await state()).key4 && (await state()).polarityShown && (await state()).oppositesComplete && (await state()).avatarAt==='entry','a reload resumes at the Hub from the local save with four Keys spent, the Wing whole, the sides, and the six pairs at '+viewport.width);
    // Build L: the Wing at full light (Stage 6), the capture the owner judges the light overlay by
    await semantic('enter-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});await page.waitForTimeout(1200);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room-stage6.png')});
    { const k=await state(); check(k.kitPieces===19 && k.kitLevel===4 && k.kitRestored===k.kitPieces && k.grime===0 && k.wingLight===1,'Build M: with four Keys every Wing kit piece is restored, the grime gone, the light full at '+viewport.width); }
    await semantic('leave-wing');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
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
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().destination==='Selected: Aries' && window.ascendantDial.snapshot().hintLevel===1 && window.ascendantDial.snapshot().canAsk),'first browser rejection stays in place at Level 1 and offers Ask Caspar');
  await action('ask-caspar');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().hintLevel===2&&window.ascendantDial.snapshot().message.includes('shares the same element after'));
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().destination==='Selected: Aries' && window.ascendantDial.snapshot().hintLevel===2 && !window.ascendantDial.snapshot().canAsk),'Ask Caspar gives the rule once with the count and the ring returns');
  await recovery.screenshot({path:path.join(out,'390-rejected.png')});
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});await action('seal');await active('Leo');
  check(await recovery.evaluate(()=>window.ascendantDial.snapshot().hintLevel===0),'a miss after the asked rule goes to the Level 3 demo, which resets to a fresh Level 0 problem');
  const sealWrongThrice=async()=>{for(let n=0;n<3;n++){await recovery.waitForFunction(()=>window.ascendantDial.snapshot().active,{},{timeout:20000});await action('seal');await recovery.waitForTimeout(200);}};
  await sealWrongThrice();await active('Sagittarius');
  await sealWrongThrice();
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().phase.includes('Paused for now'),{},{timeout:30000});
  check(await recovery.evaluate(()=>!window.ascendantDial.snapshot().active && !window.ascendantDial.snapshot().keyEarned),'browser recovery cap pauses without awarding Key');
  await recovery.screenshot({path:path.join(out,'390-recovery-cap.png')});await recoveryContext.close();
  // ---- Build E: the test set from the URL at both viewports (the opening and the Dial), the cues, the style page on both sets ----
  const base=process.env.GREYBOX_URL || 'http://127.0.0.1:8000';const withQuery=q=>base+(base.includes('?')?'&':'?')+q;
  for(const viewport of [{width:390,height:844},{width:360,height:800}]){
    const artContext=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const art=await artContext.newPage();
    await art.goto(withQuery('art=test'));
    await art.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});await art.locator('#loading').waitFor({state:'detached'});
    const snap=()=>art.evaluate(()=>window.ascendantDial.snapshot());const act=async(id)=>art.locator('#'+id).evaluate(b=>b.click());
    let s=await snap();check(s.artSet==='test'&&s.artFiles===66&&s.soundFiles===7&&!s.style,'?art=test plays the game with a file in every slot at '+viewport.width);
    check(s.lightFiles===3&&s.lightAlpha===0,'the three light overlays resolve from the test set and stay dark in the opening (Stage 1) at '+viewport.width); // Build H
    await art.locator('#name').fill('Tester');await art.locator('#name').dispatchEvent('change');await art.waitForFunction(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
    await act('birth-known');await art.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-1');await art.waitForFunction(()=>window.ascendantDial.snapshot().sunSign==='Taurus');
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
    await art.screenshot({path:path.join(out,viewport.width+'-art-atrium.png')});
    check(await art.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll with the test set at '+viewport.width);
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().lastCue==='page'&&window.ascendantDial.snapshot().cuesPlayed>=1);check(true,'a page turn plays the page cue from the test set at '+viewport.width);
    for(let n=0;n<8&&(await snap()).screen==='atrium';n++){await act('next-screen');await art.waitForTimeout(150);}
    await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canContinue,{},{timeout:120000});
    await art.screenshot({path:path.join(out,viewport.width+'-art-dial.png')});
    for(let n=0;n<16&&(await snap()).start!=='Start: Taurus';n++){if((await snap()).canContinue)await act('continue');await art.waitForTimeout(500);}
    const okStart=s=>window.ascendantDial.snapshot()?.active && window.ascendantDial.snapshot().start===('Start: '+s);await art.waitForFunction(okStart,'Taurus',{timeout:15000});await art.waitForTimeout(400);await art.waitForFunction(okStart,'Taurus',{timeout:20000});await art.waitForTimeout(150);
    const before=(await snap()).cuesPlayed;await act('forward');await art.waitForFunction(b=>window.ascendantDial.snapshot().lastCue==='step'&&window.ascendantDial.snapshot().cuesPlayed>b,before);check(true,'a wheel step plays the step cue from the test set at '+viewport.width);
    await art.screenshot({path:path.join(out,viewport.width+'-art-dial-guided.png')});
    await artContext.close();
  }
  for(const [query,set] of [['style=test','test'],['style','']]){
    const styleContext=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const stylePage=await styleContext.newPage();await stylePage.goto(withQuery(query));
    await stylePage.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='style',{},{timeout:120000});await stylePage.locator('#loading').waitFor({state:'detached'});
    const s=await stylePage.evaluate(()=>window.ascendantDial.snapshot());const where=set?'the test set':'the Art folder';
    check(s.style&&s.artSet===set&&s.styleSlots.length===66&&s.styleSounds.length===7&&!s.canSliceContinue&&!s.canName,'?'+query+' shows the style page on '+where+' with 66 art and 7 sound slots and no game controls');
    check(set?s.styleSlots.every(t=>t.endsWith(': test set'))&&s.styleSounds.every(t=>t.endsWith(': test set')):s.styleSlots.every(t=>/: (file|placeholder)$/.test(t))&&s.styleSounds.every(t=>/: (file|silent)$/.test(t)),'every slot lists its source on '+where);
    const items=await stylePage.locator('#style-list li').allTextContents();check(items.length===66&&items[0].startsWith('atrium:')&&(await stylePage.locator('#style-sounds button').count())===7,'the semantic layer lists every art slot with its source and a button per sound slot');
    check(await stylePage.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll on the style page on '+where);
    await stylePage.screenshot({path:path.join(out,'390-style'+(set?'-'+set:'')+'.png')});
    if(set){await stylePage.locator('#sound-1').evaluate(b=>b.click());await stylePage.waitForFunction(()=>window.ascendantDial.snapshot().lastCue==='seal'&&window.ascendantDial.snapshot().cuesPlayed>=1);check(true,'a sound slot plays from the style page');
      await stylePage.locator('#mute').evaluate(b=>b.click());await stylePage.waitForFunction(()=>window.ascendantDial.snapshot().muted);check((await stylePage.locator('#mute').getAttribute('aria-pressed'))==='true','the test mute toggle works from the style page');
      await stylePage.locator('#mute').evaluate(b=>b.click());await stylePage.waitForFunction(()=>!window.ascendantDial.snapshot().muted);}
    await styleContext.close();
  }
  fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
