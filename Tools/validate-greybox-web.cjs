// Run against the real locally served Unity build. Requires Playwright; no production dependency.
const playwright=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const engine=process.env.BROWSER==='webkit' ? playwright.webkit : playwright.chromium; // BROWSER=webkit approximates iPhone browsers, which all run WebKit
const fs=require('fs');
const path=require('path');
(async()=>{
  const out=process.env.EVIDENCE_DIR || 'Logs/WebEvidence';fs.mkdirSync(out,{recursive:true});
  const browser=await engine.launch(process.env.BROWSER==='webkit' ? {headless:true} : {headless:true,channel:'chrome'});
  const report=[];
  const ART_SLOTS=187,SOUND_SLOTS=7; // the manifest's slots (Slots.cs); the style checks' messages read the numbers they assert (declared first: the art-set check reads it too)
  function check(value,text){if(!value)throw Error(text);report.push('PASS: '+text);}
  // Faster checks (Sept 25): the two viewports play in parallel, each in its own browser context; VIEWPORTS=390 (or 360) runs one.
  const VIEWPORTS=(process.env.VIEWPORTS||'390,360').split(',').map(w=>w.trim()).filter(Boolean).map(w=>w==='360'?{width:360,height:800}:/^\d+x\d+$/.test(w)?{width:+w.split('x')[0],height:+w.split('x')[1]}:{width:390,height:844}); // Part 2: or any WxH
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const context=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE}); // DEVICE_SCALE=2 MOBILE=1 approximates a phone
    const page=await context.newPage();const events=[],errors=[];
    page.on('pageerror',e=>errors.push(String(e)));
    page.on('console',msg=>{const text=msg.text(),mark=text.indexOf('[CelestialDial] ');if(mark>=0){try{events.push(JSON.parse(text.slice(mark+16)));}catch{}}});
    const state=()=>page.evaluate(()=>window.ascendantDial.snapshot());
    // batch 2 (3b C, 3c C, 3d): the buttons on screen as the web state lists them, "words:look:width x height"
    const looks=async()=>((await state()).buttons||[]).map(x=>{const p=x.split(':'),d=p[2].split('x').map(Number);return {w:p[0],k:p[1],W:d[0],H:d[1]};});
    const lookOf=(b,w)=>{const x=b.find(y=>y.w===w);return x?x.k+' '+x.W+'x'+x.H:'missing';};
    const small=b=>b.filter(x=>x.W<44||x.H<44).map(x=>x.w+' '+x.W+'x'+x.H); // 3d: every target 44 px or more
    const ready=async()=>page.waitForFunction(()=>window.ascendantDial?.snapshot()?.canContinue,{},{timeout:120000});
    // Unity ignores pointer input in the first frame after a phase transition (the button is activated in
    // that same frame), so settle briefly after the state flips. A person cannot tap that fast.
    // A Level 2 problem shows its count beat one frame after it starts, so wait, let the beat begin, then wait for it to end.
    const waitActive=async(start)=>{const ok=s=>window.ascendantDial.snapshot()?.active && window.ascendantDial.snapshot().start===('Start: '+s);await page.waitForFunction(ok,start,{timeout:15000});await page.waitForTimeout(400);await page.waitForFunction(ok,start,{timeout:20000});await page.waitForTimeout(150);};
    // A canvas tap the way a thumb or a mouse makes one: the pointer arrives, then presses, then lets go, each on its own rendered frame. Unity reads
    // input once a frame, and with two viewports playing at once a page can go several frames without one; teleporting and clicking in the same
    // instant let the first tap on a new spot land nowhere (the Chamber's door at Stage 4, Sept 25). Waiting on animation frames, not milliseconds, holds.
    const frames=n=>page.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);
    const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await page.mouse.move(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await frames(2);await page.mouse.down();await frames(2);await page.mouse.up();await frames(2);};
    // A room tap that must start a walk, on the first tap. On the Key 3 visit it didn't (Sept 25): the Key 3 ceremony, run beside the table's
    // seating pause, cleared the walk home's busy mid-fade, and the fade overlay ate the tap. Fixed in SliceView.GridSeated; this guards it.
    const tapToWalk=async(x,y,label)=>{const start=JSON.stringify([(await state()).screen,(await state()).walkTarget]);await tap(x,y);
      try{await page.waitForFunction(k=>{const s=window.ascendantDial.snapshot();return s.walking||JSON.stringify([s.screen,s.walkTarget])!==k;},start,{timeout:1500});}
      catch{throw Error(label+' at '+viewport.width+' took no walk on the first canvas tap (a room tap must land the first time; see the Key 3 fix, Sept 25)');}};
    const semantic=async(id)=>page.locator('#'+id).evaluate(b=>b.click());
    // Note 10: leaving the Dial lands in the room; the room's doorway back returns to the Atrium (since the art pass, 86bcex5kc 1A). Two presses from the Dial, one from the room.
    const leaveToHub=async()=>{if((await state()).screen==='wing'){await page.waitForFunction(()=>{const s=window.ascendantDial.snapshot();return s.canLeaveDial&&!s.busy&&!s.active;},{},{timeout:15000}).catch(()=>{});await semantic('leave-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});}await semantic('poi-atrium-door');};
    await page.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');
    await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});await page.locator("#loading").waitFor({state:"detached"});
    await page.screenshot({path:path.join(out,viewport.width+'-identity.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll on identity at '+viewport.width);
    await page.locator('#name').fill('Tester');await page.locator('#name').dispatchEvent('change');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
    await page.screenshot({path:path.join(out,viewport.width+'-birth.png')});
    check(await page.evaluate(()=>window.ascendantDial.snapshot().canSliceContinue===false),'birth prompt waits for a choice at '+viewport.width);
    check(JSON.stringify((await state()).birthChoices)==='["Yes, I know my birthday","I\'ll skip it"]','Oct 7: the opening\'s question has two answers (the known path and the random sun retired) at '+viewport.width);
    // the birth-time build (owner, Oct 7): I'll skip it asks which sign the player goes by, a sign or I'm not sure; nothing else is asked
    await semantic('birth-skip');await page.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);
    check((await state()).birthStep==='sun-pick'&&!(await state()).canSignUnknown&&!(await state()).canSliceContinue,'I\'ll skip it: which sign do you go by, with no I\'m not sure (Oct 7, 86bced0tc), at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-birth-skip.png')});
    await semantic('sign-1');await page.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');
    await semantic('sign-3');await page.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await page.screenshot({path:path.join(out,viewport.width+'-birth-skip-rising.png')});
    await semantic('sign-4');await page.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done'&&window.ascendantDial.snapshot().canSliceContinue);
    check((await state()).bigThree==='\u2609 Taurus \u00b7 \u263d Cancer \u00b7 \u2191 Leo'&&(await state()).sunBasis==='picked','the sun, the moon and the rising picked, no unknown: '+(await state()).bigThree+' at '+viewport.width);
    await semantic('next-screen');
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
    await page.screenshot({path:path.join(out,viewport.width+'-atrium.png')});
    { const m=(await state()).masters||[]; check(m.includes('atrium'),'Batch 2 (the masters approved, Oct 2): the Grand Atrium draws its 1200 x 1840 master whole ('+m.join(', ')+') at '+viewport.width); }
    check((await state()).casparPose==='wry','Build P: Caspar stands wry behind the chat box on the opening\'s first page at '+viewport.width);
    { const h=(await state()).chatBoxHeight; check(h>0&&h<240,'Build X: the opening\'s chat box fits its first page ('+h+' of 240) at '+viewport.width);
      await tap(0,429+h);await page.waitForFunction(()=>window.ascendantDial.snapshot().casparPose==='warm',{},{timeout:5000}).catch(()=>{}); // Build P: the pose turns with the page; Build X: Continue rides at the fitted box's bottom
      check((await state()).casparPose==='warm','Build X: a canvas tap on Continue, at the fitted box\'s bottom, turns the page at '+viewport.width); }
    for(let n=0;n<8&&(await state()).screen==='atrium';n++){await semantic('next-screen');await page.waitForTimeout(150);}
    // Build T (owner, APK playtest, Sept 29): the opening hands over the Atrium; Caspar sends the player to the Zodiac Wing, and the Dial is tapped there.
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).atriumStage===1&&(await state()).caspar.startsWith('Our work begins in the Zodiac Wing. That door there.')&&(await state()).canEnterWing,'Build T: the opening ends in the Atrium at Stage 1, Caspar pointing to the Zodiac Wing at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-opening-hub.png')});
    { const b=await looks(); check(!b.some(x=>x.w==='THE ZODIAC WING')&&(await state()).pois.includes('wing-door')&&['Settings gear','Travel button'].every(w=>b.some(x=>x.w===w&&x.W>=44&&x.H>=44))&&small(b).length===0,'batch 2 (3c C, 3d); the art pass (86bcex5kc 1A): in the Atrium the Zodiac Wing\'s door takes the tap and its button is gone, the gear and the mini-menu button take 44 x 44 ('+lookOf(b,'Settings gear')+', '+lookOf(b,'Travel button')+'), and every target is 44 px or more (under: '+(small(b).join(', ')||'none')+') at '+viewport.width); }
    check((await state()).chatBoxHeight>0&&(await state()).chatBoxHeight<120,'Build X: the Atrium\'s chat box fits Caspar\'s two lines ('+(await state()).chatBoxHeight+' of 120) at '+viewport.width);
    check((await state()).lastCue==='page','Build E: the page hook fired on Caspar\'s pages, file or not, at '+viewport.width);
    // Build U (owner, APK playtest, Sept 29): the gear at the top right opens Settings; its rows work on the canvas; the web build has no Quit.
    const muted0=(await state()).muted;
    await tap(158,22);await page.waitForFunction(()=>window.ascendantDial.snapshot().settingsOpen,{},{timeout:5000}).catch(()=>{});
    check((await state()).settingsOpen&&!(await state()).canQuit,'Build U: a tap on the gear opens Settings; a web page offers no Quit at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-settings.png')});
    await tap(0,267);await page.waitForTimeout(200);check((await state()).muted!==muted0,'Build U: the Sound row turns the sound '+(muted0?'on':'off')+' at '+viewport.width);
    await tap(0,267);await page.waitForTimeout(200);
    const reduced0=(await state()).reducedMotion;await tap(0,323);await page.waitForTimeout(200);check((await state()).reducedMotion!==reduced0,'Build U: the Reduced motion row works from Settings at '+viewport.width);
    await tap(0,323);await page.waitForTimeout(200);
    await tap(0,553);await page.waitForFunction(()=>!window.ascendantDial.snapshot().settingsOpen,{},{timeout:5000}).catch(()=>{});
    check(!(await state()).settingsOpen&&(await state()).muted===muted0&&(await state()).reducedMotion===reduced0,'Build U: Close shuts Settings, both settings back as they were at '+viewport.width);
    // Platform fit, Part 1 (owner, Oct 1: "Hide the bar and draw behind it"; 86bcbn6mf): with a top band (a camera cutout's, simulated here: the web has none),
    // the gear moves below it and takes its taps there, its semantic box follows, and its old place no longer opens Settings.
    { const band=24;await page.evaluate(b=>window.ascendantDial.act('safe-inset:'+b),band);await page.waitForFunction(b=>Math.abs(window.ascendantDial.snapshot().safeTop-b)<.01,band,{timeout:5000}).catch(()=>{});await frames(3);
      const s=await state(),scale=Math.min(viewport.width/360,viewport.height/800),gearBox=await page.locator('#settings').evaluate(b=>parseFloat(b.style.top)+parseFloat(b.style.height)/2); // the box as laid out (off the Dial the semantic gear is disabled, so not displayed)
      await page.screenshot({path:path.join(out,viewport.width+'-safe-band.png')}); // the Atrium's title and the gear below the band
      await tap(158,22);await page.waitForTimeout(300);const oldSpot=(await state()).settingsOpen;
      await tap(158,22+band);await page.waitForFunction(()=>window.ascendantDial.snapshot().settingsOpen,{},{timeout:5000}).catch(()=>{});const newSpot=(await state()).settingsOpen;
      if(newSpot){await tap(0,553);await page.waitForFunction(()=>!window.ascendantDial.snapshot().settingsOpen,{},{timeout:5000}).catch(()=>{});}
      check(Math.abs(s.safeTop-band)<.01&&!oldSpot&&newSpot&&Math.abs(gearBox-((viewport.height-800*scale)/2+(22+band)*scale))<1.5&&!(await state()).settingsOpen,'Platform fit, Part 1: with a '+band+' px top band the gear moves below it, its tap and semantic box with it, at '+viewport.width+' (band '+s.safeTop+', old spot '+oldSpot+', new spot '+newSpot+', box '+gearBox.toFixed(1)+' for '+((viewport.height-800*scale)/2+(22+band)*scale).toFixed(1)+')');
      await page.evaluate(()=>window.ascendantDial.act('safe-inset:-1'));await page.waitForFunction(()=>window.ascendantDial.snapshot().safeTop===0,{},{timeout:5000}).catch(()=>{});await frames(3);
      check((await state()).safeTop===0,'Platform fit, Part 1: the web page has no top band of its own, so the top row stays where it was at '+viewport.width); }
    check(!(await state()).canEnterChamber,'Build T: the Chamber button waits for Key 1 at '+viewport.width);
    await semantic('poi-chamber-door');await page.waitForTimeout(300);check((await state()).screen==='hub'&&(await state()).hubNote.startsWith('Sealed.'),'Build T: before Key 1 the Chamber door only says it is sealed at '+viewport.width);
    // The room mini-menu (owner, Oct 1; 86bca07wv: Option A, v2 spacing; rooms only, travel only, one dim Sealed row; the button at the top left, mirroring the gear).
    // On the canvas: the button opens TRAVEL, the Sealed row is no door, a tap off the panel closes it, a row travels, and the semantic rows sit on the rows.
    { const rowY=i=>47+52+44*i; // TravelMenu: the panel hangs from 47; rows 44 apart from 52 into it; their centre x -23 (the panel's left edge at 17, 280 wide)
      let s=await state();check(s.travelShown&&!s.travelOpen,'the mini-menu: its button shows in the Atrium at '+viewport.width);
      await tap(-158,22);await page.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});s=await state();
      check(s.travelOpen&&JSON.stringify(s.travelRows)===JSON.stringify(['The Grand Atrium, here','The Zodiac Wing','Sealed']),'the mini-menu: a tap on its button opens TRAVEL: the Atrium here, the Zodiac Wing, one Sealed row (the Chamber waits for Key 1): '+JSON.stringify(s.travelRows)+' at '+viewport.width);
      await page.screenshot({path:path.join(out,viewport.width+'-travel-atrium.png')});
      await tap(-23,rowY(2));await page.waitForTimeout(300);check((await state()).travelOpen&&(await state()).screen==='hub','the mini-menu: the Sealed row is no door at '+viewport.width);
      await tap(0,560);await page.waitForFunction(()=>!window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});check(!(await state()).travelOpen&&(await state()).screen==='hub','the mini-menu: a tap off the panel closes it at '+viewport.width);
      await tap(-158,22);await page.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});
      await tap(-23,rowY(1));await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});s=await state();
      check(s.screen==='wingroom'&&!s.travelOpen&&s.travelShown,'the mini-menu: a tap on the Zodiac Wing row travels there at '+viewport.width);
      { const b=await looks(); check(['THE DIAL'].every(w=>b.some(x=>x.w===w&&x.k==='room'&&x.H>=44))&&!b.some(x=>x.w==='RETURN TO THE ATRIUM')&&small(b).length===0,'batch 2 (3c C, 3d): the Zodiac Wing\'s buttons wear the room look (The Dial: '+lookOf(b,'THE DIAL')+'), and Return to the Atrium is gone: the doorway back takes the tap (86bcex5kc 1A), every target 44 px or more (under: '+(small(b).join(', ')||'none')+') at '+viewport.width); }
      await semantic('travel');await page.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});s=await state();
      const scale=Math.min(viewport.width/360,viewport.height/800),row=await page.locator('#travel-atrium').evaluate(b=>{const r=b.getBoundingClientRect();return [r.left+r.width/2,r.top+r.height/2,b.hidden];});
      const at=s.travelAt||[-158,22]; // Part 2: the panel hangs under its button, which on a phone keeps to the screen's corner
      check(JSON.stringify(s.travelRows)===JSON.stringify(['The Grand Atrium','The Zodiac Wing, here','Sealed'])&&!row[2]&&Math.abs(row[0]-(viewport.width/2+(at[0]+135)*scale))<1.5&&Math.abs(row[1]-((viewport.height-800*scale)/2+(at[1]+25+52)*scale))<1.5,'the mini-menu: in the Wing the Wing is here, and the semantic rows sit on the panel\'s rows at '+viewport.width);
      await page.screenshot({path:path.join(out,viewport.width+'-travel-wing.png')});
      await semantic('travel-atrium');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
      check((await state()).screen==='hub'&&(await state()).atriumStage===1&&!(await state()).travelOpen,'the mini-menu: the Atrium row brings the Keeper back, the Atrium as it was at '+viewport.width); }
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar==='The Zodiac Wing. Mind the dust. The Dial is waiting for you; tap it when you are ready.'&&(await state()).canEnterDial,'Build T: in the Zodiac Wing Caspar points to the Dial at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-opening-wing.png')});
    check((await state()).dialEye===0,'Build Z: in the Zodiac Wing the Dial rests with its eye closed at '+viewport.width);
    // Build AB (owner, Oct 1: "the eye no longer blinks open once tapped"): read the eye off the screen, not the state. The Wing Dial's eye sits at
    // (219, 317) on the 360 x 800 layout (Build AC: measured on the Cast Dial piece); the shut lids are brass, the open glass indigo: count the glass.
    const eyeGlass=async()=>{const scale=Math.min(viewport.width/360,viewport.height/800),ox=viewport.width/2+(219-180)*scale,oy=(viewport.height-800*scale)/2+317*scale;
      const shot=await page.screenshot({clip:{x:ox-15*scale,y:oy-3*scale,width:30*scale,height:6*scale}});
      return page.evaluate(async b64=>{const img=new Image();img.src='data:image/png;base64,'+b64;await img.decode();const c=document.createElement('canvas');c.width=img.width;c.height=img.height;const g=c.getContext('2d');g.drawImage(img,0,0);const d=g.getImageData(0,0,c.width,c.height).data;let n=0;for(let i=0;i<d.length;i+=4)if(d[i+2]>d[i]+8)n++;return n/(d.length/4);},shot.toString('base64'));};
    const glassShut=await eyeGlass();
    await semantic('poi-dial');
    await page.waitForTimeout(500);const glassOpen=await eyeGlass(); // the eye opens in 0.35 s and holds while the Keeper walks
    check(glassOpen>glassShut+.5,'Build AB: the Wing Dial\'s eye is seen open on screen as the Keeper goes to it ('+Math.round(glassShut*100)+'% blue glass shut, '+Math.round(glassOpen*100)+'% open) at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-eye-opening.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});await ready();
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveDial,{},{timeout:10000}).catch(()=>{});check((await state()).canLeaveDial,'Build T: Leave the Dial shows from the first lesson on (tappable once the wheel\'s opening beat settles) at '+viewport.width);
    check((await state()).dormant&&(await state()).message===''&&(await state()).dialBoxHeight===0,'the Dial is dormant on arrival, Caspar silent (the owner cut his repeated Wing line) at '+viewport.width);
    check((await state()).artSet===''&&!(await state()).style,'no query: the game plays on the Art folder, no style page at '+viewport.width); // Build E (the page cue is checked in the Atrium, before the walk: Build T)
    await page.screenshot({path:path.join(out,viewport.width+'-encounter.png')});
    { const z=await state(); check(z.dialEye===1,'Build Z: the Wing Dial opened its eye as the Keeper came to it at '+viewport.width);
      check(z.dialRoom&&z.dialLit===0&&z.revealsPlayed.length===0&&z.eyeText===''&&z.revealing==='','Build Z: the Dial arrives in its room, asleep: no glow, the eye empty, no reveal yet at '+viewport.width); }
    // Build Z (owner, Sept 29-30): the first Continue wakes the Dial with the elements' reveal: the glow sweeps round from the top, the seats pop as it passes.
    await semantic('continue');await page.waitForFunction(()=>window.ascendantDial.snapshot().revealing==='elements',{},{timeout:5000}).catch(()=>{});
    await page.waitForFunction(()=>{const z=window.ascendantDial.snapshot();return z.dialLit>.3&&z.dialLit<.9;},{},{timeout:3000}).catch(()=>{});
    { const z=await state(); await page.screenshot({path:path.join(out,viewport.width+'-reveal-sweep.png')});
      check(z.revealing==='elements'&&z.dialLit>0&&z.dialLit<1,'Build Z: mid-reveal the glow has swept part of the ring ('+z.dialLit.toFixed(2)+') at '+viewport.width); }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().revealing==='',{},{timeout:8000});
    { const z=await state(); check(z.dialLit===1&&z.revealsPlayed.join()==='elements','Build Z: the reveal ends with the whole ring lit, the elements marked played at '+viewport.width); }
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at '+viewport.width);
    // Seven intro beats, two of them automatic, then the teaching page and the guided problem.
    for(let n=0;n<16&&(await state()).start!=='Start: Taurus';n++){if((await state()).canContinue)await semantic('continue');await page.waitForTimeout(500);}
    await waitActive('Taurus');check(!(await state()).dormant,'the Dial has woken and the guided problem began at '+viewport.width);
    { const z=await state(); await page.screenshot({path:path.join(out,viewport.width+'-eye-challenge.png')});
      check(z.eyeText==='Next Earth after Taurus'&&z.eyeSize>=13&&z.eyeSize<=16&&z.signLabel==='Taurus','Build Z: the Dial poses its challenge in the eye ('+z.eyeSize+' px) and names the framed sign above it at '+viewport.width);
      check((z.speaker==='dial')===z.dialVoice,'Build Z: the box wears the Dial\'s blue exactly when the Dial speaks ('+z.speaker+') at '+viewport.width); }
    { const t=await state(),c=t.trianglesCorners||[]; check(t.triangles && t.trianglesShader && t.trianglesMode==='teaching' && t.trianglesTaught==='Earth' && t.trianglesCrossing===0 && t.trianglesGaps>0 && t.trianglesShown>.5 && c.length===24 && [...Array(12).keys()].every(i=>Math.abs(Math.hypot(c[2*i],c[2*i+1]-270)-80.5)<.6),'batch 2, step 5: the family triangles on the worn Dial in the Web build: Earth taught, its triangle lit, every side breaking round the words ('+t.trianglesGaps+' gaps, '+Math.round(t.trianglesShown*100)+'% shown, no light on a word), the corners on the hub ring at '+viewport.width);
      await page.screenshot({path:path.join(out,viewport.width+'-triangles-teaching.png')}); }
    // Build T: mid-challenge, a tap on the canvas's Leave the Dial goes to the Zodiac Wing; the Dial, tapped again, resumes the same problem.
    await tap(0,768);await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
    check((await state()).screen==='wingroom'&&(await state()).atriumStage===1,'Build T: Leave the Dial, tapped mid-challenge in the first lesson, lands in the Zodiac Wing at '+viewport.width);
    check((await state()).dialEye===0,'Build Z: back in the room the Dial\'s eye has closed again at '+viewport.width);
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await waitActive('Taurus');check((await state()).start==='Start: Taurus'&&(await state()).active,'Build T: back at the Dial the same problem is waiting at '+viewport.width);
    const boxes=await page.locator('#seats button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height,label:b.getAttribute('aria-label')})));
    check(boxes.length===12 && boxes.every(b=>b.width>=48 && b.height>=48 && b.label.includes('position')),'12 semantic seats and effective target floor at '+viewport.width);
    // Build AC follow-up (Whitney, Oct 1): the seat boxes kept Build AB's radius 127 and 56 x 56 after the Dial moved its seats to 123 at 58 x 66, and no check failed.
    // Read each box back onto the 360 x 800 layout and hold it to the seats as the Dial reports them (seatRadius, seatSize): centred on its seat at the seat's place
    // in the turn (the framed seat at 9 o'clock), the seat's own size, upright, its long side (across the ring) on the axis the seat's radius runs nearer.
    const seatBoxes=async()=>{const s=await state(),sel=Math.max(0,s.seats.findIndex(x=>x.includes(', selected'))),size=s.seatSize||[],scale=Math.min(viewport.width/360,viewport.height/800),left=(viewport.width-360*scale)/2,top=(viewport.height-800*scale)/2;
      const rects=await page.locator('#seats button').evaluateAll(bs=>bs.map(b=>{const r=b.getBoundingClientRect();return [r.left,r.top,r.width,r.height];}));const off=[];
      for(let i=0;i<12;i++){const a=(180+(i-sel)*30)*Math.PI/180,flat=s.dialRing&&Math.abs(Math.cos(a))>Math.abs(Math.sin(a)),[x,y,w,h]=rects[i];
        const want=[Math.cos(a)*s.seatRadius,270-Math.sin(a)*s.seatRadius,size[flat?1:0],size[flat?0:1]],got=[(x+w/2-left)/scale-180,(y+h/2-top)/scale,w/scale,h/scale];
        if(!got.every((g,k)=>Math.abs(g-want[k])<=.5))off.push(s.seats[i].split(',')[0]+' at '+got.map(v=>v.toFixed(1)).join(' ')+' for '+want.map(v=>Number(v).toFixed(1)).join(' '));}
      check(s.seatRadius>0&&size.length===2&&off.length===0,'Build AC follow-up: the twelve semantic seat boxes sit on the seats the Dial draws (radius '+s.seatRadius+', '+size.join(' x ')+' along by across the ring, upright) with '+s.seats[sel].split(',')[0]+' framed at '+viewport.width+(off.length?': '+off.join('; '):''));};
    await seatBoxes();
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
    await seatBoxes(); // the boxes follow the turn: Virgo framed after the drag
    await tap(0,654);await waitActive('Virgo');
    // Select a destination through the browser semantic path (assistive action simulation).
    await semantic('seat-9');check((await state()).destination==='Selected: Capricorn','semantic direct selection at '+viewport.width);
    { const b=await looks(); check(lookOf(b,'SEAL').startsWith('plate')&&lookOf(b,'Previous').startsWith('arrow-previous')&&lookOf(b,'Next').startsWith('arrow-next')&&lookOf(b,'Leave the Dial').startsWith('rule')&&small(b).length===0,'batch 2 (3b C): the Dial\'s buttons are bronze: SEAL '+lookOf(b,'SEAL')+', Previous '+lookOf(b,'Previous')+', Next '+lookOf(b,'Next')+', Leave the Dial '+lookOf(b,'Leave the Dial')+', every target 44 px or more (under: '+(small(b).join(', ')||'none')+') at '+viewport.width);
      await page.screenshot({path:path.join(out,viewport.width+'-buttons-dial.png')}); }
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
    check((await state()).casparPose==='moved','Build P: Caspar is moved by the Keeper Key on the return\'s first page at '+viewport.width);
    for(let n=0;n<4&&(await state()).screen==='atriumreturn';n++){await semantic('next-screen');await page.waitForTimeout(150);}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='chamber');
    check((await state()).casparPose==='explain','Build R: Caspar stands behind the Chamber\'s box, explaining, on its first page at '+viewport.width);
    check(!(await state()).canInsert,'the Key cannot be inserted before Caspar finishes at '+viewport.width);
    for(let n=0;n<4&&!(await state()).canInsert;n++){await tap(0,654);await page.waitForTimeout(200);} // the visible Continue on the canvas, where a thumb lands
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canInsert,{},{timeout:5000});
    check(true,'a visible Continue turns Caspar\'s pages in the Chamber at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber.png')});
    await semantic('insert');await page.waitForFunction(()=>window.ascendantDial.snapshot().casparPose==='moved',{},{timeout:30000}); // Build R: he steps into view for "She breathes"
    await page.waitForFunction(()=>window.ascendantDial.snapshot().ended,{},{timeout:40000});
    check((await state()).locksFilled===1 && (await state()).caspar.includes('Let us continue, shall we?'),'one Key fills one lock and the amended ending plays at '+viewport.width);
    check((await state()).casparPose==='','Build R: after the first Key he steps away and the Chamber stays in view at '+viewport.width);
    check(events.some(e=>e.event_name==='key_inserted') && events.some(e=>e.event_name==='prototype_ended'),'chamber events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber-end.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the ending at '+viewport.width);
    // ---- v0.2: the return ----
    const SIGNS=['Aries','Taurus','Gemini','Cancer','Leo','Virgo','Libra','Scorpio','Sagittarius','Capricorn','Aquarius','Pisces'],ELEMENTS=['Fire','Earth','Air','Water'],ELEMENT_OF=i=>ELEMENTS[i%4];
    await semantic('next-screen');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub');
    check((await state()).atriumStage===2 && (await state()).dueCount>=6 && (await state()).caspar.includes('Stirring: one lamp lit'),'the Chamber leads to the Hub in Stage 2 with twelve items ready for practice at once, no in-game time, and the Stirring caption said once, at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().doors.join().includes('chamber-door:unlocked'),{},{timeout:8000}).catch(()=>{}); // Build T: the Chamber door was seen locked at Stage 1; it unlocks with its fade on the return
    { const a=await state(); check(a.atriumKitPieces===20 && a.atriumKitLevel===1 && a.atriumKitRestored<a.atriumKitPieces && a.atriumGrime>0 && a.doors.join()==='sealed-left:locked,wing-door:unlocked,chamber-door:unlocked','Build N: the Atrium kit at Stage 2: pieces still worn, grime in place, the sealed door locked and the Wing and Chamber doors unlocked at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-atrium-kit-stage2.png')}); }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().lightAlpha===0.25,{},{timeout:5000}); // the fade settles, then publishes
    check((await state()).lightAlpha===0.25,'the light overlay follows the stage: a quarter at Stage 2 at '+viewport.width); // Build H
    // ---- v0.4: tap-to-move. Every walk below taps the canvas where the thing is drawn, not its semantic button: the semantic layer passes
    // taps through (pointer-events:none), and for eight days the Atrium's doors and the Chamber's doorway took no canvas tap (a comment had
    // swallowed their Tappable calls, Sept 24) while every check still passed through the semantic buttons. ----
    const atriumState=await state();
    check(atriumState.room==='atrium' && atriumState.avatarAt==='entry' && ['desk','wing-door','caspar','sealed-left','chamber-door'].every(id=>atriumState.pois.includes(id)) && atriumState.canEnterChamber && atriumState.keysInHand===0,'the Atrium lists its points of interest, the Chamber doorway among them, with the marker where you came in at '+viewport.width);
    await page.waitForTimeout(400);await tap(-118,310);await page.waitForFunction(()=>window.ascendantDial.snapshot().note.startsWith('Sealed'),{},{timeout:5000}); // the sealed door, on the canvas
    check(!(await state()).walking,'a sealed door only says it is sealed at '+viewport.width);
    await tap(100,402);await page.waitForFunction(()=>window.ascendantDial.snapshot().avatarAt==='caspar'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // Caspar, on the canvas
    check((await state()).note.includes('Caspar') && events.some(e=>e.event_name==='walk_started_caspar') && events.some(e=>e.event_name==='walk_arrived_caspar'),'tapping Caspar walks the marker to him at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the Hub at '+viewport.width);
    await semantic('mute');await page.waitForFunction(()=>window.ascendantDial.snapshot().muted);check((await page.locator('#mute').getAttribute('aria-pressed'))==='true','the test sound toggle mutes from the Atrium and says so at '+viewport.width); // Build E
    await semantic('mute');await page.waitForFunction(()=>!window.ascendantDial.snapshot().muted);
    // ---- Build F: the desk is dressing, the journal is in the inventory, the fork is on the Dial, practice is the sitting ----
    check((await page.locator('#enter-seals').count())===0 && (await page.locator('#open-journal').count())===1,'Check the Seals is gone from the page; the journal has a control at '+viewport.width);
    await tap(-125,426);await page.waitForFunction(()=>window.ascendantDial.snapshot().avatarAt==='desk'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // the desk, on the canvas
    check((await state()).note.includes('journal') && (await state()).canOpenJournal,'the desk only speaks; the journal is in hand at the Atrium at '+viewport.width);
    await semantic('open-journal');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='journal',{},{timeout:5000});
    // batch 2 (owner, Oct 1: the journal's architecture; Oct 2 evening: the inscription): the first-ever open is the title page; it settles and
    // fades into the landing (the Keeper's record, the Practice and Contents doors); Contents, the Library Map and the links back, all on the canvas
    { const j=await state(); check(j.journal && j.journalView==='title' && j.journalText.includes('These pages fill as you learn.') && !j.canJournalContents,'the journal\'s first-ever open is the title page (Your Journal, KEEPER, These pages fill as you learn.) at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-title.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='landing',{},{timeout:5000});
    { const j=await state(),k=j.journalKeeper||[],name=j.playerName,line="What's up, "+name+". I'm your journal, and I'll keep a record of what you learn from the Library.";
      check(k.length===6 && k[0]==='KEEPER' && k[1]==='1 Key' && k[2]==='\u2609 Taurus \u00b7 \u263d Cancer \u00b7 \u2191 Leo' && k[3]==="What's up, "+name+"." && k[4]==="I'm your journal, and I'll keep a record" && k[5]==="of what you learn from the Library." && k.slice(3).join(' ')===line && j.journalScriptFont==='EBGaramond-Italic','the landing\'s Keeper\'s record: KEEPER, '+k[1]+', '+k[2]+' (the sun as given), and the owner\'s inscription with the saved name ('+k.slice(3).join(' / ')+') at '+viewport.width);
      check(j.journalDoors.length===8 && j.canJournalPractice && j.canJournalContents && j.caspar.includes('Contents: every chapter.') && !j.caspar.includes('not open yet'),'two doors: Practice (it opens Practice, owner Oct 3) and Contents, at '+viewport.width); }
    { const j=await state(); check(j.inscriptionId==='first' && j.inscriptionWriting && j.canJournalPractice,'the living inscription (Oct 7): the Oct 2 line writes itself as the title page finishes fading, and the doors work while it writes, at '+viewport.width); }
    await page.waitForFunction(()=>!window.ascendantDial.snapshot().inscriptionWriting,{},{timeout:8000}); // a line takes 2 to 3.5 s
    await page.waitForTimeout(300); // the fade has settled
    await page.screenshot({path:path.join(out,viewport.width+'-journal-landing.png')});
    // the inscription really draws in the Web build, in the new italic (a font can draw blank on the Web: the symbols, Sept 12): count the light ink on its lines
    { const scale=Math.min(viewport.width/360,viewport.height/800),ox=viewport.width/2,oy=(viewport.height-800*scale)/2;
      const shot=await page.screenshot({clip:{x:ox+(7-118)*scale,y:oy+158*scale,width:236*scale,height:70*scale}});
      const ink=await page.evaluate(async b64=>{const img=new Image();img.src='data:image/png;base64,'+b64;await img.decode();const c=document.createElement('canvas');c.width=img.width;c.height=img.height;const g=c.getContext('2d');g.drawImage(img,0,0);const d=g.getImageData(0,0,c.width,c.height).data;let n=0;for(let i=0;i<d.length;i+=4)if(d[i]>120&&d[i+1]>120&&d[i+2]>130)n++;return n/(d.length/4);},shot.toString('base64'));
      check(ink>.015,'the inscription draws in EB Garamond Italic in the Web build: '+(ink*100).toFixed(1)+'% light ink on its lines at '+viewport.width); }
    // Oct 3 (owner: step 4, Practice, approved): the Practice door opens the list; a whole round on the canvas (a row, the picks, Next), its end and Back to Practice
    { const d=(await state()).journalDoors; await tap(d[0],d[1]); } // the Practice door, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='practice',{},{timeout:5000});
    { const j=await state(); check(j.journalPractice.join()==='The Elements' && j.journalLink==='\u2039 Your Journal' && j.canJournalHome && j.caspar.includes('The Elements: '),'the Practice door on the canvas opens the list of concepts learned: The Elements, \u2039 Your Journal at the top left, at '+viewport.width); }
    { const t=(await looks()).filter(x=>x.k==='row'||x.k==='link'); check(t.length===2 && small(t).length===0,'its row and its link each take 44 px or more ('+t.map(x=>x.w+' '+x.W+'x'+x.H).join(', ')+') at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-practice.png')});
    { const r=(await state()).journalPracticeRows; await tap(r[0],r[1]); } // The Elements' row, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='quiz',{},{timeout:5000});
    { const j=await state(); check(j.quizConcept==='elements' && j.quizCounter==='1 of 6' && j.quizChoices.length>=2 && j.quizChoiceBoxes.length===4*j.quizChoices.length && j.canQuizChoice && !j.canQuizNext && j.journalLink==='\u2039 Practice','a row starts its round: question 1 of 6, its choices in their frames, \u2039 Practice at the top left, at '+viewport.width); }
    { const t=(await looks()).filter(x=>x.k==='choice'); check(t.length>=2 && small(t).length===0,'every choice takes 44 px or more ('+t.map(x=>x.W+'x'+x.H).join(', ')+') at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-practice-question.png')});
    for(let n=0;n<6;n++){ // the right answer first, a wrong one second, then right to the end, all on the canvas
      const j=await state(), b=j.quizChoiceBoxes, pick=n===1?(j.quizRight+1)%j.quizChoices.length:j.quizRight;
      await tap(b[4*pick],b[4*pick+1]);
      await page.waitForFunction(()=>window.ascendantDial.snapshot().canQuizNext,{},{timeout:5000});
      const a=await state();
      if(n<2){ check(a.quizPicked===pick && a.quizFeedback.startsWith(n===0?"That's it. ":"Not quite. ") && a.quizFeedback.length>12,(n===0?'a right pick on the canvas':'a wrong pick on the canvas')+': "'+a.quizFeedback+'", at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+(n===0?'-practice-right.png':'-practice-wrong.png'))}); }
      const q=a.quizButtons; await tap(q[0],q[1]); // Next, on the canvas
      await page.waitForFunction(k=>{const s=window.ascendantDial.snapshot();return s.quizOver||s.quizCounter===(k+2)+' of 6';},n,{timeout:5000});
    }
    { const j=await state(); check(j.quizOver && j.quizEnd.length>0 && j.quizChoices.length===0 && j.canQuizBack && j.caspar.includes(j.quizEnd.split(' ')[0]),'after six, the round ends with the journal\'s line ("'+j.quizEnd+'") and Back to Practice, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-practice-end.png')});
    { const q=(await state()).quizButtons; await tap(q[4],q[5]); } // Back to Practice, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='practice',{},{timeout:5000});
    await page.waitForTimeout(300); await tap(-44,84); // \u2039 Your Journal, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='landing',{},{timeout:5000});
    check(true,'Back to Practice and \u2039 Your Journal on the canvas lead back, at '+viewport.width);
    { const d=(await state()).journalDoors; await tap(d[4],d[5]); } // the Contents door, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='contents',{},{timeout:5000});
    { const j=await state(); check(j.journalChapters.join()==='The Wheel,The Library Map,Sealed,Sealed' && j.journalLink==='\u2039 Your Journal' && j.canJournalHome,'the Contents door on the canvas: The Wheel, The Library Map, two Sealed rows, \u2039 Your Journal at the top left, at '+viewport.width); }
    { const t=(await looks()).filter(x=>x.k==='row'||x.k==='link'); check(t.length===3 && small(t).length===0,'Contents\' two rows and its link each take 44 px or more ('+t.map(x=>x.w+' '+x.W+'x'+x.H).join(', ')+') at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-contents.png')});
    await page.waitForTimeout(300); await tap(7,304); // The Library Map's row, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='map',{},{timeout:5000});
    { const j=await state(); check(j.journalMapRooms.join()==='The Grand Atrium,The Zodiac Wing,The Crystal Book Chamber' && j.journalLink==='\u2039 Contents','the Library Map on the canvas: the plan, the three rooms woken so far named, \u2039 Contents at the top left, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-map.png')});
    await page.waitForTimeout(300); await tap(-44,84); // \u2039 Contents, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='contents',{},{timeout:5000});
    await page.waitForTimeout(300); await tap(-44,84); // \u2039 Your Journal, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='landing',{},{timeout:5000});
    check(true,'\u2039 Contents and \u2039 Your Journal on the canvas lead back, at '+viewport.width);
    await page.waitForTimeout(300); { const d=(await state()).journalDoors; await tap(d[4],d[5]); }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='contents',{},{timeout:5000});
    await page.waitForTimeout(300); await tap(7,200); // The Wheel's row, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='wheel',{},{timeout:5000});
    // Build AA (owner, Sept 30): the Wheel, a chapter now; a seat, the preview, a page, a link, an arrow and Back to the Wheel all work on the canvas
    { const j=await state(); check(j.journal && j.journalView==='wheel' && j.journalLink==='\u2039 Contents' && j.journalSeats.length===12 && j.journalSeats.every(x=>x) && j.journalLenses.length===0 && !j.canJournalTable && j.journalDue.some(d=>d) && !j.canJournalNext && !j.canJournalWheel && j.caspar.includes('The Wheel'),'Contents opens the Wheel, \u2039 Contents at its top left: twelve signs met, the due ones flagged, no tabs or Table yet, at '+viewport.width+' (view '+j.journalView+'; seats '+(j.journalSeats||[]).join(',')+'; due '+(j.journalDue||[]).filter(Boolean).length+'; tabs '+(j.journalLenses||[]).length+'; caspar: '+j.caspar+')'); }
    check((await state()).journalTitleFont==='UnifrakturMaguntia','the journal\'s titles are drawn in blackletter in the Web build (owner, Sept 26) at '+viewport.width);
    // and the title really draws: the Web player can leave a font blank where the Editor shows it (the symbols, Sept 12), so count the gold in the title's box
    { const scale=Math.min(viewport.width/360,viewport.height/800),ox=viewport.width/2,oy=(viewport.height-800*scale)/2;
      const shot=await page.screenshot({clip:{x:ox+(7-70)*scale,y:oy+40*scale,width:140*scale,height:32*scale}});
      const ink=await page.evaluate(async b64=>{const img=new Image();img.src='data:image/png;base64,'+b64;await img.decode();const c=document.createElement('canvas');c.width=img.width;c.height=img.height;const g=c.getContext('2d');g.drawImage(img,0,0);const d=g.getImageData(0,0,c.width,c.height).data;let n=0;for(let i=0;i<d.length;i+=4)if(d[i]>150&&d[i+1]>95&&d[i+2]<140&&d[i]>d[i+2]+60)n++;return n/(d.length/4);},shot.toString('base64'));
      check(ink>.03,'the blackletter title draws in the Web build: '+Math.round(ink*100)+'% gold in its box on the Wheel at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-wheel.png')});
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll in the journal at '+viewport.width);
    await page.waitForTimeout(400); // Unity ignores pointer input in the first frame after the seats appear (see waitActive)
    await tap(7-97.2,296); // Aries' seat at 9 o'clock, on the canvas (SliceView.SeatCentre)
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalSelected==='Aries',{},{timeout:5000});
    { const j=await state(); check(j.journalPreview==='Aries: Fire' && j.canJournalOpen && j.journalView==='wheel','a canvas tap frames Aries; its preview reads Fire, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-preview.png')});
    await tap(3,529); // Open the page, on the card (SliceView: the card's top 452, the button at 77)
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='sign',{},{timeout:5000});
    { const j=await state(); check(j.journalSign==='Aries' && j.journalFacts.join()==='Element: Fire' && j.journalGlyph==='' && j.journalChips.join()==='Leo,Sagittarius' && j.journalRibbonOut && j.journalShader && !/introduced|practicing|practising|mastered/.test(j.journalText) && j.canJournalNext && !j.canJournalPrev && j.canJournalWheel,'Open the page on the canvas: Aries with only its element, the two other Fire signs as links, its ribbon pulled out (due), no state word on screen, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-journal-sign.png')});
    { const b=(await state()).journalChipBoxes; await page.waitForTimeout(300); await tap(b[0],b[1]); } // the first link, where the game reports it
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalSign==='Leo',{},{timeout:5000});check((await state()).journalChips.join()==='Aries,Sagittarius','a link on the canvas opens Leo\'s page, which links back, at '+viewport.width);
    await tap(122,654); // the next-page arrow, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalSign==='Virgo',{},{timeout:5000});check(true,'the arrow on the canvas turns to the next sign at '+viewport.width);
    await tap(0,654); // Back to the Wheel, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='wheel',{},{timeout:5000});check((await state()).journalSelected==='Virgo','Back to the Wheel on the canvas frames the sign last read, at '+viewport.width);
    await tap(0,714); // Close the journal, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:5000});
    check((await state()).avatarAt==='desk','the journal closes back to the Atrium, the marker where it stood, on a canvas tap at '+viewport.width);
    await page.waitForTimeout(400);await tap(0,310);await page.waitForFunction(()=>window.ascendantDial.snapshot().walking&&window.ascendantDial.snapshot().walkTarget==='wing-door',{},{timeout:5000}); // the Wing's door, on the canvas
    await page.screenshot({path:path.join(out,viewport.width+'-walk.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).room==='wing' && (await state()).avatarAt==='atrium-door' && (await state()).pois.join()==='atrium-door,grid,dial,shelf' && (await state()).canEnterDial && !(await state()).canEnterShelf && !(await state()).canEnterGrid && (await state()).canLeaveWing,'the Wing doorway fades into the Wing room with the Dial, the doorway back, a dark shelf, and a dark table at '+viewport.width);
    { const k=await state(); check(k.kitPieces===19 && k.kitLevel===k.keys && k.kitRestored<k.kitPieces && k.grime>0 && Math.abs(k.wingLight-[0,.25,.5,.75,1][k.keys])<.01,'Build M: the Wing kit shows the Keys earned so far, worn pieces and grime still in place, the light by Keys at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-wing-kit-early.png')}); }
    check(events.some(e=>e.event_name==='room_entered_wing'),'room events at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room.png')});
    await tap(157,295);await page.waitForFunction(()=>window.ascendantDial.snapshot().caspar.includes('Dark and quiet'),{},{timeout:5000}); // the shelf, on the canvas
    check(!(await state()).kitUp.includes('shelf') && !(await state()).kitUp.includes('table'),'Build M: before they wake, the shelf and the table lie worn at '+viewport.width);
    check(!(await state()).walking,'before the wheel is lit the shelf only says it is dark at '+viewport.width);
    await tap(-62,400);await page.waitForFunction(()=>window.ascendantDial.snapshot().caspar.includes('dark and bare'),{},{timeout:5000}); // the table, on the canvas
    check(!(await state()).walking && !(await state()).gridOpen,'before the modality unit the table only says it is bare at '+viewport.width);
    await tap(-138,292);await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // the Wing room's doorway back, on the canvas
    check(true,'the Wing room\'s doorway back, tapped on the canvas, walks out to the Atrium at '+viewport.width);
    await page.waitForTimeout(400);await tap(0,310);await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // and in again by the Wing's door
    check(true,'the Wing\'s door, tapped on the canvas, walks back in at '+viewport.width);
    await page.waitForTimeout(400);await tap(40,337); // the Dial, on the canvasawait page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveDial,{},{timeout:15000}); // the wheel's Back button appears a frame after the screen
    check((await state()).avatarAt==='dial' && (await state()).canEnterPractice && (await state()).canContinueLesson && !(await state()).active && (await state()).canLeaveDial,'the Dial opens once the marker reaches it, on the fork: the lesson or practice, nothing started, Back still offered, at '+viewport.width);
    await page.waitForFunction(()=>{const h=window.ascendantDial.snapshot().dialBoxHeight;return h>0&&h<128;},{},{timeout:5000}); // Build R: the fork's short line gets a short box
    await page.screenshot({path:path.join(out,viewport.width+'-fork.png')});
    check((await state()).dialBoxHeight<128,'Build R: the Dial\'s box fits its short line (no empty space) at '+viewport.width);
    await tap(78,714); // Practice what you know, on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='practice'&&window.ascendantDial.snapshot().practiceMode==='dial',{},{timeout:10000});
    check((await state()).sitting===1 && (await state()).practiceCount===6 && (await state()).phase==='Practice · 1 of 6' && (await state()).canLeavePractice && !(await state()).canLeaveDial,'a canvas tap on the fork opens practice as the first sitting: six items, headed as practice, an exit on the item, no Back over the Seal at '+viewport.width);
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
          if(!reloadedMidPractice){ // Build S (owner, APK playtest, Sept 29): the long intro turns in pages, its element words in colour
            await page.waitForFunction(()=>window.ascendantDial.snapshot().casparPages>0,{},{timeout:5000});
            const p1=await state();check(p1.casparPages>=2&&p1.casparPage===1&&p1.message.startsWith('Before you turn the wheel')&&p1.casparShown.length<p1.message.length,'Build S: the Dial\'s long line shows page 1 of '+p1.casparPages+' ('+JSON.stringify(p1.casparShown)+'), the whole line still in the state at '+viewport.width);
            await tap(114,462+p1.dialBoxHeight-17);await page.waitForFunction(()=>window.ascendantDial.snapshot().casparPage===2,{},{timeout:5000}).catch(()=>{});
            await page.waitForTimeout(150);await page.screenshot({path:path.join(out,viewport.width+'-dial-page2.png')});
            const p2=await state();check(p2.casparPage===2&&p2.message===p1.message&&p2.casparShown!==p1.casparShown&&p2.active,'Build S: a tap on the box\'s Continue turns to the next page and the wheel stays live at '+viewport.width);
            const shown=[p1.casparShown,p2.casparShown];for(let k=3;k<=p2.casparPages;k++){await semantic('caspar-page');await page.waitForFunction(k=>window.ascendantDial.snapshot().casparPage===k,k,{timeout:5000});shown.push((await state()).casparShown);}
            check(shown.some(t=>t.includes('<color=#E0643C>Fire</color>'))&&shown.some(t=>t.includes('<color=#63A6E0>Water</color>')),'Build S: the pages name Fire and Water in their colours at '+viewport.width);
            await page.screenshot({path:path.join(out,viewport.width+'-dial-pages.png')});
            reloadedMidPractice=true;const before=(await state()).dueCount;await page.screenshot({path:path.join(out,viewport.width+'-practice-dial.png')});await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
            check((await state()).dueCount===before && (await state()).sitting===1,'a reload during practice keeps the deck, its ready count, and the sitting at '+viewport.width);
            await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
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
    await semantic('leave-practice');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canLeaveDial&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await page.waitForFunction(()=>{const s=window.ascendantDial.snapshot();return s.canLeaveDial&&!s.busy&&!s.active;},{},{timeout:15000}).catch(()=>{});await semantic('leave-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).screen==='wingroom' && (await state()).canLeaveWing && !(await state()).canLeaveDial,'leaving the Dial lands in the Zodiac Wing, not the Atrium; the room offers the way back (note 10) at '+viewport.width);
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // the room's doorway back returns to the Atrium
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('enter-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await waitActive('Gemini');
    check((await state()).avatarAt==='dial' && (await state()).fork==='none','the lesson continues from the fork at '+viewport.width);
    check((await state()).hintLevel===0 && !(await state()).phase.includes('Help level'),'Unit 1.1 continues on the player\'s own, no level numbers on screen, at '+viewport.width);
    for(const [from,to] of [[2,6],[6,10],[3,7],[7,11]]){await waitActive(SIGNS[from]);await semantic('seat-'+to);await semantic('seal');}
    await page.waitForFunction(()=>window.ascendantDial.snapshot().trianglesMode==='payoff',{},{timeout:8000}).catch(()=>{}); // batch 2, step 5: the payoff, once, as the whole wheel lights
    { const t=await state(); check(t.trianglesMode==='payoff' && t.trianglesCrossing===0,'the moment the whole wheel lights, all four triangles burn as flame (level B) at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-triangles-payoff.png')}); }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().wheelComplete&&window.ascendantDial.snapshot().canLeaveDial,{},{timeout:20000});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().trianglesMode==='resting',{},{timeout:8000}).catch(()=>{}); check((await state()).trianglesMode==='resting','then the flames burn down into the resting lines at '+viewport.width);
    check((await state()).seats.every(x=>!x.includes('dormant')) && events.filter(e=>e.event_name==='key1_earned').length===1,'twelve seats lit with no second Key at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-lit.png')});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===3&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).v02Complete && (await state()).avatarAt==='wing-door','one more return completes v0.2, the marker back at the Wing doorway at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-complete.png')});
    // ---- v0.3: glyphs and Key 2 ----
    await semantic('poi-wing-door');
    try{await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});}
    catch(e){console.error('wing button stalled: '+JSON.stringify(await state()));console.error('last events: '+JSON.stringify(events.slice(-14).map(x=>x.event_name+'@'+x.input_method)));throw e;}
    check((await state()).canEnterShelf && (await state()).pois.includes('shelf'),'the lit wheel wakes the bookshelf as a point of interest at '+viewport.width);
    { const s=await state(); check(s.shelfLight===1&&!s.shelfRing,'Build Y: the waking shelf glows itself (its own file lit, edge hugging it), no portal ring, at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-shelf-glow.png')});
      check(s.shelfEdge==='halo','APK Session 2, bug 1: the waking shelf\'s edge is a halo drawn from its silhouette, with no shifted copies of the picture (no smear), at '+viewport.width); }
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).glyphMode==='' && (await state()).message.includes('book upon the shelf'),'the Dial before the book only points at the shelf at '+viewport.width);
    await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveDial,{},{timeout:15000}); // the wheel's Back button appears a frame after the screen
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).kitUp.includes('shelf'),'Build M: the shelf stands restored once the book of symbols wakes, before its lesson at '+viewport.width);
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='book'&&window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    check(!!(await state()).glyphChar && (await state()).glyphOptions.length===4,'the book opens Part A: a symbol and four names at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-a.png')});
    { const b=await looks(),o=(await state()).glyphOptions; check(o.every(w=>lookOf(b,w).startsWith('plate'))&&lookOf(b,'Close the Book').startsWith('rule')&&small(b).length===0,'batch 2 (owner, Oct 2: bronze on the Book too): the Book of Symbols\' four names are plates ('+o.map(w=>lookOf(b,w)).join(', ')+') and Close the Book is on its rule ('+lookOf(b,'Close the Book')+'), every target 44 px or more (under: '+(small(b).join(', ')||'none')+') at '+viewport.width); }
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphMode==='name'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
      const s=await state();
      const target=SIGNS.findIndex(name=>s.glyphChar===['\u2648','\u2649','\u264A','\u264B','\u264C','\u264D','\u264E','\u264F','\u2650','\u2651','\u2652','\u2653'][SIGNS.indexOf(name)]);
      const slot=s.glyphOptions.indexOf(SIGNS[target]);
      await semantic('glyph-name-'+slot);await page.waitForTimeout(200);
      if(n===0){ // the Book comes alive (owner, Oct 7-8; 86bcf0x71): the answered symbol becomes its element's emblem, the right plate floats, then it inks the page
        await page.waitForFunction(()=>window.ascendantDial.snapshot().bookEmblem!=='',{},{timeout:2000}).catch(()=>{});
        const a=await state(); check(a.bookArt && a.bookEmblem==='emblem-'+SIGNS[target].toLowerCase() && a.bookPicked===SIGNS[target],'a right answer raises '+SIGNS[target]+'\'s emblem from the Book, its plate floating ('+a.bookEmblem+', '+a.bookPicked+'), at '+viewport.width);
        await page.screenshot({path:path.join(out,viewport.width+'-book-emblem.png')});
        await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        const b=await state(); check(b.bookEmblem==='' && b.bookInk[target]===2 && b.bookInk.filter(x=>x>0).length===1,'then it sinks into the pages as ink, in full (answered on its own), and no other symbol is inked, at '+viewport.width);
      }
      if(n===11){await page.waitForFunction(()=>window.ascendantDial.snapshot().bookPages,{},{timeout:15000});
        const p=await state(); check(p.bookPages && p.bookInk.every(x=>x>0),'the twelfth answer shows the full pages a moment, every symbol inked, before the room at '+viewport.width);
        await page.screenshot({path:path.join(out,viewport.width+'-book-pages.png')});
        check(p.bookInkDrawn===12,'all twelve inked symbols are really drawn on the pages ('+p.bookInkDrawn+' laid out) at '+viewport.width);
        // each laid-out symbol against the Book's own art (720x1600, two art px to a layout px): its whole box is parchment, not the gutter, the page's edge, or the cover
        const art='data:image/png;base64,'+fs.readFileSync(path.join(__dirname,'..','Assets','CelestialDial','Resources','Art','book-room.png')).toString('base64');
        const share=await page.evaluate(async({src,box})=>{const i=new Image();i.src=src;await i.decode();const c=document.createElement('canvas');c.width=i.width;c.height=i.height;const g=c.getContext('2d');g.drawImage(i,0,0);const d=g.getImageData(0,0,c.width,c.height).data;
          const parch=(x,y)=>{const k=(y*c.width+x)*4,R=d[k],G=d[k+1],B=d[k+2];return R>150&&G>105&&B>55&&R-B>30&&R+G+B>400;};
          const out=[];for(let s=0;s<12;s++){const [x0,y0,x1,y1]=box.slice(s*4,s*4+4);let n=0,on=0;for(let y=Math.floor(y0*2);y<Math.ceil(y1*2);y++)for(let x=Math.floor((x0+180)*2);x<Math.ceil((x1+180)*2);x++){n++;if(x>=0&&y>=0&&x<c.width&&y<c.height&&parch(x,y))on++;}out.push(n?on/n:0);}return out;},{src:art,box:p.bookInkBox});
        check(p.bookInkBox.length===48 && share.every(x=>x>=.97),'every inked symbol sits wholly on the parchment (least '+Math.round(Math.min(...share)*100)+'% parchment under a symbol) at '+viewport.width); }
      if(n===4){
        await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy);
        await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
        await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
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
    { const z=await state(); await page.screenshot({path:path.join(out,viewport.width+'-eye-symbols.png')});
      check(z.revealsPlayed.includes('symbols')&&z.eyeText==='Find the symbol of\n'+z.glyphTarget&&z.signLabel==='','Build Z: the symbols open with their own reveal; the eye asks for the symbol and nothing above it names a sign at '+viewport.width); }
    { const b=await state(); check(!SIGNS.some(n=>b.destination.includes(n)) && b.destination.startsWith('Selected: symbol ') && b.start==='' && b.count==='' && b.challenge===b.glyphTarget,'Part B readouts do not name the sign under the bracket; the center holds the target name (Build I) at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-glyphs-b.png')});
    for(let n=0;n<12;n++){
      await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
      const target=SIGNS.indexOf((await state()).glyphTarget);
      await semantic('seat-'+target);await semantic('seal');await page.waitForTimeout(300);
      if(n===4){
        await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy);
        await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
        await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().glyphWheel&&window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        check((await state()).glyphTarget==='Virgo','mid-placement reload resumes at Virgo after five committed placements at '+viewport.width);
        await page.screenshot({path:path.join(out,viewport.width+'-symbol-placement-restored.png')});
      }
    }
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyCeremony===2,{},{timeout:20000});await page.waitForTimeout(950);await page.screenshot({path:path.join(out,viewport.width+'-key2-ceremony.png')}); // Build I: the Key rises
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key2&&window.ascendantDial.snapshot().canLeaveDial,{},{timeout:20000});
    check((await state()).keys===2 && events.filter(e=>e.event_name==='key2_earned').length===1,'twelve symbols placed earns Key 2 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-key2.png')});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    // ---- Build D: the finished loop. Keys earned are spent in the Chamber; the Atrium restores on the return. ----
    const spendAtBooks=async(tag)=>{
      await page.waitForTimeout(400);await tapToWalk(118,310,"the Chamber's door ("+tag+")");await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='chamberroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // the Chamber's door, on the canvas
      check((await state()).room==='chamber' && (await state()).avatarAt==='atrium-door' && (await state()).pois.join()==='atrium-door,books' && !(await state()).canInsert,'the Chamber doorway fades into the Chamber as a room ('+tag+') at '+viewport.width);
      await page.waitForTimeout(400);await tap(0,300);await page.waitForFunction(()=>window.ascendantDial.snapshot().atBooks&&window.ascendantDial.snapshot().canInsert,{},{timeout:15000}); // the Books, on the canvas
      const before=(await state()).keysSpent;await page.waitForTimeout(300);await tap(0,654); // the Insert button on the canvas, where a thumb lands
      await page.waitForFunction(k=>window.ascendantDial.snapshot().keysSpent===k+1,before,{},{timeout:5000});
      await page.waitForFunction(()=>!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    };
    check((await state()).atriumStage===3 && (await state()).keysInHand===1 && (await state()).caspar.includes('carry a Key'),'one more return: Key 2 in hand, the Atrium waits for it to be spent at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key-in-hand.png')});
    await spendAtBooks('Key 2');
    check((await state()).keysSpent===2 && (await state()).keysInHand===0 && (await state()).booksOpen===0 && !(await state()).canInsert && events.filter(e=>e.event_name==='key_spent').length===1,'Key 2 fills the second lock; the Book stays shut; nothing more to spend at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-chamber-lock2.png')});
    await page.waitForFunction(()=>window.ascendantDial.snapshot().chamberKitRestored===3,{},{timeout:8000}); // the restore fade settles piece by piece
    { const c=await state(); check(c.chamberKitPieces===16 && c.chamberKitLevel===1 && c.chamberKitRestored===3,'Build O on the arc: two Keys spent light the mechanism and two candles; the rest of the Chamber waits for later Books at '+viewport.width); }
    { const kitted=(await state()).chamberKitLevel>=0; await page.waitForTimeout(400); await tap(kitted?-145:-150,kitted?287:347); } // the Chamber's doorway back, on the canvas (where the kit paints it, or the greybox door)
    await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===4&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).v03Complete && (await state()).avatarAt==='chamber-door' && (await state()).caspar.includes('Two locks filled'),'the return after spending takes the Atrium to Stage 4 at '+viewport.width);
    // ---- v0.3 revision, build 3: a clean replay hardens the symbols ----
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
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
    await page.waitForFunction(()=>window.ascendantDial.snapshot().cleanRuns===1&&!window.ascendantDial.snapshot().practice&&window.ascendantDial.snapshot().canLeaveDial,{},{timeout:20000});
    check((await state()).hard && (await state()).keys===2 && events.filter(e=>e.event_name==='key2_earned').length===1 && events.some(e=>e.event_name==='symbol_practice_clean'),'a clean replay is recorded once and the next one is hard; Key 2 is not re-earned at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-practice-clean.png')});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('poi-shelf');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='book'&&window.ascendantDial.snapshot().glyphMode==='name',{},{timeout:15000});
    { const h=await state(); check(h.hard && h.glyphChar!=='\u2648' && new Set(h.glyphOptions).size===4,'the hard replay shuffles the order and keeps four distinct names at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-practice-hard.png')});
    await semantic('close-book');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    // ---- Build A: the modalities on the Dial ----
    check((await state()).dialGlow===1,'with Key 2 in hand the Dial glows in the Wing room: a unit waits there (Build I) at '+viewport.width);await page.screenshot({path:path.join(out,viewport.width+'-wing-room-dial-glow.png')});
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await page.waitForFunction(()=>{const z=window.ascendantDial.snapshot();return z.unit==='modalities'&&z.active&&z.revealsPlayed.includes('modalities')&&z.revealing==='';},{},{timeout:15000}); // Build Z: the pattern's reveal plays first
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
    await page.waitForFunction(()=>window.ascendantDial.snapshot().modalitiesComplete&&window.ascendantDial.snapshot().canLeaveDial,{},{timeout:20000});
    check((await state()).keys===2 && (await state()).litModCount===12 && events.filter(e=>e.event_name==='modality_family_completed').length===3,'three modality families of four complete with no new Key at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-modalities-complete.png')});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    let sawModality=false;
    for(let round=0;round<6&&!sawModality;round++){ // three kinds, thirty-six items: older items come first
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
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
    if((await state()).screen==='wing'){await page.waitForFunction(()=>window.ascendantDial.snapshot().canLeaveDial,{},{timeout:15000});}
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // from the Dial, two presses: into the room, then out (note 10)
    }
    check(sawModality,'a practice carries modality items within six sittings at '+viewport.width);
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).modalitiesComplete && (await state()).gridOpen && !(await state()).gridStarted,'a reload keeps the modality unit complete, and the table has woken at '+viewport.width);
    // ---- Build B: the table and Key 3 ----
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).canEnterGrid && (await state()).caspar.includes('table has woken'),'the finished modality unit wakes the table with a room button at '+viewport.width);
    check((await state()).kitUp.includes('table'),'Build M: the table stands restored the moment it wakes, before its lesson at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room-table.png')});
    await semantic('enter-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&window.ascendantDial.snapshot().canGridPick,{},{timeout:15000});
    { const g=await state(); check(g.avatarAt==='grid'&&g.gridStarted && g.gridTiles.length===12 && g.gridCells.length===12 && g.gridCells.every(c=>c.endsWith(': empty')) && g.gridTiles.every(t=>t.endsWith(', not placed')) && g.caspar.startsWith('Four elements and three modalities') && g.keys===2,'the room button walks to the table and opens it: twelve tiles, twelve empty cells, the intro line at '+viewport.width); }
    { const g=await state(); // the row and column names sit on the table itself, not off its frame into the dark (owner, Oct 8): each laid-out name against table-room (two art px to a layout px)
      const art='data:image/png;base64,'+fs.readFileSync(path.join(__dirname,'..','Assets','CelestialDial','Resources','Art','table-room.png')).toString('base64');
      const share=await page.evaluate(async({src,box})=>{const i=new Image();i.src=src;await i.decode();const c=document.createElement('canvas');c.width=i.width;c.height=i.height;const k2=c.getContext('2d');k2.drawImage(i,0,0);const d=k2.getImageData(0,0,c.width,c.height).data;
        const wood=(x,y)=>{const k=(y*c.width+x)*4,R=d[k],G=d[k+1],B=d[k+2];return .3*R+.59*G+.11*B>22&&R>B+25;}; // the frame's warm wood, not the near-black mist round the table
        const out=[];for(let s=0;s<box.length/4;s++){const [x0,y0,x1,y1]=box.slice(s*4,s*4+4);let n=0,on=0;for(let y=Math.floor(y0*2);y<Math.ceil(y1*2);y++)for(let x=Math.floor((x0+180)*2);x<Math.ceil((x1+180)*2);x++){n++;if(x>=0&&y>=0&&x<c.width&&y<c.height&&wood(x,y))on++;}out.push(n?on/n:0);}return out;},{src:art,box:g.gridEdgeBox});
      check(g.gridArt && g.gridEdgeBox.length===28 && g.gridEdgeBox.every(v=>Number.isFinite(v)) && share.every(x=>x>=.95),'every row and column name sits on the table\'s wood (least '+Math.round(Math.min(...share)*100)+'% wood under a name) at '+viewport.width); }
    check(await page.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll at the table at '+viewport.width);
    { const b=await looks(),signs=['Aries','Taurus','Gemini','Cancer','Leo','Virgo','Libra','Scorpio','Sagittarius','Capricorn','Aquarius','Pisces']; check(lookOf(b,'SEAL').startsWith('plate')&&lookOf(b,'Leave the Table').startsWith('rule')&&signs.every(w=>b.some(x=>x.w===w&&x.H>=44))&&small(b).length===0,'batch 2 (owner, Oct 2: bronze on the Table too; 3d: 44 px): the Table\'s SEAL is a plate ('+lookOf(b,'SEAL')+'), Leave the Table is on its rule ('+lookOf(b,'Leave the Table')+'), and its twelve sign tiles are 44 px tall, every target 44 px or more (under: '+(small(b).join(', ')||'none')+') at '+viewport.width);
      await page.screenshot({path:path.join(out,viewport.width+'-buttons-table.png')}); }
    const cellBoxes=await page.locator('#grid-cells button').evaluateAll(bs=>bs.map(b=>({width:b.getBoundingClientRect().width,height:b.getBoundingClientRect().height,label:b.getAttribute('aria-label')})));
    check(cellBoxes.length===12 && cellBoxes.every(b=>b.width>=48 && b.height>=48) && cellBoxes[1].label==='Fire, fixed: empty' && cellBoxes[11].label==='Water, mutable: empty','twelve semantic cells at the target floor, labeled by element and kind, at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid.png')});
    const CELL=seat=>(seat%4)*3+seat%3;
    const settled=async()=>page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    const seatSign=async(seat)=>{await semantic('grid-sign-'+seat);await semantic('grid-cell-'+CELL(seat));await semantic('grid-seal');await settled();};
    await page.waitForTimeout(300);
    await tap(-129,344);await tap(-72,128);await tap(0,654); // the thumb path: the Aries tile, the Fire-cardinal cell, Seal on the canvas
    await page.waitForFunction(()=>window.ascendantDial.snapshot().busy,{},{timeout:3000}).catch(()=>{});
    { const g=await state(); check(g.busy && g.gridPlates[CELL(0)]==='gold' && !g.gridGoldWhole[CELL(0)],'without Reduced motion the gold spreads from the centre: not yet whole at the Seal at '+viewport.width); }
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
    // the Table comes alive (owner, Oct 7-8; 86bcf0x71): gold for a sign placed on the player's own, wood for one Caspar's rule placed; the lettering alive on both
    { const g=await state(); check(g.gridArt && g.gridPlates[CELL(0)]==='gold' && g.gridPlates[CELL(1)]==='gold' && g.gridPlates[CELL(2)]==='wood' && [0,1,2].every(k=>g.gridLive[CELL(k)]) && g.gridPlates.filter(x=>x).length===3,'the Table comes alive: Aries and Taurus (on their own) gold, Gemini (after asking) wood, all three with live lettering (symbol and name, both edged), at '+viewport.width); }
    // drag (owner, Oct 7: drag and tap): the Cancer plate dragged on the canvas from the tray into its well, then Seal
    { const scale=Math.min(viewport.width/360,viewport.height/800),X=x=>viewport.width/2+x*scale,Y=y=>(viewport.height-800*scale)/2+y*scale;
      await page.mouse.move(X(129),Y(342));await page.mouse.down();for(let k=1;k<=12;k++){await page.mouse.move(X(129+(-72-129)*k/12),Y(342+(284-342)*k/12));await page.waitForTimeout(16);}
      { const g=await state(); check(g.gridDrag==='Cancer' && g.gridHover===CELL(3),'a plate dragged over a well lights it: Cancer over Water, cardinal, at '+viewport.width); }
      await page.mouse.up();await page.waitForTimeout(150); }
    { const g=await state(); check(g.gridSign==='Cancer' && g.gridCell===CELL(3) && g.gridPlates[CELL(3)]==='pending' && g.gridDrag==='' && events.some(e=>e.event_name==='cell_chosen'&&e.input_method==='Drag'),'let go over the well, the plate sits in it until Seal, and the choice is logged as a drag, at '+viewport.width); }
    await page.screenshot({path:path.join(out,viewport.width+'-grid-drag.png')});
    await semantic('grid-seal');await page.waitForTimeout(250);
    await page.screenshot({path:path.join(out,viewport.width+'-grid-gold.png')});await settled();
    check((await state()).gridPlaced===4 && (await state()).gridPlates[CELL(3)]==='gold' && events.filter(e=>e.event_name==='grid_placed')[3].evidence_eligible,'the dragged plate seals like a tapped one and turns gold at '+viewport.width);
    await seatSign(4);
    check((await state()).gridPlaced===5,'five seated at '+viewport.width);
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).gridStarted && (await state()).gridPlaced===5 && (await state()).keys===2,'a reload mid-table keeps the five seated signs at '+viewport.width);
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('already placed'),'the room says the table waits, part seated, at '+viewport.width);
    await semantic('poi-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='grid'&&window.ascendantDial.snapshot().canGridPick,{},{timeout:15000});
    check((await state()).avatarAt==='grid'&&(await state()).gridPlaced===5 && (await state()).caspar.includes('5 of twelve') && (await state()).gridCells[CELL(4)]==='Fire, fixed: Leo','tapping the table resumes it with its five seated signs at '+viewport.width);
    { const g=await state(); check(g.gridPlates[CELL(0)]==='gold' && g.gridPlates[CELL(2)]==='wood' && g.gridPlates[CELL(4)]==='gold','after a reload the gold and wood plates read from the deck as they were at '+viewport.width); }
    await semantic('grid-sign-5');for(const wrong of [6,7,8]){await semantic('grid-cell-'+CELL(wrong));await semantic('grid-seal');await page.waitForTimeout(150);} // Virgo: three wrong cells
    await page.waitForFunction(()=>window.ascendantDial.snapshot().gridHintLevel===3||window.ascendantDial.snapshot().gridPlaced===6,{},{timeout:10000});
    await settled();
    check((await state()).gridPlaced===6 && (await state()).caspar.includes('Virgo is placed') && !events.filter(e=>e.event_name==='grid_placed')[5].evidence_eligible,'three wrong cells hand the sign to Caspar, who seats it without evidence at '+viewport.width);
    check((await state()).gridPlates[CELL(5)]==='wood' && (await state()).gridLive[CELL(5)],'the sign Caspar placed stays wood, its lettering alive (owner, Oct 7, 1A), at '+viewport.width);
    for(let seat=6;seat<8;seat++)await seatSign(seat);
    // Reduced motion (the brief: "Reduced motion throughout"): a correct Seal shows the gold whole and the lettering awake at once, with no spread
    await semantic('motion');await page.waitForFunction(()=>window.ascendantDial.snapshot().reducedMotion,{},{timeout:5000});
    await semantic('grid-sign-8');await semantic('grid-cell-'+CELL(8));await semantic('grid-seal');await page.waitForTimeout(120);
    { const g=await state(); check(g.busy && g.gridPlates[CELL(8)]==='gold' && g.gridGoldWhole[CELL(8)] && g.gridLive[CELL(8)],'with Reduced motion a correct Seal shows the gold whole and the lettering awake at once, inside the hold, at '+viewport.width); }
    await settled();
    await seatSign(9);check((await state()).gridGoldWhole[CELL(9)],'with Reduced motion the next plate is gold too at '+viewport.width);
    await semantic('motion');await page.waitForFunction(()=>!window.ascendantDial.snapshot().reducedMotion,{},{timeout:5000});
    await seatSign(10);
    await semantic('grid-sign-11');await semantic('grid-cell-'+CELL(11));await semantic('grid-seal'); // the last seat without settling, so the capture lands mid-ceremony
    await page.waitForFunction(()=>window.ascendantDial.snapshot().keyCeremony===3,{},{timeout:20000});await page.waitForTimeout(950);await page.screenshot({path:path.join(out,viewport.width+'-key3-ceremony.png')}); // Build I: the Key rises
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key3&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});
    { const g=await state(); check(g.gridPlates[CELL(11)]==='gold' && g.gridGoldWhole[CELL(11)] && g.gridLive[CELL(11)],'the twelfth plate ends gold and whole, its lettering awake, after the Key 3 ceremony at '+viewport.width); }
    check((await state()).keys===3 && (await state()).gridPlaced===12 && (await state()).gridComplete && !(await state()).canGridPick && events.filter(e=>e.event_name==='key3_earned').length===1 && (await state()).caspar.includes('Keeper Key 3 is yours'),'twelve seated earns Key 3 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-grid-key3.png')});
    await semantic('leave-grid');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).atriumStage===4 && (await state()).keysInHand===1,'back in the Atrium with Key 3 in hand at '+viewport.width);
    await spendAtBooks('Key 3');
    check((await state()).keysSpent===3 && (await state()).booksOpen===1 && !(await state()).wingWhole && events.some(e=>e.event_name==='book_opened_1') && (await state()).caspar.includes('Book opens'),'Key 3 fills the third lock and Book 1 opens at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-book-opens.png')});
    { const st=await state(),b=await looks(); check(st.canLeaveChamber && st.pois.includes('atrium-door') && !b.some(x=>x.w==='RETURN TO THE ATRIUM'),'the art pass (86bcex5kc 1A): in the Chamber the doorway back takes the tap and Return to the Atrium is gone at '+viewport.width); }
    await semantic('poi-atrium-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===5&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    { const st=await state(),b=await looks(); check(st.pois.includes('wing-door') && st.pois.includes('chamber-door') && !b.some(x=>x.w==='THE ZODIAC WING'||x.w==='THE CRYSTAL BOOK CHAMBER'),'the art pass (86bcex5kc 1A): from Stage 2 the Atrium has no Zodiac Wing or Chamber button; both doors take the tap at '+viewport.width); }
    check((await state()).caspar.includes('Three locks') && !(await state()).caspar.includes('Stirring'),'the return takes the Atrium to Stage 5 with three Keys spent; the Stirring caption is not repeated (note 9) at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key3.png')});
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).atriumStage===5 && (await state()).keys===3 && (await state()).keysSpent===3 && (await state()).booksOpen===1 && (await state()).key3 && (await state()).gridComplete && (await state()).cleanRuns===1 && (await state()).avatarAt==='entry','a reload resumes at the Hub from the local save with three Keys spent, Book 1 open, the full table, and the clean run at '+viewport.width);
    // ---- Build C: polarity, the six opposite pairs, the builder, and Key 4 ----
    const OPP=seat=>(seat+6)%12;
    const unitActive=async()=>{await page.waitForFunction(()=>window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});await page.waitForTimeout(400);await page.waitForFunction(()=>window.ascendantDial.snapshot().active&&!window.ascendantDial.snapshot().busy,{},{timeout:20000});await page.waitForTimeout(150);}; // a guided problem shows its count a frame after it starts
    const startSeat=async()=>SIGNS.indexOf((await state()).start.replace('Start: ',''));
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('last pattern'),'with three Keys the room points at the wheel\'s last pattern at '+viewport.width);
    await semantic('poi-dial');await page.waitForFunction(()=>window.ascendantDial.snapshot().fork==='both'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('continue-lesson');await page.waitForFunction(()=>{const z=window.ascendantDial.snapshot();return z.unit==='opposites'&&z.canContinue&&z.revealsPlayed.includes('opposites')&&z.revealing===''&&!z.busy;},{},{timeout:15000}); // Build Z: the pattern's reveal plays first
    check(!(await state()).polarityShown && (await state()).message.startsWith('The wheel holds one last secret') && !(await state()).active && (await state()).canLeaveDial,'after Key 3 the Dial opens the polarity beat; Leave the Dial stays on its own row, clear of the beat\'s Continue (Build T) at '+viewport.width);
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
    await semantic('continue');await page.waitForFunction(()=>{const z=window.ascendantDial.snapshot();return z.builderStep==='name'&&z.canBuilderName&&z.revealsPlayed.includes('builder')&&z.revealing==='';},{},{timeout:8000}); // Build Z: the pattern's reveal plays first
    check((await state()).revealsPlayed.join()==='builder,elements,modalities,opposites,symbols','Build Z: each of the five patterns has played its reveal once at '+viewport.width);
    check((await state()).speaker==='dial'&&(await state()).message.startsWith('Build me a sign from its parts'),'Build V: the Dial, not Caspar, asks for the sign built from its parts at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-dial-speaks.png')});
    { const b=await state(); check(b.unit==='builder' && b.builderAsk==='Earth, fixed' && b.builderOptions.length===4 && b.builderOptions.includes('Taurus') && b.built===0,'Continue opens the builder on the sun sign\'s parts: four names (Leave the Dial on its own row below them), at '+viewport.width); }
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
    await page.waitForFunction(()=>window.ascendantDial.snapshot().key4&&window.ascendantDial.snapshot().canLeaveDial,{},{timeout:20000});
    check((await state()).keys===4 && (await state()).built===3 && events.filter(e=>e.event_name==='key4_earned').length===1 && (await state()).message.includes('Keeper Key 4 is yours'),'three signs built with one unassisted earns Key 4 once at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-key4.png')});
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).atriumStage===5 && (await state()).keysInHand===1,'back in the Atrium with Key 4 in hand at '+viewport.width);
    await spendAtBooks('Key 4');
    check((await state()).keysSpent===4 && (await state()).booksOpen===1 && (await state()).wingWhole && !(await state()).canInsert && events.some(e=>e.event_name==='wing_whole') && (await state()).caspar.includes('Wing is whole') && (await state()).caspar.includes('The Zodiac Wing is complete'),'Key 4 fills Book 2\'s first lock: the Wing is whole, with the closing line and the end card at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-whole.png')});
    await semantic('poi-atrium-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&window.ascendantDial.snapshot().atriumStage===6&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    check((await state()).caspar.includes('Four Keys spent') && !(await state()).caspar.includes('Stirring'),'the return takes the Atrium to Stage 6 with four Keys spent; no caption repeats itself (note 9) at '+viewport.width);
    await page.waitForFunction(()=>{const l=window.ascendantDial.snapshot().lightAlpha;return l>0.59&&l<0.6;},{},{timeout:5000});
    check((await state()).lightAlpha>0.59&&(await state()).lightAlpha<0.6,'the light overlay sits at half plus four of 21 locks once the Wing is whole; full light waits for the last Book (owner, Sept 27) at '+viewport.width); // Build H, rekeyed to the arc
    await page.screenshot({path:path.join(out,viewport.width+'-hub-key4.png')});
    await page.reload();await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check((await state()).atriumStage===6 && (await state()).keys===4 && (await state()).keysSpent===4 && (await state()).wingWhole && (await state()).key4 && (await state()).polarityShown && (await state()).oppositesComplete && (await state()).avatarAt==='entry','a reload resumes at the Hub from the local save with four Keys spent, the Wing whole, the sides, and the six pairs at '+viewport.width);
    // Build L: the Wing at full light (Stage 6), the capture the owner judges the light overlay by
    await page.waitForFunction(()=>window.ascendantDial.snapshot().atriumKitRestored===9,{},{timeout:8000}); // the restore fade settles piece by piece
    { const a=await state(); check(a.atriumKitLevel===2 && a.atriumKitRestored===9 && a.atriumGrime>0.59 && a.atriumGrime<0.61,'Build N on the arc: with the Wing whole the Atrium holds 9 of 20 pieces and its grime; the rest waits for the other six Books (owner, Sept 27) at '+viewport.width); await page.screenshot({path:path.join(out,viewport.width+'-atrium-kit-wing-whole.png')}); }
    await semantic('poi-wing-door');await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});await page.waitForTimeout(1200);check((await state()).lastDoorOpened==='wing-door','Build N: the Wing door swings open as the Keeper reaches it at '+viewport.width);
    await page.screenshot({path:path.join(out,viewport.width+'-wing-room-stage6.png')});
    { const k=await state(); check(k.kitPieces===19 && k.kitLevel===4 && k.kitRestored===k.kitPieces && k.grime===0 && k.wingLight===1,'Build M: with four Keys every Wing kit piece is restored, the grime gone, the light full at '+viewport.width); }
    await leaveToHub();await page.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await semantic('restart');await page.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity'&&!window.ascendantDial.snapshot().resumed,{},{timeout:120000});
    check(true,'Start over wipes the save at '+viewport.width);
    check(errors.length===0,'no browser runtime exceptions at '+viewport.width);
    fs.writeFileSync(path.join(out,viewport.width+'-events.json'),JSON.stringify(events,null,2));
    await context.close();
  }));
  // Build W (owner, Sept 26 note 5; task 86bca0163): DEV Mode's Jump to. A canvas tap through Settings to one checkpoint, the rest through their web buttons.
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const devContext=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const dev=await devContext.newPage();const devErrors=[];dev.on('pageerror',e=>devErrors.push(String(e)));
    const snap=()=>dev.evaluate(()=>window.ascendantDial.snapshot());const act=async(id)=>dev.locator('#'+id).evaluate(b=>b.click());
    const frames=async n=>{for(let i=0;i<n;i++)await dev.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>r())));};
    const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await dev.mouse.move(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await frames(2);await dev.mouse.down();await frames(2);await dev.mouse.up();await frames(2);};
    const resumed=()=>dev.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed&&!window.ascendantDial.snapshot().busy,{},{timeout:120000});
    await dev.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');
    await dev.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});await dev.locator('#loading').waitFor({state:'detached'});
    await dev.locator('#name').fill('Tester');await dev.locator('#name').dispatchEvent('change');await dev.waitForFunction(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await act('next-screen');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
    await act('birth-skip');await dev.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-4');await dev.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await act('sign-7');await dev.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await act('sign-0');await dev.waitForFunction(()=>window.ascendantDial.snapshot().sunSign==='Leo'&&window.ascendantDial.snapshot().birthStep==='done');
    await act('next-screen');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
    for(let n=0;n<8&&(await snap()).screen==='atrium';n++){await act('next-screen');await dev.waitForTimeout(150);}
    await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await tap(158,22);await dev.waitForFunction(()=>window.ascendantDial.snapshot().settingsOpen,{},{timeout:5000}).catch(()=>{});
    await tap(0,449);await dev.waitForFunction(()=>window.ascendantDial.snapshot().jumpsShown,{},{timeout:5000}).catch(()=>{});
    check((await snap()).settingsOpen&&(await snap()).jumpsShown,'Build W: Settings, Testing, Jump to... opens the checkpoint list on the canvas at '+viewport.width);
    await dev.screenshot({path:path.join(out,viewport.width+'-dev-jump.png')});
    await tap(0,311);await resumed(); // the fifth row: After Key 4 (Oct 7: the list is four rows taller with the opening's samples, its top 92 px higher)
    { const s=await snap(); check(s.keys===4&&s.keysInHand===3&&s.locksFilled===1&&s.atriumStage===3&&s.playerName==='Tester'&&s.sunSign==='Leo','Build W: a canvas tap on After Key 4 reloads into the Atrium, three Keys in hand, nothing more spent, the player kept at '+viewport.width); }
    await dev.screenshot({path:path.join(out,viewport.width+'-dev-key4.png')});
    const expect={key1:s=>s.keys===1&&s.keysInHand===0&&s.locksFilled===1&&s.atriumStage===2&&!s.wheelComplete,
      wheel:s=>s.keys===1&&s.wheelComplete&&s.atriumStage===3,
      key2:s=>s.keys===2&&s.keysInHand===1&&s.atriumStage===3,
      key3:s=>s.keys===3&&s.keysInHand===2&&s.atriumStage===3,
      whole:s=>s.keys===4&&s.locksFilled===4&&s.wingWhole&&s.atriumStage===6&&s.keysInHand===0};
    for(const id of Object.keys(expect)){await act('jump-'+id);await dev.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});await resumed();check(expect[id](await snap()),'Build W: the checkpoint '+id+' lands with its Keys and stage at '+viewport.width);
      { const w=await snap(); check(w.dialWake===Math.min(4,w.keys)&&w.wakePreview===-1,'the Dial\'s wake-up (86bcbn6w6 2a A): after the checkpoint '+id+' the Dial shows step '+w.dialWake+' for '+w.keys+' Keys earned ('+w.dialLook+') at '+viewport.width); }
      if(id==='whole'){ // Build AA: at the Wing's end the journal has the Table and all four tabs; each works on the canvas
        await act('open-journal');await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='landing'&&!window.ascendantDial.snapshot().busy,{},{timeout:8000}); // a new session: the landing (after the title page, the first time)
        { const s=await snap(); check(s.journalKeeper[1]==='4 Keys \u00b7 1 Book' && s.journalKeeper[2]==='\u2609 Leo \u00b7 \u263d Scorpio \u00b7 \u2191 Aries','batch 2: at the Wing\'s end the Keeper\'s record reads 4 Keys \u00b7 1 Book; Oct 7: the jumps keep the player\'s own birth record (the three picked), the sample only stands in for none: '+s.journalKeeper[2]+' at '+viewport.width); }
        await act('journal-contents');await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='contents',{},{timeout:5000});await act('journal-chapter-wheel');await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='wheel'&&!window.ascendantDial.snapshot().busy,{},{timeout:5000});await dev.waitForTimeout(400);
        { const s=await snap(); check(s.journalView==='wheel'&&s.canJournalTable&&s.journalLenses.join()==='Element,Modality,Polarity,Opposites'&&s.journalLens==='Element','Build AA: at the Wing\'s end the journal shows the Table switch and all four tabs at '+viewport.width); }
        await tap(7+36,119);await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='table',{},{timeout:5000});check(true,'Build AA: the Table switch on the canvas lays the seats out as the Table at '+viewport.width);
        await dev.screenshot({path:path.join(out,viewport.width+'-journal-table.png')});
        await tap(7-36,119);await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalView==='wheel',{},{timeout:5000});
        await tap(7+1.5*66,143);await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalLens==='Opposites',{},{timeout:5000}); // the fourth tab, on the canvas
        await tap(7+97.2,296);await dev.waitForFunction(()=>window.ascendantDial.snapshot().journalSelected==='Libra',{},{timeout:5000}); // Libra at 3 o'clock
        check((await snap()).journalPreview.startsWith('Libra and Aries')&&(await snap()).journalPreview.includes('shares Cardinal · Yang'),'Build AA: the Opposites tab on the canvas: Libra framed, its card reads what the pair shares, at '+viewport.width);
        await dev.screenshot({path:path.join(out,viewport.width+'-journal-opposites.png')});
        await act('close-journal');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen!=='journal',{},{timeout:5000});}
      if(id==='key3'){ // the room mini-menu (86bca07wv) once the Chamber is a room: two hops each way, and no button on an instrument
        await act('travel');await dev.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});
        check(JSON.stringify((await snap()).travelRows)===JSON.stringify(['The Grand Atrium, here','The Zodiac Wing','The Crystal Book Chamber','Sealed']),'the mini-menu: after Key 3 TRAVEL lists the three rooms and one Sealed row: '+JSON.stringify((await snap()).travelRows)+' at '+viewport.width);
        await act('travel-wing');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
        await act('travel');await dev.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});await act('travel-chamber');
        await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='chamberroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
        check((await snap()).screen==='chamberroom'&&(await snap()).room==='chamber','the mini-menu: from the Wing the Chamber row goes through the Atrium to the Chamber at '+viewport.width);
        await dev.screenshot({path:path.join(out,viewport.width+'-travel-chamber.png')});
        await act('travel');await dev.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});await act('travel-wing');
        await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
        check((await snap()).screen==='wingroom'&&(await snap()).keysInHand===2,'the mini-menu: and back to the Wing, no Key spent on the way, at '+viewport.width);
        await act('poi-dial');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
        check((await snap()).screen==='wing'&&!(await snap()).travelShown,'the mini-menu: an instrument keeps its own exit (no Travel button on the Dial) at '+viewport.width);
        await act('leave-dial');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
        await act('travel');await dev.waitForFunction(()=>window.ascendantDial.snapshot().travelOpen,{},{timeout:5000}).catch(()=>{});await act('travel-atrium');
        await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});}
      if(id==='key2'){await act('poi-wing-door');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});await act('poi-dial');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
        check((await snap()).fork==='both'&&(await snap()).canContinueLesson&&(await snap()).canEnterPractice,'Build W: from after Key 2 the Dial offers the next lesson and practice at '+viewport.width);}}
    // The Dial's wake-up (owner, Oct 1; 86bcbn6w6: 2a A, 2b B): DEV Mode previews each of the five steps; each blends the looks the ruling names,
    // a look without its file standing in with today's. On the Dial, captured at 3x for the owner's check.
    { const want=['worn','worn + today','today','today + bright','bright'];await act('poi-wing-door');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});
      for(let step=0;step<5;step++){await dev.evaluate(n=>window.ascendantDial.act('wake:'+n),step);await dev.waitForFunction(n=>window.ascendantDial.snapshot().dialWake===n,step,{timeout:5000}).catch(()=>{});
        const w=await snap();check(w.dialWake===step&&w.wakePreview===step&&w.dialLook.replace(/ \(stand-in: today's\)/g,'')===want[step],'the Dial\'s wake-up: DEV Mode\'s step '+step+' blends '+w.dialLook+' at '+viewport.width);
        await act('poi-dial');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});await dev.waitForTimeout(900);
        await dev.screenshot({path:path.join(out,viewport.width+'-dial-wake-'+step+'.png')});
        { const m=(await snap()).masters||[],need=[['-worn'],['-worn',''],[''],['','-bright'],['-bright']][step].map(l=>'dial-room'+l); check(need.every(n=>m.includes(n))&&!w.dialLook.includes('stand-in'),'batch 2: at step '+step+' the Dial draws '+need.join(', ')+' as masters, and no look stands in ('+m.filter(n=>n.startsWith('dial-room')).join(', ')+') at '+viewport.width); }
        await act('leave-dial');await dev.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}).catch(()=>{});}
      await dev.evaluate(()=>window.ascendantDial.act('wake:-1'));await dev.waitForFunction(()=>window.ascendantDial.snapshot().wakePreview===-1,{},{timeout:5000}).catch(()=>{});
      check((await snap()).dialWake===Math.min(4,(await snap()).keys),'the Dial\'s wake-up: back to as earned, the Dial shows the Keys\' step at '+viewport.width); }
    // batch 2: DEV Mode's cusp-day sample (owner ruling, Oct 2 evening): a fresh opening at the cusp question, the name kept
    await act('jump-cusp');await dev.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='birth'&&window.ascendantDial.snapshot().birthStep==='cusp',{},{timeout:120000});
    { const c=await snap(); check(c.playerName==='Tester' && c.cuspSigns.join()==='Aries,Taurus' && c.cuspTime==='9:27 am' && !c.resumed,'DEV Mode\'s cusp-day sample: the opening at the cusp question (London, Apr 20 1990), the name kept, at '+viewport.width); }
    await dev.screenshot({path:path.join(out,viewport.width+'-dev-cusp.png')});
    // Oct 7 (86bced0tc): DEV Mode's sample for each other path, a fresh opening at its first question, the name kept
    for(const [id,step] of [['no-time','rising-pick'],['no-place','moon'],['neither','cusp'],['skip','sun-pick']]){
      await act('jump-birth-'+id);await dev.waitForFunction(k=>window.ascendantDial?.snapshot()?.screen==='birth'&&window.ascendantDial.snapshot().birthStep===k,step,{timeout:120000});
      const c=await snap(); check(c.playerName==='Tester' && !c.resumed && !c.canSliceContinue && (step==='moon'?c.canMoon:step==='cusp'?(c.canCusp&&c.cuspTime===''):c.canSignPick),'DEV Mode\'s '+id+' sample: the opening at its first question ('+step+'), the name kept, at '+viewport.width); }
    await dev.evaluate(()=>window.ascendantDial.act('restart'));await dev.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity'&&!window.ascendantDial.snapshot().resumed,{},{timeout:120000}); // Settings' Start over (the page's own button waits for the Atrium)
    check(devErrors.length===0,'Build W: no runtime exceptions through the jumps at '+viewport.width);
    await devContext.close();
  }));
  const recoveryContext=await browser.newContext({viewport:{width:390,height:844},reducedMotion:'reduce'});
  const recovery=await recoveryContext.newPage();
  await recovery.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');
  await recovery.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});
  const action=async(id)=>recovery.locator('#'+id).evaluate(b=>b.click());
  await action('next-screen');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
  // no unknown Big Three (owner, Oct 7, 86bced0tc): the skip path asks the sun, the moon and the rising, with no I'm not sure
  await action('birth-skip');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);check(!(await recovery.evaluate(()=>window.ascendantDial.snapshot().canSignUnknown))&&!(await recovery.$('#sign-unknown')),'the skip path has no I\'m not sure button');
  await action('sign-4');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await action('sign-7');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await action('sign-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.bigThree==='\u2609 Leo \u00b7 \u263d Scorpio \u00b7 \u2191 Aries' && r.note==='Your sun sign is Leo, your moon sign Scorpio, and your rising sign Aries.' && r.sunKnown && r.canSliceContinue,'I\'ll skip it: three picks, the note in full, no unknown: '+r.bigThree); }
  // Oct 7: I don't know where keeps the time and works out what holds in every zone; the moon (if it could be two signs) and the rising are picked
  await action('change-birth');await action('birth-chart');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthDate);
  await recovery.locator('#birthdate').fill('1990-05-10');await recovery.locator('#birthdate').dispatchEvent('change');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthTime);
  await recovery.locator('#birthtime').fill('14:30');await recovery.locator('#birthtime').dispatchEvent('change');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthPlace&&window.ascendantDial.snapshot().canPlaceUnknown);
  await recovery.screenshot({path:path.join(out,'390-birth-place-step.png')});
  await action('place-unknown');await recovery.waitForFunction(()=>['moon','rising-pick'].includes(window.ascendantDial.snapshot().birthStep));if((await recovery.evaluate(()=>window.ascendantDial.snapshot().birthStep))==='moon'){await action('moon-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');}
  await recovery.screenshot({path:path.join(out,'390-birth-rising-pick.png')});
  await action('sign-5');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.risingSign==='Virgo' && r.risingChosen && r.sunSign==='Taurus' && !r.bigThree.includes('unknown') && !r.bigThree.includes(' or ') && JSON.parse(r.birthRecord).birth.timeFrom===870 && JSON.parse(r.birthRecord).birth.place==='','I don\'t know where: the time kept, no place invented, the rising picked: '+r.bigThree); }
  // the cusp day (owner ruling, Oct 2 evening): asked with the minute on the place's clock; Oct 7: its I'm not sure is removed, then the moon and the rising
  await action('change-birth');await action('birth-chart');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthDate);
  await recovery.locator('#birthdate').fill('1990-04-20');await recovery.locator('#birthdate').dispatchEvent('change');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthTime);
  await action('time-unknown');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthPlace);await recovery.locator('#birthplace').fill('London');
  await recovery.waitForFunction(()=>(window.ascendantDial.snapshot().placeMatches||[]).length>0);await action('place-match-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='cusp');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.canCusp && r.cuspSigns.join()==='Aries,Taurus' && r.cuspTime==='9:27 am' && r.cuspQuestion.startsWith('The Sun moved from Aries into Taurus on the day you were born, at 9:27 am.') && !r.canSliceContinue && !(await recovery.$('#cusp-unsure')),'the cusp question in the browser: Aries or Taurus, the change at 9:27 am London time, no I\'m not sure'); }
  await action('cusp-why');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().cuspWhy);await action('cusp-1');await recovery.waitForFunction(()=>['moon','rising-pick'].includes(window.ascendantDial.snapshot().birthStep));if((await recovery.evaluate(()=>window.ascendantDial.snapshot().birthStep))==='moon'){await action('moon-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');}
  await action('sign-5');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.sunBasis==='picked' && r.bigThree.startsWith('\u2609 Taurus \u00b7 ') && r.bigThree.endsWith('\u2191 Virgo') && !r.bigThree.includes(' or '),'the cusp pick, then the moon and the rising picked: '+r.bigThree); }
  // the owner, Oct 7 ("Ask, like the cusp Sun"): the moon's two signs are asked (no I'm not sure since 86bced0tc); a pick is the player's moon
  await action('change-birth');await action('birth-chart');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthDate);
  await recovery.locator('#birthdate').fill('1990-05-01');await recovery.locator('#birthdate').dispatchEvent('change');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthTime);
  await action('time-unknown');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthPlace);await recovery.locator('#birthplace').fill('London');
  await recovery.waitForFunction(()=>(window.ascendantDial.snapshot().placeMatches||[]).length>0);await action('place-match-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='moon');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.canMoon && JSON.stringify(r.moonOptions)==='["Cancer","Leo"]' && r.moonQuestion==='Your Moon was in Cancer or Leo that day. Which do you go by?' && !r.canSliceContinue && !(await recovery.$('#moon-unsure')),'Oct 7: the moon asked like the cusp sun, its two signs only: '+r.moonQuestion); }
  await recovery.screenshot({path:path.join(out,'390-birth-moon.png')});
  await action('moon-1');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await action('sign-5');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.bigThree==='\u2609 Taurus \u00b7 \u263d Leo \u00b7 \u2191 Virgo' && r.moonBasis==='picked' && r.canSliceContinue,'a pick is the player\'s moon, then the rising, shown like any other: '+r.bigThree); }
  await action('change-birth');await action('birth-chart');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthDate);
  await recovery.locator('#birthdate').fill('1990-05-01');await recovery.locator('#birthdate').dispatchEvent('change');
  // batch 2: the date, then the time, then the town picked from the bundled list; the chart is worked out once (Meeus; the mechanical checks hold it to JPL Horizons)
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthTime);await recovery.locator('#birthtime').fill('14:30');await recovery.locator('#birthtime').dispatchEvent('change');
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().canBirthPlace);await recovery.locator('#birthplace').fill('Londo');
  await recovery.waitForFunction(()=>(window.ascendantDial.snapshot().placeMatches||[]).length>0);check((await recovery.evaluate(()=>window.ascendantDial.snapshot().placeMatches))[0]==='London, Britain (UK)','typing a town lists the matches from the bundled list, the biggest first (London)');
  await action('place-match-0');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='done');
  { const r=await recovery.evaluate(()=>window.ascendantDial.snapshot()); check(r.sunSign==='Taurus' && r.moonSign==='Leo' && r.risingSign==='Virgo' && r.bigThree==='\u2609 Taurus \u00b7 \u263d Leo \u00b7 \u2191 Virgo' && r.canSliceContinue,'a birth date, time and place work out the chart in the browser: '+r.bigThree+' (London, May 1 1990, 14:30)'); }
  await action('next-screen');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
  for(let n=0;n<8&&(await recovery.evaluate(()=>window.ascendantDial.snapshot().screen))==='atrium';n++){await action('next-screen');await recovery.waitForTimeout(150);}
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // Build T: the walk to the Dial
  await action('poi-wing-door');await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
  await action('poi-dial');
  await recovery.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canContinue,{},{timeout:15000});
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
  // ---- the birth-time build (owner, Oct 7): a player with no sun, the Aries start, "Your Birth" (a rising chosen, then the facts added),
  // the record saved and read back after a reload, and an old version 4 save converted once ----
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const birthContext=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const bp=await birthContext.newPage();const birthErrors=[];bp.on('pageerror',e=>birthErrors.push(e.message));bp.on('console',m=>{if(m.type()==='error'&&/Exception/.test(m.text()))birthErrors.push(m.text());});
    const snap=()=>bp.evaluate(()=>window.ascendantDial.snapshot());const act=async id=>bp.locator('#'+id).evaluate(b=>b.click());const send=c=>bp.evaluate(k=>window.ascendantDial.act(k),c);
    const frames=n=>bp.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);
    const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await bp.mouse.move(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await frames(2);await bp.mouse.down();await frames(2);await bp.mouse.up();await frames(2);};
    const until=(f,t=15000)=>bp.waitForFunction(f,{},{timeout:t});
    const resumed=()=>until(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed&&!window.ascendantDial.snapshot().busy,120000);
    const landing=async()=>{await act('open-journal');await until(()=>window.ascendantDial.snapshot().journalView==='landing'&&!window.ascendantDial.snapshot().busy,8000);};
    await bp.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');await until(()=>window.ascendantDial?.snapshot()?.screen==='identity',120000);await bp.locator('#loading').waitFor({state:'detached'});
    await bp.locator('#name').fill('Tester');await bp.locator('#name').dispatchEvent('change');await until(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await act('next-screen');await until(()=>window.ascendantDial.snapshot().screen==='birth');
    // Oct 7 (86bced0tc): the skip path's three picks (Taurus, Cancer, Leo), then the After Key 1 checkpoint under this player's own record (the jump keeps it)
    await act('birth-skip');await until(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-1');await until(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await act('sign-3');await until(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await act('sign-4');await until(()=>window.ascendantDial.snapshot().birthStep==='done');
    await act('jump-key1');await resumed();
    { const s=await snap(); check(s.bigThree==='☉ Taurus · ☽ Cancer · ↑ Leo' && s.sunKnown && s.lessonSun==='Taurus','Oct 7 (86bced0tc): no unknown in the record; the jump keeps the three picks and the lessons start from the picked sun, at '+viewport.width); }
    await act('poi-wing-door');await until(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy);await act('poi-dial');await until(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy);await bp.waitForTimeout(600);
    { const s=await snap(); check(s.start==='Your sign: Taurus','the Dial\'s idle face names the picked sun as the player\'s sign: '+s.start+' at '+viewport.width); }
    await act('leave-dial');await until(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy);await act('poi-atrium-door');await until(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);
    await landing();
    { const s=await snap(); const g=s.journalBigThreeBox||[]; check(g.length===4 && g[3]>=44 && s.journalKeeper[2]==='☉ Taurus · ☽ Cancer · ↑ Leo','the record shows the three picked, with no unknown, its line a 44 px target, at '+viewport.width);
      await tap(g[0],g[1]); }
    await until(()=>window.ascendantDial.snapshot().journalView==='birth'&&!window.ascendantDial.snapshot().busy,5000);
    { const s=await snap(); check(JSON.stringify(s.journalBirthRows)==='["Date: unknown","Time: unknown","Place: unknown"]' && s.journalBirthLines.join(' ').startsWith('Your rising is the sign that was coming up over the eastern horizon') && s.journalBirthLines.join(' ').endsWith("Until then, I'll keep the one you chose.") && s.birthRisingWords==='Change my rising' && s.canBirthAdd && s.canBirthAddTime && s.journalLink==='‹ Your Journal','a canvas tap on the line opens "Your Birth": each fact unknown with Add, the journal\'s paragraph on the chosen rising, Change my rising, Add my birth time, at '+viewport.width);
      check(s.buttons.filter(b=>b.includes(':frame:')).every(b=>{const wh=b.slice(b.lastIndexOf(':')+1).split('x');return +wh[1]>=44;}),'"Your Birth"\'s buttons each take a 44 px target at '+viewport.width); }
    await bp.screenshot({path:path.join(out,viewport.width+'-your-birth.png')});
    { const b=(await snap()).journalBirthBoxes; await tap(b[12],b[13]); } // Change my rising
    await until(()=>window.ascendantDial.snapshot().screen==='birth'&&window.ascendantDial.snapshot().canSignPick);
    { const s=await snap(); check(s.amending && s.birthHeading==='Your Birth' && !s.canSignUnknown && s.canBirthCancel,'Change my rising: the opening\'s twelve signs under "Your Birth", with the way back, at '+viewport.width); }
    await bp.screenshot({path:path.join(out,viewport.width+'-choose-rising.png')});
    await tap(-110,452); // Libra, on the canvas (the grid's left column, third row)
    await until(()=>window.ascendantDial.snapshot().journalView==='birth'&&!window.ascendantDial.snapshot().busy,5000);
    { const s=await snap(); check(JSON.stringify(s.journalBirthLines)==='["Libra it is."]' && s.risingChosen && s.bigThree==='☉ Taurus · ☽ Cancer · ↑ Libra' && s.birthRisingWords==='Change my rising','back on the page: "Libra it is."; the record shows ↑ Libra with no label (the flag hidden), at '+viewport.width); }
    { const b=(await snap()).journalBirthBoxes; await tap(b[0],b[1]); } // Add, on the date's row
    await until(()=>window.ascendantDial.snapshot().canBirthDate);
    await bp.locator('#birthdate').fill('1990-04-25');await bp.locator('#birthdate').dispatchEvent('change');await until(()=>window.ascendantDial.snapshot().canBirthTime);
    await bp.locator('#birthtime').fill('14:30');await bp.locator('#birthtime').dispatchEvent('change');await until(()=>window.ascendantDial.snapshot().canBirthPlace);
    await bp.locator('#birthplace').fill('London');await until(()=>(window.ascendantDial.snapshot().placeMatches||[]).length>0);await act('place-match-0');
    await until(()=>window.ascendantDial.snapshot().journalView==='birth'&&!window.ascendantDial.snapshot().busy,5000);
    { const s=await snap(); check(JSON.stringify(s.journalBirthLines)==='["Your rising is Virgo.","It takes the place of the one you chose.","Your moon is Taurus.","Everything you\'ve learned stays as it is."]' && JSON.stringify(s.journalBirthRows)==='["Date: 25 April 1990","Time: 2:30 pm","Place: London, Britain (UK)"]' && !s.canBirthAdd && !s.canBirthRising && s.lessonSun==='Taurus' && s.keys===1,
      'the facts added in the opening\'s boxes decide: a line for each change, the Key kept, the lessons now from Taurus: '+s.journalBirthLines.join(' / ')+' at '+viewport.width); }
    await bp.screenshot({path:path.join(out,viewport.width+'-your-birth-added.png')});
    await bp.reload();await resumed();await landing();
    { const s=await snap(); check(s.journalKeeper[2]==='☉ Taurus · ☽ Taurus · ↑ Virgo' && !s.risingChosen,'after a reload the record reads the saved chart: '+s.journalKeeper[2]+' at '+viewport.width); }
    // an old version 4 save from each path, converted once on load
    for(const [id,expect] of [['known','☉ Leo · ☽ Scorpio · ↑ unknown'],['chosen','☉ Capricorn · ☽ unknown · ↑ unknown'],['legacy','☉ Gemini · ☽ unknown · ↑ unknown']]){
      await send('jump-birth:v4-'+id);await bp.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});await resumed();await landing();
      const s=await snap(),r=JSON.parse(s.birthRecord||'{}'); check(s.journalKeeper[2]===expect && (r.choices||[]).some(c=>c.point==='sun'&&c.how===(id==='known'?'entered':id==='chosen'?'assigned':'legacy')),'an old '+id+' save converts once, its signs kept as the player\'s choices: '+s.journalKeeper[2]+' at '+viewport.width); }
    await send('jump-birth:v4-chart');await bp.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});await resumed();await landing();
    { const s=await snap(),r=JSON.parse(s.birthRecord||'{}'); check(s.journalKeeper[2].startsWith('☉ Taurus · ☽ ') && s.journalKeeper[2].includes(' or ') && s.journalKeeper[2].endsWith('↑ unknown') && r.birth.date==='1990-04-20' && r.choices.some(c=>c.point==='sun'&&c.how==='picked'),'an old chart-path save moves its facts over and works the chart out again, the cusp\'s pick kept: '+s.journalKeeper[2]+' at '+viewport.width); }
    check(birthErrors.length===0,'Oct 7: no runtime exceptions through "Your Birth" and the conversions at '+viewport.width+(birthErrors.length?': '+birthErrors[0]:''));
    await birthContext.close();
  }));
  // ---- the living inscription (owner, Oct 7: approved as scoped, all drafts; 86bceba0a): a new line on each visit after the first, written
  // in ink; a tap on the page finishes it; it holds for the visit; Reduced motion shows it whole ----
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const inkContext=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const ip=await inkContext.newPage();const inkErrors=[];ip.on('pageerror',e=>inkErrors.push(e.message));ip.on('console',m=>{if(m.type()==='error'&&/Exception/.test(m.text()))inkErrors.push(m.text());});
    const snap=()=>ip.evaluate(()=>window.ascendantDial.snapshot());const act=async id=>ip.locator('#'+id).evaluate(b=>b.click());const send=c=>ip.evaluate(k=>window.ascendantDial.act(k),c);
    const frames=n=>ip.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);
    const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await ip.mouse.move(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await frames(2);await ip.mouse.down();await frames(2);await ip.mouse.up();await frames(2);};
    const until=(f,t=15000)=>ip.waitForFunction(f,{},{timeout:t});
    const resumed=()=>until(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed&&!window.ascendantDial.snapshot().busy,120000);
    const visit=async()=>{await send('reload');await ip.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});await resumed();};
    const landing=async()=>{await act('open-journal');await until(()=>window.ascendantDial.snapshot().journalView==='landing'&&!window.ascendantDial.snapshot().busy,8000);};
    const inscription=s=>(s.journalKeeper||[]).slice(3);
    await ip.goto(process.env.GREYBOX_URL || 'http://127.0.0.1:8000');await until(()=>window.ascendantDial?.snapshot()?.screen==='identity',120000);await ip.locator('#loading').waitFor({state:'detached'});
    await ip.locator('#name').fill('Tester');await ip.locator('#name').dispatchEvent('change');await until(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await act('next-screen');await until(()=>window.ascendantDial.snapshot().screen==='birth');
    await act('birth-skip');await until(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-1');await until(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await act('sign-3');await until(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await act('sign-4');await until(()=>window.ascendantDial.snapshot().birthStep==='done');
    await act('jump-key1');await resumed();
    await act('open-journal');await until(()=>window.ascendantDial.snapshot().journalView==='landing'&&!window.ascendantDial.snapshot().inscriptionWriting,10000);
    { const s=await snap(); check(s.inscriptionId==='first' && inscription(s).join(' ')==="What's up, Tester. I'm your journal, and I'll keep a record of what you learn from the Library.",'a fresh save\'s first-ever open writes the Oct 2 line, word for word, at '+viewport.width); }
    // the next visit: a line from the groups unlocked so far, written letter by letter; a tap on the page finishes it
    await visit();await landing();
    const second=await snap(), secondLine=inscription(second).join(' ');
    check(second.inscriptionId!=='first' && ['any','key','sun'].includes(second.inscriptionGroup) && second.inscriptionWriting && inscription(second).length>=1 && inscription(second).length<=3 && !/[0-9]/.test(secondLine) && !secondLine.includes('{') && second.inscriptionRecent.join()===second.inscriptionId && second.caspar.includes(secondLine),
      'the next visit lands on a new line ('+second.inscriptionId+': '+secondLine+'), three lines at most, no number; the screen reader hears it whole while it writes, at '+viewport.width);
    await ip.screenshot({path:path.join(out,viewport.width+'-inscription-writing.png')});
    await tap(-150,600); // the page's margin, clear of the doors
    { const s=await snap(); check(!s.inscriptionWriting && s.journalView==='landing' && inscription(s).join(' ')===secondLine,'a tap on the page finishes the line at once, at '+viewport.width); }
    await ip.screenshot({path:path.join(out,viewport.width+'-inscription-written.png')});
    await act('journal-contents');await until(()=>window.ascendantDial.snapshot().journalView==='contents');await act('journal-landing');await until(()=>window.ascendantDial.snapshot().journalView==='landing');
    { const s=await snap(); check(s.inscriptionId===second.inscriptionId && !s.inscriptionWriting && s.inscriptionRecent.length===1,'back from Contents the same line shows, already written; the line holds for the visit, at '+viewport.width); }
    await tap(0,714);await until(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,8000).catch(()=>{}); // Jeffrey, #128 B1: Close the journal, tapped on the canvas from the landing
    { const s=await snap(); check(s.screen==='hub','a canvas tap on Close the journal closes it from the landing, at '+viewport.width); }
    await landing();
    { const s=await snap(); check(s.inscriptionId===second.inscriptionId && !s.inscriptionWriting,'opened again in the same visit, the same line, written, at '+viewport.width); }
    // another visit, with Reduced motion: a different line, shown whole at once
    await visit();await send('motion-on');await landing();
    { const s=await snap(); check(s.inscriptionId!==second.inscriptionId && s.inscriptionId!=='first' && !s.inscriptionWriting && inscription(s).length>=1 && s.inscriptionRecent.join()===second.inscriptionId+','+s.inscriptionId,'the visit after brings a different line ('+s.inscriptionId+'); with Reduced motion it shows whole at once, at '+viewport.width); }
    // Jeffrey, #128 B2: a reload in the middle of the first-ever title page brings the title page and the Oct 2 line back
    await send('close-journal');await act('jump-key1');await ip.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});await resumed();await act('open-journal');await until(()=>window.ascendantDial.snapshot().journalView==='title',8000);
    await visit();await act('open-journal');await until(()=>window.ascendantDial.snapshot().journalView==='landing'&&!window.ascendantDial.snapshot().inscriptionWriting,12000);
    { const s=await snap(); check(s.inscriptionId==='first' && inscription(s).join(' ')==="What's up, Tester. I'm your journal, and I'll keep a record of what you learn from the Library.",'a game closed during the first-ever title page shows the title page and the Oct 2 line next time, at '+viewport.width); }
    check(inkErrors.length===0,'Oct 7: no runtime exceptions through the inscription\'s visits at '+viewport.width+(inkErrors.length?': '+inkErrors[0]:''));
    await inkContext.close();
  }));
  // ---- Build E: the test set from the URL at both viewports (the opening and the Dial), the cues, the style page on both sets ----
  const base=process.env.GREYBOX_URL || 'http://127.0.0.1:8000';const withQuery=q=>base+(base.includes('?')?'&':'?')+q;
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const artContext=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const art=await artContext.newPage();
    await art.goto(withQuery('art=test'));
    await art.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='identity',{},{timeout:120000});await art.locator('#loading').waitFor({state:'detached'});
    const snap=()=>art.evaluate(()=>window.ascendantDial.snapshot());const act=async(id)=>art.locator('#'+id).evaluate(b=>b.click());
    let s=await snap();check(s.artSet==='test'&&s.artFiles===ART_SLOTS&&s.soundFiles===7&&!s.style,'?art=test plays the game with a file in every slot at '+viewport.width);
    check(s.lightFiles===3&&s.lightAlpha===0,'the three light overlays resolve from the test set and stay dark in the opening (Stage 1) at '+viewport.width); // Build H
    await art.locator('#name').fill('Tester');await art.locator('#name').dispatchEvent('change');await art.waitForFunction(()=>window.ascendantDial.snapshot().playerName==='Tester');
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='birth');
    await act('birth-skip');await art.waitForFunction(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-1');await art.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await act('sign-3');await art.waitForFunction(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await act('sign-4');await art.waitForFunction(()=>window.ascendantDial.snapshot().sunSign==='Taurus'&&window.ascendantDial.snapshot().birthStep==='done');
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue,{},{timeout:15000});
    await art.screenshot({path:path.join(out,viewport.width+'-art-atrium.png')});
    check(await art.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll with the test set at '+viewport.width);
    await act('next-screen');await art.waitForFunction(()=>window.ascendantDial.snapshot().lastCue==='page'&&window.ascendantDial.snapshot().cuesPlayed>=1);check(true,'a page turn plays the page cue from the test set at '+viewport.width);
    for(let n=0;n<8&&(await snap()).screen==='atrium';n++){await act('next-screen');await art.waitForTimeout(150);}
    await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000}); // Build T: the walk to the Dial
    await act('poi-wing-door');await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});
    await act('poi-dial');
    await art.waitForFunction(()=>window.ascendantDial.snapshot().screen==='wing'&&window.ascendantDial.snapshot().canContinue,{},{timeout:120000});
    await art.screenshot({path:path.join(out,viewport.width+'-art-dial.png')});
    for(let n=0;n<16&&(await snap()).start!=='Start: Taurus';n++){if((await snap()).canContinue)await act('continue');await art.waitForTimeout(500);}
    const okStart=s=>window.ascendantDial.snapshot()?.active && window.ascendantDial.snapshot().start===('Start: '+s);await art.waitForFunction(okStart,'Taurus',{timeout:15000});await art.waitForTimeout(400);await art.waitForFunction(okStart,'Taurus',{timeout:20000});await art.waitForTimeout(150);
    const before=(await snap()).cuesPlayed;await act('forward');await art.waitForFunction(b=>window.ascendantDial.snapshot().lastCue==='step'&&window.ascendantDial.snapshot().cuesPlayed>b,before);check(true,'a wheel step plays the step cue from the test set at '+viewport.width);
    await art.screenshot({path:path.join(out,viewport.width+'-art-dial-guided.png')});
    await artContext.close();
  }));
  for(const [query,set] of [['style=test','test'],['style','']]){
    const styleContext=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const stylePage=await styleContext.newPage();await stylePage.goto(withQuery(query));
    await stylePage.waitForFunction(()=>window.ascendantDial?.snapshot()?.screen==='style',{},{timeout:120000});await stylePage.locator('#loading').waitFor({state:'detached'});
    const s=await stylePage.evaluate(()=>window.ascendantDial.snapshot());const where=set?'the test set':'the Art folder';
    check(s.style&&s.artSet===set&&s.styleSlots.length===ART_SLOTS&&s.styleSounds.length===SOUND_SLOTS&&!s.canSliceContinue&&!s.canName,'?'+query+' shows the style page on '+where+' with '+ART_SLOTS+' art and '+SOUND_SLOTS+' sound slots and no game controls');
    check(set?s.styleSlots.every(t=>t.endsWith(': test set'))&&s.styleSounds.every(t=>t.endsWith(': test set')):s.styleSlots.every(t=>/: (file|placeholder)$/.test(t))&&s.styleSounds.every(t=>/: (file|silent)$/.test(t)),'every slot lists its source on '+where);
    const items=await stylePage.locator('#style-list li').allTextContents();check(items.length===ART_SLOTS&&items[0].startsWith('atrium:')&&(await stylePage.locator('#style-sounds button').count())===SOUND_SLOTS,'the semantic layer lists every art slot with its source and a button per sound slot');
    check(await stylePage.evaluate(()=>document.documentElement.scrollHeight<=innerHeight),'no vertical scroll on the style page on '+where);
    await stylePage.screenshot({path:path.join(out,'390-style'+(set?'-'+set:'')+'.png')});
    if(set){await stylePage.locator('#sound-1').evaluate(b=>b.click());await stylePage.waitForFunction(()=>window.ascendantDial.snapshot().lastCue==='seal'&&window.ascendantDial.snapshot().cuesPlayed>=1);check(true,'a sound slot plays from the style page');
      await stylePage.locator('#mute').evaluate(b=>b.click());await stylePage.waitForFunction(()=>window.ascendantDial.snapshot().muted);check((await stylePage.locator('#mute').getAttribute('aria-pressed'))==='true','the test mute toggle works from the style page');
      await stylePage.locator('#mute').evaluate(b=>b.click());await stylePage.waitForFunction(()=>!window.ascendantDial.snapshot().muted);}
    await styleContext.close();
  }
  // ---- Platform fit, Part 2 (owner, Oct 1; 86bcbn6mf; doc 2kyd583p-7114): the seven test shapes, captured at 3x. Phones from 9:16 to 9:23 fill
  // edge to edge and tablets keep the column with room art down the sides: at every shape the art reaches every edge (no flat bar in any
  // margin), the gear and the mini-menu button take their taps where the web state says they sit, and the column's own targets still land.
  if(!process.env.SKIP_SHAPES){
    const SHAPES=[['9-16',360,640],['9-19.5',360,780],['9-20',360,800],['9-21',360,840],['9-23',360,920],['10-16',500,800],['3-4',600,800]];
    const shapeRun=async([name,w,h])=>{
      const ctx=await browser.newContext({viewport:{width:w,height:h},deviceScaleFactor:3});const pg=await ctx.newPage();const errs=[];pg.on('pageerror',e=>errs.push(String(e)));
      const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot());const act=async id=>pg.locator('#'+id).evaluate(b=>b.click());
      const until=(fn,t=20000)=>pg.waitForFunction(fn,{},{timeout:t});const scale=Math.min(w/360,h/800),left=(w-360*scale)/2,top=(h-800*scale)/2;
      const frames=n=>pg.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);
      const tap=async(x,y)=>{await pg.mouse.move(w/2+x*scale,top+y*scale);await frames(2);await pg.mouse.down();await frames(2);await pg.mouse.up();await frames(2);};
      // The margins past the column, each read from a capture: a flat bar has almost no spread of brightness; art has plenty.
      const margins=async label=>{const shot=await pg.screenshot({path:path.join(out,'shape-'+name+'-'+label+'.png')});
        return pg.evaluate(async([b64,box])=>{const img=new Image();img.src='data:image/png;base64,'+b64;await img.decode();const c=document.createElement('canvas');c.width=img.width;c.height=img.height;const g=c.getContext('2d');g.drawImage(img,0,0);
          const k=img.width/box.w,res={};const strips={left:[0,0,box.left,box.h],right:[box.w-box.left,0,box.left,box.h],top:[0,0,box.w,box.top],bottom:[0,box.h-box.top,box.w,box.top]};
          for(const [side,[x,y,sw,sh]] of Object.entries(strips)){if(sw<8||sh<8)continue;const d=g.getImageData(Math.round(x*k),Math.round(y*k),Math.max(1,Math.round(sw*k)),Math.max(1,Math.round(sh*k))).data;let n=0,m=0,q=0;let bg=0;for(let i=0;i<d.length;i+=16){const l=.2126*d[i]+.7152*d[i+1]+.0722*d[i+2];n++;m+=l;q+=l*l;if(Math.abs(d[i]-19)<=3&&Math.abs(d[i+1]-19)<=3&&Math.abs(d[i+2]-23)<=3)bg++;}m/=n;res[side]={sd:Math.sqrt(Math.max(0,q/n-m*m)),bg:bg/n};}
          return res;},[shot.toString('base64'),{w,h,left,top}]);};
      const filled=(res,label)=>{const bar=Object.entries(res).filter(([,v])=>v.bg>.5||(v.sd<2.5&&v.bg>.1));check(bar.length===0,'Part 2: at '+name+' ('+w+' x '+h+') the '+label+'\'s art reaches every edge, with no bar of the canvas\'s own colour in any margin ('+(Object.keys(res).length?Object.entries(res).map(([k,v])=>k+' spread '+v.sd.toFixed(1)+', bar '+Math.round(v.bg*100)+'%').join('; '):'none: the column fills the screen')+')');}; // a bar is the canvas's own charcoal (19, 19, 23); an art edge that is dark and even (the journal's desk) carries on and passes
      await pg.goto(process.env.GREYBOX_URL||'http://127.0.0.1:8000');await until(()=>window.ascendantDial?.snapshot()?.screen==='identity',120000);await pg.locator('#loading').waitFor({state:'detached'});
      await pg.locator('#name').fill('Tester');await pg.locator('#name').dispatchEvent('change');await until(()=>window.ascendantDial.snapshot().playerName==='Tester');
      await act('next-screen');await until(()=>window.ascendantDial.snapshot().screen==='birth');await act('birth-skip');await until(()=>window.ascendantDial.snapshot().canSignPick);await act('sign-1');await until(()=>window.ascendantDial.snapshot().birthStep==='moon-pick');await act('sign-3');await until(()=>window.ascendantDial.snapshot().birthStep==='rising-pick');await act('sign-4');await until(()=>window.ascendantDial.snapshot().birthStep==='done');
      await act('next-screen');await until(()=>window.ascendantDial.snapshot().screen==='atrium'&&window.ascendantDial.snapshot().canSliceContinue);
      for(let n=0;n<8&&(await snap()).screen==='atrium';n++){await act('next-screen');await pg.waitForTimeout(150);}
      await until(()=>window.ascendantDial.snapshot().screen==='hub'&&!window.ascendantDial.snapshot().busy);
      await act('jump-key1');await pg.waitForFunction(()=>!window.ascendantDial?.snapshot()?.resumed,{},{timeout:30000}).catch(()=>{});
      await until(()=>window.ascendantDial?.snapshot()?.screen==='hub'&&window.ascendantDial.snapshot().resumed&&!window.ascendantDial.snapshot().busy,120000);await pg.waitForTimeout(900);
      filled(await margins('atrium'),'Atrium');
      const s=await snap(),g=s.gearAt,t=s.travelAt,phone=w/h<=9/16+.01;
      check(['atrium','atrium-light'].every(n=>(s.masters||[]).includes(n)),'Batch 2: at '+name+' the Atrium and its light draw their masters whole ('+(s.masters||[]).join(', ')+')');
      check(g&&t&&(phone?Math.abs((g[0]+22)*scale-(w/2))<1.5&&Math.abs((t[0]-22)*scale+(w/2))<1.5:Math.abs(g[0]-158)<.01&&Math.abs(t[0]+158)<.01),'Part 2: at '+name+' the gear and the mini-menu button sit '+(phone?'at the screen\'s top corners (a phone)':'at the column\'s corners (a tablet keeps the column)')+': gear '+JSON.stringify(g)+', button '+JSON.stringify(t));
      const gearBox=await pg.locator('#settings').evaluate(b=>[parseFloat(b.style.left)+parseFloat(b.style.width)/2,parseFloat(b.style.top)+parseFloat(b.style.height)/2]);
      check(Math.abs(gearBox[0]-(w/2+g[0]*scale))<1.5&&Math.abs(gearBox[1]-(top+g[1]*scale))<1.5,'Part 2: at '+name+' the semantic gear sits on the gear');
      await tap(g[0],g[1]);await until(()=>window.ascendantDial.snapshot().settingsOpen,5000).catch(()=>{});check((await snap()).settingsOpen,'Part 2: at '+name+' a tap on the gear opens Settings');
      await tap(0,553);await until(()=>!window.ascendantDial.snapshot().settingsOpen,5000).catch(()=>{});
      await tap(t[0],t[1]);await until(()=>window.ascendantDial.snapshot().travelOpen,5000).catch(()=>{});check((await snap()).travelOpen,'Part 2: at '+name+' a tap on the mini-menu button opens TRAVEL');
      await pg.screenshot({path:path.join(out,'shape-'+name+'-travel.png')});await act('travel');await until(()=>!window.ascendantDial.snapshot().travelOpen,5000).catch(()=>{});
      await tap(0,310);await until(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,15000).catch(()=>{});
      check((await snap()).screen==='wingroom','Part 2: at '+name+' the Zodiac Wing door takes its canvas tap in the column');await pg.waitForTimeout(700);
      filled(await margins('wing'),'Zodiac Wing');
      { const m=(await snap()).masters||[]; check(['wing','wing-light'].every(n=>m.includes(n)),'Batch 2: at '+name+' the Zodiac Wing and its light draw their masters whole ('+m.join(', ')+')'); }
      await act('poi-dial');await until(()=>window.ascendantDial.snapshot().screen==='wing'&&!window.ascendantDial.snapshot().busy,15000).catch(()=>{});await pg.waitForTimeout(1500);
      filled(await margins('dial'),'Dial');
      { const m=(await snap()).masters||[]; check(['dial-room-worn','dial-room'].every(n=>m.includes(n)),'Batch 2: at '+name+' the Dial\'s room draws its masters whole, worn and today\'s blended at Key 1 ('+m.filter(n=>n.startsWith('dial-room')).join(', ')+')'); }
      await act('leave-dial');await until(()=>window.ascendantDial.snapshot().screen==='wingroom'&&!window.ascendantDial.snapshot().busy,15000).catch(()=>{});
      await act('open-journal');await until(()=>window.ascendantDial.snapshot().screen==='journal'&&!window.ascendantDial.snapshot().busy,15000).catch(()=>{});await pg.waitForTimeout(800);
      filled(await margins('journal'),'journal');
      check(errs.length===0,'Part 2: no runtime errors at '+name+(errs.length?': '+errs.join(' | '):''));await ctx.close();};
    for(let i=0;i<SHAPES.length;i+=4)await Promise.all(SHAPES.slice(i,i+4).map(shapeRun)); // four at a time
  }
  fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
