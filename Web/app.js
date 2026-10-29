(() => {
  'use strict';
  const E = window.LuminaEngine;
  const levels = window.LUMINA_CATALOG.levels;
  const $ = id => document.getElementById(id);
  const C = {cyan:'#6be8ca',pink:'#ed89c5',amber:'#efc775'};
  const names = {emitter:'Nguồn sáng',mirror:'Gương phản xạ',splitter:'Bộ tách tia',target:'Lõi đích',crystal:'Pha lê',portal:'Cổng dịch chuyển',wall:'Tường chắn',filter:'Kính lọc màu',sensor:'Cảm biến',door:'Cửa ánh sáng'};
  const descriptions = {emitter:'Nơi hành trình bắt đầu',mirror:'Chạm để xoay 90°',splitter:'Đi thẳng và phản xạ',target:'Cần nhận tia sáng đúng màu',crystal:'Đi xuyên qua để thu thập',portal:'Giữ hướng, đổi vị trí',filter:'Thay đổi màu tia sáng',sensor:'Chiếu sáng để mở cửa',door:'Mở bằng cảm biến cùng số'};
  const chapters = ['Khởi nguồn','Kết nối','Phản chiếu','Dòng chảy'];
  const chapterCount=Math.ceil(levels.length/6);
  const chapterName=index=>chapters[index]||'Mở rộng';
  const KEY = 'lumina-flow.progress.v1';
  let state = {version:1,stars:{},sound:false,skin:'classic',current:levels[0].id};
  let storageAvailable=true;
  try {
    const saved=JSON.parse(localStorage.getItem(KEY));
    if(saved && saved.version===1) {
      for(const level of levels) {const n=saved.stars?.[level.id];if(Number.isInteger(n)&&n>=1&&n<=3)state.stars[level.id]=n;}
      state.sound=saved.sound===true;
      if(['classic','aurora','pulse'].includes(saved.skin))state.skin=saved.skin;
      if(levels.some(l=>l.id===saved.current))state.current=saved.current;
    }
  } catch {storageAvailable=false;}
  let level,result,moves=0,history=[],campaign=true,chapter=0,winTimer,toastTimer,audio;
  const pieceElements=new Map();
  const totalStars=()=>Object.values(state.stars).reduce((a,b)=>a+b,0);
  const unlocked=id=>{const index=levels.findIndex(l=>l.id===id);return index===0||(index>0&&Boolean(state.stars[levels[index-1].id]));};
  const campaignIndex=()=>levels.findIndex(l=>l.id===level.id);
  function save() {
    try {localStorage.setItem(KEY,JSON.stringify(state));}
    catch {storageAvailable=false;document.querySelector('.save-note').textContent='Tiến trình tạm thời: trình duyệt không cho lưu.';}
  }
  function toast(message) {clearTimeout(toastTimer);$('toast').textContent=message;$('toast').classList.add('visible');toastTimer=setTimeout(()=>$('toast').classList.remove('visible'),3500);}
  function sound(finish=false) {
    if(!state.sound)return;
    try {
      audio ||= new (window.AudioContext||window.webkitAudioContext)();
      audio.resume().catch(()=>{});
      const notes=[261.63,293.66,329.63,392,440];
      const play=(frequency,delay)=>{
        const oscillator=audio.createOscillator(),gain=audio.createGain();
        const at=audio.currentTime+delay;
        oscillator.type='sine';oscillator.frequency.value=frequency;
        gain.gain.setValueAtTime(0,at);gain.gain.linearRampToValueAtTime(.055,at+.015);gain.gain.exponentialRampToValueAtTime(.001,at+.45);
        oscillator.connect(gain);gain.connect(audio.destination);oscillator.start(at);oscillator.stop(at+.5);
      };
      if(finish)[0,2,4].forEach((n,i)=>play(notes[n]*2,i*.13));
      else play(notes[Math.max(0,result.segments.length-1)%5],0);
    } catch {/* Audio is optional on browsers without Web Audio. */}
  }
  function haptic(finish=false) {try {navigator.vibrate?.(finish?[20,50,30]:8);}catch {}}
  function icon(p) {
    const common='fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"';
    let body='';
    switch(p.type) {
      case 'emitter':body=`<g transform="rotate(${p.rotation*90} 24 24)"><circle cx="24" cy="24" r="21" fill="currentColor" fill-opacity=".07" stroke="none"/><path d="m24 5 19 19-19 19L5 24Z" fill="#0c2833" stroke="currentColor" stroke-opacity=".65"/><path d="m24 10 14 14-14 14-14-14Z" fill="currentColor" fill-opacity=".12" stroke="currentColor" stroke-opacity=".5"/><circle cx="24" cy="24" r="9" fill="#0d454e" stroke="currentColor" stroke-width="1.4"/><circle cx="24" cy="24" r="5.5" fill="currentColor" fill-opacity=".4" stroke="none"/><path d="m22 19 7 5-7 5Z" fill="#dffff9" stroke="none"/><path d="M5 24H1m46 0h-4M24 5V2m0 44v-3" stroke-opacity=".5"/></g>`;break;
      case 'mirror':body=`<rect x="4.5" y="4.5" width="39" height="39" rx="10" fill="#101e34" stroke="#58889a" stroke-opacity=".7" stroke-width="1"/><path d="M12 8H36a4 4 0 0 1 4 4" stroke="#c0fff5" stroke-opacity=".22" stroke-width="1"/><circle cx="24" cy="24" r="14" fill="#17353f" stroke="#83dfda" stroke-opacity=".25" stroke-dasharray="1.5 2.5"/><g class="mirror-surface" style="transform:rotate(${p.rotation%2*90}deg)"><path d="m12 36 24-24" stroke="#53e7da" stroke-opacity=".18" stroke-width="10"/><path d="m12 36 24-24" stroke="#296f80" stroke-width="6"/><path d="m12 36 24-24" stroke="#e2fffa" stroke-width="2.7"/><path d="m15 37 23-23" stroke="#8efff1" stroke-opacity=".6" stroke-width=".8"/><circle cx="12" cy="36" r="2.6" fill="#bafff4"/><circle cx="36" cy="12" r="2.6" fill="#bafff4"/></g><path d="M9 21v-4M39 27v4" stroke="#9ce8e1" stroke-opacity=".45" stroke-width="1"/>`;break;
      case 'splitter':body=`<rect x="5" y="5" width="38" height="38" rx="10" fill="#242039" stroke="#aa91de" stroke-opacity=".65"/><circle cx="24" cy="24" r="14" fill="#a683ea" fill-opacity=".08" stroke="#b99cf3" stroke-opacity=".35" stroke-dasharray="2 3"/><g class="mirror-surface" style="transform:rotate(${p.rotation%2*90}deg)"><path d="m10 37 27-27 1 27Z" fill="#c1a7ff" fill-opacity=".18" stroke="#c1a7ff"/><path d="m13 34 21-21" stroke="#eadfff" stroke-width="3"/><circle cx="24" cy="24" r="3" fill="#f2e7ff" stroke="none"/><path d="M28 26h11" stroke-dasharray="2 3"/></g>`;break;
      case 'target':body='<circle cx="24" cy="24" r="21" fill="currentColor" fill-opacity=".06" stroke="none"/><circle cx="24" cy="24" r="19" stroke-opacity=".45" stroke-width="1" stroke-dasharray="2 3"/><circle cx="24" cy="24" r="14" fill="#10263a" stroke-width="1.4"/><circle cx="24" cy="24" r="10" stroke-opacity=".5" stroke-width="1"/><path class="core-fill" d="m24 15 8 4.5v9L24 33l-8-4.5v-9Z" fill="currentColor"/><path d="m24 15 8 4.5v9L24 33l-8-4.5v-9Z" stroke-width="1"/><circle cx="24" cy="24" r="3" fill="currentColor" stroke="none"/>';break;
      case 'crystal':body='<path d="m24 5 13 19-13 19-13-19Z" fill="#bd9bef" fill-opacity=".07" stroke="none"/><path class="crystal-fill" d="m24 8 10 16-10 16-10-16Z" fill="currentColor"/><path d="m24 8 10 16-10 16-10-16Z" stroke-width="1.5"/><path d="m24 8-4 16 4 16 4-16-4-16M14 24h20" stroke-width=".9" stroke-opacity=".7"/><path d="M7 15v5m-2-2h4M39 30v5m-2-2h4" stroke-width=".8" stroke-opacity=".5"/>';break;
      case 'wall':body='<rect x="5" y="7" width="38" height="36" rx="6" fill="#050b18" stroke="none"/><rect class="wall-shape" x="5" y="5" width="38" height="36" rx="6"/><path d="M9 16V12a3 3 0 0 1 3-3h24" stroke="#8ba2bd" stroke-opacity=".3" stroke-width="1"/><path class="wall-lines" d="m10 24 14-14m-12 26 24-24M24 38l14-14"/><circle cx="10" cy="36" r="1" fill="#7891a4" stroke="none"/><circle cx="38" cy="10" r="1" fill="#7891a4" stroke="none"/>';break;
      case 'portal':body='<ellipse cx="24" cy="24" rx="12" ry="18" stroke-width="2"/><ellipse cx="24" cy="24" rx="7" ry="12" stroke-opacity=".5"/><path d="M12 10 8 6m28 4 4-4M12 38l-4 4m28-4 4 4" stroke-opacity=".4"/><circle cx="24" cy="24" r="3" fill="currentColor"/>';break;
      case 'filter':body='<path d="m18 5 12 5v28l-12 5Z" fill="currentColor" fill-opacity=".18"/><path d="M18 5v38m12-33v28" stroke-width="2.5"/><path d="m21 15 6 3m-6 6 6 3m-6 6 6 3" stroke-opacity=".5"/>';break;
      case 'sensor':body='<rect x="9" y="9" width="30" height="30" rx="8" fill="currentColor" fill-opacity=".08"/><circle cx="24" cy="24" r="8"/><circle cx="24" cy="24" r="3" fill="currentColor"/><path d="M24 3v6m0 30v6M3 24h6m30 0h6" stroke-opacity=".5"/>';break;
      case 'door':body='<path d="M12 7v34m8-34v34m8-34v34m8-34v34" stroke-width="3"/><path d="M8 6h32M8 42h32" stroke-opacity=".5"/>';break;
    }
    return `<svg viewBox="0 0 48 48" ${common} aria-hidden="true">${body}</svg>`;
  }
  function makeBoard() {
    const board=$('board');board.style.setProperty('--cols',level.width);board.style.setProperty('--rows',level.height);board.style.aspectRatio=`${level.width}/${level.height}`;
    $('beams').setAttribute('viewBox',`-0.5 -0.5 ${level.width} ${level.height}`);
    $('pieces').replaceChildren();pieceElements.clear();
    for(const p of level.pieces) {
      const rotatable=['mirror','splitter'].includes(p.type)&&!p.fixed;
      const el=document.createElement(rotatable?'button':'div');
      el.className=`piece ${p.type}${rotatable?' rotatable':''}`;
      el.style.gridColumn=String(p.x+1);el.style.gridRow=String(p.y+1);
      el.style.color=C[p.color]||(p.type==='crystal'?'#caabdf':p.type==='portal'?'#b19aef':p.type==='splitter'?'#baa4e5':p.type==='door'?'#d9b875':C.cyan);
      const label=`${names[p.type]}, cột ${p.x+1}, hàng ${p.y+1}${rotatable?', chạm để xoay':''}`;
      el.setAttribute('aria-label',label);el.title=label;el.dataset.pieceId=p.id;
      el.innerHTML=icon(p);
      if(rotatable)el.addEventListener('click',()=>rotate(p.id));else el.setAttribute('role','img');
      $('pieces').append(el);pieceElements.set(p.id,el);
    }
    const legendTypes=['emitter',...['mirror','splitter','portal','filter','sensor'].filter(t=>level.pieces.some(p=>p.type===t)),'target','crystal'].slice(0,5);
    $('legend').replaceChildren();
    for(const type of legendTypes) {
      const row=document.createElement('div');row.className='legend-row';
      const sample=document.createElement('span');sample.className='legend-svg';sample.style.color=type==='crystal'?'#caabdf':type==='target'?C.cyan:'#a4c5cc';sample.innerHTML=icon({type,rotation:0});
      const text=document.createElement('div'),title=document.createElement('strong'),desc=document.createElement('small');title.textContent=names[type];desc.textContent=descriptions[type];text.append(title,desc);row.append(sample,text);$('legend').append(row);
    }
  }
  function draw() {
    result=E.evaluate(level);
    $('beams').innerHTML=result.segments.map(s=>{
      const d=`M ${s.x1} ${s.y1} L ${s.x2} ${s.y2}`,color=C[s.color]||C.cyan;
      return `<path class="beam-glow" d="${d}" stroke="${color}"/><path class="beam-body" d="${d}" stroke="${color}"/><path class="beam-center" d="${d}"/>`;
    }).join('');
    for(const p of level.pieces) {
      const el=pieceElements.get(p.id);
      el.classList.toggle('lit',result.litTargets.has(p.id)||result.litCrystals.has(p.id)||result.activeSensors.has(p.id));
      el.classList.toggle('open',result.openDoors.has(p.id));
      const surface=el.querySelector('.mirror-surface');if(surface)surface.style.transform=`rotate(${p.rotation%2*90}deg)`;
    }
    $('move-count').textContent=moves;$('par-count').textContent=`/ mục tiêu ${level.par}`;
    const crystals=level.pieces.filter(p=>p.type==='crystal').length;
    $('crystal-count').textContent=`◇ ${result.litCrystals.size} / ${crystals}`;
    $('undo').disabled=history.length===0;
    $('hint').disabled=!level.pieces.some(p=>['mirror','splitter'].includes(p.type)&&!p.fixed&&p.solution>=0&&p.solution%2!==p.rotation%2);
    $('board-status').textContent=result.won?'Tất cả lõi đích đã được thắp sáng':`${result.litTargets.size} / ${level.pieces.filter(p=>p.type==='target').length} lõi đích đã kết nối`;
    if(result.won)complete();
  }
  function rotate(id,hint=false) {
    if(result.won||$('win-dialog').open)return;
    const p=level.pieces.find(p=>p.id===id);if(!p||p.fixed||!['mirror','splitter'].includes(p.type))return;
    history.push({id,rotation:p.rotation});p.rotation=(p.rotation+1)%4;moves++;draw();sound();haptic();
    if(hint){const el=pieceElements.get(id);el.classList.remove('hinted');void el.offsetWidth;el.classList.add('hinted');toast('Đã xoay một gương theo đường giải.');}
  }
  function renderCampaign() {
    $('chapter-label').textContent=`${String(chapter+1).padStart(2,'0')} · ${chapterName(chapter)}`;
    $('prev-chapter').disabled=chapter===0;$('next-chapter').disabled=chapter===chapterCount-1;
    $('level-grid').replaceChildren();
    for(const data of levels.slice(chapter*6,chapter*6+6)) {
      const b=document.createElement('button');b.className='level-button'+(campaign&&data.id===level.id?' active':'');
      b.disabled=!unlocked(data.id);b.setAttribute('aria-label',`Màn ${data.id}: ${data.name}${b.disabled?', chưa mở khóa':`, ${state.stars[data.id]||0} sao`}`);
      b.setAttribute('aria-current',campaign&&data.id===level.id?'step':'false');
      if(b.disabled)b.innerHTML='<svg class="lock" viewBox="0 0 16 16" aria-hidden="true"><rect x="3.5" y="7" width="9" height="7" rx="2"/><path d="M5.5 7V4a2.5 2.5 0 0 1 5 0v3"/></svg>';
      else b.append(document.createTextNode(String(data.id).padStart(2,'0')));
      const stars=document.createElement('span');stars.className='level-stars';stars.innerHTML=[0,1,2].map(n=>`<span class="${n<(state.stars[data.id]||0)?'earned':''}">✦</span>`).join('');b.append(stars);
      b.addEventListener('click',()=>load(data,true));$('level-grid').append(b);
    }
    $('chapter-dots').replaceChildren();
    for(let i=0;i<chapterCount;i++){const b=document.createElement('button');b.className='chapter-dot'+(i===chapter?' current':'');b.setAttribute('aria-label',`Chương ${i+1}: ${chapterName(i)}`);b.setAttribute('aria-pressed',String(i===chapter));b.addEventListener('click',()=>{chapter=i;renderCampaign();});$('chapter-dots').append(b);}
    const completed=levels.filter(l=>state.stars[l.id]).length;
    $('progress-label').textContent=`${completed} / ${levels.length}`;$('progress-fill').style.width=completed/levels.length*100+'%';
    $('total-stars').replaceChildren(document.createTextNode(totalStars()+' '),Object.assign(document.createElement('span'),{textContent:'/ '+levels.length*3}));
    document.querySelector('.tiny-tag').textContent=levels.length+' MÀN';
  }
  function load(data,isCampaign) {
    clearTimeout(winTimer);if($('win-dialog').open)$('win-dialog').close();
    level=E.clone(data);campaign=isCampaign;moves=0;history=[];
    if(campaign){chapter=Math.floor(campaignIndex()/6);state.current=level.id;save();}
    $('level-name').textContent=level.name;
    $('level-eyebrow').textContent=campaign?`CHƯƠNG ${String(chapter+1).padStart(2,'0')} / ${chapterName(chapter).toLocaleUpperCase('vi')}`:'KHÁM PHÁ / MÀN CHIA SẺ';
    $('level-number').replaceChildren(document.createTextNode(campaign?String(campaignIndex()+1).padStart(2,'0'):'＋'),Object.assign(document.createElement('span'),{textContent:campaign?'/'+levels.length:'UGC'}));
    $('level-tip').textContent=level.subtitle||'Tìm đường để thắp sáng tất cả các lõi. Mỗi góc xoay mở ra một khả năng.';
    makeBoard();renderCampaign();draw();
  }
  function complete() {
    const stars=E.stars(level,result,moves);
    if(campaign){state.stars[level.id]=Math.max(state.stars[level.id]||0,stars);save();renderCampaign();}
    clearTimeout(winTimer);
    winTimer=setTimeout(()=>{
      $('win-stars').innerHTML=[0,1,2].map(n=>`<span class="${n<stars?'earned':''}">✦</span>`).join('');
      const final=campaign&&campaignIndex()===levels.length-1;
      $('win-title').textContent=final?'Hành trình đã bừng sáng.':'Bạn đã tìm thấy dòng chảy.';
      $('win-detail').textContent=`${moves} lần xoay · ${result.litCrystals.size} pha lê · ${stars}/3 sao${final?` — Bạn đã hoàn thành cả ${levels.length} màn!`:''}`;
      $('next-level').textContent=!campaign?'Trở về hành trình →':final?'Trở về màn đầu tiên →':'Khám phá màn tiếp theo →';
      $('win-dialog').showModal();sound(true);haptic(true);
    },550);
  }
  function reset() {load(campaign?levels.find(l=>l.id===level.id):importedSource,campaign);}
  let importedSource=null;
  $('undo').addEventListener('click',()=>{
    if(!history.length)return;clearTimeout(winTimer);if($('win-dialog').open)$('win-dialog').close();
    const previous=history.pop();level.pieces.find(p=>p.id===previous.id).rotation=previous.rotation;moves--;draw();sound();
  });
  $('reset').addEventListener('click',reset);
  $('hint').addEventListener('click',()=>{const p=level.pieces.find(p=>['mirror','splitter'].includes(p.type)&&!p.fixed&&p.solution>=0&&p.solution%2!==p.rotation%2);if(p)rotate(p.id,true);else toast('Màn này không có gợi ý được lưu.');});
  $('replay').addEventListener('click',reset);
  $('next-level').addEventListener('click',()=>load(levels[campaign?(campaignIndex()+1)%levels.length:0],true));
  $('prev-chapter').addEventListener('click',()=>{chapter=Math.max(0,chapter-1);renderCampaign();});
  $('next-chapter').addEventListener('click',()=>{chapter=Math.min(chapterCount-1,chapter+1);renderCampaign();});
  function updateSound() {
    $('sound').innerHTML=state.sound?'<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m11 4-6 5H2v6h3l6 5ZM15 8a6 6 0 0 1 0 8m3-12a11 11 0 0 1 0 16"/></svg>':'<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m11 4-6 5H2v6h3l6 5ZM16 9l6 6m0-6-6 6"/></svg>';
    $('sound').setAttribute('aria-pressed',String(state.sound));$('sound').setAttribute('aria-label',state.sound?'Tắt âm thanh':'Bật âm thanh');
  }
  $('sound').addEventListener('click',()=>{state.sound=!state.sound;save();updateSound();sound();toast(state.sound?'Đã bật âm thanh':'Đã tắt âm thanh');});
  function openShare(isImport) {
    $('share-title').textContent=isImport?'Một hành trình mới':'Chia sẻ một tia sáng';
    $('share-error').textContent='';
    try {$('share-code').value=isImport?'':E.encode(campaign?levels[campaignIndex()]:importedSource);}
    catch(error){$('share-code').value='';$('share-error').textContent=error.message;}
    $('share-code').maxLength=65536;$('share-dialog').showModal();
    if(isImport)$('share-code').focus();
  }
  $('share').addEventListener('click',()=>openShare(false));$('import').addEventListener('click',()=>openShare(true));
  $('copy-code').addEventListener('click',async()=>{
    if(!$('share-code').value.trim()){toast('Hãy nhập mã màn chơi trước.');return;}
    try {await navigator.clipboard.writeText($('share-code').value);toast('Đã sao chép mã màn chơi.');}
    catch {$('share-code').focus();$('share-code').select();toast('Mã đã được chọn. Nhấn Ctrl+C để sao chép.');}
  });
  $('load-code').addEventListener('click',()=>{
    try {const data=E.decode($('share-code').value);importedSource=E.clone(data);$('share-dialog').close();load(data,false);toast('Đang chơi màn chia sẻ. Tiến trình hành trình được giữ nguyên.');}
    catch(error){$('share-error').textContent=error.message;}
  });
  const skins=[{id:'classic',name:'Nguyên bản',description:'Tia sáng tinh giản',cost:0},{id:'aurora',name:'Cực quang',description:'Quầng sáng dịu và rộng hơn',cost:12},{id:'pulse',name:'Nhịp thở',description:'Ánh sáng chuyển động nhẹ nhàng',cost:30}];
  function applySkin(){if((skins.find(s=>s.id===state.skin)?.cost||0)>totalStars())state.skin='classic';document.body.classList.remove('skin-classic','skin-aurora','skin-pulse');document.body.classList.add('skin-'+state.skin);}
  function skinList() {
    $('skin-list').replaceChildren();
    for(const skin of skins){const b=document.createElement('button');b.className='skin-option'+(skin.id===state.skin?' selected':'');b.disabled=totalStars()<skin.cost;b.setAttribute('aria-pressed',String(skin.id===state.skin));const text=document.createElement('span'),title=document.createElement('strong'),desc=document.createElement('small');title.textContent=skin.name;desc.textContent=skin.description+(skin.cost?` · ${skin.cost} sao để mở`:' · Có sẵn');text.append(title,desc);const sample=document.createElement('span');sample.className='skin-sample';if(skin.id==='aurora')sample.style.boxShadow='0 0 14px 5px #6be8ca55';if(skin.id==='pulse')sample.style.animation='beam-pulse 1.8s ease-in-out infinite';b.append(text,sample);b.addEventListener('click',()=>{state.skin=skin.id;applySkin();save();skinList();toast('Đã chọn hiệu ứng '+skin.name+'.');});$('skin-list').append(b);}
  }
  $('palette').addEventListener('click',()=>{skinList();$('palette-dialog').showModal();});
  document.querySelectorAll('.close-modal').forEach(b=>b.addEventListener('click',()=>b.closest('dialog').close()));
  document.querySelectorAll('dialog').forEach(dialog=>dialog.addEventListener('click',event=>{if(event.target===dialog){const r=dialog.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close();}}));
  document.addEventListener('keydown',event=>{
    if(document.querySelector('dialog[open]')||/INPUT|TEXTAREA/.test(event.target.tagName))return;
    if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='z'){event.preventDefault();$('undo').click();}
    if(event.key.toLowerCase()==='r'&&!event.ctrlKey&&!event.metaKey)reset();
  });
  updateSound();applySkin();load(unlocked(state.current)?levels.find(l=>l.id===state.current):levels[0],true);
  if(!storageAvailable)document.querySelector('.save-note').textContent='Tiến trình tạm thời: trình duyệt không cho lưu.';
})();
