# Shared level validation rules

`LevelSerializer` validates data independently of whether it is playable or solvable. A 1×1 board, empty `pieces` array, or board without emitters/targets is valid data. `LaserEngine` only wins when at least one target exists and every target is lit. A web editor may add an explicit `requirePlayable` option requiring at least one emitter and one target; leave that option off for shared structural validation and engine evaluation.

| Field | Exact C# rule |
| --- | --- |
| Level `id` | Required integer, 1 through 2147483647. |
| Level `name` | Required string, 0–120 UTF-16 code units. Empty is valid. |
| Level `subtitle` | Optional string, defaults `""`, at most 512 code units. |
| `width`, `height` | Required integers, each 1–16 inclusive. |
| `par` | Required integer, 0–1000000 inclusive. |
| `pieces` | Required array, 0–256 nonnull piece objects. |
| Piece `id` | Required string, 1–64 code units, cannot be whitespace-only, unique within the level (ordinal/case sensitive). |
| Piece `type` | Required exact lowercase string: emitter, mirror, splitter, portal, target, crystal, wall, filter, sensor, door. |
| Piece `x`, `y` | Required integers, `0 <= x < width`, `0 <= y < height`. Every piece occupies a distinct cell. |
| Piece `rotation` | Required integer, 0–3 inclusive, even on nonrotatable pieces. |
| Piece `color` | Optional string, defaults `""`. Exact values `""`, `"cyan"`, `"pink"`, `"amber"`. Emitter/filter must have a nonempty color. |
| Piece `link` | Optional string, defaults `""`, at most 64 code units. Portal/sensor/door requires a non-whitespace link. |
| Piece `fixed` | Optional boolean. If absent, false for mirror/splitter and true for every other type. Explicit false on a nonoptical piece is valid data but does not make it rotatable. |
| Piece `solution` | Optional integer, defaults -1. Values -1 through 3; values 0–3 are allowed only on mirror/splitter. |

Every portal link must occur on exactly two portals. The set of sensor links must equal the set of door links: a link can have multiple sensors and doors but requires at least one of each. Matching any sensor opens every door on that link. Portal pairing uses a separate namespace, so the same text can also appear on a sensor/door link. Link values on other piece types are ignored.

Provided fields cannot be null. Omitted optional fields take their defaults. Unknown fields are ignored after syntactic parsing. IDs and links preserve whitespace/case; only an entirely whitespace ID or required link is rejected. Strings must be well-formed Unicode and JSON control characters must be escaped. String limits above are JavaScript-compatible UTF-16 `.length`, not UTF-8 byte lengths.

Shares are Base64 of UTF-8 raw `LevelData` JSON, with no envelope. The entire incoming share string must be nonblank and no longer than 65536 characters before decoding; whitespace counts toward the cap. Standard Base64 decoding accepts whitespace. The decoded bytes must be valid UTF-8. Direct `FromJson` also limits JSON to 65536 UTF-16 code units; `Serialize` checks the final Base64 length. A valid in-memory level may still be too large to share if its strings make the final encoded text exceed this cap.

The C# JSON parser rejects duplicate object keys, trailing content, invalid escapes, invalid UTF-8/Unicode, and nesting beyond 32. Known integer fields require JSON integer tokens in 32-bit range: quoted numbers, booleans, null, fractional notation and exponent notation are rejected even if numerically integral. This last lexical restriction is stricter than JavaScript `JSON.parse` followed by `Number.isInteger`; acceptance parity for those spellings requires a strict JSON parser on the web side. All emitted share JSON uses plain integer notation.

Catalog JSON is an object with required `version: 1` and `levels` array; at most 1000 levels, unique level IDs, and at most 4194304 UTF-16 code units. Every level uses the rules above. Unknown catalog fields are ignored.
