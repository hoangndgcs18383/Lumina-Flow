const {chromium}=require('playwright-core');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const {spawn}=require('node:child_process');
const catalog=require('../Assets/Resources/Levels/catalog.json');
const root=path.resolve(__dirname,'..');
const server=spawn(process.execPath,[path.join(root,'scripts/serve.cjs')],{env:{...process.env,PORT:'4181'},stdio:['ignore','pipe','pipe']});
let browser;
(async()=>{
  await new Promise((resolve,reject)=>{server.stdout.once('data',resolve);server.once('error',reject);});
  browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--disable-dev-shm-usage']});
  const page=await browser.newPage({viewport:{width:1440,height:1050},reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const stylesheetResponse=page.waitForResponse(response=>new URL(response.url()).pathname==='/style.css');
  await page.goto('http://localhost:4181');
  const stylesheet=await stylesheetResponse;
  assert.equal(stylesheet.status(),200,'external stylesheet loads');
  assert.ok(stylesheet.headers()['content-type'].startsWith('text/css'),'stylesheet has CSS MIME type');
  assert.equal(await page.locator('.workspace').evaluate(el=>getComputedStyle(el).display),'grid','desktop stylesheet is applied');
  await page.locator('.piece.mirror').first().waitFor();
  assert.equal(await page.locator('#move-count').textContent(),'0');
  assert.equal(await page.locator('#undo').isDisabled(),true);
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  fs.mkdirSync(path.join(root,'artifacts'),{recursive:true});
  await page.screenshot({path:path.join(root,'artifacts/web-desktop.png'),fullPage:true});
  await page.locator('#share').click();assert.ok((await page.locator('#share-code').inputValue()).length>100);
  await page.locator('#share-code').fill('not a valid code!');await page.locator('#load-code').click();assert.ok((await page.locator('#share-error').textContent()).length>0);
  await page.locator('#share-dialog .close-modal').click();
  for(const level of catalog.levels){
    assert.equal(await page.locator('#level-name').textContent(),level.name);
    if(level.id===2){
      await page.locator('.piece.mirror').first().click();assert.equal(await page.locator('#move-count').textContent(),'1');
      await page.locator('#undo').click();assert.equal(await page.locator('#move-count').textContent(),'0');
      await page.locator('.piece.mirror').first().click();await page.locator('#reset').click();assert.equal(await page.locator('#move-count').textContent(),'0');
    }
    for(let n=0;n<level.par;n++)await page.locator('#hint').click();
    await page.locator('#win-dialog').waitFor({state:'visible'});
    assert.equal(await page.locator('#win-stars .earned').count(),3,`level ${level.id} gets 3 stars`);
    if(level.id===1){await page.screenshot({path:path.join(root,'artifacts/web-win.png'),fullPage:true});}
    if(level.id<24)await page.locator('#next-level').click();
  }
  await page.reload();assert.equal(await page.locator('#progress-label').textContent(),'24 / 24');assert.ok((await page.locator('#total-stars').textContent()).startsWith('72 '));
  await page.locator('#palette').click();await page.getByRole('button',{name:/Cực quang/}).click();assert.equal(await page.locator('body').getAttribute('class'),'skin-aurora');await page.locator('#palette-dialog .close-modal').click();
  await page.locator('#share').click();const code=await page.locator('#share-code').inputValue();await page.locator('#load-code').click();assert.ok((await page.locator('#level-eyebrow').textContent()).includes('MÀN CHIA SẺ'));assert.equal(await page.locator('#progress-label').textContent(),'24 / 24');
  const mobile=await browser.newPage({viewport:{width:390,height:844},isMobile:true,hasTouch:true,reducedMotion:'reduce'});
  mobile.on('pageerror',e=>errors.push(e.message));await mobile.goto('http://localhost:4181');await mobile.locator('.piece.mirror').first().waitFor();assert.equal(await mobile.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await mobile.screenshot({path:path.join(root,'artifacts/web-mobile.png'),fullPage:true});await mobile.locator('.piece.mirror').first().tap();await mobile.locator('#win-dialog').waitFor({state:'visible'});
  // This managed Chromium blocks all file:// navigation. Exercise the same standalone
  // document directly with networking disabled, without changing browser policy.
  const offlineContext=await browser.newContext({offline:true});
  const offline=await offlineContext.newPage();offline.on('pageerror',e=>errors.push(e.message));
  await offline.setContent(fs.readFileSync(path.join(root,'artifacts/lumina-flow.html'),'utf8'));
  assert.equal(await offline.locator('.workspace').evaluate(el=>getComputedStyle(el).display),'grid','standalone HTML includes working CSS');
  await offline.locator('.piece.mirror').first().waitFor();await offline.locator('#hint').click();await offline.locator('#win-dialog').waitFor({state:'visible'});
  assert.deepEqual(errors,[]);
  console.log('PASS: all 24 levels via UI, 72 stars, undo/reset, save/reload, share validation/import, skin unlocks, mobile touch, standalone HTML with networking disabled, no page errors.');
})().catch(error=>{console.error(error);process.exitCode=1;}).finally(async()=>{await browser?.close();server.kill();});
