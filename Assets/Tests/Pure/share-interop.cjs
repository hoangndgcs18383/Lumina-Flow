'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const engine = require(path.resolve(__dirname, '../../..', 'Web/engine.js'));
const cases = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const returned = [];
for (const fixture of cases) {
  const level = engine.decode(fixture.code);
  const result = engine.evaluate(level);
  assert.equal(result.won, fixture.won, fixture.label + ' win');
  for (const field of ['litTargets', 'litCrystals', 'activeSensors', 'openDoors']) {
    assert.deepEqual([...result[field]].sort(), fixture[field].sort(), fixture.label + ' ' + field);
  }
  returned.push({
    ...fixture,
    label: fixture.label, code: engine.encode(level), won: result.won,
    litTargets: [...result.litTargets], litCrystals: [...result.litCrystals],
    activeSensors: [...result.activeSensors], openDoors: [...result.openDoors]
  });
}
fs.writeFileSync(process.argv[3], JSON.stringify(returned), 'utf8');
console.log('PASS C# → JavaScript shares and results: ' + cases.length + ' fixtures.');
