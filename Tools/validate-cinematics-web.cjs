// Focused diagnostic: extracts the actual full-suite helpers, never substitutes a release-gate pass.
const fs=require('fs'),path=require('path');
const source=fs.readFileSync(path.join(__dirname,'validate-greybox-web.cjs'),'utf8');
const from=source.indexOf('  const PROLOGUE='),to=source.indexOf('  const starsSettled=');
if(from<0||to<=from)throw Error('Cinematic helper boundaries missing');
const prefix=`const fs=require('fs'),path=require('path'),playwright=require(process.env.PLAYWRIGHT_MODULE||'playwright');const out=process.env.EVIDENCE_DIR||'Logs/cinematic-diagnostic';fs.mkdirSync(out,{recursive:true});const report=[];function check(v,t){if(!v)throw Error(t);report.push('PASS: '+t);}\n`;
const driver=`
(async()=>{const browser=await playwright.chromium.launch({headless:true,channel:'chrome'});try{
for(const reduced of [false,true])for(const width of [390,360]){
const viewport={width,height:width===390?844:800},ctx=await browser.newContext({viewport,deviceScaleFactor:Number(process.env.DEVICE_SCALE||1),isMobile:!!process.env.MOBILE,hasTouch:!!process.env.MOBILE,reducedMotion:reduced?'reduce':'no-preference'}),pg=await ctx.newPage();
const errors=[];pg.on('pageerror',e=>errors.push(String(e)));
const frames=n=>pg.evaluate(k=>new Promise(done=>{const step=i=>i<=0?done():requestAnimationFrame(()=>step(i-1));step(k);}),n);
const tap=async(x,y)=>{const scale=Math.min(viewport.width/360,viewport.height/800);await pg.mouse.move(viewport.width/2+x*scale,(viewport.height-800*scale)/2+y*scale);await frames(2);await pg.mouse.down();await frames(2);await pg.mouse.up();await frames(2);};
await pg.goto(process.env.GREYBOX_URL);await loaded(pg);await pg.evaluate(()=>window.ascendantDial.act('relaunch'));
await playLaunch(pg,width+'-'+reduced+'-launch',reduced,width,reduced?'continents':null,reduced?null:tap);
await toMenu(pg);await newGame(pg);await playPrologue(pg,width+'-'+reduced+'-prologue',reduced,width,reduced?null:tap);
check(errors.length===0,'no runtime exceptions in focused cinematic diagnosis '+width+' reduced='+reduced);await ctx.close();}
fs.writeFileSync(path.join(out,'validation.txt'),report.join('\\n'));console.log(report.join('\\n'));console.log('FOCUSED DIAGNOSTIC ONLY: no release clearance');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});`;
new Function('require','process',prefix+source.slice(from,to)+driver)(require,process);
