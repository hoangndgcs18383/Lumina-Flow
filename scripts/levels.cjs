// Canonical campaign. All emitted catalog data is checked against the laser engine.
const fs = require('node:fs');
const path = require('node:path');
const E = require('../Web/engine.js');
const root = path.resolve(__dirname,'..');
const dir = (a,b) => b[0]>a[0]?0:b[1]>a[1]?1:b[0]<a[0]?2:3;
function level(points) {
  const l = {id:1,name:'',subtitle:'',width:7,height:7,par:0,pieces:[]};
  add(l,'emitter',...points[0],{rotation:dir(points[0],points[1]),color:'cyan'});
  for (let i=1;i<points.length-1;i++) {
    const incoming=dir(points[i-1],points[i]),out=dir(points[i],points[i+1]);
    const solution=E.reflects(incoming,0)===out?0:1;
    add(l,'mirror',...points[i],{rotation:solution,solution,fixed:false});
  }
  add(l,'target',...points.at(-1),{color:'cyan'});
  // A crystal on each nontrivial straight section, with no collisions.
  for (let i=0;i<points.length-1;i++) {
    const a=points[i],b=points[i+1],d=dir(a,b),dx=[1,0,-1,0][d],dy=[0,1,0,-1][d];
    if (Math.abs(a[0]-b[0])+Math.abs(a[1]-b[1])>1) add(l,'crystal',a[0]+dx,a[1]+dy);
  }
  return l;
}
function add(l,type,x,y,extra={}) {
  if(l.pieces.some(p=>p.x===x&&p.y===y)) throw new Error(`Occupied: ${x},${y}`);
  const p={id:`${type}-${l.pieces.length+1}`,type,x,y,rotation:0,color:'',link:'',fixed:true,solution:-1,...extra};
  l.pieces.push(p);return p;
}
const bases = [
  level([[0,5],[4,5],[4,1]]),
  level([[0,6],[3,6],[3,2],[6,2]]),
  level([[0,5],[5,5],[5,1],[2,1],[2,3],[0,3]]),
  level([[0,6],[6,6],[6,0],[2,0],[2,4],[4,4],[4,2]])
];
// Splitter: both the straight and reflected branches must reach a target.
let l=level([[0,5],[3,5],[3,1],[6,1]]);
l.pieces.find(p=>p.type==='mirror').type='splitter';
add(l,'crystal',5,5);add(l,'target',6,5,{color:'cyan'});bases.push(l);
// A filter changes the color before reaching its color-matched target.
l=level([[0,6],[4,6],[4,1],[6,1]]);
add(l,'filter',4,3,{color:'pink'});l.pieces.find(p=>p.type==='target').color='pink';bases.push(l);
// Portal preserves the ray direction and does not draw a ray between endpoints.
l={id:1,name:'',subtitle:'',width:7,height:7,par:0,pieces:[]};
add(l,'emitter',0,5,{color:'cyan'});add(l,'mirror',2,5,{solution:0,fixed:false});
add(l,'crystal',2,4);add(l,'portal',2,2,{link:'wormhole-a'});add(l,'portal',5,5,{link:'wormhole-a'});
add(l,'crystal',5,3);add(l,'mirror',5,1,{solution:0,fixed:false});add(l,'target',6,1,{color:'cyan'});bases.push(l);
// Sensor encountered before a downstream door: fixed-point evaluation opens it.
l=level([[0,5],[2,5],[2,1],[6,1]]);
add(l,'sensor',2,2,{color:'cyan',link:'gate-a'});add(l,'door',4,1,{link:'gate-a'});bases.push(l);
// Independent splitter branch powers a door in the other branch.
l=level([[0,5],[2,5],[2,0]]);
l.pieces.find(p=>p.type==='mirror').type='splitter';
add(l,'sensor',2,2,{color:'cyan',link:'gate-b'});add(l,'door',4,5,{link:'gate-b'});
add(l,'mirror',6,5,{solution:0,fixed:false});add(l,'crystal',6,3);add(l,'target',6,1,{color:'pink'});add(l,'filter',6,2,{color:'pink'});bases.push(l);
// Two differently colored emitters require both independent routes.
l=level([[0,6],[2,6],[2,1],[0,1]]);
add(l,'emitter',6,0,{rotation:1,color:'pink'});add(l,'mirror',6,4,{rotation:0,solution:0,fixed:false});
add(l,'crystal',4,4);add(l,'target',3,4,{color:'pink'});bases.push(l);
// Portal + filter + two reflections.
l=E.clone(bases[6]);add(l,'filter',5,2,{color:'amber'});l.pieces.find(p=>p.type==='target').color='amber';bases.push(l);
// Longer route with mixed mirror orientation and a final color change.
l=level([[0,6],[6,6],[6,0],[2,0],[2,4],[4,4],[4,2]]);
add(l,'filter',6,3,{color:'pink'});l.pieces.find(p=>p.type==='target').color='pink';bases.push(l);
const titles=['Ánh sáng đầu tiên','Một góc nhìn khác','Theo dòng ánh sáng','Đường vòng rực rỡ','Một tia, hai hướng','Lăng kính sắc màu','Bước qua không gian','Chìa khóa ánh sáng','Cùng nhau tỏa sáng','Hai nhịp đồng điệu','Cánh cổng hổ phách','Mạch sáng bí ẩn','Phía bên kia','Đổi chiều suy nghĩ','Vũ điệu phản chiếu','Mê cung tĩnh lặng','Phân nhánh ước mơ','Qua miền sắc tím','Khoảng cách vô hình','Đánh thức cánh cửa','Kết nối đôi bờ','Giao thoa sắc màu','Xuyên qua vũ trụ','Dòng chảy vô tận'];
const tips=['Chạm vào gương để đưa tia sáng đến lõi đích.','Hai tấm gương, một đường dẫn. Hãy nhìn từ đích trở lại.','Thu thập pha lê trên đường đi để nhận thêm sao.','Mỗi lần chạm xoay gương 90°. Có thể hoàn tác bất kỳ lúc nào.','Bộ tách giữ tia đi thẳng và tạo thêm một nhánh phản xạ.','Kính lọc đổi màu tia sáng. Lõi chỉ nhận đúng màu của nó.','Hai cổng cùng ký hiệu nối liền đường đi và giữ nguyên hướng tia.','Chiếu sáng cảm biến để mở cánh cửa có cùng ký hiệu.','Dùng một nhánh chiếu vào cảm biến để mở đường cho nhánh kia.','Mỗi nguồn sáng có một màu. Hãy thắp sáng tất cả các lõi.','Kết hợp cổng dịch chuyển và kính lọc để đến đúng lõi màu.','Giữ bình tĩnh, lần theo từng góc và làm chủ dòng ánh sáng.'];
const levels=[];
for(let i=0;i<24;i++) {
  l=E.clone(bases[i%12]);l.id=i+1;l.name=titles[i];l.subtitle=tips[i%12];
  const flipX=i>=12,flipY=i>=18;
  for(const p of l.pieces) {
    if(flipX) p.x=6-p.x;if(flipY) p.y=6-p.y;
    if(p.type==='emitter') {
      if(flipX&&p.rotation%2===0)p.rotation=(p.rotation+2)%4;
      if(flipY&&p.rotation%2===1)p.rotation=(p.rotation+2)%4;
    }
    if(['mirror','splitter'].includes(p.type)) {
      if(flipX!==flipY)p.solution^=1;
      p.rotation=p.solution ^ 1;
    }
  }
  const solved=E.clone(l);solved.pieces.forEach(p=>{if(p.solution>=0)p.rotation=p.solution;});
  const final=E.evaluate(solved);
  if(!final.won || final.litCrystals.size!==l.pieces.filter(p=>p.type==='crystal').length)throw new Error(`Invalid solution ${i+1}`);
  // Add deterministic walls only outside the solution's complete optical path.
  const onRay=(x,y)=>final.segments.some(s=>s.x1===s.x2?x===s.x1&&y>=Math.min(s.y1,s.y2)&&y<=Math.max(s.y1,s.y2):y===s.y1&&x>=Math.min(s.x1,s.x2)&&x<=Math.max(s.x1,s.x2));
  let count=0;
  for(let k=0;k<49&&count<Math.min(2+Math.floor(i/4),7);k++) {
    const x=(k*3+i)%7,y=(Math.floor(k/7)*2+i)%7;
    if(!l.pieces.some(p=>p.x===x&&p.y===y)&&!onRay(x,y)) {add(l,'wall',x,y);count++;}
  }
  l.par=l.pieces.filter(p=>p.solution>=0&&p.rotation%2!==p.solution%2).length;
  E.validate(l);
  if(E.evaluate(l).won)throw new Error(`Starts solved ${i+1}`);
  levels.push(l);
}
const catalog={version:1,levels};
fs.mkdirSync(path.join(root,'Assets/Resources/Levels'),{recursive:true});
fs.writeFileSync(path.join(root,'Assets/Resources/Levels/catalog.json'),JSON.stringify(catalog,null,2)+'\n');
fs.writeFileSync(path.join(root,'Web/levels.js'),'/* Generated by scripts/levels.cjs. */\nwindow.LUMINA_CATALOG = '+JSON.stringify(catalog)+';\n');
console.log(`Built ${levels.length} validated, solvable levels for Unity and web.`);
