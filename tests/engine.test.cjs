const {test}=require('node:test');
const assert=require('node:assert/strict');
const E=require('../Web/engine.js');
const catalog=require('../Assets/Resources/Levels/catalog.json');
const piece=(type,x,y,extra={})=>({id:`${type}-${x}-${y}`,type,x,y,rotation:0,color:'',link:'',fixed:!['mirror','splitter'].includes(type),solution:-1,...extra});
const level=pieces=>({id:1,name:'Kiểm tra ánh sáng',subtitle:'',width:7,height:7,par:1,pieces});
const emitter=(x,y,rotation=0,color='cyan')=>piece('emitter',x,y,{rotation,color});
const target=(x,y,color='cyan')=>piece('target',x,y,{color});
test('all four incident directions reflect correctly on both mirror orientations',()=>{
  assert.deepEqual([0,1,2,3].map(d=>E.reflects(d,0)),[3,2,1,0]);
  assert.deepEqual([0,1,2,3].map(d=>E.reflects(d,1)),[1,0,3,2]);
});
test('walls block light and targets require the matching color',()=>{
  assert.equal(E.evaluate(level([emitter(0,3),piece('wall',2,3),target(6,3)])).won,false);
  assert.equal(E.evaluate(level([emitter(0,3),target(6,3,'pink')])).won,false);
  assert.equal(E.evaluate(level([emitter(0,3),piece('filter',2,3,{color:'pink'}),target(6,3,'pink')])).won,true);
});
test('splitter lights both branches and crystals do not stop rays',()=>{
  const l=level([emitter(0,5),piece('splitter',3,5),piece('crystal',3,3),target(3,0),target(6,5)]);
  const r=E.evaluate(l);assert.equal(r.won,true);assert.equal(r.litTargets.size,2);assert.equal(r.litCrystals.size,1);
});
test('portal preserves direction without drawing between the paired portals',()=>{
  const l=level([emitter(0,5),piece('portal',2,5,{link:'a'}),piece('portal',2,1,{link:'a'}),target(6,1)]);
  const r=E.evaluate(l);assert.equal(r.won,true);
  assert.ok(r.segments.every(s=>s.x1===s.x2||s.y1===s.y2));
  assert.ok(!r.segments.some(s=>s.x1===2&&s.x2===2&&s.y1===5&&s.y2===1));
});
test('sensor on another branch opens its linked door',()=>{
  const l=level([emitter(0,5),piece('splitter',2,5),piece('sensor',2,2,{link:'a',color:'cyan'}),piece('door',4,5,{link:'a'}),target(6,5)]);
  const r=E.evaluate(l);assert.equal(r.won,true);assert.equal(r.openDoors.size,1);assert.equal(r.activeSensors.size,1);
  l.pieces.find(p=>p.type==='sensor').color='pink';
  const off=E.evaluate(l);assert.equal(off.won,false);assert.equal(off.openDoors.size,0);
});
test('multiple dependent sensor-door groups converge over multiple passes',()=>{
  const l=level([emitter(0,3),piece('sensor',1,3,{link:'a'}),piece('door',2,3,{link:'a'}),piece('sensor',3,3,{link:'b'}),piece('door',4,3,{link:'b'}),target(6,3)]);
  assert.equal(E.evaluate(l).won,true);assert.equal(E.evaluate(l).openDoors.size,2);
});
test('a closed door cannot power its own inaccessible sensor',()=>{
  const l=level([emitter(0,3),piece('door',2,3,{link:'a'}),piece('sensor',3,3,{link:'a'}),target(6,3)]);
  assert.equal(E.evaluate(l).won,false);assert.equal(E.evaluate(l).openDoors.size,0);
});
test('laser cycles terminate and do not falsely report victory',()=>{
  const l=level([emitter(0,5),piece('splitter',2,5),piece('mirror',2,1,{rotation:1}),piece('mirror',0,1),target(6,0)]);
  const r=E.evaluate(l);assert.equal(r.won,false);assert.ok(r.segments.length<50);
  // Portals can form cycles too.
  const p=level([emitter(3,3),piece('portal',5,3,{link:'a'}),piece('portal',1,3,{link:'a'}),target(6,0)]);
  assert.ok(E.evaluate(p).segments.length<20);
});
test('no targets is never a win and targets absorb rays',()=>{
  assert.equal(E.evaluate(level([emitter(0,3)])).won,false);
  const r=E.evaluate(level([emitter(0,3),target(2,3),target(6,3)]));assert.equal(r.won,false);assert.equal(r.litTargets.size,1);
});
test('star awards reflect completion, all crystals, and the par independently',()=>{
  const l=level([emitter(0,3),target(6,3),piece('crystal',3,3)]),r=E.evaluate(l);
  assert.equal(E.stars(l,r,1),3);assert.equal(E.stars(l,r,2),2);
  l.pieces.at(-1).y=4;assert.equal(E.stars(l,E.evaluate(l),1),2);
  l.pieces[1].color='pink';assert.equal(E.stars(l,E.evaluate(l),1),0);
});
test('share codes roundtrip Unicode and reject malformed, oversized, overlapping and unpaired levels',()=>{
  const l=level([emitter(0,3),target(6,3)]);
  assert.deepEqual(E.decode(E.encode(l)),l);
  for(const code of ['', 'bad!', 'x'.repeat(65537), Buffer.from('{}').toString('base64')])assert.throws(()=>E.decode(code));
  assert.throws(()=>E.validate(level([emitter(0,3),target(0,3)])));
  assert.throws(()=>E.validate(level([emitter(0,3),target(6,3),piece('portal',2,2,{link:'a'})])));
  assert.throws(()=>E.validate(level([emitter(0,3),target(6,3),piece('door',2,2,{link:'missing'})])));
});
for(const l of catalog.levels)test(`campaign ${l.id}: valid, initially unsolved, reaches three stars at par, shareable`,()=>{
  E.validate(l);assert.equal(E.evaluate(l).won,false);
  const solved=E.clone(l);solved.pieces.forEach(p=>{if(p.solution>=0)p.rotation=p.solution;});
  const result=E.evaluate(solved);assert.equal(result.won,true);assert.equal(E.stars(solved,result,l.par),3);
  assert.deepEqual(E.decode(E.encode(l)),l);
});
