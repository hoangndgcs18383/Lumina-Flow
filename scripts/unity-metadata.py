#!/usr/bin/env python3
"""Create stable Unity GUIDs for new source assets without overwriting existing metadata."""
from pathlib import Path
import uuid

root=Path(__file__).resolve().parents[1]
namespace=uuid.UUID('36a5cbfe-1752-45fc-b6ee-848624b6a990')
count=0
for asset in sorted((root/'Assets').rglob('*')):
    if asset.suffix=='.meta' or any(part in ('bin','obj','__pycache__') for part in asset.relative_to(root).parts):
        continue
    meta=Path(str(asset)+'.meta')
    if meta.exists():
        continue
    guid=uuid.uuid5(namespace,str(asset.relative_to(root))).hex
    text=f'fileFormatVersion: 2\nguid: {guid}\n'
    if asset.is_dir():
        text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif asset.suffix=='.cs':
        text+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {fileID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif asset.suffix=='.shader':
        text+='ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif asset.suffix=='.json':
        text+='TextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    else:
        text+='DefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    meta.write_text(text)
    count+=1
print(f'Added {count} stable Unity metadata files.')
