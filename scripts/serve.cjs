const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../Web');
const types = {'.html':'text/html; charset=utf-8','.css':'text/css; charset=utf-8','.js':'text/javascript; charset=utf-8','.svg':'image/svg+xml','.png':'image/png'};
const port = Number(process.env.PORT || 4173);
http.createServer((req,res) => {
  let target;
  try {target = path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));} catch {res.writeHead(400).end();return;}
  if (target !== root && !target.startsWith(root+path.sep)) {res.writeHead(403).end();return;}
  if (target === root) target = path.join(root,'index.html');
  fs.readFile(target,(err,data) => {
    if (err) {res.writeHead(404).end('Not found');return;}
    res.writeHead(200,{'Content-Type':types[path.extname(target)] || 'application/octet-stream','X-Content-Type-Options':'nosniff'});res.end(data);
  });
}).listen(port,'0.0.0.0',() => console.log(`Lumina Flow: http://localhost:${port}`));
