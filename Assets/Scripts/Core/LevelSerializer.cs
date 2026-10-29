using System;
using System.Collections.Generic;
using System.Text;

namespace LuminaFlow
{
    /// <summary>Raw UTF-8 LevelData JSON encoded as Base64. Treat imported levels as untrusted data.</summary>
    public static class LevelSerializer
    {
        public const int MaxShareLength = 64 * 1024;
        public const int MaxBoardSize = 16;
        public const int MaxPieces = 256;
        private const int MaxCatalogLength = 4 * 1024 * 1024;
        private static readonly HashSet<string> Types = new HashSet<string>(StringComparer.Ordinal)
        { "emitter", "mirror", "splitter", "portal", "target", "crystal", "wall", "filter", "sensor", "door" };

        public static string Serialize(LevelData level)
        {
            string code = Convert.ToBase64String(JsonCodec.Utf8.GetBytes(ToJson(level)));
            if (code.Length > MaxShareLength) throw new FormatException("Share code exceeds 64 KiB.");
            return code;
        }

        public static LevelData Deserialize(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > MaxShareLength)
                throw new FormatException("Share code is empty or exceeds 64 KiB.");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(code); }
            catch (FormatException) { throw new FormatException("Share code is not valid Base64."); }
            try { return FromJson(JsonCodec.Utf8.GetString(bytes)); }
            catch (DecoderFallbackException) { throw new FormatException("Share code is not valid UTF-8."); }
        }

        public static bool TryDeserialize(string code, out LevelData level, out string error)
        {
            try { level = Deserialize(code); error = null; return true; }
            catch (FormatException ex) { level = null; error = ex.Message; return false; }
        }

        public static string ToJson(LevelData level)
        {
            Validate(level);
            return JsonCodec.Write(level);
        }

        public static LevelData FromJson(string json)
        {
            if (json == null || json.Length > MaxShareLength) throw new FormatException("Level JSON is missing or too large.");
            object tree = JsonCodec.Read(json);
            RequireLevelFields(tree);
            LevelData level = JsonCodec.ConvertTo<LevelData>(tree);
            Validate(level);
            return level;
        }

        public static LevelCatalog DeserializeCatalog(string json)
        {
            if (json == null || json.Length > MaxCatalogLength) throw new FormatException("Catalog is missing or too large.");
            var map = JsonCodec.Read(json) as Dictionary<string, object>;
            RequireFields(map, "version", "levels");
            var levels = map["levels"] as List<object>;
            if (levels == null || levels.Count > 1000) throw new FormatException("Catalog levels must be an array of up to 1000 levels.");
            foreach (object level in levels) RequireLevelFields(level);
            var catalog = JsonCodec.ConvertTo<LevelCatalog>(map);
            ValidateCatalog(catalog);
            return catalog;
        }

        public static string SerializeCatalog(LevelCatalog catalog)
        {
            ValidateCatalog(catalog);
            string json = JsonCodec.Write(catalog);
            if (json.Length > MaxCatalogLength) throw new FormatException("Catalog is too large.");
            return json;
        }

        public static void Validate(LevelData level)
        {
            if (level == null) throw new FormatException("Level is missing.");
            if (level.id < 1) throw new FormatException("Level id must be positive.");
            Text(level.name, 120, "Level name"); Text(level.subtitle, 512, "Level subtitle");
            if (level.width < 1 || level.height < 1 || level.width > MaxBoardSize || level.height > MaxBoardSize)
                throw new FormatException("Board dimensions must be between 1 and 16.");
            if (level.par < 0 || level.par > 1000000) throw new FormatException("Par must be between 0 and 1000000.");
            if (level.pieces == null || level.pieces.Count > MaxPieces) throw new FormatException("Level may contain at most 256 pieces.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var cells = new HashSet<int>();
            var portals = new Dictionary<string, int>(StringComparer.Ordinal);
            var sensorLinks = new HashSet<string>(StringComparer.Ordinal);
            var doorLinks = new HashSet<string>(StringComparer.Ordinal);
            foreach (PieceData p in level.pieces)
            {
                if (p == null) throw new FormatException("Piece is missing.");
                Text(p.id, 64, "Piece id");
                if (string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id)) throw new FormatException("Piece IDs must be nonempty and unique.");
                if (p.type == null || !Types.Contains(p.type)) throw new FormatException("Unknown piece type.");
                if (p.x < 0 || p.y < 0 || p.x >= level.width || p.y >= level.height || !cells.Add(p.y * level.width + p.x))
                    throw new FormatException("Pieces must occupy distinct cells inside the board.");
                if (p.rotation < 0 || p.rotation > 3) throw new FormatException("Rotation must be 0, 1, 2, or 3.");
                if (p.solution < -1 || p.solution > 3 || (p.solution >= 0 && p.type != "mirror" && p.type != "splitter"))
                    throw new FormatException("Solution must be -1 or a mirror/splitter orientation from 0 to 3.");
                Text(p.color, 8, "Piece color"); Text(p.link, 64, "Piece link");
                if (p.color != "" && p.color != "cyan" && p.color != "pink" && p.color != "amber")
                    throw new FormatException("Color must be cyan, pink, amber, or empty.");
                if ((p.type == "emitter" || p.type == "filter") && p.color == "")
                    throw new FormatException("Emitters and filters require a color.");
                if (p.type == "portal" || p.type == "sensor" || p.type == "door")
                {
                    if (string.IsNullOrWhiteSpace(p.link)) throw new FormatException("Portals, sensors, and doors require a link.");
                    if (p.type == "sensor") sensorLinks.Add(p.link);
                    if (p.type == "door") doorLinks.Add(p.link);
                    if (p.type == "portal")
                    { int count; portals.TryGetValue(p.link, out count); portals[p.link] = count + 1; }
                }
            }
            foreach (int count in portals.Values)
                if (count != 2) throw new FormatException("Each portal link must pair exactly two portals.");
            if (!sensorLinks.SetEquals(doorLinks)) throw new FormatException("Each sensor/door link requires at least one sensor and one door.");
        }

        private static void Text(string value, int maxLength, string name)
        {
            if (value == null || value.Length > maxLength) throw new FormatException(name + " is missing or too long.");
            try { JsonCodec.Utf8.GetByteCount(value); }
            catch (EncoderFallbackException) { throw new FormatException(name + " contains invalid Unicode."); }
        }

        private static void ValidateCatalog(LevelCatalog catalog)
        {
            if (catalog == null || catalog.version != 1 || catalog.levels == null || catalog.levels.Count > 1000)
                throw new FormatException("Expected a version 1 level catalog.");
            var ids = new HashSet<int>();
            foreach (LevelData level in catalog.levels)
            { Validate(level); if (!ids.Add(level.id)) throw new FormatException("Catalog level IDs must be unique."); }
        }

        private static void RequireLevelFields(object value)
        {
            var map = value as Dictionary<string, object>;
            RequireFields(map, "id", "name", "width", "height", "par", "pieces");
            var pieces = map["pieces"] as List<object>;
            if (pieces == null || pieces.Count > MaxPieces) throw new FormatException("Expected an array of up to 256 pieces.");
            foreach (object piece in pieces)
                RequireFields(piece as Dictionary<string, object>, "id", "type", "x", "y", "rotation");
        }

        private static void RequireFields(Dictionary<string, object> map, params string[] names)
        {
            if (map == null) throw new FormatException("Expected a JSON object.");
            foreach (string name in names)
                if (!map.ContainsKey(name)) throw new FormatException("Missing required field: " + name);
        }
    }
}
