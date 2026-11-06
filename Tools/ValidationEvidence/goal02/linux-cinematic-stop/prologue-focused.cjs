const fs=require('fs'),path=require('path'),playwright=require(process.env.PLAYWRIGHT_MODULE);const out=process.env.EVIDENCE_DIR;fs.mkdirSync(out,{recursive:true});const report=[];function check(v,t){if(!v)throw Error(t);report.push('PASS: '+t);}
  const PROLOGUE=[[0,'prologue-city',2000],[1,'prologue-desk',1500],[1,'prologue-notebook',300],[2,'prologue-desk',500],[2,'prologue-light',1600],[2,'prologue-light-up',400],[2,'prologue-look',600],[2,'prologue-headphones',300],
    [3,'prologue-street-above',1000],[3,'prologue-street-above',3500],[4,'prologue-puzzled',1500],[5,'prologue-caspar-back',1000],[5,'prologue-caspar-turn',1000],[5,'prologue-caspar-face',1500],[5,'prologue-caspar-eyes',2000]]; // [shot, frame, ms to settle]
  // The launch (owner, Oct 9, 86bcg62x3): every page load opens on the logo screen, then the launch movie, then the main menu; one Skip covers
  // both. newGame leaves the launch by the screen reader's Skip, then New Game (on a first launch straight into slot 1; with a save, slot 1,
  // replacing it); Start over and the DEV samples arrive straight on the prologue. relaunch reloads the page and Continues into the Hub.
  const playPrologue=async(pg,prefix,reduced,where,tap)=>{
    const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot());
    for(const [i,[shot,frame,settle]] of PROLOGUE.entries()){
      const want=frame;
      await pg.waitForFunction(([k,f])=>{const s=window.ascendantDial.snapshot();return s.prologueShot===k&&s.prologueFrame===f;},[shot,want],{timeout:25000});
      await pg.waitForTimeout(tap&&i===1?0:settle);
      const s=await snap(),said=await pg.locator('#announcement').textContent(),skip=await pg.locator('#skip-prologue').isEnabled();
      const loopFrame=frame==='prologue-notebook'&&/^prologue-notebook(-[23])?$/.test(s.prologueFrame); // the writing loop may already have begun (it starts 0.7 s after the panel)
      check(s.screen==='prologue'&&s.prologueShot===shot&&(s.prologueFrame===want||(!reduced&&loopFrame))&&s.caspar!==''&&said.includes(s.caspar)&&s.canSkip&&skip&&!s.canSliceContinue&&!s.orbShown&&(!reduced||shot<1||s.prologueStill),'the opening scene'+(reduced?', reduced motion':'')+': shot '+(shot+1)+' ('+s.prologueShotId+'), '+want+', its spoken line announced ("'+s.caspar+'"), the semantic Skip offered'+(reduced&&shot===0?(s.prologueStill?', still from the first shot':', the first shot as it began'):'')+' at '+where);
      // Tap near the beginning of the stable wide shot, before screenshot latency can cross its panel transition.
      if(!(tap&&i===1))await pg.screenshot({path:path.join(out,prefix+'-'+String(i+1).padStart(2,'0')+'-'+s.prologueShotId+'-'+want.replace('prologue-','')+'.png')});
      // the owner, Oct 9: slight animations as frame swaps; read from the frames the shot has shown, in order (the headphones' in-between lasts 0.14 s)
      if(shot===1&&frame==='prologue-notebook'){
        if(!reduced){
          await pg.waitForFunction(()=>window.ascendantDial.snapshot().prologueFrame==='prologue-notebook-2',{},{timeout:10000});
          await pg.screenshot({path:path.join(out,prefix+'-'+String(i+1).padStart(2,'0')+'b-desk-notebook-2.png')});
          await pg.waitForFunction(()=>{const s=window.ascendantDial.snapshot(),f=s.prologueShotFrames||[];return s.prologueShot===1&&f.includes('prologue-notebook-2')&&f.includes('prologue-notebook-3')&&f.lastIndexOf('prologue-notebook')>f.indexOf('prologue-notebook-3');},{},{timeout:10000});
        }else await pg.waitForTimeout(1500);
        const seen=(await snap()).prologueShotFrames||[];const names=seen.map(f=>f.replace('prologue-','')).join(' ');
        check(reduced?JSON.stringify(seen)==='["prologue-desk","prologue-notebook"]':seen.includes('prologue-notebook-2')&&seen.includes('prologue-notebook-3')&&seen.lastIndexOf('prologue-notebook')>seen.indexOf('prologue-notebook-3'),(reduced?'reduced motion: no writing loop, the notebook held':'the hand writes: the loop runs notebook, -2, -3 and round')+' ('+names+') at '+where);
      }
      if(shot===2&&frame==='prologue-headphones'){
        const seen=(await snap()).prologueShotFrames||[];const want3=['prologue-desk','prologue-light','prologue-light-up','prologue-look'].concat(reduced?[]:['prologue-headphones-mid']).concat(['prologue-headphones']);
        check(JSON.stringify(seen)===JSON.stringify(want3),(reduced?'reduced motion: the head turns, the close-up, the headphones come down in one crossfade with no in-between':'shot 3 in order: desk, light, the head turned, the close-up, the headphones mid-fall, down')+' ('+seen.map(f=>f.replace('prologue-','')).join(' ')+') at '+where);}
      if(tap&&i===1){ // ruling 5: a tap anywhere shows Skip and never advances a shot; Skip hides again after about 3 s
        await tap(0,400);await pg.waitForFunction(()=>window.ascendantDial.snapshot().skipShown,{},{timeout:3000}).catch(()=>{});const t0=Date.now(),a=await snap();
        check(a.skipShown&&a.prologueShot===s.prologueShot&&a.prologueFrame===s.prologueFrame&&a.screen==='prologue','a canvas tap shows Skip and advances nothing (shot '+(a.prologueShot+1)+', '+a.prologueFrame+') at '+where);
        await pg.screenshot({path:path.join(out,prefix+'-skip-shown.png')});
        fs.copyFileSync(path.join(out,prefix+'-skip-shown.png'),path.join(out,prefix+'-'+String(i+1).padStart(2,'0')+'-'+s.prologueShotId+'-'+want.replace('prologue-','')+'.png'));
        await pg.waitForFunction(()=>!window.ascendantDial.snapshot().skipShown,{},{timeout:6000});const held=Date.now()-t0;
        check(held>2400&&held<4200&&(await snap()).screen==='prologue','Skip hides again after about 3 s ('+(held/1000).toFixed(1)+' s) and the scene plays on at '+where);}
    }
    await pg.waitForFunction(()=>window.ascendantDial.snapshot().prologueShot===6,{},{timeout:25000});await pg.waitForTimeout(reduced?100:400); // the white blooming out of the glow
    { const f=await snap(); check(f.prologueShotId==='flash'&&f.busy&&!f.canSkip&&!(await pg.locator('#skip-prologue').isEnabled()),'the flash: the busy guard holds and Skip waits it out at '+where);await pg.screenshot({path:path.join(out,prefix+'-'+String(PROLOGUE.length+1)+'-flash.png')}); }
    await pg.waitForFunction(()=>window.ascendantDial.snapshot().screen==='identity'&&!window.ascendantDial.snapshot().busy,{},{timeout:15000});await pg.waitForTimeout(reduced?200:600);
    { const e=await snap(); check(e.screen==='identity'&&e.prologueShot===-1&&e.orbShown&&e.orbStars===0&&e.canName,'the prologue ends in darkness, then WHO ARE YOU? with the orb above it'+(reduced?' (reduced motion)':'')+' at '+where);await pg.screenshot({path:path.join(out,prefix+'-'+String(PROLOGUE.length+2)+'-identity.png')}); }
  };

(async()=>{const browser=await playwright.chromium.launch({headless:true,channel:'chrome'});try{for(const width of [390,360]){const height=width===390?844:800,ctx=await browser.newContext({viewport:{width,height},deviceScaleFactor:2,isMobile:true,hasTouch:true}),pg=await ctx.newPage();await pg.goto(process.env.GREYBOX_URL);await pg.waitForFunction(()=>!!window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});await pg.locator('#loading').waitFor({state:'detached'});await pg.locator('#skip-prologue').evaluate(b=>b.click());await pg.waitForFunction(()=>window.ascendantDial.snapshot().screen==='menu'&&!window.ascendantDial.snapshot().busy,{},{timeout:30000});await pg.locator('#menu-new').evaluate(b=>b.click());await pg.waitForFunction(()=>window.ascendantDial.snapshot().screen==='prologue',{},{timeout:10000});const frames=n=>pg.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);const tap=async(x,y)=>{const scale=Math.min(width/360,height/800);await pg.mouse.move(width/2+x*scale,(height-800*scale)/2+y*scale);await frames(2);await pg.mouse.down();await frames(2);await pg.mouse.up();await frames(2);};await playPrologue(pg,width+'-prologue',false,width,tap);await ctx.close();}fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});
