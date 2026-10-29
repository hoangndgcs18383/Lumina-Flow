(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.LuminaEngine = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const TYPES = new Set(['emitter', 'mirror', 'splitter', 'portal', 'target', 'crystal', 'wall', 'filter', 'sensor', 'door']);
  const COLORS = new Set(['cyan', 'pink', 'amber']);
  const DX = [1, 0, -1, 0], DY = [0, 1, 0, -1];
  const clone = level => JSON.parse(JSON.stringify(level));
  const reflects = (direction, rotation) => rotation % 2 === 0 ? 3 - direction : direction ^ 1;
  const matches = (piece, color) => !piece.color || piece.color === color;

  function validate(level) {
    if (!level || !Number.isInteger(level.width) || !Number.isInteger(level.height) || level.width < 1 || level.height < 1 || level.width > 16 || level.height > 16) throw new Error('Kích thước bàn phải từ 1 đến 16 ô.');
    if (!Number.isInteger(level.id) || level.id < 1 || level.id > 2147483647 || typeof level.name !== 'string' || level.name.length > 120 || (level.subtitle !== undefined && (typeof level.subtitle !== 'string' || level.subtitle.length > 512)) || !Number.isInteger(level.par) || level.par < 0 || level.par > 1000000) throw new Error('Thông tin màn chơi không hợp lệ.');
    if (!Array.isArray(level.pieces) || level.pieces.length > 256) throw new Error('Số vật thể không hợp lệ.');
    const ids = new Set(), cells = new Set(), portals = new Map();
    for (const p of level.pieces) {
      if (!p || typeof p.id !== 'string' || !p.id.trim().length || p.id.length > 64 || ids.has(p.id) || !TYPES.has(p.type)) throw new Error('Vật thể không hợp lệ hoặc bị trùng.');
      const key = `${p.x},${p.y}`;
      if (!Number.isInteger(p.x) || !Number.isInteger(p.y) || p.x < 0 || p.y < 0 || p.x >= level.width || p.y >= level.height || cells.has(key)) throw new Error('Vật thể nằm ngoài bàn hoặc trùng ô.');
      if (!Number.isInteger(p.rotation) || p.rotation < 0 || p.rotation > 3 || (p.color !== undefined && (typeof p.color !== 'string' || (p.color !== '' && !COLORS.has(p.color))))) throw new Error('Hướng hoặc màu không hợp lệ.');
      if (p.link !== undefined && (typeof p.link !== 'string' || p.link.length > 64)) throw new Error('Liên kết không hợp lệ.');
      if (p.fixed !== undefined && typeof p.fixed !== 'boolean') throw new Error('Trạng thái khóa không hợp lệ.');
      if (p.solution !== undefined && (!Number.isInteger(p.solution) || p.solution < -1 || p.solution > 3 || (p.solution >= 0 && !['mirror','splitter'].includes(p.type)))) throw new Error('Gợi ý không hợp lệ.');
      if (['emitter', 'filter'].includes(p.type) && !COLORS.has(p.color)) throw new Error('Nguồn sáng và kính lọc phải có màu.');
      ids.add(p.id); cells.add(key);
      if (p.type === 'portal') {
        if (!p.link?.trim()) throw new Error('Cổng cần một liên kết.');
        portals.set(p.link, (portals.get(p.link) || 0) + 1);
      }
      if (['sensor', 'door'].includes(p.type) && !p.link?.trim()) throw new Error('Cảm biến và cửa cần một liên kết.');
    }
    if ([...portals.values()].some(n => n !== 2)) throw new Error('Mỗi cặp cổng phải có đúng hai cổng.');
    for (const p of level.pieces.filter(p => p.type === 'door')) if (!level.pieces.some(s => s.type === 'sensor' && s.link === p.link)) throw new Error('Cửa thiếu cảm biến cùng liên kết.');
    for (const p of level.pieces.filter(p => p.type === 'sensor')) if (!level.pieces.some(s => s.type === 'door' && s.link === p.link)) throw new Error('Cảm biến thiếu cửa cùng liên kết.');
    // UTF-8 must roundtrip without replacing unpaired UTF-16 surrogate code units.
    for (const text of [level.name,level.subtitle??'',...level.pieces.flatMap(p=>[p.id,p.type,p.color??'',p.link??''])]) {
      if (new TextDecoder('utf-8',{fatal:true}).decode(new TextEncoder().encode(text)) !== text) throw new Error('Văn bản chứa ký tự Unicode không hợp lệ.');
    }
    return level;
  }

  function evaluate(level) {
    const grid = new Map(level.pieces.map(p => [`${p.x},${p.y}`, p]));
    const portals = new Map();
    for (const p of level.pieces.filter(p => p.type === 'portal')) {
      if (!portals.has(p.link)) portals.set(p.link, []);
      portals.get(p.link).push(p);
    }
    const openLinks = new Set();
    let result;
    const doors = level.pieces.filter(p => p.type === 'door');
    for (let pass = 0; pass <= doors.length + 1; pass++) {
      result = {segments: [], litTargets: new Set(), litCrystals: new Set(), activeSensors: new Set(), openDoors: new Set(), won: false};
      for (const door of doors) if (openLinks.has(door.link)) result.openDoors.add(door.id);
      const visited = new Set();
      const queue = level.pieces.filter(p => p.type === 'emitter').map(p => ({x:p.x, y:p.y, d:p.rotation, color:p.color}));
      const newlyActive = new Set();
      for (let qi = 0; qi < queue.length; qi++) {
        let {x, y, d, color} = queue[qi];
        let sx = x, sy = y;
        const segment = () => {
          if (sx !== x || sy !== y) result.segments.push({x1:sx,y1:sy,x2:x,y2:y,color});
        };
        while (true) {
          const nx = x + DX[d], ny = y + DY[d];
          if (nx < 0 || ny < 0 || nx >= level.width || ny >= level.height) {
            x += DX[d] * .5; y += DY[d] * .5; segment(); break;
          }
          x = nx; y = ny;
          const key = `${x},${y},${d},${color}`;
          if (visited.has(key)) { segment(); break; }
          visited.add(key);
          const p = grid.get(`${x},${y}`);
          if (!p) continue;
          if (p.type === 'wall' || p.type === 'emitter' || (p.type === 'door' && !openLinks.has(p.link))) {segment();break;}
          if (p.type === 'target') {if (matches(p,color)) result.litTargets.add(p.id);segment();break;}
          if (p.type === 'crystal') {if (matches(p,color)) result.litCrystals.add(p.id);continue;}
          if (p.type === 'sensor') {if (matches(p,color)) {result.activeSensors.add(p.id);newlyActive.add(p.link);}continue;}
          if (p.type === 'mirror' || p.type === 'splitter') {
            segment();
            if (p.type === 'splitter') queue.push({x,y,d,color});
            d = reflects(d,p.rotation);sx = x;sy = y;continue;
          }
          if (p.type === 'filter') {segment();color = p.color;sx = x;sy = y;continue;}
          if (p.type === 'portal') {
            segment();
            const pair = (portals.get(p.link) || []).find(other => other.id !== p.id);
            if (!pair) break;
            x = pair.x;y = pair.y;sx = x;sy = y;continue;
          }
        }
      }
      let changed = false;
      for (const link of newlyActive) if (!openLinks.has(link)) {openLinks.add(link);changed = true;}
      if (!changed) break;
    }
    const targets = level.pieces.filter(p => p.type === 'target');
    result.won = targets.length > 0 && targets.every(p => result.litTargets.has(p.id));
    return result;
  }
  function stars(level, result, moves) {
    if (!result.won) return 0;
    return 1 + Number(level.pieces.filter(p => p.type === 'crystal').every(p => result.litCrystals.has(p.id))) + Number(moves <= level.par);
  }
  function encode(level) {
    validate(level);
    const json = JSON.stringify(level);
    const code = typeof Buffer !== 'undefined' ? Buffer.from(json,'utf8').toString('base64') : btoa(Array.from(new TextEncoder().encode(json),b => String.fromCharCode(b)).join(''));
    if(code.length>65536)throw new Error('Mã chia sẻ vượt quá 64 KiB.');
    return code;
  }
  function decode(code) {
    if (typeof code !== 'string' || !code.trim().length || code.length > 65536 || !/^[A-Za-z0-9+/=\s]+$/.test(code)) throw new Error('Mã chia sẻ không hợp lệ hoặc quá dài.');
    try {
      const compact=code.replace(/\s/g,'');
      if(compact.length%4!==0 || !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(compact))throw new Error('Base64 không hợp lệ.');
      const bytes = typeof Buffer !== 'undefined' ? Buffer.from(compact,'base64') : Uint8Array.from(atob(compact),c=>c.charCodeAt(0));
      const data=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(bytes));
      validate(data);
      data.subtitle??='';
      for(const p of data.pieces){p.color??='';p.link??='';p.fixed??=!['mirror','splitter'].includes(p.type);p.solution??=-1;}
      return data;
    } catch (error) {throw new Error('Không thể đọc màn chơi: ' + error.message);}
  }
  return {clone,validate,evaluate,stars,encode,decode,reflects};
});
