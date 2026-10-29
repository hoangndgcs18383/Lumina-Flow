const fs=require('node:fs');
const path=require('node:path');
const E=require('../Web/engine.js');
const root=path.resolve(__dirname,'..');
const catalog=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/Levels/catalog.json'),'utf8'));
if(catalog.version!==1||!Array.isArray(catalog.levels)||!catalog.levels.length||catalog.levels.length>1000)throw new Error('Invalid catalog');
const ids=new Set();
for(const level of catalog.levels){E.validate(level);if(ids.has(level.id))throw new Error('Duplicate level ID');ids.add(level.id);}
fs.writeFileSync(path.join(root,'Web/levels.js'),'/* Generated from Assets/Resources/Levels/catalog.json. */\nwindow.LUMINA_CATALOG = '+JSON.stringify(catalog)+';\n');
console.log(`Synced ${catalog.levels.length} levels to the web preview.`);
