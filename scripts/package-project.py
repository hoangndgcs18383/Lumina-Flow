#!/usr/bin/env python3
"""Package source only: no repository internals, dependencies, saves or build caches."""
from pathlib import Path
import zipfile

root=Path(__file__).resolve().parents[1]
output=root/'artifacts'/'Lumina-Flow-project.zip'
output.parent.mkdir(exist_ok=True)
excluded={'.git','node_modules','artifacts','Library','Temp','Logs','UserSettings','Build','Builds','bin','obj','__pycache__'}
included={'Assets','Packages','ProjectSettings','Web','docs','scripts','tests','.github','.gitignore','AGENTS.md','README.md','package.json','package-lock.json'}
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED) as bundle:
    for item in sorted(root.rglob('*')):
        relative=item.relative_to(root)
        if item.is_file() and relative.parts[0] in included and not any(part in excluded for part in relative.parts):
            bundle.write(item,Path('Lumina-Flow')/relative)
print(f'{output} ({output.stat().st_size:,} bytes)')
