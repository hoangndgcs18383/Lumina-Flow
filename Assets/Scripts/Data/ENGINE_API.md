# Data and gameplay integration

All core types are in `LuminaFlow`, use Unity-compatible public serializable fields, and compile without Unity references.

```csharp
LevelCatalog catalog = LevelSerializer.DeserializeCatalog(catalogJson);
var play = new MoveManager(catalog.levels[0]);
play.Changed += RefreshPresentation;
play.Rotate("mirror-2"); // false if missing, fixed, or not mirror/splitter
LaserResult result = play.Result;
play.Undo();
play.Reset();

var saves = new SaveManager(Application.persistentDataPath);
saves.Load();
int earned = saves.RecordResult(play.Level, play.Result, play.MoveCount, nextLevelId);
saves.Data.settings.sound = false;
saves.Data.settings.skin = "amber";
saves.Save();

string code = LevelSerializer.Serialize(play.Level);
LevelData imported;
string error;
if (LevelSerializer.TryDeserialize(code, out imported, out error)) { /* show imported */ }
```

`Application` above belongs to the Unity caller; persistence itself only takes a directory. `RecordResult` saves winning results, retains best stars and lowest winning move count independently, and unlocks through the supplied next level ID. Omit that ID to use `level.id + 1`. `SaveData.TotalStars` sums best scores. Settings hold `music`, `sound`, `haptics` and `skin`.

`LaserEngine.Evaluate(level)` validates and evaluates without mutation. Segments use integer cell centers and half-cell board boundaries. Teleports emit no connecting segment. Result sets contain piece IDs. Incoming beams stop at walls, emitters, closed doors and all targets, including mismatched targets. Filters change outgoing color; crystals and sensors pass beams. Empty target/crystal/sensor color accepts any beam. Visitation distinguishes position, direction, and color; finite occupied-cell states bound cycles. Door links are monotonic within one evaluation and reset on the next; only the final trace is rendered.

`PieceData.@fixed` has JSON name `fixed`; only nonfixed mirrors/splitters can rotate. The JSON loader defaults omitted fixed values to false for optics and true otherwise. `solution = -1` means no hint, and 0–3 supplies a mirror/splitter hint orientation. `DeepClone()` on catalog/level/piece prevents editing source content.

Sharing is Base64 of raw UTF-8 `LevelData` JSON, without an envelope. Imports validate positive level IDs, board dimensions (1–16), at most 256 pieces, unique piece IDs/cells, coordinates, field types, rotations, known colors/types, exact portal pairs, and sensor/door link counterparts. Share input is capped at 64 KiB before decoding. `FromJson`, `ToJson`, `SerializeCatalog`, and `DeserializeCatalog` support content tooling. Validation does not require a solvable level; the engine only wins when at least one target exists and every target is lit.

See [VALIDATION.md](VALIDATION.md) for precise field limits, defaults, JSON syntax rules and cross-platform validation details. `Assets/Scripts/Core/link.xml` preserves all six reflection-serialized DTO types for Unity IL2CPP builds.

Star rating: one for completion, one when every crystal is lit (also awarded when no crystals exist), and one for moves at or below par.

Local saves use AES-256-CBC, random IV/salt, PBKDF2-SHA256 and encrypt-then-HMAC-SHA256. Authentication precedes JSON parsing. Writes flush a staging file and atomically replace the primary, retaining the previous valid save as `.bak`. On platforms without `File.Replace`, a recoverable rename sequence preserves a backup. Loading checks primary, backup, then an interrupted staging write, validates the data and repairs a recovered primary. The first save seeds a backup. Recovery flags report what happened. Client-held keys provide local privacy and corruption detection; they cannot establish trusted scores or prevent determined client modification.
