#!/usr/bin/env python3
"""Compile actual Unity-compatible C# sources without MSBuild, NuGet, or Unity."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[3]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--interop", action="store_true", help="Compare all 2^rotatable parity combinations in both engines and persist the result dumps.")
parser.add_argument("--output", type=Path, default=Path("/tmp/lumina-csharp-results.json"), help="C# exhaustive fixture dump path.")
parser.add_argument("--catalog", type=Path, default=root / "Assets/Resources/Levels/catalog.json")
options = parser.parse_args()
dotnet = os.environ.get("LUMINA_DOTNET") or shutil.which("dotnet")
if not dotnet:
    raise SystemExit("Install .NET 8 SDK, or set LUMINA_DOTNET to its dotnet executable.")
sdks = subprocess.check_output([dotnet, "--list-sdks"], text=True).strip().splitlines()
sdks = [line for line in sdks if line.startswith("8.")]
if not sdks:
    raise SystemExit("The .NET 8 SDK is required.")
version, location = sdks[-1].split(" ", 1)
sdk_root = Path(location.strip("[]"))
compiler = sdk_root / version / "Roslyn/bincore/csc.dll"
references = sdk_root.parent / "packs/NETStandard.Library.Ref/2.1.0/ref/netstandard2.1"
if not references.is_dir():
    raise SystemExit("The SDK must include the .NET Standard 2.1 reference pack (Unity API compatibility).")
scripts = root / "Assets/Scripts"
sources = sorted((scripts / "Data").glob("*.cs")) + [
    scripts / "Gameplay/LaserEngine.cs", scripts / "Gameplay/MoveManager.cs",
    scripts / "Core/CryptoUtility.cs", scripts / "Core/SaveManager.cs",
    scripts / "Core/LevelSerializer.cs", Path(__file__).with_name("EngineTests.cs"),
]
with tempfile.TemporaryDirectory(prefix="lumina-csharp-") as directory:
    output = Path(directory) / "LuminaFlow.Tests.dll"
    args = ["-nologo", "-noconfig", "-nostdlib+", "-langversion:7.3", "-target:exe", "-out:" + str(output)]
    args.extend("-r:" + str(p) for p in sorted(references.glob("*.dll")))
    args.extend(str(p) for p in sources)
    subprocess.run([dotnet, str(compiler)] + args, check=True, cwd=root)
    output.with_suffix(".runtimeconfig.json").write_text(
        '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}'
    )
    runner = [dotnet, str(output), str(options.catalog.resolve())]
    result = subprocess.run(runner, cwd=root)
    if result.returncode:
        raise SystemExit(result.returncode)
    node = shutil.which("node")
    if node and (root / "Web/engine.js").is_file():
        exported = options.output.resolve() if options.interop else Path(directory) / "csharp-shares.json"
        imported = exported.with_name(exported.stem + ".javascript.json") if options.interop else Path(directory) / "javascript-shares.json"
        exported.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(runner + ["--export-combinations" if options.interop else "--export-interop", str(exported)], check=True, cwd=root)
        subprocess.run([node, str(Path(__file__).with_name("share-interop.cjs")), str(exported), str(imported)], check=True, cwd=root)
        subprocess.run(runner + ["--import-interop", str(imported)], check=True, cwd=root)
        if options.interop:
            print("C# results:", exported)
            print("JavaScript shares/results:", imported)
    else:
        if options.interop:
            raise SystemExit("--interop requires Node and Web/engine.js.")
        print("JavaScript interoperability skipped: Node or Web/engine.js unavailable.")
