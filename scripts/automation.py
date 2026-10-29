#!/usr/bin/env python3
"""Trusted seven-day gate and patch policy used by fresh GitHub runners."""
import argparse
import datetime as dt
import json
import os
from pathlib import Path, PurePosixPath
import subprocess
from zoneinfo import ZoneInfo

OBJECTIVES = [
    'Establish baseline; fix CSS/asset loading, offline packaging and reproducible failures.',
    'Improve desktop/mobile UI legibility, touch targets, focus states and reduced motion.',
    'Verify mirror/splitter behavior, undo/hints, and exhaustive C#/web optical parity.',
    'Improve maintainability and performance only where measurement demonstrates a problem.',
    'Review dependency audit, local saves, recovery and hostile share-code validation.',
    'Verify catalog mirroring into Web/levels.js, offline artifacts and Git backup recovery.',
    'Run release regression, document fixes and remaining Unity/device checks; summarize the pilot.',
]
EXACT = {'package.json', 'package-lock.json', 'scripts/sync-levels.cjs',
         'scripts/package-web.cjs', 'scripts/serve.cjs', 'scripts/unity-metadata.py'}
PREFIXES = ('Web/', 'Assets/Scripts/', 'Assets/Editor/', 'Assets/Resources/Levels/', 'tests/', 'docs/')

def window(enabled, start, now=None):
    if enabled != 'true':
        return {'enabled': False, 'reason': 'LUMINA_AGENT_ENABLED is not true'}
    try:
        first = dt.date.fromisoformat(start)
    except (ValueError, TypeError):
        raise ValueError('Set LUMINA_AGENT_START_DATE to YYYY-MM-DD before enabling automation.')
    today = (now or dt.datetime.now(ZoneInfo('Asia/Bangkok'))).astimezone(ZoneInfo('Asia/Bangkok')).date()
    day = (today-first).days+1
    active = 1 <= day <= 7
    return {'enabled': active, 'day': day, 'start': first.isoformat(), 'date': today.isoformat(),
            'objective': OBJECTIVES[day-1] if active else '',
            'reason': 'Inside pilot' if active else 'Outside the seven-day pilot'}

def allowed(name):
    path = PurePosixPath(name)
    if path.is_absolute() or '..' in path.parts or any(p.startswith('.') for p in path.parts):
        return False
    if name == 'tests/test_automation.py' or path.name.startswith('AGENTS'):
        return False
    return name in EXACT or name.startswith(PREFIXES)

def run(*args):
    return subprocess.check_output(args)

def validate_changes():
    names = run('git','diff','--cached','--name-only','-z','--no-renames').decode().split('\0')
    names = [n for n in names if n]
    if len(names) > 20:
        raise ValueError('Maintenance patch exceeds 20 files; split the work.')
    for name in names:
        if not allowed(name):
            raise ValueError('Maintenance policy rejects path: '+name)
        entries = run('git','ls-files','-s','--',name).decode().splitlines()
        if any(e.startswith(('120000 ', '160000 ')) for e in entries):
            raise ValueError('Symlinks and submodules are outside maintenance scope: '+name)
    if len(run('git','diff','--cached','--binary')) > 250*1024:
        raise ValueError('Maintenance patch exceeds 250 KiB.')
    subprocess.run(['git','diff','--cached','--check'],check=True)
    return names

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command',choices=['gate','export','apply'])
    parser.add_argument('--patch',default='artifacts/maintenance.patch')
    args=parser.parse_args()
    if args.command == 'gate':
        context=window(os.environ.get('LUMINA_AGENT_ENABLED',''),os.environ.get('LUMINA_AGENT_START_DATE',''))
        Path('artifacts').mkdir(exist_ok=True)
        Path('artifacts/maintenance-context.json').write_text(json.dumps(context,indent=2)+'\n')
        if os.environ.get('GITHUB_OUTPUT'):
            with open(os.environ['GITHUB_OUTPUT'],'a') as f:
                f.write('enabled='+str(context['enabled']).lower()+'\n')
                f.write('day='+str(context.get('day',0))+'\n')
                f.write('date='+context.get('date','')+'\n')
        print(json.dumps(context))
    elif args.command == 'export':
        subprocess.run(['git','add','-A'],check=True)
        validate_changes()
        Path(args.patch).parent.mkdir(parents=True,exist_ok=True)
        Path(args.patch).write_bytes(run('git','diff','--cached','--binary','--no-renames'))
    else:
        patch=Path(args.patch)
        if patch.stat().st_size > 250*1024:
            raise ValueError('Patch file exceeds 250 KiB.')
        if patch.stat().st_size:
            subprocess.run(['git','apply','--index','--whitespace=error',str(patch.resolve())],check=True)
            print('Validated changed files:',validate_changes())

if __name__ == '__main__':
    main()
