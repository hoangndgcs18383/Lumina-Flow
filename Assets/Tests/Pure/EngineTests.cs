#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LuminaFlow.Tests
{
    /// <summary>Dependency-free executable assertions; excluded from Unity player assemblies.</summary>
    internal static class EngineTests
    {
        private static int passed, failed;
        private static string catalogPath;

        public static int Main(string[] args)
        {
            catalogPath = args.Length > 0 ? args[0] : "Assets/Resources/Levels/catalog.json";
            if (args.Length == 3 && args[1] == "--export-interop") return ExportInterop(args[2]);
            if (args.Length == 3 && args[1] == "--export-combinations") return ExportInterop(args[2], true);
            if (args.Length == 3 && args[1] == "--import-interop") return ImportInterop(args[2]);
            Run("all four emitter directions and half-cell board boundaries", Boundaries);
            Run("slash/backslash reflection for all incoming directions", Mirrors);
            Run("splitter sends both branches to independent targets", Splitters);
            Run("mismatched targets still absorb beams", TargetAbsorption);
            Run("walls and emitters block incoming beams", Blockers);
            Run("crystals pass beams and enforce optional color", Crystals);
            Run("filter changes outgoing color at its center", Filters);
            Run("portal preserves direction and omits teleport segment", Portals);
            Run("portal network terminates with no false connecting segments", PortalLoop);
            Run("visited states distinguish beam colors", ColorSensitiveVisits);
            Run("splitter/mirror cycle terminates without losing exits", Cycles);
            Run("chained sensors open doors to a fixed point and reset", ChainedDoors);
            Run("mismatched sensor passes without opening its door", MismatchedSensor);
            Run("door cannot activate its own unreachable sensor", SelfLockedDoor);
            Run("every target required and targetless board cannot win", WinRules);
            Run("evaluation never mutates level data", EvaluationPurity);
            Run("move rotate/undo/reset clone catalog and refresh results", Moves);
            Run("fixed and nonoptical pieces cannot rotate", RotationRestrictions);
            Run("catalog and piece clones do not alias originals", Clones);
            Run("stars reward completion, crystals and par independently", Stars);
            Run("share is raw UTF-8 JSON Base64 with Unicode and hints", ShareRoundTrip);
            Run("omitted fixed defaults by piece type", DefaultFixed);
            Run("reject malformed JSON, Unicode and field types", InvalidJson);
            Run("reject share size, board bounds and duplicate IDs/cells", ShareBounds);
            Run("reject broken portal/sensor-door links and invalid colors", InvalidLinks);
            Run("catalog validates version and unique level IDs", CatalogValidation);
            Run("AES randomization, authentication and wrong-key rejection", Crypto);
            Run("save persists settings, progress, best stars and best moves", Persistence);
            Run("primary corruption recovers and repairs from backup", BackupRecovery);
            Run("first save has a recoverable backup", FirstSaveRecovery);
            Run("interrupted first write recovers a validated temporary file", TemporaryRecovery);
            Run("invalid authenticated save data is rejected", InvalidSave);
            Run("all corrupt saves reset safely and can be saved again", CorruptSaveReset);
            Run("canonical levels solve with hints, all crystals and valid shares", CanonicalLevels);
            Console.WriteLine("Results: " + passed + " passed, " + failed + " failed.");
            return failed == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + "\n" + ex); }
        }
        private static void Check(bool condition, string message = "Assertion failed")
        { if (!condition) throw new Exception(message); }
        private static void Equal<T>(T expected, T actual)
        { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Throws<T>(Action action) where T : Exception
        { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
        private static PieceData P(string id, string type, int x, int y, int rotation = 0, string color = "", string link = "")
        { return new PieceData { id = id, type = type, x = x, y = y, rotation = rotation, color = color, link = link, @fixed = type != "mirror" && type != "splitter" }; }
        private static LevelData L(params PieceData[] pieces)
        { return new LevelData { id = 1, name = "Test", width = 7, height = 7, par = 1, pieces = pieces.ToList() }; }
        private static LevelData Straight()
        { return L(P("e", "emitter", 0, 1, 0, "cyan"), P("t", "target", 6, 1, 0, "cyan")); }

        private static void Boundaries()
        {
            float[] xs = { 6.5f, 3, -0.5f, 3 }, ys = { 3, 6.5f, 3, -0.5f };
            for (int d = 0; d < 4; d++)
            {
                var result = LaserEngine.Evaluate(L(P("e", "emitter", 3, 3, d, "cyan")));
                Equal(1, result.Segments.Count);
                var segment = result.Segments[0];
                Equal(3f, segment.x1); Equal(3f, segment.y1); Equal(xs[d], segment.x2); Equal(ys[d], segment.y2);
                Check(!result.Won);
            }
        }

        private static void Mirrors()
        {
            int[] dx = { 1, 0, -1, 0 }, dy = { 0, 1, 0, -1 };
            int[][] reflected = { new[] { 3, 2, 1, 0 }, new[] { 1, 0, 3, 2 } };
            for (int r = 0; r < 4; r++)
                for (int d = 0; d < 4; d++)
                {
                    int output = reflected[r % 2][d];
                    var result = LaserEngine.Evaluate(L(P("e", "emitter", 3 - dx[d], 3 - dy[d], d, "pink"),
                        P("m", "mirror", 3, 3, r), P("t", "target", 3 + dx[output], 3 + dy[output], 0, "pink")));
                    Check(result.Won, "Mirror rotation " + r + " incoming " + d);
                }
        }

        private static void Splitters()
        {
            for (int rotation = 0; rotation < 2; rotation++)
            {
                var result = LaserEngine.Evaluate(L(P("e", "emitter", 0, 3, 0, "amber"), P("s", "splitter", 3, 3, rotation),
                    P("straight", "target", 6, 3), P("reflected", "target", 3, rotation == 0 ? 0 : 6)));
                Check(result.Won); Equal(2, result.LitTargets.Count); Equal(3, result.Segments.Count);
            }
        }

        private static void TargetAbsorption()
        {
            var level = Straight(); level.pieces.Insert(1, P("wrong", "target", 2, 1, 0, "pink"));
            var result = LaserEngine.Evaluate(level);
            Equal(0, result.LitTargets.Count); Check(!result.Won); Equal(1, result.Segments.Count);
            level.pieces[1].color = "";
            result = LaserEngine.Evaluate(level); Equal(1, result.LitTargets.Count); Check(!result.Won);
        }

        private static void Blockers()
        {
            foreach (string type in new[] { "wall", "emitter" })
            {
                var level = Straight(); level.pieces.Add(P("b", type, 3, 1, 3, type == "emitter" ? "pink" : ""));
                var result = LaserEngine.Evaluate(level); Check(!result.Won); Equal(0, result.LitTargets.Count);
            }
        }

        private static void Crystals()
        {
            var level = Straight();
            level.pieces.AddRange(new[] { P("any", "crystal", 1, 1), P("matching", "crystal", 2, 1, 0, "cyan"), P("wrong", "crystal", 3, 1, 0, "amber") });
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Check(result.LitCrystals.SetEquals(new[] { "any", "matching" }));
        }

        private static void Filters()
        {
            var level = Straight(); level.pieces[1].color = "pink";
            level.pieces.Add(P("f", "filter", 3, 1, 0, "pink"));
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Equal("cyan", result.Segments[0].color); Equal("pink", result.Segments[1].color);
            Equal(3f, result.Segments[0].x2); Equal(3f, result.Segments[1].x1);
        }

        private static void Portals()
        {
            var level = L(P("e", "emitter", 0, 1, 0, "cyan"), P("a", "portal", 1, 1, 1, "", "p"),
                P("b", "portal", 3, 4, 2, "", "p"), P("t", "target", 6, 4, 0, "cyan"));
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Equal(2, result.Segments.Count);
            Equal(1f, result.Segments[0].x2); Equal(3f, result.Segments[1].x1); Equal(4f, result.Segments[1].y1);
            Check(result.Segments.All(s => s.x1 == s.x2 || s.y1 == s.y2));
        }

        private static void PortalLoop()
        {
            // Inject through a splitter. Both portals lie on the loop; no connecting teleport is drawn.
            var level = L(P("e", "emitter", 2, 0, 1, "cyan"), P("s", "splitter", 2, 2, 0),
                P("a", "portal", 1, 2, 0, "", "p"), P("b", "portal", 4, 4, 0, "", "p"),
                P("m1", "mirror", 3, 4, 1), P("m2", "mirror", 3, 2, 1), P("t", "target", 6, 2));
            var result = LaserEngine.Evaluate(level);
            Check(result.Segments.Count < 50);
            Check(result.Segments.All(s => s.x1 == s.x2 || s.y1 == s.y2));
        }

        private static void ColorSensitiveVisits()
        {
            var level = L(P("cyan", "emitter", 0, 2, 0, "cyan"), P("pink", "emitter", 2, 5, 3, "pink"),
                P("s", "splitter", 2, 2, 0), P("t", "target", 6, 2, 0, "pink"));
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Check(result.Segments.Any(s => s.x1 == 2 && s.y1 == 2 && s.x2 == 6 && s.color == "pink"));
        }

        private static void Cycles()
        {
            var level = L(P("e", "emitter", 2, 0, 1, "cyan"), P("s", "splitter", 2, 2, 0),
                P("a", "mirror", 0, 2, 0), P("b", "mirror", 0, 5, 1), P("c", "mirror", 2, 5, 0), P("t", "target", 6, 2));
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Check(result.Segments.Count < 20);
        }

        private static void ChainedDoors()
        {
            var level = L(P("e", "emitter", 0, 1, 0, "cyan"), P("sa", "sensor", 1, 1, 0, "cyan", "a"),
                P("da", "door", 2, 1, 0, "", "a"), P("sb", "sensor", 3, 1, 0, "", "b"),
                P("db", "door", 4, 1, 0, "", "b"), P("t", "target", 6, 1));
            var result = LaserEngine.Evaluate(level);
            Check(result.Won); Check(result.ActiveSensors.SetEquals(new[] { "sa", "sb" }));
            Check(result.OpenDoors.SetEquals(new[] { "da", "db" })); Equal(5, result.Segments.Count);
            level.pieces[0].rotation = 3;
            result = LaserEngine.Evaluate(level); Check(!result.Won); Equal(0, result.OpenDoors.Count); Equal(0, result.ActiveSensors.Count);
            level.pieces[0].rotation = 0;
            Check(LaserEngine.Evaluate(level).Won);
        }

        private static void MismatchedSensor()
        {
            var level = Straight(); level.pieces.Add(P("s", "sensor", 1, 1, 0, "pink", "a"));
            level.pieces.Add(P("d", "door", 4, 1, 0, "", "a"));
            var result = LaserEngine.Evaluate(level);
            Check(!result.Won); Equal(0, result.ActiveSensors.Count); Equal(0, result.OpenDoors.Count);
            Check(result.Segments.Any(s => s.x1 == 1 && s.x2 == 4));
        }

        private static void SelfLockedDoor()
        {
            var level = Straight(); level.pieces.Add(P("d", "door", 1, 1, 0, "", "a"));
            level.pieces.Add(P("s", "sensor", 3, 1, 0, "", "a"));
            var result = LaserEngine.Evaluate(level); Check(!result.Won); Equal(0, result.OpenDoors.Count);
        }

        private static void WinRules()
        {
            var level = Straight(); Check(LaserEngine.Evaluate(level).Won);
            level.pieces.Add(P("other", "target", 2, 4)); Check(!LaserEngine.Evaluate(level).Won);
            level.pieces.RemoveAll(p => p.type == "target"); Check(!LaserEngine.Evaluate(level).Won);
            level.pieces.Clear(); Check(!LaserEngine.Evaluate(level).Won);
        }

        private static void EvaluationPurity()
        {
            var level = Straight(); string before = LevelSerializer.ToJson(level);
            LaserEngine.Evaluate(level); Equal(before, LevelSerializer.ToJson(level));
        }

        private static void Moves()
        {
            var level = L(P("e", "emitter", 0, 3, 0, "cyan"), P("m", "mirror", 3, 3, 1), P("t", "target", 3, 0));
            var manager = new MoveManager(level); int events = 0; manager.Changed += () => events++;
            Check(!manager.Result.Won); Check(!manager.CanUndo); Check(!manager.Undo());
            Check(manager.Rotate("m")); Check(manager.Result.Won); Equal(1, manager.MoveCount); Equal(1, level.pieces[1].rotation);
            Check(manager.Undo()); Check(!manager.Result.Won); Equal(0, manager.MoveCount);
            Check(manager.Rotate("m", -1)); Equal(0, manager.Level.pieces[1].rotation);
            manager.Reset(); Equal(1, manager.Level.pieces[1].rotation); Equal(0, manager.MoveCount); Check(!manager.CanUndo); Equal(4, events);
            Check(!manager.Rotate("missing")); Check(!manager.Rotate("m", 4)); Equal(4, events);
            manager.Level.pieces[1].rotation = 0; manager.Reset(); Equal(1, manager.Level.pieces[1].rotation);
        }

        private static void RotationRestrictions()
        {
            var level = Straight(); level.pieces[0].@fixed = false;
            level.pieces.Add(P("m", "mirror", 3, 3)); level.pieces[2].@fixed = true;
            var manager = new MoveManager(level);
            Check(!manager.Rotate("e")); Check(!manager.Rotate("m")); Check(!manager.Rotate("t")); Equal(0, manager.MoveCount);
        }

        private static void Clones()
        {
            var catalog = new LevelCatalog(); catalog.levels.Add(Straight());
            var clone = catalog.DeepClone(); clone.levels[0].pieces[0].color = "pink"; clone.levels.Add(Straight());
            Equal("cyan", catalog.levels[0].pieces[0].color); Equal(1, catalog.levels.Count);
        }

        private static void Stars()
        {
            var level = Straight(); var result = LaserEngine.Evaluate(level);
            Equal(3, StarRating.Calculate(level, result, 1)); Equal(2, StarRating.Calculate(level, result, 2));
            level.pieces.Add(P("c", "crystal", 2, 4)); result = LaserEngine.Evaluate(level);
            Equal(2, StarRating.Calculate(level, result, 1)); Equal(1, StarRating.Calculate(level, result, 2));
            level.pieces[1].color = "pink"; result = LaserEngine.Evaluate(level); Equal(0, StarRating.Calculate(level, result, 0));
            Throws<ArgumentOutOfRangeException>(() => StarRating.Calculate(level, result, -1));
        }

        private static void ShareRoundTrip()
        {
            var level = Straight(); level.name = "Ánh sáng 🌟 \"one\""; level.subtitle = "line\nnext\\path";
            level.pieces.Add(P("m", "mirror", 3, 4)); level.pieces[2].solution = 3;
            string code = LevelSerializer.Serialize(level);
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(code));
            Check(json.StartsWith("{\"id\":", StringComparison.Ordinal)); Check(json.Contains("\"fixed\""));
            var decoded = LevelSerializer.Deserialize(code);
            Equal(level.name, decoded.name); Equal(level.subtitle, decoded.subtitle); Equal(3, decoded.pieces[2].solution);
            Equal(json, LevelSerializer.ToJson(decoded));
            LevelData imported; string error;
            Check(LevelSerializer.TryDeserialize(code, out imported, out error)); Check(error == null);
            Check(!LevelSerializer.TryDeserialize("nope!", out imported, out error)); Check(imported == null && !string.IsNullOrEmpty(error));
        }

        private static void DefaultFixed()
        {
            var level = Straight(); level.pieces.Add(P("m", "mirror", 3, 3)); level.pieces.Add(P("s", "splitter", 4, 4));
            string json = LevelSerializer.ToJson(level).Replace(",\"fixed\":true", "").Replace(",\"fixed\":false", "");
            var parsed = LevelSerializer.FromJson(json);
            Check(parsed.pieces[0].@fixed); Check(parsed.pieces[1].@fixed); Check(!parsed.pieces[2].@fixed); Check(!parsed.pieces[3].@fixed);
            Equal(-1, parsed.pieces[0].solution);
        }

        private static void InvalidJson()
        {
            string valid = LevelSerializer.ToJson(Straight());
            foreach (string json in new[] { "", "null", "[]", "{}", valid + "x", valid.Replace("\"width\":7", "\"width\":7.0"),
                valid.Replace("\"width\":7", "\"width\":\"7\""), valid.Replace("\"width\":7", "\"width\":07"),
                valid.Replace("\"width\":7", "\"width\":7,\"width\":7"), valid.Replace("\"name\":\"Test\"", "\"name\":\"\\uD800\""),
                valid.Replace("\"name\":\"Test\"", "\"name\":null"), valid.Replace("\"x\":0,", ""), valid.Replace("\"rotation\":0", "\"rotation\":false") })
                Throws<FormatException>(() => LevelSerializer.FromJson(json));
            Throws<FormatException>(() => LevelSerializer.Deserialize(Convert.ToBase64String(new byte[] { 0xff, 0xfe })));
            Throws<FormatException>(() => LevelSerializer.FromJson(new string('[', 40) + new string(']', 40)));
        }

        private static void ShareBounds()
        {
            Throws<FormatException>(() => LevelSerializer.Deserialize(new string('A', LevelSerializer.MaxShareLength + 1)));
            foreach (int size in new[] { 0, -1, 17, int.MaxValue })
            { var level = Straight(); level.width = size; Throws<FormatException>(() => LevelSerializer.Validate(level)); }
            var bad = Straight(); bad.pieces[1].x = -1; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces[1].x = 7; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces[1].id = "e"; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces[1].x = 0; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); for (int i = 0; i < 256; i++) bad.pieces.Add(P("x" + i, "wall", 0, 0));
            Throws<FormatException>(() => LevelSerializer.Validate(bad));
            var max = L(); max.width = max.height = 16;
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) max.pieces.Add(P("p" + x + "_" + y, "wall", x, y));
            Equal(256, LevelSerializer.Deserialize(LevelSerializer.Serialize(max)).pieces.Count);
        }

        private static void InvalidLinks()
        {
            var bad = Straight(); bad.pieces.Add(P("p", "portal", 2, 2, 0, "", "p"));
            Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad.pieces.Add(P("q", "portal", 3, 3, 0, "", "p")); LevelSerializer.Validate(bad);
            bad.pieces.Add(P("r", "portal", 4, 4, 0, "", "p")); Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces.Add(P("s", "sensor", 2, 2, 0, "", "a")); Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces.Add(P("d", "door", 2, 2, 0, "", "a")); Throws<FormatException>(() => LevelSerializer.Validate(bad));
            foreach (string color in new[] { "", "red", "CYAN" })
            { bad = Straight(); bad.pieces[0].color = color; Throws<FormatException>(() => LevelSerializer.Validate(bad)); }
            bad = Straight(); bad.pieces[0].type = "Emitter"; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces[0].rotation = 4; Throws<FormatException>(() => LevelSerializer.Validate(bad));
            bad = Straight(); bad.pieces[0].solution = 1; Throws<FormatException>(() => LevelSerializer.Validate(bad));
        }

        private static void CatalogValidation()
        {
            var catalog = new LevelCatalog(); catalog.levels.Add(Straight());
            var parsed = LevelSerializer.DeserializeCatalog(LevelSerializer.SerializeCatalog(catalog)); Equal(1, parsed.levels.Count);
            catalog.version = 2; Throws<FormatException>(() => LevelSerializer.SerializeCatalog(catalog));
            catalog.version = 1; catalog.levels.Add(Straight()); Throws<FormatException>(() => LevelSerializer.SerializeCatalog(catalog));
        }

        private static void Crypto()
        {
            byte[] plain = Encoding.UTF8.GetBytes("private progress Ánh sáng");
            byte[] first = CryptoUtility.Encrypt(plain, "key"), second = CryptoUtility.Encrypt(plain, "key");
            Check(!first.SequenceEqual(second)); Check(plain.SequenceEqual(CryptoUtility.Decrypt(first, "key")));
            Throws<CryptographicException>(() => CryptoUtility.Decrypt(first, "other key"));
            foreach (int index in new[] { 0, 5, 21, 40, first.Length - 1 })
            { var tampered = (byte[])first.Clone(); tampered[index] ^= 1; Throws<CryptographicException>(() => CryptoUtility.Decrypt(tampered, "key")); }
            Throws<CryptographicException>(() => CryptoUtility.Decrypt(first.Take(first.Length - 1).ToArray(), "key"));
            Equal("", CryptoUtility.Decrypt(CryptoUtility.Encrypt("", "key"), "key"));
        }

        private static void InDirectory(Action<string> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "lumina-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { test(directory); } finally { Directory.Delete(directory, true); }
        }

        private static void Persistence()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test"); Equal(1, save.Load().unlockedLevel); Equal(0, save.Data.TotalStars);
                var level = Straight(); var result = LaserEngine.Evaluate(level);
                Equal(3, save.RecordResult(level, result, 1, 5)); Equal(3, save.GetStars(1)); Check(save.IsUnlocked(5)); Check(!save.IsUnlocked(6)); Check(!save.IsUnlocked(0));
                Equal(2, save.RecordResult(level, result, 3)); Equal(3, save.GetStars(1)); Equal(1, save.Data.levels[0].bestMoves);
                save.Data.settings.music = false; save.Data.settings.sound = false; save.Data.settings.haptics = false; save.Data.settings.skin = "amber"; save.Save();
                var loaded = new SaveManager(directory, "test"); loaded.Load();
                Equal(3, loaded.GetStars(1)); Equal(3, loaded.Data.TotalStars); Equal(5, loaded.Data.unlockedLevel);
                Check(!loaded.Data.settings.music && !loaded.Data.settings.sound && !loaded.Data.settings.haptics); Equal("amber", loaded.Data.settings.skin);
                Check(!Encoding.UTF8.GetString(File.ReadAllBytes(save.FilePath)).Contains("unlockedLevel"));
                result.Won = false; Equal(0, loaded.RecordResult(level, result, 0)); Equal(3, loaded.GetStars(1));
            });
        }

        private static void BackupRecovery()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test"); save.Data.settings.skin = "pink"; save.Save();
                save.Data.settings.skin = "amber"; save.Save();
                File.WriteAllText(save.FilePath, "corrupt");
                var loaded = new SaveManager(directory, "test"); loaded.Load();
                Check(loaded.RecoveredFromBackup); Check(loaded.HadCorruptSave); Equal("pink", loaded.Data.settings.skin);
                var repaired = new SaveManager(directory, "test"); repaired.Load(); Check(!repaired.RecoveredFromBackup); Equal("pink", repaired.Data.settings.skin);
                // Repair preserved the backup instead of replacing it with the corrupt primary.
                File.WriteAllText(repaired.FilePath, "corrupt again"); repaired.Load(); Check(repaired.RecoveredFromBackup); Equal("pink", repaired.Data.settings.skin);
            });
        }

        private static void FirstSaveRecovery()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test"); save.Data.unlockedLevel = 8; save.Save();
                Check(File.Exists(save.BackupPath)); File.Delete(save.FilePath); save.Load();
                Check(save.RecoveredFromBackup); Equal(8, save.Data.unlockedLevel);
            });
        }

        private static void TemporaryRecovery()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test"); save.Data.unlockedLevel = 4; save.Save();
                File.Copy(save.FilePath, save.FilePath + ".tmp"); File.Delete(save.FilePath); File.Delete(save.BackupPath);
                save.Load(); Check(save.RecoveredFromTemporary); Equal(4, save.Data.unlockedLevel); Check(File.Exists(save.FilePath));
            });
        }

        private static void InvalidSave()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test");
                foreach (string json in new[] { "{}", "{\"version\":2,\"unlockedLevel\":1,\"levels\":[],\"settings\":{}}",
                    "{\"version\":1,\"unlockedLevel\":1,\"levels\":[{\"levelId\":1,\"stars\":9,\"bestMoves\":0}],\"settings\":{}}" })
                {
                    File.WriteAllBytes(save.FilePath, CryptoUtility.Encrypt(Encoding.UTF8.GetBytes(json), "test"));
                    save.Load(); Check(save.HadCorruptSave); Equal(0, save.Data.TotalStars); Equal(1, save.Data.unlockedLevel);
                }
            });
        }

        private static void CorruptSaveReset()
        {
            InDirectory(directory =>
            {
                var save = new SaveManager(directory, "test"); save.Save();
                File.WriteAllText(save.FilePath, "bad"); File.WriteAllText(save.BackupPath, "bad"); File.WriteAllText(save.FilePath + ".tmp", "bad");
                save.Load(); Check(save.HadCorruptSave); Equal(1, save.Data.unlockedLevel);
                save.Data.unlockedLevel = 7; save.Save(); save.Load(); Equal(7, save.Data.unlockedLevel);
            });
        }

        private static void CanonicalLevels()
        {
            Check(File.Exists(catalogPath), "Pass the canonical catalog path to the runner.");
            var catalog = LevelSerializer.DeserializeCatalog(File.ReadAllText(catalogPath));
            Check(catalog.levels.Count >= 24, "Expected the 24 canonical levels.");
            foreach (var level in catalog.levels)
            {
                var solved = level.DeepClone();
                int turns = 0;
                foreach (var piece in solved.pieces)
                    if (piece.CanRotate && piece.solution >= 0)
                    { turns += (piece.solution ^ piece.rotation) & 1; piece.rotation = piece.solution; }
                var result = LaserEngine.Evaluate(solved);
                Check(result.Won, "Canonical level " + level.id + " does not solve");
                Check(StarRating.Calculate(solved, result, turns) == 3, "Canonical level " + level.id + " misses three-star solution: moves=" + turns + ", par=" + level.par + ", crystals=" + result.LitCrystals.Count);
                Equal(LevelSerializer.ToJson(level), LevelSerializer.ToJson(LevelSerializer.Deserialize(LevelSerializer.Serialize(level))));
                Check(!LaserEngine.Evaluate(level).Won, "Canonical level " + level.id + " starts solved");
            }
            Console.WriteLine("  Verified " + catalog.levels.Count + " canonical levels.");
        }

        [Serializable]
        public sealed class InteropCase
        {
            public string label;
            public string code;
            public int levelId;
            public int mask;
            public List<string> rotatableIds;
            public List<int> rotations;
            public bool won;
            public List<string> litTargets, litCrystals, activeSensors, openDoors;
        }

        private static int ExportInterop(string path, bool allCombinations = false)
        {
            var catalog = LevelSerializer.DeserializeCatalog(File.ReadAllText(catalogPath));
            var cases = new List<InteropCase>();
            foreach (var level in catalog.levels)
            {
                int count = level.pieces.Count(p => p.CanRotate);
                if (allCombinations && count > 16) throw new InvalidOperationException("Exhaustive export supports up to 16 rotatable pieces per level.");
                int combinations = allCombinations ? 1 << count : 2;
                for (int variant = 0; variant < combinations; variant++)
                {
                    var copy = level.DeepClone();
                    if (allCombinations)
                    {
                        int index = 0;
                        foreach (var piece in copy.pieces)
                            if (piece.CanRotate) piece.rotation = (variant >> index++) & 1;
                    }
                    else if (variant == 1)
                        foreach (var piece in copy.pieces)
                            if (piece.CanRotate && piece.solution >= 0) piece.rotation = piece.solution;
                    var result = LaserEngine.Evaluate(copy);
                    cases.Add(new InteropCase
                    {
                        label = "level " + level.id + (allCombinations ? " mask " + variant : variant == 1 ? " solved" : " initial"),
                        levelId = level.id, mask = allCombinations ? variant : -1,
                        rotatableIds = copy.pieces.Where(p => p.CanRotate).Select(p => p.id).ToList(),
                        rotations = copy.pieces.Where(p => p.CanRotate).Select(p => p.rotation).ToList(),
                        code = LevelSerializer.Serialize(copy), won = result.Won,
                        litTargets = result.LitTargets.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                        litCrystals = result.LitCrystals.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                        activeSensors = result.ActiveSensors.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                        openDoors = result.OpenDoors.OrderBy(x => x, StringComparer.Ordinal).ToList()
                    });
                }
            }
            File.WriteAllText(path, JsonCodec.Write(cases));
            Console.WriteLine("Exported " + cases.Count + " C# share/result fixtures.");
            return 0;
        }

        private static int ImportInterop(string path)
        {
            var cases = JsonCodec.ConvertTo<List<InteropCase>>(JsonCodec.Read(File.ReadAllText(path)));
            foreach (var test in cases)
            {
                var level = LevelSerializer.Deserialize(test.code);
                var result = LaserEngine.Evaluate(level);
                Check(result.Won == test.won && result.LitTargets.SetEquals(test.litTargets) && result.LitCrystals.SetEquals(test.litCrystals)
                    && result.ActiveSensors.SetEquals(test.activeSensors) && result.OpenDoors.SetEquals(test.openDoors), "JavaScript/C# disagreement: " + test.label);
            }
            Console.WriteLine("PASS JavaScript → C# shares and results: " + cases.Count + " fixtures.");
            return 0;
        }
    }
}
#endif
