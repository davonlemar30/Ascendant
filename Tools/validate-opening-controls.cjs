// Targeted player-control checks; also called by the complete browser suite.
const fs=require('fs');
const path=require('path');
async function run({browser,VIEWPORTS,out,check,skipPrologue,DEFAULT_CHART}) {
  // Player-control coverage: separate from the direct-command state checks below.
  // Only observation reads the test hook. Birth choices, submissions and Back tap the
  // rendered canvas; text goes through the real HTML inputs and their callbacks.
  await Promise.all(VIEWPORTS.map(async viewport=>{
    const context=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE});
    const pg=await context.newPage();const errors=[];pg.on('pageerror',e=>errors.push(String(e)));
    const snap=()=>pg.evaluate(()=>window.ascendantDial.snapshot());
    const frames=()=>pg.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
    const tap=async id=>{
      await pg.waitForFunction(k=>{const b=document.getElementById(k);return b&&!b.disabled;},id);
      await frames();
      const box=await pg.locator('#'+id).evaluate(b=>({x:parseFloat(b.style.left)+parseFloat(b.style.width)/2,y:parseFloat(b.style.top)+parseFloat(b.style.height)/2}));
      if(!Number.isFinite(box.x)||!Number.isFinite(box.y))throw Error('player-control: missing laid-out target '+id);
      await pg.mouse.move(box.x,box.y);await frames();await pg.mouse.down();await frames();await pg.mouse.up();await frames();
    };
    const step=key=>pg.waitForFunction(k=>{const s=window.ascendantDial.snapshot();return s.screen==='birth'&&s.birthStep===k&&!s.busy;},key);
    const ok=(value,text)=>check(value,'player-control coverage at '+viewport.width+': '+text);
    const editAndBlur=async(id,first,edited,key)=>{
      const input=pg.locator('#'+id);await input.fill(first);await input.fill(edited);
      // A real click away triggers blur/change; no synthetic dispatchEvent or game command.
      await pg.mouse.click(8,Math.floor(viewport.height/2));await frames();await pg.waitForTimeout(300);
      ok((await snap()).birthStep===key && await input.inputValue()===edited,id+' editing and click-away keep the step and edited answer');
      await input.focus();await input.press('Enter');await frames();await pg.waitForTimeout(300);
      ok((await snap()).birthStep===key && await input.inputValue()===edited,id+' Enter keeps the step and answer; only its button submits');
    };
    await pg.goto(process.env.GREYBOX_URL||'http://127.0.0.1:8000');await skipPrologue(pg);
    await pg.locator('#name').fill('Tester');await pg.mouse.click(8,Math.floor(viewport.height/2));await frames();await tap('next-screen');
    await pg.waitForFunction(()=>window.ascendantDial.snapshot().canBirth);
    await tap('birth-chart');await step('date');
    await editAndBlur('birthdate','1990-05-10','1990-05-01','date');
    await tap('use-date');await step('time');
    ok((await snap()).birthDraftDate==='1990-05-01','Use this date commits the edited date');
    await editAndBlur('birthtime','13:15','14:30','time');
    await tap('use-time');await step('place');
    ok((await snap()).birthDraftTime==='14:30','Use this time commits the edited time');
    await pg.locator('#birthplace').fill('Paris');await pg.locator('#birthplace').fill('Londo');
    await pg.waitForFunction(()=>(window.ascendantDial.snapshot().placeMatches||[])[0]==='London, Britain (UK)');
    await pg.mouse.click(8,Math.floor(viewport.height/2));await frames();
    ok((await snap()).birthStep==='place' && await pg.locator('#birthplace').inputValue()==='Londo','place editing and blur wait for a match selection');
    await tap('place-match-0');await step('review');
    const rows=['Date: 1 May 1990','Time: 2:30 PM','Place: London, Britain (UK)'];
    let s=await snap();ok(s.birthHeading==='Is this right?'&&JSON.stringify(s.birthReview)===JSON.stringify(rows)&&s.canBirthConfirm&&!s.sunSign,'review shows all edited facts before computing the chart');
    await pg.screenshot({path:path.join(out,viewport.width+'-player-controls-review.png')});
    await tap('birth-back');await step('place');
    ok((await snap()).birthDraftPlace==='London, Britain (UK)'&&await pg.locator('#birthplace').inputValue()==='London, Britain (UK)','Back restores the selected place in its input');
    await tap('birth-back');await step('time');
    ok(await pg.locator('#birthtime').inputValue()==='14:30','Back restores the time in its input');
    await tap('birth-back');await step('date');
    ok(await pg.locator('#birthdate').inputValue()==='1990-05-01','Back restores the date in its input');
    await tap('use-date');await step('time');await tap('use-time');await step('place');await tap('place-match-0');await step('review');
    ok(JSON.stringify((await snap()).birthReview)===JSON.stringify(rows),'forward again retains the reviewed answers');
    await tap('birth-confirm');await step('done');
    s=await snap();ok(s.bigThree==='\u2609 Taurus \u00b7 \u263d Leo \u00b7 \u2191 Virgo'&&s.canSliceContinue,'Yes commits the reviewed chart');
    await tap('change-birth');await pg.waitForFunction(()=>window.ascendantDial.snapshot().canBirth);
    await tap('birth-chart');await step('date');await tap('birth-back');
    ok((await snap()).canBirth,'Back from date returns to the path question');
    await tap('birth-skip');await step('done');
    s=await snap();ok(s.canSliceContinue&&!s.canSignPick&&!s.canBirthConfirm&&s.note.endsWith(DEFAULT_CHART)&&s.sunSign==='Scorpio','skip asks no facts or signs and uses the approved default chart');
    await tap('change-birth');await pg.waitForFunction(()=>window.ascendantDial.snapshot().canBirth);
    ok((await snap()).canBirth,'Change my answer returns from skip to the path question');
    ok(errors.length===0,'no runtime exceptions'+(errors.length?': '+errors.join('; '):''));
    await context.close();
  }));
}
module.exports=run;

if(require.main===module){
  (async()=>{
    const playwright=require(process.env.PLAYWRIGHT_MODULE||'playwright');
    const browser=await playwright.chromium.launch({headless:true,channel:'chrome'});
    const out=process.env.EVIDENCE_DIR||'Logs/OpeningControls';fs.mkdirSync(out,{recursive:true});
    const report=[];
    const check=(value,text)=>{if(!value)throw Error(text);report.push('PASS: '+text);};
    const VIEWPORTS=(process.env.VIEWPORTS||'1280x800,390').split(',').map(v=>v==='390'?{width:390,height:844}:v==='360'?{width:360,height:800}:{width:+v.split('x')[0],height:+v.split('x')[1]});
    const skipPrologue=async pg=>{
      await pg.waitForFunction(()=>!!window.ascendantDial?.snapshot()?.screen,{},{timeout:120000});
      await pg.locator('#loading').waitFor({state:'detached'});
      for(let n=0;n<160;n++){
        const s=await pg.evaluate(()=>window.ascendantDial.snapshot());
        if(s.screen==='identity'&&!s.busy)return;
        let id=(s.screen==='logo'||s.screen==='intro'||s.screen==='prologue')&&s.canSkip?'skip-prologue':s.screen==='menu'&&s.canMenuNew?'menu-new':s.screen==='saveslots'&&s.canConfirm?'confirm-start':s.screen==='saveslots'&&s.slotEnabled?.[0]?'slot-1':null;
        if(id)await pg.locator('#'+id).evaluate(b=>b.click());
        await pg.waitForTimeout(250);
      }
      throw Error('targeted setup did not reach identity');
    };
    try{
      await run({browser,VIEWPORTS,out,check,skipPrologue,DEFAULT_CHART:"You have the default chart: born November 4, 1990, at 7:19 PM, in Bushwick, Brooklyn, New York. You can add your own birth details in your journal later."});
      fs.writeFileSync(path.join(out,'validation.txt'),report.join('\n'));console.log(report.join('\n'));
    }finally{await browser.close();}
  })().catch(error=>{console.error(error);process.exitCode=1;});
}
