# Pure C# verification

From the repository root, with the .NET 8 SDK installed:

```sh
dotnet run --project Assets/Tests/Pure/LuminaFlow.Tests.csproj
```

An optional final argument selects another catalog path. The runner exits nonzero on a failing assertion. It compiles the actual data, engine, move, sharing, and persistence sources without Unity, stubs, external test packages, or SDKs. `EngineTests.cs` is preprocessor-excluded inside Unity; it adds no player code. Test artifacts live in the ignored `Pure/bin` and `Pure/obj` folders.

For restricted hosts where the SDK's MSBuild/NuGet startup cannot create its named mutex, use the direct compiler runner (Python 3 plus the same .NET SDK):

```sh
LUMINA_DOTNET=/tmp/lumina-dotnet/dotnet python3 Assets/Tests/Pure/run-tests.py
```

This path compiles against .NET Standard 2.1 reference assemblies with C# 7.3, checking the API surface used by Unity 2022.3. It writes compilation artifacts to a temporary directory and removes them after execution. Set `LUMINA_DOTNET` only when `dotnet` is not already on PATH. If Node and `Web/engine.js` are available, it additionally exports 48 initial/solved canonical shares from C#, decodes and evaluates them in JavaScript, then imports JavaScript-produced shares back into C# and compares wins, targets, crystals, sensors, and doors in both directions.

For exhaustive engine parity across every slash/backslash combination in all canonical levels:

```sh
LUMINA_DOTNET=/tmp/lumina-dotnet/dotnet python3 Assets/Tests/Pure/run-tests.py --interop
```

This exports 236 combinations (`2^rotatable` per level) to `/tmp/lumina-csharp-results.json`, compares all result sets in JavaScript, then writes JavaScript-encoded shares to `/tmp/lumina-csharp-results.javascript.json` and imports/evaluates those in C#. Each fixture includes `levelId`, `mask`, `rotatableIds`, actual `rotations`, `code` and result sets. Orientations 0/2 and 1/3 are optically equivalent; exhaustive parity uses 0/1 for each movable optic. Fixed optic rotations are preserved. `--output PATH` selects another persistent C# dump; `--catalog PATH` selects another catalog. Segment counts are intentionally not compared because the engines subdivide straight paths differently.

Assertions cover every mirror orientation and emitter direction, split branches, target absorption, color filters, crystals, portal discontinuities and loops, color-sensitive visitation, cycles, chained sensor/door convergence and reset, immutable catalog snapshots, rotation history, star calculation, raw JSON shares, hostile field/size/link validation, AES authentication, backup/temporary-file recovery, and the solved canonical catalog.

The suite uses temporary directories and always deletes its saves. No real player progress is touched.
