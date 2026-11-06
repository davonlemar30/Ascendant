const fs=require('fs'),path=require('path'),playwright=require(process.env.PLAYWRIGHT_MODULE);const out=process.env.EVIDENCE_DIR;fs.mkdirSync(out,{recursive:true});const report=[];function check(v,t){if(!v)throw Error(t);report.push('PASS: '+t);}
  const LAUNCH=[['logo','studio-logo',700,'TSG Games.'],['draw','intro-wheel-pencil-lines',1000,'A zodiac wheel draws itself in pencil, then adds its twelve symbols.'],['glyphs','intro-wheel-pencil-glyphs',650,''],
    ['alive','intro-wheel-lit',1100,'The wheel lights up in gold, and the dark fills with stars.'],['burn','intro-wheel-burning',1300,'The wheel burns with golden fire and begins to spin.'],['transform','intro-earth',700,'The fire turns to light. The wheel becomes Earth.'],
    ['earth','intro-earth',900,'Earth, seen from space.'],['continents','intro-continents',400,'North America at night.'],['america','intro-america',400,'The United States at night.'],['newyork-state','intro-newyork-state',400,'New York State.'],
    ['newyork-city','intro-newyork-city',400,'New York City, between its rivers.'],['brooklyn','intro-brooklyn',400,"Brooklyn's rooftops at night."],['block','intro-block',500,'A street of apartment buildings, a few cars, lit shopfronts.'],
    ['window','prologue-city',900,'One building. Every window is dark but one.']]; // [shot, frame, ms to settle, its spoken line (the owner's, Oct 9)] (round 4: the wheel's layers over the sky)
  const playLaunch=async(pg,prefix,reduced,where,stopAt,tap)=>{
    const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot());
    let zoomSeen=0;
    // Observe brief animated states inside the page; screenshots and RPCs can outlast a shot on Linux.
    // A missing intermediate state or missing later shot still times out, including an animation that never finishes.
    await pg.evaluate(({reduced,frames})=>{
      window.__wheel={n:0,bad:[]};window.__launchSamples={};let last='';
      const tick=()=>{const s=window.ascendantDial?.snapshot();
        if(s&&(s.screen==='logo'||s.screen==='intro')){
          const id=s.launchShotId,cam=s.launchCamera||[],ls=(s.launchLayers||[]).map(x=>x.split(':'));
          const b=ls.find(x=>x[0]==='intro-wheel-burning');
          let observed=cam.length===3&&s.launchFrame===frames[id];
          if(!reduced&&['draw','glyphs'].includes(id))observed=observed&&cam[2]>0&&cam[2]<1&&(id!=='draw'||cam[2]>.05&&cam[2]<.95);
          if(!reduced&&id==='alive')observed=observed&&cam[0]<1;
          if(!reduced&&id==='burn')observed=observed&&Math.abs(cam[1])>.1;
          if(id==='transform')observed=observed&&b&&+b[2]>0&&+b[2]<1&&ls.some(x=>x[0]==='intro-earth');
          if(!reduced&&['earth','brooklyn','window'].includes(id))observed=observed&&cam[0]>1;
          if(observed&&!window.__launchSamples[id])window.__launchSamples[id]={state:s,skip:!document.getElementById('skip-prologue').disabled,said:document.getElementById('announcement').textContent,observedAt:performance.now()};
          if(id==='burn'||id==='transform'){const key=JSON.stringify(s.launchLayers);if(key!==last){last=key;window.__wheel.n++;
            const w=ls.filter(x=>x[0].startsWith('intro-wheel'));if(w.some(x=>Math.abs(((+x[1]-+w[0][1]+540)%360)-180)>.05))window.__wheel.bad.push(key);
            if(ls.some(x=>x[0]==='intro-earth'&&Math.abs(+x[1])>.05))window.__wheel.bad.push('earth turned: '+key);}}
        }
        if(!s||s.screen==='logo'||s.screen==='intro')requestAnimationFrame(tick);
      };requestAnimationFrame(tick);
    },{reduced,frames:Object.fromEntries(LAUNCH.map(([id,frame])=>[id,frame]))});
    for(const [i,[id,frame,settle,line]] of LAUNCH.entries()){
      await pg.waitForFunction(k=>!!window.__launchSamples[k],id,{timeout:20000});
      const sample=await pg.evaluate(k=>window.__launchSamples[k],id);
      const s=sample.state,skip=sample.skip,cam=s.launchCamera||[],logo=id==='logo',said=sample.said;
      fs.writeFileSync(path.join(out,prefix+'-observations.json'),JSON.stringify(await pg.evaluate(()=>window.__launchSamples),null,2));
      check(s.screen===(logo?'logo':'intro')&&s.launchShotId===id&&s.launchFrame===frame&&s.canSkip&&skip&&!s.gearShown&&s.caspar===line&&(line===''||said.includes(line))&&!s.canSliceContinue,'the launch'+(reduced?', reduced motion':'')+': '+id+' ('+frame+') on the '+(logo?'logo screen':'launch movie')+', its spoken line '+(line?'announced ("'+line+'")':'none')+', the semantic Skip offered, no gear, at '+where);
      if(reduced&&!logo)check(s.launchStill&&cam.length===3&&Math.abs(cam[0]-1)<.001&&Math.abs(cam[1])<.01&&Math.abs(cam[2]-1)<.001,'reduced motion: '+id+' held still and level, no sweep ('+JSON.stringify(cam)+') at '+where);
      if(!reduced&&id==='draw')check(cam.length===3&&cam[2]>.05&&cam[2]<.95,'the wheel draws itself: the sweep has revealed '+Math.round((cam[2]||0)*100)+'% of its lines at '+where);
      if(!reduced&&id==='glyphs')check(cam.length===3&&cam[2]>0&&cam[2]<1&&Math.abs(cam[2]*12-Math.round(cam[2]*12))<.01,'its symbols appear one at a time: '+Math.round((cam[2]||0)*12)+' of 12 so far at '+where);
      if(!reduced&&['draw','glyphs','alive'].includes(id)){check(cam.length===3&&cam[0]>=.799&&cam[0]<1&&cam[0]>zoomSeen,'the wheel zooms in slowly through '+id+' (scale '+(cam[0]||0).toFixed(3)+', up from '+zoomSeen.toFixed(3)+') at '+where);zoomSeen=cam[0];}
      if(!reduced&&id==='burn')check(cam.length===3&&Math.abs(cam[1])>.1&&Math.abs(cam[0]-1)<.001,'the wheel spins: '+frame+' turned '+(cam[1]||0).toFixed(1)+' degrees, a round layer at its own size, at '+where);
      if(id==='transform'){const ls=(s.launchLayers||[]).map(x=>x.split(':')),b=ls.find(x=>x[0]==='intro-wheel-burning'),e=ls.findIndex(x=>x[0]==='intro-earth');
        check(cam.length===3&&Math.abs(cam[1])<.01&&b&&+b[2]<1&&e>=0&&e<ls.indexOf(b)&&(reduced||Math.abs(+b[1])>.1),'the transform: Earth fades in level under the burning wheel ('+JSON.stringify(s.launchLayers)+'), which spins on and fades away, at '+where);}
      if(id==='earth'){const w=await pg.evaluate(()=>window.__wheel);check(w&&w.n>=4&&w.bad.length===0,'through the burn and the transform every wheel layer turned at one angle and Earth never turned ('+(w?w.n:0)+' states read'+(w&&w.bad.length?'; '+w.bad.slice(0,3).join(' | '):'')+') at '+where);
        check(JSON.stringify(s.launchLayers.map(x=>x.split(':')[0]))==='["intro-earth"]','after the transform only Earth is on stage at '+where);}
      if(!reduced&&['earth','brooklyn','window'].includes(id))check(cam.length===3&&cam[0]>1&&Math.abs(cam[1])<.01,'the push-in on '+frame+' (scale '+(cam[0]||0).toFixed(3)+') at '+where);
      await pg.screenshot({path:path.join(out,prefix+'-'+String(i+1).padStart(2,'0')+'-'+id+'.png')});
      if(tap&&id==='continents'){await tap(0,400);await pg.waitForFunction(()=>window.ascendantDial.snapshot().skipShown,{},{timeout:3000}).catch(()=>{});const a=await snap();
        check(a.skipShown&&a.screen==='intro'&&a.launchShot>=s.launchShot,'in the launch movie a canvas tap shows Skip and advances nothing at '+where);}
      if(id===stopAt)return;
    }
  };

(async()=>{const browser=await playwright.chromium.launch({headless:true,channel:'chrome'});try{for(const width of [390,360]){const ctx=await browser.newContext({viewport:{width,height:width===390?844:800},deviceScaleFactor:2,isMobile:true,hasTouch:true});const pg=await ctx.newPage();await pg.goto(process.env.GREYBOX_URL);await pg.waitForFunction(()=>!!window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});await pg.locator('#loading').waitFor({state:'detached'});await pg.evaluate(()=>window.ascendantDial.act('relaunch'));await playLaunch(pg,width+'-normal',false,width,null,null);await pg.waitForFunction(()=>window.ascendantDial.snapshot().screen==='menu',{},{timeout:20000});await ctx.close();}fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});
